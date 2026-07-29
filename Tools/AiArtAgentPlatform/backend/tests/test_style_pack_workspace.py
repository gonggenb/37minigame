from pathlib import Path

import pytest
import yaml
from app.schemas.core import ProjectConfig
from app.schemas.style_pack import StyleGuide
from app.style_pack.workspace import StylePackWorkspace
from app.workspace.project_workspace import ProjectWorkspace
from pydantic import ValidationError


def _style_guide_payload(reference_root: Path) -> dict[str, object]:
    return {
        "schema_version": 1,
        "style_id": "wuxia-ink-chibi-topdown-2_5d",
        "display_name": "Q版水墨武侠俯视角",
        "reference_source": {"path": str(reference_root), "mode": "read_only"},
        "camera": {
            "projection": "orthographic_like",
            "pitch_semantic_min": 35,
            "pitch_semantic_max": 55,
            "shared_view_required": True,
            "default_facing": "right",
        },
        "palette": {
            "base": ["rice_paper", "ink_gray"],
            "accents": ["vermilion", "dark_gold"],
        },
        "rendering": {
            "character_proportion": "chibi_wuxia",
            "character_outline": "clean_ink",
            "environment_detail": "restrained",
            "surface_finish": "matte_painted_2d",
            "shadow_direction": "lower_right",
        },
        "readability": {
            "protect_playfield": True,
            "character_contrast_above_environment": True,
            "preserve_clear_silhouette": True,
            "avoid_high_frequency_ground_noise": True,
        },
        "ui": {
            "formal_text_baked_in": False,
            "border_language": ["ink_edge", "rice_paper"],
        },
        "forbidden": ["pixel_art", "photorealism", "baked_text"],
    }


def test_style_guide_is_initialized_from_the_project_preset(tmp_path: Path) -> None:
    data_dir = tmp_path / "data"
    preset_dir = tmp_path / "presets"
    reference_root = tmp_path / "references"
    reference_root.mkdir()
    preset_path = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "style-guide.yaml"
    preset_path.parent.mkdir(parents=True)
    preset_path.write_text(
        yaml.safe_dump(_style_guide_payload(reference_root), allow_unicode=True),
        encoding="utf-8",
    )
    projects = ProjectWorkspace(data_dir)
    projects.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    style_packs = StylePackWorkspace(projects, preset_dir)

    guide = style_packs.get_style_guide("wuxia-demo")

    assert guide.display_name == "Q版水墨武侠俯视角"
    project_guide = data_dir / "workspaces" / "wuxia-demo" / "style-pack" / "style-guide.yaml"
    assert project_guide.is_file()
    assert yaml.safe_load(project_guide.read_text(encoding="utf-8"))["style_id"] == guide.style_id


def test_style_guide_update_is_persisted(tmp_path: Path) -> None:
    projects = ProjectWorkspace(tmp_path / "data")
    projects.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    preset_dir = tmp_path / "presets"
    reference_root = tmp_path / "references"
    reference_root.mkdir()
    preset_path = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "style-guide.yaml"
    preset_path.parent.mkdir(parents=True)
    preset_path.write_text(
        yaml.safe_dump(_style_guide_payload(reference_root), allow_unicode=True),
        encoding="utf-8",
    )
    style_packs = StylePackWorkspace(projects, preset_dir)
    guide = style_packs.get_style_guide("wuxia-demo")

    updated = guide.model_copy(update={"display_name": "武侠水墨轻量风格"})
    style_packs.update_style_guide("wuxia-demo", updated)

    assert style_packs.get_style_guide("wuxia-demo").display_name == "武侠水墨轻量风格"


def test_style_guide_rejects_a_writable_reference_source(tmp_path: Path) -> None:
    payload = _style_guide_payload(tmp_path)
    payload["reference_source"] = {"path": str(tmp_path), "mode": "read_write"}

    with pytest.raises(ValidationError):
        StyleGuide.model_validate(payload)
