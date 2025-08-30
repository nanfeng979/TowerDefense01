# 游戏架构重构需求文档

## 概述
将游戏重构为单场景架构，通过UI状态管理控制不同界面的显示和切换。

## 核心架构设计

### 场景结构
- **单一场景**: 整个游戏只有一个Scene
- **游戏生命周期控制器**: Scene挂载一个脚本控制整个游戏的生命周期
- **UI状态管理**: 通过预制体的激活/隐藏来实现界面切换

### 界面流程设计

#### 1. 加载界面 (Loading)
- **触发时机**: 游戏启动时首先显示
- **功能**: 显示加载进度，初始化游戏服务
- **结束条件**: 所有服务初始化完成后自动关闭

#### 2. 首页界面 (MainMenu)
- **触发时机**: 加载界面关闭后自动打开
- **UI元素**: 
  - 关卡选择按钮
  - (可扩展: 设置按钮、商店按钮等)
- **交互**: 点击关卡选择按钮打开关卡选择界面

#### 3. 关卡选择界面 (LevelSelection)
- **实现方式**: 独立预制体
- **UI元素**:
  - N个关卡按钮 (第1关、第2关、...、第N关)
  - 返回按钮
- **交互**:
  - 点击关卡按钮: 打开对应关卡
  - 点击返回按钮: 关闭当前预制体，返回首页

#### 4. 关卡游戏界面 (GameLevel)
- **实现方式**: 每个关卡独立预制体
- **UI元素**:
  - 游戏内UI (血量、金币、波次等)
  - 关闭/退出按钮
- **交互**:
  - 点击关闭按钮: 关闭当前关卡，返回关卡选择界面

## 技术实现要点

### UI管理系统
- **UIManager**: 统一管理所有UI界面的显示/隐藏
- **UI状态枚举**: Loading, MainMenu, LevelSelection, GameLevel, ConfirmDialog
- **UI栈管理**: Stack<UIPanel> 支持多层界面叠加和层级返回
- **模态对话框**: 支持在任意界面上叠加确认对话框、设置面板等
- **输入管理**: ESC键/返回键的层级处理逻辑

### 预制体管理
- **按需加载**: 当前阶段使用Resources或Addressables按需加载
- **内存管理**: 适时释放不需要的预制体，避免内存泄漏
- **层级管理**: Canvas层级和Sorting Order管理
- **未来扩展**: 预留接口供后续高级预加载系统集成

### 游戏生命周期控制
- **GameController**: 主要的生命周期管理脚本
- **游戏状态机**: 管理游戏的不同状态 (Menu, Playing, Paused)
- **暂停管理**: Time.timeScale控制 + 暂停状态UI
- **事件系统**: 界面间的通信机制

### 动画系统 (后续实现)
- **过渡动画库**: 参考卡普空等大厂的UI过渡效果
- **动画类型**: 淡入淡出、滑动、缩放、翻转等
- **动画队列**: 支持动画序列和并行执行
- **性能优化**: 对象池复用动画组件

## 需要确认的问题

### 1. UI管理方式 ✅已确认
- **决策**: 实现UI栈管理，支持多层界面叠加
- **技术要点**: 
  - Stack<UIPanel> 管理界面层级
  - 支持模态对话框叠加在游戏界面上
  - 实现Back键/ESC键的层级返回逻辑

### 2. 预制体加载策略 ✅已确认
- **决策**: 暂时采用按需加载，未来实现高级预加载系统
- **未来规划**: 
  - 加载队列管理器 (LoadQueue)
  - 优先级排序机制 (Priority-based loading)
  - 抢先加载策略 (Preemptive loading)
  - 智能预测加载 (Predictive loading)

### 3. 关卡数据管理 ✅已确认
- **决策**: 继续使用JSON配置文件存储关卡数据
- **实现方式**: 扩展现有ConfigService支持关卡配置
- **配置结构**: StreamingAssets/TD/levels/level_{id}.json

