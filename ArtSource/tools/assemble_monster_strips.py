#!/usr/bin/env python3
"""Normalize paired idle/attack strips into Unity-ready 256 px sprite sheets."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw


FRAME_COUNT = 8
FRAME_SIZE = 256
GROUND_Y = 224
TOP_MARGIN = 8
SIDE_MARGIN = 8
ALPHA_THRESHOLD = 8


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--idle", required=True)
    parser.add_argument("--attack", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--normalized-root", required=True)
    parser.add_argument("--assets-dir", required=True)
    parser.add_argument("--preview", required=True)
    parser.add_argument(
        "--layout",
        choices=("horizontal", "grid4x2"),
        default="horizontal",
    )
    return parser.parse_args()


def split_sheet(path: str, layout: str) -> list[Image.Image]:
    sheet = Image.open(path).convert("RGBA")
    if layout == "horizontal":
        step = sheet.width / FRAME_COUNT
        return [
            sheet.crop(
                (
                    round(index * step),
                    0,
                    round((index + 1) * step),
                    sheet.height,
                )
            )
            for index in range(FRAME_COUNT)
        ]

    cell_width = sheet.width / 4
    cell_height = sheet.height / 2
    return [
        sheet.crop(
            (
                round(column * cell_width),
                round(row * cell_height),
                round((column + 1) * cell_width),
                round((row + 1) * cell_height),
            )
        )
        for row in range(2)
        for column in range(4)
    ]


def crop_content(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A").point(
        lambda value: 255 if value > ALPHA_THRESHOLD else 0
    )
    bbox = alpha.getbbox()
    if bbox is None:
        raise RuntimeError("No visible sprite content found in frame.")
    return image.crop(bbox)


def normalize_frame(image: Image.Image, scale: float) -> Image.Image:
    width = max(1, round(image.width * scale))
    height = max(1, round(image.height * scale))
    sprite = image.resize((width, height), Image.Resampling.NEAREST)
    canvas = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
    x = (FRAME_SIZE - width) // 2
    y = GROUND_Y - height
    canvas.alpha_composite(sprite, (x, y))
    return canvas


def save_strip(frames: list[Image.Image], path: Path) -> None:
    strip = Image.new(
        "RGBA", (FRAME_SIZE * FRAME_COUNT, FRAME_SIZE), (0, 0, 0, 0)
    )
    for index, frame in enumerate(frames):
        strip.alpha_composite(frame, (index * FRAME_SIZE, 0))
    path.parent.mkdir(parents=True, exist_ok=True)
    strip.save(path)


def save_preview(
    idle_frames: list[Image.Image],
    attack_frames: list[Image.Image],
    path: Path,
) -> None:
    cell = 128
    label_width = 96
    preview = Image.new(
        "RGBA", (label_width + cell * FRAME_COUNT, cell * 2), (30, 34, 39, 255)
    )
    draw = ImageDraw.Draw(preview)
    for row, (label, frames) in enumerate(
        (("IDLE", idle_frames), ("ATTACK", attack_frames))
    ):
        draw.text((14, row * cell + 56), label, fill=(220, 224, 228, 255))
        for column, frame in enumerate(frames):
            thumb = frame.resize((cell, cell), Image.Resampling.NEAREST)
            x = label_width + column * cell
            y = row * cell
            tile = 16
            for checker_y in range(0, cell, tile):
                for checker_x in range(0, cell, tile):
                    tone = 62 if (checker_x // tile + checker_y // tile) % 2 else 76
                    draw.rectangle(
                        (
                            x + checker_x,
                            y + checker_y,
                            x + checker_x + tile - 1,
                            y + checker_y + tile - 1,
                        ),
                        fill=(tone, tone, tone, 255),
                    )
            preview.alpha_composite(thumb, (x, y))
    path.parent.mkdir(parents=True, exist_ok=True)
    preview.convert("RGB").save(path)


def main() -> None:
    args = parse_args()
    idle_content = [
        crop_content(frame) for frame in split_sheet(args.idle, args.layout)
    ]
    attack_content = [
        crop_content(frame) for frame in split_sheet(args.attack, args.layout)
    ]

    max_width = max(frame.width for frame in idle_content + attack_content)
    max_height = max(frame.height for frame in idle_content + attack_content)
    scale = min(
        (FRAME_SIZE - SIDE_MARGIN * 2) / max_width,
        (GROUND_Y - TOP_MARGIN) / max_height,
    )

    idle_frames = [normalize_frame(frame, scale) for frame in idle_content]
    attack_frames = [normalize_frame(frame, scale) for frame in attack_content]
    # Frame 1 is the animation hand-off frame; locking it prevents a visible pop.
    attack_frames[0] = idle_frames[0].copy()

    normalized_root = Path(args.normalized_root) / args.name
    for action, frames in (("idle", idle_frames), ("attack", attack_frames)):
        frame_dir = normalized_root / action
        frame_dir.mkdir(parents=True, exist_ok=True)
        for index, frame in enumerate(frames, start=1):
            frame.save(frame_dir / f"{index:02d}.png")

    snake_name = {
        "InkWolf": "ink_wolf",
        "StoneApe": "stone_ape",
        "BambooPuppet": "bamboo_puppet",
    }[args.name]
    assets_dir = Path(args.assets_dir)
    save_strip(
        idle_frames,
        assets_dir / f"spr_enemy_{snake_name}_idle_right_8f_v01.png",
    )
    save_strip(
        attack_frames,
        assets_dir / f"spr_enemy_{snake_name}_attack_right_8f_v01.png",
    )
    save_preview(idle_frames, attack_frames, Path(args.preview))

    print(
        f"{args.name}: shared scale={scale:.4f}, "
        f"max source content={max_width}x{max_height}"
    )


if __name__ == "__main__":
    main()
