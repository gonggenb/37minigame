# 主角攻击帧动画接入

## 动作与技能

| 动作 | 实际触发 | 视觉时长上限 |
| --- | --- | --- |
| 普通攻击 `basic` | 无更高优先级出招事件；使用用户照片的八个姿势 | 0.48 秒 |
| 剑气连斩 `sword_qi` | 剑气诀、无影连环剑，以及武器追加剑气 | 0.40 秒 |
| 推掌 `venom_palm` | 毒砂掌／装备施毒、破甲掌 | 0.52 秒 |
| 蓄力重劈 `blood_cleave` | 惊鸿一式首击、修罗血域暴击爆发、血气增伤 | 0.60 秒 |

同次攻击有多个效果时优先级为：爆发／首击 → 剑气／连击 → 施毒／破甲 → 血气增伤 → 普攻。
毒伤每秒结算、持续毒雾、护盾、回血不会自行触发攻击动作；原有独立 VFX 保留。
推掌动画本身不额外绘制高亮毒云，以现有状态 VFX 区分毒掌与破甲掌。

`BattleManager` 在真实玩家攻击结算后记录独立视觉序号与 `BattleVfxCue` 快照；命中被闪避也记录出手。
UI 消费快照，不解析中文战报。敌人同帧出手和毒伤不能覆盖玩家快照；只在玩家再次出手时替换招式。
动画为一次播放，用未缩放时间；高攻速时按实际出手间隔缩短，最低 0.14 秒，不排队补播过时招式。
伤害仍在既有战斗时点结算，动作属于结算后的视觉呈现，本次没有改成帧事件伤害系统。

## 文件

修改：
- `Assets/Scripts/Battle/BattleManager.cs`：独立玩家出招快照与重置。
- `Assets/Scripts/UI/BattleScreenController.cs`：玩家独立播放进度、正确出手归属、脚底对齐。

新增：
- `Assets/Scripts/Visual/HeroAttackArt.cs`：资源缓存、动作选择和播放时长。
- `Assets/Scripts/UI/BattleScreenController.HeroAttacks.cs`：运行时自动替换、招式状态。
- `Assets/Editor/HeroAttackArtImporter.cs`：八帧 Sprite 切片导入。
- `Assets/Scripts/Debug/HeroAttackPlayModeProbe.cs`：可重复 Play Mode 验证与横竖屏截图。
- `Tools/ArtPipeline/prepare_hero_attacks.cjs`：可重复去底、源图分离、统一尺度与脚底归一化。
- `Assets/Resources/Characters/HeroAttacks/`：四张 2048×256 透明运行图集及 `.meta`。
- `ArtSource/HeroAttacks/`：用户参考图、三张生成原图、完整提示词、切片坐标、归一化记录。
- `docs/validation/hero_attacks/`：接触表、8／12 FPS 动图、引擎截图及报告。

## 美术流程

使用内置 `image_gen`，完整提示词见 `ArtSource/HeroAttacks/prompts.json`。
普通攻击直接提取用户原 JPG；曾生成的重新排版候选没有采用。生成图的棋盘格不是透明通道，
已通过图像生产脚本进行连通背景去除；真正带 Alpha 的剑气图保留原有 Alpha。
源图并非均匀网格，按审核后的重叠区域提取主要人物，避免长剑被等宽裁断。
重劈第六帧的剑尖与下一人物靴子贴连，在源坐标处做了分离，避免带入邻帧。

交付单帧 256×256、Point、160 PPU、Full Rect、无压缩、无 Mipmap；Pivot `(0.5, 0.125)`。
整条只使用一个缩放，不逐帧变焦；为长剑与举剑留白，标准站姿高约 135–136 px，
脚底为 y=223，八帧共同使用普通攻击的入势／收势静帧，避免技能切换时待机造型突变。
主世界和洞穴的八方向移动资源保持原有接入；本次替换战斗主角和战斗待机。

## 运行与验证

