//------------------------------------------------------------
// 角色属性系统 - 管理角色的所有属性状态
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using System;

/// <summary>
/// 角色属性系统
/// </summary>
public class CharacterAttributes : MonoBehaviour
{
    [Header("基础属性")]
    [SerializeField] private int m_Hp = 100;
    [SerializeField] private int m_MaxHp = 100;
    [SerializeField] private int m_Mana = 100;
    [SerializeField] private int m_MaxMana = 100;
    [SerializeField] private int m_Attack = 50;
    [SerializeField] private int m_Defense = 30;
    [SerializeField] private float m_MoveSpeed = 5f;
    [SerializeField] private float m_AttackSpeed = 1f;

    [Header("状态属性")]
    [SerializeField] private bool m_CanCastSkill = true;
    [SerializeField] private bool m_CanMove = true;
    [SerializeField] private bool m_CanAttack = true;
    [SerializeField] private bool m_CanUseItem = true;
    [SerializeField] private bool m_IsImmune = false;
    [SerializeField] private bool m_IsInvulnerable = false;

    [Header("属性修正")]
    [SerializeField] private float m_AttackSpeedModifier = 1f;
    [SerializeField] private float m_MoveSpeedModifier = 1f;
    [SerializeField] private float m_DamageModifier = 1f;
    [SerializeField] private float m_DefenseModifier = 1f;
    [SerializeField] private float m_CriticalRate = 0.1f;
    [SerializeField] private float m_CriticalDamage = 1.5f;
    [SerializeField] private float m_DodgeRate = 0.05f;
    [SerializeField] private float m_BlockRate = 0.1f;

    // 属性变更事件
    public event Action<string, object, object> OnAttributeChanged;

    #region 基础属性
    public int Hp
    {
        get => m_Hp;
        set
        {
            int oldValue = m_Hp;
            m_Hp = Mathf.Clamp(value, 0, m_MaxHp);
            if (oldValue != m_Hp)
            {
                OnAttributeChanged?.Invoke("Hp", oldValue, m_Hp);
            }
        }
    }

    public int MaxHp
    {
        get => m_MaxHp;
        set
        {
            int oldValue = m_MaxHp;
            m_MaxHp = Mathf.Max(1, value);
            if (oldValue != m_MaxHp)
            {
                OnAttributeChanged?.Invoke("MaxHp", oldValue, m_MaxHp);
            }
        }
    }

    public int Mana
    {
        get => m_Mana;
        set
        {
            int oldValue = m_Mana;
            m_Mana = Mathf.Clamp(value, 0, m_MaxMana);
            if (oldValue != m_Mana)
            {
                OnAttributeChanged?.Invoke("Mana", oldValue, m_Mana);
            }
        }
    }

    public int MaxMana
    {
        get => m_MaxMana;
        set
        {
            int oldValue = m_MaxMana;
            m_MaxMana = Mathf.Max(1, value);
            if (oldValue != m_MaxMana)
            {
                OnAttributeChanged?.Invoke("MaxMana", oldValue, m_MaxMana);
            }
        }
    }

    public int Attack
    {
        get => m_Attack;
        set
        {
            int oldValue = m_Attack;
            m_Attack = Mathf.Max(0, value);
            if (oldValue != m_Attack)
            {
                OnAttributeChanged?.Invoke("Attack", oldValue, m_Attack);
            }
        }
    }

    public int Defense
    {
        get => m_Defense;
        set
        {
            int oldValue = m_Defense;
            m_Defense = Mathf.Max(0, value);
            if (oldValue != m_Defense)
            {
                OnAttributeChanged?.Invoke("Defense", oldValue, m_Defense);
            }
        }
    }

    public float MoveSpeed
    {
        get => m_MoveSpeed;
        set
        {
            float oldValue = m_MoveSpeed;
            m_MoveSpeed = Mathf.Max(0, value);
            if (oldValue != m_MoveSpeed)
            {
                OnAttributeChanged?.Invoke("MoveSpeed", oldValue, m_MoveSpeed);
            }
        }
    }

    public float AttackSpeed
    {
        get => m_AttackSpeed;
        set
        {
            float oldValue = m_AttackSpeed;
            m_AttackSpeed = Mathf.Max(0, value);
            if (oldValue != m_AttackSpeed)
            {
                OnAttributeChanged?.Invoke("AttackSpeed", oldValue, m_AttackSpeed);
            }
        }
    }
    #endregion

