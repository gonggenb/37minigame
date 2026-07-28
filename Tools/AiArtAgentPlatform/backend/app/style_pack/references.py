from __future__ import annotations

import hashlib
from io import BytesIO
from pathlib import Path

from PIL import Image, UnidentifiedImageError

from app.schemas.style_pack import (
    ReferenceAsset,
    ReferenceFilters,
    ReferenceImportRequest,
    ReferenceIndex,
    ReferenceUpdateRequest,
    SourceReferenceFile,
)
from app.workspace.atomic_store import atomic_write_bytes, atomic_write_json, read_json
from app.workspace.path_guard import PathViolation, ensure_within, safe_child
from app.workspace.project_workspace import ProjectWorkspace

from .workspace import StylePackWorkspace

SUPPORTED_IMAGE_SUFFIXES = {".png", ".jpg", ".jpeg", ".webp"}


class ReferenceAlreadyExists(FileExistsError):
    """参考 ID 已存在。"""


class ReferenceNotFound(FileNotFoundError):
    """参考 ID 不存在。"""


class ReferenceCatalog:
    def __init__(
        self,
        projects: ProjectWorkspace,
        style_packs: StylePackWorkspace,
    ) -> None:
        self.projects = projects
        self.style_packs = style_packs

    def list_source_files(
        self,
        project_id: str,
        *,
        query: str = "",
        limit: int = 100,
    ) -> list[SourceReferenceFile]:
        if limit < 1 or limit > 500:
            raise ValueError("source file limit must be between 1 and 500")
        source_root = self._source_root(project_id)
        normalized_query = query.casefold().strip()
        files: list[SourceReferenceFile] = []
        for candidate in source_root.rglob("*"):
            if (
                not candidate.is_file()
                or candidate.suffix.casefold() not in SUPPORTED_IMAGE_SUFFIXES
            ):
                continue
            try:
                resolved = ensure_within(source_root, candidate)
            except PathViolation:
                continue
            relative_path = resolved.relative_to(source_root).as_posix()
            if normalized_query and normalized_query not in relative_path.casefold():
                continue
            files.append(
                SourceReferenceFile(
                    relative_path=relative_path,
                    size_bytes=resolved.stat().st_size,
                )
            )
        return sorted(files, key=lambda item: item.relative_path.casefold())[:limit]

    def import_reference(
        self,
        project_id: str,
        request: ReferenceImportRequest,
    ) -> ReferenceAsset:
        index = self._read_index(project_id)
        if any(item.reference_id == request.reference_id for item in index.references):
            raise ReferenceAlreadyExists(request.reference_id)
        source = self._source_file(project_id, request.source_relative_path)
        content = source.read_bytes()
        try:
            with Image.open(BytesIO(content)) as image:
                width, height = image.size
                preview = image.convert("RGBA")
        except (OSError, UnidentifiedImageError) as error:
            raise ValueError("reference source is not a supported image") from error

        project_root = self.projects.project_path(project_id)
        copy_path = safe_child(
            project_root,
            "style-pack",
            "references",
            f"{request.reference_id}{source.suffix.casefold()}",
        )
        thumbnail_path = safe_child(
            project_root,
            "style-pack",
            "thumbnails",
            f"{request.reference_id}.png",
        )
        atomic_write_bytes(copy_path, content)
        preview.thumbnail((256, 256), Image.Resampling.LANCZOS)
        thumbnail_stream = BytesIO()
        preview.save(thumbnail_stream, format="PNG")
        atomic_write_bytes(thumbnail_path, thumbnail_stream.getvalue())

        reference = ReferenceAsset(
            reference_id=request.reference_id,
            source_relative_path=source.relative_to(self._source_root(project_id)).as_posix(),
            workspace_relative_path=copy_path.relative_to(project_root).as_posix(),
            thumbnail_relative_path=thumbnail_path.relative_to(project_root).as_posix(),
            sha256=hashlib.sha256(content).hexdigest(),
            width=width,
            height=height,
            categories=request.categories,
            identities=request.identities,
            usages=request.usages,
            viewpoints=request.viewpoints,
            materials=request.materials,
            notes=request.notes,
        )
        index.references.append(reference)
        self._write_index(project_id, index)
        return reference

    def list_references(
        self,
        project_id: str,
        filters: ReferenceFilters | None = None,
    ) -> list[ReferenceAsset]:
        resolved_filters = filters or ReferenceFilters()
        references = self._read_index(project_id).references
        matched = [
            reference
            for reference in references
            if self._matches(reference, resolved_filters)
        ]
        return sorted(matched, key=lambda item: item.reference_id)[: resolved_filters.limit]

    def update_reference(
        self,
        project_id: str,
        reference_id: str,
        request: ReferenceUpdateRequest,
    ) -> ReferenceAsset:
        index = self._read_index(project_id)
        position = next(
            (
                position
                for position, item in enumerate(index.references)
                if item.reference_id == reference_id
            ),
            None,
        )
        if position is None:
            raise ReferenceNotFound(reference_id)
        current = index.references[position]
        updated = current.model_copy(
            update={
                "categories": request.categories,
                "identities": request.identities,
                "usages": request.usages,
                "viewpoints": request.viewpoints,
                "materials": request.materials,
                "notes": request.notes,
            }
        )
        index.references[position] = updated
        self._write_index(project_id, index)
        return updated

    def read_thumbnail(self, project_id: str, reference_id: str) -> bytes:
        reference = next(
            (
                item
                for item in self._read_index(project_id).references
                if item.reference_id == reference_id
            ),
            None,
        )
        if reference is None:
            raise ReferenceNotFound(reference_id)
        project_root = self.projects.project_path(project_id)
        thumbnail = safe_child(
            project_root,
            *Path(reference.thumbnail_relative_path).parts,
        )
        if not thumbnail.is_file():
            raise ReferenceNotFound(reference_id)
        return thumbnail.read_bytes()

    def count_references(self, project_id: str) -> int:
        return len(self._read_index(project_id).references)

    def delete_reference(self, project_id: str, reference_id: str) -> None:
        index = self._read_index(project_id)
        reference = next(
            (item for item in index.references if item.reference_id == reference_id),
            None,
        )
        if reference is None:
            raise ReferenceNotFound(reference_id)
        project_root = self.projects.project_path(project_id)
        for relative_path in (
            reference.workspace_relative_path,
            reference.thumbnail_relative_path,
        ):
            safe_child(project_root, *Path(relative_path).parts).unlink(missing_ok=True)
        index.references = [
            item for item in index.references if item.reference_id != reference_id
        ]
        self._write_index(project_id, index)

    def _source_root(self, project_id: str) -> Path:
        guide = self.style_packs.get_style_guide(project_id)
        root = Path(guide.reference_source.path).resolve()
        if not root.is_dir():
            raise FileNotFoundError("reference source directory does not exist")
        return root

    def _source_file(self, project_id: str, relative_path: str) -> Path:
        normalized = relative_path.replace("\\", "/")
        candidate_part = Path(normalized)
        if candidate_part.is_absolute() or any(
            part in {"", ".", ".."} for part in candidate_part.parts
        ):
            raise PathViolation("reference path must be relative and normalized")
        root = self._source_root(project_id)
        candidate = ensure_within(root, root.joinpath(*candidate_part.parts))
        if not candidate.is_file():
            raise FileNotFoundError(relative_path)
        if candidate.suffix.casefold() not in SUPPORTED_IMAGE_SUFFIXES:
            raise ValueError("unsupported reference image format")
        return candidate

    def _index_path(self, project_id: str) -> Path:
        self.projects.get_project(project_id)
        return safe_child(
            self.projects.project_path(project_id),
            "style-pack",
            "reference-index.json",
        )

    def _read_index(self, project_id: str) -> ReferenceIndex:
        path = self._index_path(project_id)
        if not path.is_file():
            return ReferenceIndex()
        return ReferenceIndex.model_validate(read_json(path))

    def _write_index(self, project_id: str, index: ReferenceIndex) -> None:
        atomic_write_json(
            self._index_path(project_id),
            index.model_dump(mode="json"),
        )

    @staticmethod
    def _matches(reference: ReferenceAsset, filters: ReferenceFilters) -> bool:
        if filters.category is not None and filters.category not in reference.categories:
            return False
        comparisons = (
            (filters.identity, reference.identities),
            (filters.usage, reference.usages),
            (filters.viewpoint, reference.viewpoints),
            (filters.material, reference.materials),
        )
        for expected, values in comparisons:
            if expected is not None and expected.casefold() not in {
                value.casefold() for value in values
            }:
                return False
        return True
