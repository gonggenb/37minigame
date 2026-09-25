# 第二关：平川山镇

第二关 `MainPrototype` 已改用平川山镇 Blender 场景。关卡选择名称改为“平川山镇”，仍通过第一关解锁，胜利后进入原第三关“竹影幽谷”。

## 内容与规模

| 内容 | 旧第二关 | 新第二关 |
|---|---:|---:|
| 普通敌人 | 28 | 40 |
| 精英敌人 | 8 | 10 |
| 洞穴入口 | 8 | 5，与模型洞口匹配 |
| 宝箱 | 6 | 8 |
| 回复/增益药草 | 5 | 7 |
| 望气灵物 / 无名奇草 | 2 / 1 | 2 / 1 |

原始设计 240 × 220 米，以 0.4 比例导入，设计范围约 96 × 88 米，硬边界约 96 × 90 米。并未把原始 240 米尺度直接用于一分钟探索。

普通敌人按入口 6、城镇 6、练武场与连接道 10、营地 8、洞区 6、北关 4 布置。精英在支路与区域目标附近；普通敌人的原属性和基础奖励由原场景模板继承，没有随地图面积提升血量或伤害。五个物理洞口保留原随机内容规则。

后续已加入 [远行奖励梯度](exploration_rewards.md)：敌人和宝箱按出生点可走路程获得 1 / 1.5 / 2 倍基础修为与铜钱，五个远端宝箱额外提升一门已学武学。下面的地图接入与 20 局记录是加入梯度前的历史基线；梯度的实际场景数据、Play Mode 回归和新跑局报告以链接文档为准。

模型保留主街建筑、摊位、营地、练武设施、五个洞口和八座桥。游戏版约 124.9 万三角面，FBX 约 47 MB；按空间单元拆分渲染网格。源文件 591 万面保留，不被导出脚本覆盖。Unity 使用便携顶点色材质，未烘焙 Blender 程序纹理，因此游戏版不等同于 Blender 渲染质量。

河流只在桥面留缺口。建筑、桥栏、关门和营地围栏采用静态碰撞；树干保留简化网格碰撞，高山采用岩体占地近似碰撞。低坡按平面控制器作为可行走地表；道路、营地、洞口清场范围予以保留。显示层跟随地形和桥拱，碰撞根节点仍在 Y=0 平面。屋顶和树叶在角色前方局部淡出。

## 文件

修改：
- `Assets/Scenes/MainPrototype.unity`：第二关环境、敌人及拾取物、出生点、摄像机、光照和绑定。
- `Assets/Scripts/Player/PlayerController.cs`：第二关地表/桥面显示高度分支。
- `Assets/Scripts/Runtime/GameTextCatalog.cs`：统一第二关名称。
- `Assets/Scripts/GameFlow/GameFlowController.cs`：新地图开局路线提示；没有更改时间状态逻辑。
- `Assets/Editor/BambooValleyLevelBuilder.cs`：从保留模板重建第三关，清除继承的第二关环境与高度开关。
- `Assets/Editor/PrototypeSceneBuilder.cs`、`TutorialRestStopBuilder.cs`：重建已存在教学关时保留其独立地图，不再复制山镇环境。
- `Assets/Scripts/Debug/HeroDirectionalPlayModeProbe.cs`：脚点检查识别新地图高度。
- `docs/gameplay_systems.md`、`docs/unity_tech.md`：当前地图、敌人数量和接入方式。

新增：
- `ArtSource/Blender/PingchuanTown/export_to_unity.py`、`gameplay_layout.py`：游戏版导出及共享路线曲线。
- `Assets/Art/Environment/PingchuanTown/`：FBX、导出清单、Shader、材质。
- `Assets/Prefabs/Environment/PingchuanTown.prefab`、`PingchuanEncounterTemplates.prefab`：环境与保留的原敌人/事件模板。
- `Assets/Resources/PingchuanTownLayout.json`、`Assets/Scripts/Map/PingchuanTownLayout.cs`、`PingchuanTownVisibility.cs`：地形数据和显示适配。
- `Assets/Editor/PingchuanTownLevelBuilder.cs`、`PingchuanTownNavigationValidation.cs`：可重复构建、数量/净空/可达性校验。
- `Assets/Scripts/Debug/PingchuanTownPlayModeProbe.cs`：真实 Rigidbody 与关卡时序集成探针。
- 本目录：`placements.json`、`navigation.json`、`playmode_report.json`、运行截图和跑局证据。

## 运行、重建与测试

已经完成场景和 Prefab 绑定，无需额外拖拽 Inspector 引用。打开 `Assets/Scenes/MainPrototype.unity` 后进入 Play Mode，开始或在关卡选择中选择第二关（仍遵守教学关解锁条件）。

重建时先保存当前场景并退出 Play Mode，执行 `37 MiniGame/Build Pingchuan Town Level 2`。不要执行旧的 `Expand Main Map` 或旧河流扩展菜单覆盖此场景。重建从保留模板读取敌人，适合重复执行；手动布怪修改应同步到构建器。

验证菜单：
1. `37 MiniGame/Validate Pingchuan Town Level 2`：数量、净空、河道排除和关键绑定。
2. `37 MiniGame/Validate Pingchuan Town Reachability`：以角色半径采样碰撞空间，检查 73 个点位从出生点可达。
3. `37 MiniGame/Validate Pingchuan Town Play Mode`：八桥实际行走、非桥河段阻挡、碰怪进洞、30 秒中期 Boss、60 秒最终 Boss、重开恢复。
4. `37 MiniGame/Validate Chinese Fonts`：中文名称和路线提示字形校验。

普通战斗继续消耗主地图时间；洞穴战斗暂停主地图时间；最终 Boss 独立计时。中期 Boss 仍在第 30 秒触发、暂停主时间并在胜利后恢复；进入中期 Boss 不自动回满血。

## 验证范围

以 JSON 报告的 `success` 和逐条结果为准。自动跑局使用既有五流派和固定种子，是真实游戏状态与角色运动的自动样本，但不是人工路线质量或触屏体验验收。静态可达性不是 NavMesh 烘焙，也不代表每段窄路均已人工走查。

待人工/设备验收：手机 GPU/内存/加载性能、触控窄路体验、最终美术观感与长期平衡；当前尚未制作完整贴图烘焙或多级 LOD，未标记最终 Approved。


## 本次实测结果（2026-09-09）

- Unity 编译无错误，中文校验覆盖 898 个非 ASCII 字形并通过。
- `playmode_report.json`：22 项集成检查全部通过。
- `navigation.json`：73 个遭遇/拾取点位全部可达；含山石、树干、建筑和河流碰撞。
- 编辑器地图总览（为展示全图暂时关闭距离雾）：`unity_overview.png`；竖屏真实运行截图：`playmode_market.png`、`playmode_plain.png`。
- [20 局固定种子报告](runs_20.md) / [逐局 CSV](runs_20.csv)：五起手、四路线策略；平均 8.70 普通 + 1.45 精英、1.35 洞穴；中期 Boss 通过 20/20，最终 Boss 到达 18/20、最终通关 9/20。计时规则在自动样本中也通过。

均衡路线平均 8.4 场，接近原先 5–8 场目标的上沿；就近探索平均 14.8 场、5/5 胜，收益显著高于其他自动策略。这个样本提示近邻连续奖励仍需人工路线验收和后续调优；本次没有同时修改流派数值、Boss 难度或奖励经济来掩盖差异。固定种子测试按五个起手分别搭配四个不同种子/策略，不能据此单独断言流派强弱。
