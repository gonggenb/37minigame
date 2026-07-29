from io import BytesIO

import numpy as np
import pytest
from app.image_processing.alpha import (
    EmptyAlphaError,
    alpha_bounds,
    clean_alpha,
    decode_rgba,
    remove_connected_background,
)
from app.schemas.image_tools import BackgroundRemovalConfig
from PIL import Image, ImageDraw


def _encode(image: Image.Image, format_name: str = "PNG") -> bytes:
    stream = BytesIO()
    image.save(stream, format=format_name)
    return stream.getvalue()


def test_decode_converts_rgb_input_to_rgba() -> None:
    decoded = decode_rgba(_encode(Image.new("RGB", (4, 3), (10, 20, 30))))

    assert decoded.mode == "RGBA"
    assert decoded.getpixel((0, 0)) == (10, 20, 30, 255)


def test_corner_background_removal_preserves_enclosed_matching_color() -> None:
    image = Image.new("RGBA", (9, 9), (250, 248, 240, 255))
    draw = ImageDraw.Draw(image)
    draw.rectangle((2, 2, 6, 6), fill=(30, 40, 30, 255))
    draw.point((4, 4), fill=(250, 248, 240, 255))

    result = remove_connected_background(
        image,
        BackgroundRemovalConfig(mode="corner_flood", color_tolerance=6),
    )

    assert result.getpixel((0, 0))[3] == 0
    assert result.getpixel((4, 4))[3] == 255
    assert result.getpixel((2, 2))[3] == 255


def test_alpha_cleanup_snaps_thresholds_without_destroying_mid_alpha() -> None:
    pixels = np.array(
        [
            [[10, 20, 30, 5], [10, 20, 30, 128], [10, 20, 30, 250]],
        ],
        dtype=np.uint8,
    )

    result = clean_alpha(Image.fromarray(pixels, mode="RGBA"), low=10, high=245)
    alpha = np.asarray(result)[:, :, 3].tolist()

    assert alpha == [[0, 128, 255]]


def test_alpha_bounds_rejects_a_fully_transparent_image() -> None:
    image = Image.new("RGBA", (8, 8), (0, 0, 0, 0))

    with pytest.raises(EmptyAlphaError):
        alpha_bounds(image)
