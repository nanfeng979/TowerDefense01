# TD.Core.Bootstrapper

统一的游戏启动器，负责注册核心服务、执行一次性的启动初始化，并在完成后广播就绪事件。采用“无反射、显式注册”的方式，便于追踪与调试。

## 角色与职责
- 唯一挂在场景中的启动入口（单场景架构）。
- 注册与持有核心服务（配置、对象池、UI 管理与资源服务等）。
- 启动时执行“必要资源预热 + 服务初始化 + 生命周期初始化”。
- 广播初始化进度与“服务就绪”事件，供 Loading/UI 使用。
- 退出时统一释放并清理服务容器。

## 生命周期概览
1) Awake
- 调用 RegisterCoreServices 显式注册核心服务：
	- IJsonLoader → StreamingAssetsJsonLoader
	- IConfigService → ConfigService
	- PoolService、StatService、RunesService
	- UIResourceService（并按 IUIResourceService 接口注册）
	- IUIManager、IAssetProvider（Resources 实现）

2) RunInitializationAsync（对外异步初始化入口）
- ReportProgress(0f)
- PrewarmMustHaveAssetsAsync()：读取 `StreamingAssets/TD/init/must_assets.json`，对 `resourcesPrefabs` 内的每个路径执行 Resources.Load + 一次实例化/销毁，以触发依赖加载。
- ReportProgress(0.5f)
- InitializeServicesAsync()：
	- 调用各需要的 InitializeAsync（当前仅 UI 资源服务）。
	- 若 `uiInject.defaultFont` 配置存在且默认字体未设置：先用 Addressables 加载，失败则回退 Resources 加载，成功后设为默认字体。
- ReportProgress(0.9f)
- InitializeAndRegisterLifecycle()：对已注册服务调用 IInitializable.Initialize（当前不进行 UpdateDriver 的注册）。
- ReportProgress(1f)
- 触发 ServicesReady 事件。

3) OnDestroy
- 调用服务的 IDisposableEx.Dispose 并清空 ServiceContainer。

## 事件
- InitializationProgress(float 0..1)：粗粒度进度，阶段性汇报（0 / 0.5 / 0.9 / 1）。
- ServicesReady()：初始化完整结束。

## 配置文件：must_assets.json（位于 StreamingAssets/TD/init）
字段只保留与启动相关的关键项：
```json
{
	"resourcesPrefabs": [
		"UI/Panels/MainMenu",
		"Levels/LevelMain"
	],
	"uiInject": {
		"defaultFont": "Fonts/MyTMPFont" // 可为 Addressables 键；若非 Addressables 则按 Resources 路径尝试
	}
}
```

说明：
- resourcesPrefabs：相对 Resources 的路径（不带 .prefab）。预热仅做“加载→瞬时实例化→销毁”以触发依赖。
- uiInject.defaultFont：优先 Addressables，再回退 Resources。

## 用法
- 将 `Bootstrapper` 作为场景唯一启动脚本放入场景根。
- 游戏入口（例如 GameController）调用：`await Bootstrapper.RunInitializationAsync()`。
- UI Loading 可订阅 `Bootstrapper.InitializationProgress` 与 `Bootstrapper.ServicesReady`。

## 设计约束与风格
- 不使用反射：所有服务以接口/具体类型显式注册，便于 IDE 跳转与维护。
- 仅在必要时做异步初始化，保持启动阶段轻量。
- 字体注入在 `InitializeServicesAsync` 内进行，避免散落在各 UI 代码里。

## 变更摘要（当前版本）
- 去除“最小时长”门槛与复杂时间映射，进度改为阶段性汇报。
- 预热仅使用 Resources，逐项实例化/销毁触发依赖加载。
- 默认字体注入：Addressables 优先，Resources 兜底。
- 生命周期初始化当前仅调用 IInitializable.Initialize。
- OnDestroy 统一释放并清理服务容器。

## 相关
- 依赖接口：`TD.Config.IConfigService`、`TD.Config.IJsonLoader`。
- 关卡校验/可视化类的编辑器校验逻辑移至 LevelVisualizer.Inspector。
