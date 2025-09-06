//------------------------------------------------------------
// 基于属性的Buff管理器 - 通过修改角色属性来实现Buff效果
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using System.Collections.Generic;

/// <summary>
/// 基于属性的Buff管理器
/// </summary>
public class AttributeBasedBuffManager : MonoBehaviour
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
                ReapplyBuffEffect(existingBuff);
                
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
        ApplyBuffEffect(buffInstance);

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

        // 移除效果
        RemoveBuffEffect(buffInstance);

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
    private void ApplyBuffEffect(BuffInstance buffInstance)
    {
        var buffData = buffInstance.BuffData;
        var target = buffInstance.Target;
        var attributes = target.GetComponent<CharacterAttributes>();
        
        if (attributes == null) return;

        // 根据属性类型修改角色属性
        switch (buffData.AttributeType)
        {
            case BuffAttributeType.Silence:
                attributes.CanCastSkill = false;
                break;
            case BuffAttributeType.Stun:
                attributes.CanCastSkill = false;
                attributes.CanMove = false;
                attributes.CanAttack = false;
                break;
            case BuffAttributeType.Root:
                attributes.CanMove = false;
                break;
            case BuffAttributeType.Fear:
                attributes.CanCastSkill = false;
                attributes.CanAttack = false;
                break;
            case BuffAttributeType.Charm:
                attributes.CanCastSkill = false;
                attributes.CanAttack = false;
                break;
            case BuffAttributeType.Attack:
                ApplyAttributeModifier(attributes, "Attack", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Defense:
                ApplyAttributeModifier(attributes, "Defense", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Speed:
                ApplyAttributeModifier(attributes, "MoveSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.AttackSpeed:
                ApplyAttributeModifier(attributes, "AttackSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalRate:
                ApplyAttributeModifier(attributes, "CriticalRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalDamage:
                ApplyAttributeModifier(attributes, "CriticalDamage", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.DodgeRate:
                ApplyAttributeModifier(attributes, "DodgeRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.BlockRate:
                ApplyAttributeModifier(attributes, "BlockRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Immunity:
                attributes.IsImmune = true;
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
    private void RemoveBuffEffect(BuffInstance buffInstance)
    {
        var buffData = buffInstance.BuffData;
        var target = buffInstance.Target;
        var attributes = target.GetComponent<CharacterAttributes>();
        
        if (attributes == null) return;

        // 根据属性类型恢复角色属性
        switch (buffData.AttributeType)
        {
            case BuffAttributeType.Silence:
                attributes.CanCastSkill = true;
                break;
            case BuffAttributeType.Stun:
                attributes.CanCastSkill = true;
                attributes.CanMove = true;
                attributes.CanAttack = true;
                break;
            case BuffAttributeType.Root:
                attributes.CanMove = true;
                break;
            case BuffAttributeType.Fear:
                attributes.CanCastSkill = true;
                attributes.CanAttack = true;
                break;
            case BuffAttributeType.Charm:
                attributes.CanCastSkill = true;
                attributes.CanAttack = true;
                break;
            case BuffAttributeType.Attack:
                RemoveAttributeModifier(attributes, "Attack", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Defense:
                RemoveAttributeModifier(attributes, "Defense", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Speed:
                RemoveAttributeModifier(attributes, "MoveSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.AttackSpeed:
                RemoveAttributeModifier(attributes, "AttackSpeed", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalRate:
                RemoveAttributeModifier(attributes, "CriticalRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalDamage:
                RemoveAttributeModifier(attributes, "CriticalDamage", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.DodgeRate:
                RemoveAttributeModifier(attributes, "DodgeRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.BlockRate:
                RemoveAttributeModifier(attributes, "BlockRate", buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Immunity:
                attributes.IsImmune = false;
                break;
        }
    }

    /// <summary>
    /// 应用属性修正
    /// </summary>
    private void ApplyAttributeModifier(CharacterAttributes attributes, string attributeName, BuffTable buffData, int stacks)
    {
        float value = buffData.AttributeValue * stacks;
        
        switch (attributeName)
        {
            case "Attack":
                attributes.DamageModifier += value / 100f; // 百分比修正
                break;
            case "Defense":
                attributes.DefenseModifier += value / 100f;
                break;
            case "MoveSpeed":
                attributes.MoveSpeedModifier += value / 100f;
                break;
            case "AttackSpeed":
                attributes.AttackSpeedModifier += value / 100f;
                break;
            case "CriticalRate":
                attributes.CriticalRate += value / 100f;
                break;
            case "CriticalDamage":
                attributes.CriticalDamage += value / 100f;
                break;
            case "DodgeRate":
                attributes.DodgeRate += value / 100f;
                break;
            case "BlockRate":
                attributes.BlockRate += value / 100f;
                break;
        }
    }

    /// <summary>
    /// 移除属性修正
    /// </summary>
    private void RemoveAttributeModifier(CharacterAttributes attributes, string attributeName, BuffTable buffData, int stacks)
    {
        float value = buffData.AttributeValue * stacks;
        
        switch (attributeName)
        {
            case "Attack":
                attributes.DamageModifier -= value / 100f;
                break;
            case "Defense":
                attributes.DefenseModifier -= value / 100f;
                break;
            case "MoveSpeed":
                attributes.MoveSpeedModifier -= value / 100f;
                break;
            case "AttackSpeed":
                attributes.AttackSpeedModifier -= value / 100f;
                break;
            case "CriticalRate":
                attributes.CriticalRate -= value / 100f;
                break;
            case "CriticalDamage":
                attributes.CriticalDamage -= value / 100f;
                break;
            case "DodgeRate":
                attributes.DodgeRate -= value / 100f;
                break;
            case "BlockRate":
                attributes.BlockRate -= value / 100f;
                break;
        }
    }

    /// <summary>
    /// 重新应用Buff效果
    /// </summary>
    private void ReapplyBuffEffect(BuffInstance buffInstance)
    {
        // 先移除效果
        RemoveBuffEffect(buffInstance);
        // 再应用效果
        ApplyBuffEffect(buffInstance);
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