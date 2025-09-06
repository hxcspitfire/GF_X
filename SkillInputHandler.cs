//------------------------------------------------------------
// 技能输入处理器 - 处理技能快捷键输入
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 技能输入处理器
/// </summary>
public class SkillInputHandler : MonoBehaviour
{
    [SerializeField] private KeyCode[] m_SkillKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T };
    [SerializeField] private int[] m_SkillIds = { 1, 2, 3, 4, 5 };
    
    private SkillManager m_SkillManager;
    private Camera m_Camera;

    private void Start()
    {
        m_SkillManager = FindObjectOfType<SkillManager>();
        m_Camera = Camera.main;
    }

    private void Update()
    {
        if (m_SkillManager == null) return;

        // 检查技能快捷键
        for (int i = 0; i < m_SkillKeys.Length && i < m_SkillIds.Length; i++)
        {
            if (Input.GetKeyDown(m_SkillKeys[i]))
            {
                CastSkillByKey(m_SkillIds[i]);
            }
        }

        // 检查鼠标右键释放技能
        if (Input.GetMouseButtonDown(1))
        {
            CastSkillByMouse();
        }
    }

    /// <summary>
    /// 通过快捷键释放技能
    /// </summary>
    private void CastSkillByKey(int skillId)
    {
        if (m_SkillManager.CanCastSkill(skillId))
        {
            Vector3 targetPos = GetMouseWorldPosition();
            m_SkillManager.CastSkill(skillId, targetPos);
        }
    }

    /// <summary>
    /// 通过鼠标释放技能
    /// </summary>
    private void CastSkillByMouse()
    {
        // 获取鼠标位置下的技能
        int skillId = GetSkillUnderMouse();
        if (skillId > 0 && m_SkillManager.CanCastSkill(skillId))
        {
            Vector3 targetPos = GetMouseWorldPosition();
            m_SkillManager.CastSkill(skillId, targetPos);
        }
    }

    /// <summary>
    /// 获取鼠标世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        if (m_Camera == null) return Vector3.zero;
        
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = m_Camera.nearClipPlane;
        return m_Camera.ScreenToWorldPoint(mousePos);
    }

    /// <summary>
    /// 获取鼠标下的技能ID
    /// </summary>
    private int GetSkillUnderMouse()
    {
        // 这里可以实现鼠标悬停检测技能UI的逻辑
        // 暂时返回0表示没有检测到技能
        return 0;
    }
}