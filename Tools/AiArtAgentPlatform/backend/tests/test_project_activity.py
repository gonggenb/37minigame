from importlib import import_module
from pathlib import Path

import yaml
from app.constraints.workspace import ConstraintWorkspace
from app.production.sequence_service import SequenceProductionService
from app.production.workspace import ProductionWorkspace
from app.schemas.core import AssetCategory, AssetTask, ProjectConfig
from app.schemas.production import ProductionRun, StaticAssetRecord
from app.schemas.sequence import SequenceTask
from app.style_pack.references import ReferenceCatalog
from app.style_pack.workspace import StylePackWorkspace
from app.workspace.project_workspace import ProjectWorkspace


def _write_style_preset(preset_dir: Path, source_root: Path) -> None:
    path = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "style-guide.yaml"
    path.parent.mkdir(parents=True)
    path.write_text(
        yaml.safe_dump(
            {
                "schema_version": 1,
                "style_id": "wuxia-ink-chibi-topdown-2_5d",
                "display_name": "武侠",
                "reference_source": {
                    "path": str(source_root),
                    "mode": "read_only",
                },
                "camera": {
                    "projection": "orthographic_like",
                    "pitch_semantic_min": 35,
                    "pitch_semantic_max": 55,
                    "shared_view_required": True,
                    "default_facing": "right",
                },
                "palette": {"base": ["ink"], "accents": []},
                "rendering": {
                    "character_proportion": "chibi",
                    "character_outline": "ink",
                    "environment_detail": "restrained",
                    "surface_finish": "matte",
                    "shadow_direction": "lower_right",
                },
                "readability": {},
                "ui": {},
                "forbidden": [],
            },
            allow_unicode=True,
        ),
        encoding="utf-8",
    )


def test_project_activity_groups_static_and_sequence_work(tmp_path: Path) -> None:
    source_root = tmp_path / "source"
    source_root.mkdir()
    preset_dir = tmp_path / "presets"
    _write_style_preset(preset_dir, source_root)
    projects = ProjectWorkspace(tmp_path / "data")
    projects.create_project(
        ProjectConfig(project_id="wuxia-demo", display_name="武侠美术")
    )
    references = ReferenceCatalog(
        projects,
        StylePackWorkspace(projects, preset_dir),
    )
    production = ProductionWorkspace(projects)
    task = AssetTask(
        asset_id="green-sword",
        category=AssetCategory.ITEM,
        name="青锋剑",
        brief="水墨青锋剑",
        usage="world-sprite",
        style_pack="wuxia-ink-chibi-topdown-2_5d",
        constraint_profile="wuxia-item",
        output_mode="single-png",
    )
    production.create_asset("wuxia-demo", StaticAssetRecord(task=task))
    production.create_run(
        ProductionRun(
            run_id="run-item",
            project_id="wuxia-demo",
            task=task,
            status="planned",
        )
    )
    sequences = SequenceProductionService(
        projects,
        ConstraintWorkspace(projects, preset_dir),
        object(),
        id_factory=lambda: "run-effect",
    )
    sequences.create_reference(
        "wuxia-demo",
        SequenceTask(
            asset_id="sword-flash",
            category=AssetCategory.EFFECT,
            name="剑光",
            action="effect",
            frame_count=4,
            rows=2,
            columns=2,
            frame_width=256,
            frame_height=256,
            preview_fps=8,
        ),
    )
    activity_module = import_module("app.workspace.project_activity")
    service = activity_module.ProjectActivityService(
        projects,
        references,
        production,
        sequences,
    )

    summary = service.summarize("wuxia-demo")

    assert [item.category for item in summary.categories] == [
        AssetCategory.CHARACTER,
        AssetCategory.SCENE,
        AssetCategory.ITEM,
        AssetCategory.ANIMATION,
        AssetCategory.EFFECT,
        AssetCategory.UI,
    ]
    item_activity = summary.categories[2]
    effect_activity = summary.categories[4]
    assert item_activity.task_count == 1
    assert item_activity.recent[0].run_id == "run-item"
    assert effect_activity.task_count == 1
    assert effect_activity.recent[0].run_id == "run-effect"
    assert sequences.list_project_runs("wuxia-demo")[0].run_id == "run-effect"