    #region 状态属性
    public bool CanCastSkill
    {
        get => m_CanCastSkill;
        set
        {
            bool oldValue = m_CanCastSkill;
            m_CanCastSkill = value;
            if (oldValue != m_CanCastSkill)
            {
                OnAttributeChanged?.Invoke("CanCastSkill", oldValue, m_CanCastSkill);
            }
        }
    }

    public bool CanMove
    {
        get => m_CanMove;
        set
        {
            bool oldValue = m_CanMove;
            m_CanMove = value;
            if (oldValue != m_CanMove)
            {
                OnAttributeChanged?.Invoke("CanMove", oldValue, m_CanMove);
            }
        }
    }

    public bool CanAttack
    {
        get => m_CanAttack;
        set
        {
            bool oldValue = m_CanAttack;
            m_CanAttack = value;
            if (oldValue != m_CanAttack)
            {
                OnAttributeChanged?.Invoke("CanAttack", oldValue, m_CanAttack);
            }
        }
    }

    public bool CanUseItem
    {
        get => m_CanUseItem;
        set
        {
            bool oldValue = m_CanUseItem;
            m_CanUseItem = value;
            if (oldValue != m_CanUseItem)
            {
                OnAttributeChanged?.Invoke("CanUseItem", oldValue, m_CanUseItem);
            }
        }
    }

    public bool IsImmune
    {
        get => m_IsImmune;
        set
        {
            bool oldValue = m_IsImmune;
            m_IsImmune = value;
            if (oldValue != m_IsImmune)
            {
                OnAttributeChanged?.Invoke("IsImmune", oldValue, m_IsImmune);
            }
        }
    }

    public bool IsInvulnerable
    {
        get => m_IsInvulnerable;
        set
        {
            bool oldValue = m_IsInvulnerable;
            m_IsInvulnerable = value;
            if (oldValue != m_IsInvulnerable)
            {
                OnAttributeChanged?.Invoke("IsInvulnerable", oldValue, m_IsInvulnerable);
            }
        }
    }
    #endregion

    #region 属性修正
    public float AttackSpeedModifier
    {
        get => m_AttackSpeedModifier;
        set
        {
            float oldValue = m_AttackSpeedModifier;
            m_AttackSpeedModifier = Mathf.Max(0, value);
            if (oldValue != m_AttackSpeedModifier)
            {
                OnAttributeChanged?.Invoke("AttackSpeedModifier", oldValue, m_AttackSpeedModifier);
            }
        }
    }

    public float MoveSpeedModifier
    {
        get => m_MoveSpeedModifier;
        set
        {
            float oldValue = m_MoveSpeedModifier;
            m_MoveSpeedModifier = Mathf.Max(0, value);
            if (oldValue != m_MoveSpeedModifier)
            {
                OnAttributeChanged?.Invoke("MoveSpeedModifier", oldValue, m_MoveSpeedModifier);
            }
        }
    }

    public float DamageModifier
    {
        get => m_DamageModifier;
        set
        {
            float oldValue = m_DamageModifier;
            m_DamageModifier = Mathf.Max(0, value);
            if (oldValue != m_DamageModifier)
            {
                OnAttributeChanged?.Invoke("DamageModifier", oldValue, m_DamageModifier);
            }
        }
    }

    public float DefenseModifier
    {
        get => m_DefenseModifier;
        set
        {
            float oldValue = m_DefenseModifier;
            m_DefenseModifier = Mathf.Max(0, value);
            if (oldValue != m_DefenseModifier)
            {
                OnAttributeChanged?.Invoke("DefenseModifier", oldValue, m_DefenseModifier);
            }
        }
    }

    public float CriticalRate
    {
        get => m_CriticalRate;
        set
        {
            float oldValue = m_CriticalRate;
            m_CriticalRate = Mathf.Clamp(value, 0, 1);
            if (oldValue != m_CriticalRate)
            {
                OnAttributeChanged?.Invoke("CriticalRate", oldValue, m_CriticalRate);
            }
        }
    }

