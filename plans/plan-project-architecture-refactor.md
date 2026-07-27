# Unity 项目底层架构与数据驱动重构

## 需求描述

在保持现有核心玩法、数值、美术、单场景运行方式和三条硬性时间规则不变的前提下，重整角色、敌人、战斗、技能、奖励、洞穴、世界物品和 UI 的职责边界。

重构目标如下：

- 角色、敌人、武学、装备、奖励和生成规则统一由 UTF-8 CSV 维护。
- Unity Editor 校验 CSV 后生成 `GameDatabase.asset`，运行时不解析 CSV。
- 角色、主地图计时、成长、奖励和自动战斗规则进入不依赖 UnityEngine 的纯 C# Domain/Application 程序集。
- 现有 `GameFlowController`、`PlayerStats`、`BattleManager` 和 IMGUI 界面继续保留在旧场景，新的管理器和 uGUI 在 `MainPrototype_Architecture` 中独立启用，避免破坏回退版本。
- 普通敌人、宝箱、草药和洞穴入口使用独立交互组件；敌人、物品和洞穴入口由区域出生点与配置表在每局生成。
- 最终交付可重复执行的 Unity Editor 自动化构建器，以及只保留检查项和可选调整项的 Editor 清单。

## 实现方案

### 架构边界

```text
CSV 源表
  -> Editor CSV 导入与校验
  -> GameDatabase.asset
  -> Unity Runtime 适配器 / 生成器 / Presenter
  -> 纯 C# Application 服务
  -> 纯 C# Domain 模型
```

- `WuxiaRoguelite.Domain`：角色属性、运行时角色、游戏状态、配置行模型和战斗值对象，不引用 UnityEngine。
- `WuxiaRoguelite.Application`：计时、成长、奖励、战斗模拟、CSV 解析和配置校验，不引用 UnityEngine。
- Unity Runtime：`GameDatabase`、角色/流程/战斗管理器、生成器、世界交互组件、uGUI View/Presenter，负责 MonoBehaviour 生命周期和资源引用。
- Unity Editor：读取 `Assets/GameData/Tables/*.csv`，输出校验结果并生成 `Assets/GameData/Generated/GameDatabase.asset`。

### 兼容迁移原则

- 不直接编辑 `Assets/Scenes/MainPrototype.unity` 或源 Prefab；自动化只重建 `MainPrototype_Architecture` 的 `ArchitectureRoot` 与 `Assets/Prefabs/Architecture/**`。
- 不删除现有 `GameFlowController`、`PlayerStats`、`BattleManager`、`EncounterTrigger`、`PrototypeHUDController`、`BattleScreenController` 和 `CaveRoomController`。
- 新架构由 `ArchitectureSceneAutomation` 自动创建对象、挂载脚本和绑定引用，并在架构场景中停用旧流程与旧交互；旧场景保持不变。
- 每个阶段保持脚本可编译，并用 EditMode 测试固定行为。

### 文件与职责

#### 纯 C# 领域层

- `Assets/Scripts/Domain/GameFlow/GameState.cs`：定义 Ready、MainMap、NormalBattle、Cave、LevelUp、BossBattle、Result。
- `Assets/Scripts/Domain/Characters/CharacterStats.cs`：不可变基础属性和值域归一化。
- `Assets/Scripts/Domain/Characters/CharacterRuntime.cs`：当前气血、属性修正、受伤和恢复。
- `Assets/Scripts/Domain/Characters/StatModifier.cs`：属性类型、加法/乘基础值修正和来源 ID。
- `Assets/Scripts/Domain/Combat/BattleModels.cs`：战斗输入、随机数接口、事件和结果。
- `Assets/Scripts/Domain/Configuration/GameConfigModels.cs`：角色、武学、装备、奖励和生成规则配置模型。

#### 纯 C# 应用层

