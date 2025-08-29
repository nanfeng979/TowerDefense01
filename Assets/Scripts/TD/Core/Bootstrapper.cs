using UnityEngine;
using TD.Config;
using TD.Common;
using TD.UI;

namespace TD.Core
{
    /// <summary>
    /// 游戏启动器：统一初始化服务容器、UpdateDriver 与核心服务。
    /// 场景中唯一挂载，负责依赖装配与生命周期管理。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class Bootstrapper : MonoBehaviour
    {
        [Header("Initialization")]
        public bool initializeOnStart = true;

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
                await InitializeServicesAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Service initialization failed: {ex.Message}");
            }
            finally
            {
                Time.timeScale = prevScale;
            }
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
