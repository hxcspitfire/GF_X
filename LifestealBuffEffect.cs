//------------------------------------------------------------
// 吸血Buff效果 - 攻击时恢复生命值
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 吸血Buff效果
/// </summary>
public class LifestealBuffEffect : MonoBehaviour
{
    [Header("吸血配置")]
    [SerializeField] private float m_LifestealRate = 0.2f; // 吸血比例（20%）
    [SerializeField] private float m_MinLifesteal = 1f; // 最小吸血值
    [SerializeField] private float m_MaxLifesteal = 50f; // 最大吸血值
    [SerializeField] private GameObject m_LifestealEffectPrefab; // 吸血特效
    [SerializeField] private string m_LifestealSound; // 吸血音效

    private BuffInstance m_BuffInstance;
    private EntityLogic m_Target;
    private bool m_IsActive = false;

    /// <summary>
    /// 初始化吸血效果
    /// </summary>
    public void Initialize(BuffInstance buffInstance, EntityLogic target)
    {
        m_BuffInstance = buffInstance;
        m_Target = target;
        m_IsActive = true;

        // 订阅攻击事件
        GF.Event.Subscribe(AttackHitEventArgs.EventId, OnAttackHit);
        
        Debug.Log($"吸血效果激活：吸血比例{m_LifestealRate * 100}%");
    }

    /// <summary>
    /// 攻击命中事件处理
    /// </summary>
    private void OnAttackHit(object sender, GameEventArgs e)
    {
        if (!m_IsActive || m_Target == null) return;

        var args = e as AttackHitEventArgs;
        if (args == null || args.CasterId != m_Target.Entity.Id) return;

        // 计算吸血量
        float lifestealAmount = CalculateLifestealAmount(args.Damage);
        
        if (lifestealAmount > 0)
        {
            // 恢复生命值
            HealTarget(lifestealAmount);
            
            // 播放吸血特效
            PlayLifestealEffect(args.TargetId, lifestealAmount);
            
            Debug.Log($"吸血效果：恢复{Mathf.RoundToInt(lifestealAmount)}点生命值");
        }
    }

    /// <summary>
    /// 计算吸血量
    /// </summary>
    private float CalculateLifestealAmount(int damage)
    {
        // 基础吸血量 = 伤害 * 吸血比例
        float baseLifesteal = damage * m_LifestealRate;
        
        // 应用Buff层数修正
        if (m_BuffInstance != null)
        {
            baseLifesteal *= m_BuffInstance.Stacks;
        }
        
        // 限制在最小值和最大值之间
        float finalLifesteal = Mathf.Clamp(baseLifesteal, m_MinLifesteal, m_MaxLifesteal);
        
        return finalLifesteal;
    }

    /// <summary>
    /// 治疗目标
    /// </summary>
    private void HealTarget(float healAmount)
    {
        if (m_Target is CombatUnitEntity combatUnit)
        {
            int heal = Mathf.RoundToInt(healAmount);
            combatUnit.Hp += heal;
            
            // 触发治疗事件
            GF.Event.Fire(this, HealEventArgs.Create(m_Target.Entity.Id, heal, "Lifesteal"));
        }
    }

    /// <summary>
    /// 播放吸血特效
    /// </summary>
    private void PlayLifestealEffect(int targetId, float healAmount)
    {
        // 获取目标位置
        var target = GF.Entity.GetEntity<EntityLogic>(targetId);
        if (target == null) return;

        // 播放特效
        if (m_LifestealEffectPrefab != null)
        {
            var effectParams = EntityParams.Create(target.CachedTransform.position);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, 1f);
            effectParams.Set<VarFloat>("HealAmount", healAmount);
            GF.Entity.ShowEffect(m_LifestealEffectPrefab.name, effectParams, 1f);
        }

        // 播放音效
        if (!string.IsNullOrEmpty(m_LifestealSound))
        {
            GF.Sound.PlayEffect(m_LifestealSound);
        }

        // 显示治疗数字
        ShowHealNumber(target.CachedTransform.position, healAmount);
    }

    /// <summary>
    /// 显示治疗数字
    /// </summary>
    private void ShowHealNumber(Vector3 position, float healAmount)
    {
        // 这里可以显示飘字效果
        // 例如：+25 (绿色)
        Debug.Log($"治疗数字：+{Mathf.RoundToInt(healAmount)} at {position}");
    }

    /// <summary>
    /// 停止吸血效果
    /// </summary>
    public void Stop()
    {
        m_IsActive = false;
        
        // 取消订阅事件
        GF.Event.Unsubscribe(AttackHitEventArgs.EventId, OnAttackHit);
        
        Debug.Log("吸血效果结束");
    }

    private void OnDestroy()
    {
        Stop();
    }
}

/// <summary>
/// 治疗事件参数
/// </summary>
public class HealEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(HealEventArgs).GetHashCode();

    public override int Id => EventId;

    public int TargetId { get; private set; }
    public int HealAmount { get; private set; }
    public string HealSource { get; private set; }

    public static HealEventArgs Create(int targetId, int healAmount, string healSource)
    {
        var args = ReferencePool.Acquire<HealEventArgs>();
        args.TargetId = targetId;
        args.HealAmount = healAmount;
        args.HealSource = healSource;
        return args;
    }

    public override void Clear()
    {
        TargetId = 0;
        HealAmount = 0;
        HealSource = string.Empty;
    }
}