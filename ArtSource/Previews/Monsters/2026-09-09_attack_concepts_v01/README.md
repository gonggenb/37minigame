# 小怪攻击帧造型预览 v01

2026-09-09 使用内置 image_gen 生成；完整提示词和原始文件路径见 manifest.json。

共六种小怪，每张展示八个攻击姿势，按从左到右观看蓄力、出招、命中和收势。

| 小怪 | 攻击表现 | 文件 |
| --- | --- | --- |
| 铁獠山猪 | 低身冲撞、獠牙顶击 | [PNG](spr_iron_tusk_boar_attack_right_8f_preview_v01.png) |
| 赤练蛇妖 | 盘身蓄力、探身毒咬 | [PNG](spr_scarlet_viper_attack_right_8f_preview_v01.png) |
| 斗笠刀匪 | 举刀蓄力、挥刀斩击 | [PNG](spr_strawhat_bandit_attack_right_8f_preview_v01.png) |
| 酒葫芦恶僧 | 沉身蓄力、葫芦重击 | [PNG](spr_gourd_rogue_monk_attack_right_8f_preview_v01.png) |
| 灯笼怨灵 | 亮灯蓄力、吐出灵火 | [PNG](spr_lantern_wraith_attack_right_8f_preview_v01.png) |
| 菌甲小妖 | 转身蓄力、根臂出拳 | [PNG](spr_moss_mushroom_imp_attack_right_8f_preview_v01.png) |

状态：Generated（造型与动作候选）。尚未 SeedApproved、Normalized、Imported、InEngineQA 或 Approved。

这批遵照用户“先生成给我看看”的要求直接生成动作概念预览，不作为已批准种子帧的正式动画生产。未修改游戏脚本、场景、Prefab 或 Assets；无 Unity 手动绑定要求，三条核心时间规则未改动。

检查：六张均目视有八个姿势。生成原图未达到 2048×256 的交付规格，部分武器和身体接近或跨过等宽分格边界，尚不能直接等宽切片播放。像素密度、脚底锚点与跨帧一致性仍需正式化处理；未做动画播放、Play Mode 或设备验收。

观看方式：直接打开 PNG，沿横排比较动作。后续选定造型，再锁定种子、修整分格、统一缩放和脚点，并补齐待机等动作。
