# 最近改动总览与导航（2025-08）

本目录整理了近期对项目的重要改动：
- 新增全局事件以驱动“回合结束 → 符文选择 → 下一回合”的流程。
- 回合收尾逻辑：回合结束后必须等待符文选择完成才进入下一回合。
- 符文系统：按稀有度的池重置、短缺报警、候选回退策略与本地化。
- UI：符文选择界面在无可选/取消时也会发出完成信号，防止流程阻塞。
- 关卡配置：`level_001.json` 的符文参数与实用建议。

请按需阅读以下专题文档：
- [TD.Core.GameEvents 事件新增与时序](./TD.Core.GameEvents.md)
- [TD.Core.RunesService 符文供给/选择与池管理](./TD.Core.RunesService.md)
- [TD.UI.RuneSelectionUI 交互与完成信号](./TD.UI.RuneSelectionUI.md)
- [TD.Gameplay.RoundSpawner 回合控制与等待机制](./TD.Gameplay.RoundSpawner.md)
- [TD.Levels.level_001 符文与回合配置说明](./TD.Levels.level_001.md)
- [TD.Runes.Flow 符文选择阶段端到端流程](./TD.Runes.Flow.md)

维护建议：
- 扩展流程优先通过 GameEvents 新增事件，保持模块解耦。
- 配置层建议每个稀有度≥2 个符文，避免频繁回退/报警。
