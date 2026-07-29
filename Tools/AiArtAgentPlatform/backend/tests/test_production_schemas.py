import pytest
from app.schemas.core import (
    AssetCategory,
    AssetTask,
    HardConstraintReport,
)
from app.schemas.image_tools import ExportRecord, ProcessedImageMetadata
from app.schemas.production import (
    AutoRepairSummary,
    ProductionCandidate,
    ProductionExportResult,
    ProductionGenerateRequest,
    ProductionRun,
    ReviewAttempt,
    StaticAssetRecord,
)
from pydantic import ValidationError


def _task(category: AssetCategory = AssetCategory.ITEM) -> AssetTask:
    return AssetTask(
        asset_id="green-sword",
        category=category,
        name="青锋剑",
        brief="Q 版水墨武侠青锋剑",
        usage="world-sprite",
        style_pack="wuxia-ink-chibi-topdown-2-5d",
        constraint_profile="wuxia-item",
        output_mode="single-png",
    )


def _candidate(candidate_id: str = "candidate-0") -> ProductionCandidate:
    return ProductionCandidate(
        candidate_id=candidate_id,
        index=0,
        raw_relative_path="assets/item/green-sword/runs/run-1/raw/candidate-0.png",
        processed_relative_path=(
            "assets/item/green-sword/runs/run-1/processed/candidate-0.png"
        ),
        metadata=ProcessedImageMetadata(
            width=128,
            height=128,
            source_alpha_bounds=(0, 0, 64, 64),
            alpha_bounds=(16, 16, 112, 112),
            scale=1.5,
            sha256="a" * 64,
            file_bytes=256,
        ),
        hard_constraints=HardConstraintReport(passed=True, checks=[]),
    )


def test_static_asset_record_rejects_animation_and_effect_categories() -> None:
    for category in (AssetCategory.ANIMATION, AssetCategory.EFFECT):
        with pytest.raises(ValidationError):
            StaticAssetRecord(task=_task(category))


def test_generation_request_limits_candidate_count_to_four() -> None:
    assert ProductionGenerateRequest(candidate_count=4).candidate_count == 4

    with pytest.raises(ValidationError):
        ProductionGenerateRequest(candidate_count=5)


def test_run_selection_must_reference_an_existing_candidate() -> None:
    with pytest.raises(ValidationError):
        ProductionRun(
            run_id="run-1",
            project_id="wuxia-demo",
            task=_task(),
            status="selected",
            candidates=[_candidate()],
            selected_candidate_id="candidate-9",
        )


def test_low_style_score_export_requires_explicit_risk_acceptance() -> None:
    export_record = ExportRecord(
        project_id="wuxia-demo",
        asset_id="green-sword",
        category=AssetCategory.ITEM,
        variant="default",
        filename="green-sword_default.png",
        relative_path=(
            "assets/item/green-sword/exports/green-sword_default.png"
        ),
        sha256="a" * 64,
        written_sha256="a" * 64,
        file_bytes=256,
        hard_constraints=HardConstraintReport(passed=True, checks=[]),
    )

    with pytest.raises(ValidationError):
        ProductionExportResult(
            export=export_record,
            style_score=70,
            minimum_style_score=75,
            style_risk_accepted=False,
        )

    accepted = ProductionExportResult(
        export=export_record,
        style_score=70,
        minimum_style_score=75,
        style_risk_accepted=True,
    )
    assert accepted.style_risk_accepted is True


def test_auto_repair_summary_limits_model_edits_to_two() -> None:
    attempts = [
        ReviewAttempt(
            attempt_index=index,
            run_id=f"run-{index}",
            candidate_id="candidate-0",
            comparison_relative_path=f"reviews/{index}/comparison.png",
        )
        for index in range(3)
    ]

    summary = AutoRepairSummary(
        retry_count=2,
        max_retries=2,
        stop_reason="retry-limit-reached",
        attempts=attempts,
    )

    assert summary.retry_count == 2

    with pytest.raises(ValidationError):
        AutoRepairSummary(
            retry_count=3,
            max_retries=2,
            stop_reason="retry-limit-reached",
            attempts=attempts,
        )
