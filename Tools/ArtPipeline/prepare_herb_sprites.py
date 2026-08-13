#!/usr/bin/env python3
"""Normalize generated herb cutouts into game-ready 256 px world sprites."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


CANVAS_SIZE = 256
SUBJECT_MAX_WIDTH = 208
SUBJECT_MAX_HEIGHT = 188
GROUND_Y = 224


def normalize(source: Path, destination: Path) -> None:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"No opaque subject found in {source}")

    subject = image.crop(bounds)
    scale = min(SUBJECT_MAX_WIDTH / subject.width, SUBJECT_MAX_HEIGHT / subject.height)
    size = (
        max(1, round(subject.width * scale)),
        max(1, round(subject.height * scale)),
    )
    subject = subject.resize(size, Image.Resampling.NEAREST)

    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    x = (CANVAS_SIZE - subject.width) // 2
    y = GROUND_Y - subject.height
    canvas.alpha_composite(subject, (x, y))
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(destination, optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    normalize(args.input, args.output)


if __name__ == "__main__":
    main()
