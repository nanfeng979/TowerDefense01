# TD.Core.UpdateDriver

职责：统一生命周期驱动器，集中调用所有服务的 Update/LateUpdate/FixedUpdate。

特点：
- 单例 MonoBehaviour，DontDestroyOnLoad
- 减少单独 MonoBehaviour 的 Update 开销
- 自动注册实现 IUpdatable/ILateUpdatable/IFixedUpdatable 的服务

使用：
- 由 Bootstrapper 自动创建
- 服务实现相应接口即可被自动驱动
- 支持运行时注册/移除
