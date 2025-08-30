using System.Threading.Tasks;
using UnityEngine;
using TD.Common;
using TD.UI;
using UnityEngine.UI;
using TD.UI.Panels;

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
        public enum GameState
        {
            Menu,
            Playing,
            Paused
        }

        public static GameController Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;

        private IUIManager _uiManager;
        // 场景内 Loading 根节点与控件（避免依赖 UIManager/类型）
        private GameObject _loadingRoot;
        private CanvasGroup _loadingCanvas;
        private Slider _loadingProgress;

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
            // 骨架流程：场景内 Loading 显示 → 等待 Bootstrapper 广播服务就绪 → 关闭 Loading → 打开 MainMenu（若 UIManager 可用）
            if (_loadingRoot != null)
            {
                if (_loadingCanvas != null) _loadingCanvas.alpha = 1f;
                _loadingRoot.SetActive(true);
                if (_loadingProgress != null) _loadingProgress.value = 0f;
            }

            // 等待 Bootstrapper.ServicesReady 事件
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            void OnReady() { tcs.TrySetResult(true); }
            void OnProgress(float p)
            {
                if (_loadingProgress != null) _loadingProgress.value = Mathf.Clamp01(p);
            }
            Bootstrapper.InitializationProgress += OnProgress;
            Bootstrapper.ServicesReady += OnReady;
            await tcs.Task;
            Bootstrapper.ServicesReady -= OnReady;
            Bootstrapper.InitializationProgress -= OnProgress;

            if (_loadingRoot != null)
            {
                if (_loadingCanvas != null) _loadingCanvas.alpha = 0f;
                _loadingRoot.SetActive(false);
            }

            await InitializeAsync();

            if (_uiManager != null)
            {
                await _uiManager.PushAsync<MainMenuPanel>("UI/MainMenu", modal: false);
            }

            EnterMainMenu();
        }

        /// <summary>
        /// 预留：如需额外初始化（非资源加载），可在此完成。
        /// </summary>
        private Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public void EnterMainMenu()
        {
            State = GameState.Menu;
        }

        public async Task EnterLevel(int levelId)
        {
            // 进入关卡：此处仅保留流程骨架
            State = GameState.Playing;
            // 例如：await _uiManager.ReplaceAsync<GameplayPanel>("UI/GameLevel_" + levelId);
            await Task.CompletedTask;
        }

        public async Task ExitLevel()
        {
            // 退出关卡：此处仅保留流程骨架
            State = GameState.Menu;
            // 返回到关卡选择面板或主菜单
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

        public void OnBackPressed()
        {
            // 默认路由到 UIManager；若未处理且在关卡中，可触发暂停/确认对话框流程（后续接入）
            if (_uiManager != null)
            {
                _uiManager.RouteBack();
            }
        }
    }
}
