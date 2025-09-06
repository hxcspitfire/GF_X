//------------------------------------------------------------
// Buff管理器 - 管理Buff和Debuff状态
//------------------------------------------------------------
using GameFramework;
using GameFramework.Event;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// Buff实例
/// </summary>
public class BuffInstance
{
    public int BuffId { get; set; }
    public int Level { get; set; }
    public int Stacks { get; set; }
    public float Duration { get; set; }
    public float StartTime { get; set; }
    public float LastTriggerTime { get; set; }
    public BuffTable BuffData { get; set; }
    public EntityLogic Target { get; set; }
    public EntityLogic Caster { get; set; }

    public BuffInstance()
    {
        BuffId = 0;
        Level = 1;
        Stacks = 1;
        Duration = 0f;
        StartTime = 0f;
        LastTriggerTime = 0f;
        BuffData = null;
        Target = null;
        Caster = null;
    }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => Time.time - StartTime >= Duration;

    /// <summary>
    /// 剩余时间
    /// </summary>
    public float RemainingTime => Mathf.Max(0f, Duration - (Time.time - StartTime));

    /// <summary>
    /// 是否可以触发
    /// </summary>
    public bool CanTrigger
    {
        get
        {
            if (BuffData == null || BuffData.TriggerInterval <= 0) return false;
            return Time.time - LastTriggerTime >= BuffData.TriggerInterval;
        }
    }
}

/// <summary>
/// Buff添加事件参数
/// </summary>
public class BuffAddedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(BuffAddedEventArgs).GetHashCode();

    public override int Id => EventId;

    public int BuffId { get; private set; }
    public int TargetId { get; private set; }
    public int CasterId { get; private set; }
    public int Stacks { get; private set; }

    public static BuffAddedEventArgs Create(int buffId, int targetId, int casterId, int stacks)
    {
        var args = ReferencePool.Acquire<BuffAddedEventArgs>();
        args.BuffId = buffId;
        args.TargetId = targetId;
        args.CasterId = casterId;
        args.Stacks = stacks;
        return args;
    }

    public override void Clear()
    {
        BuffId = 0;
        TargetId = 0;
        CasterId = 0;
        Stacks = 0;
    }
}

/// <summary>
/// Buff移除事件参数
/// </summary>
public class BuffRemovedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(BuffRemovedEventArgs).GetHashCode();

    public override int Id => EventId;

    public int BuffId { get; private set; }
    public int TargetId { get; private set; }
    public int CasterId { get; private set; }

    public static BuffRemovedEventArgs Create(int buffId, int targetId, int casterId)
    {
        var args = ReferencePool.Acquire<BuffRemovedEventArgs>();
        args.BuffId = buffId;
        args.TargetId = targetId;
        args.CasterId = casterId;
        return args;
    }

    public override void Clear()
    {
        BuffId = 0;
        TargetId = 0;
        CasterId = 0;
    }
}

