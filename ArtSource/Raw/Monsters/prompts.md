# Generated Monster Pack Prompts

Generation mode: built-in `imagegen`, using the existing wuxia swordsman strip as
the style-density reference. Raw images use a flat `#ff00ff` chroma background;
the checked-in Unity sheets are transparent.

## 墨鬃妖狼 / Ink Wolf

### Idle

Create a production Unity 2D pixel-art monster spritesheet for a lean,
right-facing dark-charcoal wolf demon with an ink-black mane, muted teal neck
talisman, amber eyes and bone-white fangs. Match the reference's pixel-cluster
density, outline weight, restrained wuxia palette and readable game scale. Make
exactly eight isolated idle frames: neutral, chest rise, mane lift, paw shift,
settle, tail down, tail back, neutral. Keep the same identity, proportions,
palette and common paw baseline. No scenery, shadows, text, UI or extra parts.

### Attack

Preserve the approved Ink Wolf identity exactly. Make exactly eight right-facing
attack frames: ready, crouch, coil, leap, bite contact, follow-through, recoil,
recover. Use strong anticipation-contact-recovery silhouettes while keeping the
complete creature inside every frame. No detached effects, scenery, shadows,
text, UI or extra limbs.

## 岩甲山魈 / Stone Ape

### Idle

Create a production Unity 2D pixel-art monster spritesheet for a broad,
right-facing mountain ape with dark-brown fur, slate stone shoulder and forearm
armor, a rust-red sash, pale jade eyes and large fists. Match the reference's
pixel density, outline and restrained wuxia palette. Make exactly eight idle
frames: guard, shoulders rise, inhale, fist settle, lower, armor relax, head
turn, guard. Keep one identity and common foot baseline.

### Attack

Preserve the approved Stone Ape identity exactly. Make exactly eight
right-facing ground-smash frames: guard, fists raise, overhead wind-up, descend,
impact, compressed follow-through, rebound, recover. Keep the complete monster
inside every frame. No debris, detached effects, scenery, shadows, text or UI.

## 青竹机关傀 / Bamboo Puppet

### Idle source grid

Preserve the approved right-facing lacquer-wood automaton with jade bamboo
armor, paper forehead talisman and compact short bamboo lance. Arrange exactly
eight idle frames in a strict 4-column by 2-row source grid, read left-to-right
on the top row then the bottom row: neutral, torso click, talisman lift, foot
shift, shoulders settle, lance dip, guard, neutral. Keep every complete figure
inside its own square cell with generous empty space and a common foot baseline.

### Attack source grid

Preserve the approved Bamboo Puppet identity exactly. Arrange exactly eight
attack frames in a strict 4-column by 2-row source grid: neutral, draw-back,
lower and aim, forward step, compact thrust contact, hold, retract, neutral.
Keep the full puppet and lance inside each cell. No grid lines, labels, scenery,
detached pieces, duplicated weapons, shadows, text or UI.

## Shared technical constraints

- Final frame: `256x256` RGBA.
- Final animation strip: `2048x256`, eight frames in one horizontal row.
- Facing: right.
- Ground line: `y=224`, leaving 32 transparent pixels below the feet.
- Pixel art: hard clusters, nearest-neighbor normalization, no antialiasing.
- Unity import: Sprite Multiple, Point filtering, 160 PPU, no mipmaps,
  uncompressed, custom pivot `(0.5, 0.125)`.
