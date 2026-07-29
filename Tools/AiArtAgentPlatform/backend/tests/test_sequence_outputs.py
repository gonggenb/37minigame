from io import BytesIO

import numpy as np
import pytest
from app.schemas.core import AssetCategory, ConstraintProfile
from app.schemas.sequence import SequenceTask
from app.sequence_processing.metrics import analyze_sequence
from app.sequence_processing.output import encode_gif, encode_webp
from app.sequence_processing.pipeline import SequenceProcessor
from PIL import Image


def _png(image: Image.Image) -> bytes:
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _task(**overrides: object) -> SequenceTask:
    payload: dict[str, object] = {
        "asset_id": "hero-idle",
        "category": AssetCategory.ANIMATION,
        "name": "少侠待机",
        "action": "idle",
        "frame_count": 4,
        "rows": 2,
        "columns": 2,
        "frame_width": 8,
        "frame_height": 8,
        "preview_fps": 10,
        "loop": True,
        "baseline": "bottom_center",
        "base_frame_workspace_relative_path": "assets/hero/base.png",
        "lock_first_frame": True,
        "pivot_x": 0.5,
        "pivot_y": 1,
    }
    payload.update(overrides)
    return SequenceTask.model_validate(payload)


def _profile(**overrides: object) -> ConstraintProfile:
    payload: dict[str, object] = {
        "profile_id": "sequence-animation",
        "category": AssetCategory.ANIMATION,
        "master_width": 16,
        "master_height": 16,
        "output_width": 8,
        "output_height": 8,
        "require_rgba": True,
        "require_transparency": True,
        "crop_mode": "alpha_bounds",
        "padding_ratio": 0,
        "occupancy_ratio": 0.5,
        "resize_algorithm": "nearest",
        "pivot_x": 0.5,
        "pivot_y": 1,
        "filename_template": "{asset_id}_{variant}.png",
        "output_sprite_sheet": True,
        "frame_count": 4,
        "rows": 2,
        "columns": 2,
        "frame_width": 8,
        "frame_height": 8,
        "preview_fps": 10,
        "loop": True,
        "baseline": "bottom_center",
        "shared_scale": True,
        "lock_first_frame": True,
        "max_center_drift_px": 2,
        "max_size_drift_ratio": 0.5,
    }
    payload.update(overrides)
    return ConstraintProfile.model_validate(payload)


