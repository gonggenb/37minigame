import json
import os
from pathlib import Path
from typing import Final

from pydantic import BaseModel

from app.schemas.activity import ProjectActivitySummary
from app.schemas.core import (
    AssetTask,
    ConstraintProfile,
    GenerationPlan,
    ProjectConfig,
    QualityReport,
)
from app.schemas.production import ProductionRun, StaticAssetRecord
from app.schemas.sequence import SequenceRun, SequenceTask

PLATFORM_ROOT: Final = Path(__file__).resolve().parents[3]
SCHEMA_DIR: Final = PLATFORM_ROOT / "shared" / "schemas"
SCHEMA_MODELS: Final[dict[str, type[BaseModel]]] = {
    "project-activity.schema.json": ProjectActivitySummary,
    "project-config.schema.json": ProjectConfig,
    "asset-task.schema.json": AssetTask,
    "constraint-profile.schema.json": ConstraintProfile,
    "generation-plan.schema.json": GenerationPlan,
    "quality-report.schema.json": QualityReport,
    "static-asset-record.schema.json": StaticAssetRecord,
    "production-run.schema.json": ProductionRun,
    "sequence-task.schema.json": SequenceTask,
    "sequence-run.schema.json": SequenceRun,
}


def write_schema(path: Path, model: type[BaseModel]) -> None:
    payload = json.dumps(
        model.model_json_schema(),
        ensure_ascii=False,
        indent=2,
        sort_keys=True,
    )
    temporary_path = path.with_suffix(f"{path.suffix}.tmp")
    temporary_path.write_text(f"{payload}\n", encoding="utf-8", newline="\n")
    os.replace(temporary_path, path)


def export_schemas() -> None:
    SCHEMA_DIR.mkdir(parents=True, exist_ok=True)
    for filename, model in SCHEMA_MODELS.items():
        write_schema(SCHEMA_DIR / filename, model)


if __name__ == "__main__":
    export_schemas()
