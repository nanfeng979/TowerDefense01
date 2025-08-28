using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TD.Core;
using TD.Config;

namespace TD.UI
{
    /// <summary>
    /// 简易符文选择 UI：监听 RoundEnded，在需要时弹出 3 个选项。
    /// 无需预制体，运行时动态创建 Canvas 与按钮。
    /// </summary>
    public class RuneSelectionUI : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _panel;
        private List<Button> _buttons = new List<Button>();
        private float _prevTimeScale = 1f;
        
        private readonly Dictionary<string, Color> _rarityColors = new Dictionary<string, Color>
        {
            { "Common", new Color(0.7f, 0.7f, 0.7f, 0.95f) },    // 灰色
            { "Rare", new Color(0.2f, 0.6f, 1f, 0.95f) },        // 蓝色
            { "Epic", new Color(0.8f, 0.2f, 0.8f, 0.95f) }       // 紫色
        };

        private void Awake()
        {
            CreateUI();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (FindObjectOfType<RuneSelectionUI>() == null)
            {
                var go = new GameObject("[RuneSelectionUI]");
                go.AddComponent<RuneSelectionUI>();
                Object.DontDestroyOnLoad(go);
            }
        }

        private void OnEnable()
        {
            GameEvents.RoundEnded += OnRoundEnded;
        }
        private void OnDisable()
        {
            GameEvents.RoundEnded -= OnRoundEnded;
        }

        private void OnRoundEnded(int round)
        {
            Debug.Log($"[RuneSelectionUI] Round {round} ended, checking for rune offers");
            if (!ServiceContainer.Instance.TryGet<RunesService>(out var runes)) 
            {
                Debug.Log($"[RuneSelectionUI] RunesService not found");
                // 如果服务不可用，也要触发完成事件让游戏继续
                TD.Core.GameEvents.RaiseRuneSelectionCompleted();
                return;
            }
            var offers = runes.GetOffersForRound(round);
            Debug.Log($"[RuneSelectionUI] Got {offers?.Count ?? 0} offers for round {round}");
            if (offers == null || offers.Count == 0) 
            {
                Debug.Log($"[RuneSelectionUI] No offers available, skipping UI and triggering completion");
                // 没有符文可选时，直接触发完成事件
                TD.Core.GameEvents.RaiseRuneSelectionCompleted();
                return;
            }

            bool pause = runes.PauseOnSelection;
            Debug.Log($"[RuneSelectionUI] Opening rune selection UI");
            TryOpen(offers, pause);
        }

        private void TryOpen(List<RuneDef> offers, bool pause)
        {
            _canvas.enabled = true;
            _panel.gameObject.SetActive(true);
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].gameObject.SetActive(false);
            }

            if (pause)
            {
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            for (int i = 0; i < offers.Count && i < 3; i++)
            {
                var idx = i;
                var def = offers[i];
                var btn = _buttons[i];
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnChoose(def.id));
                
                // 设置稀有度颜色
                var img = btn.GetComponent<Image>();
                if (img != null && _rarityColors.TryGetValue(def.rarity, out var color))
                {
                    img.color = color;
                }
                
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.text = $"<b>[{def.rarity}] {def.name}</b>\n{def.description ?? "No description"}";
                    txt.fontSize = 14;
                }
            }
        }

        private void OnChoose(string id)
        {
            Debug.Log($"[RuneSelectionUI] Player selected rune: {id}");
            
            if (ServiceContainer.Instance.TryGet<RunesService>(out var runes))
            {
                runes.ChooseRune(id);
            }
            
            // 触发符文选择事件
            TD.Core.GameEvents.RaiseRuneSelected(id);
            TD.Core.GameEvents.RaiseRuneSelectionCompleted();
            
            Close();
        }

        private void Close()
        {
            _panel.gameObject.SetActive(false);
            _canvas.enabled = false;
            Time.timeScale = _prevTimeScale;
        }

        private void CreateUI()
        {
            // Canvas
            var go = new GameObject("[RuneSelectionCanvas]");
            go.layer = LayerMask.NameToLayer("UI");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(go);

            // EventSystem 确保可交互
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(es);
            }

            // Panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(go.transform, false);
            _panel = panelGO.AddComponent<RectTransform>();
            var img = panelGO.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.6f);
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(600, 380);
            
            // 添加标题
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_panel, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(560, 40);
            titleRT.anchoredPosition = new Vector2(0, 160);
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "选择一个符文";

            // Buttons
            for (int i = 0; i < 3; i++)
            {
                var b = CreateButton(_panel, new Vector2(0, 60 - 90 * i));
                _buttons.Add(b);
            }

            _canvas.enabled = false;
            _panel.gameObject.SetActive(false);
        }

        private Button CreateButton(RectTransform parent, Vector2 anchoredPos)
        {
            var go = new GameObject("RuneButton");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(560, 80);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
            var btn = go.AddComponent<Button>();
            
            // 添加悬停效果
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 0.8f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.6f, 1f);
            btn.colors = colors;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(15, 10);
            trt.offsetMax = new Vector2(-15, -10);
            var txt = textGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.fontSize = 12;
            txt.text = "符文";

            return btn;
        }
    }
}
