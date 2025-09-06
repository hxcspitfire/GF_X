# 三重攻击技能实现说明

## 技能描述
**三重攻击**：连续攻击敌人3次，每次攻击伤害递增10%，完成后触发5秒吸血效果，攻击时恢复20%伤害的生命值。

## 实现组件

### 1. 核心组件
- **TripleAttackEffect**: 三重攻击技能效果实现
- **LifestealBuffEffect**: 吸血Buff效果实现
- **LifestealBuffManager**: 吸血Buff管理器
- **TripleAttackSkillManager**: 三重攻击技能管理器

### 2. 数据配置
- **技能表**: 配置三重攻击技能基础属性
- **技能等级表**: 配置技能升级属性
- **Buff表**: 配置吸血效果属性

## 技能流程

### 1. 技能释放流程
```
玩家按下Q键 → 检查技能可用性 → 消耗魔法值 → 设置冷却时间 → 执行三重攻击效果
```

### 2. 三重攻击效果流程
```
获取最近敌人 → 执行第1次攻击 → 等待0.3秒 → 执行第2次攻击 → 等待0.3秒 → 执行第3次攻击 → 触发吸血Buff
```

### 3. 吸血效果流程
```
添加吸血Buff → 监听攻击事件 → 计算吸血量 → 恢复生命值 → 播放特效 → 显示治疗数字
```

## 使用方法

### 1. 初始化系统
```csharp
// 在游戏开始时初始化
var tripleAttackManager = gameObject.AddComponent<TripleAttackSkillManager>();
var lifestealBuffManager = gameObject.AddComponent<LifestealBuffManager>();
```

### 2. 释放技能
```csharp
// 释放三重攻击技能
Vector3 targetPos = GetMouseWorldPosition();
bool success = await tripleAttackManager.CastTripleAttack(targetPos);
```

### 3. 升级技能
```csharp
// 升级三重攻击技能
if (tripleAttackManager.CanUpgradeTripleAttack())
{
    tripleAttackManager.UpgradeTripleAttack();
}
```

## 配置参数

### 1. 技能配置
- **技能ID**: 101
- **基础伤害**: 35
- **攻击范围**: 3.0
- **冷却时间**: 8.0秒
- **魔法消耗**: 40
- **最大等级**: 10

### 2. 吸血Buff配置
- **Buff ID**: 3001
- **吸血比例**: 20%
- **持续时间**: 5秒
- **最大叠加**: 3层
- **优先级**: 15

### 3. 攻击配置
- **攻击次数**: 3次
- **攻击间隔**: 0.3秒
- **伤害递增**: 每次增加10%
- **最小吸血**: 1点
- **最大吸血**: 50点

## 特效和音效

### 1. 攻击特效
- **攻击特效**: Effect_TripleAttack
- **攻击音效**: Sound_TripleAttack
- **攻击动画**: TripleAttack_1, TripleAttack_2, TripleAttack_3

### 2. 吸血特效
- **吸血特效**: Effect_Lifesteal
- **吸血音效**: Sound_Lifesteal
- **治疗数字**: 显示恢复的生命值

## 测试方法

### 1. 运行测试
1. 将 `TripleAttackTest` 脚本添加到场景中的GameObject
2. 运行游戏
3. 按Q键释放三重攻击技能
4. 按U键升级技能
5. 按R键重置冷却

### 2. 测试验证
- 检查技能是否正确释放
- 验证3次攻击是否按顺序执行
- 确认吸血效果是否正确触发
- 测试技能升级是否正常工作

## 扩展功能

### 1. 自定义攻击次数
```csharp
// 修改攻击次数
tripleAttackEffect.m_AttackCount = 5; // 改为5次攻击
```

### 2. 自定义吸血比例
```csharp
// 修改吸血比例
lifestealEffect.m_LifestealRate = 0.3f; // 改为30%吸血
```

### 3. 添加特殊效果
```csharp
// 在攻击时添加特殊效果
if (attackNumber == 3)
{
    // 第3次攻击的特殊效果
    PlaySpecialEffect();
}
```

## 注意事项

1. **敌人检测**: 确保场景中有敌人实体，且敌人有正确的标签
2. **特效资源**: 确保特效预制体和音效资源已正确配置
3. **事件系统**: 确保GameFramework的事件系统正常工作
4. **数据表**: 确保技能和Buff数据表已正确加载
5. **性能优化**: 大量敌人时注意性能影响

## 故障排除

### 1. 技能无法释放
- 检查魔法值是否足够
- 确认技能是否在冷却中
- 验证技能是否已解锁

### 2. 吸血效果不触发
- 检查LifestealBuffManager是否正确初始化
- 确认Buff ID是否正确
- 验证事件系统是否正常工作

### 3. 特效不显示
- 检查特效预制体是否正确配置
- 确认特效资源是否已加载
- 验证特效播放代码是否正确调用