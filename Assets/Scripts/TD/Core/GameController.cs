using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TD.Common;
using TD.UI;
using UnityEngine.UI;
using TD.UI.Panels;
using TD.Assets;
// using TD.Levels; // 避免直接依赖具体命名空间，优先通过接口
using TD.Core;

namespace TD.Core
{
    /// <summary>
    /// GameController: 单场景生命周期总控。
    /// - 管理游戏主状态（Menu, Playing, Paused）。
    /// - 驱动首屏 Loading → MainMenu 的流程。
    /// - 与 UIManager、IAssetProvider 协作进行界面切换与关卡进入/退出。
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public class GameController : MonoBehaviour
    {
        #region Types
        public enum GameState
        {
            Menu,
            Playing,
            Paused
        }
        #endregion

        #region Singleton / State
        public static GameController Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        #endregion

        #region Services & Level State
        private IUIManager _uiManager;
        private IAssetProvider _assetProvider;
        private Transform _levelRoot;
        private GameObject _currentLevel;
        private readonly List<(Camera cam, bool wasEnabled, AudioListener al, bool alWasEnabled)> _savedCamStates = new();
        #endregion
        // 场景内 Loading 根节点与控件（避免依赖 UIManager/类型）
        private GameObject _loadingRoot;
        private CanvasGroup _loadingCanvas;
        private Slider _loadingProgress;

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 依赖：UIManager（由 Bootstrapper 注册）；Loading 直接从场景查找
            ServiceContainer.Instance.TryGet<IUIManager>(out _uiManager);
            ServiceContainer.Instance.TryGet<IAssetProvider>(out _assetProvider);
            var go = GameObject.Find("Loading");
            if (go != null)
            {
                _loadingRoot = go;
                _loadingCanvas = go.GetComponentInChildren<CanvasGroup>();
                _loadingProgress = go.GetComponentInChildren<Slider>();
            }
        }

        private async void Start()
        {
            // 骨架流程：Loading 显示 → 等待 Bootstrapper 初始化 → 关闭 Loading → 打开 MainMenu
            ShowLoading();

            // 运行 Bootstrapper 初始化/预热
            void OnProgress(float p) { if (_loadingProgress != null) _loadingProgress.value = Mathf.Clamp01(p); }
            Bootstrapper.InitializationProgress += OnProgress;
            var bootstrap = FindObjectOfType<Bootstrapper>();
            if (bootstrap != null)
            {
                try { await bootstrap.RunInitializationAsync(); }
                catch { /* 错误已在 Bootstrapper 内部记录 */ }
            }
            Bootstrapper.InitializationProgress -= OnProgress;
            HideLoading();

            if (_uiManager != null)
            {
                await _uiManager.PushAsync<MainMenuPanel>("UI/MainMenu", modal: false);
            }

            EnterMainMenu();
        }
        #endregion

        #region Public API
        public void EnterMainMenu()
        {
            State = GameState.Menu;
        }

        public async Task EnterLevel(int levelId)
        {
            Debug.Log($"[GameController] Entering Level {levelId}");
            // 准备 LevelRoot
            EnsureLevelRoot();

            // 清理旧关卡（容错）
            if (_currentLevel != null)
            {
                Object.Destroy(_currentLevel);
                _currentLevel = null;
            }

            // 加载并实例化统一关卡预制体（Resources: Levels/LevelMain）
            var key = "Levels/LevelMain";
            GameObject prefab = null;
            if (_assetProvider != null)
            {
                prefab = await _assetProvider.LoadPrefabAsync(key);
            }
            if (prefab == null)
            {
                Debug.LogWarning($"[GameController] Level prefab not found: {key}. Creating placeholder.");
                _currentLevel = new GameObject($"[Level_{levelId}_Placeholder]");
                _currentLevel.transform.SetParent(_levelRoot, false);
            }
            else
            {
                _currentLevel = Object.Instantiate(prefab, _levelRoot, false);
                _currentLevel.name = $"[Level_{levelId}]";

                // 传入 LevelContext 给 LevelManager
                var lm = _currentLevel.GetComponentInChildren<ILevelManager>();
                if (lm != null)
                {
                    lm.Init(levelId);
                }
            }

            // 摄像机切换：如果关卡内自带摄像机，则临时禁用原有摄像机，启用关卡摄像机
            TrySwitchToLevelCameras();

            // 推入关卡 HUD
            if (_uiManager != null)
            {
                await _uiManager.PushAsync<LevelHUDPanel>("UI/LevelHUD", modal: false);
            }

            State = GameState.Playing;
        }

