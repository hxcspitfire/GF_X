//------------------------------------------------------------
// 被动技能管理器 - 管理被动技能的生命周期
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 被动技能管理器
/// </summary>
public class PassiveSkillManager : MonoBehaviour
{
    [Header("被动技能配置")]
    [SerializeField] private int m_TripleAttackSkillId = 101;
    [SerializeField] private int m_LifestealBuffId = 3001;

    private Dictionary<int, SkillEffectBase> m_ActivePassiveSkills = new Dictionary<int, SkillEffectBase>();
    private SkillDataModel m_SkillDataModel;
    private EntityLogic m_Player;

    private void Start()
    {
        m_SkillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();
        
        // 获取玩家实体
        m_Player = GF.Entity.GetEntity<PlayerEntity>(1);
        if (m_Player == null)
        {
            Debug.LogError("玩家实体未找到");
            return;
        }

        // 初始化被动技能
        InitializePassiveSkills();
    }

    /// <summary>
    /// 初始化被动技能
    /// </summary>
    private void InitializePassiveSkills()
    {
        // 检查三重攻击技能是否解锁
        if (m_SkillDataModel.IsSkillUnlocked(m_TripleAttackSkillId))
        {
            ActivateTripleAttackPassive();
        }

        // 订阅技能解锁事件
        GF.Event.Subscribe(SkillDataChangedEventArgs.EventId, OnSkillDataChanged);
    }

    /// <summary>
    /// 激活三重攻击被动技能
    /// </summary>
    private void ActivateTripleAttackPassive()
    {
        if (m_ActivePassiveSkills.ContainsKey(m_TripleAttackSkillId))
        {
            Debug.Log("三重攻击被动技能已激活");
            return;
        }

        // 创建被动技能组件
        var tripleAttackPassive = m_Player.gameObject.AddComponent<TripleAttackPassive>();
        tripleAttackPassive.InitializePassive(m_Player);
        
        // 激活技能
        tripleAttackPassive.Execute();
        
        // 存储技能引用
        m_ActivePassiveSkills[m_TripleAttackSkillId] = tripleAttackPassive;
        
        Debug.Log("三重攻击被动技能激活成功");
    }

    /// <summary>
    /// 技能数据变更事件处理
    /// </summary>
    private void OnSkillDataChanged(object sender, GameEventArgs e)
    {
        var args = e as SkillDataChangedEventArgs;
        if (args == null) return;

        // 检查是否为技能解锁事件
        if (args.DataType == SkillDataType.SkillUnlocked && args.SkillId == m_TripleAttackSkillId)
        {
            if (args.NewValue > 0) // 解锁
            {
                ActivateTripleAttackPassive();
            }
            else // 锁定
            {
                DeactivateTripleAttackPassive();
            }
        }
    }

    /// <summary>
    /// 停用三重攻击被动技能
    /// </summary>
    private void DeactivateTripleAttackPassive()
    {
        if (m_ActivePassiveSkills.ContainsKey(m_TripleAttackSkillId))
        {
            var skill = m_ActivePassiveSkills[m_TripleAttackSkillId];
            if (skill != null)
            {
                skill.Stop();
                Destroy(skill);
            }
            m_ActivePassiveSkills.Remove(m_TripleAttackSkillId);
            
            Debug.Log("三重攻击被动技能已停用");
        }
    }

    /// <summary>
    /// 获取被动技能状态
    /// </summary>
    public bool IsPassiveSkillActive(int skillId)
    {
        return m_ActivePassiveSkills.ContainsKey(skillId) && m_ActivePassiveSkills[skillId] != null;
    }

    /// <summary>
    /// 获取三重攻击计数
    /// </summary>
    public int GetTripleAttackCount()
    {
        if (m_ActivePassiveSkills.ContainsKey(m_TripleAttackSkillId))
        {
            var skill = m_ActivePassiveSkills[m_TripleAttackSkillId] as TripleAttackPassive;
            if (skill != null)
            {
                return skill.GetAttackCount();
            }
        }
        return 0;
    }

    /// <summary>
    /// 获取三重攻击触发进度
    /// </summary>
    public float GetTripleAttackProgress()
    {
        if (m_ActivePassiveSkills.ContainsKey(m_TripleAttackSkillId))
        {
            var skill = m_ActivePassiveSkills[m_TripleAttackSkillId] as TripleAttackPassive;
            if (skill != null)
            {
                return skill.GetTriggerProgress();
            }
        }
        return 0f;
    }

    /// <summary>
    /// 重置三重攻击计数
    /// </summary>
    public void ResetTripleAttackCount()
    {
        if (m_ActivePassiveSkills.ContainsKey(m_TripleAttackSkillId))
        {
            var skill = m_ActivePassiveSkills[m_TripleAttackSkillId] as TripleAttackPassive;
            if (skill != null)
            {
                skill.ResetAttackCount();
            }
        }
    }

    private void OnDestroy()
    {
        // 清理所有被动技能
        foreach (var skill in m_ActivePassiveSkills.Values)
        {
            if (skill != null)
            {
                skill.Stop();
                Destroy(skill);
            }
        }
        m_ActivePassiveSkills.Clear();

        // 取消订阅事件
        GF.Event.Unsubscribe(SkillDataChangedEventArgs.EventId, OnSkillDataChanged);
    }
}