from __future__ import annotations

from datetime import UTC, datetime
from typing import Literal

from pydantic import Field, model_validator

from .core import SLUG_PATTERN, AssetCategory, StrictModel

ACTION_TEMPLATES: dict[str, int] = {
    "idle": 4,
    "move": 8,
    "attack": 6,
    "hit": 4,
    "death": 8,
}


def utc_now() -> datetime:
    return datetime.now(UTC)


class SequenceTask(StrictModel):
    schema_version: Literal[1] = 1
    asset_id: str = Field(pattern=SLUG_PATTERN)
    category: AssetCategory
    name: str = Field(min_length=1, max_length=120)
    action: str = Field(min_length=1, max_length=80)
    frame_count: int = Field(ge=1, le=32)
    rows: int = Field(ge=1, le=8)
    columns: int = Field(ge=1, le=8)
    frame_width: int = Field(gt=0, le=1024)
    frame_height: int = Field(gt=0, le=1024)
    generation_frame_width: int | None = Field(default=None, gt=0, le=3840)
    generation_frame_height: int | None = Field(default=None, gt=0, le=3840)
    preview_fps: float = Field(gt=0, le=60)
    loop: bool = True
    baseline: Literal["bottom_center", "center", "custom"] = "bottom_center"
    base_frame_workspace_relative_path: str | None = Field(
        default=None,
        min_length=1,
        max_length=1000,
    )
    lock_first_frame: bool = False
    pivot_x: float = Field(default=0.5, ge=0, le=1)
    pivot_y: float = Field(default=0.5, ge=0, le=1)
    blend_mode_hint: Literal["alpha", "additive"] = "alpha"
    max_center_drift_px: float | None = Field(default=None, ge=0)
    max_size_drift_ratio: float | None = Field(default=None, ge=0)
    max_baseline_drift_px: float | None = Field(default=None, ge=0)

    @model_validator(mode="after")
    def validate_sequence_task(self) -> SequenceTask:
        if self.category not in {AssetCategory.ANIMATION, AssetCategory.EFFECT}:
            raise ValueError("sequence task category must be animation or effect")
        if self.rows * self.columns < self.frame_count:
            raise ValueError("sequence grid capacity must cover every frame")
        if (self.generation_frame_width is None) != (
            self.generation_frame_height is None
        ):
            raise ValueError(
                "generation frame width and height must be provided together"
            )
        generation_canvas_width = (
            self.columns * self.resolved_generation_frame_width
        )
        generation_canvas_height = self.rows * self.resolved_generation_frame_height
        if generation_canvas_width > 3840 or generation_canvas_height > 3840:
            raise ValueError(
                "sequence generation canvas exceeds the 3840 pixel edge limit"
            )
        if generation_canvas_width * generation_canvas_height > 8_294_400:
            raise ValueError(
                "sequence generation canvas exceeds the 8294400 pixel limit"
            )
        if (
            self.category is AssetCategory.ANIMATION
            and self.base_frame_workspace_relative_path is None
        ):
            raise ValueError("character animation requires an approved base frame")
        if self.lock_first_frame and self.base_frame_workspace_relative_path is None:
            raise ValueError("locking the first frame requires a base frame")
        return self

    @property
    def resolved_generation_frame_width(self) -> int:
        return self.generation_frame_width or self.frame_width

    @property
    def resolved_generation_frame_height(self) -> int:
        return self.generation_frame_height or self.frame_height


class SequenceGenerationRequest(StrictModel):
    candidate_count: int = Field(default=1, ge=1, le=4)
    prompt_override: str | None = Field(default=None, min_length=1, max_length=32_000)


class SequenceFrameRecord(StrictModel):
    index: int = Field(ge=0, le=31)
    relative_path: str = Field(min_length=1, max_length=1000)
    alpha_bounds: tuple[int, int, int, int]
    center_x: float
    center_y: float
    subject_width: int = Field(ge=0)
    subject_height: int = Field(ge=0)
    baseline_y: float
    area_ratio: float = Field(ge=0, le=1)
    mean_rgb: tuple[int, int, int]
    brightness: float = Field(ge=0, le=255)


class SequenceDriftReport(StrictModel):
    passed: bool
    max_center_drift_px: float = Field(ge=0)
    max_size_drift_ratio: float = Field(ge=0)
    max_baseline_drift_px: float = Field(ge=0)
    max_area_drift_ratio: float = Field(ge=0)
    max_color_drift: float = Field(ge=0)
    max_brightness_jump: float = Field(ge=0)
    first_last_difference: float = Field(ge=0)
    overflow_frames: list[int] = Field(default_factory=list)
    failed_frames: list[int] = Field(default_factory=list)
    issues: list[str] = Field(default_factory=list)
    blend_mode_hint: Literal["alpha", "additive"] = "alpha"


