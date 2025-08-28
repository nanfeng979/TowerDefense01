# TD.Core.RunesService 符文系统（供给与池管理）

位置：`Assets/Scripts/TD/Core/RunesService.cs`

## 能力
- GetOffersForRound(round)：按目标稀有度提供 3 个候选；不足时重置当前稀有度的剩余池；仍不足按策略回退。
- ChooseRune(id)：应用符文效果，从剩余池移除已选符文。
- OfferRunesForRound(round)：当本回合不提供符文或配置缺失时，直接触发 `GameEvents.RaiseRuneSelectionCompleted()` 以推进流程。

## 稀有度与池重置
- 若当前稀有度可用 < 3，检查配置中该稀有度的总量：
  - ≥3：重置该稀有度的剩余池（只重置该稀有度）。
  - <2：记录警告（建议每个稀有度至少 2 个符文）。
- 若仍不足且允许降级（`autoDowngradeRarity`），按 Epic → Rare → Common 顺序尝试。
- 最终回退：从所有剩余中混选；仍不足则使用配置中全部可用符文作为候选来源。

## 日志与诊断
- 全流程 Debug 日志：目标稀有度、池大小、重置行为、回退决策。
