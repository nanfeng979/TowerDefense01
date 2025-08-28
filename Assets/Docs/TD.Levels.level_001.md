# TD.Levels.level_001 符文与回合配置说明

位置：`Assets/StreamingAssets/TD/levels/level_001.json`

## 关键片段（当前以你最近编辑为准）
- rounds：
  - round 1：`offerRunes: true`，`rarity: Common`
  - round 2：`offerRunes: true`，`rarity: Common`
  - round 3：`offerRunes: true`，`rarity: Rare`
- runes：
  - `pauseOnSelection: true`（选择时暂停游戏）
  - `useRandomSeed: true` + `randomSeed`（可复现实验）
  - `defaultRarity: "Common"`
  - `autoDowngradeRarity: true`
  - `skipIfInsufficient: false`（不足不跳过，触发回退/重置）
  - `poolIds`: 9 个示例符文，已本地化名称与描述

## 建议
- 每个稀有度配置≥2 个符文，≥3 更佳，以减少重置与回退频率。
- 若希望更流畅：将 `pauseOnSelection` 设为 `false`，或部分回合 `offerRunes: false`。
