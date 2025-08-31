using UnityEngine;
using System.Collections.Generic;
using TD.Config;
using TD.Common;
using TD.UI;
using TD.Assets;
using TMPro;

namespace TD.Core
{
    /// <summary>
    /// 游戏启动器：统一初始化服务容器、UpdateDriver 与核心服务。
    /// 场景中唯一挂载，负责依赖装配与生命周期管理。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class Bootstrapper : MonoBehaviour
    {

        // 缓存最近一次读取的必要资源配置，供后续初始化阶段使用（如设置默认字体）
        private MustAssetsConfig _lastMustAssetsCfg;

        /// <summary>
        /// 初始化进度（0..1）。用于驱动场景内 Loading 进度条。
        /// </summary>
        public static event System.Action<float> InitializationProgress;

        /// <summary>
        /// 当所有服务完成初始化与必要预热（若有）后触发。
        /// </summary>
        public static event System.Action ServicesReady;

        private void Awake()
        {
            // 确保 UpdateDriver 存在
            if (UpdateDriver.Instance == null)
            {
                var driverGO = new GameObject("[UpdateDriver]");
                driverGO.AddComponent<UpdateDriver>();
            }

            // 同步注册核心服务，确保在任何 Start 之前可用
            try
            {
                var container = ServiceContainer.Instance;
                var driver = UpdateDriver.Instance;

                // 若已注册则跳过，避免重复（例如在手动调用初始化的情况下）
                if (!container.IsRegistered<IJsonLoader>())
                {
                    IJsonLoader jsonLoader = new StreamingAssetsJsonLoader();
                    container.Register<IJsonLoader>(jsonLoader);
                }
                if (!container.IsRegistered<IConfigService>())
                {
                    var jsonLoader = container.Get<IJsonLoader>();
                    IConfigService configService = new ConfigService(jsonLoader);
                    container.Register<IConfigService>(configService);
                }
                if (!container.IsRegistered<PoolService>())
                {
                    PoolService poolService = new PoolService();
                    container.Register<PoolService>(poolService);
                }
                // 注册 StatService / RunesService（符文系统）
                if (!container.IsRegistered<StatService>())
                {
                    var statService = new StatService();
                    container.Register<StatService>(statService);
                }
                if (!container.IsRegistered<RunesService>())
                {
                    var runesService = new RunesService();
                    container.Register<RunesService>(runesService);
                }

                // 全局 UI 资源服务：字体集中管理（通过 Assets 路径加载）
                // 直接注册 UI 资源服务（同时按接口与具体类型注册，便于解耦）
                if (!container.IsRegistered<TD.UI.UIResourceService>())
                {
                    var jsonLoader = container.Get<IJsonLoader>();
                    var uiRes = new TD.UI.UIResourceService(jsonLoader);
                    container.Register<TD.UI.UIResourceService>(uiRes);
                    if (!container.IsRegistered<IUIResourceService>())
                    {
                        container.Register<IUIResourceService>(uiRes);
                    }
                }

                // UI 管理器与资源提供者
                if (!container.IsRegistered<IUIManager>())
                {
                    container.Register<IUIManager>(new UIManager());
                }
                if (!container.IsRegistered<IAssetProvider>())
                {
                    container.Register<IAssetProvider>(new ResourcesAssetProvider());
                }

#if TD_LOCALIZATION
                // Localization service 注册（通过编译符号 TD_LOCALIZATION 启用）
                if (!container.IsRegistered<ILocalizationService>())
                {
                    var loc = new LocalizationService();
                    container.Register<ILocalizationService>(loc);
                }
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Awake registration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 供外部（如 GameController）调用的初始化入口：执行服务初始化与必要预热，并通过事件反馈进度/就绪。
        /// </summary>
        public async System.Threading.Tasks.Task RunInitializationAsync()
        {
            // 在所有服务初始化完成前，短暂暂停游戏推进
            float prevScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                ReportProgress(0f);
                // 1) 必要资源加载（统一在 Bootstrapper 内处理）→ 映射 0..0.8
                await PrewarmMustHaveAssetsAsync(minDurationSeconds: 2f);
                ReportProgress(0.8f);

                // 2) 异步初始化需要准备的服务（读取配置/预加载字体等）→ 0.9
                await InitializeServicesAsync();
                ReportProgress(0.9f);

                // 3) 初始化与注册生命周期接口（IInitializable/UpdateDriver）→ 1.0
                InitializeAndRegisterLifecycle();
                ReportProgress(1f);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Service initialization failed: {ex.Message}");
                throw;
            }
            finally
            {
                Time.timeScale = prevScale;
            }

            // 广播服务就绪，供 GameController/Loading 关闭加载界面
            try { ServicesReady?.Invoke(); } catch { }
        }

        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            try
            {
                // 仅初始化需要异步准备的服务，尽量保持启动阶段轻量，不在此处加载具体资源
                if (ServiceContainer.Instance.TryGet<IUIResourceService>(out var uiResObj))
                {
                    await uiResObj.InitializeAsync();

                    // 如配置了 uiInject.defaultFont，尝试设置一个默认字体（优先 Addressables，其次 Resources）
                    if (uiResObj.GetDefaultFont() == null && _lastMustAssetsCfg != null && _lastMustAssetsCfg.uiInject != null)
                    {
                        var fontPath = _lastMustAssetsCfg.uiInject.defaultFont;
                        if (!string.IsNullOrEmpty(fontPath))
                        {
                            TMP_FontAsset font = null;
                            // Addressables 优先
                            try
                            {
                                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<TMP_FontAsset>(fontPath);
                                await handle.Task;
                                font = handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded ? handle.Result : null;
                            }
                            catch { /* ignore */ }

                            // 如果 Addressables 未取到，尝试 Resources
                            if (font == null)
                            {
                                try { font = Resources.Load<TMP_FontAsset>(fontPath); } catch { /* ignore */ }
                            }

                            if (font != null)
                            {
                                uiResObj.SetDefaultFont(font);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] InitializeServicesAsync failed: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task PrewarmMustHaveAssetsAsync(float minDurationSeconds)
        {
            float start = Time.realtimeSinceStartup;
            float minDur = Mathf.Max(0.01f, minDurationSeconds);

            // 读取必须资源配置
            MustAssetsConfig cfg = null;
            try
            {
                var jsonLoader = ServiceContainer.Instance.Get<IJsonLoader>();
                cfg = await jsonLoader.LoadAsync<MustAssetsConfig>("init/must_assets.json");
            }
            catch { /* ignored */ }
            if (cfg == null) cfg = new MustAssetsConfig();

            // 缓存配置供后续初始化阶段使用
            _lastMustAssetsCfg = cfg;

            // 预热逻辑：顺序加载所有声明的资源，并实时汇总进度（主线程）
            var totalItems = 0;
            if (cfg.resourcesPrefabs != null) totalItems += cfg.resourcesPrefabs.Count;
            if (totalItems <= 0) totalItems = 1; // 防止除零

            int completed = 0;
            System.Func<System.Threading.Tasks.Task> report = async () =>
            {
                // 阶段内进度：0..1（按完成度与最小时长取较小）
                float done = completed / (float)totalItems;
                float tTime = Mathf.Clamp01((Time.realtimeSinceStartup - start) / minDur);
                float stage = Mathf.Min(done, tTime);
                // 映射到总进度 0..0.8
                ReportProgress(0.8f * stage);
                await System.Threading.Tasks.Task.Yield();
            };

            // Resources 预制体加载（仅实例化一次以触发依赖加载，随后立即销毁）— 主线程顺序执行，避免跨线程 Unity API 调用
            if (cfg.resourcesPrefabs != null)
            {
                foreach (var path in cfg.resourcesPrefabs)
                {
                    if (string.IsNullOrEmpty(path)) { completed++; await report(); continue; }
                    try
                    {
                        var prefab = Resources.Load<GameObject>(path);
                        if (prefab != null)
                        {
                            var temp = Object.Instantiate(prefab);
                            await System.Threading.Tasks.Task.Yield();
                            Object.Destroy(temp);
                        }
                    }
                    catch { }
                    finally
                    {
                        completed++;
                        await report();
                    }
                }
            }

        }

        private void InitializeAndRegisterLifecycle()
        {
            var container = ServiceContainer.Instance;
            var driver = UpdateDriver.Instance;
            foreach (var service in container.GetAllServices())
            {
                if (service is IInitializable init)
                {
                    init.Initialize();
                }
                if (service is IUpdatable u) driver.RegisterUpdatable(u);
                if (service is ILateUpdatable lu) driver.RegisterLateUpdatable(lu);
                if (service is IFixedUpdatable fu) driver.RegisterFixedUpdatable(fu);
            }
        }

        private static void ReportProgress(float value)
        {
            try { InitializationProgress?.Invoke(Mathf.Clamp01(value)); } catch { }
        }

        private void OnDestroy()
        {
            // 清理所有服务
            foreach (var service in ServiceContainer.Instance.GetAllServices())
            {
                if (service is IDisposableEx disposable)
                {
                    disposable.Dispose();
                }
            }
            ServiceContainer.Instance.Clear();
            if (UpdateDriver.Instance != null)
            {
                UpdateDriver.Instance.ClearAll();
            }
        }
    }
}
