from __future__ import annotations

import hashlib
import uuid
from collections.abc import Callable
from pathlib import Path
from typing import Protocol

from app.constraints.exporter import ImageExporter
from app.constraints.validator import ConstraintValidator
from app.constraints.workspace import ConstraintWorkspace
from app.image_processing.editor import CandidateImageEditor
from app.image_processing.pipeline import ImageProcessor
from app.providers.base import ImageProvider, ReviewProvider
from app.providers.models import (
    EditRequest,
    GeneratedImage,
    GenerateRequest,
    ImageInput,
    PlanningRequest,
    ProviderTrace,
    ReviewRequest,
)
from app.review.comparison import ComparisonBoardBuilder
from app.review.repair import RepairPlanner
from app.schemas.core import AssetCategory
from app.schemas.editor import (
    CandidateMaskRecord,
    CandidateMaskRequest,
    CandidateTransformRequest,
)
from app.schemas.image_tools import BackgroundRemovalConfig
from app.schemas.production import (
    AutoRepairSummary,
    CandidateEditRequest,
    CandidateReviewAndRepairRequest,
    CandidateReviewRequest,
    CandidateSelection,
    ProductionCandidate,
    ProductionExportRequest,
    ProductionExportResult,
    ProductionGenerateRequest,
    ProductionRun,
    RepairPlan,
    ReviewAttempt,
)
from app.workspace.atomic_store import atomic_write_json
from app.workspace.path_guard import PathViolation, safe_child

from .context import ProductionContextBuilder
from .workspace import ProductionWorkspace


class ProviderRegistry(Protocol):
    def image_provider(self) -> ImageProvider: ...

    def review_provider(self) -> ReviewProvider: ...


class ProductionStateError(ValueError):
    """资产运行记录不满足当前操作的前置条件。"""


class CandidateNotFound(FileNotFoundError):
    """候选不存在。"""


class StyleRiskNotAccepted(ProductionStateError):
    """低于风格阈值的候选尚未获得人工风险接受。"""


