# Coding Standards（项目编码规范，v0.1）

适用范围：本仓库所有 C# 与 JSON 文件。

## 通用原则
- 类保持单一职责；必要时用 partial 拆分文件。
- 命名空间根：`TD`；按模块分层 `TD.Common`, `TD.Core`, `TD.Gameplay`, `TD.UI`, `TD.Config`。
- MonoBehaviour 仅承载生命周期与转发，核心逻辑放纯 C# 类。
- 公共逻辑下沉到 `Assets/Scripts/Common`。
- 仅写有用代码，移除死代码与未使用成员。

## C# 规范
- 文件名与类名一致；一个文件一个顶级类型。
- 接口以 `I` 前缀；异步用 `Async` 后缀；事件用动词过去式 `OnXxx`。
- 字段私有 `_camelCase`；属性 `PascalCase`；常量 `SCREAMING_SNAKE_CASE`。
- 使用 `using` 命名空间别名谨慎；禁止通配 `using static`。
- 注释：类/公共成员用 XML 注释；重要方法给出摘要与参数/返回说明。
- 性能：优先对象池；避免在 Update/GC 热路径分配；使用 `readonly struct` where applicable。

## JSON 规范
- 编码 UTF-8；不包含注释。
- 字段名统一小写加驼峰：`startTime`（单路径模式已不使用 pathId）。
- 颜色：使用 RGBA 8位 HEX `#RRGGBBAA`。
- ID 为字符串并在域内唯一。

## 目录与资源
- StreamingAssets 放置配置：`Assets/StreamingAssets/TD`。
- 文档放 `Assets/Docs`，重要变更需在 Progress.md 记录。

## 提交规范
- 提交信息格式：`type: summary`，type 取值：`feat|fix|docs|refactor|perf|chore`。
- 中文为主，必要时附英文关键词。

## 测试与质量
- 先不强制单元测试；但新增公共 API 需最小示例或使用说明。
- 代码审查关注：命名、职责、注释、依赖、分配热点、对象池使用。
