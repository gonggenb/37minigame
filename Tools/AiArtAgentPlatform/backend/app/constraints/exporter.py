from __future__ import annotations

import hashlib

from app.schemas.core import AssetCategory, ConstraintProfile, HardConstraintReport
from app.schemas.image_tools import ExportRecord
from app.workspace.atomic_store import atomic_write_bytes
from app.workspace.path_guard import safe_child, validate_slug
from app.workspace.project_workspace import ProjectWorkspace

from .validator import ConstraintValidator


class ExportBlocked(ValueError):
    def __init__(self, report: HardConstraintReport) -> None:
        super().__init__("image export is blocked by hard constraints")
        self.report = report


class ExportConflict(FileExistsError):
    """同名导出文件已经存在。"""


class ImageExporter:
    def __init__(self, projects: ProjectWorkspace) -> None:
        self.projects = projects

    def export(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        variant: str,
        content: bytes,
        profile: ConstraintProfile,
    ) -> ExportRecord:
        validate_slug(asset_id)
        validate_slug(variant)
        self.projects.get_project(project_id)
        filename = ConstraintValidator.expected_filename(
            profile,
            asset_id=asset_id,
            variant=variant,
        )
        report = ConstraintValidator.validate(
            content,
            profile,
            asset_id=asset_id,
            variant=variant,
            filename=filename,
        )
        if not report.passed:
            raise ExportBlocked(report)
        project_root = self.projects.project_path(project_id)
        export_path = safe_child(
            project_root,
            "assets",
            category.value,
            asset_id,
            "exports",
            filename,
        )
        if export_path.exists():
            raise ExportConflict(filename)
        atomic_write_bytes(export_path, content)
        source_hash = hashlib.sha256(content).hexdigest()
        written_content = export_path.read_bytes()
        written_hash = hashlib.sha256(written_content).hexdigest()
        if source_hash != written_hash:
            export_path.unlink(missing_ok=True)
            raise OSError("exported file hash does not match source content")
        return ExportRecord(
            project_id=project_id,
            asset_id=asset_id,
            category=category,
            variant=variant,
            filename=filename,
            relative_path=export_path.relative_to(project_root).as_posix(),
            sha256=source_hash,
            written_sha256=written_hash,
            file_bytes=len(written_content),
            hard_constraints=report,
        )
