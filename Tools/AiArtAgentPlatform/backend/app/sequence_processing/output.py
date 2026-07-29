from __future__ import annotations

from io import BytesIO
from typing import cast

from PIL import Image


def encode_png(image: Image.Image) -> bytes:
    stream = BytesIO()
    image.convert("RGBA").save(
        stream,
        format="PNG",
        optimize=False,
        compress_level=6,
    )
    return stream.getvalue()


def build_sprite_sheet(
    frames: list[Image.Image],
    *,
    rows: int,
    columns: int,
    frame_width: int,
    frame_height: int,
) -> Image.Image:
    if not frames:
        raise ValueError("at least one frame is required")
    if len(frames) > rows * columns:
        raise ValueError("frame count exceeds sprite sheet capacity")
    sheet = Image.new(
        "RGBA",
        (columns * frame_width, rows * frame_height),
        (0, 0, 0, 0),
    )
    for index, frame in enumerate(frames):
        if frame.size != (frame_width, frame_height):
            raise ValueError("every frame must match the target frame dimensions")
        left = (index % columns) * frame_width
        top = (index // columns) * frame_height
        sheet.alpha_composite(frame.convert("RGBA"), (left, top))
    return sheet


def encode_gif(frames: list[Image.Image], *, fps: float, loop: bool) -> bytes:
    if not frames:
        raise ValueError("at least one frame is required")
    duration = max(1, round(1000 / fps))
    stream = BytesIO()
    first, *remaining = [frame.convert("RGBA") for frame in frames]
    if loop:
        first.save(
            stream,
            format="GIF",
            save_all=True,
            append_images=remaining,
            duration=duration,
            loop=0,
            disposal=2,
            optimize=False,
        )
    else:
        first.save(
            stream,
            format="GIF",
            save_all=True,
            append_images=remaining,
            duration=duration,
            disposal=2,
            optimize=False,
        )
    return stream.getvalue()


def encode_webp(frames: list[Image.Image], *, fps: float, loop: bool) -> bytes:
    if not frames:
        raise ValueError("at least one frame is required")
    duration = max(1, round(1000 / fps))
    stream = BytesIO()
    webp_frames: list[Image.Image] = []
    for frame in frames:
        encoded_frame = frame.convert("RGBA").copy()
        alpha = encoded_frame.getchannel("A")
        if alpha.getextrema() == (0, 255):
            bounds = alpha.getbbox()
            if bounds is not None:
                x, y = bounds[0], bounds[1]
                red, green, blue, _ = cast(
                    tuple[int, int, int, int],
                    encoded_frame.getpixel((x, y)),
                )
                encoded_frame.putpixel((x, y), (red, green, blue, 254))
        webp_frames.append(encoded_frame)
    first, *remaining = webp_frames
    first.save(
        stream,
        format="WEBP",
        save_all=True,
        append_images=remaining,
        duration=duration,
        loop=0 if loop else 1,
        lossless=True,
        method=6,
        exact=True,
    )
    return stream.getvalue()
