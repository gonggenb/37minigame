from __future__ import annotations

from pathlib import Path

from app.schemas.core import AssetCategory, AssetTask, ConstraintProfile
from app.workspace.atomic_store import atomic_write_yaml, read_yaml
from app.workspace.path_guard import safe_child
from app.workspace.project_workspace import ProjectWorkspace


class ConstraintWorkspace:
    def __init__(self, projects: ProjectWorkspace, preset_dir: Path) -> None:
        self.projects = projects
        self.preset_dir = preset_dir.resolve()

    def get_all(self, project_id: str) -> dict[AssetCategory, ConstraintProfile]:
        return {category: self.get(project_id, category) for category in AssetCategory}

    def get(self, project_id: str, category: AssetCategory) -> ConstraintProfile:
        project = self.projects.get_project(project_id)
        path = self._path(project_id, category)
        if not path.is_file():
            preset_path = safe_child(
                self.preset_dir,
                project.visual_type,
                "constraints",
                f"{category.value}.yaml",
            )
            if not preset_path.is_file():
                raise FileNotFoundError(
                    f"constraint preset not found: {project.visual_type}/{category.value}"
                )
            profile = self._read(preset_path)
            self._validate_category(category, profile)
            atomic_write_yaml(path, profile.model_dump(mode="json"))
        profile = self._read(path)
        self._validate_category(category, profile)
        return profile

    def update(
        self,
        project_id: str,
        category: AssetCategory,
        profile: ConstraintProfile,
    ) -> ConstraintProfile:
        self.projects.get_project(project_id)
        self._validate_category(category, profile)
        atomic_write_yaml(
            self._path(project_id, category),
            profile.model_dump(mode="json"),
        )
        return profile

    def resolve(self, project_id: str, task: AssetTask) -> ConstraintProfile:
        profile = self.get(project_id, task.category)
        if not task.constraint_overrides:
            return profile
        protected = {"schema_version", "profile_id", "category"}
        invalid = protected.intersection(task.constraint_overrides)
        if invalid:
            names = ", ".join(sorted(invalid))
            raise ValueError(f"constraint overrides cannot replace identity fields: {names}")
        unknown = set(task.constraint_overrides).difference(ConstraintProfile.model_fields)
        if unknown:
            names = ", ".join(sorted(unknown))
            raise ValueError(f"unknown constraint overrides: {names}")
        payload = profile.model_dump(mode="python")
        payload.update(task.constraint_overrides)
        return ConstraintProfile.model_validate(payload)

    def _path(self, project_id: str, category: AssetCategory) -> Path:
        return safe_child(
            self.projects.project_path(project_id),
            "constraints",
            f"{category.value}.yaml",
        )

    @staticmethod
    def _read(path: Path) -> ConstraintProfile:
        data = read_yaml(path)
        if not isinstance(data, dict):
            raise ValueError("constraint profile must contain a mapping")
        return ConstraintProfile.model_validate(data)

    @staticmethod
    def _validate_category(
        category: AssetCategory,
        profile: ConstraintProfile,
    ) -> None:
        if profile.category is not category:
            raise ValueError("constraint profile category must match the requested category")
