# 主角大招动作与帧内特效

本轮为现有五种满重流派绝学补齐 **角色专属八帧动作**。每帧包含角色与随手、剑、衣摆、脚部运动的特效，
外围法相仍作为背景配合。资源不是给原待机图加一个运行时光圈；蓄力、出手、回收具有不同人物姿态。

| 绝学 | 动作与烘焙进帧的特效 |
| --- | --- |
| 无影连环剑 | 蓄剑、举剑、疾刺、横斩、挑斩；青金剑芒与随身拖影 |
| 化功毒雾 | 沉身聚掌、转腰、推掌、封印收势；绿紫掌焰、莲瓣、缠臂气流 |
| 不动明王身 | 下沉架势、交臂、举手结印、震气开臂；身体金光、掌焰和脚边冲击 |
| 无相残影 | 后仰、低身闪避、突进回斩、落地收势；银蓝剑弧与动作残影 |
| 修罗血域 | 双手蓄剑、举剑、重劈、回旋收刀；赤金斩光、绕身火纹与余烬 |

触发门槛沿用上轮：对应流派绝学升至目录定义的满重（当前三重），在原战斗机制实际发动时触发。
演出共用原有 1.30 秒进度、4 秒视觉冷却。角色动作与外围特效用同一个事件和时钟，不新建伤害事件。

## 动作衔接

- 八帧为独立 Sprite；第 0、7 帧使用现有角色待机种子，保证进入和退出形象一致。
- 不同帧停留时间：进度结束点为 5.5%、16%、26%、38%、54%、72%、89%、100%，强调蓄力、释放与收势。
- 大招期间优先显示专属动作；普攻、普通技能、敌方攻击与毒发继续结算，但不会重置或覆盖角色的大招进度。
- 期间隐藏普通技能的贴身线条、旧独立拖影和旧待机分身，避免把旧出招效果叠到新姿势上；背景法相降至原亮度的一半。
- 普攻的前冲位移与受击横抖在大招姿态内不再叠加，人物动作和特效都围绕同一脚底锚点。
- 播放结束恢复最新普通动作或待机，战斗取消立即清理。三条核心计时规则保持原样。

## 生成和归一

使用内置 **image_gen**，以项目现有 `spr_hero_attack_basic_right_8f_v01.png` 的第一姿势为角色身份参考，
每种大招整条生成一次。完整提示词：[prompts.json](../../../ArtSource/Raw/Characters/HeroUltimates/prompts.json)。
原图五张保存在 `ArtSource/Raw/Characters/HeroUltimates/`，均为 2172×724、真透明背景的单行八帧。

`Tools/ArtPipeline/prepare_hero_ultimate_poses.cjs` 只做技术性切分和归一：

- 为生成原图不等宽排布记录人工核对的分格边界；少数剑芒/莲瓣使用随高度变化的接缝，避免切断相邻动作。
- 去除 alpha ≤ 8 的生成噪点，其余透明度保留；每条以所有出招帧的联合范围计算一个缩放。
- 输出严格 2048×256，八格各 256×256，脚底像素 y=223、Pivot=(0.5,0.125)、PPU=160。
- 大范围招式需要更宽留白，通过一次共享的显示缩放恢复与待机相同的角色基准身高；不放大地面阴影。
- 每格边界校验防止归一过程裁掉角色或光效；首尾锁回原待机。

预览：`contact_sheet.png`、`cast_impact.png`、4× 最近邻 `cast_impact_4x.png`、8/12 FPS GIF、
按演出节奏的 [cast_preview.gif](cast_preview.gif) 与紧凑版 [仅帧内特效预览](baked_effects_preview.gif)（末尾增加短待机停留方便对比）。`normalization.json` 保存分格、脚点、缩放与每帧边界。

## 文件清单

新增：

