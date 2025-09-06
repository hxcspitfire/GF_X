//------------------------------------------------------------
// 技能管理器 - 管理技能释放、冷却、等级等
//------------------------------------------------------------
using GameFramework;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 技能释放事件参数
/// </summary>
public class SkillCastEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(SkillCastEventArgs).GetHashCode();

    public override int Id => EventId;

    public int SkillId { get; private set; }
    public int CasterId { get; private set; }
    public Vector3 TargetPosition { get; private set; }
    public int TargetId { get; private set; }
    public bool IsSuccess { get; private set; }

    public static SkillCastEventArgs Create(int skillId, int casterId, Vector3 targetPosition, int targetId, bool isSuccess)
    {
        var args = ReferencePool.Acquire<SkillCastEventArgs>();
        args.SkillId = skillId;
        args.CasterId = casterId;
        args.TargetPosition = targetPosition;
        args.TargetId = targetId;
        args.IsSuccess = isSuccess;
        return args;
    }

    public override void Clear()
    {
        SkillId = 0;
        CasterId = 0;
        TargetPosition = Vector3.zero;
        TargetId = 0;
        IsSuccess = false;
    }
}

/// <summary>
/// 技能管理器
/// </summary>
public class SkillManager : MonoBehaviour
{
    private Dictionary<int, float> m_Cooldowns = new Dictionary<int, float>();
    private Dictionary<int, bool> m_CastingSkills = new Dictionary<int, bool>();
    private SkillDataModel m_SkillDataModel;
    private PlayerDataModel m_PlayerDataModel;

    /// <summary>
    /// 技能是否在冷却中
    /// </summary>
    public bool IsSkillOnCooldown(int skillId)
    {
        if (!m_Cooldowns.ContainsKey(skillId)) return false;
        return m_Cooldowns[skillId] > Time.time;
    }

    /// <summary>
    /// 获取技能冷却剩余时间
    /// </summary>
    public float GetSkillCooldownRemaining(int skillId)
    {
        if (!m_Cooldowns.ContainsKey(skillId)) return 0f;
        float remaining = m_Cooldowns[skillId] - Time.time;
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// 获取技能冷却进度 (0-1)
    /// </summary>
    public float GetSkillCooldownProgress(int skillId)
    {
        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        var skillRow = skillTable.GetDataRow(skillId);
        if (skillRow == null) return 0f;

        var skillData = m_SkillDataModel.GetSkillData(skillId);
        var levelTable = GF.DataTable.GetDataTable<SkillLevelTable>();
        var levelRow = levelTable.GetDataRow(skillId * 100 + skillData.Level);
        
        float totalCooldown = skillRow.BaseCooldown;
        if (levelRow != null)
        {
            totalCooldown += levelRow.CooldownModifier;
        }

        float remaining = GetSkillCooldownRemaining(skillId);
        return 1f - (remaining / totalCooldown);
    }

    /// <summary>
    /// 检查技能是否可以释放
    /// </summary>
    public bool CanCastSkill(int skillId)
    {
        // 检查技能是否解锁
        if (!m_SkillDataModel.IsSkillUnlocked(skillId)) return false;

        // 检查是否在冷却中
        if (IsSkillOnCooldown(skillId)) return false;

        // 检查是否正在释放其他技能
        if (IsCastingSkill()) return false;

        // 检查魔法值是否足够
        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        var skillRow = skillTable.GetDataRow(skillId);
        if (skillRow == null) return false;

        var skillData = m_SkillDataModel.GetSkillData(skillId);
        var levelTable = GF.DataTable.GetDataTable<SkillLevelTable>();
        var levelRow = levelTable.GetDataRow(skillId * 100 + skillData.Level);
        
        int manaCost = skillRow.BaseManaCost;
        if (levelRow != null)
        {
            manaCost += levelRow.ManaCostModifier;
        }

        if (m_PlayerDataModel.Hp < manaCost) return false;

        return true;
    }

    /// <summary>
    /// 释放技能
    /// </summary>
    public async UniTask<bool> CastSkill(int skillId, Vector3 targetPosition, int targetId = 0)
    {
        if (!CanCastSkill(skillId)) return false;

        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        var skillRow = skillTable.GetDataRow(skillId);
        if (skillRow == null) return false;

        var skillData = m_SkillDataModel.GetSkillData(skillId);
        var levelTable = GF.DataTable.GetDataTable<SkillLevelTable>();
        var levelRow = levelTable.GetDataRow(skillId * 100 + skillData.Level);

        // 消耗魔法值
        int manaCost = skillRow.BaseManaCost;
        if (levelRow != null)
        {
            manaCost += levelRow.ManaCostModifier;
        }
        m_PlayerDataModel.Hp -= manaCost;

        // 设置冷却时间
        float cooldown = skillRow.BaseCooldown;
        if (levelRow != null)
        {
            cooldown += levelRow.CooldownModifier;
        }
        m_Cooldowns[skillId] = Time.time + cooldown;

        // 设置释放状态
        m_CastingSkills[skillId] = true;

        // 播放音效
        if (!string.IsNullOrEmpty(skillRow.SoundEffect))
        {
            GF.Sound.PlayEffect(skillRow.SoundEffect);
        }

        // 播放动画
        if (!string.IsNullOrEmpty(skillRow.AnimationName))
        {
            var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1); // 假设玩家Entity ID为1
            if (playerEntity != null)
            {
                var animator = playerEntity.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(skillRow.AnimationName);
                }
            }
        }

