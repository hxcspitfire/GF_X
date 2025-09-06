//------------------------------------------------------------
// 技能UI界面 - 显示技能图标、冷却、快捷键等
//------------------------------------------------------------
using GameFramework;
using GameFramework.Event;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 技能UI界面
/// </summary>
public class SkillUIForm : UIFormBase
{
    [SerializeField] private Transform m_SkillContainer;
    [SerializeField] private GameObject m_SkillItemPrefab;
    [SerializeField] private Button m_CloseButton;
    [SerializeField] private Text m_SkillPointsText;
    [SerializeField] private ScrollRect m_SkillScrollRect;

    private Dictionary<int, SkillUIItem> m_SkillItems = new Dictionary<int, SkillUIItem>();
    private SkillManager m_SkillManager;
    private SkillDataModel m_SkillDataModel;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        m_SkillManager = FindObjectOfType<SkillManager>();
        m_SkillDataModel = GF.DataModel.GetOrCreate<SkillDataModel>();
        
        // 绑定事件
        GF.Event.Subscribe(SkillDataChangedEventArgs.EventId, OnSkillDataChanged);
        GF.Event.Subscribe(SkillCastEventArgs.EventId, OnSkillCast);
        GF.Event.Subscribe(BuffAddedEventArgs.EventId, OnBuffAdded);
        GF.Event.Subscribe(BuffRemovedEventArgs.EventId, OnBuffRemoved);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        // 初始化技能列表
        InitializeSkillList();
        
        // 更新技能点数显示
        UpdateSkillPointsDisplay();
        
        // 绑定关闭按钮
        if (m_CloseButton != null)
        {
            m_CloseButton.onClick.AddListener(OnCloseButtonClick);
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 解绑事件
        GF.Event.Unsubscribe(SkillDataChangedEventArgs.EventId, OnSkillDataChanged);
        GF.Event.Unsubscribe(SkillCastEventArgs.EventId, OnSkillCast);
        GF.Event.Unsubscribe(BuffAddedEventArgs.EventId, OnBuffAdded);
        GF.Event.Unsubscribe(BuffRemovedEventArgs.EventId, OnBuffRemoved);
        
        // 解绑按钮
        if (m_CloseButton != null)
        {
            m_CloseButton.onClick.RemoveListener(OnCloseButtonClick);
        }
        
        base.OnClose(isShutdown, userData);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        
        // 更新技能冷却显示
        UpdateSkillCooldowns();
    }

    /// <summary>
    /// 初始化技能列表
    /// </summary>
    private void InitializeSkillList()
    {
        if (m_SkillContainer == null || m_SkillItemPrefab == null) return;

        // 清空现有技能项
        foreach (Transform child in m_SkillContainer)
        {
            DestroyImmediate(child.gameObject);
        }
        m_SkillItems.Clear();

        // 获取所有技能数据
        var skillTable = GF.DataTable.GetDataTable<SkillTable>();
        if (skillTable == null) return;

        foreach (var skillRow in skillTable.GetAllDataRows())
        {
            CreateSkillItem(skillRow);
        }
    }

    /// <summary>
    /// 创建技能项
    /// </summary>
    private void CreateSkillItem(SkillTable skillRow)
    {
        GameObject skillItemObj = Instantiate(m_SkillItemPrefab, m_SkillContainer);
        SkillUIItem skillItem = skillItemObj.GetComponent<SkillUIItem>();
        
        if (skillItem != null)
        {
            skillItem.Initialize(skillRow, m_SkillManager, m_SkillDataModel);
            m_SkillItems[skillRow.Id] = skillItem;
        }
    }

    /// <summary>
    /// 更新技能冷却显示
    /// </summary>
    private void UpdateSkillCooldowns()
    {
        foreach (var skillItem in m_SkillItems.Values)
        {
            skillItem.UpdateCooldown();
        }
    }

    /// <summary>
    /// 更新技能点数显示
    /// </summary>
    private void UpdateSkillPointsDisplay()
    {
        if (m_SkillPointsText != null)
        {
            m_SkillPointsText.text = $"技能点数: {m_SkillDataModel.SkillPoints}";
        }
    }

