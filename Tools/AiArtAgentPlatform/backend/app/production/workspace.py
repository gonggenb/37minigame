from __future__ import annotations

import hashlib
from pathlib import Path
from typing import Literal

from app.schemas.core import AssetCategory
from app.schemas.production import ProductionRun, StaticAssetRecord, utc_now
from app.workspace.atomic_store import (
    atomic_write_bytes,
    atomic_write_json,
    atomic_write_yaml,
    read_json,
    read_yaml,
)
from app.workspace.path_guard import PathViolation, safe_child, validate_slug
from app.workspace.project_workspace import ProjectWorkspace


class AssetAlreadyExists(FileExistsError):
    """静态资产任务已经存在。"""


class ProductionWorkspace:
    STATIC_CATEGORIES = frozenset(
        {
            AssetCategory.ITEM,
            AssetCategory.UI,
            AssetCategory.CHARACTER,
            AssetCategory.SCENE,
        }
    )

    def __init__(self, workspace: ProjectWorkspace) -> None:
        self.workspace = workspace

    def asset_path(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
    ) -> Path:
        self._require_static_category(category)
        validate_slug(asset_id)
        self.workspace.get_project(project_id)
        return safe_child(
            self.workspace.project_path(project_id),
            "assets",
            category.value,
            asset_id,
        )

    def asset_file(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
    ) -> Path:
        return safe_child(self.asset_path(project_id, category, asset_id), "task.yaml")

    def create_asset(
        self,
        project_id: str,
        record: StaticAssetRecord,
    ) -> StaticAssetRecord:
        path = self.asset_file(project_id, record.task.category, record.task.asset_id)
        if path.exists():
            raise AssetAlreadyExists(record.task.asset_id)
        path.parent.mkdir(parents=True, exist_ok=True)
        atomic_write_yaml(path, record.model_dump(mode="json"))
        return record

    def update_asset(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        record: StaticAssetRecord,
    ) -> StaticAssetRecord:
        if record.task.category != category or record.task.asset_id != asset_id:
            raise PathViolation("asset category and id must match the request path")
        path = self.asset_file(project_id, category, asset_id)
        if not path.is_file():
            raise FileNotFoundError(asset_id)
        current = self.get_asset(project_id, category, asset_id)
        updated = record.model_copy(
            update={"created_at": current.created_at, "updated_at": utc_now()}
        )
        atomic_write_yaml(path, updated.model_dump(mode="json"))
        return updated

    def get_asset(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
    ) -> StaticAssetRecord:
        path = self.asset_file(project_id, category, asset_id)
        if not path.is_file():
            raise FileNotFoundError(asset_id)
        payload = read_yaml(path)
        if not isinstance(payload, dict):
            raise ValueError("task.yaml must contain a mapping")
        record = StaticAssetRecord.model_validate(payload)
        if record.task.category != category or record.task.asset_id != asset_id:
            raise PathViolation("stored asset identity does not match its directory")
        return record

    def list_assets(self, project_id: str) -> list[StaticAssetRecord]:
        project_path = self.workspace.project_path(project_id)
        self.workspace.get_project(project_id)
        records: list[StaticAssetRecord] = []
        for category in sorted(self.STATIC_CATEGORIES, key=lambda item: item.value):
            category_path = safe_child(project_path, "assets", category.value)
            if not category_path.is_dir():
                continue
            for task_file in sorted(category_path.glob("*/task.yaml")):
                try:
                    records.append(
                        self.get_asset(project_id, category, task_file.parent.name)
                    )
                except (FileNotFoundError, PathViolation, ValueError):
                    continue
        return sorted(records, key=lambda item: item.created_at, reverse=True)

    def run_path(self, run: ProductionRun) -> Path:
        self.get_asset(run.project_id, run.task.category, run.task.asset_id)
        validate_slug(run.run_id)
        return safe_child(
            self.asset_path(run.project_id, run.task.category, run.task.asset_id),
            "runs",
            run.run_id,
        )

    def run_file(self, run: ProductionRun) -> Path:
        return safe_child(self.run_path(run), "run.json")

    def create_run(self, run: ProductionRun) -> ProductionRun:
        path = self.run_file(run)
        if path.exists():
            raise FileExistsError(run.run_id)
        for directory in (path.parent, path.parent / "raw", path.parent / "processed"):
            directory.mkdir(parents=True, exist_ok=True)
        atomic_write_json(path, run.model_dump(mode="json"))
        return run

    def get_run(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
    ) -> ProductionRun:
        validate_slug(run_id)
        asset_path = self.asset_path(project_id, category, asset_id)
        path = safe_child(asset_path, "runs", run_id, "run.json")
        if not path.is_file():
            raise FileNotFoundError(run_id)
        run = ProductionRun.model_validate(read_json(path))
        if (
            run.project_id != project_id
            or run.task.category != category
            or run.task.asset_id != asset_id
            or run.run_id != run_id
        ):
            raise PathViolation("stored run identity does not match its directory")
        return run

    def update_run(self, run: ProductionRun) -> ProductionRun:
        path = self.run_file(run)
        if not path.is_file():
            raise FileNotFoundError(run.run_id)
        updated = run.model_copy(update={"updated_at": utc_now()})
        atomic_write_json(path, updated.model_dump(mode="json"))
        return updated

    def list_runs(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
    ) -> list[ProductionRun]:
        runs_path = safe_child(self.asset_path(project_id, category, asset_id), "runs")
        if not runs_path.is_dir():
            return []
        runs: list[ProductionRun] = []
        for run_file in sorted(runs_path.glob("*/run.json")):
            try:
                runs.append(
                    self.get_run(project_id, category, asset_id, run_file.parent.name)
                )
            except (FileNotFoundError, PathViolation, ValueError):
                continue
        return sorted(runs, key=lambda item: item.created_at, reverse=True)

    def write_candidate_image(
        self,
        run: ProductionRun,
        *,
        candidate_id: str,
        stage: Literal["raw", "processed"],
        content: bytes,
    ) -> str:
        self._validate_candidate_id(candidate_id)
        path = safe_child(self.run_path(run), stage, f"{candidate_id}.png")
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)
        return path.relative_to(self.workspace.project_path(run.project_id)).as_posix()

    def read_candidate_image(
        self,
        run: ProductionRun,
        *,
        candidate_id: str,
        stage: Literal["raw", "processed"],
    ) -> bytes:
        self._validate_candidate_id(candidate_id)
        path = safe_child(self.run_path(run), stage, f"{candidate_id}.png")
        if not path.is_file():
            raise FileNotFoundError(candidate_id)
        return path.read_bytes()

    def write_review_image(
        self,
        run: ProductionRun,
        *,
        candidate_id: str,
        content: bytes,
    ) -> str:
        self._validate_candidate_id(candidate_id)
        path = safe_child(
            self.run_path(run),
            "reviews",
            candidate_id,
            "comparison.png",
        )
        atomic_write_bytes(path, content)
        return path.relative_to(self.workspace.project_path(run.project_id)).as_posix()

    def read_review_image(
        self,
        run: ProductionRun,
        *,
        candidate_id: str,
    ) -> bytes:
        self._validate_candidate_id(candidate_id)
        path = safe_child(
            self.run_path(run),
            "reviews",
            candidate_id,
            "comparison.png",
        )
        if not path.is_file():
            raise FileNotFoundError(candidate_id)
        return path.read_bytes()

    def write_review_json(
        self,
        run: ProductionRun,
        *,
        candidate_id: str,
        filename: Literal["review.json", "repair-plan.json"],
        payload: dict[str, object],
    ) -> str:
        self._validate_candidate_id(candidate_id)
        path = safe_child(self.run_path(run), "reviews", candidate_id, filename)
        atomic_write_json(path, payload)
        return path.relative_to(self.workspace.project_path(run.project_id)).as_posix()

    def write_candidate_mask(
        self,
        run: ProductionRun,
        *,
        candidate_id: str,
        content: bytes,
    ) -> str:
        self._validate_candidate_id(candidate_id)
        digest = hashlib.sha256(content).hexdigest()
        path = safe_child(
            self.run_path(run),
            "masks",
            f"{candidate_id}-{digest[:12]}.png",
        )
        atomic_write_bytes(path, content)
        return path.relative_to(self.workspace.project_path(run.project_id)).as_posix()

    @classmethod
    def _require_static_category(cls, category: AssetCategory) -> None:
        if category not in cls.STATIC_CATEGORIES:
            raise PathViolation("stage 6 only supports static asset categories")

    @staticmethod
    def _validate_candidate_id(candidate_id: str) -> None:
        if candidate_id not in {f"candidate-{index}" for index in range(4)}:
            raise PathViolation("invalid candidate id")
