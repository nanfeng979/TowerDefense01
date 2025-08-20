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
    /// Update 循环驱动接口。
    /// </summary>
    public interface IUpdatable
    {
        void OnUpdate(float deltaTime);
    }

    /// <summary>
    /// LateUpdate 循环驱动接口。
    /// </summary>
    public interface ILateUpdatable
    {
        void OnLateUpdate(float deltaTime);
    }

    /// <summary>
    /// FixedUpdate 循环驱动接口。
    /// </summary>
    public interface IFixedUpdatable
    {
        void OnFixedUpdate(float fixedDeltaTime);
    }

    /// <summary>
    /// 销毁接口：游戏关闭或重置时调用。
    /// </summary>
    public interface IDisposableEx
    {
        void Dispose();
    }
}
