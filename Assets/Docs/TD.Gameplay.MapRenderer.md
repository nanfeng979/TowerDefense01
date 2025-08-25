# TD.Gameplay.MapRenderer

用途：根据 Level JSON 配置渲染可见的地面、路径与建造点，占位用 Primitive/LineRenderer，后续可替换为美术预制。

关键特性：
- 读取 `StreamingAssets/TD/levels/level_<id>.json`，无 Bootstrapper 时也能独立生成
- 地面：按 grid(width,height,cellSize) 生成 Plane 或瓦片阵列
- 路径：优先使用 `pathTilePrefab` 沿路径段步进铺设；若缺省则使用 `LineRenderer` 绘制
- 建造点：实例化 `buildSlotPrefab` 或方块占位
- 一键生成：Inspector 右键 “Generate Map Now”；可配置自动生成与清理

组件参数：
- levelId: 关卡 ID（示例 001）
- autoGenerateOnStart / clearBeforeGenerate / yOffset
- 预制与材质：groundTilePrefab、pathTilePrefab、buildSlotPrefab、pathLineMaterial
- 渲染：scaleGroundToCell、pathStepMultiplier、lineWidthScale

用法：
1) 在场景创建空物体并添加 `MapRenderer`
2) 设置 levelId、必要的预制体；无预制也可直接预览（Plane/LineRenderer/Cube）
3) 运行或点击 “Generate Map Now” 生成

注意：
- 生成内容位于组件下的 [Ground]/[Path]/[BuildSlots] 子节点；勾选 clearBeforeGenerate 会先清空
- 路径步进密度由 pathStepMultiplier 决定；LineRenderer 宽度由 lineWidthScale 决定
