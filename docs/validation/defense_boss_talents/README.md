# 防御收益与 Boss 随机天赋

本轮调整提高防御成长的收益，并让正式关卡每位 Boss 在开局随机取得一项流派针对天赋。第二关中期与最终 Boss 分别抽取，第三关抽最终 Boss；教学守关仍无天赋。天赋不读取玩家构筑，不因查看情报、选择难度、选武学或 Boss 转阶段而重抽；新局重新抽取，允许连续抽中相同项。

## 防御调整

- 玩家防御抵伤效率从 1.0 提高为 1.2；敌人防御公式不变。普攻、中期技能、狐火与普通怪余毒统一使用玩家有效防御，各自原有技能系数与至少 1 点基础伤害保留。
- 铁布衫每重防御由 1 增加至 1.5；气血上限增加 15% 保持不变。界面采用既有千倍数值显示，因此显示防御 +1,500。
- 反震倍率一至三重由 0.65 / 0.85 / 1.05 调整为 0.90 / 1.10 / 1.30 × 防御。
- 普攻、中期 Boss 技能、狐火每次命中均可触发反震，包括护盾完全吸收伤害。闪避不触发反震；持续余毒也不触发，避免递归与额外连锁。
- 伤害减免、妖甲吸收、实际气血损失、吸血与复盘共用实际伤害结算；过量伤害不用于吸血。

## 天赋池

| 天赋 | 针对 | 效果 | 补强方向 |
| --- | --- | --- | --- |
| 截剑势 | 快剑 | 剑气、连环剑伤害 -25% | 毒伤或反震 |
| 辟毒体 | 毒掌 | 毒伤 -30%，叠层与破甲保留 | 剑气、直接伤害 |
| 透甲劲 | 铁壁 | 忽略 20% 有效防御，反震伤害 -20% | 气血、护盾 |
| 封血印 | 血刀 | 战斗回血 -30%，暴击额外倍率 -25% | 护盾、稳定输出 |
| 照影眼 | 轻身 | 随机闪避概率乘 0.7 | 防御、无相残影必闪 |

随机池每项等概率；每位 Boss 只得到一项。针对的是机制，因此采用相同机制的混合流派也会受到对应影响。所有天赋均保留原机制的部分收益，没有毒免、禁疗或必中特权。原有妖甲/迅攻/凶猛倾向与难度档位保持独立。

情报窗口在起手武学前显示天赋和补强建议，探索期间可再次打开，查看期间暂停；战斗状态显示当前天赋，结算记录中期与最终天赋。复用现有主题、随包字体和情报图标，没有新增美术占位。

## 运行与复测

无需新增 GameObject、挂载组件或 Inspector 绑定，也无需重建场景。打开 `Assets/Scenes/MainPrototype.unity`，Play 后开局，在情报页查看两位 Boss 天赋；选择武学开始探索。第三关复用精简情报页，教学关保持原流程。

菜单 `37 MiniGame/Validate Defense and Boss Talents Play Mode` 在真实 MainPrototype Play Mode 中验证：

- 铁布衫成长与有效防御；普通攻击、中期技能、狐火的护盾反震。
- 所有天赋 × 五种伤害来源，护盾/气血结算与复盘统计。
- 暴击、回血、吸血、毒伤恢复、随机闪避与残影必闪、过量伤害。
- 随机池、单局固定、普通怪隔离、中期与最终 Boss 实际赋值。
- 普通战斗继续主时间、洞穴暂停主时间、最终 Boss 独立计时。
- 五项天赋分别完整运行一次实际 Boss 战斗协程，验证能够正常结束。

完整自动跑局菜单：`37 MiniGame/Automated Run Statistics/Run 15 Replay Challenge Runs`。CSV 新增 `mid_boss_talent` / `final_boss_talent`，便于交叉比较流派与针对天赋。自动路线和加速战斗不代表真人操作、最终平衡或手机验收。

## 验证结果

已在真实 Unity Play Mode 通过 132 项专项检查，结果见 `mechanics.json`。其中五种天赋分别通过一次完整 Boss 战斗协程；普通战斗继续主时间、洞穴暂停主时间、最终 Boss 独立计时均通过。该受控战斗用于验证机制与流程，不代表自然构筑胜率。

