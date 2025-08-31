using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TD.Assets;
using TD.Core;

namespace TD.UI
{
    /// <summary>
    /// UIManager：最小可运行的 UI 栈实现（加载逻辑后续接入实际资源）。
    /// </summary>
    public class UIManager : IUIManager
    {
        private readonly Stack<UIPanel> _stack = new Stack<UIPanel>();
        private Transform _root;
        private IAssetProvider _assetProvider;

        public int Count => _stack.Count;
        public UIPanel Top => _stack.Count > 0 ? _stack.Peek() : null;

        public async Task<TPanel> PushAsync<TPanel>(string key, object args = null, bool modal = false) where TPanel : UIPanel
        {
            EnsureRoot();
            GameObject go = null;
            // 优先使用资源提供者加载预制体
            if (_assetProvider != null && !string.IsNullOrEmpty(key))
            {
                var prefab = await _assetProvider.LoadPrefabAsync(key);
                if (prefab != null)
                {
                    go = Object.Instantiate(prefab, _root, false);
                    go.name = $"[UIPanel] {key}";
                }
            }
            // 回退：创建空对象
            if (go == null)
            {
                go = new GameObject($"[UIPanel] {key}");
                go.transform.SetParent(_root, false);
            }

            var panel = go.GetComponent<TPanel>();
            if (panel == null) panel = go.AddComponent<TPanel>();
            panel.IsModal = modal;
            // 确保新面板在最上层
            panel.transform.SetAsLastSibling();
            await panel.OnShowAsync(args);
            _stack.Push(panel);
            UpdateVisibility();
            return panel;
        }

        public async Task<bool> PopAsync()
        {
            if (_stack.Count == 0) return false;
            var panel = _stack.Pop();
            await panel.OnHideAsync();
            Object.Destroy(panel.gameObject);
            UpdateVisibility();
            return true;
        }

        public async Task ReplaceAsync<TPanel>(string key, object args = null) where TPanel : UIPanel
        {
            if (_stack.Count > 0)
            {
                var top = _stack.Pop();
                await top.OnHideAsync();
                Object.Destroy(top.gameObject);
            }
            await PushAsync<TPanel>(key, args, modal: false);
            UpdateVisibility();
        }

        public void RouteBack()
        {
            if (_stack.Count == 0) return;
            var top = _stack.Peek();
            if (!top.OnBackRequested())
            {
                // 若未消费，则弹出
                _ = PopAsync();
            }
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            // 解析资源提供者
            if (_assetProvider == null)
            {
                ServiceContainer.Instance.TryGet<IAssetProvider>(out _assetProvider);
            }
            var rootGO = GameObject.Find("[UIRoot]");
            if (rootGO == null)
            {
                rootGO = new GameObject("[UIRoot]");
                Object.DontDestroyOnLoad(rootGO);
                var canvas = rootGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = rootGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                rootGO.AddComponent<GraphicRaycaster>();
            }
            _root = rootGO.transform;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Object.DontDestroyOnLoad(es);
            }
        }

        /// <summary>
        /// 仅保留“自顶向下遇到的第一个非模态面板及其上方所有模态面板”为激活状态；其余一律隐藏。
        /// 这样：
        /// - 非模态屏（首页、关卡页、游戏HUD）之间不会互相透视
        /// - 模态层（确认对话框等）可叠加在当前屏之上
        /// </summary>
        private void UpdateVisibility()
        {
            if (_stack.Count == 0) return;
            var arr = _stack.ToArray(); // 顶部在 index 0

            // 查找自顶向下的第一个非模态面板，作为“基底屏”
            int baseIndex = -1;
            for (int i = 0; i < arr.Length; i++)
            {
                if (!arr[i].IsModal)
                {
                    baseIndex = i;
                    break;
                }
            }
            if (baseIndex < 0) baseIndex = 0; // 若全为模态，至少保留顶层

            // index <= baseIndex 的面板保持激活（顶上的模态 + 基底屏）；其余隐藏
            for (int i = 0; i < arr.Length; i++)
            {
                bool shouldActive = i <= baseIndex;
                var p = arr[i];
                if (p != null && p.gameObject.activeSelf != shouldActive)
                    p.gameObject.SetActive(shouldActive);
            }
        }
    }
}