- `Assets/Scripts/Application/Time/RunTimerService.cs`：三条硬性时间规则与显式暂停。
- `Assets/Scripts/Application/Progression/ProgressionService.cs`：支持一次奖励连续升级。
- `Assets/Scripts/Application/Rewards/RewardService.cs`：把奖励配置转成运行时奖励结果。
- `Assets/Scripts/Application/Combat/BattleService.cs`：可逐帧 Tick 的确定性自动战斗。
- `Assets/Scripts/Application/Combat/CombatEffectRegistry.cs`：按稳定效果类型应用吸血、护盾、毒、破甲等效果，不按中文显示名分支。
- `Assets/Scripts/Application/Configuration/CsvTableParser.cs`：支持引号、逗号和 CRLF 的 CSV 解析。
- `Assets/Scripts/Application/Configuration/GameDatabaseValidator.cs`：检查空 ID、重复 ID、数值范围和跨表引用。
- `Assets/Scripts/Application/Configuration/GameDatabaseIndex.cs`：为运行时数据库建立稳定 ID 索引。
- `Assets/Scripts/Application/Characters/CharacterFactory.cs`：从角色/武学/装备配置创建并更新领域对象。

#### Unity 数据与 Editor 工具

- `Assets/Scripts/Config/GameDatabase.cs`：ScriptableObject 数据库和按 ID 索引。
- `Assets/Scripts/Config/GameDatabaseProvider.cs`：场景中的数据库入口。
- `Assets/Editor/Config/GameDatabaseImporter.cs`：菜单导入、校验、生成资产和错误报告。
- `Assets/Editor/ArchitectureSceneAutomation.cs`：幂等重建架构场景、uGUI、SpawnRegion、Architecture Prefab 和 Catalog 映射。
- `Assets/GameData/Tables/characters.csv`：玩家、普通敌人、洞穴敌人和 Boss。
- `Assets/GameData/Tables/martial_arts.csv`：首批九门武学与稳定效果类型。
- `Assets/GameData/Tables/equipment.csv`：现有装备效果。
- `Assets/GameData/Tables/rewards.csv`：战斗、宝箱、药草和洞穴奖励。
- `Assets/GameData/Tables/spawns.csv`：区域、实体类型、配置 ID、数量和权重。

#### Unity Runtime 迁移组件

- `Assets/Scripts/Architecture/Characters/CharacterManager.cs`：从数据库构造玩家领域对象并发布变化事件。
- `Assets/Scripts/Architecture/GameFlow/RunManager.cs`：协调状态、计时、战斗、奖励和重新开始。
- `Assets/Scripts/Architecture/Battle/BattleRunner.cs`：把 Unity `deltaTime` 和随机数接到纯 C# `BattleService`。
- `Assets/Scripts/Architecture/Spawning/SpawnRegion.cs`：定义区域 ID、中心和范围。
- `Assets/Scripts/Architecture/Spawning/SpawnPrefabCatalog.cs`：稳定 prefab ID 到 Prefab 的 Inspector 映射。
- `Assets/Scripts/Architecture/Spawning/EnemySpawner.cs`：只生成敌人。
- `Assets/Scripts/Architecture/Spawning/ItemSpawner.cs`：只生成宝箱和草药。
- `Assets/Scripts/Architecture/Spawning/CaveSpawner.cs`：只生成洞穴入口。
- `Assets/Scripts/Architecture/Interaction/IWorldInteractable.cs`：统一交互入口。
- `Assets/Scripts/Architecture/Interaction/WorldInteractionTrigger.cs`：只负责识别玩家和防重复触发。
- `Assets/Scripts/Architecture/Interaction/EnemyEncounter.cs`：请求普通/精英战斗。
- `Assets/Scripts/Architecture/Interaction/TreasureChest.cs`：发放宝箱奖励。
- `Assets/Scripts/Architecture/Interaction/HerbPickup.cs`：恢复气血。
- `Assets/Scripts/Architecture/Interaction/CaveEntrance.cs`：进入洞穴并暂停主计时。

#### uGUI

