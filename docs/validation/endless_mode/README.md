# 无尽模式

## 运行

打开 `Assets/Scenes/BootMenu.unity`（或现有主场景），进入 Play Mode → 踏入江湖 → 选择关卡 → 无尽模式。
完成或跳过教学后入口开放，与第二关共用解锁条件。确认情报并选择起手武学后开始第一轮。
不需要新场景、GameObject、Prefab、Inspector 或素材绑定；沿用主题、按钮和随包中文字体。

## 流程

- 每轮使用第二关平川山镇、历练档、60 秒主地图。
- 第 30 秒中期 Boss 暂停主时间，胜利恢复探索；第 60 秒进入最终 Boss，独立计时。
- 普通战斗继续主时间；归零仍完成当前战斗及升级选择。洞穴暂停主时间。
- 最终 Boss 胜利自动进入下一轮，不经过结果页或起手选择；死亡才结算。
- 保留全部武学重数、秘传、装备、灵物、等级、属性、修为、铜钱、击杀和整次战斗统计。
- 保留 Boss 战后的剩余气血；进入每轮最终 Boss 仍按原规则回满。
- 刷新所有地图遭遇、采集、宝箱、洞穴内容、区域奖励与悬赏完成状态；保留装备追求目标。
- 回到出生点，重置望气视野、短时移速、每轮重选次数和两个 Boss 的状态/计时。
- Boss 倾向、天赋和悬赏目标整次固定。无尽胜利不写普通挑战或第三关解锁存档。
- 全部武学满重后仍获得等级/修为收益，但不弹出空白武学选择，防止长局卡死。
- 死亡页记录已通过轮数和止步轮数。重试清空本次成长，从第一轮开始；返回首页/普通选关清除模式。

## 首版数值

集中于 `Assets/Scripts/Runtime/EndlessModeTuning.cs`。第一轮与第二关历练一致，普通怪、精英、悬赏、
洞穴守卫/试炼、中期 Boss、最终 Boss 都参与强化。只改变战斗副本，重复预览不累乘，不改场景模板。

| 属性 | 每轮相对上一轮 |
| --- | --- |
| 最大气血 | ×1.30 |
| 攻击 | ×1.20 |
| 防御 | ×1.12 后 +1（零防御敌人也成长） |
| 攻速 | 每轮增加第一轮基础值的 5%，最高基础五倍 |
| 显示等级 | +1 |

强化的是核心作战数值，不提升概率类属性，避免后期必闪/必暴；移动速度和奖励数量仍按第二关规则。
指数计算对极端轮数保护，气血/攻击/防御计算饱和值为 1e20，防止 Infinity/NaN。
这是首版可调曲线，尚未完成自然路线长局平衡或真机验收。

## 文件

新增：
- `Assets/Scripts/GameFlow/GameFlowController.Endless.cs`：模式与跨轮状态。
- `Assets/Scripts/Runtime/EndlessModeTuning.cs`：强化参数和有限数值保护。
- `Assets/Scripts/Debug/EndlessModePlayModeProbe.cs`：Editor Play Mode 专项验证。
- 对应 `.meta` 与本验证目录。

修改：
- `GameFlowController.cs`、`GameFlowController.Challenges.cs`、`GameFlowController.RouteRewards.cs`、`LevelSequence.cs`。
- `EncounterTrigger.cs`、`GameTextCatalog.cs`、`EnemyLevelLabel.cs`。
- `PrototypeHUDController.cs`、`.MainMenu.cs`、`.Challenges.cs`、`.RunReview.cs`、`PortraitHudViews.cs`、`PortraitUiLayout.cs`。
- `docs/project_core.md`、`docs/gameplay_systems.md`。

## 验证

专项入口：`37 MiniGame/Validate Endless Mode Play Mode`。仅在 Play Mode 切场景，不保存测试夹具；
结束恢复原场景、Game View 尺寸、后台运行设置及解锁存档。测试使用受控气血/时间和真实战斗结束回调，
不把强制击败 Boss 算作自然通关或平衡证据。报告保存至本目录 `playmode_report.json`。

人工复测：连续完成两轮，确认武学重数保留、每轮中期 Boss 再出现、普通怪变强、采集与奖励刷新；
在任意战斗死亡，检查轮数统计，再试确认回到第一轮；返回主页再进入普通第二关，确认正常通关路径。
横屏 960×540、竖屏 540×960 截图由专项探针生成。中文校验使用 `37 MiniGame/Validate Chinese Fonts`。

2026-09-25：Unity 编译、中文字体校验和 Play Mode 50 项检查全部通过，运行时错误为 0。专项验证覆盖跨轮保存/刷新、普通战斗归零与升级衔接、
洞穴暂停、Boss 独立计时、四类死亡、重试/退出隔离、普通第二关回归、15 轮数据递增与极端轮数保护。
最终检查数量与结果以 `playmode_report.json` 为准。横竖屏截图已逐张查看，入口、情报、轮数与结算可读。
未完成：自然路线长局平衡、iOS/Android 真机触摸与设备性能。本次没有发布构建，也未将 UI 标记 Approved。
现有公共计时器/其他占位资源沿用项目状态；无尽模式没有新增美术或 PLACEHOLDER_UI 资源。

无尽模式现已接入 [局外修炼](../endless_progression/README.md)：战斗胜利保存专用金币，结算显示分类收益，
选关/结算可兑换技能点并升级永久属性。上述「重试清空本次成长」不清除局外钱包和永久修炼等级；
每次从第一轮开局施加一次永久属性。专项探针新增经济、持久化、去重及模式隔离检查，使用独立测试钱包。

2026-09-25 局外修炼接入后回归：最终 Play Mode 90 项通过、运行时错误为 0，中文 990 字形通过。
新增修炼页横竖屏截图 `progression_landscape.png` / `progression_portrait.png`；详细新增检查见局外修炼说明。
