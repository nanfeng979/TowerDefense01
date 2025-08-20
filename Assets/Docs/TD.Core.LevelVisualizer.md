# TD.Core.LevelVisualizer

用途：在编辑器与运行时以 Gizmos 展示关卡网格、路径与建造点，校对 JSON 坐标与网格对齐。

要点：
- [ExecuteAlways] 支持编辑器下懒加载（EditorApplication.update）
- 网格颜色读取 `grid.gizmoColor`（#RRGGBBAA），失败退回绿色
- 以组件所在物体 Transform.position 作为关卡原点

使用：
- 将脚本挂到场景空物体；填写 `levelId`（例如 001）
- 在 Scene 视图中可见网格、路径线段与建造点方块
 - 自定义 Inspector：点击 "Validate Now" 一键执行校验并显示 Summary 与 Issues
