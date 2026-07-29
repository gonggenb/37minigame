from collections.abc import Iterator
from io import BytesIO
from pathlib import Path

import pytest
import yaml
from app.constraints.exporter import ImageExporter
from app.constraints.workspace import ConstraintWorkspace
from app.production.context import ProductionContext
from app.production.service import (
    StaticProductionService,
    StyleRiskNotAccepted,
)
from app.production.workspace import ProductionWorkspace
from app.providers.models import GeneratedImage, ProviderCapabilities
from app.schemas.core import (
    AnimationReview,
    AssetCategory,
    AssetTask,
    GenerationPlan,
    HardConstraintReport,
    ImageOutputSpec,
    ProjectConfig,
    QualityReport,
    ReviewFinding,
    StyleReview,
)
from app.schemas.production import (
    CandidateEditRequest,
    CandidateReviewAndRepairRequest,
    CandidateReviewRequest,
    CandidateSelection,
    ProductionExportRequest,
    ProductionGenerateRequest,
    StaticAssetRecord,
)
from app.workspace.project_workspace import ProjectWorkspace
from PIL import Image, ImageDraw


def _png(color: tuple[int, int, int]) -> bytes:
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    ImageDraw.Draw(image).rectangle((16, 8, 47, 55), fill=(*color, 255))
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _task(category: AssetCategory = AssetCategory.ITEM) -> AssetTask:
    return AssetTask(
        asset_id="green-sword",
        category=category,
        name="青锋剑",
        brief="Q 版水墨武侠青锋剑",
        usage="world-sprite",
        style_pack="wuxia-ink-chibi-topdown-2-5d",
        constraint_profile="wuxia-item",
        candidate_count=2,
        output_mode="single-png",
    )


def _plan(category: AssetCategory = AssetCategory.ITEM) -> GenerationPlan:
    return GenerationPlan(
        asset_type=category,
        usage="world-sprite",
        selected_reference_ids=[],
        composition="主体居中",
        camera="2.5D 俯视角",
        lighting="左上柔光",
        identity_constraints=["青色剑穗"],
        prompt="Q 版水墨青锋剑，纯色可分离背景",
        negative_constraints=["文字", "照片写实"],
        output_spec=ImageOutputSpec(width=1024, height=1024),
        postprocess_steps=["背景移除", "Alpha 清理"],
        quality_checks=["主体边界", "统一描边"],
        repair_strategy=["只修正失败维度"],
    )


def _review(
    score: int = 70,
    *,
    findings: list[ReviewFinding] | None = None,
) -> QualityReport:
    return QualityReport(
        hard_constraints=HardConstraintReport(passed=False, checks=[]),
        style_review=StyleReview(
            score=score,
            identity_score=80,
            palette_score=75,
            line_style_score=72,
            composition_score=78,
            issues=["风格分略低"],
            repair_instruction="加强水墨线条",
            findings=findings or [],
        ),
        animation_review=AnimationReview(
            center_drift_px=0,
            size_drift_ratio=0,
            baseline_drift_px=0,
        ),
        export_allowed=False,
    )


class FakeImageProvider:
    def __init__(self) -> None:
        self.generate_requests = []
        self.edit_requests = []

    def capabilities(self) -> ProviderCapabilities:
        return ProviderCapabilities(model="fake-image")

    async def generate(self, request):
        self.generate_requests.append(request)
        colors = [(70, 120, 90), (120, 70, 50)]
        return [
            GeneratedImage(index=index, content=_png(colors[index]))
            for index in range(request.candidate_count)
        ]

    async def edit(self, request):
        self.edit_requests.append(request)
        return [GeneratedImage(index=0, content=_png((40, 100, 130)))]


class FakeReviewProvider:
    def __init__(self, reports: list[QualityReport] | None = None) -> None:
        self.plan_requests = []
        self.review_requests = []
        self.reports = list(reports or [])

    async def plan(self, request):
        self.plan_requests.append(request)
        return _plan(request.task.category)

    async def review(self, request):
        self.review_requests.append(request)
        return self.reports.pop(0) if self.reports else _review()


