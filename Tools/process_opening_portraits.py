#!/usr/bin/env python3
"""Remove the baked neutral checker from approved dialogue art, retaining RGB masters.

Dependencies: pillow, numpy, scipy. User authorized local Python matting.
The mask is derived from neutral bright exterior regions, not a global white key;
enclosed cloth highlights remain opaque. Edge colors are unmatted against nearby
checker pixels to prevent white fringes on the dark in-game backdrop.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / "ArtSource/Raw/OpeningDialogue"
NORMALIZED = ROOT / "ArtSource/Normalized/OpeningDialogue"
RUNTIME = ROOT / "Assets/Resources/OpeningDialogue"
PREVIEW = ROOT / "ArtSource/Previews/UI/OpeningDialogue"


def extract(image, character):
    rgb = np.asarray(image.convert("RGB"), dtype=np.float32)
    low = rgb.min(axis=2)
    chroma = rgb.max(axis=2) - low
    candidate = (low >= 210) & (chroma <= 22)
    if character == "hero":
        yy, xx = np.indices(low.shape)
        hair_air = ((yy < 490) & (xx < 485)) | ((yy < 680) & (xx < 270))
        candidate |= hair_air & (low > 165) & (chroma < 26)
    labels, _ = ndi.label(candidate)
    exterior_ids = np.unique(np.concatenate((labels[0], labels[-1], labels[:, 0], labels[:, -1])))
    exterior_ids = exterior_ids[exterior_ids != 0]
    background = np.isin(labels, exterior_ids)
    # Locked-master air regions: preserve enclosed highlights inside white cloth,
    # but clear the checker caught between hair strands, arm/body, and fox tails.
    for obj_id, sl in enumerate(ndi.find_objects(labels), 1):
        if sl is None or np.any(exterior_ids == obj_id):
            continue
        y, x = sl
        if character == "hero":
            air_region = (y.stop < 490 or (x.stop < 270 and y.stop < 730) or
                          (x.start > 680 and x.stop < 835 and y.start > 800) or
                          (340 < x.start < 400 and 950 < y.start < 1020))
        else:
            air_region = ((y.stop < 470 and (x.stop < 370 or x.start > 680)) or
                          (x.stop < 200 and y.start > 1150))
        component = labels[sl] == obj_id
        minimum_area = 700 if character == "hero" and y.start > 800 and x.start > 680 else 8
        if air_region and component.sum() >= minimum_area:
            background[sl] |= component

    if character == "hero":
        background |= candidate & hair_air
    foreground = ~background
    fg_labels, count = ndi.label(foreground)
    areas = np.bincount(fg_labels.ravel())
    # Discard only isolated sub-pixel debris, retaining distinct fine hair strands.
    background |= (areas[fg_labels] < 12)
    foreground = ~background
    interior = ndi.binary_erosion(foreground, iterations=2, border_value=1)
    edge = foreground & ~interior
    _, nearest_bg = ndi.distance_transform_edt(foreground, return_indices=True)
    _, nearest_fg = ndi.distance_transform_edt(~interior, return_indices=True)
    bg_rgb = rgb[tuple(nearest_bg)]
    fg_rgb = rgb[tuple(nearest_fg)]
    direction = fg_rgb - bg_rgb
    alpha = np.ones(low.shape, dtype=np.float32)
    alpha[background] = 0
    estimate = np.sum((rgb - bg_rgb) * direction, axis=2) / np.maximum(np.sum(direction ** 2, axis=2), 1)
    alpha[edge] = np.clip(estimate[edge], 0, 1)
    alpha[alpha < 0.05] = 0
    corrected = rgb.copy()
    edge_visible = edge & (alpha > 0)
    corrected[edge_visible] = np.clip(
        (rgb[edge_visible] - (1 - alpha[edge_visible, None]) * bg_rgb[edge_visible]) /
        alpha[edge_visible, None], 0, 255)
    corrected[alpha == 0] = 0
    rgba = np.dstack((corrected, alpha * 255)).round().astype(np.uint8)
    return Image.fromarray(rgba)


def main():
    for folder in (NORMALIZED, RUNTIME, PREVIEW):
        folder.mkdir(parents=True, exist_ok=True)
    report = []
    sheet = Image.new("RGB", (1536, 1176), (25, 28, 29))
    draw = ImageDraw.Draw(sheet)
    for i, name in enumerate(("hero", "fox")):
        raw = Image.open(RAW / f"{name}_rgb_v01.png")
        result = extract(raw, name)
        alpha = np.asarray(result.getchannel("A"))
        transparent = float(np.mean(alpha == 0))
        opaque = float(np.mean(alpha == 255))
        assert transparent > 0.12 and opaque > 0.25, (name, transparent, opaque)
        filename = f"portrait_{name}_v01.png"
        result.save(NORMALIZED / filename, optimize=True)
        result.save(RUNTIME / filename, optimize=True)
        for j, color in enumerate(((22, 26, 28, 255), (211, 202, 181, 255))):
            canvas = Image.new("RGBA", result.size, color)
            canvas.alpha_composite(result)
            canvas.resize((384, 576), Image.Resampling.LANCZOS).convert("RGB").save(
                PREVIEW / f"{name}_{'dark' if j == 0 else 'light'}_v01.jpg", quality=95)
            sheet.paste(canvas.resize((384, 576), Image.Resampling.LANCZOS).convert("RGB"),
                        (i * 768 + j * 384, 24))
        crop = result.crop((0, 32, 1024, 608))
        closeup = Image.new("RGBA", crop.size, (22, 26, 28, 255))
        closeup.alpha_composite(crop)
        sheet.paste(closeup.resize((768, 432), Image.Resampling.LANCZOS).convert("RGB"), (i * 768, 650))
        draw.text((i * 768 + 12, 8), name + " / dark and parchment", fill=(233, 223, 195))
        report.append({"asset": filename, "size": result.size, "mode": result.mode,
                       "transparent_ratio": transparent, "opaque_ratio": opaque,
                       "partial_alpha_ratio": float(np.mean((alpha > 0) & (alpha < 255))),
                       "source": str((RAW / f"{name}_rgb_v01.png").relative_to(ROOT))})
    sheet.save(PREVIEW / "alpha_edge_review_v01.jpg", quality=95)
    (NORMALIZED / "matte_report.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
