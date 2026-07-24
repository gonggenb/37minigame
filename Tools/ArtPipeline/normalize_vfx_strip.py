#!/usr/bin/env python3
"""Normalize a transparent horizontal VFX strip with a shared center and scale."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--frames-dir")
    parser.add_argument("--frames", type=int, default=6)
    parser.add_argument("--frame-size", type=int, default=256)
    parser.add_argument("--margin", type=int, default=24)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    source = Image.open(args.input).convert("RGBA")
    slices: list[Image.Image] = []
    bounds: list[tuple[int, int, int, int]] = []
    for index in range(args.frames):
        left = round(index * source.width / args.frames)
        right = round((index + 1) * source.width / args.frames)
        frame = source.crop((left, 0, right, source.height))
        alpha_bounds = frame.getchannel("A").getbbox()
        if alpha_bounds is None:
            alpha_bounds = (0, 0, 1, 1)
        slices.append(frame)
        bounds.append(alpha_bounds)

    usable_size = max(1, args.frame_size - args.margin * 2)
    max_extent = max(max(box[2] - box[0], box[3] - box[1]) for box in bounds)
    shared_scale = usable_size / max(1, max_extent)
    strip = Image.new("RGBA", (args.frame_size * args.frames, args.frame_size), (0, 0, 0, 0))
    frames_dir = Path(args.frames_dir) if args.frames_dir else None
    if frames_dir is not None:
        frames_dir.mkdir(parents=True, exist_ok=True)

    for index, (frame, box) in enumerate(zip(slices, bounds)):
        content = frame.crop(box)
        width = max(1, round(content.width * shared_scale))
        height = max(1, round(content.height * shared_scale))
        content = content.resize((width, height), Image.Resampling.NEAREST)
        slot = Image.new("RGBA", (args.frame_size, args.frame_size), (0, 0, 0, 0))
        position = ((args.frame_size - width) // 2, (args.frame_size - height) // 2)
        slot.alpha_composite(content, position)
        strip.alpha_composite(slot, (index * args.frame_size, 0))
        if frames_dir is not None:
            slot.save(frames_dir / f"frame_{index + 1:02d}.png")

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    strip.save(output)
    print(f"Wrote {output} with shared scale {shared_scale:.4f}")


if __name__ == "__main__":
    main()
