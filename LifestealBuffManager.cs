//------------------------------------------------------------
// 吸血Buff管理器 - 管理吸血Buff的特殊逻辑
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using System.Collections.Generic;

/// <summary>
/// 吸血Buff管理器
/// </summary>
public class LifestealBuffManager : MonoBehaviour
{
    private Dictionary<int, LifestealBuffEffect> m_LifestealEffects = new Dictionary<int, LifestealBuffEffect>();
    private BuffManager m_BuffManager;

    private void Start()
    {
        m_BuffManager = GetComponent<BuffManager>();
        if (m_BuffManager == null)
        {
            m_BuffManager = FindObjectOfType<BuffManager>();
        }

        // 订阅Buff事件
        if (m_BuffManager != null)
        {
            GF.Event.Subscribe(BuffAddedEventArgs.EventId, OnBuffAdded);
            GF.Event.Subscribe(BuffRemovedEventArgs.EventId, OnBuffRemoved);
        }
    }

    /// <summary>
    /// Buff添加事件处理
    /// </summary>
    private void OnBuffAdded(object sender, GameEventArgs e)
    {
        var args = e as BuffAddedEventArgs;
        if (args == null) return;

        // 检查是否为吸血Buff
        if (IsLifestealBuff(args.BuffId))
        {
            CreateLifestealEffect(args.BuffId, args.TargetId, args.CasterId);
        }
    }

    /// <summary>
    /// Buff移除事件处理
    /// </summary>
    private void OnBuffRemoved(object sender, GameEventArgs e)
    {
        var args = e as BuffRemovedEventArgs;
        if (args == null) return;

        // 检查是否为吸血Buff
        if (IsLifestealBuff(args.BuffId))
        {
            RemoveLifestealEffect(args.TargetId);
        }
    }

    /// <summary>
    /// 检查是否为吸血Buff
    /// </summary>
    private bool IsLifestealBuff(int buffId)
    {
        // 吸血Buff的ID范围：3000-3099
        return buffId >= 3000 && buffId < 3100;
    }

    /// <summary>
    /// 创建吸血效果
    /// </summary>
    private void CreateLifestealEffect(int buffId, int targetId, int casterId)
    {
        var target = GF.Entity.GetEntity<EntityLogic>(targetId);
        if (target == null) return;

        // 获取Buff实例
        var buffInstance = m_BuffManager.GetBuffByType(targetId, buffId);
        if (buffInstance == null) return;

        // 创建吸血效果组件
        var lifestealEffect = target.gameObject.AddComponent<LifestealBuffEffect>();
        lifestealEffect.Initialize(buffInstance, target);

        // 存储效果引用
        m_LifestealEffects[targetId] = lifestealEffect;

        Debug.Log($"为目标{targetId}创建吸血效果，Buff ID: {buffId}");
    }

    /// <summary>
    /// 移除吸血效果
    /// </summary>
    private void RemoveLifestealEffect(int targetId)
    {
        if (m_LifestealEffects.ContainsKey(targetId))
        {
            var effect = m_LifestealEffects[targetId];
            if (effect != null)
            {
                effect.Stop();
                Destroy(effect);
            }
            m_LifestealEffects.Remove(targetId);

            Debug.Log($"移除目标{targetId}的吸血效果");
        }
    }

    /// <summary>
    /// 获取目标的吸血效果
    /// </summary>
    public LifestealBuffEffect GetLifestealEffect(int targetId)
    {
        if (m_LifestealEffects.ContainsKey(targetId))
        {
            return m_LifestealEffects[targetId];
        }
        return null;
    }

    /// <summary>
    /// 检查目标是否有吸血效果
    /// </summary>
    public bool HasLifestealEffect(int targetId)
    {
        return m_LifestealEffects.ContainsKey(targetId) && m_LifestealEffects[targetId] != null;
    }

    /// <summary>
    /// 获取所有活跃的吸血效果
    /// </summary>
    public List<LifestealBuffEffect> GetAllLifestealEffects()
    {
        var activeEffects = new List<LifestealBuffEffect>();
        foreach (var effect in m_LifestealEffects.Values)
        {
            if (effect != null)
            {
                activeEffects.Add(effect);
            }
        }
        return activeEffects;
    }

    private void OnDestroy()
    {
        // 清理所有吸血效果
        foreach (var effect in m_LifestealEffects.Values)
        {
            if (effect != null)
            {
                effect.Stop();
                Destroy(effect);
            }
        }
        m_LifestealEffects.Clear();

        // 取消订阅事件
        GF.Event.Unsubscribe(BuffAddedEventArgs.EventId, OnBuffAdded);
        GF.Event.Unsubscribe(BuffRemovedEventArgs.EventId, OnBuffRemoved);
    }
}