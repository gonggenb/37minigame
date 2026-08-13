# HD-2D Main World v01 Prompt Set

Generation mode: built-in `imagegen`, `Generate`.

Direction after review: original wuxia HD-2D scene using 2D pixel-art backplates and cutouts inside a lit 3D diorama. The generated images provide source layers; Unity supplies depth, fog, lights, water surfaces and spatial composition.

## Mountain-and-river backdrop

```text
Use case: stylized-concept
Asset type: production landscape backdrop for a Unity 2.5D wuxia roguelite main world
Primary request: create an original wide Chinese mountain-and-river travel panorama that gives a poetic ink-wash pixel-art atmosphere
Scene/backdrop: layered misty karst mountains, a distant winding river, small ancient roofs and a pagoda silhouette far away, sparse flying birds, pale open sky
Style/medium: traditional Chinese handscroll composition translated into refined painterly pixel art, visible but restrained pixel clusters, matte watercolor and ink wash, original design
Composition/framing: 16:9 landscape, distant horizon in the upper half, strongest mountain silhouettes near left and right thirds, calm central negative space, designed to sit behind a top-down gameplay map
Lighting/mood: cool misty morning with soft warm light touching a few distant roofs, tranquil and mysterious
Color palette: desaturated celadon, blue-grey ink, warm parchment, muted moss green, tiny ochre accents
Constraints: no foreground characters, no readable text, no UI, no logos, no watermark, no frame, no border; background scenery only; cohesive low contrast so gameplay objects remain readable
Avoid: photorealism, glossy 3D, anime characters, modern buildings, detailed close foreground, heavy black outlines, neon colors, copied game screenshots
```

## Bamboo-and-rock cutout

```text
Use case: stylized-concept
Asset type: transparent environment cutout sprite for a Unity 2.5D wuxia roguelite
Primary request: one compact original bamboo grove cluster with weathered rocks and a few low grass tufts, suitable as a repeated roadside scenic prop
Style/medium: Chinese ink-wash pixel art, hand-painted brush texture translated into crisp restrained pixel clusters, matching a muted mountain-river travel world
Composition/framing: isolated full cluster, three height layers, strong readable silhouette, front three-quarter view, generous padding
Lighting/mood: soft neutral overcast lighting, no baked directional shadow
Color palette: desaturated jade green, grey-green, warm stone grey, tiny ochre dry-leaf accents
Constraints: perfectly flat solid #ff00ff chroma-key background for removal; background uniform with no texture or gradient; no #ff00ff in subject; no cast shadow, no contact shadow, no text, no watermark
Avoid: photorealism, glossy 3D, neon green, many tiny leaves, characters, buildings, pots, border
```

## Pine-and-scholar-rock cutout

```text
Use case: stylized-concept
Asset type: transparent environment cutout sprite for a Unity 2.5D wuxia roguelite
Primary request: one original windswept old pine growing beside scholar rocks, suitable as a mountain-pass landmark
Style/medium: Chinese ink-wash pixel art, hand-painted brush texture translated into crisp restrained pixel clusters, poetic handscroll silhouette
Composition/framing: isolated asymmetrical pine-and-rock cluster, broad horizontal crown, strong readable silhouette, front three-quarter view, generous padding
Lighting/mood: soft neutral misty lighting, no baked directional shadow
Color palette: charcoal blue-grey rock, muted pine green, warm dark brown trunk, subtle parchment highlights
Constraints: perfectly flat solid #ff00ff chroma-key background for removal; background uniform with no texture or gradient; no #ff00ff in subject; no cast shadow, no contact shadow, no text, no watermark
Avoid: photorealism, glossy 3D, bonsai pot, neon colors, characters, buildings, border
```

## Stream-water texture

```text
Use case: stylized-concept
Asset type: production seamless albedo texture for a shallow stream in a Unity 2.5D wuxia roguelite main world
Primary request: create one square perfectly tileable muted shallow river-water texture viewed straight down
Style/medium: Chinese ink-wash painterly pixel texture, restrained pixel clusters and soft brush bands, original design
Composition/framing: uniform full-frame texture with no focal point, no banks, no horizon, no perspective; opposite edges connect seamlessly in both axes
Lighting/mood: neutral albedo only, soft overcast ambience, no directional lighting or baked shadows
Color palette: desaturated celadon blue, pale blue-grey, small dark ink-grey ripples, very subtle warm silt undertone
Materials/textures: slow flowing shallow water, sparse horizontal ripple strokes, broad calm tonal masses, quiet at gameplay distance
Constraints: exactly one square seamless texture, low contrast, no alpha, no text, no watermark
Avoid: photorealism, foam, waves, fish, lotus, rocks, reflections of objects, bright cyan, high-frequency noise, border, gradient, vignette, specular shine
```
