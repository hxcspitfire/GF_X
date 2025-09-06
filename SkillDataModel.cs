//------------------------------------------------------------
// 技能数据模型 - 管理玩家技能数据
//------------------------------------------------------------
using GameFramework;
using GameFramework.Event;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 技能数据类型
/// </summary>
[Serializable]
public enum SkillDataType
{
    SkillLevel,     // 技能等级
    SkillUnlocked,  // 技能解锁状态
    SkillCooldown,  // 技能冷却时间
    SkillPoints     // 技能点数
}

/// <summary>
/// 技能数据
/// </summary>
[Serializable]
public class SkillData
{
    [JsonProperty]
    public int SkillId { get; set; }
    
    [JsonProperty]
    public int Level { get; set; }
    
    [JsonProperty]
    public bool IsUnlocked { get; set; }
    
    [JsonProperty]
    public float LastCastTime { get; set; }
    
    [JsonProperty]
    public int Experience { get; set; }

    public SkillData()
    {
        SkillId = 0;
        Level = 0;
        IsUnlocked = false;
        LastCastTime = 0f;
        Experience = 0;
    }

    public SkillData(int skillId)
    {
        SkillId = skillId;
        Level = 0;
        IsUnlocked = false;
        LastCastTime = 0f;
        Experience = 0;
    }
}

/// <summary>
/// 技能数据变更事件参数
/// </summary>
public class SkillDataChangedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(SkillDataChangedEventArgs).GetHashCode();

    public override int Id => EventId;

    public int SkillId { get; private set; }
    public SkillDataType DataType { get; private set; }
    public int OldValue { get; private set; }
    public int NewValue { get; private set; }
    public float OldFloatValue { get; private set; }
    public float NewFloatValue { get; private set; }

    public static SkillDataChangedEventArgs Create(int skillId, SkillDataType dataType, int oldValue, int newValue)
    {
        var args = ReferencePool.Acquire<SkillDataChangedEventArgs>();
        args.SkillId = skillId;
        args.DataType = dataType;
        args.OldValue = oldValue;
        args.NewValue = newValue;
        args.OldFloatValue = 0f;
        args.NewFloatValue = 0f;
        return args;
    }

    public static SkillDataChangedEventArgs Create(int skillId, SkillDataType dataType, float oldValue, float newValue)
    {
        var args = ReferencePool.Acquire<SkillDataChangedEventArgs>();
        args.SkillId = skillId;
        args.DataType = dataType;
        args.OldValue = 0;
        args.NewValue = 0;
        args.OldFloatValue = oldValue;
        args.NewFloatValue = newValue;
        return args;
    }

    public override void Clear()
    {
        SkillId = 0;
        DataType = SkillDataType.SkillLevel;
        OldValue = 0;
        NewValue = 0;
        OldFloatValue = 0f;
        NewFloatValue = 0f;
    }
}

/// <summary>
/// 技能数据模型
/// </summary>
public class SkillDataModel : DataModelStorageBase
{
    [JsonProperty]
    private Dictionary<int, SkillData> m_PlayerSkills;
    
    [JsonProperty]
    private int m_SkillPoints;

    /// <summary>
    /// 技能点数
    /// </summary>
    public int SkillPoints
    {
        get => m_SkillPoints;
        set
        {
            int oldValue = m_SkillPoints;
            m_SkillPoints = Mathf.Max(0, value);
            if (oldValue != m_SkillPoints)
            {
                GF.Event.Fire(this, SkillDataChangedEventArgs.Create(0, SkillDataType.SkillPoints, oldValue, m_SkillPoints));
            }
        }
    }

    public SkillDataModel()
    {
        m_PlayerSkills = new Dictionary<int, SkillData>();
        m_SkillPoints = 0;
    }

    protected override void OnInitialDataModel()
    {
        m_SkillPoints = GF.Config.GetInt("DefaultSkillPoints", 0);
        
        // 初始化基础技能
        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        if (skillTable != null)
        {
            foreach (var skillRow in skillTable.GetAllDataRows())
            {
                if (skillRow.UnlockLevel <= 1) // 1级解锁的技能
                {
                    UnlockSkill(skillRow.Id);
                }
            }
        }
    }

