# 成长反馈、装备追求与敌人弱点

本轮主轴：看强敌情报 → 选装备追求与武学 → 按路线获得成长 → 用战斗验证 → 结算复盘。

## 已实现

- 修为正收益统一累计记录，HUD 播放约 1.8 秒的修为飘字，显示距下一次武学选择还差多少修为；发生升级时显示突破提示。多个同帧收益合并，跨等级扣除不会丢失获得量。动画不延迟奖励结算、不改变时间倍率；隐藏于设置/角色/情报等窗口时保留展示时间，重新开局清空。HUD 的 `reduceGrowthMotion` 可关闭位移。
- 第二关情报页首先选择一项装备追求；主地图与结算显示进度。营地击败两个不同目标，将原随机额外装备改为指定装备，每局一次；途中已拥有目标时改给另一件未持有装备，装备池耗尽沿用 20 铜钱补偿。
- 破甲：腐骨手套，命中削减敌人防御。护体：玄武甲，每场开场护盾。淬毒：淬毒针匣，命中额外叠毒。复用已存在的装备、图标和战斗实现，不新增永久属性或装备词条系统。
- 选择只在第二关开局情报页生效，进入探索后锁定。装备遵循现有自动装备规则，背包中仍可更换。
- 9 步内可见怪物显示弱点；当前已学武学/已装备物品/实际闪避支持对应机制时才显示克制标签。卸下装备立即失去相应标签。悬赏页也展示目标弱点，武学选择展示应对标签。
- 重撞：护盾挡首击、闪避消耗强化首击。毒咬：闪避命中可避免挂毒，不是中毒免疫。开场护甲：连击与毒伤均可削盾；不谎称破甲直接无视护盾。标签不新增伤害倍率、不保证胜利。
- 角色原有连战磨砺、修为武学、铜钱商店、区域补修和 Boss 情报继续存在；本轮将“功法升重”和“装备改变出手/生存方式”作为可见的两条局内追求。

## 文件

修改既有文件：
- `Assets/Scripts/Player/PlayerStats.cs`
- `Assets/Scripts/GameFlow/GameFlowController.RouteRewards.cs`
- `Assets/Scripts/UI/PrototypeHUDController.cs`
- `Assets/Scripts/UI/PrototypeHUDController.Challenges.cs`
- `Assets/Scripts/UI/PrototypeHUDController.RunReview.cs`
- `Assets/Scripts/UI/EnemyLevelLabel.cs`
- `Assets/Scripts/UI/MainMapRegionGuide.cs`
- `Assets/Scripts/UI/MartialArtChoiceInsight.cs`

新增文件（含 Unity 自动生成的 `.meta`）：
- `Assets/Scripts/Runtime/RunPursuitCatalog.cs`
- `Assets/Scripts/UI/EnemyMatchupInsight.cs`
- `Assets/Scripts/UI/PrototypeHUDController.Growth.cs`
- `Assets/Scripts/Debug/GrowthPursuitPlayModeProbe.cs`
- 本说明、`playmode_report.json` 和横竖屏截图。

## 运行与验证

无需创建 GameObject、Prefab 或 Inspector 引用。打开 `MainPrototype`，Play，进入第二关开局情报页；选择破甲/护体/淬毒，再选起手武学。击败营地两个不同目标，检查装备、目标进度与奖励；获取修为观察飘字；靠近带特点的敌人查看弱点与克制。

自动验证菜单：`37 MiniGame/Validate Growth Pursuit Play Mode`。只在 Editor 执行，使用真实遭遇和受控数值，不保存场景；结束恢复 Game View 尺寸、后台运行与时间倍率。测试覆盖三项定向奖励、重复回调、第三场不重复发奖、已持有目标补偿、重开、装备标签增删、修为跨等级与道具、横竖屏以及三条计时规则。最终结果以同目录 `playmode_report.json` 为准。

中文文案必须执行 `37 MiniGame/Validate Chinese Fonts`。本轮未引入字体、贴图或替换场景资源。

## 本轮结果（2026-09-21）

- Unity 编译通过；字体菜单检查 978 个非 ASCII 字形，常规体/粗体均覆盖。
- 最终在独立临时工程中完成 48 项 Play Mode 检查，`success=true`，运行时错误与外部工具错误均为空。
- 已查看 960×540 横屏、540×960 竖屏的选择页、修为反馈、突破和近距离标签截图。
- 主编辑器同时运行其他任务的验证，早期记录曾混入外部菜单错误；最终报告采用隔离执行结果，并保存被验证源码 SHA-256。
- 夹具使用普通营地敌人验证额外定向奖励，避免把精英自身必掉装备混入一次性奖励数量；克制基线先清除随机起手武学。

## 验收边界

受控 Play Mode 不代表自然路线或装备平衡已经验证。仍需 10–20 局真人试玩观察追求完成率、路线取舍和 Boss 胜率，以及手机触摸、安全区和信息拥挤检查；资源沿用既有状态，不新增 Approved 声明。