没有修改三条核心计时规则，也没有调整场景、Prefab 或真实商业服务。完整自动跑局见 [15 局报告](../automated_run_stats_replay_challenges_2026-09-21.md) 与 [逐局 CSV](../automated_run_stats_replay_challenges_2026-09-21.csv)。15/15 到达中期与最终 Boss，10/15 通关；铁布衫起手 3/3 通关，普通战斗计时覆盖 15 局、洞穴暂停覆盖 13 局、最终 Boss 独立计时覆盖 15 局，全部通过。没有自动跑局超时。

这不是旧版与新版的 A/B 对照：使用当前共享 checkout（包含同期成长内容变更），同时固定种子也受帧/物理调度影响。三局铁布衫最终 Boss 均为封血印，并未覆盖透甲劲；伤害矩阵专项测试覆盖了所有五种天赋，但不能替代各流派面对自身克制天赋的完整胜率样本。轻身与血刀各为 1/3，后续需要正常速度、更多种子与人工路线验证。当前数据支持继续试玩本轮调整，不支持宣称五派已均衡。


## 文件清单

修改：
- `Assets/Scripts/Battle/BattleManager.cs`：减伤、实际伤害归因、护盾反震与回血。
- `Assets/Scripts/Battle/BattleManager.FinalBoss.cs`、`BattleManager.MidBoss.cs`、`BattleManager.EnemyTraits.cs`：技能伤害路径统一使用有效防御；Boss 技能接入天赋。
- `Assets/Scripts/Runtime/CombatantStats.cs`：战斗副本携带天赋，不修改原始 Boss 配置。
- `Assets/Scripts/Player/PlayerStats.cs`、`Assets/Scripts/MartialArts/MartialArtCatalog.cs`：铁布衫与反震数值、说明同步。
- `Assets/Scripts/GameFlow/GameFlowController.cs`、`GameFlowController.Challenges.cs`：开局随机、中期与最终 Boss 传递、教程排除。
- `Assets/Scripts/UI/PrototypeHUDController.cs`、`PrototypeHUDController.Challenges.cs`、`PrototypeHUDController.RunReview.cs`、`BattleScreenController.cs`、`MartialArtChoiceInsight.cs`：情报、战斗、结算与武学收益预览。
- `Assets/Scripts/Debug/AutomatedRunStatisticsRunner.cs`：记录两位 Boss 天赋；开局及阶段切换恢复正常时间倍率后重新设置测试加速倍率（保持真实暂停），正式游戏不受影响。
- `docs/gameplay_systems.md`：当前机制说明。

新增：
- `Assets/Scripts/Battle/BossTalentCatalog.cs`、`BattleManager.BossTalents.cs` 及 Unity `.meta`。
- `Assets/Scripts/Debug/DefenseTalentPlayModeProbe.cs` 及 Unity `.meta`。
- 本目录的说明、专项 JSON 与视觉检查证据；本轮完整自动跑局报告和 CSV。


## 界面与其他场景验证

- 真实 Play Mode 的 `960×540` 横屏与 `540×960` 竖屏情报摘要已检查；竖屏详细天赋、效果与补强说明可完整阅读，滚动范围保留同期新增的装备追求与悬赏内容。
- 竖屏战斗状态已显示实际生效天赋；第三关在实际 BambooValleyLevel 场景显示精简天赋情报页，随后进入实际最终 Boss，天赋与预告一致。
- 在实际 TutorialLevel 场景重新开局，确认两位 Boss 天赋为空、天赋情报不出现。跨场景检查见 `level_three.json` 和 `scene_checks.txt`。
- 截图见本目录 `brief_landscape.png`、`brief_portrait.png`、`talents_portrait.png`、`battle_portrait.png`、`level_three_portrait.png`。这属于 Editor Play Mode 视觉检查，不等于手机真机、触控或正式发布批准。
- Unity 中文字体校验通过；无需重新生成字体。修改范围的 `git diff --check` 通过。

未完成：正常速度下更大样本的流派对克制天赋交叉平衡验证、人工实际路线试玩和手机触控/安全区验收。当前自动结果不用于宣称最终胜率均衡。
