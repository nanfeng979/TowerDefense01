# Rune Selection UI 布局与引用关系

本 UI 负责在每回合结束后，向玩家展示 3 个符文候选，并在玩家选择后结束符文阶段。

## 视觉与交互要求
- 主题：半透明深色 + 毛玻璃（Overlay 支持可选材质赋值），200ms 淡入+缩放动效
- 字体：TextMeshPro（中文已准备）
- 快捷键：1/2/3 选择对应卡片；ESC 隐藏 UI，此时显示“显示符文选择(ESC)”按钮用于恢复
- 不允许跳过：无“跳过”按钮；若无候选，系统自动发送完成事件
- 本地化：提供 `Localize(string key)` 扩展点，便于跨项目替换

## 结构树（运行时创建）
```
[RuneSelectionCanvas] (Canvas, CanvasScaler, GraphicRaycaster, CanvasGroup)
└── Overlay (Image)  // 半透明遮罩，可选毛玻璃材质
└── Panel (RectTransform, Image, CanvasGroup)
    ├── Title (TextMeshProUGUI)
    ├── RuneButton (x3)
    │   └── Text (TextMeshProUGUI) // 名称+描述，按稀有度着色背景
    └── ResumeButton (Button, TextMeshProUGUI) // ESC 隐藏时显示，用于恢复
```

## 脚本与引用
- `Assets/Scripts/TD/UI/RuneSelectionUI.cs`
  - 公开序列化字段：
    - `_optionKeys`：KeyCode[3] 默认 [1,2,3]
    - `_toggleKey`：KeyCode 默认 ESC
    - `_blurMaterial`：可选材质（URP 毛玻璃），置空则仅半透明
  - 方法：
    - `TryOpen(List<RuneDef> offers, bool pause)`：构建并展示 UI
    - `OnChoose(string id)`：应用符文，广播 `RuneSelected` 与 `RuneSelectionCompleted`
    - `ShowPanel(bool show)`：ESC 隐藏/恢复
    - `FadeInRoutine()`：200ms 动画
    - `Localize(string key)`：本地化扩展点

## 事件对接
- 入站：监听 `GameEvents.RoundEnded(int)`，调用 RunesService 获取候选并显示 UI
- 出站：
  - `GameEvents.RaiseRuneSelected(id)`（选择时）
  - `GameEvents.RaiseRuneSelectionCompleted()`（选择后或无候选）

## 稀有度配色（默认）
- Common: #B3B3B3（浅灰）
- Rare: #3B82F6（蓝）
- Epic: #8B5CF6（紫）

> 注：可后续抽出为 ScriptableObject 配置，以支持不同项目皮肤。

## 本地化与复用
- `Localize` 方法预留字串入口；跨项目时可直接替换为 `ILocalizationService`。
- Rune 文案来自 StreamingAssets JSON（已中文化），TMP 启用自动换行。

## 动画与性能
- 使用 `CanvasGroup alpha` + `RectTransform scale` 在 Unscaled 时间线下动画 200ms
- 仅在需要时启用/显示，关闭后禁用 Canvas，避免多余开销
