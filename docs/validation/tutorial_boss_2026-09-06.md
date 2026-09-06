# 第一关新手 Boss

## 本次行为

第一关继续探索 30 秒，到时先显示守关说明，确认后挑战“山道恶霸”。如果时间在普通战斗中耗尽，完成当前战斗及升级说明、武学选择后才进入守关。胜利后进入教学总结并解锁第二关；失败进入失败总结，不新增解锁。主动跳过教学仍可进入既有第二关难度提示。

入场回满气血，保留本局成长，Boss 战独立计时。固定数值：100 气血、5 攻击、1 防御、0.65 攻速、无暴击和闪避。只使用基础自动攻击，不启用第二关的狐火、妖甲、狂暴或中期 Boss 技能。复用场景已有 `orc_warlord` 动画，横屏限制角色高度以避免遮挡状态栏。

## 修改与新增文件（本轮）

修改：

- `Assets/Scripts/GameFlow/GameFlowController.cs`：教学到时、战后及升级后衔接，教学 Boss 配置及胜负结算。
- `Assets/Scripts/GameFlow/GameFlowController.Tutorial.cs`：守关说明、确认开战与清理。
- `Assets/Scripts/Runtime/TutorialLessonCatalog.cs`：开场目标和首次 Boss 说明。
- `Assets/Scripts/Runtime/GameTextCatalog.cs`：统一 Boss 名与外观 ID。
- `Assets/Scripts/UI/BattleScreenController.cs`：教学战斗文案与角色尺寸。
- `Assets/Scripts/UI/PrototypeHUDController.cs`：最后 5 秒的新手守关预告。
- `docs/project_core.md`、`docs/gameplay_systems.md`、`docs/unity_tech.md`：同步规则。

新增：

- `Assets/Scripts/GameFlow/TutorialBossTuning.cs` 及 `.meta`：集中低难度数值。
- 本验证记录。

保留上一轮首次接触教学改动；未重建或保存 Unity 场景，未新增美术占位素材。

## 实测

Unity 6000.5.4f1 编译通过；执行 `37 MiniGame/Validate Chinese Fonts` 与既有四组 Safe Area 校验通过；`git diff --check` 通过。

Play Mode 用固定种子运行六次真实自动战斗。测试将探索剩余时间设为 0.01 秒，让真实 Update 到时触发，随后确认说明；并非六次人工完整探索路线试玩。每次使用真实 `PlayerStats.ResetRun`，因此“无拾取”仍包含正常起始装备。

| 条件 | 种子 | 守关耗时 | 胜利后气血 | 胜负 |
| --- | --- | --- | --- | --- |
| 无地图拾取 | 420 | 3.41 秒 | 100% | 胜 |
| 无地图拾取 | 421 | 3.59 秒 | 100% | 胜 |
| 无地图拾取 | 422 | 3.59 秒 | 100% | 胜 |
| 额外宝箱装备 | 423 | 3.59 秒 | 100% | 胜 |
| 额外剑气诀 | 424 | 3.70 秒 | 100% | 胜 |
| 入场前仅 1 点气血 | 425 | 3.59 秒 | 100% | 胜 |

当前正常起始装备提供 5 防御，可以抵消该 Boss 普攻；此战定位是低压力流程教学。不是全流派平衡或长期试玩结论。

所有试次确认：说明期间没有战斗或通关；确认后气血回满；守关中主时间保持 0、Boss 时间增长，特殊技能序列为 0；胜利后进入可继续下一关的教学总结。

额外定向回归：

- 洞穴中主时间停在 0.1 秒，没有提前触发 Boss。
- 普通战斗跨过时间归零仍保持同一个敌人；注入敌人死亡触发结算，先保留首次升级说明和真实武学选择，选完才显示 Boss 说明。
- 注入玩家死亡、等待战斗结算延迟结束后，确认失败总结不解锁、不开放下一关。
- 在守关战中跳过，取消战斗并进入难度提示；确认后第二关仍为 60 秒主地图、原中期 Boss、550 气血九尾妖狐及 Foxfire 阶段。
- Play Mode 截图：`Logs/TutorialBoss/boss_notice_portrait.png`（540 × 960），`boss_combat_portrait.png`，`boss_combat_landscape_fixed.png`（960 × 540）。横屏初次发现角色挡住状态栏，已限制到战斗区域高度并重新截图确认。

测试结束退出 Play Mode，恢复原 Game View 分辨率和测试前的教学解锁值；原 `MainPrototype` 场景的未保存状态保留。

## 运行与人工复验

无需新建 GameObject、挂载组件、绑定 Inspector 或拖入 Prefab / UI。教学场景运行时自动使用独立配置。

1. 运行 `Assets/Scenes/TutorialLevel.unity`，确认开场提示，探索至 30 秒耗尽。
2. 观察最后 5 秒预告，到时阅读守关说明，确认后检查气血恢复和独立计时。
3. 击败 Boss 后检查教学总结和下一关；也可在战斗中使用跳过入口。
4. 第二次尝试在快归零时接触普通敌人，检查先完成战斗、选完武学，再进入守关。

三条时间规则保持：普通战斗消耗主时间、洞穴暂停主时间、Boss 独立计时。

仍未完成：新玩家理解效果、移动真机触摸和本轮 WebGL 构建验收。截图与定向运行结果不代表最终美术或设备批准；仍沿用既有设置和跳过的程序占位图标。
