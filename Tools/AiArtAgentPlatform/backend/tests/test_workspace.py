from pathlib import Path

import pytest
import yaml
from app.schemas.core import ProjectConfig
from app.workspace.atomic_store import atomic_write_json, atomic_write_yaml, read_json, read_yaml
from app.workspace.path_guard import PathViolation, ensure_within, validate_slug
from app.workspace.project_workspace import ProjectAlreadyExists, ProjectNotFound, ProjectWorkspace


def test_validate_slug_rejects_traversal_and_absolute_paths() -> None:
    assert validate_slug("wuxia-demo") == "wuxia-demo"

    for value in ("../escape", "C:\\escape", "/escape", "a/b", "A_bad"):
        with pytest.raises(PathViolation):
            validate_slug(value)


def test_ensure_within_rejects_symlink_escape(tmp_path: Path) -> None:
    root = tmp_path / "workspace"
    root.mkdir()
    outside = tmp_path / "outside"
    outside.mkdir()
    link = root / "link"
    try:
        link.symlink_to(outside, target_is_directory=True)
    except (OSError, NotImplementedError):
        pytest.skip("symlink creation is unavailable on this Windows account")

    with pytest.raises(PathViolation):
        ensure_within(root, link / "secret.txt")


def test_atomic_yaml_and_json_round_trip(tmp_path: Path) -> None:
    yaml_path = tmp_path / "nested" / "config.yaml"
    json_path = tmp_path / "nested" / "config.json"
    payload = {"name": "武侠", "count": 4}

    atomic_write_yaml(yaml_path, payload)
    atomic_write_json(json_path, payload)

    assert read_yaml(yaml_path) == payload
    assert read_json(json_path) == payload
    assert not list(yaml_path.parent.glob("*.tmp"))


def test_project_workspace_creates_reads_and_updates_project(tmp_path: Path) -> None:
    workspace = ProjectWorkspace(tmp_path)
    project = ProjectConfig(project_id="wuxia-demo", display_name="武侠美术")

    created = workspace.create_project(project)

    assert created == project
    project_root = tmp_path / "workspaces" / "wuxia-demo"
    for directory in ("style-pack", "constraints", "assets", "jobs", "logs"):
        assert (project_root / directory).is_dir()
    assert (
        yaml.safe_load((project_root / "project.yaml").read_text(encoding="utf-8"))["project_id"]
        == "wuxia-demo"
    )

    with pytest.raises(ProjectAlreadyExists):
        workspace.create_project(project)

    updated = project.model_copy(update={"display_name": "武侠美术二期"})
    assert workspace.update_project("wuxia-demo", updated).display_name == "武侠美术二期"
    assert workspace.get_project("wuxia-demo").display_name == "武侠美术二期"
    assert [item.project_id for item in workspace.list_projects()] == ["wuxia-demo"]

    with pytest.raises(ProjectNotFound):
        workspace.get_project("missing-project")
