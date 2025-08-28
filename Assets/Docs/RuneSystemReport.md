# 塔防游戏符文系统实现报告

## 概述
成功实现了完整的符文系统，包括数据驱动的符文定义、回合结束选择机制、全局加成应用和敌人减速效果。

## 已实现的功能

### 1. 核心架构
- **服务注册**: 在 `Bootstrapper` 中注册了 `StatService` 和 `RunesService`
- **事件系统**: 实现了 `GameEvents` 用于回合结束、奖励发放和敌人生成的事件广播
- **数据模型**: 扩展了 `JsonModels.cs` 支持符文相关配置

### 2. 符文数据系统
- **独立符文JSON**: 创建了9个符文定义文件，分布在 Common/Rare/Epic 三个稀有度
- **关卡配置**: `level_001.json` 配置了符文池、稀有度策略和随机种子
- **自动加载**: `RunesService` 在关卡开始时自动加载符文定义

### 3. 选择机制
- **回合触发**: 每回合结束后根据配置决定是否提供符文选择
- **稀有度控制**: 同回合内提供相同稀有度的符文，支持自动降级策略
- **随机种子**: 支持固定种子保证可重现的符文序列
- **不足处理**: 当候选不足3个时可配置跳过或降级

### 4. 效果应用
- **塔效果**: 通过 `StatService` 全局聚合射程和伤害加成
- **敌人效果**: 直接修改敌人移动速度，确保不低于初始速度的20%
- **实时更新**: 塔的射程圈和子弹伤害即时反映全局加成

### 5. 用户界面
- **符文选择UI**: 自动创建的选择界面，支持稀有度颜色区分
- **状态显示**: 游戏状态UI显示生命、金钱、回合和剩余敌人数
- **交互反馈**: 按钮悬停效果和清晰的文本显示

## 技术特点

### 数据驱动
- 符文效果完全通过JSON配置，无需修改代码即可调整数值
- 关卡可独立配置符文池和选择策略
- 支持多种效果类型和运算方式（加法、乘法）

### 性能优化
- 使用对象池管理敌人和子弹
- 符文效果聚合减少重复计算
- UI按需创建和销毁

### 架构清晰
- 服务层分离关注点（StatService处理加成，RunesService处理选择逻辑）
- 事件驱动的松耦合设计
- 统一的生命周期管理

## 配置示例

### 符文定义 (r_tower_range_x1_1.json)
```json
{
  "id": "r_tower_range_x1_1",
  "name": "Range +10%",
  "rarity": "Common",
  "description": "+10% tower range (global)",
  "effects": [
    { "target": "Tower", "attribute": "range", "operation": "mult", "value": 1.1, "applyMode": "global" }
  ]
}
```

### 关卡符文配置 (level_001.json)
```json
{
  "runes": {
    "pauseOnSelection": true,
    "useRandomSeed": true,
    "randomSeed": 123456,
    "defaultRarity": "Common",
    "autoDowngradeRarity": true,
    "skipIfInsufficient": true,
    "poolIds": ["r_tower_range_x1_1", "r_tower_damage_x1_2", ...]
  }
}
```

## 使用说明

1. **运行游戏**: 确保场景中有 `Bootstrapper` 组件
2. **符文选择**: 每回合结束后会自动弹出符文选择界面（如果配置了 `offerRunes: true`）
3. **效果验证**: 
   - 塔效果：射程圈大小变化，子弹伤害提升
   - 敌人效果：移动速度变慢但不低于初始速度的20%

## 扩展性

### 新符文类型
- 在 `StreamingAssets/TD/runes/` 目录添加新的JSON文件
- 在关卡配置中添加到 `poolIds` 列表
- 支持的效果类型可通过修改 `RunesService.ApplyRune` 方法扩展

### 新稀有度
- 在符文JSON中定义新的 `rarity` 值
- 在UI的 `_rarityColors` 字典中添加对应颜色
- 调整 `RunesService.GetOffersForRound` 中的稀有度层级

### 新效果目标
- 扩展 `RuneEffectDef.target` 支持更多目标类型
- 在 `RunesService.ApplyRune` 中添加对应的处理逻辑

## 已知限制

1. **UI样式**: 当前使用程序生成的简单UI，可替换为预制体获得更好视觉效果
2. **效果持续性**: 符文效果仅在当前关卡内生效，关卡切换时会重置
3. **保存系统**: 未实现符文选择的存档功能
4. **动画效果**: UI显示和效果应用缺乏过渡动画

## 总结

符文系统已完整实现并可正常运行，提供了灵活的配置方式和清晰的代码结构。系统支持数据驱动的设计理念，便于后续扩展和调整。通过事件系统实现了各模块间的松耦合，保证了代码的可维护性。
