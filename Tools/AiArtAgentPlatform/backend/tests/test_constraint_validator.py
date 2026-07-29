from io import BytesIO
from pathlib import Path

import pytest
from app.constraints.exporter import ExportBlocked, ExportConflict, ImageExporter
from app.constraints.validator import ConstraintValidator
from app.image_processing.pipeline import ImageProcessor
from app.schemas.core import AssetCategory, ConstraintProfile, ProjectConfig
from app.schemas.image_tools import BackgroundRemovalConfig
from app.workspace.project_workspace import ProjectWorkspace
from PIL import Image, ImageDraw


def _encode(image: Image.Image) -> bytes:
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _profile(**overrides: object) -> ConstraintProfile:
    payload: dict[str, object] = {
        "schema_version": 1,
        "profile_id": "validator-item",
        "category": AssetCategory.ITEM,
        "master_width": 1024,
        "master_height": 1024,
        "output_width": 20,
        "output_height": 20,
        "require_rgba": True,
        "require_transparency": True,
        "crop_mode": "alpha_bounds",
        "padding_ratio": 0.1,
        "occupancy_ratio": 0.8,
        "resize_algorithm": "nearest",
        "pivot_x": 0.5,
        "pivot_y": 0.5,
        "filename_template": "{asset_id}_{variant}.png",
        "max_file_bytes": 8388608,
        "output_sprite_sheet": False,
        "shared_scale": True,
        "lock_first_frame": False,
    }
    payload.update(overrides)
    return ConstraintProfile.model_validate(payload)


def _valid_png(profile: ConstraintProfile | None = None) -> bytes:
    source = Image.new("RGBA", (10, 10), (0, 0, 0, 0))
    ImageDraw.Draw(source).rectangle((2, 2, 7, 7), fill=(160, 70, 40, 255))
    return ImageProcessor.process(
        _encode(source),
        profile or _profile(),
        BackgroundRemovalConfig(),
    ).content


def test_validator_reports_each_hard_constraint_for_a_valid_png() -> None:
    report = ConstraintValidator.validate(
        _valid_png(),
        _profile(),
        asset_id="sword-001",
        variant="default",
        filename="sword-001_default.png",
    )

    assert report.passed is True
    assert {check.name for check in report.checks} == {
        "decode",
        "png_format",
        "dimensions",
        "rgba",
        "alpha_channel",
        "subject_bounds",
        "filename",
        "file_size",
        "sprite_sheet_grid",
        "content_hash",
    }
    assert all(check.passed for check in report.checks)


def test_validator_separates_size_filename_and_grid_failures() -> None:
    animation_profile = _profile(
        profile_id="validator-animation",
        category=AssetCategory.ANIMATION,
        output_width=20,
        output_height=20,
        output_sprite_sheet=True,
        frame_count=4,
        rows=1,
        columns=4,
        frame_width=6,
        frame_height=20,
    )
    report = ConstraintValidator.validate(
        _valid_png(),
        animation_profile,
        asset_id="hero-main",
        variant="idle",
        filename="wrong-name.png",
    )
    results = {check.name: check.passed for check in report.checks}

    assert report.passed is False
    assert results["filename"] is False
    assert results["sprite_sheet_grid"] is False
    assert results["decode"] is True


def test_exporter_writes_verified_hash_and_refuses_silent_overwrite(tmp_path: Path) -> None:
    projects = ProjectWorkspace(tmp_path)
    projects.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    exporter = ImageExporter(projects)
    content = _valid_png()

    record = exporter.export(
        "wuxia-demo",
        AssetCategory.ITEM,
        "sword-001",
        "default",
        content,
        _profile(),
    )

    exported = tmp_path / "workspaces" / "wuxia-demo" / Path(record.relative_path)
    assert exported.read_bytes() == content
    assert record.hard_constraints.passed is True
    assert record.sha256 == record.written_sha256

    with pytest.raises(ExportConflict):
        exporter.export(
            "wuxia-demo",
            AssetCategory.ITEM,
            "sword-001",
            "default",
            content,
            _profile(),
        )


def test_exporter_blocks_invalid_image_before_writing(tmp_path: Path) -> None:
    projects = ProjectWorkspace(tmp_path)
    projects.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    exporter = ImageExporter(projects)

    with pytest.raises(ExportBlocked):
        exporter.export(
            "wuxia-demo",
            AssetCategory.ITEM,
            "sword-001",
            "default",
            b"not-a-png",
            _profile(),
        )

    export_root = (
        tmp_path
        / "workspaces"
        / "wuxia-demo"
        / "assets"
        / "item"
        / "sword-001"
        / "exports"
    )
    assert not export_root.exists()
