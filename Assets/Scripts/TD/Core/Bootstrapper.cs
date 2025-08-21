using UnityEngine;
using TD.Config;
using TD.Common;
using TD.Core;

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
            // 仅做数据预热，避免阻塞 Awake 中的服务注册
            if (!initializeOnStart) return;
            await PrewarmConfigsAsync();
        }

        private async System.Threading.Tasks.Task PrewarmConfigsAsync()
        {
            try
            {
                var configService = ServiceContainer.Instance.Get<IConfigService>();
                var elements = await configService.GetElementsAsync();
                var towers = await configService.GetTowersAsync();
                var enemies = await configService.GetEnemiesAsync();
                Debug.Log($"[Bootstrap] Config prewarmed: {elements.elements.Count} elements, {towers.towers.Count} towers, {enemies.enemies.Count} enemies");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] Config prewarm failed: {ex.Message}");
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
