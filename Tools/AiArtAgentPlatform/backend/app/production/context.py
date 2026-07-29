from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

from app.agent.reference_selector import ReferenceSelector
from app.providers.models import ImageInput
from app.schemas.core import AssetCategory, AssetTask, ProjectConfig
from app.schemas.style_pack import CharacterIdentity, ReferenceAsset
from app.style_pack.identity import CharacterIdentityStore
from app.style_pack.references import ReferenceCatalog
from app.style_pack.workspace import StylePackWorkspace
from app.workspace.path_guard import safe_child
from app.workspace.project_workspace import ProjectWorkspace


@dataclass(frozen=True, slots=True)
class ProductionContext:
    project: ProjectConfig
    style_guide: str
    reference_descriptions: list[str]
    reference_images: list[ImageInput]


class ProductionContextBuilder:
    def __init__(
        self,
        projects: ProjectWorkspace,
        style_packs: StylePackWorkspace,
        references: ReferenceCatalog,
        identities: CharacterIdentityStore,
    ) -> None:
        self.projects = projects
        self.style_packs = style_packs
        self.references = references
        self.identities = identities

    def build(self, project_id: str, task: AssetTask) -> ProductionContext:
        project = self.projects.get_project(project_id)
        guide = self.style_packs.get_style_guide(project_id)
        references = self.references.list_references(project_id)
        identity = self._identity(project_id, task)
        selected = ReferenceSelector.select(
            task,
            references,
            identity_id=identity.asset_id if identity is not None else None,
            max_references=4,
        )
        return ProductionContext(
            project=project,
            style_guide=json.dumps(
                guide.model_dump(mode="json"),
                ensure_ascii=False,
                separators=(",", ":"),
            ),
            reference_descriptions=[self._describe_reference(item) for item in selected],
            reference_images=[self._read_reference(project_id, item) for item in selected],
        )

    def _identity(
        self,
        project_id: str,
        task: AssetTask,
    ) -> CharacterIdentity | None:
        if task.category is not AssetCategory.CHARACTER:
            return None
        try:
            return self.identities.get(project_id, task.asset_id)
        except FileNotFoundError:
            return None

    @staticmethod
    def _describe_reference(reference: ReferenceAsset) -> str:
        tags = [
            *reference.usages,
            *reference.viewpoints,
            *reference.materials,
        ]
        suffix = f"；标签：{'、'.join(tags)}" if tags else ""
        notes = f"；备注：{reference.notes}" if reference.notes else ""
        return f"{reference.reference_id}（{reference.source_relative_path}）{suffix}{notes}"

    def _read_reference(
        self,
        project_id: str,
        reference: ReferenceAsset,
    ) -> ImageInput:
        relative = Path(reference.workspace_relative_path)
        path = safe_child(
            self.projects.project_path(project_id),
            *relative.parts,
        )
        if not path.is_file():
            raise FileNotFoundError(reference.reference_id)
        suffix = path.suffix.casefold()
        mime_type: Literal["image/png", "image/jpeg", "image/webp"]
        if suffix in {".jpg", ".jpeg"}:
            mime_type = "image/jpeg"
        elif suffix == ".webp":
            mime_type = "image/webp"
        else:
            mime_type = "image/png"
        return ImageInput(
            filename=path.name,
            content=path.read_bytes(),
            mime_type=mime_type,
        )
