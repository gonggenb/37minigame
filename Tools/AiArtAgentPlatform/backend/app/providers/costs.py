from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

from app.schemas.providers import CostBreakdown, ProjectCostSummary
from app.workspace.atomic_store import read_json
from app.workspace.project_workspace import ProjectWorkspace

from .models import ProviderUsage


@dataclass(slots=True)
class _Bucket:
    request_count: int = 0
    known_cost_usd: float = 0.0
    unknown_cost_count: int = 0

    def add(self, usage: ProviderUsage) -> None:
        self.request_count += 1
        if usage.estimated_cost_usd is None:
            self.unknown_cost_count += 1
        else:
            self.known_cost_usd += usage.estimated_cost_usd

    def schema(self, key: str) -> CostBreakdown:
        return CostBreakdown(
            key=key,
            request_count=self.request_count,
            known_cost_usd=round(self.known_cost_usd, 8),
            unknown_cost_count=self.unknown_cost_count,
        )


class CostAggregator:
    def __init__(self, workspace: ProjectWorkspace) -> None:
        self.workspace = workspace

    def summarize(self, project_id: str) -> ProjectCostSummary:
        self.workspace.get_project(project_id)
        project_path = self.workspace.project_path(project_id)
        total = _Bucket()
        by_model: dict[str, _Bucket] = defaultdict(_Bucket)
        by_category: dict[str, _Bucket] = defaultdict(_Bucket)
        invalid_record_count = 0
        latest_at: datetime | None = None

        for run_path in sorted((project_path / "assets").glob("*/*/runs/*")):
            category = self._category(run_path, project_path)
            records, invalid = self._read_run_records(run_path)
            invalid_record_count += invalid
            for usage in records:
                total.add(usage)
                by_model[usage.model].add(usage)
                by_category[category].add(usage)
                if latest_at is None or usage.created_at > latest_at:
                    latest_at = usage.created_at

        return ProjectCostSummary(
            project_id=project_id,
            request_count=total.request_count,
            known_cost_usd=round(total.known_cost_usd, 8),
            unknown_cost_count=total.unknown_cost_count,
            invalid_record_count=invalid_record_count,
            by_model=[by_model[key].schema(key) for key in sorted(by_model)],
            by_category=[by_category[key].schema(key) for key in sorted(by_category)],
            latest_at=latest_at,
        )

    @staticmethod
    def _category(run_path: Path, project_path: Path) -> str:
        relative = run_path.relative_to(project_path)
        return relative.parts[1] if len(relative.parts) > 1 else "unknown"

    @staticmethod
    def _read_run_records(run_path: Path) -> tuple[list[ProviderUsage], int]:
        history_path = run_path / "cost-history.json"
        cost_path = run_path / "cost.json"
        source = history_path if history_path.is_file() else cost_path
        if not source.is_file():
            return [], 0
        try:
            payload = read_json(source)
        except (OSError, ValueError):
            return [], 1
        items = payload if isinstance(payload, list) else [payload]
        records: list[ProviderUsage] = []
        invalid = 0
        for item in items:
            try:
                records.append(ProviderUsage.model_validate(item))
            except ValueError:
                invalid += 1
        return records, invalid
