//------------------------------------------------------------
// 技能效果工厂 - 创建和管理技能效果
//------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 技能效果类型
/// </summary>
public enum SkillEffectType
{
    AreaDamage,     // 范围伤害
    LineDamage,     // 直线伤害
    Summon,         // 召唤
    Heal,           // 治疗
    Teleport,       // 位移
    Buff,           // 增益
    Debuff,         // 减益
    Projectile,     // 投射物
    Chain,          // 链式
    Aura            // 光环
}

/// <summary>
/// 技能效果配置
/// </summary>
[Serializable]
public class SkillEffectConfig
{
    public SkillEffectType EffectType;
    public string EffectPrefabName;
    public float Duration;
    public float Damage;
    public float Range;
    public int MaxTargets;
    public LayerMask TargetLayerMask;
    public string[] AdditionalParams;
}

/// <summary>
/// 技能效果工厂
/// </summary>
public class SkillEffectFactory : MonoBehaviour
{
    [SerializeField] private GameObject[] m_EffectPrefabs;
    [SerializeField] private SkillEffectConfig[] m_EffectConfigs;

    private Dictionary<SkillEffectType, GameObject> m_EffectPrefabMap;
    private Dictionary<int, SkillEffectConfig> m_EffectConfigMap;
    private List<SkillEffectBase> m_ActiveEffects;

    private void Awake()
    {
        InitializeEffectMaps();
        m_ActiveEffects = new List<SkillEffectBase>();
    }

    /// <summary>
    /// 初始化效果映射
    /// </summary>
    private void InitializeEffectMaps()
    {
        m_EffectPrefabMap = new Dictionary<SkillEffectType, GameObject>();
        m_EffectConfigMap = new Dictionary<int, SkillEffectConfig>();

        // 初始化预制体映射
        if (m_EffectPrefabs != null)
        {
            foreach (var prefab in m_EffectPrefabs)
            {
                var effect = prefab.GetComponent<SkillEffectBase>();
                if (effect != null)
                {
                    // 根据效果类型映射预制体
                    if (effect is AreaDamageEffect)
                        m_EffectPrefabMap[SkillEffectType.AreaDamage] = prefab;
                    else if (effect is LineDamageEffect)
                        m_EffectPrefabMap[SkillEffectType.LineDamage] = prefab;
                    else if (effect is SummonEffect)
                        m_EffectPrefabMap[SkillEffectType.Summon] = prefab;
                    else if (effect is HealEffect)
                        m_EffectPrefabMap[SkillEffectType.Heal] = prefab;
                    else if (effect is TeleportEffect)
                        m_EffectPrefabMap[SkillEffectType.Teleport] = prefab;
                }
            }
        }

        // 初始化配置映射
        if (m_EffectConfigs != null)
        {
            for (int i = 0; i < m_EffectConfigs.Length; i++)
            {
                m_EffectConfigMap[i] = m_EffectConfigs[i];
            }
        }
    }

    /// <summary>
    /// 创建技能效果
    /// </summary>
    public async UniTask<SkillEffectBase> CreateEffect(int skillId, SkillEffectType effectType, int casterId, Vector3 targetPosition, int targetId = 0)
    {
        if (!m_EffectPrefabMap.ContainsKey(effectType))
        {
            Debug.LogError($"Effect type {effectType} not found in prefab map");
            return null;
        }

        GameObject prefab = m_EffectPrefabMap[effectType];
        GameObject effectObj = Instantiate(prefab);
        SkillEffectBase effect = effectObj.GetComponent<SkillEffectBase>();

        if (effect == null)
        {
            Debug.LogError($"Effect prefab {prefab.name} does not have SkillEffectBase component");
            Destroy(effectObj);
            return null;
        }

        // 应用配置
        ApplyEffectConfig(effect, skillId);

        // 初始化效果
        effect.Initialize(skillId, casterId, targetPosition, targetId);

        // 添加到活跃效果列表
        m_ActiveEffects.Add(effect);

        // 执行效果
        await effect.Execute();

        // 从活跃效果列表移除
        m_ActiveEffects.Remove(effect);

        // 销毁效果对象
        Destroy(effectObj);

        return effect;
    }

    /// <summary>
    /// 应用效果配置
    /// </summary>
    private void ApplyEffectConfig(SkillEffectBase effect, int skillId)
    {
        // 根据技能ID获取配置
        var config = GetEffectConfig(skillId);
        if (config == null) return;

        // 应用基础配置
        effect.m_Duration = config.Duration;
        effect.m_Damage = config.Damage;
        effect.m_Range = config.Range;
        effect.m_MaxTargets = config.MaxTargets;
        effect.m_TargetLayerMask = config.TargetLayerMask;

        // 应用特定效果配置
        if (effect is AreaDamageEffect areaEffect)
        {
            ApplyAreaDamageConfig(areaEffect, config);
        }
        else if (effect is LineDamageEffect lineEffect)
        {
            ApplyLineDamageConfig(lineEffect, config);
        }
        else if (effect is SummonEffect summonEffect)
        {
            ApplySummonConfig(summonEffect, config);
        }
        else if (effect is HealEffect healEffect)
        {
            ApplyHealConfig(healEffect, config);
        }
        else if (effect is TeleportEffect teleportEffect)
        {
            ApplyTeleportConfig(teleportEffect, config);
        }
    }

