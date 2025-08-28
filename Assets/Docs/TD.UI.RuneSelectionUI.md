# TD.UI.RuneSelectionUI 交互与完成信号

位置：`Assets/Scripts/TD/UI/RuneSelectionUI.cs`

## 流程
- 监听 `GameEvents.RoundEnded(int round)` → 调用 `RunesService.GetOffersForRound(round)` 获取候选。
- 若候选为空或服务缺失：直接 `RaiseRuneSelectionCompleted()`，不阻塞回合。
- 展示 UI（可暂停 `Time.timeScale`），用户点击：
  - 调用 `RunesService.ChooseRune(id)` 应用效果。
  - 触发 `RaiseRuneSelected(id)` 和 `RaiseRuneSelectionCompleted()`。

## UI 细节
- 按稀有度配色显示，按钮 hover 高亮。
- 关闭时恢复 `Time.timeScale`。

## 关键点
- 无论是否有候选，均要保证发出 `RuneSelectionCompleted`，让回合推进。
