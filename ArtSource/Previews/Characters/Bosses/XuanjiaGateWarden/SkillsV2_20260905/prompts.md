# 玄甲镇关使技能预览 · 2026-09-05

生成方式：内置 image_gen；参考为当前项目已使用的左朝向角色种子帧与震岳斩条带。仅供视觉审阅，未写入 Unity 运行时。

## double

Use case: identity-preserve. Asset type: eight frame pixel art character animation sprite strip for an existing Unity wuxia game, visual preview.
Input image 1 is the EXISTING character identity seed; input image 2 is existing animation ONLY for palette, scale, direction reference.
Animate THIS EXACT black/navy lamellar armored Chinese gate warden with red scarf and red head ribbons, one single long shaft guandao with silver curved blade and gold details. Face LEFT throughout. Match seed face, hair, armor, proportions, weapon length, crispy pixel clusters, dark outlines. Do not redesign or mirror.
Produce exactly ONE horizontal row of 8 equally spaced square animation cells, target canvas 2048x256, each cell256x256. Truly transparent RGBA background. Shared feet ground at y224, stable body scale, torso anchor x128, no character cropping across cells. Character occupying about150px standing height matching seed. Whole weapon stays within each cell.
New skill CROSSING DOUBLE CLEAVE / 横刀连破: 1 standing ready with blade pointing low left; 2 grounded knees bent torso coils with guandao drawn diagonally back; 3 FIRST distinct horizontal sweeping cut to LEFT chest height, both hands on shaft; 4 follow-through blade low left and shoulders turning; 5 wind up reverse sweep with blade across upper torso; 6 SECOND distinct rising diagonal cut toward LEFT, red scarf trails right; 7 decelerate hands pull shaft to center and knees straighten; 8 return exactly ready seed stance. Two distinct impacts at frames3 and6. Use natural poses; feet remain stable except intentional small step. Clean character-only sprites, NO energy slash trails, no glow, no VFX, no shadows, no scenery, no text, no cell lines, no panels, no logos. All eight frames in one generation; NOT a contact sheet grid.

## guard

Use case: identity-preserve. Asset type: eight frame pixel art character animation sprite strip for an existing Unity wuxia game, visual preview.
Input image 1 locks existing character identity. Input image 2 locks art style and scale. Animate exactly this black/navy lamellar armored Chinese gate warden, red scarf and head ribbons, one long guandao silver curved blade with golden fittings. LEFT facing throughout, unchanged face, outfit, weapon design and length, proportions, crisp pixel art, same pixel cluster density.
Exactly ONE HORIZONTAL ROW of 8 equally spaced square cells, target2048x256, each256x256, truly TRANSPARENT background. Shared feet ground y224, torso anchorx128, matching seed standing body height150px, same scale entire strip, no inter-cell clipping.
New skill IRON GUARD / 玄甲固守, defensive deliberate martial stance: frame1 existing ready seed pose; frame2 bend knees and pull long weapon closer with both hands; frame3 raise shaft horizontally in front of chest with blade still projecting LEFT; frame4 firm low stance bracing weapon as a bar, shoulders squared toward LEFT; frame5 held brace, slightly compressed knees and scarf lifted by inner energy; frame6 held brace relaxed slightly, perfectly readable silhouette; frame7 lower weapon diagonally; frame8 return exact seed ready pose. No new shield equipment, no transformation. Character ONLY, NO magical armor ring, VFX, glow, shadow, terrain, text, borders, divisions, labels. Keep entire weapon and character inside every square. Eight frames one request, not a multi-row grid.

## slashes

