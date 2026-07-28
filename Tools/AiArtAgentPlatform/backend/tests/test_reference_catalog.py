from __future__ import annotations

import hashlib
from pathlib import Path

import pytest
import yaml
from app.schemas.core import AssetCategory, ProjectConfig
from app.schemas.style_pack import (
    ReferenceFilters,
    ReferenceImportRequest,
    ReferenceUpdateRequest,
)
from app.style_pack.references import (
    ReferenceAlreadyExists,
    ReferenceCatalog,
    ReferenceNotFound,
)
from app.style_pack.workspace import StylePackWorkspace
from app.workspace.path_guard import PathViolation
from app.workspace.project_workspace import ProjectWorkspace
from PIL import Image


def _write_image(path: Path, size: tuple[int, int], color: tuple[int, int, int, int]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.new("RGBA", size, color).save(path)


def _create_catalog(tmp_path: Path) -> tuple[ReferenceCatalog, Path, Path]:
    data_dir = tmp_path / "data"
    preset_dir = tmp_path / "presets"
    source_root = tmp_path / "source"
    source_root.mkdir()
    preset_path = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "style-guide.yaml"
    preset_path.parent.mkdir(parents=True)
    preset_path.write_text(
        yaml.safe_dump(
            {
                "schema_version": 1,
                "style_id": "wuxia-ink-chibi-topdown-2_5d",
                "display_name": "Q版水墨武侠俯视角",
                "reference_source": {"path": str(source_root), "mode": "read_only"},
                "camera": {
                    "projection": "orthographic_like",
                    "pitch_semantic_min": 35,
                    "pitch_semantic_max": 55,
                    "shared_view_required": True,
                    "default_facing": "right",
                },
                "palette": {"base": ["rice_paper"], "accents": ["vermilion"]},
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
                "ui": {"formal_text_baked_in": False, "border_language": ["ink_edge"]},
                "forbidden": ["pixel_art", "photorealism"],
            },
            allow_unicode=True,
        ),
        encoding="utf-8",
    )
    projects = ProjectWorkspace(data_dir)
    projects.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    style_packs = StylePackWorkspace(projects, preset_dir)
    return ReferenceCatalog(projects, style_packs), source_root, data_dir


def test_source_listing_only_returns_supported_images(tmp_path: Path) -> None:
    catalog, source_root, _ = _create_catalog(tmp_path)
    _write_image(source_root / "角色" / "hero.png", (128, 256), (80, 90, 70, 255))
    _write_image(source_root / "UI" / "panel.webp", (320, 160), (230, 220, 190, 255))
    (source_root / "notes.txt").write_text("not an image", encoding="utf-8")

    listed = catalog.list_source_files("wuxia-demo")

    assert [item.relative_path for item in listed] == ["UI/panel.webp", "角色/hero.png"]
    assert catalog.list_source_files("wuxia-demo", query="hero")[0].relative_path.endswith(
        "hero.png"
    )


def test_import_creates_an_identical_copy_and_bounded_thumbnail(tmp_path: Path) -> None:
    catalog, source_root, data_dir = _create_catalog(tmp_path)
    source = source_root / "角色" / "hero.png"
    _write_image(source, (640, 960), (80, 90, 70, 255))
    original_bytes = source.read_bytes()
    original_mtime = source.stat().st_mtime_ns
    request = ReferenceImportRequest(
        reference_id="hero-main",
        source_relative_path="角色/hero.png",
        categories=[AssetCategory.CHARACTER, AssetCategory.ANIMATION],
        identities=["hero"],
        usages=["gameplay"],
        viewpoints=["topdown-45"],
        materials=["ink-cloth"],
    )

    imported = catalog.import_reference("wuxia-demo", request)

    project_root = data_dir / "workspaces" / "wuxia-demo"
    copied = project_root / Path(imported.workspace_relative_path)
    thumbnail = project_root / Path(imported.thumbnail_relative_path)
    assert copied.read_bytes() == original_bytes
    assert imported.sha256 == hashlib.sha256(original_bytes).hexdigest()
    assert (imported.width, imported.height) == (640, 960)
    with Image.open(thumbnail) as preview:
        assert max(preview.size) <= 256
        assert preview.format == "PNG"
    assert source.read_bytes() == original_bytes
    assert source.stat().st_mtime_ns == original_mtime

    with pytest.raises(ReferenceAlreadyExists):
        catalog.import_reference("wuxia-demo", request)


def test_import_rejects_traversal_and_symlink_escape(tmp_path: Path) -> None:
    catalog, source_root, _ = _create_catalog(tmp_path)
    outside = tmp_path / "outside.png"
    _write_image(outside, (32, 32), (255, 0, 0, 255))

    with pytest.raises(PathViolation):
        catalog.import_reference(
            "wuxia-demo",
            ReferenceImportRequest(
                reference_id="escape",
                source_relative_path="../outside.png",
                categories=[AssetCategory.CHARACTER],
            ),
        )

    link = source_root / "linked.png"
    try:
        link.symlink_to(outside)
    except (OSError, NotImplementedError):
        pytest.skip("symlink creation is unavailable on this Windows account")

    with pytest.raises(PathViolation):
        catalog.import_reference(
            "wuxia-demo",
            ReferenceImportRequest(
                reference_id="linked",
                source_relative_path="linked.png",
                categories=[AssetCategory.CHARACTER],
            ),
        )


def test_reference_filters_match_category_identity_usage_and_viewpoint(tmp_path: Path) -> None:
    catalog, source_root, _ = _create_catalog(tmp_path)
    _write_image(source_root / "hero.png", (64, 64), (60, 70, 50, 255))
    _write_image(source_root / "forest.png", (64, 64), (40, 80, 40, 255))
    catalog.import_reference(
        "wuxia-demo",
        ReferenceImportRequest(
            reference_id="hero-main",
            source_relative_path="hero.png",
            categories=[AssetCategory.CHARACTER],
            identities=["hero"],
            usages=["gameplay"],
            viewpoints=["topdown-45"],
        ),
    )
    catalog.import_reference(
        "wuxia-demo",
        ReferenceImportRequest(
            reference_id="forest",
            source_relative_path="forest.png",
            categories=[AssetCategory.SCENE],
            usages=["battle-background"],
            viewpoints=["topdown-45"],
        ),
    )

    matched = catalog.list_references(
        "wuxia-demo",
        ReferenceFilters(
            category=AssetCategory.CHARACTER,
            identity="hero",
            usage="gameplay",
            viewpoint="topdown-45",
        ),
    )

    assert [item.reference_id for item in matched] == ["hero-main"]


def test_reference_metadata_update_thumbnail_and_material_filter(
    tmp_path: Path,
) -> None:
    catalog, source_root, _ = _create_catalog(tmp_path)
    source = source_root / "hero.png"
    _write_image(source, (96, 128), (60, 80, 70, 255))
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    imported = catalog.import_reference(
        "wuxia-demo",
        ReferenceImportRequest(
            reference_id="hero-main",
            source_relative_path="hero.png",
            categories=[AssetCategory.CHARACTER],
            identities=["hero"],
            usages=["gameplay"],
            viewpoints=["topdown-45"],
            materials=["ink-cloth"],
        ),
    )

    updated = catalog.update_reference(
        "wuxia-demo",
        "hero-main",
        ReferenceUpdateRequest(
            categories=[AssetCategory.CHARACTER, AssetCategory.ANIMATION],
            identities=["hero", "young-swordsman"],
            usages=["gameplay", "animation-seed"],
            viewpoints=["topdown-45"],
            materials=["ink-cloth", "rice-paper"],
            notes="批准的角色身份参考",
        ),
    )

    assert updated.sha256 == imported.sha256
    assert updated.workspace_relative_path == imported.workspace_relative_path
    assert updated.thumbnail_relative_path == imported.thumbnail_relative_path
    assert updated.materials == ["ink-cloth", "rice-paper"]
    assert catalog.read_thumbnail("wuxia-demo", "hero-main").startswith(b"\x89PNG")
    assert [
        item.reference_id
        for item in catalog.list_references(
            "wuxia-demo",
            ReferenceFilters(material="rice-paper"),
        )
    ] == ["hero-main"]
    assert catalog.count_references("wuxia-demo") == 1
    assert hashlib.sha256(source.read_bytes()).hexdigest() == source_hash

    with pytest.raises(ReferenceNotFound):
        catalog.update_reference(
            "wuxia-demo",
            "missing",
            ReferenceUpdateRequest(categories=[AssetCategory.CHARACTER]),
        )
