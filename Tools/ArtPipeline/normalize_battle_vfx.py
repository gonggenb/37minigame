#!/usr/bin/env python3
"""Normalize generated black-background VFX masters into Unity-ready 6-frame strips."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageEnhance


FRAME_SIZE = 256
FRAME_COUNT = 6


def extract_black_background(source: Image.Image, threshold: int = 18) -> Image.Image:
    rgba = source.convert("RGBA")
    pixels = []
    for red, green, blue, _ in rgba.get_flattened_data():
        value = max(red, green, blue)
        alpha = max(0, min(255, round((value - threshold) * 255 / (190 - threshold))))
        if alpha == 0:
            pixels.append((0, 0, 0, 0))
            continue

        compensation = 255 / max(alpha, 96)
        pixels.append((
            min(255, round(red * compensation)),
            min(255, round(green * compensation)),
            min(255, round(blue * compensation)),
            alpha,
        ))
    rgba.putdata(pixels)
    return rgba


def crop_visible(image: Image.Image) -> Image.Image:
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("source has no visible pixels after background extraction")
    return image.crop(bounds)


def pixel_fit(image: Image.Image, maximum_width: int, maximum_height: int) -> Image.Image:
    scale = min(maximum_width / image.width, maximum_height / image.height)
    width = max(2, round(image.width * scale))
    height = max(2, round(image.height * scale))
    low_width = max(1, width // 2)
    low_height = max(1, height // 2)
    low = image.resize((low_width, low_height), Image.Resampling.LANCZOS)
    return low.resize((width, height), Image.Resampling.NEAREST)


def alpha_scale(image: Image.Image, opacity: float) -> Image.Image:
    result = image.copy()
    result.putalpha(result.getchannel("A").point(lambda value: round(value * opacity)))
    return result


def make_strip(source: Image.Image, kind: str) -> Image.Image:
    visible = crop_visible(extract_black_background(source))
    visible = ImageEnhance.Contrast(visible).enhance(1.08)
    if kind == "sword":
        master = pixel_fit(visible, 236, 192)
        scales = (0.55, 0.78, 1.00, 0.92, 0.74, 0.50)
        opacities = (0.28, 0.68, 1.00, 0.86, 0.52, 0.18)
        angles = (-8, -4, 0, 3, 7, 10)
    else:
        master = pixel_fit(visible, 218, 218)
        scales = (0.64, 0.82, 1.00, 1.04, 0.92, 0.72)
        opacities = (0.26, 0.62, 0.96, 0.84, 0.54, 0.20)
        angles = (-5, -2, 0, 3, 6, 9)

    strip = Image.new("RGBA", (FRAME_SIZE * FRAME_COUNT, FRAME_SIZE), (0, 0, 0, 0))
    for index, (scale, opacity, angle) in enumerate(zip(scales, opacities, angles)):
        width = max(2, round(master.width * scale))
        height = max(2, round(master.height * scale))
        frame_effect = master.resize((width, height), Image.Resampling.NEAREST)
        frame_effect = frame_effect.rotate(angle, resample=Image.Resampling.NEAREST, expand=True)
        frame_effect = alpha_scale(frame_effect, opacity)
        x = index * FRAME_SIZE + (FRAME_SIZE - frame_effect.width) // 2
        y = (FRAME_SIZE - frame_effect.height) // 2
        strip.alpha_composite(frame_effect, (x, y))
    return strip


def save_preview(strip: Image.Image, output: Path) -> None:
    frames = [strip.crop((index * FRAME_SIZE, 0, (index + 1) * FRAME_SIZE, FRAME_SIZE))
              for index in range(FRAME_COUNT)]
    frames[0].save(output, save_all=True, append_images=frames[1:], duration=83,
                   loop=0, disposal=2, transparency=0)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sword-source", required=True, type=Path)
    parser.add_argument("--poison-source", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--preview-dir", required=True, type=Path)
    args = parser.parse_args()

    args.output_dir.mkdir(parents=True, exist_ok=True)
    args.preview_dir.mkdir(parents=True, exist_ok=True)
    jobs = (
        (args.sword_source, "sword", "spr_vfx_sword_qi_6f_v01.png"),
        (args.poison_source, "poison", "spr_vfx_poison_mist_6f_v01.png"),
    )
    for source_path, kind, filename in jobs:
        strip = make_strip(Image.open(source_path), kind)
        strip.save(args.output_dir / filename, optimize=True)
        save_preview(strip, args.preview_dir / filename.replace(".png", "_preview.gif"))
        print(f"normalized {source_path} -> {args.output_dir / filename}")


if __name__ == "__main__":
    main()
