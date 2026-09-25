#!/usr/bin/env python3
"""Reviewed, conservative cutout repairs; never resize or repack animation frames.

uv run --with pillow --with numpy --with scipy python Tools/ArtPipeline/clean_character_alpha_edges.py
Add --apply after reviewing docs/validation/alpha_cleanup. Originals are retained
outside Assets. Repeated runs always start from those originals, not cleaned pixels.
"""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parents[2]
BASELINE = ROOT / "ArtSource/Raw/AlphaCleanup"
OUTPUT = ROOT / "ArtSource/Normalized/AlphaCleanup"
REVIEW = ROOT / "docs/validation/alpha_cleanup"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def edge_band(alpha, width):
    visible = alpha > 0
    return visible & ~ndi.binary_erosion(visible, iterations=width, border_value=1)


def repair_portrait(pixels, name):
    result = pixels.copy()
    rgb = pixels[:, :, :3].astype(float)
    alpha = pixels[:, :, 3]
    light = rgb.mean(2)
    if name == "hero":
        # Reviewed dark hair and blue shoulder only. White sleeves, skin, face,
        # jewelry and the lower costume are outside this mask.
        yy, xx = np.indices(alpha.shape)
        region = (((xx < 440) & (yy < 680)) |
                  ((yy < 290) & (xx < 690)) |
                  ((xx > 630) & (xx < 720) & (yy < 455)) |
                  ((xx > 290) & (xx < 780) & (yy > 450) & (yy < 525)))
        radius = 6
        valid = np.where(alpha > 200, light, 1000)
        darkest = ndi.minimum_filter(valid, radius * 2 + 1, mode="constant", cval=1000)
        change = (region & edge_band(alpha, 6) & (light > darkest + 45) &
                  (darkest < 135) & (light > 100) & (np.ptp(rgb, axis=2) < 60))
        # Use a real nearby RGB triplet, not independent channel minima. Padded
        # windows prevent sampling the opposite side of the texture at a border.
        padded_light = np.pad(valid, radius, constant_values=1000)
        padded_rgb = np.pad(pixels[:, :, :3], ((radius, radius), (radius, radius), (0, 0)))
        h, w = alpha.shape
        pending = change.copy()
        for dy in range(radius * 2 + 1):
            for dx in range(radius * 2 + 1):
                take = pending & (padded_light[dy:dy+h, dx:dx+w] == darkest)
                result[:, :, :3][take] = padded_rgb[dy:dy+h, dx:dx+w][take]
                pending[take] = False
    else:
        # Fox fur is intentionally white. Only decontaminate a narrow bright
        # rim where a nearby opaque, darker interior supplies the local color.
        interior = ndi.binary_erosion(alpha >= 250, iterations=2, border_value=1)
        distance, indices = ndi.distance_transform_edt(~interior, return_indices=True)
        reference = rgb[tuple(indices)]
        change = (edge_band(alpha, 2) & (distance <= 3) &
                  (light > reference.mean(2) + 45) & (reference.mean(2) < 135))
        result[:, :, :3][change] = reference[change].astype(np.uint8)
    # Color-only defringing preserves the exact silhouette and fine hair alpha.
    assert np.array_equal(result[:, :, 3], alpha)
    return result


def repair_frame(pixels):
    result = pixels.copy()
    rgb = pixels[:, :, :3].astype(float)
    alpha = pixels[:, :, 3]
    light = rgb.mean(2)
    chroma = np.ptp(rgb, axis=2)
    yy, _ = np.indices(alpha.shape)
    dark = (alpha >= 240) & (light < 85) & (chroma < 25) & (yy < 170)
    labels, count = ndi.label(dark)
    if not count:
        return result
    areas = np.bincount(labels.ravel())
    areas[0] = 0
    ident = int(areas.argmax())
    if areas[ident] < 60:
        return result
    hair = labels == ident
    distance, indices = ndi.distance_transform_edt(~hair, return_indices=True)
    pale = (alpha > 0) & (light > 125) & (chroma < 30)
    pale_labels, _ = ndi.label(pale, np.ones((3, 3)))
    pale_areas = np.bincount(pale_labels.ravel())
    # Only tiny exterior specks adjacent to the dark hair cluster. Large white
    # components (sword blades, cuffs, effects) and enclosed eye glints survive.
    change = (pale & (pale_areas[pale_labels] <= 12) &
              edge_band(alpha, 1) & (distance <= 1.5))
    result[:, :, :3][change] = pixels[:, :, :3][tuple(indices)][change]
    return result


