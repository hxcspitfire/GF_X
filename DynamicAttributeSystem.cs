//------------------------------------------------------------
// 动态属性系统 - 支持动态添加和配置属性
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// 属性类型
/// </summary>
public enum AttributeType
{
    Boolean,    // 布尔值
    Integer,    // 整数
    Float,      // 浮点数
    String      // 字符串
}

/// <summary>
/// 属性值
/// </summary>
[Serializable]
public class AttributeValue
{
    public AttributeType Type { get; set; }
    public object Value { get; set; }
    public object DefaultValue { get; set; }

    public AttributeValue()
    {
        Type = AttributeType.Boolean;
        Value = false;
        DefaultValue = false;
    }

    public AttributeValue(AttributeType type, object value, object defaultValue = null)
    {
        Type = type;
        Value = value;
        DefaultValue = defaultValue ?? value;
    }

    /// <summary>
    /// 获取布尔值
    /// </summary>
    public bool GetBool()
    {
        if (Type == AttributeType.Boolean)
            return (bool)Value;
        return false;
    }

    /// <summary>
    /// 设置布尔值
    /// </summary>
    public void SetBool(bool value)
    {
        if (Type == AttributeType.Boolean)
            Value = value;
    }

    /// <summary>
    /// 获取整数值
    /// </summary>
    public int GetInt()
    {
        if (Type == AttributeType.Integer)
            return (int)Value;
        return 0;
    }

    /// <summary>
    /// 设置整数值
    /// </summary>
    public void SetInt(int value)
    {
        if (Type == AttributeType.Integer)
            Value = value;
    }

    /// <summary>
    /// 获取浮点值
    /// </summary>
    public float GetFloat()
    {
        if (Type == AttributeType.Float)
            return (float)Value;
        return 0f;
    }

    /// <summary>
    /// 设置浮点值
    /// </summary>
    public void SetFloat(float value)
    {
        if (Type == AttributeType.Float)
            Value = value;
    }

    /// <summary>
    /// 获取字符串值
    /// </summary>
    public string GetString()
    {
        if (Type == AttributeType.String)
            return (string)Value;
        return string.Empty;
    }

    /// <summary>
    /// 设置字符串值
    /// </summary>
    public void SetString(string value)
    {
        if (Type == AttributeType.String)
            Value = value ?? string.Empty;
    }

    /// <summary>
    /// 重置为默认值
    /// </summary>
    public void ResetToDefault()
    {
        Value = DefaultValue;
    }
}

/// <summary>
/// 属性定义
/// </summary>
[Serializable]
public class AttributeDefinition
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public AttributeType Type { get; set; }
    public object DefaultValue { get; set; }
    public object MinValue { get; set; }
    public object MaxValue { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsVisible { get; set; }

    public AttributeDefinition()
    {
        Name = string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
        Type = AttributeType.Boolean;
        DefaultValue = false;
        MinValue = null;
        MaxValue = null;
        IsReadOnly = false;
        IsVisible = true;
    }
}

/// <summary>
/// 动态属性系统
/// </summary>
public class DynamicAttributeSystem : MonoBehaviour
{
    [Header("属性配置")]
    [SerializeField] private TextAsset m_AttributeConfigFile;
    [SerializeField] private bool m_AutoLoadConfig = true;

    private Dictionary<string, AttributeValue> m_Attributes = new Dictionary<string, AttributeValue>();
    private Dictionary<string, AttributeDefinition> m_AttributeDefinitions = new Dictionary<string, AttributeDefinition>();
    
    // 属性变更事件
    public event Action<string, object, object> OnAttributeChanged;
    public event Action<string> OnAttributeAdded;
    public event Action<string> OnAttributeRemoved;

    private void Start()
    {
        if (m_AutoLoadConfig && m_AttributeConfigFile != null)
        {
            LoadAttributeConfig();
        }
        
        // 初始化默认属性
        InitializeDefaultAttributes();
    }