    public float CriticalDamage
    {
        get => m_CriticalDamage;
        set
        {
            float oldValue = m_CriticalDamage;
            m_CriticalDamage = Mathf.Max(1, value);
            if (oldValue != m_CriticalDamage)
            {
                OnAttributeChanged?.Invoke("CriticalDamage", oldValue, m_CriticalDamage);
            }
        }
    }

    public float DodgeRate
    {
        get => m_DodgeRate;
        set
        {
            float oldValue = m_DodgeRate;
            m_DodgeRate = Mathf.Clamp(value, 0, 1);
            if (oldValue != m_DodgeRate)
            {
                OnAttributeChanged?.Invoke("DodgeRate", oldValue, m_DodgeRate);
            }
        }
    }

    public float BlockRate
    {
        get => m_BlockRate;
        set
        {
            float oldValue = m_BlockRate;
            m_BlockRate = Mathf.Clamp(value, 0, 1);
            if (oldValue != m_BlockRate)
            {
                OnAttributeChanged?.Invoke("BlockRate", oldValue, m_BlockRate);
            }
        }
    }
    #endregion

    #region 计算属性
    /// <summary>
    /// 获取实际移动速度
    /// </summary>
    public float GetActualMoveSpeed()
    {
        return m_MoveSpeed * m_MoveSpeedModifier;
    }

    /// <summary>
    /// 获取实际攻击速度
    /// </summary>
    public float GetActualAttackSpeed()
    {
        return m_AttackSpeed * m_AttackSpeedModifier;
    }

    /// <summary>
    /// 获取实际攻击力
    /// </summary>
    public int GetActualAttack()
    {
        return Mathf.RoundToInt(m_Attack * m_DamageModifier);
    }

    /// <summary>
    /// 获取实际防御力
    /// </summary>
    public int GetActualDefense()
    {
        return Mathf.RoundToInt(m_Defense * m_DefenseModifier);
    }

    /// <summary>
    /// 获取生命值百分比
    /// </summary>
    public float GetHpPercentage()
    {
        return (float)m_Hp / m_MaxHp;
    }

    /// <summary>
    /// 获取魔法值百分比
    /// </summary>
    public float GetManaPercentage()
    {
        return (float)m_Mana / m_MaxMana;
    }
    #endregion

    #region 状态检查
    /// <summary>
    /// 检查是否死亡
    /// </summary>
    public bool IsDead()
    {
        return m_Hp <= 0;
    }

    /// <summary>
    /// 检查是否满血
    /// </summary>
    public bool IsFullHp()
    {
        return m_Hp >= m_MaxHp;
    }

    /// <summary>
    /// 检查是否满魔
    /// </summary>
    public bool IsFullMana()
    {
        return m_Mana >= m_MaxMana;
    }

    /// <summary>
    /// 检查是否能够执行指定动作
    /// </summary>
    public bool CanPerformAction(string action)
    {
        switch (action.ToLower())
        {
            case "castskill":
            case "skill":
                return m_CanCastSkill && !IsDead();
            case "move":
            case "movement":
                return m_CanMove && !IsDead();
            case "attack":
                return m_CanAttack && !IsDead();
            case "useitem":
            case "item":
                return m_CanUseItem && !IsDead();
            default:
                return false;
        }
    }
    #endregion

    #region 属性修改
    /// <summary>
    /// 恢复生命值
    /// </summary>
    public int Heal(int amount)
    {
        int oldHp = m_Hp;
        Hp += amount;
        return m_Hp - oldHp;
    }

    /// <summary>
    /// 恢复魔法值
    /// </summary>
    public int RestoreMana(int amount)
    {
        int oldMana = m_Mana;
        Mana += amount;
        return m_Mana - oldMana;
    }

    /// <summary>
    /// 造成伤害
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (m_IsInvulnerable) return 0;
        
        int oldHp = m_Hp;
        Hp -= amount;
        return oldHp - m_Hp;
    }

    /// <summary>
    /// 消耗魔法值
    /// </summary>
    public int ConsumeMana(int amount)
    {
        int oldMana = m_Mana;
        Mana -= amount;
        return oldMana - m_Mana;
    }
    #endregion

    private void Start()
    {
        // 初始化属性
        Hp = m_MaxHp;
        Mana = m_MaxMana;
    }
}