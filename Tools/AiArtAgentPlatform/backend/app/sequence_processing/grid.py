from __future__ import annotations

from io import BytesIO

from PIL import Image

from .normalize import normalize_frames_shared_scale


def _decode_rgba(image_bytes: bytes) -> Image.Image:
    if not image_bytes:
        raise ValueError("image bytes must not be empty")
    with Image.open(BytesIO(image_bytes)) as source:
        return source.convert("RGBA")


def create_reference_grid(
    base_png: bytes,
    *,
    rows: int,
    columns: int,
    frame_width: int,
    frame_height: int,
    baseline: str,
) -> Image.Image:
    """Create a transparent grid with the approved base frame in slot zero."""

    if rows <= 0 or columns <= 0:
        raise ValueError("grid rows and columns must be positive")
    if frame_width <= 0 or frame_height <= 0:
        raise ValueError("frame dimensions must be positive")

    base_frame = _decode_rgba(base_png)
    normalized, _ = normalize_frames_shared_scale(
        [base_frame],
        frame_width=frame_width,
        frame_height=frame_height,
        occupancy_ratio=1,
        padding_ratio=0,
        baseline=baseline,
        pivot_x=0.5,
        pivot_y=1 if baseline == "bottom_center" else 0.5,
        resize_algorithm="nearest",
    )
    grid = Image.new(
        "RGBA",
        (columns * frame_width, rows * frame_height),
        (0, 0, 0, 0),
    )
    grid.alpha_composite(normalized[0], (0, 0))
    return grid


def slice_grid(
    strip_png: bytes,
    *,
    rows: int,
    columns: int,
    frame_count: int,
    expected_frame_width: int | None = None,
    expected_frame_height: int | None = None,
) -> list[Image.Image]:
    """Split a generated strip into fixed row-major RGBA frames."""

    if rows <= 0 or columns <= 0:
        raise ValueError("grid rows and columns must be positive")
    if frame_count <= 0:
        raise ValueError("frame count must be positive")
    if frame_count > rows * columns:
        raise ValueError("frame count exceeds grid capacity")
    if (expected_frame_width is None) != (expected_frame_height is None):
        raise ValueError("expected frame width and height must be provided together")

    strip = _decode_rgba(strip_png)
    if expected_frame_width is not None and expected_frame_height is not None:
        expected_size = (
            columns * expected_frame_width,
            rows * expected_frame_height,
        )
        if strip.size != expected_size:
            raise ValueError(
                f"expected generation canvas {expected_size[0]}x{expected_size[1]}, "
                f"received {strip.width}x{strip.height}"
            )
    if strip.width % columns != 0 or strip.height % rows != 0:
        raise ValueError("image dimensions must be divisible by the grid")

    frame_width = strip.width // columns
    frame_height = strip.height // rows
    frames: list[Image.Image] = []
    for index in range(frame_count):
        column = index % columns
        row = index // columns
        left = column * frame_width
        top = row * frame_height
        frames.append(
            strip.crop((left, top, left + frame_width, top + frame_height)).convert(
                "RGBA"
            )
        )
    return frames
