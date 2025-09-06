//------------------------------------------------------------
// 三重攻击技能效果 - 连续攻击敌人3次，触发吸血效果
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
// 三重攻击技能效果
/// </summary>
public class TripleAttackEffect : SkillEffectBase
{
    [Header("三重攻击配置")]
    [SerializeField] private float m_AttackInterval = 0.3f; // 攻击间隔
    [SerializeField] private float m_AttackRange = 2f; // 攻击范围
    [SerializeField] private int m_LifestealBuffId = 3001; // 吸血Buff ID
    [SerializeField] private float m_LifestealDuration = 5f; // 吸血持续时间
    [SerializeField] private GameObject m_AttackEffectPrefab; // 攻击特效
    [SerializeField] private string m_AttackSound; // 攻击音效

    private int m_AttackCount = 0;
    private const int MAX_ATTACKS = 3;

    public override async UniTask<bool> Execute()
    {
        if (!m_IsActive) return false;

        var caster = GF.Entity.GetEntity<EntityLogic>(m_CasterId);
        if (caster == null) return false;

        // 获取最近的敌人
        var nearestEnemy = GetNearestEnemy(caster.CachedTransform.position, m_AttackRange);
        if (nearestEnemy == null)
        {
            Debug.Log("三重攻击：没有找到目标敌人");
            return false;
        }

        // 执行3次连续攻击
        for (int i = 0; i < MAX_ATTACKS; i++)
        {
            if (!m_IsActive) break;

            // 执行单次攻击
            await ExecuteSingleAttack(caster, nearestEnemy, i + 1);
            
            // 等待攻击间隔
            if (i < MAX_ATTACKS - 1)
            {
                await UniTask.Delay((int)(m_AttackInterval * 1000));
            }
        }

        // 触发吸血效果
        await TriggerLifestealEffect(caster);

        m_IsActive = false;
        return true;
    }

    /// <summary>
    /// 执行单次攻击
    /// </summary>
    private async UniTask ExecuteSingleAttack(EntityLogic caster, Collider target, int attackNumber)
    {
        // 播放攻击特效
        if (m_AttackEffectPrefab != null)
        {
            var effectParams = EntityParams.Create(target.transform.position);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, 1f);
            GF.Entity.ShowEffect(m_AttackEffectPrefab.name, effectParams, 1f);
        }

        // 播放攻击音效
        if (!string.IsNullOrEmpty(m_AttackSound))
        {
            GF.Sound.PlayEffect(m_AttackSound);
        }

        // 对目标造成伤害
        var targetEntity = target.GetComponent<EntityLogic>();
        if (targetEntity is CombatUnitEntity combatUnit)
        {
            int damage = CalculateDamage(caster, attackNumber);
            combatUnit.Hp -= damage;
            
            Debug.Log($"三重攻击第{attackNumber}次：造成{damage}点伤害");
            
            // 触发攻击事件
            GF.Event.Fire(this, AttackHitEventArgs.Create(caster.Entity.Id, targetEntity.Entity.Id, damage, attackNumber));
        }

        // 播放攻击动画
        await PlayAttackAnimation(caster, attackNumber);

        m_AttackCount++;
    }

    /// <summary>
    /// 计算伤害
    /// </summary>
    private int CalculateDamage(EntityLogic caster, int attackNumber)
    {
        // 基础伤害
        float baseDamage = m_Damage;
        
        // 根据攻击次数调整伤害（可选：每次攻击伤害递增）
        float damageMultiplier = 1f + (attackNumber - 1) * 0.1f; // 每次攻击伤害增加10%
        
        // 应用Buff修正
        float finalDamage = baseDamage * damageMultiplier;
        
        return Mathf.RoundToInt(finalDamage);
    }

    /// <summary>
    /// 播放攻击动画
    /// </summary>
    private async UniTask PlayAttackAnimation(EntityLogic caster, int attackNumber)
    {
        var animator = caster.GetComponent<Animator>();
        if (animator != null)
        {
            // 根据攻击次数播放不同动画
            string animationName = $"TripleAttack_{attackNumber}";
            animator.SetTrigger(animationName);
            
            // 等待动画播放时间
            await UniTask.Delay(200); // 假设每次攻击动画200ms
        }
    }

    /// <summary>
    /// 触发吸血效果
    /// </summary>
    private async UniTask TriggerLifestealEffect(EntityLogic caster)
    {
        // 添加吸血Buff
        var buffManager = caster.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            buffManager = FindObjectOfType<BuffManager>();
        }

        if (buffManager != null)
        {
            int buffInstanceId = buffManager.AddBuff(m_LifestealBuffId, 1, m_LifestealDuration, caster, caster);
            if (buffInstanceId > 0)
            {
                Debug.Log($"三重攻击完成：触发吸血效果，持续{m_LifestealDuration}秒");
                
                // 播放吸血特效
                PlayLifestealEffect(caster);
            }
        }
    }

    /// <summary>
    /// 播放吸血特效
    /// </summary>
    private void PlayLifestealEffect(EntityLogic caster)
    {
        var effectParams = EntityParams.Create(caster.CachedTransform.position);
        effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, m_LifestealDuration);
        GF.Entity.ShowEffect("Effect_Lifesteal", effectParams, m_LifestealDuration);
        
        // 播放吸血音效
        GF.Sound.PlayEffect("Sound_Lifesteal");
    }

    /// <summary>
    /// 获取最近的敌人
    /// </summary>
    protected override Collider GetNearestEnemy(Vector3 center, float range)
    {
        Collider[] targets = GetTargetsInRange(center, range);
        if (targets.Length == 0) return null;

        Collider nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in targets)
        {
            // 检查是否为敌人
            var entity = target.GetComponent<EntityLogic>();
            if (entity is CombatUnitEntity combatUnit && IsEnemy(combatUnit))
            {
                float distance = Vector3.Distance(center, target.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = target;
                    nearestDistance = distance;
                }
            }
        }

        return nearest;
    }

    /// <summary>
    /// 检查是否为敌人
    /// </summary>
    private bool IsEnemy(CombatUnitEntity entity)
    {
        // 这里需要根据实际的阵营系统来判断
        // 假设玩家ID为1，其他为敌人
        return entity.Entity.Id != 1;
    }
}

/// <summary>
/// 攻击命中事件参数
/// </summary>
public class AttackHitEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(AttackHitEventArgs).GetHashCode();

    public override int Id => EventId;

    public int CasterId { get; private set; }
    public int TargetId { get; private set; }
    public int Damage { get; private set; }
    public int AttackNumber { get; private set; }

    public static AttackHitEventArgs Create(int casterId, int targetId, int damage, int attackNumber)
    {
        var args = ReferencePool.Acquire<AttackHitEventArgs>();
        args.CasterId = casterId;
        args.TargetId = targetId;
        args.Damage = damage;
        args.AttackNumber = attackNumber;
        return args;
    }

    public override void Clear()
    {
        CasterId = 0;
        TargetId = 0;
        Damage = 0;
        AttackNumber = 0;
    }
}