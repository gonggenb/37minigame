import json
from pathlib import Path

import pytest
import yaml
from app.schemas.activity import ProjectActivitySummary
from app.schemas.core import (
    AssetCategory,
    AssetTask,
    ConstraintProfile,
    GenerationPlan,
    JobStatus,
    ProjectConfig,
    QualityReport,
)
from app.schemas.production import ProductionRun, StaticAssetRecord
from app.schemas.sequence import SequenceRun, SequenceTask
from pydantic import BaseModel, ValidationError

PLATFORM_ROOT = Path(__file__).resolve().parents[2]
PRESET_ROOT = (
    PLATFORM_ROOT / "shared" / "presets" / "wuxia-ink-chibi-topdown-2_5d"
)


def test_asset_category_is_closed_enum() -> None:
    assert [item.value for item in AssetCategory] == [
        "character",
        "scene",
        "item",
        "animation",
        "effect",
        "ui",
    ]


def test_candidate_count_cannot_exceed_four() -> None:
    with pytest.raises(ValidationError):
        AssetTask(
            asset_id="sword-001",
            category="item",
            name="青锋剑",
            brief="俯视角武侠长剑",
            usage="world_sprite",
            style_pack="wuxia-ink-chibi-topdown-2_5d",
            constraint_profile="item",
            candidate_count=5,
            output_mode="rgba_png",
        )


def test_project_defaults_to_postprocess_transparency() -> None:
    project = ProjectConfig(project_id="wuxia-demo", display_name="武侠项目")

    assert project.generation.transparency_mode == "postprocess"
    assert project.models.image_model == "gpt-image-2"
    assert project.visual_type == "wuxia-ink-chibi-topdown-2_5d"


def test_interrupted_is_a_valid_job_status() -> None:
    assert JobStatus.INTERRUPTED.value == "interrupted"


def test_unknown_fields_are_rejected() -> None:
    with pytest.raises(ValidationError):
        ProjectConfig(
            project_id="wuxia-demo",
            display_name="武侠项目",
            database_url="sqlite:///unexpected.db",
        )


def test_generation_plan_requires_repair_strategy() -> None:
    with pytest.raises(ValidationError):
        GenerationPlan(
            asset_type="item",
            usage="world_sprite",
            selected_reference_ids=[],
            composition="主体居中",
            camera="45度俯视",
            lighting="左上柔光",
            identity_constraints=["青色剑穗"],
            prompt="Q版水墨武侠青锋剑",
            negative_constraints=["无文字"],
            output_spec={"width": 1024, "height": 1024},
            postprocess_steps=["remove_background"],
            quality_checks=["alpha_channel"],
        )


def test_quality_report_blocks_export_when_hard_constraints_fail() -> None:
    report = QualityReport.model_validate(
        {
            "hard_constraints": {
                "passed": False,
                "checks": [
                    {
                        "name": "size",
                        "passed": False,
                        "message": "expected 128x128",
                    }
                ],
            },
            "style_review": {
                "score": 82,
                "identity_score": 85,
                "palette_score": 80,
                "line_style_score": 78,
                "composition_score": 84,
                "issues": [],
                "repair_instruction": "",
            },
            "animation_review": None,
            "export_allowed": False,
        }
    )

    assert report.export_allowed is False


def test_wuxia_preset_matches_project_schema() -> None:
    project_data = yaml.safe_load((PRESET_ROOT / "project.yaml").read_text(encoding="utf-8"))
    style_data = yaml.safe_load((PRESET_ROOT / "style-guide.yaml").read_text(encoding="utf-8"))

    project = ProjectConfig.model_validate(project_data)

    assert project.visual_type == "wuxia-ink-chibi-topdown-2_5d"
    assert project.models.image_model == "gpt-image-2"
    assert project.generation.transparency_mode == "postprocess"
    assert style_data["camera"]["pitch_semantic_min"] == 35
    assert style_data["camera"]["pitch_semantic_max"] == 55
    assert {"pixel_art", "photorealism", "low_poly_3d", "neon_palette"} <= set(
        style_data["forbidden"]
    )


@pytest.mark.parametrize(
    ("filename", "model"),
    [
        ("project-activity.schema.json", ProjectActivitySummary),
        ("project-config.schema.json", ProjectConfig),
        ("asset-task.schema.json", AssetTask),
        ("constraint-profile.schema.json", ConstraintProfile),
        ("generation-plan.schema.json", GenerationPlan),
        ("quality-report.schema.json", QualityReport),
        ("static-asset-record.schema.json", StaticAssetRecord),
        ("production-run.schema.json", ProductionRun),
        ("sequence-task.schema.json", SequenceTask),
        ("sequence-run.schema.json", SequenceRun),
    ],
)
def test_generated_json_schema_matches_model(filename: str, model: type[BaseModel]) -> None:
    schema_path = PLATFORM_ROOT / "shared" / "schemas" / filename

    generated_schema = json.loads(schema_path.read_text(encoding="utf-8"))

    assert generated_schema == model.model_json_schema()
