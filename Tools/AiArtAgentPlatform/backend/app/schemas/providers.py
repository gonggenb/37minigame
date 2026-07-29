from __future__ import annotations

from datetime import datetime

from pydantic import Field

from app.providers.errors import ProviderErrorCode

from .core import StrictModel


class ModelStatusResponse(StrictModel):
    api_key_configured: bool
    review_model: str
    image_model: str
    timeout_seconds: float
    max_retries: int


class ModelAvailabilityRequest(StrictModel):
    include_image: bool = False


class ModelCheckResult(StrictModel):
    capability: str
    model: str
    available: bool
    error_code: ProviderErrorCode | None = None
    retryable: bool = False
    detail: str = ""


class ModelAvailabilityResponse(StrictModel):
    checks: list[ModelCheckResult] = Field(min_length=1)


class CostBreakdown(StrictModel):
    key: str
    request_count: int = Field(ge=0)
    known_cost_usd: float = Field(ge=0)
    unknown_cost_count: int = Field(ge=0)


class ProjectCostSummary(StrictModel):
    project_id: str
    request_count: int = Field(ge=0)
    known_cost_usd: float = Field(ge=0)
    unknown_cost_count: int = Field(ge=0)
    invalid_record_count: int = Field(ge=0)
    by_model: list[CostBreakdown] = Field(default_factory=list)
    by_category: list[CostBreakdown] = Field(default_factory=list)
    latest_at: datetime | None = None
