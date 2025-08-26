# JSON 规格草案（提案版）

本文件定义塔防项目的首批 JSON 数据结构草案，存放位置建议为 `StreamingAssets/TD/` 目录。请在确认后再开始实现加载与使用代码。

通用规定：
- 文本编码：UTF-8，无 BOM。
- 注释：生产 JSON 不包含注释；本文件中的注释仅作说明。
- 坐标系：关卡内坐标统一以“关卡原点”为参考，单位为米（Unity 单位）。
- 浮点：默认小数，角度单位为度，时间单位为秒，速度为单位/秒，距离为米（Unity 单位）。
- 枚举字符串大小写统一使用小写，以下示例按此规范。
- ID 使用字符串，需在各自域内唯一。

目录建议：
- StreamingAssets/TD/
  - elements.json （元素与克制）
  - towers.json （塔定义）
  - enemies.json （敌人定义）
  - levels/
    - level_001.json （关卡与波次与路径，文件名使用 level_<短ID>.json，如 level_001.json）

---

## 1. 元素系统 elements.json
```json
{
  "version": 1,
  "elements": ["metal", "wood", "water", "fire", "earth"],
  "multipliers": [
    { "attacker": "metal", "defender": "wood",  "mult": 1.25 },
    { "attacker": "wood",  "defender": "water", "mult": 1.25 },
    { "attacker": "water", "defender": "fire",  "mult": 1.25 },
    { "attacker": "fire",  "defender": "earth", "mult": 1.25 },
    { "attacker": "earth", "defender": "metal", "mult": 1.25 }
  ],
  "default": 1.0,
  "countered": 0.8
}
```
说明：
- 完整环：metal→wood→water→fire→earth→metal。
- 若未匹配任何克制规则，使用 `default`（1.0）。若被克关系出现时（反向），使用 `countered`（0.8）。

---

## 2. 塔定义 towers.json（首发 3 塔）
```json
{
  "version": 1,
  "towers": [
    {
      "id": "gun_basic",
      "name": "Basic Gun",
      "element": "metal",
      "cost": 50,
      "range": 6.0,
      "fireRate": 1.0,
      "bullet": {
        "speed": 18.0,
        "damage": 10.0,
        "lifeTime": 2.0
      },
      "targeting": "first"  
    },
    {
      "id": "gun_fast",
      "name": "Fast Gun",
      "element": "wood",
      "cost": 65,
      "range": 5.0,
      "fireRate": 2.0,
      "bullet": { "speed": 20.0, "damage": 6.0, "lifeTime": 2.0 },
      "targeting": "closest"
    },
    {
      "id": "gun_heavy",
      "name": "Heavy Gun",
      "element": "earth",
      "cost": 90,
      "range": 7.5,
      "fireRate": 0.6,
      "bullet": { "speed": 16.0, "damage": 22.0, "lifeTime": 2.5 },
      "targeting": "strongest"
    }
  ]
}
```
说明：
- `targeting` 取值提案：first/last/closest/strongest。

---

## 3. 敌人定义 enemies.json
```json
{
  "version": 1,
  "enemies": [
    {
      "id": "grunt",
      "name": "Grunt",
      "element": "wood",
      "hp": 60.0,
      "moveSpeed": 2.2,
      "bounty": 5
    },
    {
      "id": "runner",
      "name": "Runner",
      "element": "fire",
      "hp": 40.0,
      "moveSpeed": 3.5,
      "bounty": 6
    },
    {
      "id": "tank",
      "name": "Tank",
      "element": "metal",
      "hp": 180.0,
      "moveSpeed": 1.6,
      "bounty": 12
    }
  ]
}
```

---

