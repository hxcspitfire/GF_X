//------------------------------------------------------------
// 三重攻击技能测试脚本
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 三重攻击技能测试
/// </summary>
public class TripleAttackTest : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private KeyCode m_TripleAttackKey = KeyCode.Q;
    [SerializeField] private KeyCode m_UpgradeKey = KeyCode.U;
    [SerializeField] private KeyCode m_ResetCooldownKey = KeyCode.R;

    private TripleAttackSkillManager m_TripleAttackManager;
    private SkillDataModel m_SkillDataModel;

    private void Start()
    {
        // 获取三重攻击管理器
        m_TripleAttackManager = FindObjectOfType<TripleAttackSkillManager>();
        if (m_TripleAttackManager == null)
        {
            m_TripleAttackManager = gameObject.AddComponent<TripleAttackSkillManager>();
        }

        // 获取技能数据模型
        m_SkillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();

        // 初始化测试数据
        InitializeTestData();
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    private void InitializeTestData()
    {
        // 解锁三重攻击技能
        m_SkillDataModel.UnlockSkill(101);
        m_SkillDataModel.SetSkillLevel(101, 1);

        // 添加技能点数
        m_SkillDataModel.SkillPoints = 1000;

        Debug.Log("三重攻击技能测试数据初始化完成");
    }

    private void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 释放三重攻击技能
        if (Input.GetKeyDown(m_TripleAttackKey))
        {
            CastTripleAttack();
        }

        // 升级技能
        if (Input.GetKeyDown(m_UpgradeKey))
        {
            UpgradeTripleAttack();
        }

        // 重置冷却
        if (Input.GetKeyDown(m_ResetCooldownKey))
        {
            ResetCooldown();
        }
    }

    /// <summary>
    /// 释放三重攻击技能
    /// </summary>
    private async void CastTripleAttack()
    {
        if (m_TripleAttackManager == null)
        {
            Debug.LogError("三重攻击管理器未找到");
            return;
        }

        if (!m_TripleAttackManager.CanCastTripleAttack())
        {
            Debug.Log("三重攻击技能无法释放");
            return;
        }

        Vector3 targetPos = GetMouseWorldPosition();
        Debug.Log($"释放三重攻击技能，目标位置: {targetPos}");

        bool success = await m_TripleAttackManager.CastTripleAttack(targetPos);
        
        if (success)
        {
            Debug.Log("三重攻击技能释放成功");
        }
        else
        {
            Debug.Log("三重攻击技能释放失败");
        }
    }

    /// <summary>
    /// 升级三重攻击技能
    /// </summary>
    private void UpgradeTripleAttack()
    {
        if (m_TripleAttackManager == null) return;

        if (m_TripleAttackManager.CanUpgradeTripleAttack())
        {
            bool success = m_TripleAttackManager.UpgradeTripleAttack();
            if (success)
            {
                int newLevel = m_TripleAttackManager.GetTripleAttackLevel();
                Debug.Log($"三重攻击技能升级成功，当前等级: {newLevel}");
            }
            else
            {
                Debug.Log("三重攻击技能升级失败");
            }
        }
        else
        {
            Debug.Log("三重攻击技能无法升级");
        }
    }

    /// <summary>
    /// 重置冷却
    /// </summary>
    private void ResetCooldown()
    {
        var skillManager = FindObjectOfType<SkillManager>();
        if (skillManager != null)
        {
            skillManager.ResetAllCooldowns();
            Debug.Log("所有技能冷却已重置");
        }
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
    /// 显示测试信息
    /// </summary>
    private void OnGUI()
    {
        if (m_TripleAttackManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.Label("三重攻击技能测试", GUI.skin.box);
        
        GUILayout.Space(10);
        GUILayout.Label("控制说明:");
        GUILayout.Label($"Q键 - 释放三重攻击技能");
        GUILayout.Label($"U键 - 升级技能");
        GUILayout.Label($"R键 - 重置冷却");
        
        GUILayout.Space(10);
        GUILayout.Label("技能信息:");
        GUILayout.Label(m_TripleAttackManager.GetTripleAttackInfo());
        
        GUILayout.Space(10);
        GUILayout.Label("状态信息:");
        GUILayout.Label($"技能点数: {m_SkillDataModel.SkillPoints}");
        GUILayout.Label($"技能等级: {m_TripleAttackManager.GetTripleAttackLevel()}");
        GUILayout.Label($"可释放: {m_TripleAttackManager.CanCastTripleAttack()}");
        GUILayout.Label($"可升级: {m_TripleAttackManager.CanUpgradeTripleAttack()}");
        
        float cooldown = m_TripleAttackManager.GetTripleAttackCooldownRemaining();
        if (cooldown > 0)
        {
            GUILayout.Label($"冷却剩余: {cooldown:F1}秒");
        }
        else
        {
            GUILayout.Label("技能就绪");
        }

        GUILayout.EndArea();
    }
}