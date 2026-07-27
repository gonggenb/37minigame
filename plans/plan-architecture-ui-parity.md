# 新架构完整复刻旧版 GUI

## 需求描述

以 `Assets/Scenes/MainPrototype_Architecture.unity` 作为主场景，在保留新架构 `RunManager`、`CharacterManager`、`BattleRunner`、CSV 数据库和三条核心时间规则的前提下，完整复刻 `MainPrototype.unity` 中 `PrototypeHUDController` 与 `BattleScreenController` 的界面效果和交互能力。

本需求覆盖主菜单、主地图 HUD、角色状态、装备背包、设置、升级三选一、结算、调试入口、地图敌人等级和全屏战斗表现。玩家接触敌人后必须进入独立的全屏战斗表现，显示背景、双方角色、名称、等级、气血和战斗反馈；战斗结束后按新架构状态机返回正确阶段。

测试由用户在后续独立测试任务中执行。本轮开发不得调用 Unity MCP、进入 Play Mode 或直接修改 Scene、Prefab 和 Inspector 序列化状态。

## 实现方案

### 方案选择

采用“新架构 uGUI 完整复刻”方案，不重新启用旧 `GameFlowController`、`BattleManager`、`PrototypeHUDController` 或 `BattleScreenController`。

不采用以下方案：

- 不复制一套新的 IMGUI 控制器，避免长期维护两套界面实现。
- 不重新启用旧流程，避免新旧战斗、奖励、计时和状态机同时运行。

### UI 架构

- 保留 `GameUiPresenter` 作为界面状态入口，集中订阅 `RunManager`、`CharacterManager` 和 `BattleRunner`。
- 扩展现有 `MainMenuView`、`HudView`、`BattleView`、`LevelUpView` 和 `ResultView`，使布局、文本层级、颜色、面板关系和旧 GUI 对齐。
- 新增角色面板、设置面板和调试面板 View，分别负责角色属性/武学、装备背包、暂停设置和开发调试命令。
- 主地图 HUD 仅在允许显示的非战斗阶段启用；进入普通战斗、洞穴战斗或 Boss 战时隐藏主地图 HUD 和角色面板，显示覆盖全屏的战斗面板。
- UI 只读取新架构状态和数据，不直接调用旧组件。

### 战斗进入与表现链路

```text
WorldInteractionTrigger
  -> EnemyEncounter
  -> RunManager.TryBeginNormalBattle / TryBeginCaveBattle
  -> BattleRunner.BeginBattle
  -> RunManager / BattleRunner 事件
  -> GameUiPresenter
  -> 全屏 BattleView
```

- `EnemyEncounter` 只有在 `RunManager` 接受战斗请求后才被消费并隐藏，拒绝请求时保持可交互。
- `BattleRunner` 对外提供当前敌人的配置、显示名称、视觉 ID、显示等级和战斗快照。
- `GameUiPresenter` 同时监听运行状态和战斗变化，避免状态已经切入战斗但战斗对象尚未刷新时漏开战斗面板。
- 战斗面板使用现有 Catalog/视觉 ID 加载玩家、敌人和背景资源；缺少美术引用时保留气血、名称、等级和日志，不得阻断战斗流程。
- 普通战斗结束返回主地图或进入升级/Boss；洞穴战斗结束返回洞穴；Boss 战结束进入结算。

### 敌人等级

- 在 Domain/Application 可测试层新增与旧 `CombatantStats.DisplayLevel` 一致的等级换算规则：显式等级大于零时使用显式值，否则根据最大气血、防御、攻击和攻速计算并限制为 `1..99`。
- 新架构地图敌人等级标签从 `EnemyEncounter` 的角色配置读取，不再依赖已被移除的旧 `EncounterTrigger`。
- 战斗界面显示玩家当前境界和敌人显示等级。
- Editor 自动构建器为 Architecture 敌人 Prefab 配置新等级标签所需引用；实际 Prefab 写入留到独立 Editor 工作任务。

### 角色、武学与装备界面

- `CharacterManager` 增加只读装备库存，开局装备和后续装备奖励均进入库存。
- 保持当前自动换装行为，但换下的装备继续保留在库存中，装备面板可查看并手动切换。
- 角色状态页显示气血、攻击、防御、攻速、暴击、闪避、吸血、移速和已学武学等级。
- 装备页显示武器、防具、饰品槽位、库存、品质、属性摘要和当前装备状态。
- 所有显示名称和描述从 `GameDatabase` 读取，不按中文名称驱动规则。

### 设置与调试入口

- 设置面板沿用旧版 `Esc` 开关行为，通过 `RunManager.SetExplicitPause` 暂停运行状态和玩家移动，并保留恢复前状态的能力。
- 背景音乐开关仅在对应音乐组件存在时启用；缺少组件时不得抛出异常。
- 保留 `P` 角色状态、`B` 装备背包和 `F1` 调试面板快捷键。
- 调试入口只调用新架构公开命令，包括重新开始、增加修为、增加战力、进入 Boss 和进入已有洞穴流程；不重新依赖旧 `GameFlowController`。

### Editor 自动构建边界

- 修改 `Assets/Editor/ArchitectureSceneAutomation.cs`，使后续执行 `37 MiniGame > Architecture > Rebuild Architecture Scene` 时生成完整 UI 层级、绑定 Presenter，并为敌人/玩家 Architecture Prefab 准备新组件。
- 脚本开发阶段不直接修改 `Assets/Scenes/*.unity`、`Assets/Prefabs/**/*.prefab` 或 Inspector 数据。
- 脚本阶段完成后停止，列出需要用户另行发起的 Editor 工作。

### 测试设计

- 为显示等级换算、装备库存和换装、战斗面板可见条件、战斗敌人展示数据添加 EditMode 测试。
- 为自动构建器增加静态验收，检查完整 View、Presenter 引用和敌人等级标签配置。
- 用户在独立测试阶段运行 Unity EditMode、打开 `MainPrototype_Architecture` 进入 Play Mode，并对照旧场景验证全部界面和战斗流程。
- 必须重新验证三条时间规则：普通战斗继续主倒计时、洞穴战斗暂停主倒计时、Boss 使用独立时间。

## 变更记录

### 2026-07-27 - 确认完整复刻方案
- **新增文件**：`plans/plan-architecture-ui-parity.md`
- **变更内容**：确认以新架构 uGUI 完整复刻旧版 GUI，明确战斗进入链路、敌人等级、装备库存、全屏战斗表现、自动构建边界和测试职责。
- **关联说明**：本轮先执行脚本开发，不调用 Unity MCP、不修改 Scene/Prefab、不进入 Play Mode；测试由用户在后续独立任务中完成。