    /// <summary>
    /// 技能数据变更事件处理
    /// </summary>
    private void OnSkillDataChanged(object sender, GameEventArgs e)
    {
        var args = e as SkillDataChangedEventArgs;
        if (args == null) return;

        switch (args.DataType)
        {
            case SkillDataType.SkillLevel:
                if (m_SkillItems.ContainsKey(args.SkillId))
                {
                    m_SkillItems[args.SkillId].UpdateLevel(args.NewValue);
                }
                break;
            case SkillDataType.SkillUnlocked:
                if (m_SkillItems.ContainsKey(args.SkillId))
                {
                    m_SkillItems[args.SkillId].SetUnlocked(args.NewValue > 0);
                }
                break;
            case SkillDataType.SkillPoints:
                UpdateSkillPointsDisplay();
                break;
        }
    }

    /// <summary>
    /// 技能释放事件处理
    /// </summary>
    private void OnSkillCast(object sender, GameEventArgs e)
    {
        var args = e as SkillCastEventArgs;
        if (args == null) return;

        if (m_SkillItems.ContainsKey(args.SkillId))
        {
            m_SkillItems[args.SkillId].OnSkillCast(args.IsSuccess);
        }
    }

    /// <summary>
    /// Buff添加事件处理
    /// </summary>
    private void OnBuffAdded(object sender, GameEventArgs e)
    {
        var args = e as BuffAddedEventArgs;
        if (args == null) return;

        // 可以在这里添加Buff相关的UI更新逻辑
    }

    /// <summary>
    /// Buff移除事件处理
    /// </summary>
    private void OnBuffRemoved(object sender, GameEventArgs e)
    {
        var args = e as BuffRemovedEventArgs;
        if (args == null) return;

        // 可以在这里添加Buff相关的UI更新逻辑
    }

    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseButtonClick()
    {
        GF.UI.CloseUIForm(this);
    }
}

/// <summary>
/// 技能UI项
/// </summary>
public class SkillUIItem : MonoBehaviour
{
    [SerializeField] private Image m_SkillIcon;
    [SerializeField] private Text m_SkillName;
    [SerializeField] private Text m_SkillLevel;
    [SerializeField] private Text m_SkillDescription;
    [SerializeField] private Button m_SkillButton;
    [SerializeField] private Button m_UpgradeButton;
    [SerializeField] private Image m_CooldownOverlay;
    [SerializeField] private Text m_CooldownText;
    [SerializeField] private GameObject m_LockedOverlay;
    [SerializeField] private Text m_KeyBindingText;

    private SkillTable m_SkillData;
    private SkillManager m_SkillManager;
    private SkillDataModel m_SkillDataModel;
    private SkillData m_PlayerSkillData;

    public void Initialize(SkillTable skillData, SkillManager skillManager, SkillDataModel skillDataModel)
    {
        m_SkillData = skillData;
        m_SkillManager = skillManager;
        m_SkillDataModel = skillDataModel;
        m_PlayerSkillData = skillDataModel.GetSkillData(skillData.Id);

        // 设置技能信息
        if (m_SkillIcon != null)
        {
            // 加载技能图标
            GF.Resource.LoadAsset(skillData.SkillIcon, OnSkillIconLoaded);
        }

        if (m_SkillName != null)
        {
            m_SkillName.text = skillData.SkillName;
        }

        if (m_SkillDescription != null)
        {
            m_SkillDescription.text = skillData.SkillDescription;
        }

        // 设置按钮事件
        if (m_SkillButton != null)
        {
            m_SkillButton.onClick.AddListener(OnSkillButtonClick);
        }

        if (m_UpgradeButton != null)
        {
            m_UpgradeButton.onClick.AddListener(OnUpgradeButtonClick);
        }

        // 设置快捷键显示
        if (m_KeyBindingText != null)
        {
            m_KeyBindingText.text = GetKeyBindingText(skillData.Id);
        }

        // 更新显示状态
        UpdateDisplay();
    }

