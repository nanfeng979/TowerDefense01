# TD.Core.UpdateDriver

职责：
- 集中驱动 IUpdatable/ILateUpdatable/IFixedUpdatable，减少分散 Update

使用：
- 由 Bootstrapper 自动挂载并注册为单例；业务系统可从容器解析并注册自身
