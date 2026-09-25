# 第二关：难度、强敌情报与悬赏

本次实现集中在 MainPrototype（平川山镇）。保留原有 22 个旧小怪 + 18 个新小怪，复用现有精英位置；不增加地图面积、不改变角色永久属性。

## 如何试玩

打开 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode 后开始游戏。开场对话结束后先显示情报，再选起手武学，随后开始六十息。已经完成过第二关的存档可直接选择「险境」。

- **历练 → 险境 → 绝境**：通关当前档解锁下一档。失败不会解锁；结算页可直接挑战下一档或重试本关。
- **强敌情报**：每局固定一种最终 Boss 倾向；重开必定换一种。切换难度不刷新情报和悬赏，开局后不可更改难度。
- **妖甲固守**：开场获得最大气血 10% 的护甲，七成血护甲加厚，攻击降低。所有伤害均能削盾。
- **疾风连击**：普攻加快，狐火间隔缩短，单次攻击降低。
- **烈焰强攻**：普攻与狐火伤害提高，普攻减速，首次狐火更晚。
- **两条可选悬赏**：一条在练武场，一条在其他支路；从现有精英中选取。额外气血 +45%、攻击 +20%，分别带护甲或重撞。击败后提升当前主修中已学、尚未满重的一门武学；主修全部满重时补 40 铜钱，每个目标每局仅一次。
- **途中查阅**：地图右侧「情报」可重看，本窗口暂停探索；关闭恢复。无需领取悬赏即可选择目标或绕行。

## 数值与机制

|档位|普通/精英气血、攻击|中期 Boss 气血、攻击|最终 Boss 气血、攻击|额外规则|
|---|---|---|---|---|
|历练|×1.00 / ×1.00|×1.10 / ×1.04|×1.18 / ×1.08|悬赏自带词条|
|险境|×1.06 / ×1.04|×1.14 / ×1.07|×1.23 / ×1.12|精英开场护甲，最终 Boss 七成血后加紧狐火|
|绝境|×1.12 / ×1.08|×1.20 / ×1.12|×1.40 / ×1.20|精英首击重撞，最终 Boss 七成/三成五血后加紧狐火|

Boss 倾向额外修正：固守攻击 ×0.9；连击攻击 ×0.85、攻速 ×1.25；强攻攻击 ×1.15、攻速 ×0.85。悬赏词条优先于档位精英词条。参数施加于战斗副本，不回写场景配置，也不累乘。

三条时间规则保留：普通战斗继续扣主时间；洞穴暂停主时间；最终 Boss 独立计时。

## 文件与绑定

新增运行脚本：
- `Assets/Scripts/Runtime/RunChallengeCatalog.cs`：档位、Boss 倾向规则。
- `Assets/Scripts/GameFlow/RunChallengeState.cs`：本局悬赏及持久解锁。
- `Assets/Scripts/GameFlow/GameFlowController.Challenges.cs`：开局、奖励、重开与解锁流程。
- `Assets/Scripts/UI/PrototypeHUDController.Challenges.cs`：横竖屏情报窗口。
- `Assets/Scripts/UI/ChallengeArt.cs`：图标资源加载。
- `Assets/Scripts/Debug/ChallengeReplayPlayModeProbe.cs`：仅编辑器启用的专项验证。

修改已有文件：`GameFlowController.cs`、`CombatantStats.cs`、`GameTextCatalog.cs`、`EncounterTrigger.cs`、`BattleManager.cs`、`PrototypeHUDController.cs`、`PrototypeHUDController.RunReview.cs`、`EnemyLevelLabel.cs`、`MobileInputController.cs`、`BattleScreenController.cs`、`AutomatedRunStatisticsRunner.cs`。

无需新增 GameObject、挂组件或 Inspector 手动绑定；通过既有控制器与 Resources 自动加载。本次不保存场景或 Prefab，不覆盖此前场景调整。

## 自生成美术

五张独立生成的透明图标：挑战徽记、妖甲、连击、爆发、悬赏令。

- 原始图、完整生成提示词和来源：`ArtSource/Raw/ChallengeIcons/`。
- 规范母版：`ArtSource/Normalized/ChallengeIcons/`，256×256、安全区 192。
- 运行资源：`Assets/Resources/ChallengeIcons/`，128×128、安全区 96。
- 使用项目既有 `Tools/ArtPipeline/normalize_icon_sheet.py` 归一化；Sprite Single / FullRect / Bilinear / Clamp / 无 Mip / 无压缩。
- 尺寸、透明通道、Resources 加载通过；横竖屏截图证明已接入 Unity。状态为 Generated / Normalized / Imported / InEngineQA，尚无手机真人验收或最终 Approved。

![图标预览](icons_preview.png)

## 验证入口与边界

- `37 MiniGame/Validate Replay Challenges Play Mode`：受控功能检查，自动恢复解锁存档、Game View 尺寸及运行状态，输出 `playmode_report.json` 和截图。受控胜负/高气血夹具只验证路径，不用于证明平衡。
- `37 MiniGame/Validate Routes Traits and Review Play Mode`：旧小怪、路线奖励、战斗反馈和三条时间规则回归。
- `37 MiniGame/Automated Run Statistics/Run 15 Replay Challenge Runs`：三档 × 五种起手，正式场景移动、碰撞、战斗。跨档同起手共享基础种子与指定 Boss 倾向，采用均衡路线；不加玩家属性，结束恢复测试前的持久解锁。洞穴使用正式事件但跳过室内移动/商店购买。
- `37 MiniGame/Validate Chinese Fonts`：随包中文字体校验。

每档五局仅用于找极端难度和流程问题；并非真人胜率，也未验证玩家长时间重复游玩的意愿。手机触摸、低端机表现，以及不同路线与自选悬赏收益仍需后续真人试玩。

## 最终验证记录

- 专项 Play Mode：44 项通过，运行时错误 0（`playmode_report.json`）。
- 旧功能回归：46 项通过，运行时错误 0（`regression/playmode_report.json`）；包含 22+18 混合小怪和三条时间规则。旧目录历史证据已恢复，未用本次报告覆盖历史结果。
- 横屏 960×540、竖屏 540×960 截图已检查；横屏两条悬赏奖励无需滚动，结算页顶端直接显示新档位解锁。
- Unity 编译、随包常规/粗体中文字体校验通过；不用操作系统字体兜底。
- 前两轮各 15 局记录分别保留于 `initial_balance/`、`second_balance/`。首轮绝境 0/5 通过中期，次轮已到 4/5 通过中期，但高档位仍无通关；因此继续降低早期战损并把阶段狐火的加速上限放宽至 2.4 秒 / 2 秒。
- 最终参数实战 15 局：历练 **3/5**、险境 **4/5**、绝境 **1/5** 通关；三档中期均 **5/5** 通过。普通计时覆盖 15 局、洞穴暂停覆盖 8 局、最终独立计时覆盖 14 局，全部通过。逐局路线、构筑、悬赏和战损见 [最终统计](final_balance/automated_run_stats_replay_challenges_2026-09-10.md) 及 [CSV](final_balance/automated_run_stats_replay_challenges_2026-09-10.csv)。
- 共跑三轮 45 局。历练/险境最终五局胜率出现倒挂，说明当前样本受路线、成长、随机事件和帧/物理调度影响明显，**未证明稳定的档位胜率曲线**；不继续用这五个样本反复拟合数值。可确认高档能正常完成全程、最高档有通关路径、悬赏实际发奖，仍需真人扩充路线与构筑样本。
- 测试结束已退出 Play Mode、恢复原有解锁档位（险境）及正常时间倍率；没有替玩家解锁绝境。
