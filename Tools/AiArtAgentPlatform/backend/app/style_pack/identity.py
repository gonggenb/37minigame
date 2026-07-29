from __future__ import annotations

from pathlib import Path

from app.schemas.style_pack import CharacterIdentity
from app.workspace.atomic_store import atomic_write_json, read_json
from app.workspace.path_guard import safe_child, validate_slug
from app.workspace.project_workspace import ProjectWorkspace


class CharacterIdentityStore:
    def __init__(self, projects: ProjectWorkspace) -> None:
        self.projects = projects

    def path(self, project_id: str, asset_id: str) -> Path:
        validate_slug(asset_id)
        self.projects.get_project(project_id)
        return safe_child(
            self.projects.project_path(project_id),
            "assets",
            "character",
            asset_id,
            "identity.json",
        )

    def save(self, project_id: str, identity: CharacterIdentity) -> CharacterIdentity:
        atomic_write_json(
            self.path(project_id, identity.asset_id),
            identity.model_dump(mode="json"),
        )
        return identity

    def get(self, project_id: str, asset_id: str) -> CharacterIdentity:
        path = self.path(project_id, asset_id)
        if not path.is_file():
            raise FileNotFoundError(asset_id)
        return CharacterIdentity.model_validate(read_json(path))
