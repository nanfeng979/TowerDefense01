# TD.Core.GameEvents 事件总览（新增整理）

集中管理全局事件：解耦“回合 → 敌人生成 → 奖励 → 符文系统 → 下一回合”。

## 事件列表
- RoundEnded(int round)：某回合结束（清场完成）。触发自 RoundSpawner。
- RoundRewardGranted(int reward)：回合奖励已发放。触发自 RoundSpawner。
- EnemySpawned(EnemyAgent agent)：生成单个敌人后触发。触发自 RoundSpawner。
- RuneSelected(string runeId)（新增）：玩家在符文选择界面中选择某符文。触发自 RuneSelectionUI。
- RuneSelectionCompleted()（新增）：符文选择阶段结束（选择或跳过）。触发自 RuneSelectionUI 和 RunesService（在无符文/不提供时）。

## 典型时序
1) RoundSpawner 清场完成 → RaiseRoundRewardGranted → RaiseRoundEnded。
2) RuneSelectionUI 监听 RoundEnded，若有可选符文则弹窗，否则直接 RaiseRuneSelectionCompleted。
3) 玩家选择或跳过 → RaiseRuneSelected（可选）→ RaiseRuneSelectionCompleted。
4) RoundSpawner 监听 RuneSelectionCompleted，结束等待，进入下一回合。
