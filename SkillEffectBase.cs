//------------------------------------------------------------
// 技能效果基类 - 所有技能效果的基类
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 技能效果基类
/// </summary>
public abstract class SkillEffectBase : MonoBehaviour
{
    [SerializeField] protected float m_Duration = 1f;
    [SerializeField] protected float m_Damage = 10f;
    [SerializeField] protected float m_Range = 5f;
    [SerializeField] protected int m_MaxTargets = 10;
    [SerializeField] protected LayerMask m_TargetLayerMask = -1;

    protected int m_SkillId;
    protected int m_CasterId;
    protected Vector3 m_TargetPosition;
    protected int m_TargetId;
    protected bool m_IsActive = false;

    /// <summary>
    /// 初始化效果
    /// </summary>
    public virtual void Initialize(int skillId, int casterId, Vector3 targetPosition, int targetId = 0)
    {
        m_SkillId = skillId;
        m_CasterId = casterId;
        m_TargetPosition = targetPosition;
        m_TargetId = targetId;
        m_IsActive = true;
    }

    /// <summary>
    /// 执行效果
    /// </summary>
    public abstract UniTask<bool> Execute();

    /// <summary>
    /// 停止效果
    /// </summary>
    public virtual void Stop()
    {
        m_IsActive = false;
    }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive => m_IsActive;

    /// <summary>
    /// 获取范围内的目标
    /// </summary>
    protected virtual Collider[] GetTargetsInRange(Vector3 center, float range)
    {
        return Physics.OverlapSphere(center, range, m_TargetLayerMask);
    }

    /// <summary>
    /// 获取最近的敌人
    /// </summary>
    protected virtual Collider GetNearestEnemy(Vector3 center, float range)
    {
        Collider[] targets = GetTargetsInRange(center, range);
        if (targets.Length == 0) return null;

        Collider nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in targets)
        {
            float distance = Vector3.Distance(center, target.transform.position);
            if (distance < nearestDistance)
            {
                nearest = target;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    protected virtual void DealDamage(EntityLogic target, float damage)
    {
        if (target is CombatUnitEntity combatUnit)
        {
            combatUnit.Hp -= (int)damage;
        }
    }

    /// <summary>
    /// 对目标进行治疗
    /// </summary>
    protected virtual void HealTarget(EntityLogic target, float heal)
    {
        if (target is CombatUnitEntity combatUnit)
        {
            combatUnit.Hp += (int)heal;
        }
    }
}

/// <summary>
/// 范围伤害效果
/// </summary>
public class AreaDamageEffect : SkillEffectBase
{
    [SerializeField] private GameObject m_EffectPrefab;
    [SerializeField] private float m_EffectDuration = 2f;

    public override async UniTask<bool> Execute()
    {
        if (!m_IsActive) return false;

        // 播放特效
        if (m_EffectPrefab != null)
        {
            var effectParams = EntityParams.Create(m_TargetPosition);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, m_EffectDuration);
            GF.Entity.ShowEffect(m_EffectPrefab.name, effectParams, m_EffectDuration);
        }

        // 获取范围内的目标
        Collider[] targets = GetTargetsInRange(m_TargetPosition, m_Range);
        
        // 对每个目标造成伤害
        foreach (var target in targets)
        {
            if (m_MaxTargets > 0 && targets.Length > m_MaxTargets)
            {
                break;
            }

            var entity = target.GetComponent<EntityLogic>();
            if (entity != null)
            {
                DealDamage(entity, m_Damage);
            }
        }

        // 等待效果持续时间
        await UniTask.Delay((int)(m_Duration * 1000));

        m_IsActive = false;
        return true;
    }
}

/// <summary>
/// 直线伤害效果
/// </summary>
public class LineDamageEffect : SkillEffectBase
{
    [SerializeField] private GameObject m_EffectPrefab;
    [SerializeField] private float m_LineWidth = 1f;