def repair_strip(pixels, name):
    assert pixels.shape[0] == 256 and pixels.shape[1] % 256 == 0
    result = pixels.copy()
    for x in range(0, pixels.shape[1], 256):
        result[:, x:x+256] = repair_frame(pixels[:, x:x+256])
    if name == "spr_hero_attack_basic_right_8f_v01.png":
        frame = result[:, 5*256:6*256]
        rgb = frame[:, :, :3].astype(int)
        pale = (rgb.min(2) > 190) & (np.ptp(rgb, axis=2) < 30) & (frame[:, :, 3] > 0)
        labels, _ = ndi.label(pale)
        # Audited checker island inside the sixth pose's cyan sword trail.
        # Fail closed if future source art moves or changes this patch.
        ident = labels[180, 140]
        island = (labels == ident) if ident else np.zeros_like(pale)
        ys, xs = np.where(island)
        if not (1000 <= len(xs) <= 1500 and xs.min() >= 105 and xs.max() <= 180 and
                ys.min() >= 160 and ys.max() <= 200):
            raise ValueError("Basic attack checker patch no longer matches the reviewed source")
        frame[island] = 0
    return result


def preview(before, after, name, index):
    portrait = before.height != 256
    tile_w, tile_h = (256, 384) if portrait else (256, 256)
    # Animation comparison uses the impact pose; the full output retains every frame.
    if not portrait and before.width > 256:
        before = before.crop((1280, 0, 1536, 256))
        after = after.crop((1280, 0, 1536, 256))
    sheet = Image.new("RGB", (tile_w*4, tile_h+32), (25, 31, 34))
    draw = ImageDraw.Draw(sheet)
    for j, (im, color, label) in enumerate([
        (before, (25,31,34,255), "before / dark"), (after, (25,31,34,255), "after / dark"),
        (before, (211,202,181,255), "before / light"), (after, (211,202,181,255), "after / light")]):
        canvas = Image.new("RGBA", im.size, color)
        canvas.alpha_composite(im)
        canvas = canvas.resize((tile_w,tile_h), Image.Resampling.LANCZOS if portrait else Image.Resampling.NEAREST)
        sheet.paste(canvas.convert("RGB"), (j*tile_w, 32))
        draw.text((j*tile_w+8, 8), label, fill="white")
    sheet.save(REVIEW / f"{index:02d}_{name}.png")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    REVIEW.mkdir(parents=True, exist_ok=True)
    manifest_path = BASELINE / "manifest.json"
    manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else {}
    # Once reviewed, freeze the set. New characters must not silently inherit
    # a cleanup profile intended for this dark-haired hero.
    if manifest:
        paths = [ROOT / rel for rel in sorted(manifest)]
    else:
        paths = []
        for folder in ("HeroAttacks", "HeroEightDirections"):
            paths += sorted((ROOT / "Assets/Resources/Characters" / folder).glob("*.png"))
        paths += [ROOT / "Assets/Resources/OpeningDialogue" / f"portrait_{name}_v01.png"
                  for name in ("fox", "hero")]
    report = []
    prepared = []
    for path in paths:
        rel = path.relative_to(ROOT).as_posix()
        source = BASELINE / rel
        if source.exists():
            if digest(source) != manifest[rel]["source_sha256"]:
                raise ValueError(f"Baseline changed: {rel}")
            if digest(path) not in (manifest[rel]["source_sha256"], manifest[rel]["output_sha256"]):
                raise ValueError(f"New source needs review before refreshing its baseline: {rel}")
        else:
            source = path
        im = Image.open(source).convert("RGBA")
        before = np.array(im)
        after = (repair_portrait(before, "hero" if "hero" in path.name else "fox")
                 if path.name.startswith("portrait_") else repair_strip(before, path.name))
        changed = np.any(before != after, axis=2)
        if not changed.any():
            continue
        output = OUTPUT / rel
        output.parent.mkdir(parents=True, exist_ok=True)
        result = Image.fromarray(after)
        result.save(output, optimize=True)
        preview(im, result, path.stem, len(report))
        alpha_changed = before[:, :, 3] != after[:, :, 3]
        if "attack_basic_right_" not in path.name:
            assert not alpha_changed.any(), rel
        report.append({"path": rel, "size": list(im.size), "changed_pixels": int(changed.sum()),
                       "alpha_changed_pixels": int(alpha_changed.sum()),
                       "source_sha256": digest(source), "output_sha256": digest(output)})
        prepared.append((path, source, output, rel))
    # Validate the whole set before writing anything into Assets.
    if args.apply:
        for path, source, output, rel in prepared:
            backup = BASELINE / rel
            if not backup.exists():
                backup.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, backup)
            manifest[rel] = {"source_sha256": digest(backup), "output_sha256": digest(output)}
            if digest(path) != digest(output):
                shutil.copy2(output, path)
            if path.name.startswith("portrait_"):
                shutil.copy2(output, ROOT / "ArtSource/Normalized/OpeningDialogue" / path.name)
        manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
    result_report = {"applied": args.apply, "assets_scanned": len(paths), "assets_changed": len(report),
                     "scope": "Reviewed hero cutouts and two opening portraits; original dimensions, frame slots and pivots retained.",
                     "assets": report}
    (REVIEW / "pixel_report.json").write_text(json.dumps(result_report, indent=2) + "\n")
    print(json.dumps(result_report, indent=2))


if __name__ == "__main__":
    main()
