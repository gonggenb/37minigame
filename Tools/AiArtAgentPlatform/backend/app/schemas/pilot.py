from __future__ import annotations

from pathlib import Path
from typing import Literal

from pydantic import Field, field_validator, model_validator

from .core import SLUG_PATTERN, AssetCategory, HardConstraintReport, StrictModel
from .sequence import SequenceDriftReport

StaticPilotCategory = Literal["character", "scene", "item", "ui"]
PilotAction = Literal["idle", "move", "attack", "hit", "death"]


def _validate_relative_path(value: str) -> str:
    normalized = value.replace("\\", "/")
    path = Path(normalized)
    if path.is_absolute() or any(part in {"", ".", ".."} for part in path.parts):
        raise ValueError("pilot source paths must be normalized relative paths")
    return path.as_posix()


class PilotReferenceSpec(StrictModel):
    reference_id: str = Field(pattern=SLUG_PATTERN)
    source_relative_path: str = Field(min_length=1, max_length=1000)
    categories: list[AssetCategory] = Field(min_length=1)
    identities: list[str] = Field(default_factory=list)
    usages: list[str] = Field(default_factory=list)
    viewpoints: list[str] = Field(default_factory=list)
    materials: list[str] = Field(default_factory=list)
    notes: str = Field(default="", max_length=2000)

    @field_validator("source_relative_path")
    @classmethod
    def validate_source_path(cls, value: str) -> str:
        return _validate_relative_path(value)


class PilotStaticAssetSpec(StrictModel):
    asset_id: str = Field(pattern=SLUG_PATTERN)
    category: StaticPilotCategory
    source_relative_path: str = Field(min_length=1, max_length=1000)

    @field_validator("source_relative_path")
    @classmethod
    def validate_source_path(cls, value: str) -> str:
        return _validate_relative_path(value)


class PilotActionSpec(StrictModel):
    action: PilotAction
    source_relative_paths: list[str] = Field(min_length=1, max_length=32)
    frame_count: int = Field(ge=1, le=32)
    preview_fps: float = Field(gt=0, le=60)
    loop: bool = True
    derive_hit_proxy: bool = False
    max_center_drift_px: float | None = Field(default=None, ge=0)
    max_size_drift_ratio: float | None = Field(default=None, ge=0)
    max_baseline_drift_px: float | None = Field(default=None, ge=0)

    @field_validator("source_relative_paths")
    @classmethod
    def validate_source_paths(cls, values: list[str]) -> list[str]:
        return [_validate_relative_path(value) for value in values]

    @model_validator(mode="after")
    def validate_hit_proxy(self) -> PilotActionSpec:
        if self.derive_hit_proxy and self.action != "hit":
            raise ValueError("only the hit action can derive a hit proxy")
        return self


class PilotEffectSpec(StrictModel):
    asset_id: str = Field(pattern=SLUG_PATTERN)
    source_relative_path: str = Field(min_length=1, max_length=1000)
    frame_count: int = Field(ge=1, le=32)
    rows: int = Field(ge=1, le=8)
    columns: int = Field(ge=1, le=8)
    preview_fps: float = Field(gt=0, le=60)
    loop: bool = False
    blend_mode_hint: Literal["alpha", "additive"] = "additive"

    @field_validator("source_relative_path")
    @classmethod
    def validate_source_path(cls, value: str) -> str:
        return _validate_relative_path(value)

    @model_validator(mode="after")
    def validate_grid(self) -> PilotEffectSpec:
        if self.rows * self.columns < self.frame_count:
            raise ValueError("effect grid capacity must cover every frame")
        return self


class OfflinePilotManifest(StrictModel):
    schema_version: Literal[1] = 1
    pilot_id: str = Field(pattern=SLUG_PATTERN)
    display_name: str = Field(min_length=1, max_length=160)
    source_root: str = Field(min_length=1)
    references: list[PilotReferenceSpec] = Field(min_length=10, max_length=30)
    static_assets: list[PilotStaticAssetSpec] = Field(min_length=4, max_length=4)
    character_id: str = Field(pattern=SLUG_PATTERN)
    actions: list[PilotActionSpec] = Field(min_length=5, max_length=5)
    effect: PilotEffectSpec

    @field_validator("source_root")
    @classmethod
    def validate_source_root(cls, value: str) -> str:
        if not Path(value).is_absolute():
            raise ValueError("pilot source root must be absolute")
        return value

    @model_validator(mode="after")
    def validate_coverage(self) -> OfflinePilotManifest:
        reference_ids = [item.reference_id for item in self.references]
        if len(reference_ids) != len(set(reference_ids)):
            raise ValueError("pilot reference ids must be unique")
        covered = {category for item in self.references for category in item.categories}
        if covered != set(AssetCategory):
            raise ValueError("pilot references must cover all six asset categories")
        static_categories = {item.category for item in self.static_assets}
        if static_categories != {"character", "scene", "item", "ui"}:
            raise ValueError("pilot static assets must cover character, scene, item and ui")
        actions = [item.action for item in self.actions]
        if set(actions) != {"idle", "move", "attack", "hit", "death"}:
            raise ValueError("pilot actions must contain all five base actions")
        if len(actions) != len(set(actions)):
            raise ValueError("pilot actions must be unique")
        return self


class PilotArtifactRecord(StrictModel):
    category: AssetCategory
    asset_id: str = Field(pattern=SLUG_PATTERN)
    kind: str
    relative_path: str = Field(min_length=1, max_length=1000)
    width: int | None = Field(default=None, gt=0)
    height: int | None = Field(default=None, gt=0)
    hard_constraints: HardConstraintReport | None = None
    drift_report: SequenceDriftReport | None = None


class OfflinePilotReport(StrictModel):
    schema_version: Literal[1] = 1
    pilot_id: str = Field(pattern=SLUG_PATTERN)
    display_name: str
    source_root: str
    source_unchanged: bool
    reference_count: int = Field(ge=10, le=30)
    categories: list[AssetCategory]
    actions: list[PilotAction]
    artifacts: list[PilotArtifactRecord] = Field(min_length=6)
    limitations: list[str] = Field(default_factory=list)
