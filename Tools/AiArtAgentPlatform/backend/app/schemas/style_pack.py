from __future__ import annotations

from typing import Literal

from pydantic import Field, field_validator, model_validator

from .core import (
    SLUG_PATTERN,
    AssetCategory,
    AssetTask,
    ImageOutputSpec,
    StrictModel,
)


class ReferenceSource(StrictModel):
    path: str = Field(min_length=1)
    mode: Literal["read_only"] = "read_only"

    @field_validator("path")
    @classmethod
    def require_absolute_path(cls, value: str) -> str:
        from pathlib import Path

        if not Path(value).is_absolute():
            raise ValueError("reference source path must be absolute")
        return value


class CameraStyle(StrictModel):
    projection: str = Field(min_length=1)
    pitch_semantic_min: int = Field(ge=0, le=90)
    pitch_semantic_max: int = Field(ge=0, le=90)
    shared_view_required: bool = True
    default_facing: str = Field(min_length=1)

    @model_validator(mode="after")
    def validate_pitch_range(self) -> CameraStyle:
        if self.pitch_semantic_min > self.pitch_semantic_max:
            raise ValueError("minimum camera pitch cannot exceed maximum camera pitch")
        return self


class PaletteStyle(StrictModel):
    base: list[str] = Field(min_length=1)
    accents: list[str] = Field(default_factory=list)


class RenderingStyle(StrictModel):
    character_proportion: str = Field(min_length=1)
    character_outline: str = Field(min_length=1)
    environment_detail: str = Field(min_length=1)
    surface_finish: str = Field(min_length=1)
    shadow_direction: str = Field(min_length=1)


class ReadabilityStyle(StrictModel):
    protect_playfield: bool = True
    character_contrast_above_environment: bool = True
    preserve_clear_silhouette: bool = True
    avoid_high_frequency_ground_noise: bool = True


class UiStyle(StrictModel):
    formal_text_baked_in: bool = False
    border_language: list[str] = Field(default_factory=list)


class StyleGuide(StrictModel):
    schema_version: Literal[1] = 1
    style_id: str = Field(min_length=1, max_length=160)
    display_name: str = Field(min_length=1, max_length=160)
    reference_source: ReferenceSource
    camera: CameraStyle
    palette: PaletteStyle
    rendering: RenderingStyle
    readability: ReadabilityStyle
    ui: UiStyle
    forbidden: list[str] = Field(default_factory=list)


class SourceReferenceFile(StrictModel):
    relative_path: str
    size_bytes: int = Field(ge=0)


class ReferenceImportRequest(StrictModel):
    reference_id: str = Field(pattern=SLUG_PATTERN)
    source_relative_path: str = Field(min_length=1, max_length=1000)
    categories: list[AssetCategory] = Field(min_length=1)
    identities: list[str] = Field(default_factory=list)
    usages: list[str] = Field(default_factory=list)
    viewpoints: list[str] = Field(default_factory=list)
    materials: list[str] = Field(default_factory=list)
    notes: str = Field(default="", max_length=2000)


class ReferenceUpdateRequest(StrictModel):
    categories: list[AssetCategory] = Field(min_length=1)
    identities: list[str] = Field(default_factory=list)
    usages: list[str] = Field(default_factory=list)
    viewpoints: list[str] = Field(default_factory=list)
    materials: list[str] = Field(default_factory=list)
    notes: str = Field(default="", max_length=2000)


class ReferenceAsset(StrictModel):
    reference_id: str = Field(pattern=SLUG_PATTERN)
    source_relative_path: str
    workspace_relative_path: str
    thumbnail_relative_path: str
    sha256: str = Field(pattern=r"^[a-f0-9]{64}$")
    width: int = Field(gt=0)
    height: int = Field(gt=0)
    categories: list[AssetCategory] = Field(min_length=1)
    identities: list[str] = Field(default_factory=list)
    usages: list[str] = Field(default_factory=list)
    viewpoints: list[str] = Field(default_factory=list)
    materials: list[str] = Field(default_factory=list)
    notes: str = ""


class ReferenceIndex(StrictModel):
    schema_version: Literal[1] = 1
    references: list[ReferenceAsset] = Field(default_factory=list)


class ReferenceFilters(StrictModel):
    category: AssetCategory | None = None
    identity: str | None = None
    usage: str | None = None
    viewpoint: str | None = None
    material: str | None = None
    limit: int = Field(default=100, ge=1, le=500)


class CharacterIdentity(StrictModel):
    asset_id: str = Field(pattern=SLUG_PATTERN)
    display_name: str = Field(min_length=1, max_length=120)
    silhouette: list[str] = Field(default_factory=list)
    face: list[str] = Field(default_factory=list)
    hair: list[str] = Field(default_factory=list)
    costume: list[str] = Field(default_factory=list)
    palette: list[str] = Field(default_factory=list)
    equipment: list[str] = Field(default_factory=list)
    immutable_traits: list[str] = Field(default_factory=list)


PromptSectionKey = Literal[
    "project_style",
    "asset_task",
    "identity",
    "references",
    "composition_camera",
    "lighting_materials",
    "output_spec",
    "forbidden",
    "postprocess",
]


class PromptSection(StrictModel):
    key: PromptSectionKey
    label: str
    content: str


class PromptPreviewRequest(StrictModel):
    task: AssetTask
    identity: CharacterIdentity | None = None
    viewpoint: str = ""
    composition: str = ""
    lighting: str = ""
    materials: list[str] = Field(default_factory=list)
    output_spec: ImageOutputSpec
    additional_negative_constraints: list[str] = Field(default_factory=list)
    prompt_override: str | None = None


class CompiledPrompt(StrictModel):
    task: AssetTask
    selected_reference_ids: list[str] = Field(max_length=4)
    sections: list[PromptSection] = Field(min_length=9, max_length=9)
    prompt: str = Field(min_length=1)
    negative_constraints: list[str]
