from __future__ import annotations

from pydantic import Field, model_validator

from .core import StrictModel


class CropRect(StrictModel):
    x: int = Field(ge=0)
    y: int = Field(ge=0)
    width: int = Field(gt=0, le=3840)
    height: int = Field(gt=0, le=3840)


class CandidateTransformRequest(StrictModel):
    candidate_id: str = Field(pattern=r"^candidate-[0-3]$")
    crop: CropRect | None = None
    output_width: int | None = Field(default=None, gt=0, le=3840)
    output_height: int | None = Field(default=None, gt=0, le=3840)
    padding_ratio: float | None = Field(default=None, ge=0, lt=0.5)
    remove_background: bool = False

    @model_validator(mode="after")
    def require_complete_output_size(self) -> CandidateTransformRequest:
        if (self.output_width is None) != (self.output_height is None):
            raise ValueError("output width and height must be set together")
        return self


class CandidateMaskRequest(StrictModel):
    mask_png_base64: str = Field(min_length=1, max_length=30_000_000)


class CandidateMaskRecord(StrictModel):
    workspace_relative_path: str = Field(min_length=1, max_length=1000)
    width: int = Field(gt=0)
    height: int = Field(gt=0)
    sha256: str = Field(pattern=r"^[a-f0-9]{64}$")
