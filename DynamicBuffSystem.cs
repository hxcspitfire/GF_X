//------------------------------------------------------------
// 动态Buff系统 - 基于动态属性系统实现Buff效果
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 动态Buff系统
/// </summary>
public class DynamicBuffSystem : MonoBehaviour
{
    private Dictionary<int, List<BuffInstance>> m_TargetBuffs = new Dictionary<int, List<BuffInstance>>();
    private Dictionary<int, BuffInstance> m_AllBuffs = new Dictionary<int, BuffInstance>();
    private int m_NextBuffInstanceId = 1;

    /// <summary>
    /// 添加Buff
    /// </summary>
    public int AddBuff(int buffId, int level, float duration, EntityLogic target = null, EntityLogic caster = null)
    {
        var buffTable = GF.DataTable.GetDataTable<BuffTable>();
        var buffRow = buffTable.GetDataRow(buffId);
        if (buffRow == null) return 0;

        if (target == null)
        {
            var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1);
            if (playerEntity == null) return 0;
            target = playerEntity;
        }

        int targetId = target.Entity.Id;
        int casterId = caster?.Entity.Id ?? 0;

        // 获取目标的动态属性系统
        var attributeSystem = target.GetComponent<DynamicAttributeSystem>();
        if (attributeSystem == null) return 0;

        // 检查是否已存在相同类型的Buff
        var existingBuff = GetBuffByType(targetId, buffId);
        if (existingBuff != null)
        {
            if (buffRow.CanStack)
            {
                // 可以叠加，增加层数
                int newStacks = Mathf.Min(existingBuff.Stacks + 1, buffRow.MaxStacks);
                existingBuff.Stacks = newStacks;
                existingBuff.Duration = Mathf.Max(existingBuff.Duration, duration);
                
                // 重新应用效果（考虑层数变化）
                ReapplyBuffEffect(existingBuff, attributeSystem);
                
                GF.Event.Fire(this, BuffAddedEventArgs.Create(buffId, targetId, casterId, newStacks));
                return existingBuff.BuffId;
            }
            else
            {
                // 不能叠加，刷新持续时间
                existingBuff.Duration = duration;
                existingBuff.StartTime = Time.time;
                return existingBuff.BuffId;
            }
        }

        // 创建新的Buff实例
        var buffInstance = new BuffInstance
        {
            BuffId = m_NextBuffInstanceId++,
            Level = level,
            Stacks = 1,
            Duration = duration,
            StartTime = Time.time,
            LastTriggerTime = Time.time,
            BuffData = buffRow,
            Target = target,
            Caster = caster
        };

        // 添加到目标Buff列表
        if (!m_TargetBuffs.ContainsKey(targetId))
        {
            m_TargetBuffs[targetId] = new List<BuffInstance>();
        }
        m_TargetBuffs[targetId].Add(buffInstance);
        m_AllBuffs[buffInstance.BuffId] = buffInstance;

        // 应用Buff效果
        ApplyBuffEffect(buffInstance, attributeSystem);