    /// <summary>
    /// 加载属性配置
    /// </summary>
    public void LoadAttributeConfig()
    {
        if (m_AttributeConfigFile == null) return;

        try
        {
            var config = JsonConvert.DeserializeObject<AttributeDefinition[]>(m_AttributeConfigFile.text);
            if (config != null)
            {
                foreach (var def in config)
                {
                    RegisterAttributeDefinition(def);
                }
                Debug.Log($"加载了 {config.Length} 个属性定义");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载属性配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 注册属性定义
    /// </summary>
    public void RegisterAttributeDefinition(AttributeDefinition definition)
    {
        if (string.IsNullOrEmpty(definition.Name)) return;

        m_AttributeDefinitions[definition.Name] = definition;
        
        // 如果属性不存在，创建它
        if (!m_Attributes.ContainsKey(definition.Name))
        {
            var attributeValue = new AttributeValue(definition.Type, definition.DefaultValue, definition.DefaultValue);
            m_Attributes[definition.Name] = attributeValue;
            OnAttributeAdded?.Invoke(definition.Name);
        }
    }

    /// <summary>
    /// 初始化默认属性
    /// </summary>
    private void InitializeDefaultAttributes()
    {
        // 基础属性
        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "Hp",
            DisplayName = "生命值",
            Description = "当前生命值",
            Type = AttributeType.Integer,
            DefaultValue = 100,
            MinValue = 0,
            MaxValue = 1000
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "MaxHp",
            DisplayName = "最大生命值",
            Description = "最大生命值",
            Type = AttributeType.Integer,
            DefaultValue = 100,
            MinValue = 1,
            MaxValue = 1000
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "Mana",
            DisplayName = "魔法值",
            Description = "当前魔法值",
            Type = AttributeType.Integer,
            DefaultValue = 100,
            MinValue = 0,
            MaxValue = 1000
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "MaxMana",
            DisplayName = "最大魔法值",
            Description = "最大魔法值",
            Type = AttributeType.Integer,
            DefaultValue = 100,
            MinValue = 1,
            MaxValue = 1000
        });

        // 状态属性
        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "CanCastSkill",
            DisplayName = "能否释放技能",
            Description = "是否可以释放技能",
            Type = AttributeType.Boolean,
            DefaultValue = true
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "CanMove",
            DisplayName = "能否移动",
            Description = "是否可以移动",
            Type = AttributeType.Boolean,
            DefaultValue = true
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "CanAttack",
            DisplayName = "能否攻击",
            Description = "是否可以攻击",
            Type = AttributeType.Boolean,
            DefaultValue = true
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "CanUseItem",
            DisplayName = "能否使用道具",
            Description = "是否可以使用道具",
            Type = AttributeType.Boolean,
            DefaultValue = true
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "IsImmune",
            DisplayName = "是否免疫",
            Description = "是否免疫所有伤害",
            Type = AttributeType.Boolean,
            DefaultValue = false
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "IsInvulnerable",
            DisplayName = "是否无敌",
            Description = "是否无敌",
            Type = AttributeType.Boolean,
            DefaultValue = false
        });

