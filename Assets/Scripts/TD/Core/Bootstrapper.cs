using UnityEngine;
using TD.Config;
using TD.Common;

namespace TD.Core
{
    /// <summary>
    /// 游戏启动器：统一初始化服务容器、UpdateDriver 与核心服务。
    /// 场景中唯一挂载，负责依赖装配与生命周期管理。
    /// </summary>
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
        }

        private async void Start()
        {
            if (initializeOnStart)
            {
                await InitializeServices();
            }
        }

        public async System.Threading.Tasks.Task InitializeServices()
        {
            Debug.Log("[Bootstrap] Starting services initialization...");

            try
            {
                var container = ServiceContainer.Instance;
                var driver = UpdateDriver.Instance;

                // 注册核心服务
                IJsonLoader jsonLoader = new StreamingAssetsJsonLoader();
                IConfigService configService = new ConfigService(jsonLoader);

                container.Register<IJsonLoader>(jsonLoader);
                container.Register<IConfigService>(configService);

                // 初始化所有 IInitializable 服务
                foreach (var service in container.GetAllServices())
                {
                    if (service is IInitializable init)
                    {
                        init.Initialize();
                    }
                    // 自动注册生命周期接口到 UpdateDriver
                    if (service is IUpdatable u) driver.RegisterUpdatable(u);
                    if (service is ILateUpdatable lu) driver.RegisterLateUpdatable(lu);
                    if (service is IFixedUpdatable fu) driver.RegisterFixedUpdatable(fu);
                }

                // 预热配置加载（可选）
                var elements = await configService.GetElementsAsync();
                var towers = await configService.GetTowersAsync();
                var enemies = await configService.GetEnemiesAsync();

                Debug.Log($"[Bootstrap] Services initialized. Config loaded: {elements.elements.Count} elements, {towers.towers.Count} towers, {enemies.enemies.Count} enemies");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Initialization failed: {ex.Message}");
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
