using System;
using System.Collections.Generic;

namespace TD.Common.Pooling
{
    /// <summary>
    /// 泛型对象池：适用于纯 C# 对象（非 UnityEngine.Object）。
    /// 通过工厂与重置委托创建与回收。
    /// </summary>
    public class ObjectPool<T>
    {
        private readonly Stack<T> _stack = new Stack<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;

        public int CountInactive => _stack.Count;

        public ObjectPool(Func<T> factory, Action<T> reset = null, int prewarm = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
            Prewarm(prewarm);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _stack.Push(_factory());
            }
        }

        public T Get()
        {
            return _stack.Count > 0 ? _stack.Pop() : _factory();
        }

        public void Release(T item)
        {
            _reset?.Invoke(item);
            _stack.Push(item);
        }

        public void Clear()
        {
            _stack.Clear();
        }
    }
}
