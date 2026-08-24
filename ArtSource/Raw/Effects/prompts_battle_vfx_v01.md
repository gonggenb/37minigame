# 战斗特效 v01 生成记录

## 统一目标

- 用途：Unity 自动战斗画面中的瞬时技能反馈。
- 交付：单帧 `256 × 256`、横向 6 帧、最终图集 `1536 × 256`、透明背景。
- 播放：12 FPS、非循环；持续状态由角色染色和低频循环叠层表达。
- 风格：像素化武侠墨气、剪影优先、克制发光；不得盖住角色与伤害数字。

## 剑气母版

```text
One centered crescent-shaped sword qi slash with a sharp diagonal leading edge and broken ink-brush particles.
Pale jade-white core, cool cyan edge, tiny muted gold sparks. No character, weapon, scenery, text or watermark.
Isolated effect intended for a Unity battle VFX sprite.
```

生成方式：Codex 内置 `imagegen`。生成原图保存在同目录的 `src_vfx_sword_qi_v01.png`，再由
`Tools/ArtPipeline/normalize_battle_vfx.py` 执行黑底转透明、像素化和 6 帧归一化。

## 毒雾母版

```text
One compact circular poison aura made of curling toxic mist, three poison droplets and a broken ink-brush ring.
Dark violet outer mist, jade-green toxic core. No character, skull, bottle, scenery, text or watermark.
Isolated on black for alpha extraction and intended for a Unity battle VFX sprite.
```

生成方式：Codex 内置 `imagegen`。生成原图保存在同目录的 `src_vfx_poison_mist_v01.png`，使用同一归一化工具输出。
