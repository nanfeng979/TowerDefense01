# TD.Core.ServiceContainer

职责：
- 作为极简服务定位器，仅支持 RegisterSingleton/Resolve/TryResolve
- 用于初始化阶段统一注册：IJsonLoader、IConfigService、UpdateDriver 等

注意：
- 主线程使用，未实现线程安全；不建议在多线程访问
- 仅做初始化阶段与小型项目，后续可替换更完整 DI