Use case: stylized-concept. Asset type: 6 frame VFX sprite animation strip for existing pixel wuxia combat. Exactly one horizontal row, 6 equally sized square cells. Target1536x256, six256x256 frames, shared effect center128,128, true transparent RGBA background.
Effect: two successive intersecting guandao slash energy crescents traveling from RIGHT towards LEFT, for 横刀连破. Restrained antique GOLD edges, ivory hot small core, rust red fine streaks, distinct hard crisp pixel clusters matching HD2D wuxia sprites. NOT photorealistic, no blur bloom.
Frame1 thin flattened crescent starts at right midheight and curls toward left; frame2 FIRST powerful broad mostly-horizontal blade arc curling left with bright small impact star at left center; frame3 first crescent fragments and fades, small gold motes, empty central region; frame4 SECOND rising diagonal slash starts bottom right sweeping toward upper left; frame5 peak second diagonal slash crossed with only faint residual first trail, left tip white gold sparks; frame6 all dissipate into sparse thin gold pixel fragments. Each effect isolated with32px safe margins, no crossing cell bounds, do not add a character, sword, scenery, rocks, labels, border or fake checkerboard. Every frame one coherent isolated energy swoosh. Two clear visual hit beats at frames2 and5. One strip, not a multirow grid.

## ward

Use case: stylized-concept. Asset type: 6 frame VFX sprite strip for existing HD2D pixel wuxia game, preview of temporary breakable armor.
Exactly ONE horizontal row of6 equal square cells, target1536x256, each256x256. Real transparent background, no fake checkerboard, no scenery, no labels, no character.
Effect for 玄甲固守: sparse antique gold SEGMENTED vertical shield silhouette made of six angular lamellar plates around an empty dark-free transparent center. Fits armored warrior but do NOT draw warrior. Restrained geometric armor motif, NOT bubble, no rune text, no electric neon, no explosion. Keep center transparent for body readability. Crisp pixel clusters, ivory small glints, dark bronze edges; golden lower ellipse anchors bottom of ward, effect centered128,128 with32px outer safe margin.
Frame1 two small amber arcs form near bottom; frame2 angular plate fragments rise on both sides; frame3 six gold lamellar shield plates lock in around the empty center, tall roughly hexagonal outline with beveled corners; frame4 stable dim protective outline with one soft pale-gold traveling glint; frame5 two narrow cracks appear in side plates and upper segments drift outward; frame6 segments dissolve into tiny gold motes and vanish. Sequence assemble->stable->break, not six identical shields. Avoid opaque fill or large bloom. All frames produced together as one horizontal strip.

## double_alpha

Use case: background-extraction. Edit the supplied existing eight-frame horizontal character animation. Remove ONLY the entire white and light grey CHECKERBOARD backdrop and replace it with REAL transparent alpha channel, not a painted checkerboard. Preserve all 8 existing characters, pixels, colors, exact frame order, positions, silhouette, facing left, poses, weapon and scarf. DO NOT redesign, add frames, reposition, crop or change composition. Return RGBA PNG with genuinely zero-alpha empty regions. It is a game sprite sheet requiring actual transparency.

## guard_alpha

Use case: background-extraction. Remove ONLY white and light grey checkerboard backdrop from this existing eight-frame horizontal character animation. Replace with REAL zero-alpha transparent pixels, not a painted checkerboard. Preserve all eight poses, pixel details, leftward direction, ONE single guandao per character, one silver curved blade at LEFT end and plain wooden shaft butt at RIGHT end, armor, red scarf, positions, order and scale. Do not add any new artwork. Return PNG with real alpha.

## double_fixed

Use case: precise-object-edit. This eight-frame LEFT facing guandao combat sprite strip has one error in the SIXTH character from left: weapon has two silver blades and the raised blade is behind him. Correct ONLY this sixth pose: the single guandao shaft should rise diagonally from lower RIGHT near hips toward upper LEFT, with its ONE curved silver blade at the upper LEFT end, thrusting/slashing toward the left-hand opponent. Both hands hold the single shaft, plain wooden butt at lower right. Keep red scarf trailing right, feet exactly in place at same baseline, face LEFT, matching all other frames armor/character scale. Preserve all other seven character poses and their pixel style unchanged. Truly transparent alpha backdrop, no fake checkerboard, no labels, no extra weapons, one horizontal strip of exactly eight separate characters in same positions. Only fix sixth pose anatomy and weapon, preserve canvas and other characters.


