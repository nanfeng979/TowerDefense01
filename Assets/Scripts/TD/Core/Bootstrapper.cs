using UnityEngine;
using System.Collections.Generic;
using TD.Config;
using TD.Common;
using TD.UI;
using TD.Assets;

namespace TD.Core
{
    /// <summary>
    /// 游戏启动器：统一初始化服务容器、UpdateDriver 与核心服务。
    /// 场景中唯一挂载，负责依赖装配与生命周期管理。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class Bootstrapper : MonoBehaviour
    {
        [System.Serializable]
        private class MustAssetsConfig
        {
            public List<string> resourcesPrefabs = new List<string>();
            public List<string> addressableAssets = new List<string>();
            public List<string> uiFonts = new List<string>();
        }

        [Header("Initialization")]
        public bool initializeOnStart = true;

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

                // 如需符文选择界面，请在场景中添加 TD.UI.RuneSelectionUI 组件

                // 初始化与注册生命周期接口
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
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Awake registration failed: {ex.Message}");
            }
        }

        private async void Start()
        {
            if (!initializeOnStart) return;

            // 在所有服务初始化完成前，短暂暂停游戏推进
            float prevScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                ReportProgress(0f);
                await InitializeServicesAsync();
                ReportProgress(0.1f);
                // 必备资源的预热（当前阶段：仅做最小 2s 的进度演示与管线打通）
                await PrewarmMustHaveAssetsAsync(minDurationSeconds: 2f);
                ReportProgress(1f);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Service initialization failed: {ex.Message}");
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
                if (ServiceContainer.Instance.TryGet<TD.UI.UIResourceService>(out var uiResObj))
                {
                    await uiResObj.InitializeAsync();
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

            // 预热逻辑：并行加载所有声明的资源，并实时汇总进度
            var totalItems = 0;
            if (cfg.resourcesPrefabs != null) totalItems += cfg.resourcesPrefabs.Count;
            if (cfg.addressableAssets != null) totalItems += cfg.addressableAssets.Count;
            if (totalItems <= 0) totalItems = 1; // 防止除零

            int completed = 0;
            System.Func<System.Threading.Tasks.Task> report = async () =>
            {
                // 进度区间：服务初始化后从 0.1 → 1.0
                float baseProgress = 0.1f + 0.9f * (completed / (float)totalItems);
                // 同时考虑最小时长：取两者较小值以避免瞬间结束
                float tTime = Mathf.Clamp01((Time.realtimeSinceStartup - start) / minDur);
                float visual = Mathf.Min(baseProgress, Mathf.Lerp(0.1f, 1f, tTime));
                ReportProgress(visual);
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

#if ENABLE_ADDRESSABLES
            // Addressables 资源加载（仅加载到内存，不实例化）
            if (cfg.addressableAssets != null)
            {
                foreach (var addr in cfg.addressableAssets)
                {
                    if (string.IsNullOrEmpty(addr)) { completed++; continue; }
                    try
                    {
                        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Object>(addr);
                        await handle.Task;
                        // 不在此释放，保持缓存；若想释放可在此调用 Addressables.Release(handle);
                    }
                    catch { }
                    finally
                    {
                        completed++;
                        await report();
                    }
                }
            }
#endif

            // 保证最小时间
            while ((Time.realtimeSinceStartup - start) < minDur)
            {
                await report();
                await System.Threading.Tasks.Task.Delay(33);
            }
            ReportProgress(1f);
        }

        private static void ReportProgress(float value)
        {
            try { InitializationProgress?.Invoke(Mathf.Clamp01(value)); } catch { }
        }

        private static void DebugLogError(string msg)
        {
            Debug.LogError(msg);
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
