using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TD.Core;
using TD.Config;
using TMPro;
using TD.UI;

namespace TD.UI
{
    /// <summary>
    /// 简易符文选择 UI：监听 RoundEnded，在需要时弹出 3 个选项。
    /// 无需预制体，运行时动态创建 Canvas 与按钮。
    /// </summary>
    public class RuneSelectionUI : MonoBehaviour
    {
        [System.Serializable]
        private class RuneSelectionUiSettings
        {
            public string locale = "zh-CN";
            public string[] optionKeys = new[] { "Alpha1", "Alpha2", "Alpha3" };
            public string toggleKey = "Escape";
            // 指定 Resources 内的 TMP_FontAsset 路径（不带扩展名），如 "Fonts/ChineseTMP"
            public string tmpFontResourcesPath = null;
            // 指定系统字体名（Windows 示例："Microsoft YaHei UI"），运行时动态构建 TMP 字体
            public string osFontName = null;
            // 指定字体文件路径（绝对路径，或相对 StreamingAssets 的相对路径）
            public string ttfFilePath = null;
        }
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Image _overlay; // 半透明遮罩
        private RectTransform _panel;
        private CanvasGroup _panelGroup;
        private List<Button> _buttons = new List<Button>();
        private float _prevTimeScale = 1f;
        private bool _hiddenTemporarily = false; // ESC 隐藏状态
        private Button _resumeButton; // 恢复显示按钮
        private string _registeredFontPath; // Windows 动态注册的字体路径，用于清理
        [Header("输入配置")]
        [SerializeField] private KeyCode[] _optionKeys = new KeyCode[3] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
        [SerializeField] private KeyCode _toggleKey = KeyCode.Escape;
        [Header("视觉配置")]
        [SerializeField] private Material _blurMaterial; // 可选：毛玻璃材质（URP 自定义）

        private readonly Dictionary<string, Color> _rarityColors = new Dictionary<string, Color>
        {
            { "Common", new Color(0.7f, 0.7f, 0.7f, 0.95f) },    // 灰色
            { "Rare", new Color(0.2f, 0.6f, 1f, 0.95f) },        // 蓝色
            { "Epic", new Color(0.8f, 0.2f, 0.8f, 0.95f) }       // 紫色
        };

        private void Awake()
        {
            Debug.Log("[RuneSelectionUI] Awake");
            CreateUI();
        }

        private async void Start()
        {
            // 读取外部配置（快捷键/语言），若失败则沿用默认
            try
            {
                if (ServiceContainer.Instance.TryGet<IJsonLoader>(out var loader))
                {
                    var settings = await loader.LoadAsync<RuneSelectionUiSettings>("ui/rune_selection.json");
                    if (settings != null)
                    {
                        // 解析选择键
                        if (settings.optionKeys != null && settings.optionKeys.Length > 0)
                        {
                            var list = new List<KeyCode>(3);
                            for (int i = 0; i < Mathf.Min(3, settings.optionKeys.Length); i++)
                            {
                                if (TryParseKeyCode(settings.optionKeys[i], out var kc)) list.Add(kc);
                            }
                            if (list.Count == 3) _optionKeys = list.ToArray();
                        }
                        // 解析切换键
                        if (!string.IsNullOrEmpty(settings.toggleKey) && TryParseKeyCode(settings.toggleKey, out var toggle))
                        {
                            _toggleKey = toggle;
                        }
#if TD_LOCALIZATION
            // 设置语言（若 Bootstrapper 未设置）
            if (ServiceContainer.Instance.TryGet<TD.Core.ILocalizationService>(out var loc))
            {
                if (!string.IsNullOrEmpty(settings.locale))
                loc.SetLocale(settings.locale);
            }
#endif
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RuneSelectionUI] Load settings failed: {ex.Message}");
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
            _overlay.raycastTarget = true;
            _overlay.gameObject.SetActive(true);
            _panel.gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _panel.localScale = Vector3.one * 0.96f;
            _panelGroup.alpha = 0f;
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

                var tmp = btn.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = $"<b>[{def.rarity}] {def.name}</b>\n{def.description ?? "No description"}";
                    tmp.fontSize = 20;
                    tmp.enableWordWrapping = true;
                    tmp.richText = true;
                }
            }

            // 进入动画 200ms
            StartCoroutine(FadeInRoutine());
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
            _overlay.gameObject.SetActive(false);
            _canvas.enabled = false;
            Time.timeScale = _prevTimeScale;
        }

        private void CreateUI()
        {
            ServiceContainer.Instance.TryGet<IUIResourceService>(out var uiResObj);

            // Canvas
            var go = new GameObject("[RuneSelectionCanvas]");
            go.layer = LayerMask.NameToLayer("UI");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            _canvasGroup = go.AddComponent<CanvasGroup>();
            DontDestroyOnLoad(go);

            // EventSystem 确保可交互
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(es);
            }

