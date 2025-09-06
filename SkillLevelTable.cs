//------------------------------------------------------------
// 技能等级表 - 配置技能每个等级的属性
//------------------------------------------------------------
using GameFramework;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName | Obfuz.ObfuzScope.MethodName)]
/// <summary>
/// 技能等级表
/// </summary>
public class SkillLevelTable : DataRowBase
{
    private int m_Id = 0;
    public override int Id
    {
        get { return m_Id; }
    }

    /// <summary>
    /// 技能ID
    /// </summary>
    public int SkillId { get; private set; }

    /// <summary>
    /// 技能等级
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// 冷却时间修正值
    /// </summary>
    public float CooldownModifier { get; private set; }

    /// <summary>
    /// 魔法消耗修正值
    /// </summary>
    public int ManaCostModifier { get; private set; }

    /// <summary>
    /// 伤害修正值
    /// </summary>
    public float DamageModifier { get; private set; }

    /// <summary>
    /// 范围修正值
    /// </summary>
    public float RangeModifier { get; private set; }

    /// <summary>
    /// 持续时间修正值
    /// </summary>
    public float DurationModifier { get; private set; }

    /// <summary>
    /// 升级消耗金币
    /// </summary>
    public int UpgradeCost { get; private set; }

    /// <summary>
    /// 升级消耗经验
    /// </summary>
    public int UpgradeExp { get; private set; }

    /// <summary>
    /// 特殊效果描述
    /// </summary>
    public string SpecialEffect { get; private set; }

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
        SkillId = int.Parse(columnStrings[index++]);
        Level = int.Parse(columnStrings[index++]);
        CooldownModifier = float.Parse(columnStrings[index++]);
        ManaCostModifier = int.Parse(columnStrings[index++]);
        DamageModifier = float.Parse(columnStrings[index++]);
        RangeModifier = float.Parse(columnStrings[index++]);
        DurationModifier = float.Parse(columnStrings[index++]);
        UpgradeCost = int.Parse(columnStrings[index++]);
        UpgradeExp = int.Parse(columnStrings[index++]);
        SpecialEffect = columnStrings[index++];

        return true;
    }

    public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
    {
        using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
        {
            using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
            {
                m_Id = binaryReader.Read7BitEncodedInt32();
                SkillId = binaryReader.Read7BitEncodedInt32();
                Level = binaryReader.Read7BitEncodedInt32();
                CooldownModifier = binaryReader.ReadSingle();
                ManaCostModifier = binaryReader.Read7BitEncodedInt32();
                DamageModifier = binaryReader.ReadSingle();
                RangeModifier = binaryReader.ReadSingle();
                DurationModifier = binaryReader.ReadSingle();
                UpgradeCost = binaryReader.Read7BitEncodedInt32();
                UpgradeExp = binaryReader.Read7BitEncodedInt32();
                SpecialEffect = binaryReader.ReadString();
            }
        }

        return true;
    }
}