//------------------------------------------------------------
// 三重攻击被动技能测试脚本
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 三重攻击被动技能测试
/// </summary>
public class TripleAttackPassiveTest : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private KeyCode m_AttackKey = KeyCode.Space; // 攻击键
    [SerializeField] private KeyCode m_UpgradeKey = KeyCode.U; // 升级键
    [SerializeField] private KeyCode m_ResetKey = KeyCode.R; // 重置键

    private PassiveSkillManager m_PassiveSkillManager;
    private SkillDataModel m_SkillDataModel;
    private int m_AttackCount = 0;

    private void Start()
    {
        // 获取被动技能管理器
        m_PassiveSkillManager = FindObjectOfType<PassiveSkillManager>();
        if (m_PassiveSkillManager == null)
        {
            m_PassiveSkillManager = gameObject.AddComponent<PassiveSkillManager>();
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
        // 解锁三重攻击被动技能
        m_SkillDataModel.UnlockSkill(101);
        m_SkillDataModel.SetSkillLevel(101, 1);

        // 添加技能点数
        m_SkillDataModel.SkillPoints = 1000;

        Debug.Log("三重攻击被动技能测试数据初始化完成");
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
        // 模拟攻击
        if (Input.GetKeyDown(m_AttackKey))
        {
            SimulateAttack();
        }

        // 升级技能
        if (Input.GetKeyDown(m_UpgradeKey))
        {
            UpgradeSkill();
        }

        // 重置计数
        if (Input.GetKeyDown(m_ResetKey))
        {
            ResetCount();
        }
    }

    /// <summary>
    /// 模拟攻击
    /// </summary>
    private void SimulateAttack()
    {
        if (m_PassiveSkillManager == null) return;

        // 增加攻击计数
        m_AttackCount++;
        
        // 模拟攻击事件
        var playerEntity = GF.Entity.GetEntity<PlayerEntity>(1);
        if (playerEntity != null)
        {
            // 模拟对敌人造成伤害
            int damage = Random.Range(20, 40);
            int targetId = Random.Range(2, 10); // 模拟敌人ID
            
            // 触发攻击事件
            GF.Event.Fire(this, AttackHitEventArgs.Create(playerEntity.Entity.Id, targetId, damage, 1));
            
            Debug.Log($"模拟攻击 #{m_AttackCount}: 造成{damage}点伤害");
        }
    }

    /// <summary>
    /// 升级技能
    /// </summary>
    private void UpgradeSkill()
    {
        if (m_SkillDataModel == null) return;

        if (m_SkillDataModel.CanUpgradeSkill(101))
        {
            bool success = m_SkillDataModel.UpgradeSkill(101);
            if (success)
            {
                int newLevel = m_SkillDataModel.GetSkillData(101).Level;
                Debug.Log($"三重攻击被动技能升级成功，当前等级: {newLevel}");
            }
            else
            {
                Debug.Log("三重攻击被动技能升级失败");
            }
        }
        else
        {
            Debug.Log("三重攻击被动技能无法升级");
        }
    }

    /// <summary>
    /// 重置计数
    /// </summary>
    private void ResetCount()
    {
        if (m_PassiveSkillManager != null)
        {
            m_PassiveSkillManager.ResetTripleAttackCount();
            m_AttackCount = 0;
            Debug.Log("攻击计数已重置");
        }
    }

    /// <summary>
    /// 显示测试信息
    /// </summary>
    private void OnGUI()
    {
        if (m_PassiveSkillManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 350));
        GUILayout.Label("三重攻击被动技能测试", GUI.skin.box);
        
        GUILayout.Space(10);
        GUILayout.Label("控制说明:");
        GUILayout.Label($"空格键 - 模拟攻击");
        GUILayout.Label($"U键 - 升级技能");
        GUILayout.Label($"R键 - 重置计数");
        
        GUILayout.Space(10);
        GUILayout.Label("技能信息:");
        GUILayout.Label($"技能类型: 被动技能");
        GUILayout.Label($"技能描述: 攻击敌人3次后触发吸血效果");
        GUILayout.Label($"技能等级: {m_SkillDataModel.GetSkillData(101).Level}");
        GUILayout.Label($"技能解锁: {m_SkillDataModel.IsSkillUnlocked(101)}");
        
        GUILayout.Space(10);
        GUILayout.Label("状态信息:");
        GUILayout.Label($"技能点数: {m_SkillDataModel.SkillPoints}");
        GUILayout.Label($"被动技能激活: {m_PassiveSkillManager.IsPassiveSkillActive(101)}");
        GUILayout.Label($"攻击计数: {m_PassiveSkillManager.GetTripleAttackCount()}/3");
        GUILayout.Label($"触发进度: {m_PassiveSkillManager.GetTripleAttackProgress():P0}");
        GUILayout.Label($"总攻击次数: {m_AttackCount}");
        
        GUILayout.Space(10);
        GUILayout.Label("说明:");
        GUILayout.Label("这是一个被动技能，不需要主动释放。");
        GUILayout.Label("每次攻击都会增加计数，达到3次时触发吸血效果。");
        GUILayout.Label("吸血效果持续5秒，攻击时恢复20%伤害的生命值。");

        GUILayout.EndArea();
    }
}