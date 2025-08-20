using System;
using System.Collections.Generic;

namespace TD.Core
{
    /// <summary>
    /// 极简服务容器实现：线程不安全，主线程使用；仅用于本项目初始化阶段。
    /// </summary>
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, object> _map = new Dictionary<Type, object>();

        public void RegisterSingleton<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var t = typeof(T);
            _map[t] = instance;
        }

        public T Resolve<T>() where T : class
        {
            if (TryResolve<T>(out var s)) return s;
            throw new InvalidOperationException($"Service not found: {typeof(T).Name}");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (_map.TryGetValue(typeof(T), out var obj))
            {
                service = obj as T;
                return service != null;
            }
            service = null;
            return false;
        }
    }
}