        // 修正属性
        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "AttackSpeedModifier",
            DisplayName = "攻击速度修正",
            Description = "攻击速度修正倍数",
            Type = AttributeType.Float,
            DefaultValue = 1f,
            MinValue = 0f,
            MaxValue = 10f
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "MoveSpeedModifier",
            DisplayName = "移动速度修正",
            Description = "移动速度修正倍数",
            Type = AttributeType.Float,
            DefaultValue = 1f,
            MinValue = 0f,
            MaxValue = 10f
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "DamageModifier",
            DisplayName = "伤害修正",
            Description = "伤害修正倍数",
            Type = AttributeType.Float,
            DefaultValue = 1f,
            MinValue = 0f,
            MaxValue = 10f
        });

        RegisterAttributeDefinition(new AttributeDefinition
        {
            Name = "DefenseModifier",
            DisplayName = "防御修正",
            Description = "防御修正倍数",
            Type = AttributeType.Float,
            DefaultValue = 1f,
            MinValue = 0f,
            MaxValue = 10f
        });
    }

    /// <summary>
    /// 设置属性值
    /// </summary>
    public bool SetAttribute(string name, object value)
    {
        if (!m_Attributes.ContainsKey(name)) return false;

        var attribute = m_Attributes[name];
        var definition = m_AttributeDefinitions.ContainsKey(name) ? m_AttributeDefinitions[name] : null;

        // 检查是否只读
        if (definition != null && definition.IsReadOnly) return false;

        // 检查值范围
        if (definition != null)
        {
            if (definition.MinValue != null && CompareValues(value, definition.MinValue) < 0) return false;
            if (definition.MaxValue != null && CompareValues(value, definition.MaxValue) > 0) return false;
        }

        object oldValue = attribute.Value;
        attribute.Value = value;

        OnAttributeChanged?.Invoke(name, oldValue, value);
        return true;
    }

    /// <summary>
    /// 获取属性值
    /// </summary>
    public object GetAttribute(string name)
    {
        if (!m_Attributes.ContainsKey(name)) return null;
        return m_Attributes[name].Value;
    }

    /// <summary>
    /// 获取布尔属性
    /// </summary>
    public bool GetBoolAttribute(string name)
    {
        if (!m_Attributes.ContainsKey(name)) return false;
        return m_Attributes[name].GetBool();
    }

    /// <summary>
    /// 设置布尔属性
    /// </summary>
    public bool SetBoolAttribute(string name, bool value)
    {
        return SetAttribute(name, value);
    }

    /// <summary>
    /// 获取整数属性
    /// </summary>
    public int GetIntAttribute(string name)
    {
        if (!m_Attributes.ContainsKey(name)) return 0;
        return m_Attributes[name].GetInt();
    }

    /// <summary>
    /// 设置整数属性
    /// </summary>
    public bool SetIntAttribute(string name, int value)
    {
        return SetAttribute(name, value);
    }

    /// <summary>
    /// 获取浮点属性
    /// </summary>
    public float GetFloatAttribute(string name)
    {
        if (!m_Attributes.ContainsKey(name)) return 0f;
        return m_Attributes[name].GetFloat();
    }

    /// <summary>
    /// 设置浮点属性
    /// </summary>
    public bool SetFloatAttribute(string name, float value)
    {
        return SetAttribute(name, value);
    }

    /// <summary>
    /// 获取字符串属性
    /// </summary>
    public string GetStringAttribute(string name)
    {
        if (!m_Attributes.ContainsKey(name)) return string.Empty;
        return m_Attributes[name].GetString();
    }

    /// <summary>
    /// 设置字符串属性
    /// </summary>
    public bool SetStringAttribute(string name, string value)
    {
        return SetAttribute(name, value);
    }

    /// <summary>
    /// 检查属性是否存在
    /// </summary>
    public bool HasAttribute(string name)
    {
        return m_Attributes.ContainsKey(name);
    }

    /// <summary>
    /// 添加新属性
    /// </summary>
    public bool AddAttribute(string name, AttributeType type, object defaultValue)
    {
        if (m_Attributes.ContainsKey(name)) return false;

        var attributeValue = new AttributeValue(type, defaultValue, defaultValue);
        m_Attributes[name] = attributeValue;
        OnAttributeAdded?.Invoke(name);
        return true;
    }

    /// <summary>
    /// 移除属性
    /// </summary>
    public bool RemoveAttribute(string name)
    {
        if (!m_Attributes.ContainsKey(name)) return false;

        m_Attributes.Remove(name);
        OnAttributeRemoved?.Invoke(name);
        return true;
    }

    /// <summary>
    /// 重置属性为默认值
    /// </summary>
    public void ResetAttribute(string name)
    {
        if (!m_Attributes.ContainsKey(name)) return;

        var attribute = m_Attributes[name];
        object oldValue = attribute.Value;
        attribute.ResetToDefault();
        OnAttributeChanged?.Invoke(name, oldValue, attribute.Value);
    }

    /// <summary>
    /// 重置所有属性为默认值
    /// </summary>
    public void ResetAllAttributes()
    {
        foreach (var kvp in m_Attributes)
        {
            ResetAttribute(kvp.Key);
        }
    }

    /// <summary>
    /// 获取所有属性名称
    /// </summary>
    public string[] GetAllAttributeNames()
    {
        var names = new string[m_Attributes.Count];
        m_Attributes.Keys.CopyTo(names, 0);
        return names;
    }

    /// <summary>
    /// 获取属性定义
    /// </summary>
    public AttributeDefinition GetAttributeDefinition(string name)
    {
        return m_AttributeDefinitions.ContainsKey(name) ? m_AttributeDefinitions[name] : null;
    }

    /// <summary>
    /// 检查是否可以执行指定动作
    /// </summary>
    public bool CanPerformAction(string action)
    {
        switch (action.ToLower())
        {
            case "castskill":
            case "skill":
                return GetBoolAttribute("CanCastSkill") && !IsDead();
            case "move":
            case "movement":
                return GetBoolAttribute("CanMove") && !IsDead();
            case "attack":
                return GetBoolAttribute("CanAttack") && !IsDead();
            case "useitem":
            case "item":
                return GetBoolAttribute("CanUseItem") && !IsDead();
            default:
                return false;
        }
    }

    /// <summary>
    /// 检查是否死亡
    /// </summary>
    public bool IsDead()
    {
        return GetIntAttribute("Hp") <= 0;
    }

    /// <summary>
    /// 比较两个值
    /// </summary>
    private int CompareValues(object a, object b)
    {
        if (a is IComparable comparableA && b is IComparable comparableB)
        {
            return comparableA.CompareTo(comparableB);
        }
        return 0;
    }

    /// <summary>
    /// 获取属性信息（用于调试）
    /// </summary>
    public string GetAttributeInfo()
    {
        var info = new System.Text.StringBuilder();
        info.AppendLine("=== 属性信息 ===");
        
        foreach (var kvp in m_Attributes)
        {
            var definition = m_AttributeDefinitions.ContainsKey(kvp.Key) ? m_AttributeDefinitions[kvp.Key] : null;
            string displayName = definition?.DisplayName ?? kvp.Key;
            info.AppendLine($"{displayName}: {kvp.Value.Value} ({kvp.Value.Type})");
        }
        
        return info.ToString();
    }
}