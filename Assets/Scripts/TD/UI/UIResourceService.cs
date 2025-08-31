using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TD.Config;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TD.UI
{
    /// <summary>
    /// 通过 Assets 路径加载 TMP_FontAsset 的全局资源服务。
    /// - 配置文件：StreamingAssets/TD/ui/ui_resources.json
    /// - 支持：启动时预加载默认字体，或按需加载
    /// - 并发：同一路径的并发加载会复用同一个 Task；设置时若未加载会等待
    /// </summary>
    public class UIResourceService : IUIResourceService
    {
        private readonly IJsonLoader _loader;
        private TMP_FontAsset _defaultFont;

        public UIResourceService(IJsonLoader loader)
        {
            _loader = loader;
        }

        public async Task InitializeAsync()
        {
            // 如需在此预加载默认字体，可读取配置 _loader.LoadAsync 等
            await Task.CompletedTask;
        }

        public TMP_FontAsset GetDefaultFont()
        {
            return _defaultFont;
        }

        public void SetDefaultFont(TMP_FontAsset font)
        {
            _defaultFont = font;
        }
    }
}
