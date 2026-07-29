from pathlib import Path

import pytest
from app.production.workspace import (
    AssetAlreadyExists,
    ProductionWorkspace,
)
from app.schemas.core import AssetCategory, AssetTask, ProjectConfig
from app.schemas.production import ProductionRun, StaticAssetRecord
from app.workspace.path_guard import PathViolation
from app.workspace.project_workspace import ProjectWorkspace


def _task(asset_id: str = "green-sword") -> AssetTask:
    return AssetTask(
        asset_id=asset_id,
        category=AssetCategory.ITEM,
        name="青锋剑",
        brief="Q 版水墨青锋剑",
        usage="world-sprite",
        style_pack="wuxia-ink-chibi-topdown-2-5d",
        constraint_profile="wuxia-item",
        output_mode="single-png",
    )


def _workspace(tmp_path: Path) -> ProductionWorkspace:
    projects = ProjectWorkspace(tmp_path / "data")
    projects.create_project(
        ProjectConfig(project_id="wuxia-demo", display_name="武侠美术")
    )
    return ProductionWorkspace(projects)


def test_asset_records_are_atomic_and_category_scoped(tmp_path: Path) -> None:
    workspace = _workspace(tmp_path)
    created = workspace.create_asset("wuxia-demo", StaticAssetRecord(task=_task()))

    assert created.task.asset_id == "green-sword"
    assert workspace.get_asset(
        "wuxia-demo", AssetCategory.ITEM, "green-sword"
    ).task.name == "青锋剑"

    with pytest.raises(AssetAlreadyExists):
        workspace.create_asset("wuxia-demo", StaticAssetRecord(task=_task()))

    updated_record = created.model_copy(
        update={"task": created.task.model_copy(update={"name": "青锋长剑"})}
    )
    workspace.update_asset(
        "wuxia-demo", AssetCategory.ITEM, "green-sword", updated_record
    )

    assert workspace.list_assets("wuxia-demo")[0].task.name == "青锋长剑"


def test_run_history_and_candidate_images_are_persisted(tmp_path: Path) -> None:
    workspace = _workspace(tmp_path)
    asset = workspace.create_asset("wuxia-demo", StaticAssetRecord(task=_task()))
    older = ProductionRun(
        run_id="run-older",
        project_id="wuxia-demo",
        task=asset.task,
        status="planned",
    )
    newer = ProductionRun(
        run_id="run-newer",
        project_id="wuxia-demo",
        task=asset.task,
        status="planned",
    )
    workspace.create_run(older)
    workspace.create_run(newer)

    relative = workspace.write_candidate_image(
        newer,
        candidate_id="candidate-0",
        stage="raw",
        content=b"png-bytes",
    )

    assert relative.endswith("runs/run-newer/raw/candidate-0.png")
    assert workspace.read_candidate_image(
        newer, candidate_id="candidate-0", stage="raw"
    ) == b"png-bytes"
    assert [run.run_id for run in workspace.list_runs(
        "wuxia-demo", AssetCategory.ITEM, "green-sword"
    )] == ["run-newer", "run-older"]


def test_workspace_rejects_asset_identity_and_path_mismatches(tmp_path: Path) -> None:
    workspace = _workspace(tmp_path)
    record = StaticAssetRecord(task=_task())

    with pytest.raises(PathViolation):
        workspace.update_asset(
            "wuxia-demo",
            AssetCategory.ITEM,
            "different-id",
            record,
        )

    with pytest.raises(PathViolation):
        workspace.get_asset("wuxia-demo", AssetCategory.ANIMATION, "green-sword")


def test_historical_run_remains_writable_after_asset_task_is_updated(
    tmp_path: Path,
) -> None:
    workspace = _workspace(tmp_path)
    asset = workspace.create_asset("wuxia-demo", StaticAssetRecord(task=_task()))
    run = workspace.create_run(
        ProductionRun(
            run_id="run-history",
            project_id="wuxia-demo",
            task=asset.task,
            status="planned",
        )
    )
    workspace.update_asset(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        asset.model_copy(
            update={"task": asset.task.model_copy(update={"name": "新版青锋剑"})}
        ),
    )

    updated = workspace.update_run(run.model_copy(update={"status": "failed"}))

    assert updated.status == "failed"
