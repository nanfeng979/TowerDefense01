using UnityEngine;
using TD.Core;
using TD.Config;

namespace TD.Gameplay.UI
{
    /// <summary>
    /// 游戏状态显示UI：生命、金钱、当前回合等信息。
    /// 自动创建简单的屏幕UI显示基本游戏信息。
    /// </summary>
    public class GameStatusUI : MonoBehaviour
    {
        private Canvas _canvas;
        private UnityEngine.UI.Text _statusText;
        private int _lives = 20;
        private int _money = 100;
        private int _currentRound = 0;
        private int _enemiesRemaining = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindObjectOfType<GameStatusUI>() == null)
            {
                var go = new GameObject("[GameStatusUI]");
                go.AddComponent<GameStatusUI>();
                Object.DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            CreateUI();
        }

        private void OnEnable()
        {
            GameEvents.RoundEnded += OnRoundEnded;
            GameEvents.RoundRewardGranted += OnRoundRewardGranted;
            GameEvents.EnemySpawned += OnEnemySpawned;
        }

        private void OnDisable()
        {
            GameEvents.RoundEnded -= OnRoundEnded;
            GameEvents.RoundRewardGranted -= OnRoundRewardGranted;
            GameEvents.EnemySpawned -= OnEnemySpawned;
        }

        private void Update()
        {
            if (_statusText != null)
            {
                _enemiesRemaining = TD.Gameplay.Enemy.EnemyRegistry.All.Count;
                _statusText.text = $"生命: {_lives}   金钱: {_money}   回合: {_currentRound}   剩余敌人: {_enemiesRemaining}";
            }
        }

        private void OnRoundEnded(int round)
        {
            _currentRound = round;
        }

        private void OnRoundRewardGranted(int reward)
        {
            _money += reward;
        }

        private void OnEnemySpawned(TD.Gameplay.Enemy.EnemyAgent agent)
        {
            // 可以在这里处理敌人生成的UI反馈
        }

        private void CreateUI()
        {
            // Canvas
            var canvasGO = new GameObject("[GameStatusCanvas]");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10; // 确保在其他UI之上
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            DontDestroyOnLoad(canvasGO);

            // Status Text
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 1);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.pivot = new Vector2(0.5f, 1);
            textRT.sizeDelta = new Vector2(-20, 40);
            textRT.anchoredPosition = new Vector2(0, -10);

            _statusText = textGO.AddComponent<UnityEngine.UI.Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _statusText.fontSize = 16;
            _statusText.color = Color.white;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.text = $"生命: {_lives}   金钱: {_money}   回合: {_currentRound}   剩余敌人: {_enemiesRemaining}";

            // 添加背景
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(textGO.transform, false);
            bgGO.transform.SetAsFirstSibling();
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0);
            bgRT.anchorMax = new Vector2(1, 1);
            bgRT.offsetMin = new Vector2(-10, -5);
            bgRT.offsetMax = new Vector2(10, 5);
            var bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0.7f);
        }

        public void SetLives(int lives)
        {
            _lives = lives;
        }

        public void SetMoney(int money)
        {
            _money = money;
        }

        public void AddMoney(int amount)
        {
            _money += amount;
        }

        public void TakeDamage(int damage = 1)
        {
            _lives = Mathf.Max(0, _lives - damage);
        }
    }
}
