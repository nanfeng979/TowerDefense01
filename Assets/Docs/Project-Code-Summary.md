# 项目代码总览（TowerDefense01）

本文件汇总项目的核心架构、目录组织、关键脚本与职责、初始化流程、数据/可视化/渲染、对象池与最小玩法循环，便于快速上手与协作。

## 技术栈与约束
- 引擎：Unity 2021.3.1f1c1（Built-in RP, 3D）
- 平台：PC（当前）/ Android（后续）；目标 60 FPS
- 数据：StreamingAssets/TD 下 JSON 驱动，不使用 ScriptableObject
- UI：UGUI（后续）

## 目录结构（关键部分）
- Assets/StreamingAssets/TD/
  - elements.json, towers.json, enemies.json, levels/level_001.json
- Assets/Scripts/TD/
  - Common/
    - Interfaces.cs（生命周期接口）
    - ColorUtil.cs（RGBA 十六进制解析）
    - Pooling/（IPoolable、ObjectPool<T>、GameObjectPool）
  - Config/
    - JsonModels.cs（数据模型）
    - IJsonLoader.cs, StreamingAssetsJsonLoader.cs
    - ConfigServiceInterfaces.cs, ConfigService.cs
  - Core/
    - ServiceContainer.cs（服务定位器）
    - UpdateDriver.cs（集中式 Update/Late/Fixed 驱动）
    - Bootstrapper.cs（启动器，负责装配与预热）
    - LevelVisualizer.cs + Editor/LevelVisualizerInspector.cs（Gizmos 可视化与校验）
    - PoolService.cs（统一 GameObject 池管理）
  - Gameplay/
    - Bullet/（Bullet、BulletShooter）
    - Enemy/（EnemyMover、EnemyAgent、EnemyRegistry）
    - Combat/（IDamageable、Health）
    - Tower/（SimpleTower）
    - Map/（MapRenderer：根据 JSON 渲染地面/路径/建造点）
  
- Assets/Docs/
  - CodingStandards.md，Conversation-2025-08-20.md，Progress.md
  - Specs/（JSON-Schema-Proposals.md、Loader-Design.md）
  - TD.Core.*.md，TD.Common.Pooling.md，Demo-Pooling-Setup.md
  - TD.Gameplay.MapRenderer.md，TD.Gameplay.Combat.md

## 初始化与生命周期
- Bootstrapper（[DefaultExecutionOrder(-10000)]）
  - Awake：确保 UpdateDriver 存在；注册核心服务（IJsonLoader、IConfigService、PoolService），初始化 IInitializable，并自动把 IUpdatable/ILateUpdatable/IFixedUpdatable 注册进 UpdateDriver
  - Start：异步预热 Elements/Towers/Enemies 配置
  - OnDestroy：释放所有 IDisposableEx，并清空 ServiceContainer/UpdateDriver
- ServiceContainer：静态单例，类型安全注册/获取服务；提供 IsRegistered、TryGet、GetAllServices
- UpdateDriver：在一个 MonoBehaviour 中集中驱动各生命周期接口，避免分散 Update 带来的开销与时序复杂度

## 配置数据与可视化
- JsonModels：Elements/Towers/Enemies/Level（含 Grid、Paths、BuildSlots、Waves）
- StreamingAssetsJsonLoader：
  - Editor/PC：直接文件 IO
  - Android：UnityWebRequest 读取 StreamingAssets
- ConfigService：聚合读取与缓存（Elements/Towers/Enemies/Level）
- LevelVisualizer：
  - [ExecuteAlways]，编辑器下懒加载 Level 并用 Gizmos 绘制网格/路径/建造点
  - 自定义 Inspector：Validate Now 执行交叉校验（敌人/路径引用）并汇总

## 地图“渲染”
- MapRenderer：
  - 从 Level JSON 生成地面/路径/建造点的可见物体
  - Ground：无预制→Plane 缩放；有预制→按格子铺设
  - Path：优先沿段步进实例化 pathTilePrefab；无预制→LineRenderer 退化
  - BuildSlot：预制/Primitive 占位；支持一键生成与重复生成前清理

## 对象池与服务
- Pooling（TD.Common.Pooling）：
  - IPoolable：Spawn/Despawn 回调
  - ObjectPool<T>：泛型池（纯 C# 对象）
  - GameObjectPool：Prefab 实例池（预热、回收、复用）
- PoolService（TD.Core）：集中管理多个 GameObject 池；在 Bootstrapper 中注册为服务

## 最小玩法循环（MVP）
- 敌人：
  - EnemyMover：沿 Level.pathId 指定路径点移动
  - Health（IDamageable）：承伤与死亡回调，支持 destroyOnDeath
  - EnemyAgent：注册到 EnemyRegistry，死亡/禁用时自动移除
- 子弹：
  - Bullet：直线飞行；超时回收；OnTriggerEnter 命中 `IDamageable` 调用 Damage 后回收
  - BulletShooter：演示用连续发射，使用 PoolService 获取池并预热
- 塔：
  - SimpleTower：检索射程内最近目标（EnemyRegistry），转向并池化发射子弹

## 场景快速搭建
- 放置 Bootstrapper（确保服务注册与配置预热）
- 地图：
  - 可用 LevelVisualizer 做 Gizmos 校验
  - 或用 MapRenderer 从 JSON 直接生成可见地面/路径/建造点（支持回退）
- 敌人 prefab：添加 Health + EnemyAgent（可叠加 EnemyMover）
- 塔：添加 SimpleTower，配置 bulletPrefab（含 Bullet + Trigger Collider）
- 物理：确保 Layer 矩阵允许子弹与敌人触发

## 关键类速查
- TD.Core
  - ServiceContainer：服务注册/获取/判重
  - UpdateDriver：集中式 Update/Late/Fixed 调度
  - Bootstrapper：启动装配与配置预热
  - PoolService：GameObject 池管理
  - LevelVisualizer：关卡 Gizmos 可视化与校验
- TD.Config
  - ConfigService、StreamingAssetsJsonLoader、JsonModels
- TD.Gameplay
  - Combat：IDamageable、Health
  - Enemy：EnemyMover、EnemyAgent、EnemyRegistry
  - Bullet：Bullet、BulletShooter
  - Tower：SimpleTower
  - Map：MapRenderer

## 设计要点与约定
- 运行时代码尽量通过服务访问（避免硬引用），MonoBehaviour 仅承载视图与桥接
- 所有编辑器专用 API 均使用条件编译保护，保证 Player 可编译
- 兼容 C# 语言级别限制（避免 target-typed new 等 2021 版本不支持语法）
- 数据与行为分离：JSON 描述关卡/单位参数，代码按模型驱动可视化与行为

## 下一步建议
- 塔目标选择策略与 JSON 对齐（first/last/closest/strongest）
- 元素克制计算接入（ElementsConfig → 伤害倍率）
- 敌人数据映射（EnemiesConfig → HP/Speed/Bounty 实例化）
- 波次刷怪系统（Waves 按路径定时生成）
- MapRenderer 美术替换与路径贴花/材质细化，网格装饰
- 命中反馈：飘字/音效/特效（与池化结合）

—— 以上为当前代码的概览与使用说明，详情可参阅各模块下的专用文档（Docs/TD.*.md）。