        public async Task ExitLevel()
        {
            // 销毁当前关卡实例
            if (_currentLevel != null)
            {
                Object.Destroy(_currentLevel);
                _currentLevel = null;
            }
            // 恢复进入关卡前的摄像机状态
            RestoreCameras();
            State = GameState.Menu;
            await Task.CompletedTask;
        }

        public void Pause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                Time.timeScale = 0f;
            }
        }

        public void Resume()
        {
            if (State == GameState.Paused)
            {
                State = GameState.Playing;
                Time.timeScale = 1f;
            }
        }

        public async void OnBackPressed()
        {
            if (_uiManager == null)
                return;

            // 在关卡中：ESC/返回键 → 若已打开对话框(Top.IsModal)则关闭；否则打开确认对话框
            if (State == GameState.Playing)
            {
                var top = _uiManager.Top;
                if (top != null && top.IsModal)
                {
                    await _uiManager.PopAsync();
                }
                else
                {
                    await _uiManager.PushAsync<TD.UI.Panels.ConfirmDialogPanel>("UI/ConfirmDialog", modal: true);
                }
                return;
            }

            // 其他状态：按普通回退处理
            _uiManager.RouteBack();
        }
        #endregion

        #region Helpers
        private void ShowLoading()
        {
            if (_loadingRoot == null) return;
            if (_loadingCanvas != null) _loadingCanvas.alpha = 1f;
            _loadingRoot.SetActive(true);
            if (_loadingProgress != null) _loadingProgress.value = 0f;
        }

        private void HideLoading()
        {
            if (_loadingRoot == null) return;
            if (_loadingCanvas != null) _loadingCanvas.alpha = 0f;
            _loadingRoot.SetActive(false);
        }

        private void EnsureLevelRoot()
        {
            if (_levelRoot != null) return;
            var root = GameObject.Find("[LevelRoot]");
            if (root == null)
            {
                root = new GameObject("[LevelRoot]");
                Object.DontDestroyOnLoad(root);
            }
            _levelRoot = root.transform;
        }

        private void TrySwitchToLevelCameras()
        {
            if (_currentLevel == null) return;
            var levelCams = _currentLevel.GetComponentsInChildren<Camera>(true);
            if (levelCams == null || levelCams.Length == 0)
            {
                // 关卡未提供摄像机，继续使用场景原有摄像机
                return;
            }

            // 记录并禁用当前所有“非关卡”的已启用摄像机及其AudioListener
            _savedCamStates.Clear();
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null) continue;
                if (IsUnder(cam.transform, _levelRoot)) continue; // 不处理关卡内摄像机
                var al = cam.GetComponent<AudioListener>();
                _savedCamStates.Add((cam, cam.enabled, al, al != null && al.enabled));
                cam.enabled = false;
                if (al != null) al.enabled = false;
            }

            // 启用关卡摄像机（通常预制体已配置好）
            foreach (var cam in levelCams)
            {
                if (cam == null) continue;
                cam.enabled = true;
            }
        }

        private void RestoreCameras()
        {
            if (_savedCamStates.Count == 0) return;
            foreach (var s in _savedCamStates)
            {
                if (s.cam != null) s.cam.enabled = s.wasEnabled;
                if (s.al != null) s.al.enabled = s.alWasEnabled;
            }
            _savedCamStates.Clear();
        }

        private static bool IsUnder(Transform node, Transform root)
        {
            if (node == null || root == null) return false;
            var t = node;
            while (t != null)
            {
                if (t == root) return true;
                t = t.parent;
            }
            return false;
        }
        #endregion
    }
}
