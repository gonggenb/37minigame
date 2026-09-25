# 满重流派绝学演出

用户确定的「武学至高」门槛：**对应流派绝学达到目录定义的最高重数**。当前五种绝学均为三重。
取得或升满绝学本身不播放；只在既有战斗逻辑实际发动该绝学时播放专属大招演出。
普通武学即使满重也不触发，其他流派满重不会替当前武学解锁演出。

| 满重绝学 | 实际触发时点 | 专属视觉 |
| --- | --- | --- |
| 无影连环剑 | 连击追加伤害发动（三重时每三次命中） | 青金七剑法相、剑光向敌人汇聚、冲击环 |
| 化功毒雾 | 敌人已有毒层时的周期毒发 | 碧紫毒莲、旋转雾环、扩散毒光 |
| 不动明王身 | 每场战斗开始生成护盾 | 金钟法相、脚下三层震环 |
| 无相残影 | 固定间隔的强制闪避（三重时每第四次敌方攻击） | 银蓝月轮、四道残影、月弧 |
| 修罗血域 | 主角暴击发动血域 | 赤金血轮、旋转刀芒、交叉斩击 |

演出持续 1.30 秒，按未缩放时间播放。五种演出共用每场战斗 4 秒的视觉间隔；
同帧多个满重绝学以首个发动者演出，其他技能伤害、护盾、毒伤、闪避等继续照常结算。
换战斗清空视觉间隔，因此金钟开场不会被上一场的特效挡住。不会新增伤害、额外技能、停表或慢动作。

视觉分为战场压暗、角色后方法相、角色前方释放三层；复用现有角色动作和像素绘制工具。
范围裁剪到战斗舞台，法相底缘跟随主角脚底；竖屏尺寸按人物上方可用空间约束。
血条和主计时位于舞台外，伤害数字在特效上层。
「至高绝学」标题沿用现有深墨底、金框与随包中文字体；Boss 技能预警出现时让出标题位置，同一绝学的普通技能名提示不再重复绘制。
普通技能的贴身效果和三种随机普攻继续使用原机制。

## 文件清单

新增运行时文件及 Unity `.meta`：

- `Assets/Scripts/MartialArts/MartialUltimateCatalog.cs`：五种绝学、素材映射、满重判定、视觉时长。
- `Assets/Scripts/Battle/BattleManager.Ultimates.cs`：独立事件序列、战斗序号、视觉冷却。
- `Assets/Scripts/UI/BattleScreenController.Ultimates.cs`：三层演出、生命周期、Boss 标题避让。
- `Assets/Scripts/Visual/HeroUltimateArt.cs`：五张 Resources 贴图的加载缓存。
- `Assets/Resources/HeroUltimates/`：五张 512×512 RGBA 贴图。
- `Assets/Editor/HeroUltimateArtImporter.cs`：Point、无 Mipmap、不可读、512px 导入设置。
- `Assets/Scripts/Debug/HeroAttackPlayModeProbe.Ultimates.cs`：触发门槛与实际战斗钩子的 Play Mode 验证。
- `ArtSource/Raw/VFX/HeroUltimates/`：原始生成图与完整提示词 `prompts.json`。
- `Tools/ArtPipeline/prepare_hero_ultimates.cjs`：保留透明度的尺寸归一与安全边距，生成对比图。

修改：

- `BattleManager.cs`：在武学实际激活处记录演出，在取消/新战斗时清理。
- `BattleScreenController.cs`：观察事件，在角色前后分别绘制。
- `BattleScreenController.HeroAttacks.cs`：随战斗角色预加载特效贴图。
- `HeroAttackArt.cs`：现有显式释放入口同步清空大招缓存。
- `GameTextCatalog.cs`：集中维护新增标题「至高绝学」。
- `HeroAttackPlayModeProbe.cs`：接入验证，保留普攻、技能与计时回归。
- `docs/hero_attack_pack.md` 与本目录报告、横竖屏截图。

## 运行与测试

不需要新建 GameObject、挂载脚本、拖入素材或手动绑定 Inspector。
打开 `Assets/Scenes/MainPrototype.unity`，进入 Play，学习并将对应流派绝学升满，按上表条件发动。
仍遵守三条时间规则：普通地图战斗继续主时间，洞穴战斗暂停主时间，最终 Boss 独立计时。

复现自动验证：编辑态在 MainPrototype 执行 `37 MiniGame/Validate Hero Attack Pack Play Mode`。
临时夹具通过真实 `DoAttack`、开场护盾、周期毒发等代码触发效果，并在 960×540、540×960 截图。
截图固定在动作或演出的中段，避免编辑器偶发帧延迟错过短动作；自然结束单独验证。
低重数和视觉冷却边界以受控激活/时间字段检查；不将冷却夹具描述为等待四秒的真人试玩。
检查结束退出 Play 并恢复 Game View，不保存夹具状态。
报告为 `playmode_report.json`；对应完整回归报告也写入 `../hero_attacks/playmode_report.json`。
字体校验使用 `37 MiniGame/Validate Chinese Fonts`。

贴图生命周期：五张纹理首次加载后共用，逐帧不新建纹理、材质或粒子对象，不使用战斗随机数。
桌面未压缩纹理合计约 5 MiB；各平台最终压缩遵循项目纹理策略，本轮未重建 WebGL 包。

## 验收边界

生成、透明底归一、Unity 导入、Play Mode 触发验证和横竖屏截图目视核对分别记录。
手机/GPU 性能、连续实战的最终节奏感与用户美术批准仍需单独验收；未标记最终 Approved。

## 2026-09-21 最终验证记录

- Unity 6000.5.4f1 编译完成，最终实际 Play Mode **565 项检查通过**，运行错误为零。
- 五种绝学分别检查零重、一重、二重不触发，以及三重真实战斗机制触发；普通满重不触发。
- 横竖屏分别验证独立事件、特效生命周期、共享视觉冷却、同帧混合流派、取消和重新开战。
- 三种随机普攻和贴身技能效果回归通过；普通主时间继续、洞穴暂停、最终 Boss 独立计时均通过真实协程验证。
- Boss 妖甲阶段自然推进时与大招同屏验证通过；目视确认 Boss 图标、名称和解释位于最上层，大招标题让位。
- 已执行 Unity 菜单 `37 MiniGame/Validate Chinese Fonts`，校验通过。
- 已目视核对最终横竖屏截图；法相与脚底对齐，未覆盖 HUD 血条和计时。
- [实战局部对比](combat_preview.png)：从左到右、从上到下为七剑、毒莲、金钟、月影、血轮。
- [横屏完整截图](landscape_screenshots.png)、[竖屏完整截图](portrait_screenshots.png)、[Boss 预警同屏](boss_warning_portrait.png)。
- 当前状态为 `InEngineQA`。截图使用高血量和延长主地图时间的临时夹具，不代表正式数值或更改正式 60 秒规则。
- 中途一次短动作截图检查受编辑器帧延迟影响；已将截图采样固定到动作中段，保留独立自然结束检查，最终复测通过。
