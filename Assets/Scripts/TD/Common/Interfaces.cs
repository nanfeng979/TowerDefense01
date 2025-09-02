// 通用生命周期接口，供 UpdateDriver 统一调度

namespace TD.Common
{
    /// <summary>
    /// 初始化接口：服务注册后立即调用一次。
    /// </summary>
    public interface IInitializable
    {
        void Initialize();
    }

    /// <summary>
    /// 销毁接口：游戏关闭或重置时调用。
    /// </summary>
    public interface IDisposableEx
    {
        void Dispose();
    }
}
