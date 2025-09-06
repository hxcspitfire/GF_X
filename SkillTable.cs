//------------------------------------------------------------
// 技能表 - 配置所有技能的基础信息
//------------------------------------------------------------
using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
/// <summary>
/// 技能表
/// </summary>
public class SkillTable : DataRowBase
{
    private int m_Id = 0;
    public override int Id
    {
        get { return m_Id; }
    }

    /// <summary>
    /// 技能名称
    /// </summary>
    public string SkillName { get; private set; }

    /// <summary>
    /// 技能描述
    /// </summary>
    public string SkillDescription { get; private set; }

    /// <summary>
    /// 技能图标资源名
    /// </summary>
    public string SkillIcon { get; private set; }

    /// <summary>
    /// 技能类型 (Active=主动技能, Passive=被动技能)
    /// </summary>
    public SkillType SkillType { get; private set; }

    /// <summary>
    /// 技能分类 (Attack=攻击, Defense=防御, Support=辅助, Movement=移动)
    /// </summary>
    public SkillCategory SkillCategory { get; private set; }

    /// <summary>
    /// 基础冷却时间(秒)
    /// </summary>
    public float BaseCooldown { get; private set; }

    /// <summary>
    /// 基础魔法消耗
    /// </summary>
    public int BaseManaCost { get; private set; }

    /// <summary>
    /// 基础伤害
    /// </summary>
    public float BaseDamage { get; private set; }

    /// <summary>
    /// 技能范围
    /// </summary>
    public float SkillRange { get; private set; }

    /// <summary>
    /// 技能持续时间(秒)
    /// </summary>
    public float Duration { get; private set; }

    /// <summary>
    /// 技能特效预制体
    /// </summary>
    public string EffectPrefab { get; private set; }

    /// <summary>
    /// 技能音效
    /// </summary>
    public string SoundEffect { get; private set; }

    /// <summary>
    /// 最大等级
    /// </summary>
    public int MaxLevel { get; private set; }

    /// <summary>
    /// 解锁等级要求
    /// </summary>
    public int UnlockLevel { get; private set; }

    /// <summary>
    /// 前置技能ID(用逗号分隔)
    /// </summary>
    public string PrerequisiteSkills { get; private set; }

    /// <summary>
    /// 技能目标类型 (Self=自身, Enemy=敌人, Ally=友军, Ground=地面)
    /// </summary>
    public SkillTargetType TargetType { get; private set; }

    /// <summary>
    /// 是否可被打断
    /// </summary>
    public bool CanBeInterrupted { get; private set; }

    /// <summary>
    /// 技能施法时间(秒)
    /// </summary>
    public float CastTime { get; private set; }

    /// <summary>
    /// 技能动画名称
    /// </summary>
    public string AnimationName { get; private set; }

    public override bool ParseDataRow(string dataRowString, object userData)
    {
        string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
        for (int i = 0; i < columnStrings.Length; i++)
        {
            columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
        }

        int index = 0;
        index++;
        m_Id = int.Parse(columnStrings[index++]);
        index++;
        SkillName = columnStrings[index++];
        SkillDescription = columnStrings[index++];
        SkillIcon = columnStrings[index++];
        SkillType = (SkillType)int.Parse(columnStrings[index++]);
        SkillCategory = (SkillCategory)int.Parse(columnStrings[index++]);
        BaseCooldown = float.Parse(columnStrings[index++]);
        BaseManaCost = int.Parse(columnStrings[index++]);
        BaseDamage = float.Parse(columnStrings[index++]);
        SkillRange = float.Parse(columnStrings[index++]);
        Duration = float.Parse(columnStrings[index++]);
        EffectPrefab = columnStrings[index++];
        SoundEffect = columnStrings[index++];
        MaxLevel = int.Parse(columnStrings[index++]);
        UnlockLevel = int.Parse(columnStrings[index++]);
        PrerequisiteSkills = columnStrings[index++];
        TargetType = (SkillTargetType)int.Parse(columnStrings[index++]);
        CanBeInterrupted = bool.Parse(columnStrings[index++]);
        CastTime = float.Parse(columnStrings[index++]);
        AnimationName = columnStrings[index++];

        return true;
    }

    public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
    {
        using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
        {
            using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
            {
                m_Id = binaryReader.Read7BitEncodedInt32();
                SkillName = binaryReader.ReadString();
                SkillDescription = binaryReader.ReadString();
                SkillIcon = binaryReader.ReadString();
                SkillType = (SkillType)binaryReader.Read7BitEncodedInt32();
                SkillCategory = (SkillCategory)binaryReader.Read7BitEncodedInt32();
                BaseCooldown = binaryReader.ReadSingle();
                BaseManaCost = binaryReader.Read7BitEncodedInt32();
                BaseDamage = binaryReader.ReadSingle();
                SkillRange = binaryReader.ReadSingle();
                Duration = binaryReader.ReadSingle();
                EffectPrefab = binaryReader.ReadString();
                SoundEffect = binaryReader.ReadString();
                MaxLevel = binaryReader.Read7BitEncodedInt32();
                UnlockLevel = binaryReader.Read7BitEncodedInt32();
                PrerequisiteSkills = binaryReader.ReadString();
                TargetType = (SkillTargetType)binaryReader.Read7BitEncodedInt32();
                CanBeInterrupted = binaryReader.ReadBoolean();
                CastTime = binaryReader.ReadSingle();
                AnimationName = binaryReader.ReadString();
            }
        }

        return true;
    }
}

/// <summary>
/// 技能类型
/// </summary>
public enum SkillType
{
    Active = 0,     // 主动技能
    Passive = 1     // 被动技能
}

/// <summary>
/// 技能分类
/// </summary>
public enum SkillCategory
{
    Attack = 0,     // 攻击
    Defense = 1,    // 防御
    Support = 2,    // 辅助
    Movement = 3    // 移动
}

/// <summary>
/// 技能目标类型
/// </summary>
public enum SkillTargetType
{
    Self = 0,       // 自身
    Enemy = 1,      // 敌人
    Ally = 2,       // 友军
    Ground = 3      // 地面
}