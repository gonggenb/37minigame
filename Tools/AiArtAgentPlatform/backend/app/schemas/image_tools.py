from typing import Literal

from pydantic import Field, model_validator

from .core import (
    SLUG_PATTERN,
    AssetCategory,
    HardConstraintReport,
    StrictModel,
)


class BackgroundRemovalConfig(StrictModel):
    mode: Literal["preserve", "corner_flood"] = "preserve"
    color_tolerance: int = Field(default=18, ge=0, le=255)
    alpha_low_threshold: int = Field(default=8, ge=0, le=254)
    alpha_high_threshold: int = Field(default=247, ge=1, le=255)

    @model_validator(mode="after")
    def validate_alpha_thresholds(self) -> "BackgroundRemovalConfig":
        if self.alpha_low_threshold >= self.alpha_high_threshold:
            raise ValueError("low alpha threshold must be below high threshold")
        return self


class ProcessedImageMetadata(StrictModel):
    width: int = Field(gt=0)
    height: int = Field(gt=0)
    mode: Literal["RGBA"] = "RGBA"
    source_alpha_bounds: tuple[int, int, int, int]
    alpha_bounds: tuple[int, int, int, int]
    scale: float = Field(gt=0)
    sha256: str = Field(pattern=r"^[a-f0-9]{64}$")
    file_bytes: int = Field(gt=0)


class ExportRecord(StrictModel):
    project_id: str
    asset_id: str
    category: AssetCategory
    variant: str
    filename: str
    relative_path: str
    sha256: str = Field(pattern=r"^[a-f0-9]{64}$")
    written_sha256: str = Field(pattern=r"^[a-f0-9]{64}$")
    file_bytes: int = Field(gt=0)
    hard_constraints: HardConstraintReport


class WorkspaceImageRequest(StrictModel):
    workspace_relative_path: str = Field(min_length=1, max_length=1000)
    asset_id: str = Field(pattern=SLUG_PATTERN)
    variant: str = Field(default="default", pattern=SLUG_PATTERN)
    background: BackgroundRemovalConfig = Field(default_factory=BackgroundRemovalConfig)


class ProcessPreviewResponse(StrictModel):
    processed_png_base64: str
    metadata: ProcessedImageMetadata
    hard_constraints: HardConstraintReport
