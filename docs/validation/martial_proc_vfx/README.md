# 四类武学特效接入

2026-09-25。四套六帧外置特效已通过 Resources 接入现有 Unity 自动战斗。

## 绑定与触发

| 动画 | 绑定武学 | 结算条件与表现 |
| --- | --- | --- |
| 碎金穿劲 | 破甲掌 | 成功命中后，敌人腹部出现窄金劲气与少量碎甲；玄铁戒、腐骨手套复用 |
| 双燕追锋 | 无影连环剑 | 按已学重数每 5 / 4 / 3 次成功命中触发追加伤害时，播放双弧连斩；疾剑式只有攻速加成，不伪装成连击 |
| 金钟回波 | 反震诀 | 玩家受击或护盾实际吸收伤害后，身前护弧压缩并弹出短波；金钟罩仍只提供护盾 |
| 赤玉归元 | 饮血刀法、吸星诀 | 实际吸血或毒伤回血时，红玉光点由敌人回到玩家胸腹；以毒养血、装备等吸血来源共用，满血不播放 |

闪避回血继续使用普通玉绿回复，敌人自身吸血不会把光点送给玩家。未增加武学，没有修改伤害、触发频率、暴击随机数或回血数值。

## 实现

- `BattleManager.MartialProcs.cs`：32 条固定容量结算记录，同帧普攻、反击与毒伤不会互相覆盖；取消或换战斗时清空并更新战斗版本。
- `BattleScreenController.MartialProcs.cs`：每种最多两个活动实例、共八个；角色锚点按当前布局计算，切换横竖屏清理旧布局实例。
- 基础播放时长 0.5 秒（12 FPS），按战斗倍率缩放，使用 `Time.time`，跟随暂停与慢放。
- 破甲和连击替换原通用掌劲/剑气层；反震替换旧冷白冲击；吸血替换旧普通回血爆点，避免重复叠亮。
- 吸血路径在动画前 70% 从敌人移动到玩家，后段固定在胸腹收束。全程不改变游戏结算时机。
- 资源预加载、Point 采样、固定中心，不在 OnGUI 中创建贴图或改写像素。

## 文件清单

修改：

- `Assets/Scripts/Battle/BattleManager.cs`：在真实破甲、连击、反震、实际吸血处记录事件。
- `Assets/Scripts/Battle/BattleVfxCue.cs`：区分吸血与其他回血。
- `Assets/Scripts/UI/BattleScreenController.cs`：读取并绘制新效果，去除重复的旧反馈。
- `Assets/Scripts/UI/BattleScreenController.HeroAttacks.cs`：预加载图集。
- `Assets/Scripts/UI/BattleScreenController.HeroSkillVfx.cs`：保留角色动作，替换对应的旧外放层。
- `docs/battle_vfx_catalog.md`：同步绑定说明。

新增：

- `Assets/Scripts/Battle/BattleManager.MartialProcs.cs`。
- `Assets/Scripts/Visual/MartialProcVfxArt.cs`。
- `Assets/Scripts/UI/BattleScreenController.MartialProcs.cs`。
- `Assets/Editor/MartialProcVfxImporter.cs`。
- `Assets/Scripts/Debug/MartialProcVfxPlayModeProbe.cs`（仅 Editor）。
- `Assets/Resources/Effects/MartialProcs/` 四张图集及 Unity `.meta`。
- `ArtSource/MartialProcVfx/` 母版、提示词与来源记录。
- `Tools/ArtPipeline/prepare_martial_proc_vfx.cjs` 与本目录证据。

无需创建新 GameObject、挂组件或 Inspector 绑定；场景与 Prefab 没有改动。

## 运行与检查

首次接入结果：专项 Play Mode **121 项通过**，原攻击动画回归 **758 项通过**，合计 **879 项**；两份报告均 `success: true` 且无运行时错误。24 个 Sprite 已在 Unity 加载。最终退出 Play Mode，恢复 `MainPrototype`，场景 `isDirty: false`；`git diff --check` 通过。后续尺寸调整单独记录如下，未重复执行 758 项攻击回归。

打开 `Assets/Scenes/MainPrototype.unity` 进入 Play Mode。学习上述武学后进入战斗即可；无影连环剑等待命中次数达到阈值，吸血需要玩家缺血。

专项检查：Unity 菜单 `37 MiniGame/Validate Martial Proc VFX Play Mode`。会临时进入 Play Mode，使用真实结算方法和实际战斗协程，检查完退出并恢复 Game View。请在编辑模式运行，不要在已有试玩中启动。

- [本轮 Play Mode 报告](playmode/playmode_report.json)：资源和帧序、武学正负触发、装备协同、满血/闪避/敌人吸血排除、同帧事件、有限叠加、暂停/慢放、横竖屏切换、取消清理与高攻速实际战斗。
- [原攻击动画回归报告](hero_regression_report.json)：普通攻击、剑气、毒掌、重斩、自身特效和绝学兼容性。
- [资源检查](asset_report.json)：24 个不同帧、真实透明、无跨格裁切、统一缩放和中心。
- [动画预览](animation_preview.gif)：从左到右为破甲、连击、反震、吸血，循环仅供审阅，游戏内单次播放。
- `playmode/` 内为实际 Game View 截图。测试用敌人名称、大血量与 600 秒仅为取证夹具，不代表正式数值。

时间规则使用真实流程验证：普通战斗继续消耗主时间；洞穴战斗暂停主时间；最终 Boss 主时间不动且独立战斗时间推进。

状态：Generated / Normalized / Imported / Editor InEngineQA。未进行本轮 WebGL 发布构建、手机真机性能和触摸验收；不将 Editor 通过视为最终设备批准。

## 放大调整（2026-09-25）

用户反馈特效偏小后，调整 `BattleScreenController.MartialProcs.cs` 的显示尺寸：相对首次接入，破甲宽高为 **1.8 倍**，连击 **1.7 倍**，反震 **1.4 倍**，吸血 **1.6 倍**。图集、伤害、触发频率、播放时长与实例上限保持原配置。

反震护弧随尺寸增加略向前移，保留玩家面部与动作；吸血发射尾部贴近敌人，接收点仍落在玩家胸腹，防止放大后从屏幕右侧溢出。

- 新增 [前后对比页](size_review/comparison.html) 和 [对比截图](size_review/comparison.png)，`size_review/before/` 保留调整前 33 张截图与专项报告。对比来自不同运行，背景及动画采样可能相差一帧。
- 最新专项报告时间 **2026-09-25T11:08:39Z**，**121 项通过**，`findings`、`runtimeErrors` 均为空。包含横竖屏、混合高攻速、暂停慢放、清理及三条核心时间规则。
- 逐项查看四套特效横竖屏关键帧、吸血起止点和竖屏混合实战截图：放大后未侵入血条、伤害数字与底部战报，角色脚点仍清晰。高攻速时效果更密集，仍限制为每类最多两层。
- 无新增运行时文件、资源或手动绑定；本轮只修改显示脚本、此记录和特效账本，更新 Play Mode 证据。Unity 无编译/运行错误，已退出 Play Mode，`MainPrototype` 无未保存场景修改。
- 手机真机性能与最终视觉验收仍待执行。