    public override async UniTask<bool> Execute()
    {
        if (!m_IsActive) return false;

        // 获取施法者位置
        var caster = GF.Entity.GetEntity<EntityLogic>(m_CasterId);
        if (caster == null) return false;

        Vector3 direction = (m_TargetPosition - caster.CachedTransform.position).normalized;
        Vector3 startPos = caster.CachedTransform.position;

        // 播放特效
        if (m_EffectPrefab != null)
        {
            var effectParams = EntityParams.Create(startPos);
            effectParams.Set<VarVector3>(EntityParams.DIRECTION, direction);
            effectParams.Set<VarFloat>(EntityParams.RANGE, m_Range);
            GF.Entity.ShowEffect(m_EffectPrefab.name, effectParams, m_Duration);
        }

        // 沿直线检测目标
        RaycastHit[] hits = Physics.RaycastAll(startPos, direction, m_Range, m_TargetLayerMask);
        
        foreach (var hit in hits)
        {
            var entity = hit.collider.GetComponent<EntityLogic>();
            if (entity != null)
            {
                DealDamage(entity, m_Damage);
            }
        }

        // 等待效果持续时间
        await UniTask.Delay((int)(m_Duration * 1000));

        m_IsActive = false;
        return true;
    }
}

/// <summary>
/// 召唤效果
/// </summary>
public class SummonEffect : SkillEffectBase
{
    [SerializeField] private string m_SummonPrefabName;
    [SerializeField] private int m_SummonCount = 1;
    [SerializeField] private float m_SummonRadius = 2f;
    [SerializeField] private float m_SummonDuration = 30f;

    public override async UniTask<bool> Execute()
    {
        if (!m_IsActive) return false;

        // 在目标位置周围召唤单位
        for (int i = 0; i < m_SummonCount; i++)
        {
            Vector3 summonPos = m_TargetPosition + Random.insideUnitSphere * m_SummonRadius;
            summonPos.y = m_TargetPosition.y;

            var summonParams = EntityParams.Create(summonPos);
            summonParams.Set<VarFloat>(EntityParams.LIFE_TIME, m_SummonDuration);
            summonParams.Set<VarInt32>(EntityParams.CASTER_ID, m_CasterId);
            
            GF.Entity.ShowEntity(m_SummonPrefabName, "Summon", 0, summonParams);
        }

        m_IsActive = false;
        return true;
    }
}

/// <summary>
/// 治疗效果
/// </summary>
public class HealEffect : SkillEffectBase
{
    [SerializeField] private GameObject m_EffectPrefab;
    [SerializeField] private bool m_HealSelf = true;
    [SerializeField] private bool m_HealAllies = false;

    public override async UniTask<bool> Execute()
    {
        if (!m_IsActive) return false;

        // 播放治疗特效
        if (m_EffectPrefab != null)
        {
            var effectParams = EntityParams.Create(m_TargetPosition);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, m_Duration);
            GF.Entity.ShowEffect(m_EffectPrefab.name, effectParams, m_Duration);
        }

        // 治疗目标
        if (m_HealSelf)
        {
            var caster = GF.Entity.GetEntity<EntityLogic>(m_CasterId);
            if (caster != null)
            {
                HealTarget(caster, m_Damage);
            }
        }

        if (m_HealAllies)
        {
            // 治疗范围内的友军
            Collider[] targets = GetTargetsInRange(m_TargetPosition, m_Range);
            foreach (var target in targets)
            {
                var entity = target.GetComponent<EntityLogic>();
                if (entity != null && IsAlly(entity))
                {
                    HealTarget(entity, m_Damage);
                }
            }
        }

        // 等待效果持续时间
        await UniTask.Delay((int)(m_Duration * 1000));

        m_IsActive = false;
        return true;
    }

    /// <summary>
    /// 检查是否为友军
    /// </summary>
    private bool IsAlly(EntityLogic entity)
    {
        // 这里需要根据实际的阵营系统来判断
        // 暂时简单判断为友军
        return true;
    }
}

/// <summary>
/// 位移效果
/// </summary>
public class TeleportEffect : SkillEffectBase
{
    [SerializeField] private GameObject m_EffectPrefab;
    [SerializeField] private float m_TeleportDelay = 0.5f;

    public override async UniTask<bool> Execute()
    {
        if (!m_IsActive) return false;

        var caster = GF.Entity.GetEntity<EntityLogic>(m_CasterId);
        if (caster == null) return false;

        // 播放传送特效
        if (m_EffectPrefab != null)
        {
            var effectParams = EntityParams.Create(caster.CachedTransform.position);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, m_TeleportDelay);
            GF.Entity.ShowEffect(m_EffectPrefab.name, effectParams, m_TeleportDelay);
        }

        // 等待传送延迟
        await UniTask.Delay((int)(m_TeleportDelay * 1000));

        // 执行传送
        caster.CachedTransform.position = m_TargetPosition;

        // 播放到达特效
        if (m_EffectPrefab != null)
        {
            var effectParams = EntityParams.Create(m_TargetPosition);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, m_TeleportDelay);
            GF.Entity.ShowEffect(m_EffectPrefab.name, effectParams, m_TeleportDelay);
        }

        m_IsActive = false;
        return true;
    }
}