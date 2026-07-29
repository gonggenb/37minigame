import pytest
from app.schemas.core import AssetCategory
from app.schemas.sequence import (
    ACTION_TEMPLATES,
    SequenceOutput,
    SequenceTask,
)
from pydantic import ValidationError


def _animation_task(**overrides) -> SequenceTask:
    payload = {
        "asset_id": "hero-idle",
        "category": AssetCategory.ANIMATION,
        "name": "少侠待机",
        "action": "idle",
        "frame_count": 4,
        "rows": 1,
        "columns": 4,
        "frame_width": 256,
        "frame_height": 256,
        "preview_fps": 8,
        "loop": True,
        "baseline": "bottom_center",
        "base_frame_workspace_relative_path": (
            "assets/character/hero/selected/hero_default.png"
        ),
        "lock_first_frame": True,
    }
    payload.update(overrides)
    return SequenceTask.model_validate(payload)


def test_action_templates_match_the_project_defaults() -> None:
    assert ACTION_TEMPLATES == {
        "idle": 4,
        "move": 8,
        "attack": 6,
        "hit": 4,
        "death": 8,
    }


def test_animation_requires_a_workspace_base_frame_but_effect_does_not() -> None:
    with pytest.raises(ValidationError):
        _animation_task(base_frame_workspace_relative_path=None)

    effect = SequenceTask(
        asset_id="sword-flash",
        category=AssetCategory.EFFECT,
        name="剑光特效",
        action="slash",
        frame_count=6,
        rows=2,
        columns=3,
        frame_width=256,
        frame_height=256,
        preview_fps=12,
        loop=False,
        baseline="center",
    )
    assert effect.base_frame_workspace_relative_path is None


def test_sequence_grid_capacity_and_frame_range_are_validated() -> None:
    with pytest.raises(ValidationError):
        _animation_task(frame_count=8, rows=1, columns=4)

    with pytest.raises(ValidationError):
        _animation_task(frame_count=33, rows=4, columns=9)


def test_sequence_output_dimensions_must_match_the_grid() -> None:
    output = SequenceOutput(
        frame_count=4,
        rows=1,
        columns=4,
        frame_width=256,
        frame_height=256,
        sprite_sheet_width=1024,
        sprite_sheet_height=256,
        frame_relative_paths=[f"frames/frame-{index:03d}.png" for index in range(4)],
        sprite_sheet_relative_path="sprite-sheet.png",
        gif_relative_path="preview.gif",
        webp_relative_path="preview.webp",
        drift_report_relative_path="drift-report.json",
        content_sha256="a" * 64,
    )
    assert output.sprite_sheet_width == 1024

    with pytest.raises(ValidationError):
        output.model_copy(update={"sprite_sheet_width": 1000}).model_validate(
            output.model_copy(update={"sprite_sheet_width": 1000}).model_dump()
        )


def test_sequence_generation_frame_size_is_distinct_from_final_output() -> None:
    task = _animation_task(
        generation_frame_width=512,
        generation_frame_height=512,
        frame_width=256,
        frame_height=256,
    )

    assert task.resolved_generation_frame_width == 512
    assert task.resolved_generation_frame_height == 512
    assert task.frame_width == 256
    assert task.frame_height == 256


def test_legacy_sequence_task_falls_back_to_final_frame_size() -> None:
    task = _animation_task(frame_width=256, frame_height=128)

    assert task.resolved_generation_frame_width == 256
    assert task.resolved_generation_frame_height == 128


@pytest.mark.parametrize(
    "override",
    [
        {"generation_frame_width": 512},
        {"generation_frame_height": 512},
    ],
)
def test_sequence_generation_frame_dimensions_must_be_provided_together(
    override: dict[str, int],
) -> None:
    with pytest.raises(ValidationError, match="provided together"):
        _animation_task(**override)


def test_sequence_rejects_generation_canvas_that_would_allocate_above_model_maximum() -> None:
    with pytest.raises(ValidationError, match="generation canvas exceeds"):
        _animation_task(
            rows=2,
            columns=8,
            generation_frame_width=512,
            generation_frame_height=512,
        )
