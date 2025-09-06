//------------------------------------------------------------
// Buff状态表 - 配置所有Buff和Debuff效果
//------------------------------------------------------------
using GameFramework;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
/// <summary>
/// Buff状态表
/// </summary>
public class BuffTable : DataRowBase
{
    private int m_Id = 0;
    public override int Id
    {
        get { return m_Id; }
    }

    /// <summary>
    /// Buff名称
    /// </summary>
    public string BuffName { get; private set; }

    /// <summary>
    /// Buff描述
    /// </summary>
    public string BuffDescription { get; private set; }

    /// <summary>
    /// Buff图标
    /// </summary>
    public string BuffIcon { get; private set; }

    /// <summary>
    /// Buff类型 (Buff=增益, Debuff=减益)
    /// </summary>
    public BuffType BuffType { get; private set; }

    /// <summary>
    /// 影响属性类型
    /// </summary>
    public BuffAttributeType AttributeType { get; private set; }

    /// <summary>
    /// 属性修改值
    /// </summary>
    public float AttributeValue { get; private set; }

    /// <summary>
    /// 修改方式 (Add=加法, Multiply=乘法, Percent=百分比)
    /// </summary>
    public BuffModifierType ModifierType { get; private set; }

    /// <summary>
    /// 持续时间(秒)
    /// </summary>
    public float Duration { get; private set; }

    /// <summary>
    /// 是否可叠加
    /// </summary>
    public bool CanStack { get; private set; }

    /// <summary>
    /// 最大叠加层数
    /// </summary>
    public int MaxStacks { get; private set; }

    /// <summary>
    /// 是否可被驱散
    /// </summary>
    public bool CanBeDispelled { get; private set; }

    /// <summary>
    /// 是否可被净化
    /// </summary>
    public bool CanBeCleansed { get; private set; }

    /// <summary>
    /// 触发间隔(秒, 0表示不触发)
    /// </summary>
    public float TriggerInterval { get; private set; }

    /// <summary>
    /// 触发效果ID
    /// </summary>
    public int TriggerEffectId { get; private set; }

    /// <summary>
    /// 特效预制体
    /// </summary>
    public string EffectPrefab { get; private set; }

    /// <summary>
    /// 音效
    /// </summary>
    public string SoundEffect { get; private set; }

    /// <summary>
    /// 优先级(数值越大优先级越高)
    /// </summary>
    public int Priority { get; private set; }

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
        BuffName = columnStrings[index++];
        BuffDescription = columnStrings[index++];
        BuffIcon = columnStrings[index++];
        BuffType = (BuffType)int.Parse(columnStrings[index++]);
        AttributeType = (BuffAttributeType)int.Parse(columnStrings[index++]);
        AttributeValue = float.Parse(columnStrings[index++]);
        ModifierType = (BuffModifierType)int.Parse(columnStrings[index++]);
        Duration = float.Parse(columnStrings[index++]);
        CanStack = bool.Parse(columnStrings[index++]);
        MaxStacks = int.Parse(columnStrings[index++]);
        CanBeDispelled = bool.Parse(columnStrings[index++]);
        CanBeCleansed = bool.Parse(columnStrings[index++]);
        TriggerInterval = float.Parse(columnStrings[index++]);
        TriggerEffectId = int.Parse(columnStrings[index++]);
        EffectPrefab = columnStrings[index++];
        SoundEffect = columnStrings[index++];
        Priority = int.Parse(columnStrings[index++]);

        return true;
    }

    public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
    {
        using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
        {
            using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
            {
                m_Id = binaryReader.Read7BitEncodedInt32();
                BuffName = binaryReader.ReadString();
                BuffDescription = binaryReader.ReadString();
                BuffIcon = binaryReader.ReadString();
                BuffType = (BuffType)binaryReader.Read7BitEncodedInt32();
                AttributeType = (BuffAttributeType)binaryReader.Read7BitEncodedInt32();
                AttributeValue = binaryReader.ReadSingle();
                ModifierType = (BuffModifierType)binaryReader.Read7BitEncodedInt32();
                Duration = binaryReader.ReadSingle();
                CanStack = binaryReader.ReadBoolean();
                MaxStacks = binaryReader.Read7BitEncodedInt32();
                CanBeDispelled = binaryReader.ReadBoolean();
                CanBeCleansed = binaryReader.ReadBoolean();
                TriggerInterval = binaryReader.ReadSingle();
                TriggerEffectId = binaryReader.Read7BitEncodedInt32();
                EffectPrefab = binaryReader.ReadString();
                SoundEffect = binaryReader.ReadString();
                Priority = binaryReader.Read7BitEncodedInt32();
            }
        }

        return true;
    }
}

/// <summary>
/// Buff类型
/// </summary>
public enum BuffType
{
    Buff = 0,       // 增益
    Debuff = 1      // 减益
}

/// <summary>
/// Buff影响属性类型
/// </summary>
public enum BuffAttributeType
{
    None = 0,           // 无
    Health = 1,         // 生命值
    Mana = 2,           // 魔法值
    Attack = 3,         // 攻击力
    Defense = 4,        // 防御力
    Speed = 5,          // 移动速度
    AttackSpeed = 6,    // 攻击速度
    CriticalRate = 7,   // 暴击率
    CriticalDamage = 8, // 暴击伤害
    DodgeRate = 9,      // 闪避率
    BlockRate = 10,     // 格挡率
    Damage = 11,        // 持续伤害
    Heal = 12,          // 持续治疗
    Immunity = 13,      // 免疫
    Stun = 14,          // 眩晕
    Silence = 15,       // 沉默
    Slow = 16,          // 减速
    Root = 17,          // 定身
    Fear = 18,          // 恐惧
    Charm = 19          // 魅惑
}

/// <summary>
/// Buff修改方式
/// </summary>
public enum BuffModifierType
{
    Add = 0,        // 加法
    Multiply = 1,   // 乘法
    Percent = 2     // 百分比
}