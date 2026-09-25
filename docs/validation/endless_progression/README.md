# 无尽局外修炼

## 运行与边界

从 `Assets/Scenes/BootMenu.unity` 进入 Play Mode → 踏入江湖 → 选关页右上角「无尽修炼」。
无尽死亡结算也可直接进入修炼页，兑换或升级后返回结算并「从第一轮再战」。
无尽入口继续沿用完成/跳过教学的解锁条件。修炼页可提前查看，但没有赠送金币。
不需要创建 GameObject、挂载脚本、绑定 Inspector、Prefab、字体或图标。

本系统为局外永久属性成长；局内武学、重数、秘传和装备仍按原有构筑规则获得。
技能点只能通过无尽金币兑换；无尽金币和局内铜钱分开保存。升级只影响下一次无尽挑战，
不改变已结束的本局属性；普通三关、教学及第二关挑战难度不吃永久加成。

## 表现奖励

| 表现 | 无尽金币 |
| --- | ---: |
| 普通敌人胜利 | 2 |
| 精英/悬赏目标胜利 | 5 |
| 洞穴战斗胜利 | 4 |
| 中期 Boss 胜利 | 12 |
| 最终 Boss 胜利、通过一轮 | 30 |

每次胜利记账并立即保存；死亡页汇总分类次数与总收益，不再次发同一份奖励。
中途返回主页保留已经赢得的金币；退出应用保留已经写入的金币，但不提供无尽战局续玩。
零胜利死亡收益为零。不按挂机时长、进洞次数、铜钱或购买次数发金币。
同一局用唯一 ID 和累计已到账金额去重；新局拒绝旧局补领，兑换花掉金币后重开结算也不能重新领取。

## 技能点与数值

25 无尽金币兑换 1 技能点，每次点击兑换 1 点，余额不足时不可兑换。
各技能独立升级，初始 0 级、上限 10 级；升级前的 0–2 / 3–5 / 6–8 / 9 级分别消耗 1 / 2 / 3 / 4 点。
单项升满需要 22 点，全部升满需要 132 点；全部满级后关闭兑换，保留剩余货币。
购买前显示当前效果、下一等级效果、点数消耗和不足/满级状态。

| 修炼 | 每级加成 | 10 级累计 |
| --- | --- | --- |
| 养元功 | 气血上限 +5% | +50% |
| 破锋诀 | 攻击 +3% | +30% |
| 护体功 | 防御 +0.5 | +5 |
| 疾风诀 | 攻速 +2% | +20% |
| 凝神诀 | 暴击率 +1 个百分点 | +10 个百分点 |
| 灵燕步 | 闪避率 +1 个百分点 | +10 个百分点 |

百分比加成为相应开局属性的线性增量，基于角色与起始装备重置后的属性一次性施加。
跨轮不会再次叠乘；重试先重置角色，再读存档施加一次。概率通过现有 0–1 范围保护。
首版曲线集中在 `EndlessProgressionCatalog`，仍需自然路线试玩验证成长速度和长期平衡。

## 存档与显示

本地 `PlayerPrefs` 单 JSON key：`WuxiaRoguelite.EndlessProgression.v1`。
包含版本、金币、未用技能点、六项稳定索引等级、最高通过轮数、当前局 ID 和累计到账金额。
金币、技能点、技能等级在同一笔 JSON 更新中写入并 Save；没有账号、联网或云存档。
读取时限制负数、越界等级和数值上限；无法解析或遇到不支持的版本时保留原档并拒绝写入。
本地数据不是防作弊存档。钱包及每局统计上限为 10 亿，避免数值溢出。

横竖屏复用既有暗木/黄铜九宫格、字体与按钮；横屏两列、竖屏单列滚动。
使用 `Resources/Icons` 已有六张武学图标，稳定 iconId 位于目录类；无新增美术或 PLACEHOLDER_UI。
修炼页面只在 Ready/Result 可操作，独立拦截底层菜单/结算；购买后显示明确反馈。

## 文件

新增：
- `Assets/Scripts/Runtime/EndlessProgressionCatalog.cs`：配置、专名、稳定图标 ID、属性计算。
- `Assets/Scripts/Runtime/EndlessProgression.cs`：本地存档、交易、累计入账去重。
- `Assets/Scripts/GameFlow/GameFlowController.EndlessProgression.cs`：表现记录、消费状态限制、开局加成。
- `Assets/Scripts/UI/PrototypeHUDController.EndlessProgression.cs`：修炼与收益明细。
- `Assets/Scripts/Debug/EndlessModePlayModeProbe.Progression.cs`：经济与流程回归检查。
- 对应 `.meta` 和本说明。

修改：`GameFlowController.cs`、`GameFlowController.Endless.cs`、`PrototypeHUDController.cs`、
`PrototypeHUDController.MainMenu.cs`、`PrototypeHUDController.RunReview.cs`、`EndlessModePlayModeProbe.cs`、
`docs/project_core.md`、`docs/gameplay_systems.md`、`docs/validation/endless_mode/README.md`。

## 验证与人工复测

菜单 `37 MiniGame/Validate Chinese Fonts`，随后 `37 MiniGame/Validate Endless Mode Play Mode`。
扩展原有无尽模式探针，保留三条时间规则、跨轮、死亡与普通第二关回归。
修炼探针使用隔离的钱包，结束删除测试 key，原用户金币和技能等级不参与测试、不被覆盖。
报告及横竖屏截图位于 `../endless_mode/`，自动测试通过与视觉/真机验收分别记录。

人工复测：
1. 无修炼存档进入无尽，击败敌人后死亡，核对分类奖励与余额。
2. 25 金币兑换 1 点，升级破锋诀；再战确认攻击增加，再过一轮确认不重复加成。
3. 返回主页、重新进入 Play Mode，确认钱包和永久等级保留。
4. 普通第二关和教学确认使用原属性，三条时间规则保持不变。
5. 960×540、540×960 检查滚动、触摸、满级与余额不足提示；真机另行验证。

当前验证状态见下方记录；未完成自然路线多局平衡、移动真机和最终美术批准，本次不发布构建。

### 2026-09-25 验证结果

- Unity 6000.5.4f1 编译通过，无编译错误。
- 执行 `37 MiniGame/Validate Chinese Fonts`：990 个非 ASCII 字形通过，常规体/粗体与文本完整性通过；无需更新字体子集。
- Editor Play Mode 最终 **90 项检查通过**，`runtimeErrors: []`。
- 实际战斗完成回调验证普通、精英、洞穴、中期 Boss、最终 Boss 奖励；零胜利为零金币。
- 验证 24/25 金币兑换边界、余额不足、所有技能满级、点数递增、六项属性数值、读回持久化、旧局/重复结算拒绝、异常版本存档不覆盖。
- 验证重试应用一次、跨轮不叠加、退出保留已赚金币；普通第二关与教学不吃永久属性、普通 Boss 不发无尽金币。
- 保留普通战斗持续消耗主时间、洞穴暂停、最终 Boss 独立计时的实机引擎探针检查。
- 已查看 960×540 与 540×960 的选关入口、修炼页、结算页截图。新页面为 `InEngineQA`，不是 `Approved`。
- 探针使用受控属性/时间与强制死亡来覆盖流程，不是自然通关或经济平衡证据；钱包隔离避免污染用户存档。

证据：[完整报告](../endless_mode/playmode_report.json)、[竖屏修炼](../endless_mode/progression_portrait.png)、
[横屏修炼](../endless_mode/progression_landscape.png)、[竖屏结算](../endless_mode/result_portrait.png)。
