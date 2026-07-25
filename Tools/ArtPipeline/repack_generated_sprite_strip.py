#!/usr/bin/env python3
"""Repack separated characters from an AI-generated horizontal sprite strip."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--frames", type=int, default=8)
    parser.add_argument("--alpha-threshold", type=int, default=24)
    parser.add_argument("--min-component-pixels", type=int, default=100)
    parser.add_argument("--padding", type=int, default=8)
    return parser.parse_args()


def find_components(
    image: Image.Image,
    alpha_threshold: int,
    min_component_pixels: int,
) -> list[tuple[int, tuple[int, int, int, int]]]:
    alpha = image.getchannel("A")
    width, height = image.size
    pixels = alpha.load()
    visited = bytearray(width * height)
    components: list[tuple[int, tuple[int, int, int, int]]] = []

    for y in range(height):
        for x in range(width):
            flat_index = y * width + x
            if visited[flat_index] or pixels[x, y] <= alpha_threshold:
                continue

            visited[flat_index] = 1
            queue = deque([(x, y)])
            count = 0
            min_x = max_x = x
            min_y = max_y = y

            while queue:
                current_x, current_y = queue.popleft()
                count += 1
                min_x = min(min_x, current_x)
                max_x = max(max_x, current_x)
                min_y = min(min_y, current_y)
                max_y = max(max_y, current_y)

                for next_x, next_y in (
                    (current_x - 1, current_y),
                    (current_x + 1, current_y),
                    (current_x, current_y - 1),
                    (current_x, current_y + 1),
                ):
                    if next_x < 0 or next_x >= width or next_y < 0 or next_y >= height:
                        continue
                    next_index = next_y * width + next_x
                    if visited[next_index] or pixels[next_x, next_y] <= alpha_threshold:
                        continue
                    visited[next_index] = 1
                    queue.append((next_x, next_y))

            if count >= min_component_pixels:
                components.append((count, (min_x, min_y, max_x + 1, max_y + 1)))

    return components


def main() -> None:
    args = parse_args()
    source = Image.open(args.input).convert("RGBA")
    components = find_components(
        source,
        args.alpha_threshold,
        args.min_component_pixels,
    )
    components = sorted(components, key=lambda item: item[0], reverse=True)[: args.frames]
    if len(components) != args.frames:
        raise SystemExit(
            f"Expected {args.frames} character components, detected {len(components)}."
        )

    components.sort(key=lambda item: item[1][0])
    crops = [source.crop(bounds) for _, bounds in components]
    slot_width = max(crop.width for crop in crops) + args.padding * 2
    output = Image.new(
        "RGBA",
        (slot_width * args.frames, source.height),
        (0, 0, 0, 0),
    )

    for index, crop in enumerate(crops):
        offset_x = index * slot_width + (slot_width - crop.width) // 2
        offset_y = source.height - crop.height
        output.alpha_composite(crop, (offset_x, offset_y))

    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output.save(output_path)
    print(
        f"Wrote {output_path} with {args.frames} slots of {slot_width}px "
        f"from {len(components)} detected components."
    )


if __name__ == "__main__":
    main()
