# TD.Core.ServiceContainer

职责：简易服务定位器，支持单例服务注册与获取。

特点：
- 线程不安全，仅限主线程使用
- 静态实例访问：ServiceContainer.Instance
- 类型安全：Register<T>、Get<T>、TryGet<T>
- 批量操作：GetAllServices() 供 Bootstrapper 初始化时使用

使用：
```csharp
var container = ServiceContainer.Instance;
container.Register<IConfigService>(new ConfigService(...));
var config = container.Get<IConfigService>();
```