class FakeRegistry:
    def __init__(self, reports: list[QualityReport] | None = None) -> None:
        self.images = FakeImageProvider()
        self.reviews = FakeReviewProvider(reports)

    def image_provider(self):
        return self.images

    def review_provider(self):
        return self.reviews


class FakeContextBuilder:
    def __init__(self, project: ProjectConfig) -> None:
        self.project = project

    def build(self, project_id: str, task: AssetTask) -> ProductionContext:
        assert project_id == self.project.project_id
        return ProductionContext(
            project=self.project,
            style_guide="Q 版水墨武侠风格圣经",
            reference_descriptions=[],
            reference_images=[],
        )


def _id_factory() -> Iterator[str]:
    yield "run-plan"
    yield "run-edit-1"
    yield "run-edit-2"
    yield "run-edit-3"


def _write_item_preset(preset_dir: Path) -> None:
    target = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "constraints"
    target.mkdir(parents=True)
    profile = {
        "schema_version": 1,
        "profile_id": "wuxia-item",
        "category": "item",
        "master_width": 1024,
        "master_height": 1024,
        "output_width": 64,
        "output_height": 64,
        "require_rgba": True,
        "require_transparency": True,
        "crop_mode": "alpha_bounds",
        "padding_ratio": 0.125,
        "occupancy_ratio": 0.75,
        "resize_algorithm": "nearest",
        "pivot_x": 0.5,
        "pivot_y": 0.5,
        "filename_template": "{asset_id}_{variant}.png",
        "max_file_bytes": 8388608,
        "output_sprite_sheet": False,
        "shared_scale": True,
        "lock_first_frame": False,
    }
    (target / "item.yaml").write_text(
        yaml.safe_dump(profile, sort_keys=False), encoding="utf-8"
    )


def _service(tmp_path: Path, reports: list[QualityReport] | None = None):
    projects = ProjectWorkspace(tmp_path / "data")
    project = projects.create_project(
        ProjectConfig(project_id="wuxia-demo", display_name="武侠美术")
    )
    preset_dir = tmp_path / "presets"
    _write_item_preset(preset_dir)
    production = ProductionWorkspace(projects)
    production.create_asset("wuxia-demo", StaticAssetRecord(task=_task()))
    registry = FakeRegistry(reports)
    ids = _id_factory()
    service = StaticProductionService(
        production,
        ConstraintWorkspace(projects, preset_dir),
        ImageExporter(projects),
        registry,
        FakeContextBuilder(project),
        id_factory=lambda: next(ids),
    )
    return service, production, registry


@pytest.mark.asyncio
async def test_plan_generate_select_review_and_export_static_asset(
    tmp_path: Path,
) -> None:
    service, production, registry = _service(tmp_path)

    planned = await service.plan_asset(
        "wuxia-demo", AssetCategory.ITEM, "green-sword"
    )
    generated = await service.generate_candidates(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        planned.run_id,
        ProductionGenerateRequest(
            candidate_count=2,
            prompt_override="人工修改后的 Q 版水墨青锋剑提示词",
        ),
    )
    selected = service.select_candidate(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        generated.run_id,
        CandidateSelection(candidate_id="candidate-0"),
    )
    reviewed = await service.review_candidate(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        selected.run_id,
        CandidateReviewRequest(candidate_id="candidate-0"),
    )

    assert planned.plan is not None
    assert len(generated.candidates) == 2
    assert generated.prompt == "人工修改后的 Q 版水墨青锋剑提示词"
    assert generated.candidates[0].hard_constraints.passed is True
    assert production.read_candidate_image(
        generated, candidate_id="candidate-0", stage="processed"
    ).startswith(b"\x89PNG")
    assert reviewed.candidates[0].quality_report is not None
    assert reviewed.candidates[0].quality_report.hard_constraints.passed is True
    assert reviewed.candidates[0].quality_report.export_allowed is True
    assert len(registry.images.generate_requests) == 1
    assert (
        registry.images.generate_requests[0].prompt
        == "人工修改后的 Q 版水墨青锋剑提示词"
    )

    with pytest.raises(StyleRiskNotAccepted):
        service.export_selected(
            "wuxia-demo",
            AssetCategory.ITEM,
            "green-sword",
            reviewed.run_id,
            ProductionExportRequest(),
        )

    exported = service.export_selected(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        reviewed.run_id,
        ProductionExportRequest(accept_style_risk=True),
    )
    assert exported.export.filename == "green-sword_default.png"
    assert exported.style_risk_accepted is True


