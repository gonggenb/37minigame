#!/usr/bin/env python3
"""Normalize generated UI frames into deterministic Unity nine-slice textures."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageEnhance


TINTS = {
    "normal": ((255, 255, 255), 0.0, 1.0),
    "hover": ((209, 170, 90), 0.08, 1.12),
    "pressed": ((176, 111, 52), 0.08, 0.78),
    "selected": ((209, 170, 90), 0.13, 1.02),
    "primary": ((181, 138, 70), 0.10, 1.0),
    "primary_hover": ((209, 170, 90), 0.14, 1.12),
}


def visible_bbox(image: Image.Image, threshold: int) -> tuple[int, int, int, int]:
    alpha = image.getchannel("A").point(lambda value: 255 if value > threshold else 0)
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("input contains no visible pixels")
    return bbox


def crop_with_padding(image: Image.Image, threshold: int, padding_ratio: float) -> Image.Image:
    left, top, right, bottom = visible_bbox(image, threshold)
    width = right - left
    height = bottom - top
    padding = max(2, round(max(width, height) * padding_ratio))
    left = max(0, left - padding)
    top = max(0, top - padding)
    right = min(image.width, right + padding)
    bottom = min(image.height, bottom + padding)
    return image.crop((left, top, right, bottom))


def contain(image: Image.Image, width: int, height: int) -> Image.Image:
    scale = min(width / image.width, height / image.height)
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.LANCZOS,
    )
    output = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    output.alpha_composite(resized, ((width - resized.width) // 2, (height - resized.height) // 2))
    return output


def apply_tone(image: Image.Image, variant: str) -> Image.Image:
    tint, tint_strength, brightness = TINTS[variant]
    alpha = image.getchannel("A")
    rgb = ImageEnhance.Brightness(image.convert("RGB")).enhance(brightness)
    if tint_strength > 0:
        overlay = Image.new("RGB", rgb.size, tint)
        rgb = Image.blend(rgb, overlay, tint_strength)
    rgb.putalpha(alpha)
    return rgb


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--width", required=True, type=int)
    parser.add_argument("--height", required=True, type=int)
    parser.add_argument("--alpha-threshold", type=int, default=12)
    parser.add_argument("--padding-ratio", type=float, default=0.012)
    parser.add_argument("--variant", choices=tuple(TINTS), default="normal")
    args = parser.parse_args()

    source = Image.open(args.input).convert("RGBA")
    cropped = crop_with_padding(source, args.alpha_threshold, args.padding_ratio)
    normalized = contain(cropped, args.width, args.height)
    normalized = apply_tone(normalized, args.variant)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    normalized.save(args.output, optimize=True)


if __name__ == "__main__":
    main()
