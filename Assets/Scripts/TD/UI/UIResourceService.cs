using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using TD.Config;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


// Addressables 相关引用在宏内



#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

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

        [System.Serializable]
        private class UiResourcesCfg
        {
            public string defaultFontAssetPath; // 例如 "Assets/Fonts/ChineseTMP.asset"
            public bool preloadDefault = false;
            // Addressables 支持（运行时推荐）：
            public string defaultFontAddress; // 例如 "Fonts/ChineseTMP"
        }

        private UiResourcesCfg _cfg = new UiResourcesCfg();
        private TMP_FontAsset _defaultFont;
        private readonly Dictionary<string, Task<TMP_FontAsset>> _loadingTasks = new Dictionary<string, Task<TMP_FontAsset>>();

        public UIResourceService(IJsonLoader loader)
        {
            _loader = loader;
        }

        public async Task InitializeAsync()
        {
            Debug.Log("UIResourceService InitializeAsync");
            try
            {
                _cfg = await _loader.LoadAsync<UiResourcesCfg>("ui/ui_resources.json") ?? new UiResourcesCfg();
            }
            catch
            {
                _cfg = new UiResourcesCfg();
            }
            // 启动阶段仅加载配置，不做资源预加载；字体在首次需要时按需加载
        }

        public async Task<TMP_FontAsset> GetOrLoadDefaultFontAsync()
        {
            if (_defaultFont != null) return _defaultFont;
            if (!string.IsNullOrEmpty(_cfg.defaultFontAddress))
            {
                _defaultFont = await LoadFontByAddressAsync(_cfg.defaultFontAddress);
                return _defaultFont;
            }
            if (!string.IsNullOrEmpty(_cfg.defaultFontAssetPath))
            {
                _defaultFont = await LoadFontAssetAsync(_cfg.defaultFontAssetPath);
            }
            return _defaultFont;
        }

        public async Task SetDefaultFontAsync(TMP_Text text)
        {
            if (text == null) return;
            var font = await GetOrLoadDefaultFontAsync();
            if (font != null) text.font = font;
        }

        public async Task SetFontAsync(TMP_Text text, string assetPath)
        {
            if (text == null) return;
            if (string.IsNullOrEmpty(assetPath))
            {
                await SetDefaultFontAsync(text);
                return;
            }
            var font = await LoadFontAssetAsync(assetPath);
            if (font != null) text.font = font;
        }

        private Task<TMP_FontAsset> LoadFontAssetAsync(string assetPath)
        {
            if (_loadingTasks.TryGetValue(assetPath, out var existing))
                return existing;

            TMP_FontAsset asset = null;
            try
            {
#if UNITY_EDITOR
                asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
#else
                Debug.LogWarning($"[UIResourceService] Runtime cannot load assets by path: {assetPath}. Use pre-built Resources instead or preload in editor.");
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UIResourceService] Load font failed: {assetPath}, {ex.Message}");
            }

            var task = Task.FromResult(asset);
            _loadingTasks[assetPath] = task;
            return task;
        }

        private Task<TMP_FontAsset> LoadFontByAddressAsync(string address)
        {
            if (_loadingTasks.TryGetValue(address, out var existing))
                return existing;

// #if ENABLE_ADDRESSABLES
            var handle = Addressables.LoadAssetAsync<TMP_FontAsset>(address);
            var tcs = new TaskCompletionSource<TMP_FontAsset>();
            _loadingTasks[address] = tcs.Task;
            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    tcs.SetResult(op.Result);
                else
                {
                    Debug.LogWarning($"[UIResourceService] Addressables load failed: {address}, {op.OperationException}");
                    tcs.SetResult(null);
                }
            };
            return tcs.Task;
// #else
//             Debug.LogWarning($"[UIResourceService] Addressables not enabled. Define ENABLE_ADDRESSABLES and set up Addressables.");
//             var task = Task.FromResult<TMP_FontAsset>(null);
//             _loadingTasks[address] = task;
//             return task;
// #endif
        }
    }
}