- `Assets/Scripts/Architecture/UI/MainMenuView.cs`：开始按钮。
- `Assets/Scripts/Architecture/UI/HudView.cs`：主时间、气血、修为、铜钱和状态文本。
- `Assets/Scripts/Architecture/UI/BattleView.cs`：双方气血、战斗日志和 Boss 独立时间。
- `Assets/Scripts/Architecture/UI/CaveView.cs`：洞穴暂停提示和离开命令。
- `Assets/Scripts/Architecture/UI/LevelUpView.cs`：三选一与刷新命令。
- `Assets/Scripts/Architecture/UI/ResultView.cs`：胜负、击杀、洞穴次数和重新开始按钮。
- `Assets/Scripts/Architecture/UI/GameUiPresenter.cs`：订阅 `RunManager`/`CharacterManager`，刷新 View 并转发按钮命令。

### TDD 阶段

1. 先保留并运行 `CharacterRuntimeTests`、`RunTimerServiceTests`、`ProgressionServiceTests`，确认因类型缺失而失败。
2. 以最小实现让首批测试通过。
3. 新增战斗、CSV 和配置校验测试，确认分别因行为缺失而失败。
4. 实现最小战斗服务、解析器和校验器后再次运行全部 EditMode 测试。
5. 先新增场景自动化验收测试，确认因 Architecture Prefab、核心引用和 uGUI 缺失而失败。
6. 实现 Editor 自动化构建器并通过 UnityMCP执行，复跑同一组测试转绿。
7. 全量 EditMode 与 Play Mode 验证后，更新 Editor 清单为自动生成后的检查项。

### 分阶段验收

- [x] 阶段 A：asmdef 和首批 RED 测试存在，日志明确显示缺失类型错误。
- [x] 阶段 B：角色、计时、成长、奖励和战斗领域服务通过 18 项纯 C# 测试。
- [x] 阶段 C：CSV 解析与校验通过；Editor 导入器已生成 `Assets/GameData/Generated/GameDatabase.asset`。
- [x] 阶段 D：新管理器、生成器和独立交互组件静态编译通过，并且不要求删除旧组件。
- [x] 阶段 E：uGUI View/Presenter 静态编译通过，逐项绑定清单已完成。
- [x] 阶段 F：三条硬性时间规则有自动测试，Play Mode 四组手动验证步骤已写入文档。
- [x] 阶段 G：UnityMCP 自动完成脚本挂载、完整 uGUI、四个 SpawnRegion、七个 Architecture Prefab 与 Catalog 映射；全量 22 项 EditMode 测试通过。
- [x] 阶段 H：Play Mode 验证主菜单、HUD、战斗、洞穴、升级和结果页；运行态数值再次确认三条时间规则。

## 变更记录

### 2026-07-27 - 初始化架构重构
- **修改文件**：`Packages/manifest.json`
- **新增文件**：`README.md`、`plans/plan-project-architecture-refactor.md`
- **变更内容**：建立项目说明、配置约定、重构目标和完整开发轨迹；显式声明测试与 uGUI 依赖。
- **关联说明**：后续每个可运行迁移批次继续追加记录，不覆盖用户已有的 `ProjectSettings/ProjectSettings.asset` 修改。

### 2026-07-27 - 完成现状核对与迁移设计
- **修改文件**：`plans/plan-project-architecture-refactor.md`
- **变更内容**：记录四层架构、兼容迁移边界、精确文件职责、TDD 顺序和阶段验收标准。
- **关联说明**：确认现有首批测试处于预期 RED；现有场景、Prefab、旧管理器和 IMGUI 在人工完成新绑定前保持不变。

### 2026-07-27 - 建立纯 C# 领域层与测试基线
- **修改文件**：`Assets/Tests/EditMode/WuxiaRoguelite.EditModeTests.asmdef`
- **新增文件**：`Assets/Scripts/Domain/**`、`Assets/Scripts/Application/**`、`Assets/Tests/EditMode/Core/**`、`Assets/Tests/EditMode/Unity/GameDatabaseTests.cs`
- **变更内容**：实现角色属性与运行时角色、稳定效果类型、游戏状态、计时、成长、奖励、战斗、角色工厂、CSV 解析、配置校验和数据库索引；通过 RED→GREEN 建立 18 项纯 C# 测试和 1 项 Unity GameDatabase 测试。
- **关联说明**：三条核心时间规则由 `RunTimerServiceTests` 固定；属性乘基础值修正已验证为与应用顺序无关。

