//------------------------------------------------------------
// 技能系统使用示例 - 展示如何使用技能系统
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 技能系统使用示例
/// </summary>
public class SkillSystemExample : MonoBehaviour
{
    [Header("技能配置")]
    [SerializeField] private int[] m_TestSkillIds = { 1, 2, 3, 4, 5 };
    [SerializeField] private KeyCode[] m_SkillKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T };

    private SkillManager m_SkillManager;
    private BuffManager m_BuffManager;
    private SkillDataModel m_SkillDataModel;
    private SkillEffectFactory m_EffectFactory;

    private void Start()
    {
        InitializeSkillSystem();
        SetupTestSkills();
    }

    /// <summary>
    /// 初始化技能系统
    /// </summary>
    private void InitializeSkillSystem()
    {
        // 获取技能管理器
        m_SkillManager = FindObjectOfType<SkillManager>();
        if (m_SkillManager == null)
        {
            m_SkillManager = gameObject.AddComponent<SkillManager>();
        }

        // 获取Buff管理器
        m_BuffManager = FindObjectOfType<BuffManager>();
        if (m_BuffManager == null)
        {
            m_BuffManager = gameObject.AddComponent<BuffManager>();
        }

        // 获取技能数据模型
        m_SkillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();

        // 获取效果工厂
        m_EffectFactory = FindObjectOfType<SkillEffectFactory>();
        if (m_EffectFactory == null)
        {
            m_EffectFactory = gameObject.AddComponent<SkillEffectFactory>();
        }
    }

    /// <summary>
    /// 设置测试技能
    /// </summary>
    private void SetupTestSkills()
    {
        // 解锁测试技能
        foreach (int skillId in m_TestSkillIds)
        {
            m_SkillDataModel.UnlockSkill(skillId);
            m_SkillDataModel.SetSkillLevel(skillId, 1);
        }

        // 添加技能点数
        m_SkillDataModel.SkillPoints = 100;
    }

    private void Update()
    {
        HandleSkillInput();
        HandleTestCommands();
    }

    /// <summary>
    /// 处理技能输入
    /// </summary>
    private void HandleSkillInput()
    {
        for (int i = 0; i < m_SkillKeys.Length && i < m_TestSkillIds.Length; i++)
        {
            if (Input.GetKeyDown(m_SkillKeys[i]))
            {
                CastSkill(m_TestSkillIds[i]);
            }
        }
    }

    /// <summary>
    /// 处理测试命令
    /// </summary>
    private void HandleTestCommands()
    {
        // 按1键添加攻击力Buff
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddTestBuff(1001, 10f); // 攻击力Buff
        }

        // 按2键添加防御力Buff
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddTestBuff(1002, 15f); // 防御力Buff
        }

        // 按3键添加移动速度Buff
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AddTestBuff(1003, 20f); // 移动速度Buff
        }

        // 按4键添加持续伤害Debuff
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            AddTestBuff(2001, 5f); // 持续伤害Debuff
        }

        // 按5键清除所有Debuff
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ClearAllDebuffs();
        }

        // 按6键升级技能
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            UpgradeRandomSkill();
        }

        // 按7键重置所有技能冷却
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            ResetAllCooldowns();
        }
    }

    /// <summary>
    /// 释放技能
    /// </summary>
    private async void CastSkill(int skillId)
    {
        if (m_SkillManager == null) return;

        Vector3 targetPos = GetMouseWorldPosition();
        bool success = await m_SkillManager.CastSkill(skillId, targetPos);
        
        if (success)
        {
            Debug.Log($"技能 {skillId} 释放成功");
        }
        else
        {
            Debug.Log($"技能 {skillId} 释放失败");
        }
    }

    /// <summary>
    /// 添加测试Buff
    /// </summary>
    private void AddTestBuff(int buffId, float duration)
    {
        if (m_BuffManager == null) return;

        var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1);
        if (playerEntity == null) return;

        int instanceId = m_BuffManager.AddBuff(buffId, 1, duration, playerEntity);
        if (instanceId > 0)
        {
            Debug.Log($"添加Buff {buffId}，持续时间 {duration}秒");
        }
        else
        {
            Debug.Log($"添加Buff {buffId} 失败");
        }
    }

    /// <summary>
    /// 清除所有Debuff
    /// </summary>
    private void ClearAllDebuffs()
    {
        if (m_BuffManager == null) return;

        var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1);
        if (playerEntity == null) return;

        m_BuffManager.RemoveAllBuffs(playerEntity.Entity.Id, true);
        Debug.Log("清除所有Debuff");
    }

    /// <summary>
    /// 升级随机技能
    /// </summary>
    private void UpgradeRandomSkill()
    {
        if (m_SkillDataModel == null) return;

        var unlockedSkills = m_SkillDataModel.GetUnlockedSkills();
        if (unlockedSkills.Count == 0) return;

        var randomSkill = unlockedSkills[Random.Range(0, unlockedSkills.Count)];
        if (m_SkillDataModel.CanUpgradeSkill(randomSkill.SkillId))
        {
            bool success = m_SkillDataModel.UpgradeSkill(randomSkill.SkillId);
            if (success)
            {
                Debug.Log($"升级技能 {randomSkill.SkillId} 成功，当前等级 {randomSkill.Level + 1}");
            }
        }
    }

    /// <summary>
    /// 重置所有技能冷却
    /// </summary>
    private void ResetAllCooldowns()
    {
        if (m_SkillManager == null) return;

        m_SkillManager.ResetAllCooldowns();
        Debug.Log("重置所有技能冷却");
    }

    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Camera camera = Camera.main;
        if (camera == null) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = camera.nearClipPlane;
        return camera.ScreenToWorldPoint(mousePos);
    }

    /// <summary>
    /// 显示技能信息
    /// </summary>
    private void OnGUI()
    {
        if (m_SkillDataModel == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("技能系统测试", GUI.skin.box);
        GUILayout.Label($"技能点数: {m_SkillDataModel.SkillPoints}");
        
        GUILayout.Space(10);
        GUILayout.Label("快捷键:");
        GUILayout.Label("Q/W/E/R/T - 释放技能");
        GUILayout.Label("1/2/3 - 添加Buff");
        GUILayout.Label("4 - 添加Debuff");
        GUILayout.Label("5 - 清除Debuff");
        GUILayout.Label("6 - 升级随机技能");
        GUILayout.Label("7 - 重置冷却");

        GUILayout.Space(10);
        GUILayout.Label("技能状态:");
        foreach (int skillId in m_TestSkillIds)
        {
            bool isUnlocked = m_SkillDataModel.IsSkillUnlocked(skillId);
            int level = m_SkillDataModel.GetSkillData(skillId).Level;
            float cooldown = m_SkillManager.GetSkillCooldownRemaining(skillId);
            
            string status = isUnlocked ? $"Lv.{level}" : "锁定";
            if (cooldown > 0) status += $" (冷却:{cooldown:F1}s)";
            
            GUILayout.Label($"技能{skillId}: {status}");
        }

        GUILayout.EndArea();
    }
}