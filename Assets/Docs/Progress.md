# Progress 日志

用于记录每天的工作进展与下一步计划。

## 2025-08-21
- 建立 StreamingAssets/TD 示例 JSON：elements/towers/enemies/levels/level_001.json
- 编写 JSON 规格草案并收敛：多路径、建造点类型仅 ground、每波奖励、RGBA gizmoColor
- 创建 CodingStandards.md 编码规范初版

下一步：
- 定义并实现最小本地加载器（StreamingAssetsJsonLoader）
- 实现 ConfigService（缓存 + 聚合访问）
- 场景校验器：Bootstrapper 挂载并输出加载/交叉校验日志
