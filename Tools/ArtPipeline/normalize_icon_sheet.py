#!/usr/bin/env python3
"""Split a transparent icon sheet into consistently padded Unity PNG icons."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--out-dir", required=True)
    parser.add_argument("--names", required=True, help="Comma-separated filenames without extension.")
    parser.add_argument("--columns", type=int, default=3)
    parser.add_argument("--rows", type=int, default=2)
    parser.add_argument("--size", type=int, default=128)
    parser.add_argument("--safe-size", type=int, default=96)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    names = [name.strip() for name in args.names.split(",") if name.strip()]
    if len(names) > args.columns * args.rows:
        raise ValueError("More names were provided than grid cells.")

    source = Image.open(args.input).convert("RGBA")
    output_dir = Path(args.out_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    for index, name in enumerate(names):
        column = index % args.columns
        row = index // args.columns
        left = round(column * source.width / args.columns)
        right = round((column + 1) * source.width / args.columns)
        top = round(row * source.height / args.rows)
        bottom = round((row + 1) * source.height / args.rows)
        cell = source.crop((left, top, right, bottom))
        bounds = cell.getchannel("A").getbbox()
        if bounds is None:
            raise ValueError(f"Grid cell {index} for {name} is empty.")

        content = cell.crop(bounds)
        scale = min(args.safe_size / content.width, args.safe_size / content.height)
        width = max(1, round(content.width * scale))
        height = max(1, round(content.height * scale))
        content = content.resize((width, height), Image.Resampling.LANCZOS)

        icon = Image.new("RGBA", (args.size, args.size), (0, 0, 0, 0))
        position = ((args.size - width) // 2, (args.size - height) // 2)
        icon.alpha_composite(content, position)
        output = output_dir / f"{name}.png"
        icon.save(output)
        print(output)


if __name__ == "__main__":
    main()
