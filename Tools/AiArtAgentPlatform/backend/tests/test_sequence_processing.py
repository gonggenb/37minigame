from io import BytesIO

import numpy as np
import pytest
from app.sequence_processing.grid import create_reference_grid, slice_grid
from app.sequence_processing.normalize import normalize_frames_shared_scale
from PIL import Image


def _png(image: Image.Image) -> bytes:
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _bbox(image: Image.Image) -> tuple[int, int, int, int]:
    bounds = image.getchannel("A").getbbox()
    assert bounds is not None
    return bounds


def test_reference_grid_places_the_base_frame_only_in_the_first_slot() -> None:
    base = Image.new("RGBA", (2, 2), (200, 30, 20, 255))

    grid = create_reference_grid(
        _png(base),
        rows=1,
        columns=2,
        frame_width=4,
        frame_height=4,
        baseline="bottom_center",
    )

    assert grid.size == (8, 4)
    assert grid.crop((0, 0, 4, 4)).getchannel("A").getbbox() is not None
    assert grid.crop((4, 0, 8, 4)).getchannel("A").getbbox() is None


def test_grid_slicing_is_row_major_and_rejects_non_divisible_images() -> None:
    grid = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
    colors = [(255, 0, 0, 255), (0, 255, 0, 255), (0, 0, 255, 255), (255, 255, 0, 255)]
    for index, color in enumerate(colors):
        x = (index % 2) * 2
        y = (index // 2) * 2
        grid.paste(Image.new("RGBA", (2, 2), color), (x, y))

    frames = slice_grid(_png(grid), rows=2, columns=2, frame_count=4)

    assert [frame.getpixel((0, 0)) for frame in frames] == colors

    with pytest.raises(ValueError):
        slice_grid(_png(Image.new("RGBA", (5, 4))), rows=2, columns=2, frame_count=4)


def test_grid_slicing_rejects_a_canvas_that_does_not_match_generation_cells() -> None:
    strip = _png(Image.new("RGBA", (65, 64), (0, 0, 0, 0)))

    with pytest.raises(ValueError, match="expected generation canvas"):
        slice_grid(
            strip,
            rows=2,
            columns=2,
            frame_count=4,
            expected_frame_width=32,
            expected_frame_height=32,
        )


def test_sequence_uses_one_shared_scale_and_bottom_center_baseline() -> None:
    small = Image.new("RGBA", (2, 2), (150, 50, 30, 255))
    large = Image.new("RGBA", (4, 4), (50, 100, 160, 255))

    normalized, scale = normalize_frames_shared_scale(
        [small, large],
        frame_width=8,
        frame_height=8,
        occupancy_ratio=1,
        padding_ratio=0,
        baseline="bottom_center",
        pivot_x=0.5,
        pivot_y=1,
        resize_algorithm="nearest",
    )

    assert scale == 2
    assert _bbox(normalized[0]) == (2, 4, 6, 8)
    assert _bbox(normalized[1]) == (0, 0, 8, 8)
    assert np.array_equal(np.asarray(normalized[0]), np.asarray(normalized[0].copy()))
