#!/usr/bin/env python3
"""Remove cross-slot fragments from a generated horizontal sprite strip."""

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
    return parser.parse_args()


def largest_component_mask(image: Image.Image, threshold: int) -> Image.Image:
    alpha = image.getchannel("A")
    width, height = image.size
    pixels = alpha.load()
    visited = bytearray(width * height)
    largest: list[tuple[int, int]] = []

    for y in range(height):
        for x in range(width):
            flat_index = y * width + x
            if visited[flat_index] or pixels[x, y] <= threshold:
                continue

            visited[flat_index] = 1
            queue = deque([(x, y)])
            component: list[tuple[int, int]] = []
            while queue:
                current_x, current_y = queue.popleft()
                component.append((current_x, current_y))
                for next_x, next_y in (
                    (current_x - 1, current_y),
                    (current_x + 1, current_y),
                    (current_x, current_y - 1),
                    (current_x, current_y + 1),
                ):
                    if next_x < 0 or next_x >= width or next_y < 0 or next_y >= height:
                        continue
                    next_index = next_y * width + next_x
                    if visited[next_index] or pixels[next_x, next_y] <= threshold:
                        continue
                    visited[next_index] = 1
                    queue.append((next_x, next_y))

            if len(component) > len(largest):
                largest = component

    mask = Image.new("L", image.size, 0)
    mask_pixels = mask.load()
    for x, y in largest:
        mask_pixels[x, y] = alpha.getpixel((x, y))
    return mask


def main() -> None:
    args = parse_args()
    source = Image.open(args.input).convert("RGBA")
    slots: list[Image.Image] = []
    step = source.width / args.frames

    for index in range(args.frames):
        left = int(round(index * step))
        right = int(round((index + 1) * step))
        slot = source.crop((left, 0, right, source.height))
        mask = largest_component_mask(slot, args.alpha_threshold)
        cleaned = Image.new("RGBA", slot.size, (0, 0, 0, 0))
        cleaned.paste(slot, (0, 0), mask)
        slots.append(cleaned)

    slot_width = max(slot.width for slot in slots)
    output = Image.new("RGBA", (slot_width * args.frames, source.height), (0, 0, 0, 0))
    for index, slot in enumerate(slots):
        offset_x = index * slot_width + (slot_width - slot.width) // 2
        output.alpha_composite(slot, (offset_x, 0))

    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output.save(output_path)


if __name__ == "__main__":
    main()
