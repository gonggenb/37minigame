from __future__ import annotations

from pathlib import Path

from app.schemas.style_pack import StyleGuide
from app.workspace.atomic_store import atomic_write_yaml, read_yaml
from app.workspace.path_guard import safe_child
from app.workspace.project_workspace import ProjectWorkspace


class StylePackWorkspace:
    def __init__(self, projects: ProjectWorkspace, preset_dir: Path) -> None:
        self.projects = projects
        self.preset_dir = preset_dir.resolve()

    def style_guide_path(self, project_id: str) -> Path:
        self.projects.get_project(project_id)
        return safe_child(self.projects.project_path(project_id), "style-pack", "style-guide.yaml")

    def get_style_guide(self, project_id: str) -> StyleGuide:
        project = self.projects.get_project(project_id)
        path = self.style_guide_path(project_id)
        if not path.is_file():
            preset_path = safe_child(
                self.preset_dir,
                project.visual_type,
                "style-guide.yaml",
            )
            if not preset_path.is_file():
                raise FileNotFoundError(f"style preset not found: {project.visual_type}")
            preset = self._read_guide(preset_path)
            atomic_write_yaml(path, preset.model_dump(mode="json"))
        return self._read_guide(path)

    def update_style_guide(self, project_id: str, guide: StyleGuide) -> StyleGuide:
        project = self.projects.get_project(project_id)
        if guide.style_id != project.visual_type:
            raise ValueError("style guide id must match the project visual type")
        atomic_write_yaml(
            self.style_guide_path(project_id),
            guide.model_dump(mode="json"),
        )
        return guide

    @staticmethod
    def _read_guide(path: Path) -> StyleGuide:
        data = read_yaml(path)
        if not isinstance(data, dict):
            raise ValueError("style-guide.yaml must contain a mapping")
        return StyleGuide.model_validate(data)
