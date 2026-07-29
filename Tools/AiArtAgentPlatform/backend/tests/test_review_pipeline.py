from io import BytesIO

from app.providers.models import ImageInput
from app.review.comparison import ComparisonBoardBuilder
from app.review.repair import RepairPlanner
from app.schemas.core import (
    HardConstraintCheck,
    HardConstraintReport,
    QualityReport,
    ReviewFinding,
    StyleReview,
)
from PIL import Image, ImageDraw


def _png(color: tuple[int, int, int], *, size: tuple[int, int] = (96, 64)) -> bytes:
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(image).rounded_rectangle(
        (12, 8, size[0] - 13, size[1] - 9),
        radius=8,
        fill=(*color, 255),
    )
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _report(
    *,
    score: int,
    findings: list[ReviewFinding] | None = None,
    issues: list[str] | None = None,
    hard_passed: bool = True,
) -> QualityReport:
    checks = []
    if not hard_passed:
        checks.append(
            HardConstraintCheck(
                name="transparent_background",
                passed=False,
                message="背景仍包含不透明像素",
            )
        )
    return QualityReport(
        hard_constraints=HardConstraintReport(passed=hard_passed, checks=checks),
        style_review=StyleReview(
            score=score,
            identity_score=score,
            palette_score=score,
            line_style_score=score,
            composition_score=score,
            issues=issues or [],
            findings=findings or [],
        ),
        export_allowed=hard_passed,
    )


def test_comparison_board_is_stable_and_contains_candidate_and_references() -> None:
    candidate = _png((62, 112, 86))
    references = [
        ImageInput(filename="ref-a.png", content=_png((120, 70, 55))),
        ImageInput(filename="ref-b.png", content=_png((60, 85, 125))),
    ]

    first = ComparisonBoardBuilder.build(candidate, references)
    second = ComparisonBoardBuilder.build(candidate, references)

    assert first == second
    with Image.open(BytesIO(first)) as board:
        assert board.format == "PNG"
        assert board.mode == "RGBA"
        assert board.width > board.height


def test_comparison_board_still_works_without_references() -> None:
    content = ComparisonBoardBuilder.build(_png((62, 112, 86)), [])

    with Image.open(BytesIO(content)) as board:
        assert board.width > 0
        assert board.height > 0


def test_repair_planner_targets_only_actionable_style_failures() -> None:
    report = _report(
        score=66,
        findings=[
            ReviewFinding(
                dimension="palette",
                severity="error",
                summary="高饱和紫色偏离项目配色",
                evidence="剑穗和护手出现大面积霓虹紫",
                repair_hint="改为朱红点缀和青绿色主体",
            ),
            ReviewFinding(
                dimension="composition",
                severity="info",
                summary="主体轮廓清晰",
                evidence="缩略图下仍可辨认",
                actionable=False,
            ),
        ],
    )

    repair = RepairPlanner.plan(report, minimum_style_score=75, retry_index=0)

    assert repair.action == "edit"
    assert repair.retry_allowed is True
    assert repair.target_dimensions == ["palette"]
    assert "霓虹紫" in repair.prompt
    assert "保持" in repair.prompt


def test_repair_planner_stops_when_low_score_has_no_explicit_reason() -> None:
    repair = RepairPlanner.plan(
        _report(score=60),
        minimum_style_score=75,
        retry_index=0,
    )

    assert repair.action == "none"
    assert repair.retry_allowed is False
    assert repair.stop_reason == "no-actionable-failure"


def test_repair_planner_does_not_send_hard_constraint_failures_to_image_model() -> None:
    repair = RepairPlanner.plan(
        _report(score=90, hard_passed=False),
        minimum_style_score=75,
        retry_index=0,
    )

    assert repair.action == "manual"
    assert repair.retry_allowed is False
    assert repair.target_dimensions == ["hard_constraint"]
    assert repair.stop_reason == "manual-review-required"


def test_repair_planner_enforces_two_retry_limit() -> None:
    finding = ReviewFinding(
        dimension="identity",
        severity="error",
        summary="角色发冠身份丢失",
        evidence="候选没有参考图中的青玉发冠",
        repair_hint="恢复青玉发冠，不改变姿态",
    )

    repair = RepairPlanner.plan(
        _report(score=68, findings=[finding]),
        minimum_style_score=75,
        retry_index=2,
    )

    assert repair.action == "none"
    assert repair.retry_allowed is False
    assert repair.stop_reason == "retry-limit-reached"