def _raw_grid() -> Image.Image:
    grid = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    colors = [
        (20, 30, 220, 255),
        (30, 180, 70, 255),
        (220, 150, 30, 255),
        (150, 40, 180, 255),
    ]
    for index, color in enumerate(colors):
        frame = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
        frame.paste(Image.new("RGBA", (2, 2), color), (index % 2, index // 2))
        grid.alpha_composite(frame, ((index % 2) * 4, (index // 2) * 4))
    return grid


def test_processor_locks_first_frame_and_encodes_stable_sequence_outputs() -> None:
    base = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
    base.paste(Image.new("RGBA", (2, 2), (210, 35, 25, 255)), (1, 2))
    processor = SequenceProcessor()

    first = processor.process(
        strip_png=_png(_raw_grid()),
        task=_task(),
        profile=_profile(),
        base_frame_png=_png(base),
    )
    second = processor.process(
        strip_png=_png(_raw_grid()),
        task=_task(),
        profile=_profile(),
        base_frame_png=_png(base),
    )

    assert len(first.frame_pngs) == 4
    assert first.frame_pngs == second.frame_pngs
    assert first.sprite_sheet_png == second.sprite_sheet_png
    assert first.gif_preview == second.gif_preview
    assert first.webp_preview == second.webp_preview
    assert first.content_sha256 == second.content_sha256

    with Image.open(BytesIO(first.frame_pngs[0])) as locked:
        locked_rgba = np.asarray(locked.convert("RGBA"))
        opaque_pixels = locked_rgba[:, :, :3][locked_rgba[:, :, 3] > 0]
        assert opaque_pixels.size > 0
        assert {tuple(pixel) for pixel in opaque_pixels.tolist()} == {(210, 35, 25)}

    with Image.open(BytesIO(first.sprite_sheet_png)) as sheet:
        rgba_sheet = sheet.convert("RGBA")
        assert rgba_sheet.size == (16, 16)
        for index, frame_png in enumerate(first.frame_pngs):
            left = (index % 2) * 8
            top = (index // 2) * 8
            with Image.open(BytesIO(frame_png)) as frame:
                assert np.array_equal(
                    np.asarray(rgba_sheet.crop((left, top, left + 8, top + 8))),
                    np.asarray(frame.convert("RGBA")),
                )

    for preview in (first.gif_preview, first.webp_preview):
        with Image.open(BytesIO(preview)) as animation:
            assert animation.n_frames == 4
            animation.seek(0)
            animation.load()
            assert animation.info["duration"] == 100
            assert animation.info["loop"] == 0
            assert animation.convert("RGBA").getpixel((0, 0))[3] == 0


def test_metrics_report_frame_geometry_color_and_drift_from_the_first_frame() -> None:
    frames: list[Image.Image] = []
    specs = [
        ((2, 3, 4, 7), (200, 20, 10, 255)),
        ((3, 2, 5, 7), (20, 180, 30, 255)),
        ((2, 3, 5, 8), (30, 40, 210, 255)),
    ]
    for bounds, color in specs:
        frame = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
        subject_size = (bounds[2] - bounds[0], bounds[3] - bounds[1])
        frame.paste(Image.new("RGBA", subject_size, color), bounds[:2])
        frames.append(frame)

    task = _task(frame_count=3, rows=1, columns=3, lock_first_frame=False)
    profile = _profile(
        frame_count=3,
        rows=1,
        columns=3,
        master_width=24,
        master_height=8,
        max_center_drift_px=1,
        max_size_drift_ratio=0.4,
        lock_first_frame=False,
    )

    records, report = analyze_sequence(frames, task=task, profile=profile)

    assert [record.alpha_bounds for record in records] == [item[0] for item in specs]
    assert records[0].center_x == 3
    assert records[0].center_y == 5
    assert records[0].subject_width == 2
    assert records[0].subject_height == 4
    assert records[0].baseline_y == 7
    assert records[0].area_ratio == pytest.approx(8 / 64)
    assert records[0].mean_rgb == (200, 20, 10)
    assert records[0].brightness == pytest.approx(57.5, abs=0.1)
    assert report.max_center_drift_px == pytest.approx(5**0.5 / 2)
    assert report.max_size_drift_ratio == pytest.approx(0.5)
    assert report.max_baseline_drift_px == 1
    assert report.max_area_drift_ratio == pytest.approx(0.875)
    assert report.max_color_drift > 0
    assert report.max_brightness_jump > 0
    assert report.first_last_difference > 0
    assert report.failed_frames == [1, 2]
    assert not report.passed


def test_action_level_drift_limits_override_strict_profile_limits() -> None:
    first = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    first.paste(Image.new("RGBA", (4, 4), (80, 95, 70, 255)), (6, 6))
    second = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    second.paste(Image.new("RGBA", (6, 4), (80, 95, 70, 255)), (6, 9))
    task = _task(
        action="death",
        frame_count=2,
        rows=1,
        columns=2,
        frame_width=16,
        frame_height=16,
        loop=False,
        max_center_drift_px=4,
        max_size_drift_ratio=0.6,
        max_baseline_drift_px=3,
    )
    profile = _profile(
        frame_count=2,
        rows=1,
        columns=2,
        output_width=16,
        output_height=16,
        master_width=32,
        master_height=16,
        frame_width=16,
        frame_height=16,
        max_center_drift_px=2,
        max_size_drift_ratio=0.2,
    )

    _, report = analyze_sequence([first, second], task=task, profile=profile)

    assert report.max_center_drift_px == pytest.approx(10**0.5)
    assert report.max_size_drift_ratio == pytest.approx(0.5)
    assert report.max_baseline_drift_px == 3
    assert report.failed_frames == []
    assert report.passed


def test_sequence_without_action_overrides_keeps_strict_global_limits() -> None:
    first = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    first.paste(Image.new("RGBA", (4, 4), (80, 95, 70, 255)), (6, 6))
    second = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    second.paste(Image.new("RGBA", (6, 4), (80, 95, 70, 255)), (6, 9))
    task = _task(
        action="move",
        frame_count=2,
        rows=1,
        columns=2,
        frame_width=16,
        frame_height=16,
    )
    profile = _profile(
        frame_count=2,
        rows=1,
        columns=2,
        output_width=16,
        output_height=16,
        master_width=32,
        master_height=16,
        frame_width=16,
        frame_height=16,
        max_center_drift_px=2,
        max_size_drift_ratio=0.2,
    )

    _, report = analyze_sequence([first, second], task=task, profile=profile)

    assert report.failed_frames == [1]
    assert not report.passed


def test_effect_metrics_report_edge_overflow_and_preserve_blend_mode_hint() -> None:
    frame = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    frame.paste(Image.new("RGBA", (3, 3), (245, 220, 160, 255)), (5, 5))
    task = SequenceTask(
        asset_id="sword-flash",
        category=AssetCategory.EFFECT,
        name="剑光",
        action="slash",
        frame_count=1,
        rows=1,
        columns=1,
        frame_width=8,
        frame_height=8,
        preview_fps=20,
        loop=False,
        baseline="center",
        blend_mode_hint="additive",
    )
    profile = _profile(
        profile_id="sequence-effect",
        category=AssetCategory.EFFECT,
        master_width=8,
        master_height=8,
        frame_count=1,
        rows=1,
        columns=1,
        preview_fps=20,
        loop=False,
        baseline="center",
        pivot_y=0.5,
        lock_first_frame=False,
        max_center_drift_px=None,
        max_size_drift_ratio=None,
    )

    _, report = analyze_sequence([frame], task=task, profile=profile)

    assert report.overflow_frames == [0]
    assert report.blend_mode_hint == "additive"
    assert not report.passed


def test_non_looping_previews_do_not_repeat_after_the_last_frame() -> None:
    frames = [
        Image.new("RGBA", (4, 4), (200, 20, 10, 255)),
        Image.new("RGBA", (4, 4), (20, 80, 200, 255)),
    ]

    with Image.open(BytesIO(encode_gif(frames, fps=12, loop=False))) as gif:
        assert "loop" not in gif.info
    with Image.open(BytesIO(encode_webp(frames, fps=12, loop=False))) as webp:
        assert webp.info["loop"] == 1


def test_processor_removes_connected_opaque_background_per_grid_slot() -> None:
    strip = Image.new("RGBA", (8, 4), (246, 241, 222, 255))
    strip.paste(Image.new("RGBA", (2, 2), (160, 35, 25, 255)), (1, 1))
    strip.paste(Image.new("RGBA", (2, 2), (40, 100, 150, 255)), (5, 1))
    task = _task(
        frame_count=2,
        rows=1,
        columns=2,
        lock_first_frame=False,
    )
    profile = _profile(
        frame_count=2,
        rows=1,
        columns=2,
        master_width=8,
        master_height=4,
        lock_first_frame=False,
    )

    result = SequenceProcessor.process(
        strip_png=_png(strip),
        task=task,
        profile=profile,
    )

    for frame_png in result.frame_pngs:
        with Image.open(BytesIO(frame_png)) as frame:
            rgba = np.asarray(frame.convert("RGBA"))
            assert rgba[0, 0, 3] == 0
            opaque_colors = rgba[:, :, :3][rgba[:, :, 3] > 0]
            assert (246, 241, 222) not in {
                tuple(pixel) for pixel in opaque_colors.tolist()
            }
