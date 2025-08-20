# TD.Common.Pooling

包含：
- IPoolable：对象被 Spawn/Despawn 时的回调
- ObjectPool<T>：泛型对象池，适于纯 C# 对象
- GameObjectPool：基于 prefab 的对象池，支持预热、复用与 IPoolable 回调

用法示例：
```csharp
var pool = new GameObjectPool(bulletPrefab, root, prewarm: 20);
var go = pool.Spawn(pos, rot);
// ...
pool.Despawn(go);
```
