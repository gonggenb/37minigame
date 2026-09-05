# 竖屏 UI v02 生产素材

2026-09-05；生成工具：内置 image_gen。四张无字 RGBA 组件，不使用整屏概念图切片。

| 文件 | 已选生成记录 | 用途 |
| --- | --- | --- |
| panel.png | exec-af812979-5791-439c-afab-56880d95425f | 暗木/黑铁面板与槽位 |
| paper.png | exec-63883a1a-45de-42c3-a9b2-f7ed941c2092 | 旧纸内衬 |
| timer.png | exec-b400b2f8-001c-491a-88c3-a1619c556ddb | 无字圆形计时盘 |
| button.png | exec-950277d0-c868-4bf6-aeac-b0312b64c5d7 | 无字横向按钮 |

初始按钮候选因外发光或假棋盘透明被淘汰，未放入 Assets。最终按钮使用真正 Alpha；归一化时以 alpha-threshold 220 去掉外缘薄雾，其余资源阈值 12。全部 padding-ratio 0。

使用 Tools/ArtPipeline/normalize_ui_nineslice.py：面板 128×128、槽位 64×64、按钮 128×48、时钟 256×256。normal/hover/pressed/selected/primary/primary_hover 通过脚本色调派生，不重复生成形状。结果保存在 ArtSource/Normalized/UI/PortraitRefresh_20260905，并导入 Assets/Resources/UI/Theme/*_v02.png；保留全部 v01 以便回退。

导入九宫格边界：面板 14、槽位 8、按钮左右 26/上下 14；时钟不切边。中文和实时数字均由运行时代码绘制。

## 初始生产提示词

### panel

Production UI asset for a dark eastern martial arts fantasy roguelite, HD-2D inspired visual language without copying any existing game asset, aged parchment, dark wood, black iron, antique brass, subtle gold ornament, restrained ink and jade accents, low saturation, warm lighting, high value contrast, handcrafted adventure interface, readable silhouette.
Use case: production Unity UI component, matching the approved portrait UI screen concept.
A SINGLE SQUARE UI panel 1024x1024 on genuine transparent background. Panel occupies 94% canvas centered. Opaque nearly black charcoal interior #141716 with extremely subtle fine dark wood grain, black iron outer structural border width only 3% of panel. Restrained fine double antique brass line 1% of width inside. Tiny square engraved Chinese geometric corners ONLY in 12% corner safe zones. The central 76% and all straight edges must be quiet stretchable nine-slice regions. Flat front-facing orthographic; no perspective. No large crest, nothing on top or bottom protrudes. No drop shadow, external glow or cast shadow. For use resized to 128x128 nine-slice then stretched to many sizes. One panel only, NO TEXT, numbers, icons, logos, watermark, scenery or characters. Small squared chamfer corners, not rounded.
No modern app rounded cards, neon, magic circles, western fantasy scrollwork, watermark. TRANSPARENT exterior, opaque interior.

### paper

Production UI asset for a dark eastern martial arts fantasy roguelite, HD-2D inspired visual language without copying any existing game asset, aged parchment, dark wood, black iron, antique brass, subtle gold ornament, restrained ink and jade accents, low saturation, warm lighting, high value contrast, handcrafted adventure interface, readable silhouette.
Use case: production Unity UI component, matching the approved portrait UI screen concept.
A SINGLE SQUARE UI parchment inset panel 1024x1024 on genuine transparent background. Panel fills central 94% canvas, symmetric. Opaque warm old paper #D8CBA5 inside, subtle faint fibers and gentle weathering along edge only, entire center empty and calm, no stains or drawings. Very slim dark wood border with one fine muted brass inner line, squared edges, tiny folded paper corner accents within 12% safe corners. The whole central 76% and straight sides must stretch cleanly as nine-slice. For resized 128x128 Unity panel. NO TEXT, symbols, logos, watermark, icons, scenes, shadows, perspective or objects. No scroll rods. One single empty panel only.
No modern app rounded cards, neon, magic circles, western fantasy scrollwork, watermark. TRANSPARENT exterior, opaque interior.

### button

Production UI asset for a dark eastern martial arts fantasy roguelite, HD-2D inspired visual language without copying any existing game asset, aged parchment, dark wood, black iron, antique brass, subtle gold ornament, restrained ink and jade accents, low saturation, warm lighting, high value contrast, handcrafted adventure interface, readable silhouette.
Use case: production Unity UI component, matching the approved portrait UI screen concept.
A SINGLE horizontal button plate 1536x576, ratio exactly 8:3, centered with 3% genuine transparent padding. Front orthographic symmetric UI asset. Dark brown-black iron opaque fill #292724 with low-contrast fine handworked material. Thin antique brass outer line, tiny angular short chamfer corners and small geometric engraved end pieces, corner detail entirely within outer 15% width and 25% height. Long central 70% perfectly quiet horizontal stretch zone, no medallion. Restrained muted aged brass, not bright gold. Designed to resize to 128x48 and nine-slice stretch into primary secondary selected pressed buttons through runtime tint. One single button, NO TEXT or symbols or labels, no logos, no shadows, no white background, no scenery, no character, no glow.
No modern app rounded cards, neon, magic circles, western fantasy scrollwork, watermark. TRANSPARENT exterior, opaque interior.

### timer

Production UI asset for a dark eastern martial arts fantasy roguelite, HD-2D inspired visual language without copying any existing game asset, aged parchment, dark wood, black iron, antique brass, subtle gold ornament, restrained ink and jade accents, low saturation, warm lighting, high value contrast, handcrafted adventure interface, readable silhouette.
Use case: production Unity UI component, matching the approved portrait UI screen concept.
A SINGLE circular antique brass sundial face for a mobile wuxia HUD. Exact square 1024x1024 transparent canvas, clock diameter 90% of canvas centered. Genuine transparent pixels OUTSIDE the circular disk. Inside solid nearly black #121615 matte metal face, completely blank central 65% diameter for runtime number. Slim antique brass concentric outer rims, tiny evenly spaced neutral engraved ticks, quiet dark annulus from 73% to 82% diameter where runtime radial progress will be drawn. No hands, pointers, numbers, lettering, runes, colored progress, sectors, central ornament, hang loop, knob, shadow, glow or scenic background. Front orthographic symmetrical flat disk, low saturation handcrafted edge polish, legible at 80x80 logical pixels. Plain restrained circular UI frame not ornate pocket watch, NOT a full screen or poster.
No modern app rounded cards, neon, magic circles, western fantasy scrollwork, watermark. TRANSPARENT exterior, opaque interior.

