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
    - level_001.json （关卡与波次与路径）

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

## 4. 关卡与波次 level_001.json（支持多路径、多建造点类型、每波奖励）
```json
{
  "version": 1,
  "levelId": "level_001",
  "displayName": "Greenfield",
  "grid": {
    "cellSize": 1.0,
    "width": 20,
    "height": 12,
    "showGizmos": true,
    "gizmoColor": "#00FF00"
  },
  "paths": [
    {
      "id": "p_main",
      "waypoints": [
        { "x": 1,  "y": 0,  "z": 1 },
        { "x": 5,  "y": 0,  "z": 1 },
        { "x": 5,  "y": 0,  "z": 8 },
        { "x": 15, "y": 0,  "z": 8 }
      ]
    },
    {
      "id": "p_alt",
      "waypoints": [
        { "x": 2,  "y": 0,  "z": 0 },
        { "x": 2,  "y": 0,  "z": 6 },
        { "x": 12, "y": 0,  "z": 6 },
        { "x": 18, "y": 0,  "z": 10 }
      ]
    }
  ],
  "buildSlots": [
    { "x": 1,  "y": 0, "z": 2,  "type": "ground" },
    { "x": 2,  "y": 0, "z": 2,  "type": "ground" },
    { "x": 6,  "y": 0, "z": 2,  "type": "ground" },
    { "x": 6,  "y": 0, "z": 7,  "type": "ground" },
    { "x": 10, "y": 0, "z": 7,  "type": "ground" },
    { "x": 14, "y": 0, "z": 7,  "type": "ground" }
  ],
  "waves": [
    {
      "wave": 1,
      "startTime": 0.0,
      "reward": 20,
      "groups": [
        { "enemyId": "grunt",  "count": 8,  "spawnInterval": 0.8, "pathId": "p_main" }
      ]
    },
    {
      "wave": 2,
      "startTime": 12.0,
      "reward": 25,
      "groups": [
        { "enemyId": "grunt",  "count": 6,  "spawnInterval": 0.7, "pathId": "p_main" },
        { "enemyId": "runner", "count": 5,  "spawnInterval": 0.6, "delay": 2.0, "pathId": "p_alt" }
      ]
    },
    {
      "wave": 3,
      "startTime": 26.0,
      "reward": 40,
      "groups": [
        { "enemyId": "tank",   "count": 3,  "spawnInterval": 1.5, "pathId": "p_main" },
        { "enemyId": "runner", "count": 6,  "spawnInterval": 0.6, "delay": 3.0, "pathId": "p_alt" }
      ]
    }
  ],
  "lives": 20,
  "startMoney": 100
}
```
字段说明：
- grid：
  - `cellSize` 作为统一格子单位（每关可配置）。
  - `width`/`height` 方便编辑与边界判定。
  - `showGizmos`/`gizmoColor`：用于场景中可视化网格，便于对齐与摆放。
- path.waypoints：折线 Waypoints（世界坐标或关卡局部坐标，建议使用关卡局部）。
- paths：多条敌方行进路径；刷怪默认从每条路径的第一个 waypoint 出生。
- groups.pathId：指明该组敌人沿哪条路径移动；可扩展 `startIndex` 指定从路径中途出生。
- buildSlots：允许建塔的离散格位坐标（不在路径上，且不阻断路径），并用 `type` 区分建造点类型（如 `ground`）。
- waves：
  - `wave`：序号；`startTime`：该波起始时间（相对关卡开始）。
  - `groups`：本波内多个出怪组，按 `delay`（可选）相对本波开始的延迟触发。
  - `spawnInterval`：该组内相邻单位的间隔。
  - `reward`：该波清算奖励（金币）。
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
