from io import BytesIO

import numpy as np
from app.image_processing.pipeline import ImageProcessor
from app.schemas.core import AssetCategory, ConstraintProfile
from app.schemas.image_tools import BackgroundRemovalConfig
from PIL import Image, ImageDraw


def _encode(image: Image.Image) -> bytes:
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _profile(**overrides: object) -> ConstraintProfile:
    payload: dict[str, object] = {
        "schema_version": 1,
        "profile_id": "golden-item",
        "category": AssetCategory.ITEM,
        "master_width": 1024,
        "master_height": 1024,
        "output_width": 20,
        "output_height": 20,
        "require_rgba": True,
        "require_transparency": True,
        "crop_mode": "alpha_bounds",
        "padding_ratio": 0.1,
        "occupancy_ratio": 0.5,
        "resize_algorithm": "nearest",
        "pivot_x": 0.5,
        "pivot_y": 0.5,
        "filename_template": "{asset_id}_{variant}.png",
        "max_file_bytes": 8388608,
        "output_sprite_sheet": False,
        "shared_scale": True,
        "lock_first_frame": False,
    }
    payload.update(overrides)
    return ConstraintProfile.model_validate(payload)


def test_alpha_crop_scale_padding_and_center_anchor_are_stable() -> None:
    source = Image.new("RGBA", (10, 10), (0, 0, 0, 0))
    ImageDraw.Draw(source).rectangle((2, 1, 5, 6), fill=(180, 40, 30, 255))

    processed = ImageProcessor.process(
        _encode(source),
        _profile(),
        BackgroundRemovalConfig(mode="preserve"),
    )

    assert (processed.metadata.width, processed.metadata.height) == (20, 20)
    left, top, right, bottom = processed.metadata.alpha_bounds
    assert right - left <= 10
    assert bottom - top == 10
    assert abs(((left + right) / 2) - 10) <= 0.5
    assert abs(((top + bottom) / 2) - 10) <= 0.5
    assert min(left, top, 20 - right, 20 - bottom) >= 2


def test_bottom_center_anchor_uses_the_safe_canvas_baseline() -> None:
    source = Image.new("RGBA", (6, 10), (0, 0, 0, 0))
    ImageDraw.Draw(source).rectangle((1, 1, 4, 8), fill=(30, 80, 150, 255))

    processed = ImageProcessor.process(
        _encode(source),
        _profile(
            profile_id="golden-character",
            category=AssetCategory.CHARACTER,
            occupancy_ratio=0.8,
            pivot_y=1.0,
        ),
        BackgroundRemovalConfig(),
    )

    left, _, right, bottom = processed.metadata.alpha_bounds
    assert abs(((left + right) / 2) - 10) <= 0.5
    assert bottom == 18


def test_scene_crop_none_fills_the_entire_target_canvas() -> None:
    source = Image.new("RGB", (8, 4), (40, 80, 40))
    processed = ImageProcessor.process(
        _encode(source),
        _profile(
            profile_id="golden-scene",
            category=AssetCategory.SCENE,
            output_width=12,
            output_height=12,
            require_rgba=False,
            require_transparency=False,
            crop_mode="none",
            padding_ratio=0.0,
            occupancy_ratio=1.0,
            resize_algorithm="lanczos",
        ),
        BackgroundRemovalConfig(),
    )

    with Image.open(BytesIO(processed.content)) as result:
        assert result.size == (12, 12)
        assert result.getbbox() == (0, 0, 12, 12)


def test_nearest_golden_pixels_and_png_bytes_are_deterministic() -> None:
    source_pixels = np.array(
        [
            [[255, 0, 0, 255], [0, 255, 0, 255]],
            [[0, 0, 255, 255], [255, 255, 255, 255]],
        ],
        dtype=np.uint8,
    )
    source = Image.fromarray(source_pixels, mode="RGBA")
    profile = _profile(
        profile_id="golden-fixed",
        output_width=4,
        output_height=4,
        require_transparency=False,
        crop_mode="fixed",
        padding_ratio=0.0,
        occupancy_ratio=1.0,
        resize_algorithm="nearest",
    )

    first = ImageProcessor.process(_encode(source), profile, BackgroundRemovalConfig())
    second = ImageProcessor.process(_encode(source), profile, BackgroundRemovalConfig())
    expected = np.repeat(np.repeat(source_pixels, 2, axis=0), 2, axis=1)
    with Image.open(BytesIO(first.content)) as result:
        assert np.array_equal(np.asarray(result.convert("RGBA")), expected)

    assert first.content == second.content
    assert first.metadata.sha256 == second.metadata.sha256
