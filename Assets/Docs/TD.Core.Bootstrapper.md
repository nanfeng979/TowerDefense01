# TD.Core.Bootstrapper

职责：
- 游戏启动器：统一初始化服务容器、UpdateDriver 与核心服务。
- 场景中唯一挂载，负责依赖装配与生命周期管理。

关键点：
- 依赖接口：`TD.Config.IConfigService`、`TD.Config.IJsonLoader`
- 自动创建 UpdateDriver 并注册实现生命周期接口的服务
- 预热配置加载并输出摘要信息
- OnDestroy 时清理所有服务

注意：
- 替换了原来的一次性校验，现在专注于服务初始化
- 校验功能移至 LevelVisualizer.Inspector 进行