无需创建 GameObject、挂载额外组件或拖 Inspector 引用。现有 `BattleScreenController.Awake`
自动通过 Resources 加载；三个现有场景共用该组件。正常打开 `MainPrototype` 并 Play，
碰怪后观察；学习对应武学后在其真实触发时更换招式。

重新制作图集：设置 `NODE_PATH` 指向安装了 sharp 的 Node 模块目录，运行
`node Tools/ArtPipeline/prepare_hero_attacks.cjs`，然后执行 Unity 菜单
`37 MiniGame/Art/Reimport Hero Attack Pack`。

自动检查：在 `MainPrototype` 编辑态运行 `37 MiniGame/Validate Hero Attack Pack Play Mode`。
探针会临时切换 960×540／540×960 Game View，使用真实技能结算函数的可控夹具截图，
随后恢复真实战斗协程验证高攻速、普通／洞穴／Boss 计时；完成后退出 Play 并恢复 Game View 设置。
报告路径为 `docs/validation/hero_attacks/playmode_report.json`。该探针不代表自然路线平衡测试。

三条核心规则保持：普通战斗继续主时间，洞穴战斗暂停主时间，最终 Boss 独立计时。
最终状态与实测结果以本目录报告为准；手机触控、观感与用户最终美术确认独立于自动化检查。

## 本次验证结果（2026-09-08）

- 四组资源已完成 `Generated → Normalized → Imported → InEngineQA`；尚未标记 `Approved`。
- Unity 6000.5.4f1 编译通过，Play Mode 报告 `success: true`，运行错误列表为空。
- 已在 MainPrototype 的实际 960×540 和 540×960 Game View 检查四种动作，保存八张战斗截图。
- 实际技能结算、同帧敌人攻击、毒伤防误触、非循环结束、混合流派优先级、取消重置和高攻速验证通过。
- 真实协程的普通战斗继续主时间、洞穴暂停主时间、最终 Boss 独立计时均通过。
- 执行 `37 MiniGame/Validate Chinese Fonts`：898 个非 ASCII 字形检查通过。
- 未重新打 WebGL 包；现有 WebGL Builds 仍是先前版本。手机实机观感与最终美术确认待验收。

## 特效辨识度增强（同日追加）

新增 `BattleScreenController.HeroSkillVfx.cs`，修改 `BattleScreenController.HeroAttacks.cs`、
`BattleScreenController.cs` 与 `HeroAttackPlayModeProbe.cs`。复用已有掌法 icon、毒雾和命中图集，
增加实时绘制的斩波、扩散环及地面冲击，不新增光栅素材、不需要额外 Inspector 绑定。

- 剑气：纵向高弧飞行斩波、延后的尾迹、到达敌人后的斜切；连击增加交叉切线。
- 掌法：可辨识的实体掌印从主角飞向敌人，命中后扩散环与毒雾；破甲用金色扩散和碎片。
- 重劈：自上而下劈光、贴地椭圆冲击波和放射裂纹；惊鸿首击为金色，血域爆发为朱红色。
- 特效独立保存出招快照，敌方同帧伤害与毒发不覆盖特效。最多同时保存三个实例，
  单次生命周期 0.68 秒，上一道斩波可在下一次角色出手后继续飞至目标；取消战斗清空。
- 特效裁剪到交锋区域，绘制在伤害字、技能提示之前，避免遮挡血条、计时和战报。
- 普攻保持原有短剑光，形成常态与武学触发之间的差异；没有新增全屏闪光或停顿。

追加探针检查独立特效入队、同帧覆盖保护、毒伤防重复、到期清理与高攻速容量，
增加每套动作命中阶段截图 `*_impact_*.png`；原有三条时间规则检查继续执行。

增强版已通过 Unity 编译及 97 项 Play Mode 检查，运行时错误为零；横屏验证金色惊鸿首击，
竖屏验证朱红血域爆发。高攻速场景下剑气保持可见且实例数不超过三个。
横竖屏动作与命中阶段共 16 张截图已更新；当前状态 `InEngineQA`，未重新构建 WebGL。
