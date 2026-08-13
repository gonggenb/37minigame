#!/usr/bin/env python3
"""Normalize generated main-map ground art into a quiet seamless Unity texture."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageEnhance


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--preview", type=Path)
    parser.add_argument("--size", type=int, default=1024)
    parser.add_argument("--contrast", type=float, default=0.96)
    parser.add_argument("--saturation", type=float, default=0.94)
    return parser.parse_args()


def center_square(image: Image.Image) -> Image.Image:
    width, height = image.size
    side = min(width, height)
    left = (width - side) // 2
    top = (height - side) // 2
    return image.crop((left, top, left + side, top + side))


def make_offset_blended_seamless(image: Image.Image) -> Image.Image:
    """Move source edges to the center, then blend the cross without mirror symmetry."""
    pixels = np.asarray(image, dtype=np.float32)
    height, width = pixels.shape[:2]
    shifted = np.roll(pixels, shift=(height // 2, width // 2), axis=(0, 1))

    blend_width = max(24, int(min(width, height) * 0.176))
    x = np.arange(width, dtype=np.float32)
    y = np.arange(height, dtype=np.float32)
    weight_x = np.clip(1.0 - np.abs(x - width * 0.5) / blend_width, 0.0, 1.0)
    weight_y = np.clip(1.0 - np.abs(y - height * 0.5) / blend_width, 0.0, 1.0)
    weight_x = weight_x * weight_x * (3.0 - 2.0 * weight_x)
    weight_y = weight_y * weight_y * (3.0 - 2.0 * weight_y)
    mask = np.maximum(weight_y[:, None], weight_x[None, :])[..., None]

    blended = shifted * (1.0 - mask) + pixels * mask
    return Image.fromarray(np.clip(blended, 0.0, 255.0).astype(np.uint8), "RGB")


def seam_metrics(image: Image.Image) -> tuple[float, float, float]:
    pixels = np.asarray(image, dtype=np.float32)
    left_right = float(np.abs(pixels[:, 0] - pixels[:, -1]).mean())
    top_bottom = float(np.abs(pixels[0] - pixels[-1]).mean())
    internal_x = np.abs(pixels[:, 1:] - pixels[:, :-1]).mean()
    internal_y = np.abs(pixels[1:] - pixels[:-1]).mean()
    internal = float((internal_x + internal_y) * 0.5)
    return left_right, top_bottom, internal


def save_preview(image: Image.Image, path: Path) -> None:
    preview_tile = image.resize((512, 512), Image.Resampling.LANCZOS)
    preview = Image.new("RGB", (1024, 1024))
    for y in (0, 512):
        for x in (0, 512):
            preview.paste(preview_tile, (x, y))
    path.parent.mkdir(parents=True, exist_ok=True)
    preview.save(path, optimize=True)


def main() -> None:
    args = parse_args()
    if args.size <= 0:
        raise ValueError("--size must be positive")

    with Image.open(args.input) as source:
        image = center_square(source.convert("RGB"))
    image = image.resize((args.size, args.size), Image.Resampling.LANCZOS)
    image = make_offset_blended_seamless(image)
    image = ImageEnhance.Contrast(image).enhance(args.contrast)
    image = ImageEnhance.Color(image).enhance(args.saturation)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.output, optimize=True)
    if args.preview is not None:
        save_preview(image, args.preview)

    left_right, top_bottom, internal = seam_metrics(image)
    ratio_lr = left_right / max(internal, 0.001)
    ratio_tb = top_bottom / max(internal, 0.001)
    if max(ratio_lr, ratio_tb) > 1.35:
        raise RuntimeError(
            f"Texture edge transition is too visible: lr={ratio_lr:.3f}, tb={ratio_tb:.3f}"
        )
    print(
        f"normalized={args.output} size={image.size} mode={image.mode} "
        f"edge_delta_lr={left_right:.3f} edge_delta_tb={top_bottom:.3f} "
        f"internal_delta={internal:.3f} seam_ratio_lr={ratio_lr:.3f} "
        f"seam_ratio_tb={ratio_tb:.3f}"
    )


if __name__ == "__main__":
    main()