class StaticProductionService:
    def __init__(
        self,
        production: ProductionWorkspace,
        constraints: ConstraintWorkspace,
        exporter: ImageExporter,
        providers: ProviderRegistry,
        context_builder: ProductionContextBuilder,
        *,
        id_factory: Callable[[], str] | None = None,
    ) -> None:
        self.production = production
        self.constraints = constraints
        self.exporter = exporter
        self.providers = providers
        self.context_builder = context_builder
        self.id_factory = id_factory or (lambda: f"run-{uuid.uuid4().hex[:12]}")

    async def plan_asset(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
    ) -> ProductionRun:
        asset = self.production.get_asset(project_id, category, asset_id)
        run_id = self.id_factory()
        context = self.context_builder.build(project_id, asset.task)
        trace = ProviderTrace(
            project_id=project_id,
            category=category,
            asset_id=asset_id,
            run_id=run_id,
        )
        plan = await self.providers.review_provider().plan(
            PlanningRequest(
                project=context.project,
                task=asset.task,
                style_guide=context.style_guide,
                reference_descriptions=context.reference_descriptions,
                trace=trace,
            )
        )
        if plan.asset_type is not category:
            raise ProductionStateError("generation plan category must match the asset")
        run = ProductionRun(
            run_id=run_id,
            project_id=project_id,
            task=asset.task,
            status="planned",
            plan=plan,
            prompt=plan.prompt,
        )
        self.production.create_run(run)
        self._write_run_artifacts(run)
        return run

    async def generate_candidates(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: ProductionGenerateRequest,
    ) -> ProductionRun:
        run = self.production.get_run(project_id, category, asset_id, run_id)
        if run.plan is None:
            raise ProductionStateError("asset must be planned before generation")
        if request.prompt_override is not None:
            run = self.production.update_run(
                run.model_copy(update={"prompt": request.prompt_override})
            )
            self._write_run_artifacts(run)
        context = self.context_builder.build(project_id, run.task)
        profile = self.constraints.resolve(project_id, run.task)
        candidate_count = min(
            request.candidate_count,
            run.task.candidate_count,
            self.providers.image_provider().capabilities().max_candidates,
        )
        trace = self._trace(run)
        if context.reference_images:
            images = await self.providers.image_provider().edit(
                EditRequest(
                    prompt=run.prompt,
                    images=context.reference_images,
                    width=profile.master_width,
                    height=profile.master_height,
                    candidate_count=candidate_count,
                    quality=context.project.generation.image_quality,
                    background="opaque",
                    trace=trace,
                )
            )
        else:
            images = await self.providers.image_provider().generate(
                GenerateRequest(
                    prompt=run.prompt,
                    width=profile.master_width,
                    height=profile.master_height,
                    candidate_count=candidate_count,
                    quality=context.project.generation.image_quality,
                    background="opaque",
                    trace=trace,
                )
            )
        candidates = self._process_images(run, images)
        return self.production.update_run(
            run.model_copy(update={"status": "generated", "candidates": candidates})
        )

    def select_candidate(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: CandidateSelection,
    ) -> ProductionRun:
        run = self.production.get_run(project_id, category, asset_id, run_id)
        self._candidate(run, request.candidate_id)
        return self.production.update_run(
            run.model_copy(
                update={
                    "status": "selected",
                    "selected_candidate_id": request.candidate_id,
                }
            )
        )

    async def edit_candidate(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: CandidateEditRequest,
    ) -> ProductionRun:
        source_run = self.production.get_run(project_id, category, asset_id, run_id)
        source_candidate = self._candidate(source_run, request.candidate_id)
        if source_run.plan is None:
            raise ProductionStateError("source run does not contain a generation plan")
        profile = self.constraints.resolve(project_id, source_run.task)
        child = ProductionRun(
            run_id=self.id_factory(),
            project_id=project_id,
            task=source_run.task,
            status="planned",
            plan=source_run.plan,
            prompt=f"{source_run.prompt}\n\n定向编辑：{request.instruction}",
            source_run_id=source_run.run_id,
            source_candidate_id=source_candidate.candidate_id,
            edit_instruction=request.instruction,
            review_attempts=source_run.review_attempts,
        )
        self.production.create_run(child)
        self._write_run_artifacts(child)
        source_content = self.production.read_candidate_image(
            source_run,
            candidate_id=source_candidate.candidate_id,
            stage="processed",
        )
        mask = (
            self._workspace_image(project_id, request.mask_workspace_relative_path)
            if request.mask_workspace_relative_path is not None
            else None
        )
        context = self.context_builder.build(project_id, source_run.task)
        images = await self.providers.image_provider().edit(
            EditRequest(
                prompt=child.prompt,
                images=[
                    ImageInput(
                        filename=f"{source_candidate.candidate_id}.png",
                        content=source_content,
                    )
                ],
                mask=mask,
                width=profile.master_width,
                height=profile.master_height,
                candidate_count=request.candidate_count,
                quality=context.project.generation.image_quality,
                background="opaque",
                trace=self._trace(child),
            )
        )
        candidates = self._process_images(child, images)
        return self.production.update_run(
            child.model_copy(update={"status": "generated", "candidates": candidates})
        )

    def transform_candidate(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: CandidateTransformRequest,
    ) -> ProductionRun:
        source_run = self.production.get_run(project_id, category, asset_id, run_id)
        source_candidate = self._candidate(source_run, request.candidate_id)
        source_content = self.production.read_candidate_image(
            source_run,
            candidate_id=source_candidate.candidate_id,
            stage="processed",
        )
        transformed_content = (
            CandidateImageEditor.crop(source_content, request.crop)
            if request.crop is not None
            else source_content
        )
        overrides = dict(source_run.task.constraint_overrides)
        if request.output_width is not None and request.output_height is not None:
            overrides.update(
                {
                    "output_width": request.output_width,
                    "output_height": request.output_height,
                }
            )
        if request.padding_ratio is not None:
            overrides["padding_ratio"] = request.padding_ratio
        task = source_run.task.model_copy(update={"constraint_overrides": overrides})
        child = ProductionRun(
            run_id=self.id_factory(),
            project_id=project_id,
            task=task,
            status="planned",
            plan=source_run.plan,
            prompt=source_run.prompt,
            source_run_id=source_run.run_id,
            source_candidate_id=source_candidate.candidate_id,
            edit_instruction="本地裁切、缩放、透明留白或背景透明化",
            review_attempts=source_run.review_attempts,
        )
        self.production.create_run(child)
        self._write_run_artifacts(child)
        background = BackgroundRemovalConfig(
            mode="corner_flood" if request.remove_background else "preserve"
        )
        candidates = self._process_images(
            child,
            [GeneratedImage(index=0, content=transformed_content)],
            background_override=background,
        )
        return self.production.update_run(
            child.model_copy(
                update={
                    "status": "selected",
                    "candidates": candidates,
                    "selected_candidate_id": "candidate-0",
                }
            )
        )

    def save_candidate_mask(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        candidate_id: str,
        request: CandidateMaskRequest,
    ) -> CandidateMaskRecord:
        run = self.production.get_run(project_id, category, asset_id, run_id)
        self._candidate(run, candidate_id)
        candidate_content = self.production.read_candidate_image(
            run,
            candidate_id=candidate_id,
            stage="processed",
        )
        expected_size = CandidateImageEditor.size(candidate_content)
        decoded = CandidateImageEditor.decode_base64_png(request.mask_png_base64)
        normalized = CandidateImageEditor.normalize_mask(
            decoded,
            expected_size=expected_size,
        )
        relative_path = self.production.write_candidate_mask(
            run,
            candidate_id=candidate_id,
            content=normalized,
        )
        return CandidateMaskRecord(
            workspace_relative_path=relative_path,
            width=expected_size[0],
            height=expected_size[1],
            sha256=hashlib.sha256(normalized).hexdigest(),
        )

    async def review_candidate(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: CandidateReviewRequest,
    ) -> ProductionRun:
        run = self.production.get_run(project_id, category, asset_id, run_id)
        reviewed, _, _ = await self._review_once(
            run,
            request.candidate_id,
            retry_index=0,
            history=[],
        )
        return reviewed

    async def review_and_repair(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: CandidateReviewAndRepairRequest,
    ) -> ProductionRun:
        current = self.production.get_run(project_id, category, asset_id, run_id)
        attempts: list[ReviewAttempt] = []
        current, attempt, repair = await self._review_once(
            current,
            request.candidate_id,
            retry_index=0,
            history=attempts,
        )
        attempts.append(attempt)
        retry_count = 0
        project = self.production.workspace.get_project(project_id)
        max_retries = min(
            request.max_retries,
            project.generation.automatic_retry_count,
            RepairPlanner.MAX_RETRIES,
        )

        if request.automatic_repair:
            while repair.retry_allowed and retry_count < max_retries:
                child = await self.edit_candidate(
                    project_id,
                    category,
                    asset_id,
                    current.run_id,
                    CandidateEditRequest(
                        candidate_id=attempt.candidate_id,
                        instruction=repair.prompt,
                        candidate_count=1,
                    ),
                )
                child = self.select_candidate(
                    project_id,
                    category,
                    asset_id,
                    child.run_id,
                    CandidateSelection(candidate_id="candidate-0"),
                )
                retry_count += 1
                current, attempt, repair = await self._review_once(
                    child,
                    "candidate-0",
                    retry_index=retry_count,
                    history=attempts,
                )
                attempts.append(attempt)

        stop_reason = repair.stop_reason
        if not request.automatic_repair:
            stop_reason = "disabled"
        elif repair.retry_allowed and retry_count >= max_retries:
            stop_reason = "retry-limit-reached"
        if stop_reason is None:
            stop_reason = "manual-review-required"
        summary = AutoRepairSummary(
            retry_count=retry_count,
            max_retries=max_retries,
            stop_reason=stop_reason,
            attempts=attempts,
        )
        return self.production.update_run(
            current.model_copy(
                update={
                    "review_attempts": attempts,
                    "auto_repair_summary": summary,
                }
            )
        )

    def export_selected(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: ProductionExportRequest,
    ) -> ProductionExportResult:
        run = self.production.get_run(project_id, category, asset_id, run_id)
        if run.selected_candidate_id is None:
            raise ProductionStateError("select a candidate before export")
        candidate = self._candidate(run, run.selected_candidate_id)
        if candidate.quality_report is None:
            raise ProductionStateError("review the selected candidate before export")
        if not candidate.hard_constraints.passed:
            raise ProductionStateError("hard constraints must pass before export")
        project = self.production.workspace.get_project(project_id)
        style_score = candidate.quality_report.style_review.score
        minimum = project.review.minimum_style_score
        if style_score < minimum and not request.accept_style_risk:
            raise StyleRiskNotAccepted(
                "style score is below the project threshold; explicit acceptance required"
            )
        content = self.production.read_candidate_image(
            run,
            candidate_id=candidate.candidate_id,
            stage="processed",
        )
        profile = self.constraints.resolve(project_id, run.task)
        export = self.exporter.export(
            project_id,
            category,
            asset_id,
            request.variant,
            content,
            profile,
        )
        self.production.update_run(
            run.model_copy(update={"status": "exported", "export": export})
        )
        return ProductionExportResult(
            export=export,
            style_score=style_score,
            minimum_style_score=minimum,
            style_risk_accepted=style_score < minimum and request.accept_style_risk,
        )

    def _process_images(
        self,
        run: ProductionRun,
        images: list[GeneratedImage],
        *,
        background_override: BackgroundRemovalConfig | None = None,
    ) -> list[ProductionCandidate]:
        profile = self.constraints.resolve(run.project_id, run.task)
        background = background_override or BackgroundRemovalConfig(
            mode="corner_flood" if profile.require_transparency else "preserve"
        )
        candidates: list[ProductionCandidate] = []
        for index, image in enumerate(images[:4]):
            candidate_id = f"candidate-{index}"
            raw_path = self.production.write_candidate_image(
                run,
                candidate_id=candidate_id,
                stage="raw",
                content=image.content,
            )
            processed = ImageProcessor.process(image.content, profile, background)
            processed_path = self.production.write_candidate_image(
                run,
                candidate_id=candidate_id,
                stage="processed",
                content=processed.content,
            )
            filename = ConstraintValidator.expected_filename(
                profile,
                asset_id=run.task.asset_id,
                variant=candidate_id,
            )
            hard_constraints = ConstraintValidator.validate(
                processed.content,
                profile,
                asset_id=run.task.asset_id,
                variant=candidate_id,
                filename=filename,
            )
            candidates.append(
                ProductionCandidate(
                    candidate_id=candidate_id,
                    index=index,
                    raw_relative_path=raw_path,
                    processed_relative_path=processed_path,
                    metadata=processed.metadata,
                    hard_constraints=hard_constraints,
                    revised_prompt=image.revised_prompt,
                )
            )
        if not candidates:
            raise ProductionStateError("image provider returned no candidates")
        return candidates

    async def _review_once(
        self,
        run: ProductionRun,
        candidate_id: str,
        *,
        retry_index: int,
        history: list[ReviewAttempt],
    ) -> tuple[ProductionRun, ReviewAttempt, RepairPlan]:
        candidate = self._candidate(run, candidate_id)
        if run.plan is None:
            raise ProductionStateError("run does not contain a generation plan")
        context = self.context_builder.build(run.project_id, run.task)
        candidate_png = self.production.read_candidate_image(
            run,
            candidate_id=candidate.candidate_id,
            stage="processed",
        )
        comparison_png = ComparisonBoardBuilder.build(
            candidate_png,
            context.reference_images,
        )
        comparison_path = self.production.write_review_image(
            run,
            candidate_id=candidate.candidate_id,
            content=comparison_png,
        )
        provider_report = await self.providers.review_provider().review(
            ReviewRequest(
                project=context.project,
                task=run.task,
                plan=run.plan,
                candidate_png=candidate_png,
                comparison_png=comparison_png,
                reference_descriptions=context.reference_descriptions,
                trace=self._trace(run),
            )
        )
        report = provider_report.model_copy(
            update={
                "hard_constraints": candidate.hard_constraints,
                "export_allowed": candidate.hard_constraints.passed,
                "review_basis": [
                    "候选处理图",
                    (
                        f"{len(context.reference_images)} 张项目参考图"
                        if context.reference_images
                        else "当前任务未选择项目参考图"
                    ),
                    "本地确定性硬约束报告",
                ],
            }
        )
        repair = RepairPlanner.plan(
            report,
            minimum_style_score=context.project.review.minimum_style_score,
            retry_index=retry_index,
        )
        decision = (
            "pass"
            if repair.stop_reason == "passed"
            else "retry"
            if repair.retry_allowed
            else "manual_review"
        )
        report = report.model_copy(update={"decision": decision})
        updated_candidate = candidate.model_copy(
            update={
                "quality_report": report,
                "comparison_relative_path": comparison_path,
            }
        )
        candidates = [
            updated_candidate if item.candidate_id == candidate.candidate_id else item
            for item in run.candidates
        ]
        attempt = ReviewAttempt(
            attempt_index=retry_index,
            run_id=run.run_id,
            candidate_id=candidate.candidate_id,
            comparison_relative_path=comparison_path,
            quality_report=report,
            repair_plan=repair,
        )
        reviewed = self.production.update_run(
            run.model_copy(
                update={
                    "status": "reviewed",
                    "candidates": candidates,
                    "review_attempts": [*history, attempt],
                }
            )
        )
        self.production.write_review_json(
            reviewed,
            candidate_id=candidate.candidate_id,
            filename="review.json",
            payload=report.model_dump(mode="json"),
        )
        self.production.write_review_json(
            reviewed,
            candidate_id=candidate.candidate_id,
            filename="repair-plan.json",
            payload=repair.model_dump(mode="json"),
        )
        atomic_write_json(
            safe_child(self.production.run_path(reviewed), "review.json"),
            report.model_dump(mode="json"),
        )
        return reviewed, attempt, repair

    def _workspace_image(self, project_id: str, relative_path: str) -> ImageInput:
        normalized = relative_path.replace("\\", "/")
        relative = Path(normalized)
        if relative.is_absolute() or any(
            part in {"", ".", ".."} for part in relative.parts
        ):
            raise PathViolation("mask path must be relative and normalized")
        path = safe_child(
            self.production.workspace.project_path(project_id),
            *relative.parts,
        )
        if not path.is_file():
            raise FileNotFoundError(relative_path)
        return ImageInput(filename=path.name, content=path.read_bytes())

    @staticmethod
    def _candidate(run: ProductionRun, candidate_id: str) -> ProductionCandidate:
        candidate = next(
            (item for item in run.candidates if item.candidate_id == candidate_id),
            None,
        )
        if candidate is None:
            raise CandidateNotFound(candidate_id)
        return candidate

    @staticmethod
    def _trace(run: ProductionRun) -> ProviderTrace:
        return ProviderTrace(
            project_id=run.project_id,
            category=run.task.category,
            asset_id=run.task.asset_id,
            run_id=run.run_id,
        )

    def _write_run_artifacts(self, run: ProductionRun) -> None:
        run_path = self.production.run_path(run)
        if run.plan is not None:
            atomic_write_json(
                safe_child(run_path, "plan.json"),
                run.plan.model_dump(mode="json"),
            )
        atomic_write_json(
            safe_child(run_path, "prompt.json"),
            {"prompt": run.prompt},
        )
