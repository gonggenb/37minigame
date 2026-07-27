# 一炷江湖 Unity 原型

这是一个使用 Unity `6000.5.4f1` 制作的 1 分钟武侠 Roguelite 自动战斗原型。

当前仓库已完成数据驱动底层架构、纯 C# 领域/应用层、EditMode 测试、运行时适配器、生成器、独立世界交互组件和基础 uGUI。`MainPrototype_Architecture` 的脚本引用、Canvas、六组 View、Presenter、出生区域、Architecture Prefab 和 Catalog 均由 Editor 自动化工具生成；`MainPrototype_InkArt` 在此基础上提供竖屏/横屏自适应水墨 UI、运行时水墨 Prefab 和独立全屏战斗舞台；原 `MainPrototype` 场景继续保留为旧闭环回退版本。

战斗系统的当前运行链路、公式、武学/装备效果、新旧架构差异和已知缺口见[当前战斗系统文档](docs/combat_system.md)。

## 核心循环

1. 玩家进入主地图，60 秒主倒计时开始。
2. 玩家移动探索并触发敌人、宝箱、草药或隐藏洞穴。
3. 普通战斗期间主倒计时继续流逝。
4. 隐藏洞穴期间主倒计时暂停。
5. 主倒计时结束后，在当前普通战斗和升级选择完成后进入最终 Boss 战。
6. Boss 战独立计时，结束后进入结算并可重新开始。

## 三条硬性时间规则

- 普通地图碰怪战斗时，主地图 60 秒倒计时继续。
- 隐藏洞穴内战斗时，主地图 60 秒倒计时暂停。
- 最终 Boss 战不受主地图 60 秒限制，独立计算战斗时间。

## 架构

```text
Assets/GameData/Tables/*.csv
  -> Assets/Editor/Config/GameDatabaseImporter.cs
  -> Assets/GameData/Generated/GameDatabase.asset
  -> Assets/Scripts/Architecture（Unity 适配器、生成器、交互、uGUI）
  -> Assets/Scripts/Application（计时、成长、奖励、战斗、CSV、校验）
  -> Assets/Scripts/Domain（角色、战斗值对象、配置模型、游戏状态）
```

- `WuxiaRoguelite.Domain` 和 `WuxiaRoguelite.Application` 均设置 `noEngineReferences`，可以脱离 UnityEngine 测试。
- `WuxiaRoguelite.Config` 保存 `GameDatabase` ScriptableObject 和场景数据库入口。
- 现有 `GameFlowController`、`PlayerStats`、`BattleManager`、`EncounterTrigger` 和 IMGUI 界面仍保留在旧场景中，用于兼容和回退。
- 新 `RunManager`、`CharacterManager`、`BattleRunner` 在 `MainPrototype_Architecture` 和由其派生的 `MainPrototype_InkArt` 中启用；自动化工具会停用旧流程组件和旧地图交互对象。
- 源 Prefab 不直接修改；工具复制到 `Assets/Prefabs/Architecture/**` 后移除旧交互依赖并挂载新组件。

## 目录

```text
Assets/
  Art/
    Q版水墨国风（行侠仗义五千年）/  只读测试素材库，不建立运行时直接引用
    RuntimeInkArt/                  精选水墨运行时副本
  GameData/
    Tables/          CSV 唯一源数据
    Generated/       已生成 GameDatabase.asset
    Runtime/         InkArtCatalog 与 InkSpawnPrefabCatalog
  Scripts/
    Domain/          纯 C# 领域模型
    Application/     纯 C# 应用服务
    Config/          Unity ScriptableObject 数据库
    Architecture/    新管理器、生成器、交互组件和 uGUI
    GameFlow/...     现有兼容实现
  Editor/            架构场景自动化构建器
  Editor/Config/     CSV 导入与校验
  Tests/EditMode/    EditMode 测试程序集
  Scenes/            MainPrototype 旧闭环；MainPrototype_Architecture 新架构；MainPrototype_InkArt 水墨场景
  Prefabs/           源 Prefab、Architecture 副本与 InkArt 水墨副本
  Scripts/Config/    GameDatabaseProvider 与 SpawnPrefabCatalog.asset
docs/                 项目规则、自动化结果和 Editor 检查清单
plans/                需求方案和变更轨迹
```

## CSV 数据约定

- `Assets/GameData/Tables/*.csv` 是角色、敌人、武学、装备、奖励和生成数值的唯一源数据。
- 文件必须使用 UTF-8；可用 Excel 编辑后导出 CSV。
- 配置 ID 必须使用稳定的小写 ASCII 字母、数字、下划线或短横线。
- 中文名称只用于显示；战斗效果使用 `CombatEffectType`，不按中文武学名或装备名分支。
- Editor 导入器会检查空/重复 ID、概率和数值范围、武学等级数组以及跨表引用。
- 校验失败时不会更新 `GameDatabase.asset`。
- 运行时只读取 `GameDatabase.asset`，不解析 CSV。

## 首次打开与生成数据库

