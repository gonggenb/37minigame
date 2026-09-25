# 武学结算特效动画

四套六帧，来自 `ArtSource/VfxExpansionRound1` 已选 A1 / B1 / C1 / D1。

| 图集 ID | 主绑定 | 共享反馈 |
| --- | --- | --- |
| armor_break | 破甲掌 | 玄铁戒、腐骨手套的命中破甲 |
| swift_combo | 无影连环剑 | 一至三重分别每 5 / 4 / 3 次成功命中触发 |
| retaliation | 反震诀 | 受到伤害或护盾吸收伤害后反击；闪避不触发 |
| life_drain | 饮血刀法 | 吸星诀、以毒养血以及其他实际吸血；满血不触发 |

四张母版由内置 `image_gen` 分别参照选定方向生成，完整提示词、源文件路径、SHA-256 在 `prompts.json`。
每张为 1536×1024 RGBA、3×2 排列。没有逐帧独立生成，没有程序重画美术。

`Tools/ArtPipeline/prepare_martial_proc_vfx.cjs` 使用 sharp 做等格裁切、统一缩放及图集排版，保持 alpha。
每格统一缩至 224×224 后放入 256×256 画布中心；不对各帧分别裁到内容边缘，以免锚点跳动。
输出到 `Assets/Resources/Effects/MartialProcs/`，预览和检查记录在 `docs/validation/martial_proc_vfx/`。

Unity 设置：6 帧横排、1536×256、中心 pivot、PPU 256、Point、Clamp、无 mipmap、无压缩。
菜单 `37 MiniGame/Art/Reimport Martial Proc VFX` 负责切片，保持已有 Sprite ID；本轮已执行。

Generated、Normalized、Imported 和 Editor Play Mode 检查已执行。正式设备性能、触摸体验及用户最终美术批准单独验收。
见 [接入与验证说明](../../docs/validation/martial_proc_vfx/README.md)。