### 4. 界面切换动画 ✅已确认
- **决策**: 需要实现界面切换过渡动画
- **参考标准**: 卡普空等大厂的过渡方式
- **实现优先级**: 后续阶段实现，先搭建基础架构
- **技术方案**: DOTween + 自定义过渡效果库

### 5. 游戏暂停机制 ✅已确认
- **决策**: 关卡内需要暂停机制
- **交互流程**: 点击关闭按钮 → 暂停游戏 → 显示确认对话框
- **技术实现**: Time.timeScale = 0 + 模态确认对话框

## 实现优先级

### 阶段1: 核心架构 (立即开始)
1. **GameController**: 游戏生命周期主控制器
   - 游戏状态管理 (Menu, Playing, Paused)
   - 场景初始化和服务集成
2. **UIManager**: UI栈管理系统
   - Stack<UIPanel> 实现
   - 界面显示/隐藏逻辑
   - ESC键层级返回处理
3. **UIPanel基类**: 所有UI界面的基础类
   - 显示/隐藏生命周期
   - 栈管理接口实现

### 阶段2: 基础界面 (核心架构完成后)
1. **加载界面**: 复用现有Bootstrapper逻辑
2. **首页界面**: MainMenu预制体实现
3. **关卡选择界面**: LevelSelection预制体实现
4. **确认对话框**: ConfirmDialog模态对话框
5. **界面切换逻辑**: 基础的显示/隐藏切换

### 阶段3: 关卡集成 (界面完成后)
1. **关卡配置**: 扩展ConfigService支持level_{id}.json
2. **关卡预制体**: GameLevel独立预制体结构
3. **游戏暂停系统**: Time.timeScale + 确认对话框集成
4. **现有符文系统**: 适配新的UI栈架构

### 阶段4: 动画和优化 (功能完整后)
1. **过渡动画系统**: DOTween + 自定义动画库
2. **高级预加载系统**: 队列管理器和优先级调度
3. **性能优化**: 内存管理和对象池
4. **音效集成**: UI音效和背景音乐管理

## 待讨论事项
1. 是否保留现有的符文选择UI，还是重新设计为关卡内的UI元素？
2. 存档系统如何集成到新的架构中？
3. 音效和背景音乐的管理策略？

请您review以上需求分析，并告诉我您的想法和偏好，特别是标记为"您的偏好: ?"的问题。

---

## 核心组件详细设计（草案）

以下为核心组件的职责、关键接口与交互流程，基于已确认的需求（UI栈、按需加载、JSON关卡、过渡动画后做、暂停+确认对话框）。

### 1. GameController（单场景生命周期总控）
- 职责：
  - 统一驱动游戏状态（Menu, Playing, Paused）
  - 首屏显示 Loading → 等待服务初始化 → 进入 MainMenu
  - 关卡进入/退出全流程协调（与 UIManager、LevelLoader 协作）
- 依赖：ServiceContainer（已有）、UIManager、IAssetProvider（下文）
- 关键接口（示意）：
  - InitializeAsync(): Task
  - EnterMainMenu(): void
  - EnterLevel(int levelId): Task
  - ExitLevel(): Task
  - Pause(): void / Resume(): void
  - OnBackPressed(): void（路由给 UIManager 或当前顶层面板）
- 事件：
  - OnLevelEntered(int levelId), OnLevelExited(int levelId)

### 2. UIManager（界面栈与显示协调）
- 职责：
  - 维护 Stack<UIPanel>；支持多层界面叠加、模态对话框
  - 统一的 Push/Pop/Replace/BringToFront 操作
  - 输入路由：ESC/返回键优先询问栈顶面板是否处理
  - 资源按需加载：加载面板预制体并实例化（Addressables/Resources 二选一）
- 关键接口：
  - Task<TPanel> PushAsync<TPanel>(string key, object args = null, bool modal = false)
  - Task<bool> PopAsync()（返回是否成功弹出）
  - Task ReplaceAsync<TPanel>(string key, object args = null)
  - int Count { get; }
  - UIPanel Top { get; }