            // Overlay（半透明 + 可选毛玻璃）
            var overlayGO = new GameObject("Overlay");
            overlayGO.transform.SetParent(go.transform, false);
            var overlayRT = overlayGO.AddComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero; overlayRT.anchorMax = Vector2.one; overlayRT.offsetMin = Vector2.zero; overlayRT.offsetMax = Vector2.zero;
            _overlay = overlayGO.AddComponent<Image>();
            _overlay.color = new Color(0f, 0f, 0f, 0.55f);
            if (_blurMaterial != null) { _overlay.material = _blurMaterial; }
            _overlay.raycastTarget = true;

            // Panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(go.transform, false);
            _panel = panelGO.AddComponent<RectTransform>();
            var img = panelGO.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            img.raycastTarget = true;
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(880, 560);
            _panelGroup = panelGO.AddComponent<CanvasGroup>();

            // Header 标题
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_panel, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(800, 60);
            titleRT.anchoredPosition = new Vector2(0, 210);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;
            titleTMP.fontSize = 36;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.text = Localize("选择一个符文");
            titleTMP.font = uiResObj.GetDefaultFont();

            // Buttons 区域
            for (int i = 0; i < 3; i++)
            {
                var b = CreateButton(_panel, new Vector2(0, 90 - 150 * i));
                _buttons.Add(b);
            }

            // Footer: 恢复按钮（ESC 隐藏后显示，用于恢复 UI）
            var resumeGO = new GameObject("ResumeButton");
            resumeGO.transform.SetParent(_panel, false);
            var rrt = resumeGO.AddComponent<RectTransform>();
            rrt.sizeDelta = new Vector2(220, 52);
            rrt.anchoredPosition = new Vector2(0, -230);
            var rImg = resumeGO.AddComponent<Image>();
            rImg.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            _resumeButton = resumeGO.AddComponent<Button>();
            _resumeButton.onClick.AddListener(() => { ShowPanel(true); });
            // 创建 Resume 按钮的文本子对象
            var rTextGO = new GameObject("Text");
            rTextGO.transform.SetParent(resumeGO.transform, false);
            var rTextRT = rTextGO.AddComponent<RectTransform>();
            rTextRT.anchorMin = Vector2.zero;
            rTextRT.anchorMax = Vector2.one;
            rTextRT.offsetMin = Vector2.zero;
            rTextRT.offsetMax = Vector2.zero;
            var rText = rTextGO.AddComponent<TextMeshProUGUI>();
            rText.text = Localize("显示符文选择 (ESC)");
            rText.color = Color.white;
            rText.fontSize = 22;
            rText.alignment = TextAlignmentOptions.Center;
            rText.font = uiResObj.GetDefaultFont();
            _resumeButton.gameObject.SetActive(false);

            _canvas.enabled = false;
            _overlay.gameObject.SetActive(false);
            _panel.gameObject.SetActive(false);
        }

        private Button CreateButton(RectTransform parent, Vector2 anchoredPos)
        {
            ServiceContainer.Instance.TryGet<IUIResourceService>(out var uiResObj);

            var go = new GameObject("RuneButton");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 120);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
            var btn = go.AddComponent<Button>();

            // 添加悬停效果
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.2f, 0.3f, 1f);
            colors.pressedColor = new Color(0.16f, 0.16f, 0.22f, 1f);
            btn.colors = colors;
            // 文本（TMP）
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var trt = textGO.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(20, 16);
            trt.offsetMax = new Vector2(-20, -16);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.fontSize = 20;
            tmp.text = Localize("符文");
            tmp.font = uiResObj.GetDefaultFont();

            return btn;
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.enabled) return;
            if (!_hiddenTemporarily)
            {
                for (int i = 0; i < _buttons.Count && i < _optionKeys.Length; i++)
                {
                    if (Input.GetKeyDown(_optionKeys[i]) && _buttons[i].gameObject.activeSelf)
                    {
                        _buttons[i].onClick.Invoke();
                        return;
                    }
                }
            }
            if (Input.GetKeyDown(_toggleKey))
            {
                ShowPanel(_hiddenTemporarily); // 反向：隐藏中则显示，否则隐藏
            }
        }

        private void ShowPanel(bool show)
        {
            _hiddenTemporarily = !show;
            _panel.gameObject.SetActive(show);
            _resumeButton.gameObject.SetActive(!show);
        }

        private System.Collections.IEnumerator FadeInRoutine()
        {
            float t = 0f;
            const float dur = 0.2f; // 200ms
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _canvasGroup.alpha = k;
                _panelGroup.alpha = k;
                _panel.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, k);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
            _panelGroup.alpha = 1f;
            _panel.localScale = Vector3.one;
        }

        private string Localize(string key)
        {
#if TD_LOCALIZATION
            if (ServiceContainer.Instance.TryGet<TD.Core.ILocalizationService>(out var loc))
            {
                return loc.T(key, key);
            }
#endif
            return key;
        }

        private bool TryParseKeyCode(string name, out KeyCode key)
        {
            key = KeyCode.None;
            if (string.IsNullOrEmpty(name)) return false;
            // Name 与 UnityEngine.KeyCode 枚举同名，如 "Alpha1"、"Escape"
            try
            {
                key = (KeyCode)System.Enum.Parse(typeof(KeyCode), name, ignoreCase: true);
                return true;
            }
            catch { return false; }
        }

        private void OnDestroy()
        {
        }
    }
}
