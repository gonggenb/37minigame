from __future__ import annotations

from collections import defaultdict

from app.production.sequence_service import SequenceProductionService
from app.production.workspace import ProductionWorkspace
from app.schemas.activity import (
    ProjectActivityItem,
    ProjectActivitySummary,
    ProjectCategoryActivity,
)
from app.schemas.core import AssetCategory
from app.style_pack.references import ReferenceCatalog
from app.workspace.project_workspace import ProjectWorkspace

CATEGORY_ORDER = (
    AssetCategory.CHARACTER,
    AssetCategory.SCENE,
    AssetCategory.ITEM,
    AssetCategory.ANIMATION,
    AssetCategory.EFFECT,
    AssetCategory.UI,
)


class ProjectActivityService:
    def __init__(
        self,
        projects: ProjectWorkspace,
        references: ReferenceCatalog,
        production: ProductionWorkspace,
        sequences: SequenceProductionService,
    ) -> None:
        self.projects = projects
        self.references = references
        self.production = production
        self.sequences = sequences

    def summarize(self, project_id: str) -> ProjectActivitySummary:
        self.projects.get_project(project_id)
        items: dict[AssetCategory, list[ProjectActivityItem]] = defaultdict(list)
        counts: dict[AssetCategory, set[str]] = defaultdict(set)

        for asset in self.production.list_assets(project_id):
            runs = self.production.list_runs(
                project_id,
                asset.task.category,
                asset.task.asset_id,
            )
            latest = runs[0] if runs else None
            counts[asset.task.category].add(asset.task.asset_id)
            items[asset.task.category].append(
                ProjectActivityItem(
                    workflow="static",
                    category=asset.task.category,
                    asset_id=asset.task.asset_id,
                    name=asset.task.name,
                    status=latest.status if latest else "draft",
                    run_id=latest.run_id if latest else None,
                    updated_at=latest.updated_at if latest else asset.updated_at,
                )
            )

        for run in self.sequences.list_project_runs(project_id):
            counts[run.task.category].add(run.task.asset_id)
            items[run.task.category].append(
                ProjectActivityItem(
                    workflow="sequence",
                    category=run.task.category,
                    asset_id=run.task.asset_id,
                    name=run.task.name,
                    status=run.status,
                    run_id=run.run_id,
                    updated_at=run.updated_at,
                )
            )

        categories = [
            ProjectCategoryActivity(
                category=category,
                task_count=len(counts[category]),
                recent=sorted(
                    items[category],
                    key=lambda item: item.updated_at,
                    reverse=True,
                )[:5],
            )
            for category in CATEGORY_ORDER
        ]
        return ProjectActivitySummary(
            project_id=project_id,
            reference_count=self.references.count_references(project_id),
            categories=categories,
        )
