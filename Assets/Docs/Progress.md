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
 - Gizmo 可视化：LevelVisualizer 显示网格/路径/建造点，ColorUtil 解析 RGBA Hex
 - 自定义 Inspector：LevelVisualizerInspector 提供一键校验并显示摘要/问题
- 核心框架：ServiceContainer（服务定位器）、UpdateDriver（统一生命周期）、Interfaces（生命周期接口）
- 重写 Bootstrapper：接入统一初始化流程，预热配置，自动注册生命周期服务到 UpdateDriver
 - 对象池：新增 ObjectPool<T>/GameObjectPool 与 PoolService，并接入 Bootstrapper

下一步：
- 验证 Unity 编译与运行
- 考虑增加对象池框架或开始 Gameplay 层实现
