using UnityEngine;
using TD.Config;
using TD.Common;
using TD.UI;
using TD.Assets;
using TMPro;

namespace TD.Core
{
    /// <summary>
    /// 游戏启动器：统一初始化服务容器与核心服务。
    /// 场景中唯一挂载，负责依赖装配与生命周期管理。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class Bootstrapper : MonoBehaviour
    {
        #region Fields
        // 最近一次读取的必要资源配置（用于后续 UI 注入等）
        private MustAssetsConfig _lastMustAssetsCfg;
        #endregion

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
            // 同步注册核心服务（避免重复注册）
            RegisterCoreServices();
        }

        /// <summary>
        /// 供外部（如 GameController）调用的初始化入口：执行服务初始化与必要预热，并通过事件反馈进度/就绪。
        /// </summary>
        public async System.Threading.Tasks.Task RunInitializationAsync()
        {
            try
            {
                ReportProgress(0f);

                // 1) 必要资源预热（仅 Resources）
                await PrewarmMustHaveAssetsAsync();
                ReportProgress(0.5f);

                // 2) 服务异步初始化（例如 UI 资源服务等）
                await InitializeServicesAsync();
                ReportProgress(0.9f);

                // 3) 注册生命周期（IInitializable/IUpdatable 等）
                InitializeAndRegisterLifecycle();
                ReportProgress(1f);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Service initialization failed: {ex.Message}");
                throw;
            }

            // 广播服务就绪
            try { ServicesReady?.Invoke(); } catch { }
        }

        #region Initialization
        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            try
            {
                // 仅初始化需要异步准备的服务
                if (ServiceContainer.Instance.TryGet<IUIResourceService>(out var uiResourceService))
                {
                    await uiResourceService.InitializeAsync();

                    // 从配置注入默认字体（仅 Resources）
                    if (uiResourceService.GetDefaultFont() == null && _lastMustAssetsCfg?.uiInject != null)
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
                                uiResourceService.SetDefaultFont(font);
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

        private async System.Threading.Tasks.Task PrewarmMustHaveAssetsAsync()
        {
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

            // 预热：顺序加载 Resources 预制体（实例化→销毁以触发依赖加载）
            if (cfg.resourcesPrefabs != null)
            {
                foreach (var path in cfg.resourcesPrefabs)
                {
                    if (string.IsNullOrEmpty(path)) continue;
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
                    catch { /* ignored */ }
                }
            }
        }

        private void InitializeAndRegisterLifecycle()
        {
            var container = ServiceContainer.Instance;
            foreach (var service in container.GetAllServices())
            {
                if (service is IInitializable initializable) initializable.Initialize();
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
        }

        #endregion

        #region Service Registration
        private void RegisterCoreServices()
        {
            try
            {
                var container = ServiceContainer.Instance;

                // JsonLoader / ConfigService
                if (!container.IsRegistered<IJsonLoader>())
                {
                    container.Register<IJsonLoader>(new StreamingAssetsJsonLoader());
                }
                if (!container.IsRegistered<IConfigService>())
                {
                    var jsonLoader = container.Get<IJsonLoader>();
                    container.Register<IConfigService>(new ConfigService(jsonLoader));
                }

                // Object Pool
                if (!container.IsRegistered<PoolService>())
                {
                    container.Register<PoolService>(new PoolService());
                }

                // Stats / Runes
                if (!container.IsRegistered<StatService>())
                {
                    container.Register<StatService>(new StatService());
                }
                if (!container.IsRegistered<RunesService>())
                {
                    container.Register<RunesService>(new RunesService());
                }

                // UI Resource Service
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

                // UI Manager / Asset Provider
                if (!container.IsRegistered<IUIManager>())
                {
                    container.Register<IUIManager>(new UIManager());
                }
                if (!container.IsRegistered<IAssetProvider>())
                {
                    container.Register<IAssetProvider>(new ResourcesAssetProvider());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] RegisterCoreServices failed: {ex.Message}");
            }
        }
        #endregion
    }
}
