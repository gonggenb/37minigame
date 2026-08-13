# UI Theme Nine-Slice v01

## Production status

| Asset | Source | Normalized | Imported | InEngineQA | Approved |
| --- | --- | --- | --- | --- | --- |
| `UI_Panel_Default` | Yes | Yes | Yes | Yes | No |
| `UI_Panel_Paper` | Yes | Yes | Yes | Yes | No |
| `UI_Panel_Boss` | Yes | Yes | Yes | Yes | No |
| `UI_Button` states | Yes | Yes | Yes | Yes | No |

The source images were generated with the built-in `imagegen` mode. Magenta-key
sources were converted to alpha with the installed `remove_chroma_key.py` helper,
then normalized with `Tools/ArtPipeline/normalize_ui_nineslice.py`.

## Shared prompt direction

```text
Production UI asset for a dark eastern martial arts fantasy roguelite,
HD-2D inspired visual language without copying any existing game asset,
aged parchment, dark wood, black iron, antique brass, subtle gold ornament,
restrained ink and jade accents, low saturation, warm lighting,
high value contrast, handcrafted adventure interface, readable silhouette.

Front-facing orthographic asset, symmetrical geometry. Keep ornaments inside
fixed corner safe zones. Leave the center and middle portions of all edges quiet,
straight, and tile-safe for Unity nine-slice stretching. Flat #ff00ff chroma-key
outside and in every empty area. No text, icon, character, scenery, shadow,
watermark, copyrighted motif, modern app UI, neon, glassmorphism, rounded card,
saturated gradient, or direct imitation of an existing UI.
```

Object-specific additions:

- `Panel_Default`: dark wood outer frame, black iron inner edge, antique brass
  corner fittings, restrained cloud engraving.
- `Panel_Paper`: narrow aged parchment lip held by dark wood and brass.
- `Panel_Boss`: thicker black iron and charcoal stone with blade-shaped brass
  corners and very small dark-crimson seal accents.
- `Button`: 3:1 black-iron button with chamfered ends, dark wood inset and brass
  edge; one empty button only.

## Output contract

- Panel delivery: `128 x 128` RGBA PNG.
- Slot delivery: `64 x 64` RGBA PNG derived from the default frame.
- Button delivery: `128 x 48` RGBA PNG; normal, hover, pressed, selected,
  primary and primary-hover states share one geometry.
- Unity path: `Assets/Resources/UI/Theme/`.
- Runtime border values are mirrored by `WuxiaUiThemeAssetImporter` and
  `WuxiaUiTheme`.
- `InEngineQA` covers Editor Play Mode and responsive WebGL smoke tests at
  `540 x 960` and `960 x 540`. Physical-device safe-area approval is still
  pending, so these assets remain unapproved.