/// <summary>
/// Buff管理器
/// </summary>
public class BuffManager : MonoBehaviour
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
    /// 移除目标的所有Buff
    /// </summary>
    public void RemoveAllBuffs(int targetId, bool onlyDebuffs = false)
    {
        if (!m_TargetBuffs.ContainsKey(targetId)) return;

        var buffsToRemove = new List<BuffInstance>();
        foreach (var buff in m_TargetBuffs[targetId])
        {
            if (!onlyDebuffs || buff.BuffData.BuffType == BuffType.Debuff)
            {
                buffsToRemove.Add(buff);
            }
        }

        foreach (var buff in buffsToRemove)
        {
            RemoveBuff(buff.BuffId);
        }
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

    /// <summary>
    /// 应用Buff效果
    /// </summary>
    private void ApplyBuffEffect(BuffInstance buffInstance)
    {
        var buffData = buffInstance.BuffData;
        var target = buffInstance.Target;

        // 根据属性类型应用效果
        switch (buffData.AttributeType)
        {
            case BuffAttributeType.Health:
                ApplyHealthEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Mana:
                ApplyManaEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Attack:
                ApplyAttackEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Defense:
                ApplyDefenseEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Speed:
                ApplySpeedEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.AttackSpeed:
                ApplyAttackSpeedEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalRate:
                ApplyCriticalRateEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalDamage:
                ApplyCriticalDamageEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.DodgeRate:
                ApplyDodgeRateEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.BlockRate:
                ApplyBlockRateEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Damage:
                StartDamageOverTime(buffInstance);
                break;
            case BuffAttributeType.Heal:
                StartHealOverTime(buffInstance);
                break;
            case BuffAttributeType.Immunity:
                ApplyImmunityEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Stun:
                ApplyStunEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Silence:
                ApplySilenceEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Slow:
                ApplySlowEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Root:
                ApplyRootEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Fear:
                ApplyFearEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Charm:
                ApplyCharmEffect(target, buffData, buffInstance.Stacks);
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

        // 根据属性类型移除效果
        switch (buffData.AttributeType)
        {
            case BuffAttributeType.Health:
                RemoveHealthEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Mana:
                RemoveManaEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Attack:
                RemoveAttackEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Defense:
                RemoveDefenseEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Speed:
                RemoveSpeedEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.AttackSpeed:
                RemoveAttackSpeedEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalRate:
                RemoveCriticalRateEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.CriticalDamage:
                RemoveCriticalDamageEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.DodgeRate:
                RemoveDodgeRateEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.BlockRate:
                RemoveBlockRateEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Immunity:
                RemoveImmunityEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Stun:
                RemoveStunEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Silence:
                RemoveSilenceEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Slow:
                RemoveSlowEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Root:
                RemoveRootEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Fear:
                RemoveFearEffect(target, buffData, buffInstance.Stacks);
                break;
            case BuffAttributeType.Charm:
                RemoveCharmEffect(target, buffData, buffInstance.Stacks);
                break;
        }
    }

    /// <summary>
    /// 应用生命值效果
    /// </summary>
    private void ApplyHealthEffect(EntityLogic target, BuffTable buffData, int stacks)
    {
        if (target is CombatUnitEntity combatUnit)
        {
            float value = CalculateBuffValue(buffData, stacks);
            if (buffData.ModifierType == BuffModifierType.Add)
            {
                combatUnit.Hp += (int)value;
            }
            else if (buffData.ModifierType == BuffModifierType.Percent)
            {
                combatUnit.Hp += (int)(combatUnit.Hp * value / 100f);
            }
        }
    }

    /// <summary>
    /// 移除生命值效果
    /// </summary>
    private void RemoveHealthEffect(EntityLogic target, BuffTable buffData, int stacks)
    {
        // 通常生命值Buff移除时不需要特殊处理
    }

    /// <summary>
    /// 应用攻击力效果
    /// </summary>
    private void ApplyAttackEffect(EntityLogic target, BuffTable buffData, int stacks)
    {
        // 这里需要根据实际的属性系统来实现
        // 例如：target.GetComponent<CombatStats>().AddAttackModifier(buffData.Id, value);
    }

    /// <summary>
    /// 移除攻击力效果
    /// </summary>
    private void RemoveAttackEffect(EntityLogic target, BuffTable buffData, int stacks)
    {
        // 移除攻击力修正
        // target.GetComponent<CombatStats>().RemoveAttackModifier(buffData.Id);
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
            await UniTask.Delay(100); // 每100ms检查一次
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
            await UniTask.Delay(100); // 每100ms检查一次
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

    // 其他属性效果的实现方法...
    private void ApplyManaEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveManaEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveDefenseEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyDefenseEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplySpeedEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveSpeedEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyAttackSpeedEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveAttackSpeedEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyCriticalRateEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveCriticalRateEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyCriticalDamageEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveCriticalDamageEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyDodgeRateEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveDodgeRateEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyBlockRateEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveBlockRateEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyImmunityEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveImmunityEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyStunEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveStunEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplySilenceEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveSilenceEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplySlowEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveSlowEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyRootEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveRootEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyFearEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveFearEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void ApplyCharmEffect(EntityLogic target, BuffTable buffData, int stacks) { }
    private void RemoveCharmEffect(EntityLogic target, BuffTable buffData, int stacks) { }

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