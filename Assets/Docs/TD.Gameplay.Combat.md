# TD.Gameplay.Combat

包含最小战斗组件：

- IDamageable：可受伤害接口，统一 `Damage(float)` 入口。
- Health：实现 `IDamageable`，提供 `Damage(float)`、`OnDeath`、`OnDamaged`，可配置 `destroyOnDeath`。
- Bullet 命中：Collider.IsTrigger 命中后通过 `GetComponent<IDamageable>()?.Damage(damage)` 调用接口，性能优于 SendMessage。

使用指南：
1) 在敌人 prefab 上添加 `Health` 组件，设置 maxHp。
2) 子弹 prefab：添加 Collider（IsTrigger=true），Rigidbody（可设 Kinematic）。
3) 物理层：确保子弹与敌人所在 Layer 的碰撞矩阵允许触发器接触。
4) 命中后：目标若实现 IDamageable（例如 Health），将执行 Damage 并在 HP<=0 触发死亡（默认 Destroy）。

可扩展：
- 如需扩展命中反馈，可在 Bullet 命中处增加接口判定（如 IKnockbackable 等）并分发。
- 元素克制、护甲、暴击等在后续引入数据驱动后叠加。
