using TD.Assets;
using TD.Config;
using TD.UI;
using UnityEngine;

namespace TD.Core
{
    public partial class Bootstrapper : MonoBehaviour
    {
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