using System;
using System.Collections.Generic;

namespace TD.Core
{
    /// <summary>
    /// 简易服务定位器，支持单例注册与获取。
    /// 线程不安全，仅供主线程使用。
    /// </summary>
    public class ServiceContainer
    {
        private static ServiceContainer _instance;
        public static ServiceContainer Instance => _instance ??= new ServiceContainer();

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// 注册服务实例。
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
                throw new InvalidOperationException($"Service {type.Name} already registered");
            _services[type] = service;
        }

        /// <summary>
        /// 以运行时类型注册服务实例。
        /// </summary>
        public void Register(System.Type type, object service)
        {
            if (type == null || service == null) throw new System.ArgumentNullException();
            if (_services.ContainsKey(type))
                throw new InvalidOperationException($"Service {type.Name} already registered");
            _services[type] = service;
        }

        /// <summary>
        /// 获取服务实例。
        /// </summary>
        public T Get<T>() where T : class
        {
            var type = typeof(T);
            if (!_services.TryGetValue(type, out var service))
                throw new InvalidOperationException($"Service {type.Name} not registered");
            return (T)service;
        }

        /// <summary>
        /// 尝试获取服务实例。
        /// </summary>
        public bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>
        /// 通过运行时类型尝试获取服务实例（无需泛型引用）。
        /// </summary>
        public bool TryGet(System.Type type, out object service)
        {
            return _services.TryGetValue(type, out service);
        }

        /// <summary>
        /// 检查服务是否已注册。
        /// </summary>
        public bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 通过运行时类型检查是否已注册。
        /// </summary>
        public bool IsRegistered(System.Type type)
        {
            return _services.ContainsKey(type);
        }

        /// <summary>
        /// 清空所有服务。
        /// </summary>
        public void Clear()
        {
            _services.Clear();
        }

        /// <summary>
        /// 获取所有服务实例（便于批量操作）。
        /// </summary>
        public IEnumerable<object> GetAllServices()
        {
            return _services.Values;
        }
    }
}