### 2026-07-27 - 建立 CSV 与 GameDatabase 导入链路
- **修改文件**：无既有业务脚本修改。
- **新增文件**：`Assets/GameData/Tables/*.csv`、`Assets/GameData/Generated/GameDatabase.asset`、`Assets/Scripts/Config/**`、`Assets/Editor/Config/GameDatabaseImporter.cs`
- **变更内容**：加入 8 个角色、9 门武学、6 件装备、7 组奖励和 7 条生成规则；导入器支持 UTF-8、引号 CSV、跨表校验、自动重导入和 ScriptableObject 资产更新。
- **关联说明**：实际 CSV 已完成解析与校验；Unity 自动导入已生成并序列化 `GameDatabase.asset`。

### 2026-07-27 - 完成并行运行时与 uGUI 迁移代码
- **修改文件**：`README.md`、`plans/plan-project-architecture-refactor.md`
- **新增文件**：`Assets/Scripts/Architecture/**`、`docs/unity_editor_architecture_binding.md`
- **变更内容**：实现 `CharacterManager`、`RunManager`、`BattleRunner`、三个生成器、四类世界交互、主菜单/HUD/战斗/洞穴/突破/结算 View 和 `GameUiPresenter`；输出完整场景、Prefab、Inspector 和 Play Mode 绑定验证清单。
- **关联说明**：Unity 已成功重编译 Domain、Application、Config、EditModeTests、Assembly-CSharp 和 Editor 程序集；旧场景与旧 IMGUI 未修改，等待用户在复制场景中人工绑定。

### 2026-07-27 - 校正当前 UI 与迁移状态说明
- **修改文件**：`README.md`、`plans/plan-project-architecture-refactor.md`
- **变更内容**：根据仓库现状补充玩家、敌人、物品、洞穴入口 Prefab 与 `SpawnPrefabCatalog.asset` 已存在；明确默认 `MainPrototype.unity` 仍使用旧 IMGUI，`MainPrototype_Architecture.unity` 仅部分接入新架构，uGUI Canvas、Panel、`GameUiPresenter` 和生成器引用尚未完成绑定。
- **关联说明**：本次仅修正文档，不修改 UI 脚本、场景或 Prefab。

### 2026-07-27 - 完成 UnityMCP 场景、uGUI 与 Prefab 自动化
- **修改文件**：`README.md`、`docs/unity_editor_architecture_binding.md`、`plans/plan-project-architecture-refactor.md`、`Assets/Scenes/MainPrototype_Architecture.unity`、`Assets/Scripts/Config/SpawnPrefabCatalog.asset`
- **新增文件**：`Assets/Editor/ArchitectureSceneAutomation.cs`、`Assets/Tests/EditMode/Unity/ArchitectureSceneAutomationTests.cs`、`Assets/Prefabs/Architecture/**`
- **变更内容**：新增可重复执行的 Editor 构建器，自动创建并绑定数据库入口、三个管理器、三个生成器、四个 SpawnRegion、运行时生成根、Canvas、EventSystem、六组 View 和 `GameUiPresenter`；从源 Prefab 复制七个 Architecture 副本，移除旧 `EncounterTrigger` 及其依赖指示器，挂载新交互组件并重写 Catalog 映射。
- **关联说明**：验收测试先以 3/3 失败确认缺口，再转为 3/3 通过；全量 EditMode 为 22/22。UnityMCP Play Mode 验证开始后生成 18 个本局对象，主菜单、HUD、普通战斗、洞穴、升级和结果页状态切换正常；普通战斗、洞穴、Boss 的计时结果分别为 `60→55`、`60→60`、主时间 `60→60` 且 Boss 时间 `0→5`。旧 `MainPrototype.unity` 和源 Prefab 未覆盖。
