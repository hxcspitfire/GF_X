//------------------------------------------------------------
// 三重攻击被动技能 - 攻击敌人3次后触发吸血效果
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 三重攻击被动技能
/// </summary>
public class TripleAttackPassive : SkillEffectBase
{
    [Header("三重攻击被动配置")]
    [SerializeField] private int m_AttackCount = 0;
    [SerializeField] private int m_TriggerCount = 3; // 触发次数
    [SerializeField] private int m_LifestealBuffId = 3001; // 吸血Buff ID
    [SerializeField] private float m_LifestealDuration = 5f; // 吸血持续时间
    [SerializeField] private GameObject m_TriggerEffectPrefab; // 触发特效
    [SerializeField] private string m_TriggerSound; // 触发音效

    private bool m_IsActive = false;
    private EntityLogic m_Player;

    public override async UniTask<bool> Execute()
    {
        if (!m_IsActive) return false;

        // 被动技能不需要主动执行，只需要激活
        m_IsActive = true;
        
        // 订阅攻击事件
        GF.Event.Subscribe(AttackHitEventArgs.EventId, OnPlayerAttack);
        
        Debug.Log("三重攻击被动技能激活：攻击3次后触发吸血效果");
        
        return true;
    }

    /// <summary>
    /// 玩家攻击事件处理
    /// </summary>
    private void OnPlayerAttack(object sender, GameEventArgs e)
    {
        if (!m_IsActive) return;

        var args = e as AttackHitEventArgs;
        if (args == null || args.CasterId != m_Player.Entity.Id) return;

        // 增加攻击计数
        m_AttackCount++;
        Debug.Log($"三重攻击计数: {m_AttackCount}/{m_TriggerCount}");

        // 检查是否达到触发条件
        if (m_AttackCount >= m_TriggerCount)
        {
            // 触发吸血效果
            TriggerLifestealEffect();
            
            // 重置计数
            m_AttackCount = 0;
        }
    }

    /// <summary>
    /// 触发吸血效果
    /// </summary>
    private async void TriggerLifestealEffect()
    {
        if (m_Player == null) return;

        // 添加吸血Buff
        var buffManager = m_Player.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            buffManager = FindObjectOfType<BuffManager>();
        }

        if (buffManager != null)
        {
            int buffInstanceId = buffManager.AddBuff(m_LifestealBuffId, 1, m_LifestealDuration, m_Player, m_Player);
            if (buffInstanceId > 0)
            {
                Debug.Log($"三重攻击触发：吸血效果激活，持续{m_LifestealDuration}秒");
                
                // 播放触发特效
                PlayTriggerEffect();
            }
        }
    }

    /// <summary>
    /// 播放触发特效
    /// </summary>
    private void PlayTriggerEffect()
    {
        if (m_Player == null) return;

        // 播放特效
        if (m_TriggerEffectPrefab != null)
        {
            var effectParams = EntityParams.Create(m_Player.CachedTransform.position);
            effectParams.Set<VarFloat>(ParticleEntity.LIFE_TIME, 2f);
            GF.Entity.ShowEffect(m_TriggerEffectPrefab.name, effectParams, 2f);
        }

        // 播放音效
        if (!string.IsNullOrEmpty(m_TriggerSound))
        {
            GF.Sound.PlayEffect(m_TriggerSound);
        }
    }

    /// <summary>
    /// 停止被动技能
    /// </summary>
    public override void Stop()
    {
        m_IsActive = false;
        m_AttackCount = 0;
        
        // 取消订阅事件
        GF.Event.Unsubscribe(AttackHitEventArgs.EventId, OnPlayerAttack);
        
        Debug.Log("三重攻击被动技能停止");
    }

    /// <summary>
    /// 初始化被动技能
    /// </summary>
    public void InitializePassive(EntityLogic player)
    {
        m_Player = player;
        m_AttackCount = 0;
        m_IsActive = true;
    }

    /// <summary>
    /// 获取当前攻击计数
    /// </summary>
    public int GetAttackCount()
    {
        return m_AttackCount;
    }

    /// <summary>
    /// 获取触发进度 (0-1)
    /// </summary>
    public float GetTriggerProgress()
    {
        return (float)m_AttackCount / m_TriggerCount;
    }

    /// <summary>
    /// 重置攻击计数
    /// </summary>
    public void ResetAttackCount()
    {
        m_AttackCount = 0;
    }
}