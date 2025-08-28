# TD.Gameplay.RoundSpawner 回合控制与等待机制

位置：`Assets/Scripts/TD/Gameplay/Spawn/RoundSpawner.cs`

## 主要职责
- 依关卡配置顺序生成敌人；按全局/回合 `spawnInterval`；回合间 `roundInterval`。
- 清场检测：等待 `EnemyRegistry.All.Count == 0`。
- 回合收尾：发奖励 → 广播 `RoundEnded` → 等待 `RuneSelectionCompleted` → 间隔后进入下一回合。

## 新增/修改点
- 事件订阅：在 Start 中订阅 `GameEvents.RuneSelectionCompleted`，在 OnDisable 中取消订阅。
- 等待标记：`_waitingForRuneSelection` 控制等待循环，日志记录进入/退出等待。
- 回合间隔：只在“非最后一回合且完成符文阶段”后才执行等待间隔。

## 小贴士
- 若需要“立即进入下一回合”体验，可在关卡配置中将回合设为不提供符文或让 UI 不暂停。
