# 第二关远行奖励

## 目的与规则

让玩家愿意花费有限的探索时间离开西南出生点，前往不同方向的高价值目标。沿途基础成长保留，
不延长 60 秒、不调整怪物战斗属性，不增加地图点位。

| 从出生点的可走路程 | 普通/精英/宝箱基础修为和铜钱 | 宝箱额外收益 |
| --- | --- | --- |
| 小于 35 米 | 1 倍 | 原有装备 |
| 35 米至不足 70 米 | 1.5 倍，四舍五入 | 原有装备 |
| 70 米及以上 | 2 倍 | 原有装备 + 一门已学武学升一重 |

武学只从已学且未满重的候选中抽取；全部满重或没有已学武学时，额外给予 20 铜钱。
先升级武学，再处理修为升级三选一，避免奖励列表保留刚刚满重的武学。宝箱沿用消耗与重开恢复逻辑。
装备仍沿用原有去重随机池，装备池耗尽时不会虚构新装备。敌人的额外随机掉落不受本次倍率影响。

编辑器使用 0.4 米网格和 0.34 米角色半径，八方向最短路径禁止斜穿障碍角点。
桥、河道、山石和建筑参与路径计算；触发式遭遇不作为障碍。它是可走路程估计，不是实际战斗耗时或真人路线。
距离写入 EncounterTrigger.rewardRouteDistance，基础奖励值保留；运行时只读取倍率，不进行全图寻路。
默认距离 -1 保留旧奖励，因此教学、第三关、旧 Prefab 和洞穴内部奖励不会因新增字段而获得倍率。

## 文件与接入

新增：
- `Assets/Scripts/Map/ExplorationRewardTuning.cs`：距离阈值、倍率与满重补偿。
- `Assets/Editor/PingchuanTownRewardBuilder.cs`：只应用到 MainPrototype 的路程采样和奖励报告。
- 上述脚本的 Unity `.meta` 文件。
- 本文、`exploration_rewards.json`、`exploration_playmode_report.json`、`exploration_runs_20.md/.csv`，以及四张 `exploration_*.png` 运行截图。

修改：
- `EncounterTrigger.cs`：保存烘焙路程并提供结算奖励。
- `GameFlowController.cs`：普通/精英和宝箱实际发奖、远端武学升级与补偿。
- `GameTextCatalog.cs`、`TreasureMapIndicator.cs`：复用原宝箱标识展示奖励档位和收益。
- `PingchuanTownLevelBuilder.cs`：以后重建第二关也自动应用远行奖励。
- `PingchuanTownPlayModeProbe.cs`：真实结算、重复触发、满重补偿、重开与计时回归。
- `Assets/Scenes/MainPrototype.unity`、`docs/gameplay_systems.md`、本目录 `README.md`：场景数据、当前规则与历史基线说明。

## 运行与复现

场景数据应用后无需新建 GameObject、挂载脚本或拖拽 Inspector 引用。打开 MainPrototype，进入 Play Mode，
从关卡选择进入第二关并选择起手武学。沿路观察丰厚宝箱与远行秘藏，打开后在角色面板核对资源和武学重数。

如移动点位、出生点或修改地图碰撞，先保存场景，退出 Play Mode，执行
`37 MiniGame/Apply Pingchuan Exploration Rewards` 重新计算并保存。
完整地图重建菜单也会重新计算；本次不需要重新导出环境或重建全部场景。

验证菜单：`37 MiniGame/Validate Pingchuan Town Play Mode`，然后执行 `37 MiniGame/Validate Chinese Fonts`。
探针覆盖近中远结算、重复触发无收益、重开不叠乘、满重补偿、远端战斗奖励传入，
以及八桥实际通行、河道阻挡、普通战斗继续计时、洞穴暂停、中期 Boss 恢复和最终 Boss 独立计时。

## 验证结果

2026-09-09 验证：

- Unity 编译通过，最终 Play Mode 的 Console 无错误；中文菜单校验通过，常规体与粗体覆盖 913 个非 ASCII 字形。
- 第二关已保存 73 个点位的距离字段，其中 58 个敌人/宝箱应用倍率：近程 5、中程 26、远程 27。
- 宝箱为近程 1、中程 2、远程 5；远程宝箱位于东部练武场、东南营地、北关及西北/东北洞区。
- 重复应用编辑器配置后，报告 SHA-256 完全一致，基础奖励不变；场景里无运行时寻路组件或新 GameObject。
- `exploration_playmode_report.json`：38 项实际 Play Mode 集成检查通过，含三条时间规则。
- `exploration_treasure_portrait.png`（540×960）与 `exploration_treasure_landscape.png`（960×540）：
  实际 Game View 中检查远行秘藏标题和收益提示，无缺字或文字截断。仅新提示通过此检查，不代表整个旧 HUD 或真机批准。
- [20 局自动样本](exploration_runs_20.md) / [逐局 CSV](exploration_runs_20.csv)：12/20 通关；中期 Boss 20/20 通过，最终 Boss 到达 19/20。
  平均 8.25 普通 + 1.90 精英战、0.95 个洞穴、等级 7.15、4 件新增装备；三条时间规则均通过。
- 相较同地图梯度前的历史 9/20 通关，这轮为 12/20，但随机奖励调用和物理路线会影响后续状态，不能把差值当作严格因果实验。
  就近探索仍为 5/5 胜，均衡 4/5、战斗优先 1/5、洞穴优先 2/5；本次实现了远端收益，尚未证明各路线达到平衡。
  平均 7.15 次武学选择也高于原 2–4 次目标，后续应一起评估升级节奏，不能把增加成长当成平衡已完成。

测试过程：先完成完整集成回归；保持原宝箱的装备抽取顺序后再次回归。一次已中断的跑局任务残留在 Editor SessionState，曾与集成探针同时启动导致暂停检查失败；清除该测试请求、分开运行后，38 项重新全部通过。最终报告为分开执行的结果。奖励菜单在场景未保存时会拒绝执行；最终保存后已重新应用成功，场景与奖励报告内容均与验证版本一致。
真人 10–20 局的路线取舍、奖励密度与手机观感仍需单独验收。
