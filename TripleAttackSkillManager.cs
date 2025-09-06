//------------------------------------------------------------
// 三重攻击技能管理器 - 专门处理三重攻击技能
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 三重攻击技能管理器
/// </summary>
public class TripleAttackSkillManager : MonoBehaviour
{
    [Header("三重攻击配置")]
    [SerializeField] private int m_TripleAttackSkillId = 101;
    [SerializeField] private int m_LifestealBuffId = 3001;
    [SerializeField] private GameObject m_TripleAttackEffectPrefab;
    [SerializeField] private string m_TripleAttackSound = "Sound_TripleAttack";

    private SkillManager m_SkillManager;
    private LifestealBuffManager m_LifestealBuffManager;

    private void Start()
    {
        // 获取技能管理器
        m_SkillManager = GetComponent<SkillManager>();
        if (m_SkillManager == null)
        {
            m_SkillManager = FindObjectOfType<SkillManager>();
        }

        // 获取吸血Buff管理器
        m_LifestealBuffManager = GetComponent<LifestealBuffManager>();
        if (m_LifestealBuffManager == null)
        {
            m_LifestealBuffManager = FindObjectOfType<LifestealBuffManager>();
        }

        // 如果没有吸血Buff管理器，创建一个
        if (m_LifestealBuffManager == null)
        {
            m_LifestealBuffManager = gameObject.AddComponent<LifestealBuffManager>();
        }
    }

    /// <summary>
    /// 释放三重攻击技能
    /// </summary>
    public async UniTask<bool> CastTripleAttack(Vector3 targetPosition, int targetId = 0)
    {
        if (m_SkillManager == null)
        {
            Debug.LogError("技能管理器未找到");
            return false;
        }

        // 检查技能是否可以释放
        if (!m_SkillManager.CanCastSkill(m_TripleAttackSkillId))
        {
            Debug.Log("三重攻击技能无法释放");
            return false;
        }

        // 获取技能数据
        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        var skillRow = skillTable.GetDataRow(m_TripleAttackSkillId);
        if (skillRow == null)
        {
            Debug.LogError($"技能ID {m_TripleAttackSkillId} 未找到");
            return false;
        }

        // 获取玩家实体
        var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1);
        if (playerEntity == null)
        {
            Debug.LogError("玩家实体未找到");
            return false;
        }

        // 创建三重攻击效果
        GameObject effectObj = new GameObject("TripleAttackEffect");
        var tripleAttackEffect = effectObj.AddComponent<TripleAttackEffect>();
        
        // 配置效果参数
        tripleAttackEffect.m_Damage = skillRow.BaseDamage;
        tripleAttackEffect.m_Range = skillRow.SkillRange;
        tripleAttackEffect.m_LifestealBuffId = m_LifestealBuffId;
        tripleAttackEffect.m_AttackEffectPrefab = m_TripleAttackEffectPrefab;
        tripleAttackEffect.m_AttackSound = m_TripleAttackSound;

        // 初始化并执行效果
        tripleAttackEffect.Initialize(m_TripleAttackSkillId, 1, targetPosition, targetId);
        bool success = await tripleAttackEffect.Execute();

        // 清理效果对象
        Destroy(effectObj);

        return success;
    }

    /// <summary>
    /// 检查三重攻击技能是否可用
    /// </summary>
    public bool CanCastTripleAttack()
    {
        if (m_SkillManager == null) return false;
        return m_SkillManager.CanCastSkill(m_TripleAttackSkillId);
    }

    /// <summary>
    /// 获取三重攻击技能冷却剩余时间
    /// </summary>
    public float GetTripleAttackCooldownRemaining()
    {
        if (m_SkillManager == null) return 0f;
        return m_SkillManager.GetSkillCooldownRemaining(m_TripleAttackSkillId);
    }

    /// <summary>
    /// 获取三重攻击技能冷却进度
    /// </summary>
    public float GetTripleAttackCooldownProgress()
    {
        if (m_SkillManager == null) return 0f;
        return m_SkillManager.GetSkillCooldownProgress(m_TripleAttackSkillId);
    }

    /// <summary>
    /// 升级三重攻击技能
    /// </summary>
    public bool UpgradeTripleAttack()
    {
        var skillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();
        if (skillDataModel == null) return false;

        return skillDataModel.UpgradeSkill(m_TripleAttackSkillId);
    }

    /// <summary>
    /// 检查三重攻击技能是否可以升级
    /// </summary>
    public bool CanUpgradeTripleAttack()
    {
        var skillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();
        if (skillDataModel == null) return false;

        return skillDataModel.CanUpgradeSkill(m_TripleAttackSkillId);
    }

    /// <summary>
    /// 获取三重攻击技能等级
    /// </summary>
    public int GetTripleAttackLevel()
    {
        var skillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();
        if (skillDataModel == null) return 0;

        var skillData = skillDataModel.GetSkillData(m_TripleAttackSkillId);
        return skillData?.Level ?? 0;
    }

    /// <summary>
    /// 获取三重攻击技能信息
    /// </summary>
    public string GetTripleAttackInfo()
    {
        var skillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();
        if (skillDataModel == null) return "技能数据未找到";

        var skillData = skillDataModel.GetSkillData(m_TripleAttackSkillId);
        if (skillData == null) return "技能数据未找到";

        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        var skillRow = skillTable.GetDataRow(m_TripleAttackSkillId);
        if (skillRow == null) return "技能配置未找到";

        float cooldown = GetTripleAttackCooldownRemaining();
        string cooldownText = cooldown > 0 ? $" (冷却: {cooldown:F1}s)" : "";

        return $"三重攻击 Lv.{skillData.Level}{cooldownText}\n" +
               $"描述: {skillRow.SkillDescription}\n" +
               $"伤害: {skillRow.BaseDamage}\n" +
               $"范围: {skillRow.SkillRange}\n" +
               $"冷却: {skillRow.BaseCooldown}s\n" +
               $"魔法消耗: {skillRow.BaseManaCost}";
    }
}