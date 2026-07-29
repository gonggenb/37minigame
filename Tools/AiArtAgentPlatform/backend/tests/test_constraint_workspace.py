from pathlib import Path

import pytest
import yaml
from app.constraints.workspace import ConstraintWorkspace
from app.schemas.core import AssetCategory, ConstraintProfile, ProjectConfig
from app.workspace.project_workspace import ProjectWorkspace


def _profile(category: AssetCategory) -> dict[str, object]:
    animation = category in {AssetCategory.ANIMATION, AssetCategory.EFFECT}
    return {
        "schema_version": 1,
        "profile_id": f"wuxia-{category.value}",
        "category": category.value,
        "master_width": 1024,
        "master_height": 1024,
        "output_width": 256 if category is AssetCategory.CHARACTER else 128,
        "output_height": 256 if category is AssetCategory.CHARACTER else 128,
        "require_rgba": category is not AssetCategory.SCENE,
        "require_transparency": category is not AssetCategory.SCENE,
        "crop_mode": "none" if category is AssetCategory.SCENE else "alpha_bounds",
        "padding_ratio": 0.125,
        "occupancy_ratio": 0.75,
        "resize_algorithm": "lanczos",
        "pivot_x": 0.5,
        "pivot_y": 1.0 if category in {AssetCategory.CHARACTER, AssetCategory.ANIMATION} else 0.5,
        "filename_template": "{asset_id}_{variant}.png",
        "max_file_bytes": 8388608,
        "output_sprite_sheet": animation,
        "frame_count": 4 if animation else None,
        "rows": 1 if animation else None,
        "columns": 4 if animation else None,
        "frame_width": 256 if animation else None,
        "frame_height": 256 if animation else None,
        "preview_fps": 8 if animation else None,
        "loop": True if animation else None,
        "baseline": "bottom_center" if category is AssetCategory.ANIMATION else None,
        "shared_scale": True,
        "lock_first_frame": category is AssetCategory.ANIMATION,
        "max_center_drift_px": 4 if animation else None,
        "max_size_drift_ratio": 0.08 if animation else None,
    }


def _create_workspace(tmp_path: Path) -> tuple[ConstraintWorkspace, Path]:
    projects = ProjectWorkspace(tmp_path / "data")
    projects.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    preset_dir = tmp_path / "presets"
    constraint_dir = (
        preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "constraints"
    )
    constraint_dir.mkdir(parents=True)
    for category in AssetCategory:
        (constraint_dir / f"{category.value}.yaml").write_text(
            yaml.safe_dump(_profile(category), allow_unicode=True, sort_keys=False),
            encoding="utf-8",
        )
    return ConstraintWorkspace(projects, preset_dir), tmp_path / "data"


def test_constraint_workspace_initializes_all_six_category_profiles(tmp_path: Path) -> None:
    constraints, data_dir = _create_workspace(tmp_path)

    profiles = constraints.get_all("wuxia-demo")

    assert set(profiles) == set(AssetCategory)
    for category, profile in profiles.items():
        assert profile.category is category
        assert (
            data_dir
            / "workspaces"
            / "wuxia-demo"
            / "constraints"
            / f"{category.value}.yaml"
        ).is_file()


def test_constraint_workspace_persists_updates_and_rejects_category_mismatch(
    tmp_path: Path,
) -> None:
    constraints, _ = _create_workspace(tmp_path)
    item = constraints.get("wuxia-demo", AssetCategory.ITEM)
    updated = item.model_copy(update={"output_width": 192, "output_height": 192})

    constraints.update("wuxia-demo", AssetCategory.ITEM, updated)

    assert constraints.get("wuxia-demo", AssetCategory.ITEM).output_width == 192
    mismatched = ConstraintProfile.model_validate(
        {**_profile(AssetCategory.UI), "profile_id": "wuxia-ui"}
    )
    with pytest.raises(ValueError, match="category"):
        constraints.update("wuxia-demo", AssetCategory.ITEM, mismatched)