1. 使用普通用户权限启动 Unity Hub，不要以管理员身份运行 Unity。
2. 使用 Unity `6000.5.4f1` 打开项目。
3. 等待包恢复、`.meta` 生成和脚本编译。
4. 执行 `Tools > 一炷江湖 > 导入 CSV 配置`。
5. 确认生成 `Assets/GameData/Generated/GameDatabase.asset`。

## 运行方式

### 现有原型

1. 打开 `Assets/Scenes/MainPrototype.unity`。
2. 进入 Play Mode。
3. 点击“踏入江湖”。

### 新架构

1. 确认已生成 `Assets/GameData/Generated/GameDatabase.asset`。
2. 执行 `37 MiniGame > Architecture > Rebuild Architecture Scene`。
3. 打开 `Assets/Scenes/MainPrototype_Architecture.unity`。
4. 进入 Play Mode，点击“踏入江湖”。
5. 详细的自动生成内容和可选 Editor 调整见[Unity Editor 架构重构绑定清单](docs/unity_editor_architecture_binding.md)。

自动化命令可重复执行。它只重建 `MainPrototype_Architecture` 中的 `ArchitectureRoot` 和 `Assets/Prefabs/Architecture/**`，不会覆盖 `MainPrototype.unity` 或源 Prefab。

### 水墨 UI

1. 先按“新架构”步骤完成数据库与架构场景生成。
2. 执行 `37 MiniGame > Ink Art > Rebuild Ink Art Scene`。
3. 打开 `Assets/Scenes/MainPrototype_InkArt.unity`。
4. 进入 Play Mode，点击“踏入江湖”。
5. 玩家碰到敌人后应显示全屏战斗背景、玩家与敌人角色，以及双方气血 UI；战斗结束后返回主地图。

`Assets/Art/Q版水墨国风（行侠仗义五千年）/` 只作为只读测试素材库。自动化工具只把实际采用的图片复制到 `Assets/Art/RuntimeInkArt/`，场景和 Prefab 不直接引用原素材库，也不再引用旧 `Generated/ThirdParty` 混合美术。

水墨场景包含两套完整界面：竖屏 `PortraitCanvas` 使用 750×1338 参考分辨率，横屏 `LandscapeCanvas` 使用 1920×1080 参考分辨率。`AdaptivePresentationController` 根据运行窗口的宽高比互斥激活其中一套；切换只改变表现层与相机参数，不重启单局、不重建 `RunManager` 状态。

## 测试

- EditMode 测试覆盖角色、属性修正、成长、三条计时规则、战斗、奖励、CSV、配置校验、数据库索引、角色工厂和 GameDatabase。
- 打开 `Window > General > Test Runner > EditMode`，执行 `Run All`。当前共发现 33 项测试，`33/33` 通过。
- 涉及流程、战斗、洞穴或 Boss 的修改必须重新验证三条硬性时间规则。
- 当前角色控制器与架构场景相关回归测试为 4/4 通过，覆盖核心引用、完整 uGUI、Architecture Prefab/Catalog，以及 `RunManager` 对主地图移动状态的控制。
- UnityMCP Play Mode 已验证：开始运行进入 `MainMap`、HUD 显示、生成本局对象；水墨场景普通战斗会激活全屏 `BattlePanel` 与 `InkBattleStage`，并加载战斗背景、玩家和敌人 Sprite。
- 双布局运行态验证：750×1338 时仅竖屏 Canvas 激活；切换到 1920×1080 后仅横屏 Canvas 激活，`RunManager` 仍保持同一局 `MainMap` 状态，主倒计时继续流逝而未重置。
- 运行态时间验证结果：普通战斗期间主时间继续下降；洞穴状态主时间保持 `59.1503→59.1503`；Boss 状态主时间保持 `59.1503→59.1503`，Boss 独立时间从 `0` 增加到 `1.5589`。

## 当前边界

- `MainPrototype.unity` 仍是默认 Build Settings 场景；是否正式切换到 `MainPrototype_InkArt` 需要在目标设备完整试玩后由项目负责人决定。
- 新洞穴流程已实现暂停、界面和离开命令，但洞穴内敌人、商人和秘藏内容仍属于后续内容迁移，不在本轮自动生成范围。
- `MainPrototype_Architecture` 保留功能完整的基础 UI；`MainPrototype_InkArt` 提供当前水墨表现版本。正式音效反馈、逐招动画和更多战斗特效仍可继续迭代。
- 不接入真实广告、支付、后端、账号、联机、排行榜或云存档。
- 不修改三条核心时间规则，不更换 Unity 技术路线。

## 开发规则

执行任务前先阅读 `AGENTS.md` 和其中指定的 `docs/` 文档。重构设计、阶段与变更记录位于 `plans/plan-project-architecture-refactor.md`。

所有开发任务必须先区分为“脚本开发任务”“Editor 工作任务”或“测试任务”，一次只执行一种类型。跨阶段需求按“脚本开发 -> Editor 工作 -> 测试”依次拆分，每个阶段结束后停止并等待下一步指令。脚本开发阶段禁止调用 Unity MCP；Editor 与测试阶段调用任何 MCP 工具前必须先说明工具、目标、操作和影响并取得用户明确审批，调用时还需逐次告知。详细规则见 `AGENTS.md`、`docs/codex_workflow.md` 和 `docs/task_prompts.md`。
