using System.Collections.Generic;
using UnityEngine;

namespace TD.Common.Pooling
{
    /// <summary>
    /// GameObject 池：通过 prefab 实例化，统一管理激活/隐藏与 IPoolable 回调。
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Stack<GameObject> _stack = new Stack<GameObject>();

        public int CountInactive => _stack.Count;

        public GameObjectPool(GameObject prefab, Transform root = null, int prewarm = 0)
        {
            _prefab = prefab;
            _root = root;
            Prewarm(prewarm);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Object.Instantiate(_prefab, _root);
                go.SetActive(false);
                _stack.Push(go);
            }
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject go = _stack.Count > 0 ? _stack.Pop() : Object.Instantiate(_prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            foreach (var comp in go.GetComponents<IPoolable>()) comp.OnSpawned();
            return go;
        }

        public void Despawn(GameObject go)
        {
            foreach (var comp in go.GetComponents<IPoolable>()) comp.OnDespawned();
            go.SetActive(false);
            if (_root != null) go.transform.SetParent(_root, false);
            _stack.Push(go);
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                var go = _stack.Pop();
                if (go != null) Object.Destroy(go);
            }
        }
    }
}
