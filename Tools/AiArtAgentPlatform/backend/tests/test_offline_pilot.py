import os
import subprocess
import sys
from io import BytesIO
from pathlib import Path

import pytest
from app.pilot.runner import OfflinePilotRunner
from app.schemas.core import AssetCategory
from app.schemas.pilot import (
    OfflinePilotManifest,
    PilotActionSpec,
    PilotEffectSpec,
    PilotReferenceSpec,
    PilotStaticAssetSpec,
)
from PIL import Image, ImageDraw
from pydantic import ValidationError


def _write_image(path: Path, color: tuple[int, int, int], size=(96, 96)) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(image).ellipse(
        (12, 8, size[0] - 13, size[1] - 9),
        fill=(*color, 255),
    )
    stream = BytesIO()
    image.save(stream, format="PNG")
    path.write_bytes(stream.getvalue())


def _write_effect_sheet(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sheet)
    for index in range(16):
        column = index % 4
        row = index // 4
        inset = 8 + (index % 4) * 2
        draw.ellipse(
            (
                column * 64 + inset,
                row * 64 + inset,
                (column + 1) * 64 - inset,
                (row + 1) * 64 - inset,
            ),
            fill=(210, 95 + index * 4, 45, 220),
        )
    stream = BytesIO()
    sheet.save(stream, format="PNG")
    path.write_bytes(stream.getvalue())


def _manifest(source_root: Path) -> OfflinePilotManifest:
    references = [
        PilotReferenceSpec(
            reference_id=f"ref-{index:02d}",
            source_relative_path=f"refs/ref-{index:02d}.png",
            categories=[list(AssetCategory)[index % len(AssetCategory)]],
            notes="离线测试参考",
        )
        for index in range(10)
    ]
    return OfflinePilotManifest(
        pilot_id="wuxia-stage-9",
        display_name="武侠离线试点",
        source_root=str(source_root),
        references=references,
        static_assets=[
            PilotStaticAssetSpec(
                asset_id="pilot-character",
                category="character",
                source_relative_path="static/character.png",
            ),
            PilotStaticAssetSpec(
                asset_id="pilot-scene",
                category="scene",
                source_relative_path="static/scene.png",
            ),
            PilotStaticAssetSpec(
                asset_id="pilot-item",
                category="item",
                source_relative_path="static/item.png",
            ),
            PilotStaticAssetSpec(
                asset_id="pilot-ui",
                category="ui",
                source_relative_path="static/ui.png",
            ),
        ],
        character_id="pilot-hero",
        actions=[
            PilotActionSpec(
                action="idle",
                source_relative_paths=["actions/idle.png"],
                frame_count=4,
                preview_fps=6,
            ),
            PilotActionSpec(
                action="move",
                source_relative_paths=["actions/move-0.png", "actions/move-1.png"],
                frame_count=8,
                preview_fps=10,
            ),
            PilotActionSpec(
                action="attack",
                source_relative_paths=["actions/attack-0.png", "actions/attack-1.png"],
                frame_count=6,
                preview_fps=12,
                loop=False,
            ),
            PilotActionSpec(
                action="hit",
                source_relative_paths=["actions/idle.png"],
                frame_count=4,
                preview_fps=12,
                loop=False,
                derive_hit_proxy=True,
            ),
            PilotActionSpec(
                action="death",
                source_relative_paths=["actions/death-0.png", "actions/death-1.png"],
                frame_count=8,
                preview_fps=8,
                loop=False,
                max_center_drift_px=16,
                max_size_drift_ratio=0.2,
                max_baseline_drift_px=2,
            ),
        ],
        effect=PilotEffectSpec(
            asset_id="pilot-fire",
            source_relative_path="effects/fire.png",
            frame_count=16,
            rows=4,
            columns=4,
            preview_fps=12,
        ),
    )


def _prepare_source(source_root: Path) -> None:
    colors = [(50 + index * 10, 90, 70) for index in range(10)]
    for index, color in enumerate(colors):
        _write_image(source_root / f"refs/ref-{index:02d}.png", color)
    _write_image(source_root / "static/character.png", (55, 110, 80))
    _write_image(source_root / "static/scene.png", (140, 155, 115), (320, 180))
    _write_image(source_root / "static/item.png", (80, 125, 100))
    _write_image(source_root / "static/ui.png", (135, 80, 55), (180, 72))
    for name, color in (
        ("idle.png", (55, 110, 80)),
        ("move-0.png", (58, 112, 82)),
        ("move-1.png", (62, 116, 86)),
        ("attack-0.png", (125, 75, 50)),
        ("attack-1.png", (145, 85, 45)),
        ("death-0.png", (80, 80, 75)),
        ("death-1.png", (60, 60, 58)),
    ):
        _write_image(source_root / "actions" / name, color)
    _write_effect_sheet(source_root / "effects/fire.png")


