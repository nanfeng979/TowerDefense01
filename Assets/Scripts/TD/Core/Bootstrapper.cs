using UnityEngine;
using TD.Config;

namespace TD.Core
{
    /// <summary>
    /// 入口脚本：初始化服务容器与全局 UpdateDriver，不再执行数据校验。
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private static IServiceContainer _container;

        private void Awake()
        {
            if (_container != null)
            {
                // 已初始化，避免重复实例
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            // 初始化服务容器
            var container = new ServiceContainer();

            // 注册配置相关服务
            IJsonLoader loader = new StreamingAssetsJsonLoader();
            container.RegisterSingleton<IJsonLoader>(loader);
            container.RegisterSingleton<IConfigService>(new ConfigService(loader));

            // 确保 UpdateDriver 挂载
            var driver = gameObject.GetComponent<UpdateDriver>();
            if (driver == null) driver = gameObject.AddComponent<UpdateDriver>();
            container.RegisterSingleton<UpdateDriver>(driver);

            _container = container;
            Debug.Log("[Bootstrap] ServiceContainer initialized and UpdateDriver attached.");
        }

        public static T Resolve<T>() => _container.Resolve<T>();
        public static bool TryResolve<T>(out T service) => _container.TryResolve(out service);
    }
}