- 行为约定：
  - 模态面板阻断下层交互；可选全屏半透明遮罩
  - 面板的 OnShow/OnHide 返回 Task，用于后续接入过渡动画

### 3. UIPanel（所有 UI 面板的抽象基类）
- 字段与属性：
  - bool IsModal, bool BlocksRaycast
  - Canvas / CanvasGroup / Raycaster 引用
  - string PanelKey（资源定位键：Addressable 地址或 Resources 路径）
- 生命周期：
  - Task OnShowAsync(object args)
  - Task OnHideAsync()
  - bool OnBackRequested()（返回 true 表示已消费返回键）
- 动画扩展点：
  - 预留 TransitionHandle 或 ITransition 接口（后续对接过渡动画系统）

### 4. ConfirmDialog（通用模态确认对话框）
- 职责：暂停流（由调用方控制 Time.timeScale），提供确认/取消
- 接口：
  - Task<bool> ShowAsync(string title, string message, string confirmText = "确定", string cancelText = "取消")
- 行为：
  - 打开时置顶为栈顶；ESC 等价于取消

### 5. LevelLoader / IAssetProvider（关卡与面板的统一加载抽象）
- 现阶段：按需加载（Addressables 或 Resources）
- 接口（示意）：
  - Task<GameObject> LoadPrefabAsync(string key)
  - void Release(object handleOrKey)
- 未来扩展：
  - LoadQueue：支持优先级、抢占、顺序
  - 预热接口：PreloadAsync(IEnumerable<string> keys, Priority p)

### 6. LevelSelectionPanel（关卡选择面板）
- 数据来源：StreamingAssets/TD/levels/index.json（列出关卡 id、名称、地址）
- 行为：
  - 动态生成 N 个关卡按钮 → 点击调用 GameController.EnterLevel(id)
  - 返回按钮：Pop 当前面板，回到 MainMenu

### 7. LevelController（关卡预制体上的驱动脚本）
- 职责：
  - 承担本关卡 GameObject 生命周期；只保留必要 Mono 行为
  - Close 按钮：调用 GameController.ExitLevel()
  - 关卡内部暂停菜单可选（与全局 ConfirmDialog 复用）

### 8. 输入与返回键路由
- 全局监听 ESC/Back：
  - 若 UI 栈非空：询问 Top.OnBackRequested()；若未消费则 PopAsync()
  - 若处于关卡中且栈为空：调用 GameController.Pause() → ConfirmDialog → 按选择决定继续或退出关卡

### 9. 暂停机制与确认对话框流程
- 规范流程：
  - 用户点击关卡内“关闭”按钮 → GameController.Pause()（Time.timeScale = 0）
  - UIManager.PushAsync<ConfirmDialog>() 并 ShowAsync(...)
  - 若确认：ExitLevel()；若取消：Resume()，关闭对话框

### 10. 关卡数据（JSON）建议结构
- 路径：StreamingAssets/TD/levels/
- index.json：
  - levels: [ { id:int, name:string, prefabKey:string, preview:string? } ]
- level_{id}.json（可选）：
  - 敌人波次、初始资源、地图参数（可后续集成现有配置服务）

### 11. 资源定位约定
- Addressables：prefabKey 为地址（推荐）
- Resources：prefabKey 为资源路径（备用）
- UI 面板亦遵循相同 key 规则，便于统一加载

### 12. 错误与边界情况
- 缺少预制体或加载失败：
  - UIManager 需要回退与错误提示；禁止把空对象入栈
- 快速重复点击：
  - UIManager 内部做并发保护（每个 key 的加载去重）
- 移动端返回键：
  - 等价 ESC，走统一路由

> 注：以上接口均为草案，具体签名可在实现时根据 Unity 协程/Task 的兼容性细化。
