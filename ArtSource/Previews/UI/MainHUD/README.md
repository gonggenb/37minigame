# Main HUD Visual Mockups

## Portrait auto-battle v03

- Preview: `hud_portrait_autobattle_mockup_v03.png`
- Generated: 2026-09-05
- Canvas: `941 × 1672` portrait (`9:16` target composition)
- Mode: built-in `imagegen`, targeted edit of portrait v02
- Status: `Generated`
- Runtime role: current portrait exploration HUD direction; visual reference only, not imported into `Assets`
- Auto-battle correction: no active-skill tray, no attack/ultimate buttons, and no tappable cooldown controls; exploration keeps only a collapsed `武学·自动运转` build summary and the contextual world-interaction button
- Future battle-state rule: automatic martial arts may expose read-only trigger/cooldown feedback, but never use button affordances or reserve touch input regions

## Portrait-first v02

- Preview: `hud_portrait_visual_mockup_v02.png`
- Generated: 2026-09-05
- Canvas: `941 × 1672` portrait (`9:16` target composition)
- Mode: built-in `imagegen`, two-pass edit from the current `540 × 960` portrait Play Mode screenshot
- Status: `Superseded` by portrait auto-battle v03
- Runtime role: visual direction reference only; not imported into `Assets`, not sliced, and not connected to a Canvas or Prefab
- Portrait decisions: independent top-center timer, compact player strip, two-button upper-right cluster, capped three-slot status rail, collapsed build tab, no persistent joystick, and a shallow bottom skill tray above the gesture safe area

## Landscape v01

- Preview: `hud_mainmap_visual_mockup_v01.png`
- Generated: 2026-08-13
- Mode: built-in `imagegen`, edit from the current main-map Play Mode screenshot
- Status: `Generated`
- Runtime role: visual direction reference only; the screenshot is not imported as a gameplay texture
- Integration: semantic colors, material panels, buttons, slots and the timer motif are implemented through `WuxiaUiTheme` and `PrototypeHUDController`

## Prompt

```text
Use case: ui-mockup
Asset type: high-fidelity Unity game HUD redesign mockup for the existing 960x540 main-map screenshot
Input image 1: edit target; preserve the exact gameplay scene, camera, player, enemies, props, lighting, and composition. Change only the HUD and UI overlays.
Primary request: redesign the main-map HUD for a one-minute eastern wuxia roguelite using the project's approved visual system.
Visual direction: production UI asset for a dark eastern martial arts fantasy roguelite, HD-2D inspired visual language without copying any existing game asset, aged parchment, dark wood, black iron, antique brass, subtle gold ornament, restrained ink and jade accents, low saturation, warm lighting, high value contrast, handcrafted adventure interface, readable silhouette.
Layout: compact top-left player status cluster with circular portrait in an antique brass and black-iron frame, warm-paper player name, level seal, segmented dark-crimson health bar, a slim cultivation and copper row, and one compact horizontal row of martial-art and buff slots beneath it. Keep the center of the gameplay view unobstructed. Replace the tiny timer chip with a distinct compact antique-brass sundial/pocket-watch timer centered at the top, showing a clear 60-second value and a short three-stage jade/brass/crimson progress track. Right edge has three compact square black-iron icon buttons with restrained brass outlines. Bottom status message becomes a narrow dark parchment-and-brass strip. Keep the joystick functional-looking but smaller, darker, and integrated with the same brass/jade material language.
Text hierarchy: use warm off-white Chinese-style interface lettering only where needed; preserve the meanings of player name, level, health, cultivation, copper, martial arts, and 60 seconds. Do not add marketing copy.
Materials: matte dark wood, worn black iron, antique brass edge highlights, subtle rice-paper texture, one simplified cloud or sword motif only; no glossy gold slabs.
Constraints: practical shippable game UI mockup, crisp at gameplay scale, low screen coverage, strong value hierarchy, square or lightly clipped corners, no large banner, no new gameplay controls, no changes to the world scene, no copied motifs or layouts from any commercial game.
Avoid: modern mobile app UI, neon sci-fi, glassmorphism, oversized rounded cards, saturated gradients, flat web dashboard, default Unity UI, excessive particles, clutter, logo, watermark.
```
