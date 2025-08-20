using System;

namespace TD.Core
{
    /// <summary>
    /// 极简服务容器接口，仅支持单例注册与解析。
    /// </summary>
    public interface IServiceContainer
    {
        void RegisterSingleton<T>(T instance) where T : class;
        T Resolve<T>() where T : class;
        bool TryResolve<T>(out T service) where T : class;
    }
}
