from datetime import datetime
from typing import Literal

from pydantic import Field, model_validator

from .core import SLUG_PATTERN, AssetCategory, StrictModel


class ProjectActivityItem(StrictModel):
    workflow: Literal["static", "sequence"]
    category: AssetCategory
    asset_id: str = Field(pattern=SLUG_PATTERN)
    name: str = Field(min_length=1, max_length=120)
    status: str = Field(min_length=1, max_length=80)
    run_id: str | None = Field(default=None, pattern=SLUG_PATTERN)
    updated_at: datetime


class ProjectCategoryActivity(StrictModel):
    category: AssetCategory
    task_count: int = Field(ge=0)
    recent: list[ProjectActivityItem] = Field(default_factory=list, max_length=5)


class ProjectActivitySummary(StrictModel):
    schema_version: Literal[1] = 1
    project_id: str = Field(pattern=SLUG_PATTERN)
    reference_count: int = Field(ge=0)
    categories: list[ProjectCategoryActivity] = Field(min_length=6, max_length=6)

    @model_validator(mode="after")
    def require_all_categories_once(self) -> "ProjectActivitySummary":
        actual = [item.category for item in self.categories]
        if len(set(actual)) != 6 or set(actual) != set(AssetCategory):
            raise ValueError("project activity must contain every asset category once")
        return self