    /// <summary>
    /// 获取效果配置
    /// </summary>
    private SkillEffectConfig GetEffectConfig(int skillId)
    {
        // 这里可以根据技能ID返回对应的配置
        // 暂时返回默认配置
        if (m_EffectConfigMap.ContainsKey(skillId))
        {
            return m_EffectConfigMap[skillId];
        }
        return null;
    }

    /// <summary>
    /// 应用范围伤害配置
    /// </summary>
    private void ApplyAreaDamageConfig(AreaDamageEffect effect, SkillEffectConfig config)
    {
        if (config.AdditionalParams != null && config.AdditionalParams.Length > 0)
        {
            if (config.AdditionalParams.Length > 0)
            {
                effect.m_EffectDuration = float.Parse(config.AdditionalParams[0]);
            }
        }
    }

    /// <summary>
    /// 应用直线伤害配置
    /// </summary>
    private void ApplyLineDamageConfig(LineDamageEffect effect, SkillEffectConfig config)
    {
        if (config.AdditionalParams != null && config.AdditionalParams.Length > 0)
        {
            if (config.AdditionalParams.Length > 0)
            {
                effect.m_LineWidth = float.Parse(config.AdditionalParams[0]);
            }
        }
    }

    /// <summary>
    /// 应用召唤配置
    /// </summary>
    private void ApplySummonConfig(SummonEffect effect, SkillEffectConfig config)
    {
        if (config.AdditionalParams != null && config.AdditionalParams.Length > 0)
        {
            if (config.AdditionalParams.Length > 0)
            {
                effect.m_SummonPrefabName = config.AdditionalParams[0];
            }
            if (config.AdditionalParams.Length > 1)
            {
                effect.m_SummonCount = int.Parse(config.AdditionalParams[1]);
            }
            if (config.AdditionalParams.Length > 2)
            {
                effect.m_SummonRadius = float.Parse(config.AdditionalParams[2]);
            }
            if (config.AdditionalParams.Length > 3)
            {
                effect.m_SummonDuration = float.Parse(config.AdditionalParams[3]);
            }
        }
    }

    /// <summary>
    /// 应用治疗配置
    /// </summary>
    private void ApplyHealConfig(HealEffect effect, SkillEffectConfig config)
    {
        if (config.AdditionalParams != null && config.AdditionalParams.Length > 0)
        {
            if (config.AdditionalParams.Length > 0)
            {
                effect.m_HealSelf = bool.Parse(config.AdditionalParams[0]);
            }
            if (config.AdditionalParams.Length > 1)
            {
                effect.m_HealAllies = bool.Parse(config.AdditionalParams[1]);
            }
        }
    }

    /// <summary>
    /// 应用位移配置
    /// </summary>
    private void ApplyTeleportConfig(TeleportEffect effect, SkillEffectConfig config)
    {
        if (config.AdditionalParams != null && config.AdditionalParams.Length > 0)
        {
            if (config.AdditionalParams.Length > 0)
            {
                effect.m_TeleportDelay = float.Parse(config.AdditionalParams[0]);
            }
        }
    }

    /// <summary>
    /// 停止所有效果
    /// </summary>
    public void StopAllEffects()
    {
        foreach (var effect in m_ActiveEffects)
        {
            if (effect != null)
            {
                effect.Stop();
            }
        }
        m_ActiveEffects.Clear();
    }

    /// <summary>
    /// 停止指定技能的效果
    /// </summary>
    public void StopSkillEffects(int skillId)
    {
        var effectsToRemove = new List<SkillEffectBase>();
        foreach (var effect in m_ActiveEffects)
        {
            if (effect != null && effect.m_SkillId == skillId)
            {
                effect.Stop();
                effectsToRemove.Add(effect);
            }
        }

        foreach (var effect in effectsToRemove)
        {
            m_ActiveEffects.Remove(effect);
        }
    }

    /// <summary>
    /// 获取活跃效果数量
    /// </summary>
    public int GetActiveEffectCount()
    {
        return m_ActiveEffects.Count;
    }

    /// <summary>
    /// 获取指定技能的效果数量
    /// </summary>
    public int GetSkillEffectCount(int skillId)
    {
        int count = 0;
        foreach (var effect in m_ActiveEffects)
        {
            if (effect != null && effect.m_SkillId == skillId)
            {
                count++;
            }
        }
        return count;
    }
}