        // 等待施法时间
        if (skillRow.CastTime > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(skillRow.CastTime));
        }

        // 执行技能效果
        bool success = await ExecuteSkillEffect(skillId, targetPosition, targetId);

        // 清除释放状态
        m_CastingSkills.Remove(skillId);

        // 触发技能释放事件
        GF.Event.Fire(this, SkillCastEventArgs.Create(skillId, 1, targetPosition, targetId, success));

        return success;
    }

    /// <summary>
    /// 执行技能效果
    /// </summary>
    private async UniTask<bool> ExecuteSkillEffect(int skillId, Vector3 targetPosition, int targetId)
    {
        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        var skillRow = skillTable.GetDataRow(skillId);
        if (skillRow == null) return false;

        var skillData = m_SkillDataModel.GetSkillData(skillId);
        var levelTable = GF.DataTable.GetDataTable<SkillLevelTable>();
        var levelRow = levelTable.GetDataRow(skillId * 100 + skillData.Level);

        // 计算技能属性
        float damage = skillRow.BaseDamage;
        float range = skillRow.SkillRange;
        float duration = skillRow.Duration;

        if (levelRow != null)
        {
            damage += levelRow.DamageModifier;
            range += levelRow.RangeModifier;
            duration += levelRow.DurationModifier;
        }

        // 根据技能类型执行不同效果
        switch (skillRow.SkillCategory)
        {
            case SkillCategory.Attack:
                return await ExecuteAttackSkill(skillId, targetPosition, targetId, damage, range);
            case SkillCategory.Defense:
                return await ExecuteDefenseSkill(skillId, targetPosition, targetId, damage, duration);
            case SkillCategory.Support:
                return await ExecuteSupportSkill(skillId, targetPosition, targetId, damage, duration);
            case SkillCategory.Movement:
                return await ExecuteMovementSkill(skillId, targetPosition, targetId, range);
            default:
                return false;
        }
    }

    /// <summary>
    /// 执行攻击技能
    /// </summary>
    private async UniTask<bool> ExecuteAttackSkill(int skillId, Vector3 targetPosition, int targetId, float damage, float range)
    {
        // 创建技能特效
        if (!string.IsNullOrEmpty(skillId.ToString()))
        {
            var effectParams = EntityParams.Create(targetPosition);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, 2f);
            GF.Entity.ShowEffect($"Skill/Skill_{skillId}", effectParams, 2f);
        }

        // 查找范围内的敌人
        var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1);
        if (playerEntity == null) return false;

        var hitsList = JobsPhysics.OverlapSphereNearest(
            CombatUnitEntity.CombatFlag.Player, 
            new Vector3[] { targetPosition }, 
            range);

        // 对范围内的敌人造成伤害
        for (int i = 0; i < hitsList.Length; i++)
        {
            int entityId = hitsList[i];
            if (entityId == 0) break;
            
            if (GF.Entity.HasEntity(entityId))
            {
                var enemy = GF.Entity.GetEntity<CombatUnitEntity>(entityId);
                if (enemy != null)
                {
                    enemy.Attack(playerEntity, (int)damage);
                }
            }
        }

        hitsList.Dispose();
        return true;
    }

    /// <summary>
    /// 执行防御技能
    /// </summary>
    private async UniTask<bool> ExecuteDefenseSkill(int skillId, Vector3 targetPosition, int targetId, float value, float duration)
    {
        // 添加防御Buff
        var buffManager = GetComponent<BuffManager>();
        if (buffManager != null)
        {
            // 假设防御Buff的ID为1000 + skillId
            int buffId = 1000 + skillId;
            buffManager.AddBuff(buffId, 1, duration);
        }
        return true;
    }

    /// <summary>
    /// 执行辅助技能
    /// </summary>
    private async UniTask<bool> ExecuteSupportSkill(int skillId, Vector3 targetPosition, int targetId, float value, float duration)
    {
        // 添加辅助Buff
        var buffManager = GetComponent<BuffManager>();
        if (buffManager != null)
        {
            // 假设辅助Buff的ID为2000 + skillId
            int buffId = 2000 + skillId;
            buffManager.AddBuff(buffId, 1, duration);
        }
        return true;
    }

    /// <summary>
    /// 执行移动技能
    /// </summary>
    private async UniTask<bool> ExecuteMovementSkill(int skillId, Vector3 targetPosition, int targetId, float range)
    {
        var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1);
        if (playerEntity == null) return false;

        // 瞬移到目标位置
        playerEntity.CachedTransform.position = targetPosition;
        return true;
    }

    /// <summary>
    /// 检查是否正在释放技能
    /// </summary>
    public bool IsCastingSkill()
    {
        foreach (var casting in m_CastingSkills.Values)
        {
            if (casting) return true;
        }
        return false;
    }

    /// <summary>
    /// 中断技能释放
    /// </summary>
    public void InterruptSkill(int skillId)
    {
        if (m_CastingSkills.ContainsKey(skillId))
        {
            m_CastingSkills[skillId] = false;
        }
    }

    /// <summary>
    /// 重置所有技能冷却
    /// </summary>
    public void ResetAllCooldowns()
    {
        m_Cooldowns.Clear();
    }

    private void Start()
    {
        m_SkillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();
        m_PlayerDataModel = GF.DataModel.GetOrCreate<PlayerDataModel>();
    }

    private void Update()
    {
        // 更新技能冷却
        var keysToRemove = new List<int>();
        foreach (var kvp in m_Cooldowns)
        {
            if (kvp.Value <= Time.time)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            m_Cooldowns.Remove(key);
        }
    }
}