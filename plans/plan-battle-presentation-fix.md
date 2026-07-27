# 战斗界面切换修复

## 需求描述

修复水墨 UI 更新后，玩家接触敌人虽然开始自动战斗并显示双方气血，但主地图画面和主地图 HUD 仍然保留，未切换到独立全屏战斗表现的问题。

## 实现方案

- 沿 `WorldInteractionTrigger -> EnemyEncounter -> RunManager -> BattleRunner -> GameUiPresenter` 调用链确认战斗状态与战斗模拟是否正常启动。
- 使用现有水墨场景结构回归测试固定完整战斗舞台引用，确保普通战斗时全屏战斗面板覆盖主地图呈现，并显示战斗背景与双方角色。
- 对新架构与水墨双布局做最小呈现修复，不改变敌人触发、战斗数值、奖励结算或时间规则。
- 通过 EditMode 测试、Unity Console 与 Play Mode 截图验证修复，并同步更新相关文档。

## 变更记录

### 2026-07-27 - 建立战斗界面切换修复计划
- **新增文件**：`plans/plan-battle-presentation-fix.md`
- **变更内容**：记录故障调用链、最小修复边界、TDD 回归测试与 Unity Play Mode 验证方案。
- **关联说明**：本修复只处理战斗呈现切换，不修改普通战斗、洞穴和 Boss 的三条核心时间规则。

### 2026-07-27 - 恢复水墨全屏战斗舞台引用
- **修改文件**：`README.md`、`plans/plan-battle-presentation-fix.md`、`plans/plan-ink-art-replacement.md`、`Assets/Scenes/MainPrototype_InkArt.unity`、`Assets/GameData/Runtime/InkArtCatalog.asset`、`Assets/GameData/Runtime/InkSpawnPrefabCatalog.asset`、`Assets/Prefabs/InkArt/**`、`Assets/Art/RuntimeInkArt/**`
- **变更内容**：确认遭遇、状态切换和自动战斗逻辑正常；定位到已生成水墨场景中的两个 `InkBattleStage.catalog` 引用为空，导致战斗背景与双方角色 Sprite 无法加载。重新完整执行水墨场景自动化构建，恢复 Catalog、RunManager、BattleRunner、背景和角色引用。
- **关联说明**：水墨场景测试由引用缺失时的失败转为 `5/5` 通过，全量 EditMode 为 `33/33`；Play Mode 确认 `NormalBattle`、自动战斗、全屏战斗面板、战斗舞台和三张可见 Sprite 同时生效。未修改战斗数值、奖励或三条核心时间规则。