class SequenceOutput(StrictModel):
    frame_count: int = Field(ge=1, le=32)
    rows: int = Field(ge=1, le=8)
    columns: int = Field(ge=1, le=8)
    frame_width: int = Field(gt=0, le=1024)
    frame_height: int = Field(gt=0, le=1024)
    sprite_sheet_width: int = Field(gt=0, le=8192)
    sprite_sheet_height: int = Field(gt=0, le=8192)
    frame_relative_paths: list[str] = Field(min_length=1, max_length=32)
    sprite_sheet_relative_path: str = Field(min_length=1, max_length=1000)
    gif_relative_path: str = Field(min_length=1, max_length=1000)
    webp_relative_path: str = Field(min_length=1, max_length=1000)
    drift_report_relative_path: str = Field(min_length=1, max_length=1000)
    content_sha256: str = Field(pattern=r"^[a-f0-9]{64}$")
    frames: list[SequenceFrameRecord] = Field(default_factory=list, max_length=32)
    drift_report: SequenceDriftReport | None = None

    @model_validator(mode="after")
    def validate_output_grid(self) -> SequenceOutput:
        if len(self.frame_relative_paths) != self.frame_count:
            raise ValueError("frame path count must match frame count")
        if self.sprite_sheet_width != self.columns * self.frame_width:
            raise ValueError("sprite sheet width must match columns and frame width")
        if self.sprite_sheet_height != self.rows * self.frame_height:
            raise ValueError("sprite sheet height must match rows and frame height")
        if self.rows * self.columns < self.frame_count:
            raise ValueError("sprite sheet grid capacity must cover every frame")
        return self


class SequenceCandidate(StrictModel):
    candidate_id: str = Field(pattern=r"^candidate-[0-3]$")
    index: int = Field(ge=0, le=3)
    raw_strip_relative_path: str = Field(min_length=1, max_length=1000)
    output: SequenceOutput | None = None

    @model_validator(mode="after")
    def validate_candidate_id(self) -> SequenceCandidate:
        if self.candidate_id != f"candidate-{self.index}":
            raise ValueError("sequence candidate id and index must match")
        return self


class SequenceRun(StrictModel):
    schema_version: Literal[1] = 1
    run_id: str = Field(pattern=SLUG_PATTERN)
    project_id: str = Field(pattern=SLUG_PATTERN)
    task: SequenceTask
    status: Literal[
        "draft",
        "reference_ready",
        "generated",
        "processed",
        "exported",
        "failed",
    ] = "draft"
    prompt: str = ""
    reference_grid_relative_path: str | None = Field(default=None, max_length=1000)
    candidates: list[SequenceCandidate] = Field(default_factory=list, max_length=4)
    selected_candidate_id: str | None = Field(default=None, pattern=r"^candidate-[0-3]$")
    created_at: datetime = Field(default_factory=utc_now)
    updated_at: datetime = Field(default_factory=utc_now)

    @model_validator(mode="after")
    def validate_selected_candidate(self) -> SequenceRun:
        if self.selected_candidate_id is not None and self.selected_candidate_id not in {
            candidate.candidate_id for candidate in self.candidates
        }:
            raise ValueError("selected sequence candidate must exist")
        return self


class SequenceSelection(StrictModel):
    candidate_id: str = Field(pattern=r"^candidate-[0-3]$")


class SequenceExportFile(StrictModel):
    kind: Literal["frame", "sprite_sheet", "gif", "webp", "report"]
    filename: str = Field(min_length=1, max_length=200)
    relative_path: str = Field(min_length=1, max_length=1000)
    sha256: str = Field(pattern=r"^[a-f0-9]{64}$")
    file_bytes: int = Field(gt=0)


class SequenceExportResult(StrictModel):
    project_id: str = Field(pattern=SLUG_PATTERN)
    asset_id: str = Field(pattern=SLUG_PATTERN)
    category: AssetCategory
    files: list[SequenceExportFile] = Field(min_length=1)
    drift_report: SequenceDriftReport

    @model_validator(mode="after")
    def require_sequence_category(self) -> SequenceExportResult:
        if self.category not in {AssetCategory.ANIMATION, AssetCategory.EFFECT}:
            raise ValueError("sequence export category must be animation or effect")
        return self
