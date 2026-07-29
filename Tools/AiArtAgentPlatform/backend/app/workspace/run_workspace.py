from __future__ import annotations

from pathlib import Path

from app.providers.models import ProviderTrace

from .path_guard import safe_child, validate_slug
from .project_workspace import ProjectWorkspace


class RunWorkspace:
    def __init__(self, workspace: ProjectWorkspace) -> None:
        self.workspace = workspace

    def run_path(self, trace: ProviderTrace) -> Path:
        validate_slug(trace.asset_id)
        validate_slug(trace.run_id)
        project_path = self.workspace.project_path(trace.project_id)
        self.workspace.get_project(trace.project_id)
        return safe_child(
            project_path,
            "assets",
            trace.category.value,
            trace.asset_id,
            "runs",
            trace.run_id,
        )

    def ensure_run(self, trace: ProviderTrace) -> Path:
        run_path = self.run_path(trace)
        for directory in (run_path, run_path / "raw", run_path / "processed"):
            directory.mkdir(parents=True, exist_ok=True)
        return run_path