@pytest.mark.asyncio
async def test_edit_candidate_creates_a_traced_child_run(tmp_path: Path) -> None:
    service, _, registry = _service(tmp_path)
    planned = await service.plan_asset(
        "wuxia-demo", AssetCategory.ITEM, "green-sword"
    )
    generated = await service.generate_candidates(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        planned.run_id,
        ProductionGenerateRequest(candidate_count=1),
    )

    edited = await service.edit_candidate(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        generated.run_id,
        CandidateEditRequest(
            candidate_id="candidate-0",
            instruction="只把剑穗改为朱红色",
        ),
    )

    assert edited.source_run_id == generated.run_id
    assert edited.source_candidate_id == "candidate-0"
    assert edited.edit_instruction == "只把剑穗改为朱红色"
    assert len(edited.candidates) == 1
    assert len(registry.images.edit_requests) == 1


@pytest.mark.asyncio
async def test_review_and_repair_uses_comparison_board_and_stops_after_two_edits(
    tmp_path: Path,
) -> None:
    palette = ReviewFinding(
        dimension="palette",
        severity="error",
        summary="配色偏紫",
        evidence="护手出现大面积霓虹紫",
        repair_hint="恢复青绿主体和朱红点缀",
    )
    identity = ReviewFinding(
        dimension="identity",
        severity="error",
        summary="剑穗身份不一致",
        evidence="候选缺少参考中的朱红剑穗",
        repair_hint="只恢复朱红剑穗",
    )
    service, production, registry = _service(
        tmp_path,
        [_review(60, findings=[palette]), _review(68, findings=[identity]), _review(88)],
    )
    planned = await service.plan_asset(
        "wuxia-demo", AssetCategory.ITEM, "green-sword"
    )
    generated = await service.generate_candidates(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        planned.run_id,
        ProductionGenerateRequest(candidate_count=1),
    )

    result = await service.review_and_repair(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        generated.run_id,
        CandidateReviewAndRepairRequest(
            candidate_id="candidate-0",
            automatic_repair=True,
            max_retries=2,
        ),
    )

    assert result.auto_repair_summary is not None
    assert result.auto_repair_summary.retry_count == 2
    assert result.auto_repair_summary.stop_reason == "passed"
    assert len(result.auto_repair_summary.attempts) == 3
    assert len(registry.images.edit_requests) == 2
    assert len(registry.reviews.review_requests) == 3
    assert all(
        request.comparison_png is not None
        for request in registry.reviews.review_requests
    )
    for attempt in result.auto_repair_summary.attempts:
        comparison = production.workspace.project_path("wuxia-demo") / (
            attempt.comparison_relative_path
        )
        assert comparison.read_bytes().startswith(b"\x89PNG")


@pytest.mark.asyncio
async def test_review_and_repair_does_not_edit_without_explicit_failure_reason(
    tmp_path: Path,
) -> None:
    report = _review(60).model_copy(
        update={
            "style_review": _review(60).style_review.model_copy(
                update={"issues": [], "repair_instruction": "", "findings": []}
            )
        }
    )
    service, _, registry = _service(tmp_path, [report])
    planned = await service.plan_asset(
        "wuxia-demo", AssetCategory.ITEM, "green-sword"
    )
    generated = await service.generate_candidates(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        planned.run_id,
        ProductionGenerateRequest(candidate_count=1),
    )

    result = await service.review_and_repair(
        "wuxia-demo",
        AssetCategory.ITEM,
        "green-sword",
        generated.run_id,
        CandidateReviewAndRepairRequest(candidate_id="candidate-0"),
    )

    assert result.auto_repair_summary is not None
    assert result.auto_repair_summary.stop_reason == "no-actionable-failure"
    assert result.auto_repair_summary.retry_count == 0
    assert registry.images.edit_requests == []
