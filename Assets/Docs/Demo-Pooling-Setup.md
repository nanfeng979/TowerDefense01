# 对象池演示搭建指南

目标：在一个空场景内演示 GameObjectPool 的运行，发射子弹与敌人沿路径移动。

步骤：
1) 场景内放置：
   - 空物体 A: 添加 `TD.Core.Bootstrapper`
   - 空物体 B: 添加 `TD.Core.LevelVisualizer`（levelId=001）
2) 子弹演示：
   - 创建一个 Cube 作为子弹 prefab（缩放 0.2,0.2,0.6），添加 `TD.Gameplay.Bullet.Bullet`
   - 场景放置空物体 `Shooter`，挂 `TD.Gameplay.Bullet.BulletShooter`，拖入子弹 prefab，调整 fireRate
3) 敌人演示：
   - 创建一个 Capsule 作为敌人，添加 `TD.Gameplay.Enemy.EnemyMover`（仅需设置 levelId，单路径）
   - 运行后敌人将沿关卡路径移动

说明：
- Bullet 超时会通过回调交给 Shooter 回收
- 若未见路径可视化，确认 `LevelVisualizer` 的 gizmo 设置与 StreamingAssets JSON 是否存在
