#!/usr/bin/env python3
"""Normalize generated cave backgrounds and build Unity-ready scene assets."""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


THEMES = ("combat", "sanctuary", "vault", "mystic")
ORIENTATIONS = {
    "landscape": (1920, 1080),
    "portrait": (1080, 1920),
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--raw-dir", type=Path, required=True)
    parser.add_argument("--normalized-dir", type=Path, required=True)
    parser.add_argument("--runtime-dir", type=Path, required=True)
    parser.add_argument("--preview", type=Path, required=True)
    parser.add_argument("--key-helper", type=Path, required=True)
    return parser.parse_args()


def crop_to_ratio(image: Image.Image, target_size: tuple[int, int]) -> Image.Image:
    target_width, target_height = target_size
    target_ratio = target_width / target_height
    source_ratio = image.width / image.height
    if source_ratio > target_ratio:
        cropped_width = round(image.height * target_ratio)
        left = (image.width - cropped_width) // 2
        image = image.crop((left, 0, left + cropped_width, image.height))
    elif source_ratio < target_ratio:
        cropped_height = round(image.width / target_ratio)
        top = (image.height - cropped_height) // 2
        image = image.crop((0, top, image.width, top + cropped_height))
    return image.resize(target_size, Image.Resampling.LANCZOS)


def fit_transparent(image: Image.Image, target_size: tuple[int, int], margin: int) -> Image.Image:
    image = image.convert("RGBA")
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError("Exit arch has no visible pixels after chroma-key removal")
    image = image.crop(bbox)
    max_width = target_size[0] - margin * 2
    max_height = target_size[1] - margin * 2
    scale = min(max_width / image.width, max_height / image.height)
    fitted = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.LANCZOS,
    )
    canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
    left = (target_size[0] - fitted.width) // 2
    top = (target_size[1] - fitted.height) // 2
    canvas.alpha_composite(fitted, (left, top))
    return canvas


def build_preview(runtime_dir: Path, output_path: Path) -> None:
    canvas = Image.new("RGB", (1600, 1050), (24, 27, 28))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default(size=22)
    small_font = ImageFont.load_default(size=17)
    draw.text((36, 24), "Wuxia Cave Scene Set v01", fill=(226, 196, 123), font=font)

    landscape_size = (360, 203)
    portrait_size = (180, 320)
    for index, theme in enumerate(THEMES):
        x = 36 + index * 388
        landscape = Image.open(runtime_dir / f"bg_cave_{theme}_landscape_v01.png").convert("RGB")
        landscape.thumbnail(landscape_size, Image.Resampling.LANCZOS)
        canvas.paste(landscape, (x, 72))
        draw.text((x, 286), f"{theme} / landscape", fill=(226, 226, 220), font=small_font)

        portrait = Image.open(runtime_dir / f"bg_cave_{theme}_portrait_v01.png").convert("RGB")
        portrait.thumbnail(portrait_size, Image.Resampling.LANCZOS)
        portrait_x = x + (landscape_size[0] - portrait.width) // 2
        canvas.paste(portrait, (portrait_x, 338))
        draw.text((x + 78, 670), f"{theme} / portrait", fill=(226, 226, 220), font=small_font)

    exit_arch = Image.open(runtime_dir / "cave_exit_arch_v01.png").convert("RGBA")
    checker = Image.new("RGBA", exit_arch.size, (45, 50, 51, 255))
    square = 32
    checker_draw = ImageDraw.Draw(checker)
    for y in range(0, checker.height, square):
        for x in range(0, checker.width, square):
            if (x // square + y // square) % 2 == 0:
                checker_draw.rectangle((x, y, x + square, y + square), fill=(72, 78, 78, 255))
    checker.alpha_composite(exit_arch)
    checker.thumbnail((270, 270), Image.Resampling.LANCZOS)
    canvas.paste(checker.convert("RGB"), (665, 740))
    draw.text((706, 1014), "transparent exit arch", fill=(226, 226, 220), font=small_font)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output_path, optimize=True)


def main() -> None:
    args = parse_args()
    args.normalized_dir.mkdir(parents=True, exist_ok=True)
    args.runtime_dir.mkdir(parents=True, exist_ok=True)

    created: list[str] = []
    for theme in THEMES:
        for orientation, target_size in ORIENTATIONS.items():
            source = args.raw_dir / f"cave_scene_{theme}_{orientation}_raw.png"
            output_name = f"bg_cave_{theme}_{orientation}_v01.png"
            normalized = crop_to_ratio(Image.open(source).convert("RGB"), target_size)
            normalized.save(args.normalized_dir / output_name, optimize=True)
            normalized.save(args.runtime_dir / output_name, optimize=True)
            created.append(output_name)

    with tempfile.TemporaryDirectory(prefix="wuxia-cave-exit-") as temp_dir_value:
        keyed_path = Path(temp_dir_value) / "cave_exit_arch_keyed.png"
        subprocess.run([
            sys.executable,
            str(args.key_helper),
            "--input", str(args.raw_dir / "cave_exit_arch_raw.png"),
            "--out", str(keyed_path),
            "--auto-key", "border",
            "--soft-matte",
            "--transparent-threshold", "12",
            "--opaque-threshold", "110",
            "--edge-contract", "1",
            "--despill",
            "--force",
        ], check=True)
        exit_arch = fit_transparent(Image.open(keyed_path), (512, 512), 22)

    alpha = exit_arch.getchannel("A")
    alpha_histogram = alpha.histogram()
    pixel_count = exit_arch.width * exit_arch.height
    transparent_ratio = alpha_histogram[0] / pixel_count
    visible_ratio = sum(alpha_histogram[32:]) / pixel_count
    if transparent_ratio < 0.25 or visible_ratio < 0.08:
        raise RuntimeError(
            f"Exit alpha validation failed: transparent={transparent_ratio:.3f}, visible={visible_ratio:.3f}")
    exit_arch.save(args.normalized_dir / "cave_exit_arch_v01.png", optimize=True)
    exit_arch.save(args.runtime_dir / "cave_exit_arch_v01.png", optimize=True)
    created.append("cave_exit_arch_v01.png")

    build_preview(args.runtime_dir, args.preview)
    print(f"Created {len(created)} cave scene assets")
    print("\n".join(created))


if __name__ == "__main__":
    main()
