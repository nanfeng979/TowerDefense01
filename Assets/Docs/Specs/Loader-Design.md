# Loader 设计（接口版）

目标：在不实现具体业务的前提下，定义 JSON 加载的接口、约束与最小流程，确保后续代码实现一致。

## 路径约定
- 根目录：`Assets/StreamingAssets/TD`
- 文件：
  - `elements.json` / `towers.json` / `enemies.json`
  - `levels/level_<id>.json`

## 接口
- `TD.Config.IJsonLoader`
  - `Task<T> LoadAsync<T>(string relativePath)`：相对 TD 根目录的路径。
- `TD.Config.IConfigService`
  - `GetElementsAsync()` / `GetTowersAsync()` / `GetEnemiesAsync()` / `GetLevelAsync(levelId)`

## 数据模型
- 参见 `Assets/Scripts/TD/Config/JsonModels.cs`
- 使用 UnityEngine.JsonUtility 初期兼容；如遇需求（字典、可空值）可平滑切换第三方 JSON 库。

## 错误与健壮性（约定）
- 文件缺失：抛出受控异常，业务上兜底（例如加载占位配置或阻止开始游戏）。
- 解析失败：记录错误日志并返回失败；保持线程在主线程上回调（后续实现关注）。
- 版本字段：保留 `version` 便于后续演进（暂不强校验）。

## 性能与缓存
- 首次加载后将结果缓存至内存；关卡切换可选择释放或复用。

## 可视化与调试
- 关卡 gizmos 使用 `grid.showGizmos/gizmoColor`；颜色为 RGBA `#RRGGBBAA`。

本设计为接口草案，待后续实现与验证后再补充。
