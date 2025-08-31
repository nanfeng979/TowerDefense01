using UnityEngine;
using UnityEngine.UI;

namespace TD.Core
{
    /// <summary>
    /// 全局时间缩放控制器：通过 Inspector 或 UI Slider 调整 Time.timeScale。
    /// 可选择自动创建一个简易 UI 面板（Canvas+Panel+Slider+Text）。
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    public class TimeScaleController : MonoBehaviour
    {
        [Header("Time Scale")]
        [Range(0f, 10f)] public float timeScale = 1f;
        [Range(0f, 10f)] public float minScale = 0f;
        [Range(0f, 10f)] public float maxScale = 4f;
        public bool applyOnAwake = true;
        public bool dontDestroyOnLoad = true;

        [Header("UI Binding (Optional)")]
        public Slider slider;           // 若指定则用该 Slider 控制
        public Text label;              // 可选，用于显示数值（x1.0）
        public string labelFormat = "x{0:0.00}";

        [Header("Auto Create UI")]
        public bool autoCreateUI = true;      // 若未绑定 Slider，则自动创建一个面板
        public Vector2 panelAnchor = new Vector2(1f, 1f); // 右上角
        public Vector2 panelPivot = new Vector2(1f, 1f);
        public Vector2 panelOffset = new Vector2(-20f, -20f);

        private static TimeScaleController _instance;
        private float _defaultFixedDeltaTime = 0.02f;

        private void Awake()
        {
            // 单例（可选）
            if (dontDestroyOnLoad)
            {
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                _instance = this;
            }

            _defaultFixedDeltaTime = Time.fixedDeltaTime;
            // 保障 min/max 合法
            if (maxScale < minScale) maxScale = minScale;
            timeScale = Mathf.Clamp(timeScale, minScale, maxScale);

            if (applyOnAwake && Application.isPlaying)
                ApplyTimeScale(timeScale);

            EnsureUI();
            BindUI();
            UpdateUILabel();
        }

        private void OnValidate()
        {
            if (maxScale < minScale) maxScale = minScale;
            timeScale = Mathf.Clamp(timeScale, minScale, maxScale);
            if (Application.isPlaying)
                ApplyTimeScale(timeScale);
            UpdateUISliderValue();
            UpdateUILabel();
        }

        public void SetTimeScale(float value)
        {
            timeScale = Mathf.Clamp(value, minScale, maxScale);
            ApplyTimeScale(timeScale);
            UpdateUISliderValue();
            UpdateUILabel();
        }

        private void ApplyTimeScale(float value)
        {
            Time.timeScale = value;
            var scale = Mathf.Max(0.0001f, value);
            Time.fixedDeltaTime = _defaultFixedDeltaTime * scale; // 保持物理步长相对一致
        }

        private void EnsureUI()
        {
            if (slider != null) return;
            if (!autoCreateUI) return;

            // 找现有 Canvas，或创建一个
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Panel
            var panelGO = new GameObject("[TimeScalePanel]", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)panelGO.transform;
            rt.anchorMin = rt.anchorMax = panelAnchor;
            rt.pivot = panelPivot;
            rt.anchoredPosition = panelOffset;
            rt.sizeDelta = new Vector2(260, 56);
            var bg = panelGO.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            // Slider 容器
            var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGO.transform.SetParent(panelGO.transform, false);
            var srt = (RectTransform)sliderGO.transform;
            srt.anchorMin = new Vector2(0f, 0.5f);
            srt.anchorMax = new Vector2(1f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, 0f);
            srt.sizeDelta = new Vector2(-110f, 24f);
            slider = sliderGO.GetComponent<Slider>();

            // Background
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRT = (RectTransform)bgGO.transform;
            bgRT.anchorMin = new Vector2(0f, 0.25f);
            bgRT.anchorMax = new Vector2(1f, 0.75f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.15f);

            // Fill Area + Fill
            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var faRT = (RectTransform)fillAreaGO.transform;
            faRT.anchorMin = new Vector2(0f, 0f);
            faRT.anchorMax = new Vector2(1f, 1f);
            faRT.offsetMin = new Vector2(5f, 9f);
            faRT.offsetMax = new Vector2(-25f, -9f);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRT = (RectTransform)fillGO.transform;
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGO.GetComponent<Image>();
            fillImg.color = new Color(0.3f, 0.8f, 1f, 0.9f);

            // Handle Area + Handle
            var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var haRT = (RectTransform)handleAreaGO.transform;
            haRT.anchorMin = new Vector2(0f, 0f);
            haRT.anchorMax = new Vector2(1f, 1f);
            haRT.offsetMin = new Vector2(10f, 7f);
            haRT.offsetMax = new Vector2(-10f, -7f);

            var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRT = (RectTransform)handleGO.transform;
            handleRT.sizeDelta = new Vector2(16f, 16f);
            var handleImg = handleGO.GetComponent<Image>();
            handleImg.color = new Color(1f, 1f, 1f, 0.95f);

            // 配置 slider 的引用
            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(panelGO.transform, false);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = new Vector2(1f, 0.5f);
            lrt.anchorMax = new Vector2(1f, 0.5f);
            lrt.pivot = new Vector2(1f, 0.5f);
            lrt.anchoredPosition = new Vector2(-8f, 0f);
            lrt.sizeDelta = new Vector2(90f, 24f);

            // 使用内置字体创建 Text
            var text = labelGO.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleRight;
            text.raycastTarget = false;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 18;
            label = text;
        }

        private void BindUI()
        {
            if (slider == null) return;
            slider.minValue = minScale;
            slider.maxValue = maxScale;
            slider.wholeNumbers = false;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.onValueChanged.RemoveListener(OnSliderChanged);
            slider.onValueChanged.AddListener(OnSliderChanged);
            UpdateUISliderValue();
        }

        private void OnDestroy()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        private void OnSliderChanged(float val)
        {
            SetTimeScale(val);
        }

        private void UpdateUISliderValue()
        {
            if (slider != null && !Mathf.Approximately(slider.value, timeScale))
            {
                slider.minValue = minScale; // 确保 min/max 同步
                slider.maxValue = maxScale;
                slider.value = timeScale;
            }
        }

        private void UpdateUILabel()
        {
            if (label != null)
            {
                label.text = string.Format(labelFormat, timeScale);
            }
        }
    }
}
