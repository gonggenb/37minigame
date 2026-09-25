# A1 / B1 / C1 外置特效动画接入

日期：2026-09-24。Unity 6000.5.4f1。状态：`InEngineQA`。
用户已确认 A1、B1、C1 种子方向并授权动画制作；手机最终验收仍单列。

## 已接入

| 用户选择 | 图集 | 触发与表现 |
| --- | --- | --- |
| A1 月牙剑气 | `spr_vfx_hero_sword_qi_6f_v01` | 剑气诀、武器剑气、连环剑触发；从主角向敌人飞行 |
| B1 前冲掌劲 | `spr_vfx_hero_palm_force_6f_v01` | 毒掌为玉绿气浪；破甲掌同轮廓调为暖金，替换旧实体手掌 icon |
| C1 赤金斜劈 | `spr_vfx_hero_heavy_cleave_6f_v01` | 惊鸿、血气重斩；在敌方命中区域播放向右下方的斜劈，首击偏暖金、血气偏赤红 |

每套六个不同的 RGBA 帧，单帧 256×256，横条 1536×256；中心 pivot，PPU 256，Point，Clamp，
FullRect，无 Mipmap、无压缩。资源由内置 image_gen 按已选种子重新生成，真实透明通道保留。
源图、参考和完整提示词在 `ArtSource/HeroExternalVfx/`，归一报告为 [asset_report.json](asset_report.json)。

播放继承前轮修正：动作 22% 释放，持续动作时长的 72%，动作 94% 前收尾；高速出手同步压缩，
不以固定 12 FPS 拖长尾效。单次不循环，发射点锁定，目标继续跟随敌人，横竖屏改变时舍弃旧布局效果。
普通攻击图内效果、贴身层和满重大招保留；这里只替换外放主轮廓，并保留小范围命中反馈。
伤害、攻速、技能概率和计时逻辑没有修改。

三套图集在战斗场景 Awake 时预载，绘制只查缓存及选帧，不在出手时创建粒子对象或材质；
返回首页时随现有主角缓存释放。BootMenu 不预载该战斗资源。

## 文件清单

新增：

- `Assets/Resources/Effects/HeroExternal/`：三张正式运行图集及 Unity `.meta`，上级新增目录 `.meta`。
- `Assets/Scripts/Visual/HeroExternalVfxArt.cs` 及 `.meta`：资源缓存、帧顺序和非循环取帧。
- `Assets/Editor/HeroExternalVfxImporter.cs` 及 `.meta`：导入设置与六帧切片，重导入保留已有 Sprite ID。
- `Tools/ArtPipeline/prepare_hero_external_vfx.cjs`：透明/裁边/重复帧检查、统一缩放和图集打包。
- `ArtSource/HeroExternalVfx/`：生成原图、已选参考图、提示词和制作说明。
- 本验证目录：报告、深浅底接触表、逐帧 PNG、素材动图和 Game View 截图。

修改：

- `Assets/Scripts/UI/BattleScreenController.HeroSkillVfx.cs`：使用正式帧图替换程序外放弧线、旧掌印和主斩光；保留出招时序。
- `Assets/Scripts/UI/BattleScreenController.HeroAttacks.cs`：预载新帧图，移除旧掌法 icon 预载。
- `Assets/Scripts/Visual/HeroAttackArt.cs`：返回首页一并释放外放动画缓存。
- `Assets/Scripts/Debug/HeroAttackPlayModeProbe.cs`：新增 18 帧导入规格、帧顺序、非循环边界检查。
- `docs/hero_attack_pack.md`：链接当前正式图集说明。
- 既有 `hero_attacks`、`hero_ultimates`、`hero_ultimate_poses` 下探针报告/截图随回归刷新。

## 验证

- 归一脚本检查三张图的真实 Alpha、六格非空、边缘无裁切、18 帧分别有独立哈希；深浅底目视检查无黑底矩形或明显白边。
- Unity 编译成功，正式切片已实际执行；[playmode_report.json](playmode_report.json) 包含 758 项通过，运行错误为零。
- 960×540 与 540×960，剑气/毒掌/重斩的普通与高速出手共 24 张取样截图；另保存破甲和连环剑横竖屏样本。
- 人物与外放层使用同一出招相位取样；目视核对方向、实际大小、透明背景、命中位置和 HUD 可读性。
- 高速出手停止后旧外放层清理；取消战斗清空，普通攻击没有重复外放层，大招仍接管其自身演出。
- 普通战斗继续主倒计时；洞穴战斗暂停主倒计时；最终 Boss 推进独立计时，主时间保持。均通过真实协程检查。
- 检查完成退出 Play Mode，未保存测试夹具。无运行时中文新增，未触发平台构建。

## 预览

[animation_preview.gif](animation_preview.gif) 是三套素材的 12 FPS 循环审阅图，顺序为剑气、掌劲、重斩；
游戏内采用上述自适应一次性播放，不使用审阅图的循环节奏。

- [深底全帧](contact_dark.png) / [浅底全帧](contact_light.png)
- [横屏剑气飞行](screenshots/Cohesion_SwordQi_normal_landscape_flight.png)
- [横屏掌劲命中](screenshots/Cohesion_VenomPalm_normal_landscape_hit.png)
- [横屏重斩命中](screenshots/Cohesion_BloodCleave_normal_landscape_hit.png)
- [竖屏高速剑气](screenshots/Cohesion_SwordQi_fast_portrait_flight.png)
- [竖屏高速重斩](screenshots/Cohesion_BloodCleave_fast_portrait_hit.png)
- [暖金破甲掌](screenshots/Self_ArmorBreak_landscape.png)

## 运行、复测与手动绑定

打开 `Assets/Scenes/MainPrototype.unity` 进入 Play，学习对应武学后碰怪；起始青钢剑的追加剑气也会使用 A1。
不需要创建 GameObject、挂载新组件、拖入 Prefab/资源或修改 Inspector；Resources 自动加载，图集已切片。

重新导入：`37 MiniGame/Art/Reimport Hero External VFX`。
完整复测：在 `MainPrototype` 编辑态运行 `37 MiniGame/Validate Hero Attack Pack Play Mode`。
复测会临时调整 Game View 后恢复，不保存场景；新报告位于 `docs/validation/hero_attacks/playmode_report.json`。
本目录报告和截图为当前交付快照。

未完成事项：未重新打 WebGL 包，未做手机实机触控/帧率验收；这些不包含在 Editor 验证通过结论中。