    /// <summary>
    /// 获取技能数据
    /// </summary>
    public SkillData GetSkillData(int skillId)
    {
        if (!m_PlayerSkills.ContainsKey(skillId))
        {
            m_PlayerSkills[skillId] = new SkillData(skillId);
        }
        return m_PlayerSkills[skillId];
    }

    /// <summary>
    /// 设置技能等级
    /// </summary>
    public void SetSkillLevel(int skillId, int level, bool triggerEvent = true)
    {
        var skillData = GetSkillData(skillId);
        int oldLevel = skillData.Level;
        skillData.Level = Mathf.Max(0, level);
        
        if (triggerEvent && oldLevel != skillData.Level)
        {
            GF.Event.Fire(this, SkillDataChangedEventArgs.Create(skillId, SkillDataType.SkillLevel, oldLevel, skillData.Level));
        }
    }

    /// <summary>
    /// 解锁技能
    /// </summary>
    public void UnlockSkill(int skillId, bool triggerEvent = true)
    {
        var skillData = GetSkillData(skillId);
        if (!skillData.IsUnlocked)
        {
            skillData.IsUnlocked = true;
            if (triggerEvent)
            {
                GF.Event.Fire(this, SkillDataChangedEventArgs.Create(skillId, SkillDataType.SkillUnlocked, 0, 1));
            }
        }
    }

    /// <summary>
    /// 设置技能冷却时间
    /// </summary>
    public void SetSkillCooldown(int skillId, float cooldownTime, bool triggerEvent = true)
    {
        var skillData = GetSkillData(skillId);
        float oldTime = skillData.LastCastTime;
        skillData.LastCastTime = cooldownTime;
        
        if (triggerEvent)
        {
            GF.Event.Fire(this, SkillDataChangedEventArgs.Create(skillId, SkillDataType.SkillCooldown, oldTime, cooldownTime));
        }
    }

    /// <summary>
    /// 检查技能是否解锁
    /// </summary>
    public bool IsSkillUnlocked(int skillId)
    {
        return GetSkillData(skillId).IsUnlocked;
    }

    /// <summary>
    /// 检查技能是否可以升级
    /// </summary>
    public bool CanUpgradeSkill(int skillId)
    {
        var skillData = GetSkillData(skillId);
        if (!skillData.IsUnlocked) return false;
        
        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        var skillRow = skillTable.GetDataRow(skillId);
        if (skillRow == null) return false;
        
        if (skillData.Level >= skillRow.MaxLevel) return false;
        
        var levelTable = GF.DataTable.GetDataTable<SkillLevelTable>();
        var levelRow = levelTable.GetDataRow(skillId * 100 + skillData.Level + 1);
        if (levelRow == null) return false;
        
        return m_SkillPoints >= levelRow.UpgradeCost;
    }

    /// <summary>
    /// 升级技能
    /// </summary>
    public bool UpgradeSkill(int skillId)
    {
        if (!CanUpgradeSkill(skillId)) return false;
        
        var skillData = GetSkillData(skillId);
        var levelTable = GF.DataTable.GetDataTable<SkillLevelTable>();
        var levelRow = levelTable.GetDataRow(skillId * 100 + skillData.Level + 1);
        
        if (levelRow != null)
        {
            m_SkillPoints -= levelRow.UpgradeCost;
            SetSkillLevel(skillId, skillData.Level + 1);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// 获取所有已解锁的技能
    /// </summary>
    public List<SkillData> GetUnlockedSkills()
    {
        var unlockedSkills = new List<SkillData>();
        foreach (var skillData in m_PlayerSkills.Values)
        {
            if (skillData.IsUnlocked)
            {
                unlockedSkills.Add(skillData);
            }
        }
        return unlockedSkills;
    }

    /// <summary>
    /// 获取技能冷却剩余时间
    /// </summary>
    public float GetSkillCooldownRemaining(int skillId)
    {
        var skillData = GetSkillData(skillId);
        if (skillData.LastCastTime <= 0) return 0f;
        
        float remaining = skillData.LastCastTime - Time.time;
        return Mathf.Max(0f, remaining);
    }
}