        // 播放特效
        if (!string.IsNullOrEmpty(buffRow.EffectPrefab))
        {
            var effectParams = EntityParams.Create(target.CachedTransform.position);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, duration);
            GF.Entity.ShowEffect(buffRow.EffectPrefab, effectParams, duration);
        }

        // 播放音效
        if (!string.IsNullOrEmpty(buffRow.SoundEffect))
        {
            GF.Sound.PlayEffect(buffRow.SoundEffect);
        }

        // 触发事件
        GF.Event.Fire(this, BuffAddedEventArgs.Create(buffId, targetId, casterId, 1));

        return buffInstance.BuffId;
    }

    /// <summary>
    /// 移除Buff
    /// </summary>
    public bool RemoveBuff(int buffInstanceId)
    {
        if (!m_AllBuffs.ContainsKey(buffInstanceId)) return false;

        var buffInstance = m_AllBuffs[buffInstanceId];
        int targetId = buffInstance.Target.Entity.Id;
        int casterId = buffInstance.Caster?.Entity.Id ?? 0;

        // 获取目标的动态属性系统
        var attributeSystem = buffInstance.Target.GetComponent<DynamicAttributeSystem>();
        if (attributeSystem != null)
        {
            // 移除效果
            RemoveBuffEffect(buffInstance, attributeSystem);
        }

        // 从列表中移除
        if (m_TargetBuffs.ContainsKey(targetId))
        {
            m_TargetBuffs[targetId].Remove(buffInstance);
            if (m_TargetBuffs[targetId].Count == 0)
            {
                m_TargetBuffs.Remove(targetId);
            }
        }
        m_AllBuffs.Remove(buffInstanceId);

        // 触发事件
        GF.Event.Fire(this, BuffRemovedEventArgs.Create(buffInstance.BuffData.Id, targetId, casterId));

        return true;
    }

    /// <summary>
    /// 应用Buff效果
    /// </summary>
    private void ApplyBuffEffect(BuffInstance buffInstance, DynamicAttributeSystem attributeSystem)
    {
        var buffData = buffInstance.BuffData;
        var target = buffInstance.Target;

        // 根据属性类型修改角色属性
        switch (buffData.AttributeType)
        {
            case BuffAttributeType.Silence:
                attributeSystem.SetBoolAttribute("CanCastSkill", false);
                break;
            case BuffAttributeType.Stun:
                attributeSystem.SetBoolAttribute("CanCastSkill", false);
                attributeSystem.SetBoolAttribute("CanMove", false);
                attributeSystem.SetBoolAttribute("CanAttack", false);
                break;
            case BuffAttributeType.Root:
                attributeSystem.SetBoolAttribute("CanMove", false);
                break;
            case BuffAttributeType.Fear:
                attributeSystem.SetBoolAttribute("CanCastSkill", false);
                attributeSystem.SetBoolAttribute("CanAttack", false);
                break;
            case BuffAttributeType.Charm:
                attributeSystem.SetBoolAttribute("CanCastSkill", false);
                attributeSystem.SetBoolAttribute("CanAttack", false);
                break;
            case BuffAttributeType.Attack:
                ApplyAttributeModifier(attributeSystem, "Attack", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Defense:
                ApplyAttributeModifier(attributeSystem, "Defense", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Speed:
                ApplyAttributeModifier(attributeSystem, "MoveSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.AttackSpeed:
                ApplyAttributeModifier(attributeSystem, "AttackSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalRate:
                ApplyAttributeModifier(attributeSystem, "CriticalRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalDamage:
                ApplyAttributeModifier(attributeSystem, "CriticalDamage", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.DodgeRate:
                ApplyAttributeModifier(attributeSystem, "DodgeRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.BlockRate:
                ApplyAttributeModifier(attributeSystem, "BlockRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Immunity:
                attributeSystem.SetBoolAttribute("IsImmune", true);
                break;
            case BuffAttributeType.Damage:
                StartDamageOverTime(buffInstance);
                break;
            case BuffAttributeType.Heal:
                StartHealOverTime(buffInstance);
                break;
        }
    }

    /// <summary>
    /// 移除Buff效果
    /// </summary>
    private void RemoveBuffEffect(BuffInstance buffInstance, DynamicAttributeSystem attributeSystem)
    {
        var buffData = buffInstance.BuffData;
        var target = buffInstance.Target;

        // 根据属性类型恢复角色属性
        switch (buffData.AttributeType)
        {
            case BuffAttributeType.Silence:
                attributeSystem.SetBoolAttribute("CanCastSkill", true);
                break;
            case BuffAttributeType.Stun:
                attributeSystem.SetBoolAttribute("CanCastSkill", true);
                attributeSystem.SetBoolAttribute("CanMove", true);
                attributeSystem.SetBoolAttribute("CanAttack", true);
                break;
            case BuffAttributeType.Root:
                attributeSystem.SetBoolAttribute("CanMove", true);
                break;
            case BuffAttributeType.Fear:
                attributeSystem.SetBoolAttribute("CanCastSkill", true);
                attributeSystem.SetBoolAttribute("CanAttack", true);
                break;
            case BuffAttributeType.Charm:
                attributeSystem.SetBoolAttribute("CanCastSkill", true);
                attributeSystem.SetBoolAttribute("CanAttack", true);
                break;
            case BuffAttributeType.Attack:
                RemoveAttributeModifier(attributeSystem, "Attack", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Defense:
                RemoveAttributeModifier(attributeSystem, "Defense", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Speed:
                RemoveAttributeModifier(attributeSystem, "MoveSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.AttackSpeed:
                RemoveAttributeModifier(attributeSystem, "AttackSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalRate:
                RemoveAttributeModifier(attributeSystem, "CriticalRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalDamage:
                RemoveAttributeModifier(attributeSystem, "CriticalDamage", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.DodgeRate:
                RemoveAttributeModifier(attributeSystem, "DodgeRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.BlockRate:
                RemoveAttributeModifier(attributeSystem, "BlockRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Immunity:
                attributeSystem.SetBoolAttribute("IsImmune", false);
                break;
        }
    }

    /// <summary>
    /// 应用属性修正
    /// </summary>
    private void ApplyAttributeModifier(DynamicAttributeSystem attributeSystem, string attributeName, BuffTable buffData, int stacks)
    {
        float value = buffData.AttributeValue * stacks;
        
        switch (attributeName)
        {
            case "Attack":
                // 攻击力修正通过伤害修正实现
                float currentDamageModifier = attributeSystem.GetFloatAttribute("DamageModifier");
                attributeSystem.SetFloatAttribute("DamageModifier", currentDamageModifier + value / 100f);
                break;
            case "Defense":
                // 防御力修正通过防御修正实现
                float currentDefenseModifier = attributeSystem.GetFloatAttribute("DefenseModifier");
                attributeSystem.SetFloatAttribute("DefenseModifier", currentDefenseModifier + value / 100f);
                break;
            case "MoveSpeed":
                // 移动速度修正
                float currentMoveSpeedModifier = attributeSystem.GetFloatAttribute("MoveSpeedModifier");
                attributeSystem.SetFloatAttribute("MoveSpeedModifier", currentMoveSpeedModifier + value / 100f);
                break;
            case "AttackSpeed":
                // 攻击速度修正
                float currentAttackSpeedModifier = attributeSystem.GetFloatAttribute("AttackSpeedModifier");
                attributeSystem.SetFloatAttribute("AttackSpeedModifier", currentAttackSpeedModifier + value / 100f);
                break;
            case "CriticalRate":
                // 暴击率修正
                float currentCriticalRate = attributeSystem.GetFloatAttribute("CriticalRate");
                attributeSystem.SetFloatAttribute("CriticalRate", currentCriticalRate + value / 100f);
                break;
            case "CriticalDamage":
                // 暴击伤害修正
                float currentCriticalDamage = attributeSystem.GetFloatAttribute("CriticalDamage");
                attributeSystem.SetFloatAttribute("CriticalDamage", currentCriticalDamage + value / 100f);
                break;
            case "DodgeRate":
                // 闪避率修正
                float currentDodgeRate = attributeSystem.GetFloatAttribute("DodgeRate");
                attributeSystem.SetFloatAttribute("DodgeRate", currentDodgeRate + value / 100f);
                break;
            case "BlockRate":
                // 格挡率修正
                float currentBlockRate = attributeSystem.GetFloatAttribute("BlockRate");
                attributeSystem.SetFloatAttribute("BlockRate", currentBlockRate + value / 100f);
                break;
        }
    }

    /// <summary>
    /// 移除属性修正
    /// </summary>
    private void RemoveAttributeModifier(DynamicAttributeSystem attributeSystem, string attributeName, BuffTable buffData, int stacks)
    {
        float value = buffData.AttributeValue * stacks;
        
        switch (attributeName)
        {
            case "Attack":
                float currentDamageModifier = attributeSystem.GetFloatAttribute("DamageModifier");
                attributeSystem.SetFloatAttribute("DamageModifier", currentDamageModifier - value / 100f);
                break;
            case "Defense":
                float currentDefenseModifier = attributeSystem.GetFloatAttribute("DefenseModifier");
                attributeSystem.SetFloatAttribute("DefenseModifier", currentDefenseModifier - value / 100f);
                break;
            case "MoveSpeed":
                float currentMoveSpeedModifier = attributeSystem.GetFloatAttribute("MoveSpeedModifier");
                attributeSystem.SetFloatAttribute("MoveSpeedModifier", currentMoveSpeedModifier - value / 100f);
                break;
            case "AttackSpeed":
                float currentAttackSpeedModifier = attributeSystem.GetFloatAttribute("AttackSpeedModifier");
                attributeSystem.SetFloatAttribute("AttackSpeedModifier", currentAttackSpeedModifier - value / 100f);
                break;
            case "CriticalRate":
                float currentCriticalRate = attributeSystem.GetFloatAttribute("CriticalRate");
                attributeSystem.SetFloatAttribute("CriticalRate", currentCriticalRate - value / 100f);
                break;
            case "CriticalDamage":
                float currentCriticalDamage = attributeSystem.GetFloatAttribute("CriticalDamage");
                attributeSystem.SetFloatAttribute("CriticalDamage", currentCriticalDamage - value / 100f);
                break;
            case "DodgeRate":
                float currentDodgeRate = attributeSystem.GetFloatAttribute("DodgeRate");
                attributeSystem.SetFloatAttribute("DodgeRate", currentDodgeRate - value / 100f);
                break;
            case "BlockRate":
                float currentBlockRate = attributeSystem.GetFloatAttribute("BlockRate");
                attributeSystem.SetFloatAttribute("BlockRate", currentBlockRate - value / 100f);
                break;
        }
    }

    /// <summary>
    /// 重新应用Buff效果
    /// </summary>
    private void ReapplyBuffEffect(BuffInstance buffInstance, DynamicAttributeSystem attributeSystem)
    {
        // 先移除效果
        RemoveBuffEffect(buffInstance, attributeSystem);
        // 再应用效果
        ApplyBuffEffect(buffInstance, attributeSystem);
    }

    /// <summary>
    /// 开始持续伤害
    /// </summary>
    private async void StartDamageOverTime(BuffInstance buffInstance)
    {
        while (!buffInstance.IsExpired && buffInstance.Target != null)
        {
            if (buffInstance.CanTrigger)
            {
                float damage = CalculateBuffValue(buffInstance.BuffData, buffInstance.Stacks);
                if (buffInstance.Target is CombatUnitEntity combatUnit)
                {
                    combatUnit.Hp -= (int)damage;
                }
                buffInstance.LastTriggerTime = Time.time;
            }
            await UniTask.Delay(100);
        }
    }

    /// <summary>
    /// 开始持续治疗
    /// </summary>
    private async void StartHealOverTime(BuffInstance buffInstance)
    {
        while (!buffInstance.IsExpired && buffInstance.Target != null)
        {
            if (buffInstance.CanTrigger)
            {
                float heal = CalculateBuffValue(buffInstance.BuffData, buffInstance.Stacks);
                if (buffInstance.Target is CombatUnitEntity combatUnit)
                {
                    combatUnit.Hp += (int)heal;
                }
                buffInstance.LastTriggerTime = Time.time;
            }
            await UniTask.Delay(100);
        }
    }

    /// <summary>
    /// 计算Buff数值
    /// </summary>
    private float CalculateBuffValue(BuffTable buffData, int stacks)
    {
        float value = buffData.AttributeValue;
        if (buffData.ModifierType == BuffModifierType.Multiply)
        {
            value *= stacks;
        }
        else if (buffData.ModifierType == BuffModifierType.Percent)
        {
            value *= stacks;
        }
        return value;
    }

    /// <summary>
    /// 获取目标的Buff
    /// </summary>
    public List<BuffInstance> GetTargetBuffs(int targetId)
    {
        if (m_TargetBuffs.ContainsKey(targetId))
        {
            return new List<BuffInstance>(m_TargetBuffs[targetId]);
        }
        return new List<BuffInstance>();
    }

    /// <summary>
    /// 获取指定类型的Buff
    /// </summary>
    public BuffInstance GetBuffByType(int targetId, int buffTypeId)
    {
        if (!m_TargetBuffs.ContainsKey(targetId)) return null;

        foreach (var buff in m_TargetBuffs[targetId])
        {
            if (buff.BuffData.Id == buffTypeId)
            {
                return buff;
            }
        }
        return null;
    }

    /// <summary>
    /// 检查目标是否有指定类型的Buff
    /// </summary>
    public bool HasBuff(int targetId, int buffTypeId)
    {
        return GetBuffByType(targetId, buffTypeId) != null;
    }

    private void Update()
    {
        // 更新所有Buff
        var expiredBuffs = new List<int>();
        foreach (var kvp in m_AllBuffs)
        {
            if (kvp.Value.IsExpired)
            {
                expiredBuffs.Add(kvp.Key);
            }
        }

        foreach (var buffId in expiredBuffs)
        {
            RemoveBuff(buffId);
        }
    }
}