- `Assets/Resources/Characters/HeroAttacks/spr_hero_ultimate_{swift_sword,venom_mist,iron_guard,shadow_moon,blood_domain}_right_8f_v01.png` 及 `.meta`。
- `ArtSource/Raw/Characters/HeroUltimates/`：五张生成母版与提示词。
- `Tools/ArtPipeline/prepare_hero_ultimate_poses.cjs`：归一工具。
- 本目录的说明、预览、Play Mode 截图和验证报告。

修改：

- `Assets/Scripts/Visual/HeroUltimateArt.cs`：角色帧缓存、显示比例和非均匀逐帧节奏。
- `Assets/Scripts/UI/BattleScreenController.Ultimates.cs`：大招动作状态、旧效果清理与外围法相亮度。
- `Assets/Scripts/UI/BattleScreenController.HeroAttacks.cs`：预加载、专属帧优先级、普通特效抑制与显示比例。
- `Assets/Scripts/UI/BattleScreenController.HeroSelfVfx.cs`：大招期间停用普通贴身层。
- `Assets/Scripts/UI/BattleScreenController.cs`：完整动作播放和脚点稳定。
- `Assets/Editor/HeroAttackArtImporter.cs`：允许按指定路径切片，只导入本轮五条新资源。
- `Assets/Scripts/Debug/HeroAttackPlayModeProbe.Ultimates.cs`：真实触发、连击不打断、逐帧采样、结束恢复与横竖屏验证。
- `docs/hero_attack_pack.md`：接入索引。

## 运行和复测

无需新建 GameObject、手动挂脚本、Inspector 绑定或拖入资源。新条带已通过现有导入器切成 40 个 Sprite。
在 `MainPrototype` 进入 Play，将某一流派绝学升满，按它原有机制触发。

自动复测：编辑态在 MainPrototype 执行 `37 MiniGame/Validate Hero Attack Pack Play Mode`。
使用真实攻击、开场护盾、闪避和周期毒伤逻辑，输出横竖屏截图与受控八帧采样。
连续手动触发攻击用于检验表现不可打断，实际高速协程和三条计时规则仍由完整套件另行验证。
截图使用固定动作中段采样，播放自然结束单独检查；受控高血量与 600 秒地图夹具不改变正式 60 秒配置。

本轮没有新增运行时中文文案；沿用已通过字体校验的标题和技能名。
资源状态分别为 Generated / Normalized / Imported / InEngineQA；最终美术 Approved 与手机性能验收需要单独确认。
未重新构建 WebGL 包。新增五张未压缩角色图集约 10 MiB，平台压缩由现有纹理策略管理。

## 2026-09-21 最终验证

- Unity 6000.5.4f1 编译通过，五张角色图集均已切为八帧，共 **40 个新 Sprite**。
- 最终受控 Play Mode **690 项检查通过，运行错误为零**；报告：[playmode_report.json](playmode_report.json)。
- 八帧尺寸、Point、PPU、脚底 Pivot、真实绝学触发与角色替换、连续攻防/毒伤不打断、自然结束恢复、取消清理均通过。
- 开场明王护盾和实际强制闪避无需普通攻击事件即可切换专属角色帧。
- 横竖屏实测；竖屏逐一采样五套八帧，已目视检查动作、身体与特效随动、脚底和 HUD；Boss 同屏预警保留。
- 三种随机普攻、原武学动作和三条计时规则继续通过回归：普通战斗主时间继续，洞穴主时间暂停，Boss 独立计时。
- 新图集 WebGL 导入覆盖保留 2048px 尺寸；本轮没有构建/浏览器/真机性能结果。
- 当前状态 **InEngineQA**，未标记最终 Approved。
- [游戏内八帧采样动图](in_engine_casts.gif)、[出招画面对比](combat_preview.png)。动图按演出节奏组合受控帧采样，不冒充实时屏幕录制。
- 本轮最终仅移除了旧运行时待机分身；新帧中已烘焙的动作残影保留。
