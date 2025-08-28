# TD.Runes.Flow 符文选择阶段端到端流程

```text
[回合清场完成]
   │
   ├─ RoundSpawner → RaiseRoundRewardGranted
   ├─ RoundSpawner → RaiseRoundEnded(round)
   │
   ├─ RuneSelectionUI.OnRoundEnded(round)
   │      ├─ offers = RunesService.GetOffersForRound(round)
   │      ├─ if offers.Count == 0 → RaiseRuneSelectionCompleted()
   │      └─ else → 打开UI（可暂停）
   │             └─ 玩家点击：
   │                    ├─ RunesService.ChooseRune(id)
   │                    ├─ RaiseRuneSelected(id)
   │                    └─ RaiseRuneSelectionCompleted()
   │
   └─ RoundSpawner 等待 RuneSelectionCompleted → 继续下一回合（如非最后一回合则等待 roundInterval）
```

## 异常/边界
- 无符文/不提供：RunesService 或 UI 直接触发完成事件。
- 符文池不足：RunesService 重置当前稀有度池；不足 2 个时记录警告；最终回退混选。
- UI 被销毁/服务缺失：UI 捕获并直接触发完成事件以解锁流程。
