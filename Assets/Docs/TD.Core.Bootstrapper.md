# TD.Core.Bootstrapper

职责：
- 在场景中挂载，进行一次最小的配置加载与交叉校验，验证 JSON 与数据模型匹配。
- 后续将迁移为更完整的服务容器与系统装配；本脚本仅用于当前阶段验证数据管线。

关键点：
- 依赖接口：`TD.Config.IConfigService`、`TD.Config.IJsonLoader`
- 只做一次 `Start()` 异步验证，不参与游戏循环。

注意：
- Android 平台 StreamingAssets 走 UWR；PC/Editor 直接 File IO。
