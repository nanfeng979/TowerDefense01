using System.Collections.Generic;
using UnityEngine;
using TD.Common.Pooling;
using TD.Common;

namespace TD.Core
{
    /// <summary>
    /// 对象池服务：集中管理 GameObject 池与纯对象池。
    /// 可通过 ServiceContainer 获取与统一清理。
    /// </summary>
    public class PoolService : IInitializable, IDisposableEx
    {
    private readonly Dictionary<string, GameObjectPool> _goPools = new Dictionary<string, GameObjectPool>();

        public void Initialize() { }

        public void Dispose()
        {
            foreach (var p in _goPools.Values) p.Clear();
            _goPools.Clear();
        }

        public GameObjectPool GetOrCreate(string key, GameObject prefab, Transform root = null, int prewarm = 0)
        {
            if (_goPools.TryGetValue(key, out var pool)) return pool;
            pool = new GameObjectPool(prefab, root, prewarm);
            _goPools[key] = pool;
            return pool;
        }

        public bool TryGet(string key, out GameObjectPool pool) => _goPools.TryGetValue(key, out pool);
    }
}
