from __future__ import annotations

from datetime import UTC, datetime
from typing import Any, Literal

from pydantic import Field

from app.schemas.core import (
    AssetCategory,
    AssetTask,
    GenerationPlan,
    ProjectConfig,
    StrictModel,
)


class ProviderCapabilities(StrictModel):
    model: str
    supports_generate: bool = True
    supports_edit: bool = True
    supports_native_transparency: bool = False
    max_candidates: int = 4


class ProviderTrace(StrictModel):
    project_id: str = Field(pattern=r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
    category: AssetCategory
    asset_id: str = Field(pattern=r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
    run_id: str = Field(pattern=r"^[a-z0-9]+(?:-[a-z0-9]+)*$")


ProviderOperation = Literal["plan", "review", "generate", "edit", "unknown"]


class ProviderUsage(StrictModel):
    model: str
    operation: ProviderOperation = "unknown"
    raw: dict[str, Any] = Field(default_factory=dict)
    estimated_cost_usd: float | None = None
    created_at: datetime = Field(default_factory=lambda: datetime.now(UTC))


class ImageInput(StrictModel):
    filename: str = Field(min_length=1, max_length=160)
    content: bytes = Field(min_length=1)
    mime_type: Literal["image/png", "image/jpeg", "image/webp"] = "image/png"


class GenerateRequest(StrictModel):
    prompt: str = Field(min_length=1, max_length=32_000)
    width: int = Field(gt=0, le=3840)
    height: int = Field(gt=0, le=3840)
    candidate_count: int = Field(default=1, ge=1, le=4)
    quality: Literal["low", "medium", "high", "auto"] = "high"
    background: Literal["auto", "opaque", "transparent"] = "opaque"
    trace: ProviderTrace | None = None


class EditRequest(StrictModel):
    prompt: str = Field(min_length=1, max_length=32_000)
    images: list[ImageInput] = Field(min_length=1, max_length=4)
    mask: ImageInput | None = None
    width: int = Field(gt=0, le=3840)
    height: int = Field(gt=0, le=3840)
    candidate_count: int = Field(default=1, ge=1, le=4)
    quality: Literal["low", "medium", "high", "auto"] = "high"
    background: Literal["auto", "opaque", "transparent"] = "opaque"
    trace: ProviderTrace | None = None


class GeneratedImage(StrictModel):
    index: int = Field(ge=0)
    content: bytes
    mime_type: Literal["image/png"] = "image/png"
    revised_prompt: str | None = None


class PlanningRequest(StrictModel):
    project: ProjectConfig
    task: AssetTask
    style_guide: str = ""
    reference_descriptions: list[str] = Field(default_factory=list, max_length=4)
    trace: ProviderTrace | None = None


class ReviewRequest(StrictModel):
    project: ProjectConfig
    task: AssetTask
    plan: GenerationPlan
    candidate_png: bytes = Field(min_length=1)
    comparison_png: bytes | None = Field(default=None, min_length=1)
    reference_descriptions: list[str] = Field(default_factory=list, max_length=4)
    trace: ProviderTrace | None = None