    /// <summary>
    /// 技能图标加载完成
    /// </summary>
    private void OnSkillIconLoaded(object asset)
    {
        if (m_SkillIcon != null && asset is Sprite sprite)
        {
            m_SkillIcon.sprite = sprite;
        }
    }

    /// <summary>
    /// 技能按钮点击
    /// </summary>
    private void OnSkillButtonClick()
    {
        if (m_SkillManager == null || m_SkillData == null) return;

        if (m_SkillData.SkillType == SkillType.Active)
        {
            // 释放主动技能
            Vector3 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPos.z = 0;
            m_SkillManager.CastSkill(m_SkillData.Id, targetPos);
        }
    }

    /// <summary>
    /// 升级按钮点击
    /// </summary>
    private void OnUpgradeButtonClick()
    {
        if (m_SkillDataModel == null || m_SkillData == null) return;

        if (m_SkillDataModel.CanUpgradeSkill(m_SkillData.Id))
        {
            m_SkillDataModel.UpgradeSkill(m_SkillData.Id);
        }
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    public void UpdateDisplay()
    {
        if (m_SkillData == null || m_PlayerSkillData == null) return;

        // 更新等级显示
        if (m_SkillLevel != null)
        {
            m_SkillLevel.text = $"Lv.{m_PlayerSkillData.Level}";
        }

        // 更新解锁状态
        bool isUnlocked = m_PlayerSkillData.IsUnlocked;
        if (m_LockedOverlay != null)
        {
            m_LockedOverlay.SetActive(!isUnlocked);
        }

        // 更新升级按钮状态
        if (m_UpgradeButton != null)
        {
            bool canUpgrade = m_SkillDataModel.CanUpgradeSkill(m_SkillData.Id);
            m_UpgradeButton.gameObject.SetActive(isUnlocked && canUpgrade);
        }

        // 更新技能按钮状态
        if (m_SkillButton != null)
        {
            bool canCast = m_SkillManager.CanCastSkill(m_SkillData.Id);
            m_SkillButton.interactable = isUnlocked && canCast;
        }
    }

    /// <summary>
    /// 更新冷却显示
    /// </summary>
    public void UpdateCooldown()
    {
        if (m_SkillManager == null || m_SkillData == null) return;

        float cooldownRemaining = m_SkillManager.GetSkillCooldownRemaining(m_SkillData.Id);
        float cooldownProgress = m_SkillManager.GetSkillCooldownProgress(m_SkillData.Id);

        // 更新冷却遮罩
        if (m_CooldownOverlay != null)
        {
            m_CooldownOverlay.fillAmount = cooldownProgress;
            m_CooldownOverlay.gameObject.SetActive(cooldownRemaining > 0);
        }

        // 更新冷却文本
        if (m_CooldownText != null)
        {
            if (cooldownRemaining > 0)
            {
                m_CooldownText.text = cooldownRemaining.ToString("F1");
                m_CooldownText.gameObject.SetActive(true);
            }
            else
            {
                m_CooldownText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 更新等级
    /// </summary>
    public void UpdateLevel(int newLevel)
    {
        if (m_SkillLevel != null)
        {
            m_SkillLevel.text = $"Lv.{newLevel}";
        }
        UpdateDisplay();
    }

    /// <summary>
    /// 设置解锁状态
    /// </summary>
    public void SetUnlocked(bool unlocked)
    {
        if (m_LockedOverlay != null)
        {
            m_LockedOverlay.SetActive(!unlocked);
        }
        UpdateDisplay();
    }

    /// <summary>
    /// 技能释放回调
    /// </summary>
    public void OnSkillCast(bool success)
    {
        // 可以在这里添加技能释放的视觉反馈
        if (success)
        {
            // 播放成功特效
        }
        else
        {
            // 播放失败特效
        }
    }

    /// <summary>
    /// 获取快捷键文本
    /// </summary>
    private string GetKeyBindingText(int skillId)
    {
        // 根据技能ID返回对应的快捷键
        switch (skillId)
        {
            case 1: return "Q";
            case 2: return "W";
            case 3: return "E";
            case 4: return "R";
            default: return "";
        }
    }
}