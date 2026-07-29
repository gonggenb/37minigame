from typing import Any

from pydantic import Field

from .core import StrictModel


class JobCreateRequest(StrictModel):
    kind: str = Field(min_length=1, max_length=80)
    payload: dict[str, Any] = Field(default_factory=dict)
    max_attempts: int = Field(default=2, ge=0, le=5)
