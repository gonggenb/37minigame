from __future__ import annotations

from datetime import UTC, datetime
from typing import Literal

from pydantic import Field, model_validator

from .core import (
    SLUG_PATTERN,
    AssetCategory,
    AssetTask,
    GenerationPlan,
    HardConstraintReport,
    QualityReport,
    ReviewDimension,
    StrictModel,
)
from .image_tools import ExportRecord, ProcessedImageMetadata

StaticAssetCategory = Literal["item", "ui", "character", "scene"]
ProductionRunStatus = Literal[
    "planned",
    "generated",
    "selected",
    "reviewed",
    "exported",
    "failed",
]


def utc_now() -> datetime:
    return datetime.now(UTC)


class StaticAssetRecord(StrictModel):
    schema_version: Literal[1] = 1
    task: AssetTask
    created_at: datetime = Field(default_factory=utc_now)
    updated_at: datetime = Field(default_factory=utc_now)

    @model_validator(mode="after")
    def require_static_category(self) -> StaticAssetRecord:
        if self.task.category not in {
            AssetCategory.ITEM,
            AssetCategory.UI,
            AssetCategory.CHARACTER,
            AssetCategory.SCENE,
        }:
            raise ValueError("stage 6 only supports static asset categories")
        return self


class ProductionCandidate(StrictModel):
    candidate_id: str = Field(pattern=r"^candidate-[0-3]$")
    index: int = Field(ge=0, le=3)
    raw_relative_path: str = Field(min_length=1, max_length=1000)
    processed_relative_path: str = Field(min_length=1, max_length=1000)
    metadata: ProcessedImageMetadata
    hard_constraints: HardConstraintReport
    revised_prompt: str | None = None
    quality_report: QualityReport | None = None
    comparison_relative_path: str | None = Field(
        default=None,
        min_length=1,
        max_length=1000,
    )

    @model_validator(mode="after")
    def match_candidate_id_and_index(self) -> ProductionCandidate:
        if self.candidate_id != f"candidate-{self.index}":
            raise ValueError("candidate id and index must match")
        return self


class ProductionRun(StrictModel):
    schema_version: Literal[1] = 1
    run_id: str = Field(pattern=SLUG_PATTERN)
    project_id: str = Field(pattern=SLUG_PATTERN)
    task: AssetTask
    status: ProductionRunStatus
    plan: GenerationPlan | None = None
    prompt: str = ""
    candidates: list[ProductionCandidate] = Field(default_factory=list, max_length=4)
    selected_candidate_id: str | None = Field(default=None, pattern=r"^candidate-[0-3]$")
    source_run_id: str | None = Field(default=None, pattern=SLUG_PATTERN)
    source_candidate_id: str | None = Field(default=None, pattern=r"^candidate-[0-3]$")
    edit_instruction: str = ""
    review_attempts: list[ReviewAttempt] = Field(default_factory=list, max_length=3)
    auto_repair_summary: AutoRepairSummary | None = None
    export: ExportRecord | None = None
    created_at: datetime = Field(default_factory=utc_now)
    updated_at: datetime = Field(default_factory=utc_now)

    @model_validator(mode="after")
    def validate_run_links(self) -> ProductionRun:
        if self.task.category not in {
            AssetCategory.ITEM,
            AssetCategory.UI,
            AssetCategory.CHARACTER,
            AssetCategory.SCENE,
        }:
            raise ValueError("stage 6 only supports static asset categories")
        candidate_ids = {candidate.candidate_id for candidate in self.candidates}
        if (
            self.selected_candidate_id is not None
            and self.selected_candidate_id not in candidate_ids
        ):
            raise ValueError("selected candidate must exist in the run")
        if (self.source_run_id is None) != (self.source_candidate_id is None):
            raise ValueError("source run and candidate must be set together")
        return self


class ProductionGenerateRequest(StrictModel):
    candidate_count: int = Field(default=4, ge=1, le=4)
    prompt_override: str | None = Field(default=None, min_length=1, max_length=32_000)


class CandidateSelection(StrictModel):
    candidate_id: str = Field(pattern=r"^candidate-[0-3]$")


class CandidateEditRequest(CandidateSelection):
    instruction: str = Field(min_length=1, max_length=4000)
    candidate_count: int = Field(default=1, ge=1, le=4)
    mask_workspace_relative_path: str | None = Field(
        default=None,
        min_length=1,
        max_length=1000,
    )


class CandidateReviewRequest(CandidateSelection):
    pass


RepairAction = Literal["none", "edit", "reprocess", "manual"]
AutoRepairStopReason = Literal[
    "passed",
    "retry-limit-reached",
    "no-actionable-failure",
    "manual-review-required",
    "disabled",
]


class RepairPlan(StrictModel):
    action: RepairAction
    reason: str = Field(min_length=1, max_length=2000)
    target_dimensions: list[ReviewDimension] = Field(default_factory=list)
    prompt: str = Field(default="", max_length=8000)
    retry_allowed: bool = False
    stop_reason: AutoRepairStopReason | None = None

    @model_validator(mode="after")
    def validate_edit_action(self) -> RepairPlan:
        if self.action == "edit" and (not self.retry_allowed or not self.prompt.strip()):
            raise ValueError("edit repair requires an actionable prompt")
        if self.retry_allowed and self.action != "edit":
            raise ValueError("only edit repair can be retried through the image model")
        return self


class ReviewAttempt(StrictModel):
    attempt_index: int = Field(ge=0, le=2)
    run_id: str = Field(pattern=SLUG_PATTERN)
    candidate_id: str = Field(pattern=r"^candidate-[0-3]$")
    comparison_relative_path: str = Field(min_length=1, max_length=1000)
    quality_report: QualityReport | None = None
    repair_plan: RepairPlan | None = None
    created_at: datetime = Field(default_factory=utc_now)


class AutoRepairSummary(StrictModel):
    retry_count: int = Field(ge=0, le=2)
    max_retries: int = Field(ge=0, le=2)
    stop_reason: AutoRepairStopReason
    attempts: list[ReviewAttempt] = Field(default_factory=list, max_length=3)

    @model_validator(mode="after")
    def validate_retry_count(self) -> AutoRepairSummary:
        if self.retry_count > self.max_retries:
            raise ValueError("retry count cannot exceed max retries")
        if self.attempts and len(self.attempts) != self.retry_count + 1:
            raise ValueError("attempt history must contain initial review plus each retry")
        return self


class CandidateReviewAndRepairRequest(CandidateSelection):
    automatic_repair: bool = True
    max_retries: int = Field(default=2, ge=0, le=2)


class ProductionExportRequest(StrictModel):
    variant: str = Field(default="default", pattern=SLUG_PATTERN)
    accept_style_risk: bool = False


class ProductionExportResult(StrictModel):
    export: ExportRecord
    style_score: int = Field(ge=0, le=100)
    minimum_style_score: int = Field(ge=0, le=100)
    style_risk_accepted: bool = False

    @model_validator(mode="after")
    def require_low_score_risk_acceptance(self) -> ProductionExportResult:
        if self.style_score < self.minimum_style_score and not self.style_risk_accepted:
            raise ValueError("low style score export requires explicit risk acceptance")
        return self
