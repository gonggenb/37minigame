#!/usr/bin/env python3
"""Prepare generated HD-2D main-world art for Unity and build atmosphere maps."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageEnhance, ImageFilter


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path.cwd())
    return parser.parse_args()


def resize_rgba_cutout(source: Path, destination: Path) -> None:
    with Image.open(source) as opened:
        image = opened.convert("RGBA")

    pixels = np.asarray(image, dtype=np.uint8).copy()
    alpha = pixels[..., 3].astype(np.float32)
    alpha = np.clip((alpha - 8.0) * (255.0 / 239.0), 0.0, 255.0)
    pixels[..., 3] = alpha.astype(np.uint8)
    image = Image.fromarray(pixels, "RGBA")

    box = image.getchannel("A").getbbox()
    if box is None:
        raise RuntimeError(f"No visible cutout pixels in {source}")
    image = image.crop(box)

    max_extent = 922
    scale = min(max_extent / image.width, max_extent / image.height)
    resized = image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.Resampling.LANCZOS,
    )
    canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    canvas.alpha_composite(
        resized,
        ((canvas.width - resized.width) // 2, canvas.height - resized.height - 48),
    )
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination, optimize=True)


def prepare_backdrop(source: Path, destination: Path) -> None:
    with Image.open(source) as opened:
        image = opened.convert("RGB")
    image = image.resize((2048, 1152), Image.Resampling.LANCZOS)
    image = ImageEnhance.Contrast(image).enhance(1.04)
    image = ImageEnhance.Color(image).enhance(0.96)
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination, optimize=True)


def prepare_panorama(source: Path, destination: Path) -> None:
    """Build a horizontally seamless 2:1 sky panorama from the approved backdrop."""
    with Image.open(source) as opened:
        backdrop = opened.convert("RGB")

    half = backdrop.resize((1024, 576), Image.Resampling.LANCZOS)
    half_pixels = np.asarray(half, dtype=np.uint8)
    top_color = half_pixels[:24].mean(axis=(0, 1)).astype(np.uint8)
    bottom_color = half_pixels[-24:].mean(axis=(0, 1)).astype(np.uint8)

    tile_pixels = np.empty((1024, 1024, 3), dtype=np.uint8)
    for y in range(224):
        blend = y / 223.0
        tile_pixels[y] = np.clip(
            top_color * (1.0 - blend * 0.18) + half_pixels[0].mean(axis=0) * (blend * 0.18),
            0,
            255,
        ).astype(np.uint8)
    tile_pixels[224:800] = half_pixels
    for y in range(800, 1024):
        blend = (y - 800) / 223.0
        tile_pixels[y] = np.clip(
            half_pixels[-1].mean(axis=0) * (1.0 - blend * 0.3) + bottom_color * (blend * 0.3),
            0,
            255,
        ).astype(np.uint8)

    tile = Image.fromarray(tile_pixels, "RGB")
    panorama = Image.new("RGB", (2048, 1024))
    panorama.paste(tile, (0, 0))
    panorama.paste(tile.transpose(Image.Transpose.FLIP_LEFT_RIGHT), (1024, 0))
    panorama = ImageEnhance.Contrast(panorama).enhance(0.96)
    panorama = ImageEnhance.Color(panorama).enhance(0.92)
    destination.parent.mkdir(parents=True, exist_ok=True)
    panorama.save(destination, optimize=True)


def make_offset_blended_seamless(image: Image.Image) -> Image.Image:
    pixels = np.asarray(image.convert("RGB"), dtype=np.float32)
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


def prepare_water(source: Path, destination: Path, preview: Path) -> None:
    with Image.open(source) as opened:
        image = opened.convert("RGB").resize((1024, 1024), Image.Resampling.LANCZOS)
    image = make_offset_blended_seamless(image)
    image = ImageEnhance.Contrast(image).enhance(0.94)
    image = ImageEnhance.Color(image).enhance(0.92)
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination, optimize=True)

    tile = image.resize((512, 512), Image.Resampling.LANCZOS)
    tiled = Image.new("RGB", (1024, 1024))
    for top in (0, 512):
        for left in (0, 512):
            tiled.paste(tile, (left, top))
    preview.parent.mkdir(parents=True, exist_ok=True)
    tiled.save(preview, optimize=True)


def build_mist(destination: Path) -> None:
    rng = np.random.default_rng(240813)
    coarse = (rng.random((24, 96)) * 255).astype(np.uint8)
    noise = Image.fromarray(coarse, "L").resize((1024, 256), Image.Resampling.BICUBIC)
    noise = noise.filter(ImageFilter.GaussianBlur(18))
    values = np.asarray(noise, dtype=np.float32) / 255.0
    y = np.linspace(-1.0, 1.0, 256, dtype=np.float32)
    envelope = np.clip(1.0 - np.abs(y) ** 1.7, 0.0, 1.0)[:, None]
    alpha = np.clip((values * 0.48 + 0.22) * envelope * 150.0, 0.0, 150.0).astype(np.uint8)
    rgba = np.zeros((256, 1024, 4), dtype=np.uint8)
    rgba[..., 0] = 210
    rgba[..., 1] = 222
    rgba[..., 2] = 216
    rgba[..., 3] = alpha
    destination.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(destination, optimize=True)


def build_light_beam(destination: Path) -> None:
    width, height = 256, 512
    x = np.linspace(-1.0, 1.0, width, dtype=np.float32)[None, :]
    y = np.linspace(0.0, 1.0, height, dtype=np.float32)[:, None]
    half_width = 0.18 + y * 0.62
    horizontal = np.clip(1.0 - np.abs(x) / half_width, 0.0, 1.0) ** 1.8
    vertical = np.clip(np.sin(np.pi * np.clip(y, 0.0, 1.0)), 0.0, 1.0) ** 0.75
    alpha = np.clip(horizontal * vertical * 90.0, 0.0, 90.0).astype(np.uint8)
    rgba = np.zeros((height, width, 4), dtype=np.uint8)
    rgba[..., 0] = 255
    rgba[..., 1] = 226
    rgba[..., 2] = 166
    rgba[..., 3] = alpha
    destination.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba, "RGBA").save(destination, optimize=True)


def main() -> None:
    root = parse_args().project_root.resolve()
    raw = root / "ArtSource/Raw/Environment/HD2D"
    normalized = root / "ArtSource/Normalized/Environment/HD2D"
    previews = root / "ArtSource/Previews/Environment/HD2D"
    unity = root / "Assets/Art/Generated/Environment/HD2D"

    backdrop = normalized / "tex_env_hd2d_mountain_backdrop_2048x1152_v01.png"
    panorama = normalized / "tex_env_hd2d_mountain_panorama_2048x1024_v01.png"
    bamboo = normalized / "spr_env_hd2d_bamboo_cluster_1024_v01.png"
    pine = normalized / "spr_env_hd2d_pine_rock_1024_v01.png"
    water = normalized / "tex_env_hd2d_water_albedo_1024_v01.png"
    mist = normalized / "spr_env_hd2d_mist_band_1024x256_v01.png"
    beam = normalized / "spr_env_hd2d_light_beam_256x512_v01.png"

    prepare_backdrop(raw / "tex_env_hd2d_mountain_backdrop_v01_raw.png", backdrop)
    prepare_panorama(backdrop, panorama)
    resize_rgba_cutout(raw / "spr_env_hd2d_bamboo_cluster_v01_raw.png", bamboo)
    resize_rgba_cutout(raw / "spr_env_hd2d_pine_rock_v01_raw.png", pine)
    prepare_water(
        raw / "tex_env_hd2d_water_v01_raw.png",
        water,
        previews / "tex_env_hd2d_water_tiles_v01.png",
    )
    build_mist(mist)
    build_light_beam(beam)

    unity.mkdir(parents=True, exist_ok=True)
    for source in (backdrop, panorama, bamboo, pine, water, mist, beam):
        (unity / source.name).write_bytes(source.read_bytes())
        print(f"prepared={source.relative_to(root)} -> {(unity / source.name).relative_to(root)}")


if __name__ == "__main__":
    main()
