//------------------------------------------------------------
// 动态属性系统测试脚本
//------------------------------------------------------------
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 动态属性系统测试
/// </summary>
public class DynamicAttributeTest : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private KeyCode m_AddAttributeKey = KeyCode.A;
    [SerializeField] private KeyCode m_RemoveAttributeKey = KeyCode.R;
    [SerializeField] private KeyCode m_ModifyAttributeKey = KeyCode.M;
    [SerializeField] private KeyCode m_ResetAttributeKey = KeyCode.T;

    private DynamicAttributeSystem m_AttributeSystem;
    private DynamicBuffSystem m_BuffSystem;
    private DynamicSkillManager m_SkillManager;

    private void Start()
    {
        // 获取组件
        m_AttributeSystem = GetComponent<DynamicAttributeSystem>();
        if (m_AttributeSystem == null)
        {
            m_AttributeSystem = gameObject.AddComponent<DynamicAttributeSystem>();
        }

        m_BuffSystem = GetComponent<DynamicBuffSystem>();
        if (m_BuffSystem == null)
        {
            m_BuffSystem = gameObject.AddComponent<DynamicBuffSystem>();
        }

        m_SkillManager = GetComponent<DynamicSkillManager>();
        if (m_SkillManager == null)
        {
            m_SkillManager = gameObject.AddComponent<DynamicSkillManager>();
        }

        // 订阅属性变更事件
        m_AttributeSystem.OnAttributeChanged += OnAttributeChanged;
        m_AttributeSystem.OnAttributeAdded += OnAttributeAdded;
        m_AttributeSystem.OnAttributeRemoved += OnAttributeRemoved;

        // 添加一些测试属性
        AddTestAttributes();
    }

    /// <summary>
    /// 添加测试属性
    /// </summary>
    private void AddTestAttributes()
    {
        // 添加自定义属性
        m_AttributeSystem.AddAttribute("TestBool", AttributeType.Boolean, true);
        m_AttributeSystem.AddAttribute("TestInt", AttributeType.Integer, 100);
        m_AttributeSystem.AddAttribute("TestFloat", AttributeType.Float, 3.14f);
        m_AttributeSystem.AddAttribute("TestString", AttributeType.String, "Hello World");

        Debug.Log("测试属性添加完成");
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
        // 添加属性
        if (Input.GetKeyDown(m_AddAttributeKey))
        {
            AddRandomAttribute();
        }

        // 移除属性
        if (Input.GetKeyDown(m_RemoveAttributeKey))
        {
            RemoveRandomAttribute();
        }

        // 修改属性
        if (Input.GetKeyDown(m_ModifyAttributeKey))
        {
            ModifyRandomAttribute();
        }

        // 重置属性
        if (Input.GetKeyDown(m_ResetAttributeKey))
        {
            ResetAllAttributes();
        }

        // 测试Buff
        if (Input.GetKeyDown(KeyCode.B))
        {
            TestBuff();
        }

        // 测试技能
        if (Input.GetKeyDown(KeyCode.S))
        {
            TestSkill();
        }
    }

    /// <summary>
    /// 添加随机属性
    /// </summary>
    private void AddRandomAttribute()
    {
        string name = "RandomAttribute_" + Random.Range(1000, 9999);
        AttributeType type = (AttributeType)Random.Range(0, 4);
        
        object value = null;
        switch (type)
        {
            case AttributeType.Boolean:
                value = Random.value > 0.5f;
                break;
            case AttributeType.Integer:
                value = Random.Range(0, 100);
                break;
            case AttributeType.Float:
                value = Random.Range(0f, 100f);
                break;
            case AttributeType.String:
                value = "Random_" + Random.Range(1000, 9999);
                break;
        }

        if (m_AttributeSystem.AddAttribute(name, type, value))
        {
            Debug.Log($"添加属性成功: {name} = {value} ({type})");
        }
        else
        {
            Debug.Log($"添加属性失败: {name}");
        }
    }

    /// <summary>
    /// 移除随机属性
    /// </summary>
    private void RemoveRandomAttribute()
    {
        var names = m_AttributeSystem.GetAllAttributeNames();
        if (names.Length > 0)
        {
            string name = names[Random.Range(0, names.Length)];
            if (m_AttributeSystem.RemoveAttribute(name))
            {
                Debug.Log($"移除属性成功: {name}");
            }
            else
            {
                Debug.Log($"移除属性失败: {name}");
            }
        }
    }

    /// <summary>
    /// 修改随机属性
    /// </summary>
    private void ModifyRandomAttribute()
    {
        var names = m_AttributeSystem.GetAllAttributeNames();
        if (names.Length > 0)
        {
            string name = names[Random.Range(0, names.Length)];
            var definition = m_AttributeSystem.GetAttributeDefinition(name);
            
            if (definition != null)
            {
                object newValue = null;
                switch (definition.Type)
                {
                    case AttributeType.Boolean:
                        newValue = !m_AttributeSystem.GetBoolAttribute(name);
                        break;
                    case AttributeType.Integer:
                        newValue = m_AttributeSystem.GetIntAttribute(name) + Random.Range(-10, 10);
                        break;
                    case AttributeType.Float:
                        newValue = m_AttributeSystem.GetFloatAttribute(name) + Random.Range(-1f, 1f);
                        break;
                    case AttributeType.String:
                        newValue = "Modified_" + Random.Range(1000, 9999);
                        break;
                }

                if (m_AttributeSystem.SetAttribute(name, newValue))
                {
                    Debug.Log($"修改属性成功: {name} = {newValue}");
                }
                else
                {
                    Debug.Log($"修改属性失败: {name}");
                }
            }
        }
    }

    /// <summary>
    /// 重置所有属性
    /// </summary>
    private void ResetAllAttributes()
    {
        m_AttributeSystem.ResetAllAttributes();
        Debug.Log("所有属性已重置为默认值");
    }

    /// <summary>
    /// 测试Buff
    /// </summary>
    private void TestBuff()
    {
        // 添加沉默Buff
        m_BuffSystem.AddBuff(2004, 1, 5f); // 沉默Buff ID
        Debug.Log("添加沉默Buff，持续5秒");
    }

    /// <summary>
    /// 测试技能
    /// </summary>
    private void TestSkill()
    {
        if (m_SkillManager.CanCastSkill(101))
        {
            Vector3 targetPos = GetMouseWorldPosition();
            m_SkillManager.CastSkill(101, targetPos);
            Debug.Log("释放技能");
        }
        else
        {
            Debug.Log("无法释放技能");
        }
    }

    /// <summary>
    /// 属性变更事件处理
    /// </summary>
    private void OnAttributeChanged(string name, object oldValue, object newValue)
    {
        Debug.Log($"属性变更: {name} {oldValue} -> {newValue}");
    }

    /// <summary>
    /// 属性添加事件处理
    /// </summary>
    private void OnAttributeAdded(string name)
    {
        Debug.Log($"属性添加: {name}");
    }

    /// <summary>
    /// 属性移除事件处理
    /// </summary>
    private void OnAttributeRemoved(string name)
    {
        Debug.Log($"属性移除: {name}");
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
        if (m_AttributeSystem == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 500, 400));
        GUILayout.Label("动态属性系统测试", GUI.skin.box);
        
        GUILayout.Space(10);
        GUILayout.Label("控制说明:");
        GUILayout.Label($"A键 - 添加随机属性");
        GUILayout.Label($"R键 - 移除随机属性");
        GUILayout.Label($"M键 - 修改随机属性");
        GUILayout.Label($"T键 - 重置所有属性");
        GUILayout.Label($"B键 - 测试Buff");
        GUILayout.Label($"S键 - 测试技能");
        
        GUILayout.Space(10);
        GUILayout.Label("属性信息:");
        GUILayout.Label(m_AttributeSystem.GetAttributeInfo());
        
        GUILayout.Space(10);
        GUILayout.Label("状态检查:");
        GUILayout.Label($"能否释放技能: {m_AttributeSystem.CanPerformAction("castskill")}");
        GUILayout.Label($"能否移动: {m_AttributeSystem.CanPerformAction("move")}");
        GUILayout.Label($"能否攻击: {m_AttributeSystem.CanPerformAction("attack")}");
        GUILayout.Label($"是否死亡: {m_AttributeSystem.IsDead()}");

        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (m_AttributeSystem != null)
        {
            m_AttributeSystem.OnAttributeChanged -= OnAttributeChanged;
            m_AttributeSystem.OnAttributeAdded -= OnAttributeAdded;
            m_AttributeSystem.OnAttributeRemoved -= OnAttributeRemoved;
        }
    }
}