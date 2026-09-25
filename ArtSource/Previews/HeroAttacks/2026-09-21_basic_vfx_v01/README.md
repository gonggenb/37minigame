# 主角普通攻击：自带特效候选

后续：用户已选定三版全部用于随机普攻，已另行归一化并接入 Unity。
本目录继续保留生成原图；最新资源、运行与验证状态见
[随机普攻接入记录](../../../../docs/validation/hero_basic_variants/README.md)。

使用内置 image_gen，以现有普攻八帧图为角色参考，每版一次生成完整八帧横条。

| 版本 | 视觉方向 | 原图 |
| --- | --- | --- |
| 青白剑罡 | jade_crescent | [PNG](spr_hero_attack_basic_jade_crescent_right_8f_concept_v01.png) |
| 赤金烈斩 | golden_ember | [PNG](spr_hero_attack_basic_golden_ember_right_8f_concept_v01.png) |
| 墨影残像 | ink_afterimage | [PNG](spr_hero_attack_basic_ink_afterimage_right_8f_concept_v01.png) |

状态：Generated。三版原图均为 2172 × 724；可见八个连续动作，特效与角色同图。尚未达到 2048 × 256 的正式图集规格，不能直接按 256 像素切片使用。特效在峰值帧接近邻帧边界，选定后需修整边界、统一比例与脚点，并检查像素密度及透明边缘。

完整提示词和生成源路径见 prompts.json。本批只新增候选图与记录，没有修改已有脚本、场景或正式动画。无需 Unity 手动绑定；直接打开 PNG 比较动作和特效。尚未 Normalized、Imported、InEngineQA 或 Approved，也未进行 Play Mode 测试。后续需为选定版制作可播放预览并完成 Unity 横竖屏验收。

普通战斗继续主计时、洞穴暂停主计时、最终 Boss 独立计时：本批不修改任何计时逻辑。