def _write_box(
    path: Path,
    color: tuple[int, int, int],
    bounds: tuple[int, int, int, int],
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    ImageDraw.Draw(image).rectangle(bounds, fill=(*color, 255))
    stream = BytesIO()
    image.save(stream, format="PNG")
    path.write_bytes(stream.getvalue())


def test_manifest_requires_ten_to_thirty_references_and_five_actions(
    tmp_path: Path,
) -> None:
    manifest = _manifest(tmp_path)

    with pytest.raises(ValidationError):
        manifest.model_copy(update={"references": manifest.references[:9]}).model_validate(
            manifest.model_copy(update={"references": manifest.references[:9]}).model_dump()
        )

    with pytest.raises(ValidationError):
        OfflinePilotManifest.model_validate(
            manifest.model_copy(update={"actions": manifest.actions[:4]}).model_dump()
        )


def test_offline_pilot_produces_six_categories_and_preserves_sources(
    tmp_path: Path,
) -> None:
    source_root = tmp_path / "source"
    output_root = tmp_path / "pilot-output"
    _prepare_source(source_root)
    manifest = _manifest(source_root)
    before = {
        path.relative_to(source_root).as_posix(): path.read_bytes()
        for path in source_root.rglob("*.png")
    }
    preset_dir = Path(__file__).resolve().parents[2] / "shared" / "presets"

    report = OfflinePilotRunner(preset_dir=preset_dir).run(
        manifest,
        output_root=output_root,
    )

    after = {
        path.relative_to(source_root).as_posix(): path.read_bytes()
        for path in source_root.rglob("*.png")
    }
    assert before == after
    assert report.source_unchanged is True
    assert report.reference_count == 10
    assert set(report.categories) == set(AssetCategory)
    assert set(report.actions) == {"idle", "move", "attack", "hit", "death"}
    assert (output_root / "pilot-report.json").is_file()
    assert (output_root / "unity-acceptance.md").is_file()
    assert (output_root / "outputs/animation/pilot-hero/attack/sprite-sheet.png").is_file()
    assert (output_root / "outputs/effect/pilot-fire/preview.gif").is_file()
    assert all(item.relative_path for item in report.artifacts)

    with pytest.raises(FileExistsError):
        OfflinePilotRunner(preset_dir=preset_dir).run(
            manifest,
            output_root=output_root,
        )


def test_offline_pilot_applies_death_overrides_without_relaxing_move_limits(
    tmp_path: Path,
) -> None:
    source_root = tmp_path / "source"
    output_root = tmp_path / "pilot-output"
    _prepare_source(source_root)
    _write_box(source_root / "actions/move-1.png", (62, 116, 86), (8, 12, 87, 87))
    _write_box(source_root / "actions/death-1.png", (60, 60, 58), (8, 12, 87, 87))
    manifest = _manifest(source_root)
    preset_dir = Path(__file__).resolve().parents[2] / "shared" / "presets"

    report = OfflinePilotRunner(preset_dir=preset_dir).run(
        manifest,
        output_root=output_root,
    )

    move = next(
        item
        for item in report.artifacts
        if item.kind == "sprite_sheet" and "/move/" in item.relative_path
    )
    death = next(
        item
        for item in report.artifacts
        if item.kind == "sprite_sheet" and "/death/" in item.relative_path
    )
    assert move.drift_report is not None
    assert not move.drift_report.passed
    assert death.drift_report is not None
    assert death.drift_report.passed


def test_pilot_module_cli_loads_without_runtime_warning() -> None:
    backend_root = Path(__file__).resolve().parents[1]
    environment = os.environ.copy()
    environment["PYTHONPATH"] = str(backend_root)

    result = subprocess.run(
        [
            sys.executable,
            "-W",
            "error::RuntimeWarning",
            "-m",
            "app.pilot.runner",
            "--help",
        ],
        cwd=backend_root,
        env=environment,
        capture_output=True,
        check=False,
    )

    assert result.returncode == 0, result.stderr.decode(errors="replace")
    assert b"RuntimeWarning" not in result.stderr


def test_pilot_package_preserves_lazy_runner_export() -> None:
    from app.pilot import OfflinePilotRunner as ExportedRunner

    assert ExportedRunner is OfflinePilotRunner