## 4. 关卡与回合（rounds）level_001.json（单一路径、每回合奖励）
```json
{
  "version": 1,
  "levelId": "001",
  "displayName": "Greenfield",
  "grid": {
    "cellSize": 1.0,
    "width": 20,
    "height": 12,
    "showGizmos": true,
  "gizmoColor": "#00FF00FF"
  },
  "path": {
    "id": "p_main",
    "waypoints": [
      { "x": 1,  "y": 0,  "z": 1 },
      { "x": 5,  "y": 0,  "z": 1 },
      { "x": 5,  "y": 0,  "z": 8 },
      { "x": 15, "y": 0,  "z": 8 }
    ]
  },
  "buildSlots": [
    { "x": 1,  "y": 0, "z": 2,  "type": "ground" },
    { "x": 2,  "y": 0, "z": 2,  "type": "ground" },
    { "x": 6,  "y": 0, "z": 2,  "type": "ground" },
    { "x": 6,  "y": 0, "z": 7,  "type": "ground" },
    { "x": 10, "y": 0, "z": 7,  "type": "ground" },
    { "x": 14, "y": 0, "z": 7,  "type": "ground" }
  ],
  "rounds": {
    "global": { "spawnInterval": 0.8, "roundInterval": 10.0 },
    "list": [
      { "round": 1, "reward": 20, "enemies": ["grunt","grunt","grunt","grunt","grunt","grunt","grunt","grunt"] },
      { "round": 2, "reward": 25, "enemies": ["grunt","grunt","grunt","grunt","runner","runner","runner","runner","runner"], "spawnInterval": 0.7 },
      { "round": 3, "reward": 40, "enemies": ["tank","tank","tank","runner","runner","runner","runner","runner","runner"] }
    ]
  },
  "lives": 20,
  "startMoney": 100
}
```
字段说明：
- grid：
  - `cellSize` 作为统一格子单位（每关可配置）。
  - `width`/`height` 方便编辑与边界判定。
  - `showGizmos`/`gizmoColor`：用于场景中可视化网格；`gizmoColor` 使用 8 位 RGBA 十六进制 `#RRGGBBAA`。
- path.waypoints：折线 Waypoints（世界坐标或关卡局部坐标，建议使用关卡局部）。
- path：单条敌方行进路径；刷怪默认从第一个 waypoint 出生。
- buildSlots：允许建塔的离散格位坐标（不在路径上，且不阻断路径），并用 `type` 区分建造点类型（目前仅支持 `ground`）。
- rounds：
  - `global`：全局生成间隔 `spawnInterval` 与回合间隔 `roundInterval`
  - `list[]`：每回合配置；字段：`round` 序号、`reward` 奖励、`enemies` 敌人类型数组（按顺序生成）、可选 `spawnInterval`（覆盖本回合生成间隔）
- 经济与生命：`startMoney` 初始金币；`lives` 玩家生命数。

---

## 5. 伤害计算提案（简式）
```
finalDamage = baseDamage * elementMultiplier(attacker, defender)
```
- 其中 `elementMultiplier` 按 elements.json 查表：克制 1.25，被克 0.8，否则 1.0。
- 未来可扩展暴击、护甲、穿透、范围衰减等（不在首发范围）。

---

请审阅以上草案：若认可我将据此生成示例 JSON 文件与加载器接口定义；若需调整，请标注字段或提供偏好，我会更新草案并记录变更。

---

附：waves → rounds 迁移说明（v2025-08-26）
- 旧：`waves[]`（含 startTime、groups[count,enemyId,spawnInterval,delay]）
- 新：`rounds { global{spawnInterval,roundInterval}, list[{round,reward,enemies[],spawnInterval?}] }`
- 迁移：
  1) 删除 `waves`，新增 `rounds` 节点
  2) 将每个波的敌人展开为回合 `enemies` 数组；组内 `count` → 重复 N 次该 `enemyId`
  3) 组/波的 `spawnInterval` 迁到 round.spawnInterval 或 rounds.global.spawnInterval
  4) `startTime` 由 `roundInterval` 控制相邻回合间隔（或在 Spawner 中自定义）
