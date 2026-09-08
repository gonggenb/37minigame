# 第一关：山脚古道的小歇脚地 — Unity 接入

## 已实现

- 现有 `Assets/Scenes/TutorialLevel.unity` 保留流程与 UI，替换为 Blender 歇脚地模型。场景 GUID 和关卡序列保持原有引用。
- 独立环境 Prefab：`Assets/Prefabs/Environment/TutorialRestStop.prefab`。FBX 与 32 个材质位于 `Assets/Art/Environment/TutorialRestStop`。
- 南侧短桥出生；西北驿亭宝箱 `(-5.2,0,4.15)`；东北洞口 `(5.65,0,4.1)`；西南药圃 `(-5.55,0,-2.1)`；东南敌人 `(4.6,0,-2.15)`。坐标为 Unity X/Y/Z，Y 为碰撞平面。
- 桥侧、溪流、边界、中央树与家具、帐篷、火堆、推车、洞壁、亭柱和平台侧缘采用简化碰撞体。角色碰撞根节点维持平面，视觉子节点跟随桥面和台阶高度。
- 宝箱和药草使用模型中的独立网格，采集后消失，重开恢复。旧宝箱/药草精灵移除；现有互动提示和敌人精灵保留。
- 树叶、竹子及屋瓦在镜头与角色之间进行抖动淡出。独立 shader 全局参数，不影响竹影幽谷场景。
- 暖色日光、局部橙色灯火、冷色环境光和远景雾。采用可移植顶点色材质，保留屋瓦、石板、竹节、灯笼等真实几何；Blender 程序节点、体积光和景深未逐项复刻。
- 删除旧教学场景中禁用的正式关卡遭遇，修复 `StartRun` 重新激活旧敌人的问题。
- 教学首次进入执行 `PlayerStats.ResetRun`，与重玩时统一初始化基础属性及起始装备。HUD 的 ×1000 数值显示保持不变。
- 教学敌人和宝箱各给 20 修为，任意一个都能达到第一阶所需 18 修为，体验一次三选一。

## 新手关卡应该教什么

| 场景动作 | 玩家学到什么 | 当前实现 |
|---|---|---|
| 从短桥走进歇脚地 | 摇杆/拖动或 WASD 移动、触碰互动 | 开场说明，点击后才开始 30 秒计时 |
| 靠近营地敌人 | 战斗自动进行；普通战斗仍消耗时间 | 首触说明，确认后真实自动战斗 |
| 进入旧驿亭开箱 | 无战斗成长，装备与修为奖励 | 20 修为保证第一次武学选择 |
| 获得第一次成长 | 武学自动生效，可以选适合自己的效果 | 首次升级说明，三选一暂停时间 |
| 顺路采药 | 药草立即生效；回血适合受伤后取 | 首触说明、采集、模型消失 |
| 进入山洞再返回 | 洞内主时间暂停，是路线收益选择 | 首触说明，复用真实洞穴房间/出口 |
| 30 秒结束后打守关人 | 用本局成长挑战最终对手 | 回满气血，独立 Boss 计时；胜利解锁第二关 |

建议验收目标是“玩家能自己移动、体验一次成长、理解自动战斗与时间”，不要求 30 秒收齐全部四点。保留自由路线与跳过；中央茶桌、帐篷、推车本轮只作环境，不新增对话、商店或强制任务链。下一轮真人试玩观察第一次触碰所需时间、是否看见武学选择、是否知道洞穴怎么退出，再判断是否需要缩短说明或移动节点。

## 重建与运行

1. 正常运行：打开 `TutorialLevel.unity`，进入 Play Mode，点击“开始探索”；或从首页关卡选择进入关卡1。
2. 仅重适配 Unity：打开已保存的 `TutorialLevel.unity`，退出 Play Mode，执行 `37 MiniGame/Adapt Tutorial Rest Stop`。此菜单替换该场景的环境并重新绑定，无手动 Inspector 引用要补。
3. 重新导出模型：
   ```sh
   /Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Blender/TutorialRestStop/TutorialRestStop_v01.blend --python ArtSource/Blender/TutorialRestStop/export_to_unity.py
   ```
   然后等待 Unity 导入，执行上述适配菜单。导出不会改写 Blender 母版。
4. `PrototypeSceneBuilder` 的旧 `Build Tutorial Level` 会复制正式关卡重建旧教学壳；如使用它，必须再运行歇脚地适配菜单。
5. 运行时中文 UI 沿用项目 `RuntimeChineseFont`；本轮不新增 UI 文案和字体字形。

## 验证与限制

受控 Play Mode 检查菜单为 `37 MiniGame/Validate Tutorial Rest Stop Play Mode`；结果写入 `docs/validation/tutorial_reststop/playmode_report.json`。测试使用真实 Rigidbody 输入、首次触碰和战斗阶段；为单独验证阶段规则，会传送角色、临时设置战斗血量，不能当成自然通关或平衡证明。测试恢复教学解锁偏好，不将测试数值保存到场景。

- 普通战斗主计时继续；洞内战斗暂停主计时；最终 Boss 独立计时。教学仍为 30 秒，正式地图仍为 60 秒。
- 当前环境有 618 个空间拆分网格、约 60.4 万导出顶点、51.5 万多边形，外圈植被关闭实时投影。未完成真机 GPU/内存/耗电、WebGL 包体与加载时间验收；移动端发行前应做纹理烘焙、LOD、合批与更精简的远景版本。
- 平面 UV 用于可移植顶点色，并非正式 PBR 烘焙 UV。近景几何比白盒精细，但不能称为已经完成八方旅人级别的光影复刻。
- 本轮未替换正式关卡、战斗 UI 或洞穴内部美术，也未发布构建。

2026-09-08 验证结果：受控 Play Mode **34 项通过**，中文字体 **898 个非 ASCII 字形通过**，540×960 竖屏与 960×540 横屏实际截图已检查。Console 无错误，结束时 TutorialLevel 已保存且退出 Play Mode。

## 本轮文件清单

修改：`Assets/Scenes/TutorialLevel.unity`、`Assets/Scripts/Player/PlayerController.cs`、`Assets/Scripts/GameFlow/GameFlowController.cs`、`docs/project_core.md`、`docs/gameplay_systems.md`、`docs/unity_tech.md`，以及 Blender 交付目录的 README/manifest 接入状态。

新增：`Assets/Editor/TutorialRestStopBuilder.cs`、`Assets/Scripts/Map/TutorialRestStopLayout.cs`、`Assets/Scripts/Map/TutorialRestStopProps.cs`、`Assets/Scripts/Debug/TutorialRestStopPlayModeProbe.cs` 及对应 Unity meta；`Assets/Art/Environment/TutorialRestStop/` 的 FBX、导出清单、shader、32 个材质；`Assets/Prefabs/Environment/TutorialRestStop.prefab`；`ArtSource/Blender/TutorialRestStop/export_to_unity.py`；本说明与 `docs/validation/tutorial_reststop/` 下的报告和截图。
