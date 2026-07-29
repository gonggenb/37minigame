from enum import StrEnum
from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

SLUG_PATTERN = r"^[a-z0-9]+(?:-[a-z0-9]+)*$"


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid")


class AssetCategory(StrEnum):
    CHARACTER = "character"
    SCENE = "scene"
    ITEM = "item"
    ANIMATION = "animation"
    EFFECT = "effect"
    UI = "ui"


class JobStatus(StrEnum):
    DRAFT = "draft"
    PLANNING = "planning"
    PLANNED = "planned"
    GENERATING = "generating"
    PROCESSING = "processing"
    REVIEWING = "reviewing"
    READY = "ready"
    NEEDS_INPUT = "needs_input"
    EXPORTING = "exporting"
    EXPORTED = "exported"
    FAILED = "failed"
    CANCELLED = "cancelled"
    INTERRUPTED = "interrupted"


class ModelSettings(StrictModel):
    planner_model: str = "gpt-5.6"
    review_model: str = "gpt-5.6"
    image_model: str = "gpt-image-2"


class GenerationSettings(StrictModel):
    candidate_count: int = Field(default=4, ge=1, le=4)
    automatic_retry_count: int = Field(default=2, ge=0, le=2)
    image_quality: Literal["low", "medium", "high", "auto"] = "high"
    transparency_mode: Literal["postprocess", "opaque"] = "postprocess"


class ReviewSettings(StrictModel):
    enabled: bool = True
    minimum_style_score: int = Field(default=75, ge=0, le=100)
    hard_constraints_required: bool = True


class ProjectConfig(StrictModel):
    schema_version: Literal[1] = 1
    project_id: str = Field(pattern=SLUG_PATTERN)
    display_name: str = Field(min_length=1, max_length=120)
    visual_type: str = "wuxia-ink-chibi-topdown-2_5d"
    language: Literal["zh-CN", "en-US"] = "zh-CN"
    models: ModelSettings = Field(default_factory=ModelSettings)
    generation: GenerationSettings = Field(default_factory=GenerationSettings)
    review: ReviewSettings = Field(default_factory=ReviewSettings)


class AssetTask(StrictModel):
    asset_id: str = Field(pattern=SLUG_PATTERN)
    category: AssetCategory
    name: str = Field(min_length=1, max_length=120)
    brief: str = Field(min_length=1, max_length=4000)
    usage: str = Field(min_length=1, max_length=120)
    style_pack: str = Field(min_length=1, max_length=160)
    reference_ids: list[str] = Field(default_factory=list, max_length=4)
    constraint_profile: str = Field(min_length=1, max_length=120)
    constraint_overrides: dict[str, Any] = Field(default_factory=dict)
    candidate_count: int = Field(default=4, ge=1, le=4)
    output_mode: str = Field(min_length=1, max_length=80)


class ConstraintProfile(StrictModel):
    schema_version: Literal[1] = 1
    profile_id: str = Field(pattern=SLUG_PATTERN)
    category: AssetCategory
    master_width: int = Field(gt=0, le=3840)
    master_height: int = Field(gt=0, le=3840)
    output_width: int = Field(gt=0, le=3840)
    output_height: int = Field(gt=0, le=3840)
    require_rgba: bool = True
    require_transparency: bool = True
    crop_mode: Literal["alpha_bounds", "fixed", "none"] = "alpha_bounds"
    padding_ratio: float = Field(default=0.125, ge=0, lt=0.5)
    occupancy_ratio: float = Field(default=0.75, gt=0, le=1)
    resize_algorithm: Literal["lanczos", "nearest"] = "lanczos"
    pivot_x: float = Field(default=0.5, ge=0, le=1)
    pivot_y: float = Field(default=0.5, ge=0, le=1)
    filename_template: str = "{asset_id}_{variant}.png"
    max_file_bytes: int = Field(default=8_388_608, gt=0)
    output_sprite_sheet: bool = False
    frame_count: int | None = Field(default=None, gt=0)
    rows: int | None = Field(default=None, gt=0)
    columns: int | None = Field(default=None, gt=0)
    frame_width: int | None = Field(default=None, gt=0)
    frame_height: int | None = Field(default=None, gt=0)
    preview_fps: float | None = Field(default=None, gt=0)
    loop: bool | None = None
    baseline: Literal["bottom_center", "center", "custom"] | None = None
    shared_scale: bool = True
    lock_first_frame: bool = False
    max_center_drift_px: float | None = Field(default=None, ge=0)
    max_size_drift_ratio: float | None = Field(default=None, ge=0)


class ImageOutputSpec(StrictModel):
    width: int = Field(gt=0, le=3840)
    height: int = Field(gt=0, le=3840)
    format: Literal["png"] = "png"
    transparent_required: bool = True


class GenerationPlan(StrictModel):
    asset_type: AssetCategory
    usage: str
    selected_reference_ids: list[str] = Field(max_length=4)
    composition: str
    camera: str
    lighting: str
    identity_constraints: list[str]
    prompt: str = Field(min_length=1)
    negative_constraints: list[str]
    output_spec: ImageOutputSpec
    postprocess_steps: list[str]
    quality_checks: list[str]
    repair_strategy: list[str] = Field(min_length=1)


class HardConstraintCheck(StrictModel):
    name: str
    passed: bool
    message: str = ""


class HardConstraintReport(StrictModel):
    passed: bool
    checks: list[HardConstraintCheck]


class StyleReview(StrictModel):
    score: int = Field(ge=0, le=100)
    identity_score: int = Field(ge=0, le=100)
    palette_score: int = Field(ge=0, le=100)
    line_style_score: int = Field(ge=0, le=100)
    composition_score: int = Field(ge=0, le=100)
    issues: list[str]
    repair_instruction: str = ""
    summary: str = ""
    strengths: list[str] = Field(default_factory=list)
    findings: list["ReviewFinding"] = Field(default_factory=list)
    risk_notes: list[str] = Field(default_factory=list)


ReviewDimension = Literal[
    "hard_constraint",
    "identity",
    "palette",
    "line_style",
    "composition",
    "animation",
]


class ReviewFinding(StrictModel):
    dimension: ReviewDimension
    severity: Literal["info", "warning", "error"]
    summary: str = Field(min_length=1, max_length=400)
    evidence: str = Field(min_length=1, max_length=1200)
    repair_hint: str = Field(default="", max_length=1200)
    actionable: bool = True


class AnimationReview(StrictModel):
    center_drift_px: float = Field(ge=0)
    size_drift_ratio: float = Field(ge=0)
    baseline_drift_px: float = Field(ge=0)
    issues: list[str] = Field(default_factory=list)


class QualityReport(StrictModel):
    hard_constraints: HardConstraintReport
    style_review: StyleReview
    animation_review: AnimationReview | None = None
    export_allowed: bool
    review_basis: list[str] = Field(default_factory=list)
    decision: Literal["pass", "retry", "manual_review"] = "manual_review"

    @model_validator(mode="after")
    def prevent_invalid_export(self) -> "QualityReport":
        if self.export_allowed and not self.hard_constraints.passed:
            raise ValueError("export cannot be allowed when hard constraints fail")
        return self
