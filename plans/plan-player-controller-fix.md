# 角色控制器移动修复

## 需求描述

修复 `MainPrototype_Architecture` 中角色开始游戏后无法响应方向键或 WASD 移动的问题，并保证非主地图阶段继续锁定角色移动。

## 实现方案

- 保留现有 Unity Legacy Input 配置与 `PlayerController` 的输入读取方式。
- 在新架构 `RunManager` 中绑定场景玩家的 `PlayerController`，由运行状态统一控制移动开关。
- 仅当状态为 `MainMap` 且未显式暂停时允许移动；普通战斗、洞穴、突破、Boss 和结算阶段禁用移动。
- 更新 `ArchitectureSceneAutomation` 自动绑定玩家控制器，避免重建场景后引用再次丢失。
- 增加状态行为测试与场景绑定测试，并通过 Play Mode 验证开始游戏后移动已启用。

## 变更记录

### 2026-07-27 - 建立角色移动故障修复计划
- **修改文件**：无
- **新增文件**：`plans/plan-player-controller-fix.md`
- **变更内容**：记录故障根因、最小修复边界、测试策略和自动化绑定要求。
- **关联说明**：本修复不修改输入框架、战斗流程或三条核心时间规则。

### 2026-07-27 - 修复新架构角色移动状态未接入
- **修改文件**：`Assets/Scripts/Architecture/GameFlow/RunManager.cs`、`Assets/Editor/ArchitectureSceneAutomation.cs`、`Assets/Tests/EditMode/Unity/ArchitectureSceneAutomationTests.cs`、`Assets/Scenes/MainPrototype_Architecture.unity`、`Assets/Scripts/Config/SpawnPrefabCatalog.asset`、`README.md`
- **变更内容**：为 `RunManager` 增加并校验 `PlayerController` 引用；仅在未暂停的 `MainMap` 状态启用移动；自动化构建器查找并绑定场景玩家控制器；新增状态切换和场景绑定回归测试；重建架构场景。
- **关联说明**：RED 测试先确认 `RunManager.playerController` 缺失，修复后相关测试 4/4 通过；Play Mode 验证开始游戏后 `canMove=True` 且控制器产生实际位移。全量 33 项测试中 29 项通过，剩余 4 项为缺少水墨场景/目录资产的既有失败。
