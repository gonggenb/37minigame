from __future__ import annotations

import hashlib
import os
import shutil
import tempfile
import uuid
from collections.abc import Callable
from datetime import UTC, datetime
from pathlib import Path
from typing import Literal

from PIL import Image

from app.constraints.exporter import ExportConflict
from app.constraints.workspace import ConstraintWorkspace
from app.production.service import ProviderRegistry
from app.providers.models import (
    EditRequest,
    GenerateRequest,
    ImageInput,
    ProviderTrace,
)
from app.schemas.core import AssetCategory, ConstraintProfile
from app.schemas.sequence import (
    SequenceCandidate,
    SequenceExportFile,
    SequenceExportResult,
    SequenceGenerationRequest,
    SequenceOutput,
    SequenceRun,
    SequenceSelection,
    SequenceTask,
)
from app.sequence_processing.grid import create_reference_grid
from app.sequence_processing.pipeline import ProcessedSequence, SequenceProcessor
from app.workspace.atomic_store import atomic_write_bytes, atomic_write_json, read_json
from app.workspace.path_guard import PathViolation, safe_child, validate_slug
from app.workspace.project_workspace import ProjectWorkspace


class SequenceStateError(ValueError):
    """序列运行记录不满足当前操作的前置条件。"""


class SequenceCandidateNotFound(FileNotFoundError):
    """序列候选不存在。"""


class SequenceProductionService:
    SEQUENCE_CATEGORIES = frozenset({AssetCategory.ANIMATION, AssetCategory.EFFECT})

    def __init__(
        self,
        workspace: ProjectWorkspace,
        constraints: ConstraintWorkspace,
        providers: ProviderRegistry,
        *,
        id_factory: Callable[[], str] | None = None,
    ) -> None:
        self.workspace = workspace
        self.constraints = constraints
        self.providers = providers
        self.id_factory = id_factory or (lambda: f"run-{uuid.uuid4().hex[:12]}")

    def create_reference(self, project_id: str, task: SequenceTask) -> SequenceRun:
        self._require_sequence_category(task.category)
        self.workspace.get_project(project_id)
        run_id = self.id_factory()
        validate_slug(run_id)
        run_root = self._run_path_by_ids(
            project_id,
            task.category,
            task.asset_id,
            run_id,
        )
        if run_root.exists():
            raise FileExistsError(run_id)
        run_root.mkdir(parents=True)

        base_frame = self._base_frame_content(project_id, task)
        generation_frame_width = task.resolved_generation_frame_width
        generation_frame_height = task.resolved_generation_frame_height
        if base_frame is None:
            reference = Image.new(
                "RGBA",
                (
                    task.columns * generation_frame_width,
                    task.rows * generation_frame_height,
                ),
                (0, 0, 0, 0),
            )
        else:
            reference = create_reference_grid(
                base_frame,
                rows=task.rows,
                columns=task.columns,
                frame_width=generation_frame_width,
                frame_height=generation_frame_height,
                baseline=task.baseline,
            )
        reference_path = safe_child(run_root, "reference-grid.png")
        atomic_write_bytes(reference_path, self._encode_png(reference))
        run = SequenceRun(
            run_id=run_id,
            project_id=project_id,
            task=task,
            status="reference_ready",
            prompt=self._default_prompt(task),
            reference_grid_relative_path=self._project_relative(
                project_id,
                reference_path,
            ),
        )
        atomic_write_json(
            safe_child(run_root, "sequence-task.json"),
            task.model_dump(mode="json"),
        )
        self._write_run(run)
        return run

    async def generate(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        request: SequenceGenerationRequest,
    ) -> SequenceRun:
        run = self.get_run(project_id, category, asset_id, run_id)
        if run.status not in {"reference_ready", "generated", "processed"}:
            raise SequenceStateError("sequence reference must be ready before generation")
        if request.prompt_override is not None:
            run = run.model_copy(update={"prompt": request.prompt_override})
            run = self._update_run(run)

        project = self.workspace.get_project(project_id)
        provider = self.providers.image_provider()
        candidate_count = min(
            request.candidate_count,
            provider.capabilities().max_candidates,
        )
        width = run.task.columns * run.task.resolved_generation_frame_width
        height = run.task.rows * run.task.resolved_generation_frame_height
        trace = ProviderTrace(
            project_id=project_id,
            category=category,
            asset_id=asset_id,
            run_id=run_id,
        )
        if (
            run.task.category is AssetCategory.ANIMATION
            or run.task.base_frame_workspace_relative_path is not None
        ):
            if run.reference_grid_relative_path is None:
                raise SequenceStateError("sequence reference grid is missing")
            reference_content = self._read_project_relative(
                project_id,
                run.reference_grid_relative_path,
            )
            generated = await provider.edit(
                EditRequest(
                    prompt=run.prompt,
                    images=[
                        ImageInput(
                            filename="reference-grid.png",
                            content=reference_content,
                        )
                    ],
                    width=width,
                    height=height,
                    candidate_count=candidate_count,
                    quality=project.generation.image_quality,
                    background="opaque",
                    trace=trace,
                )
            )
        else:
            generated = await provider.generate(
                GenerateRequest(
                    prompt=run.prompt,
                    width=width,
                    height=height,
                    candidate_count=candidate_count,
                    quality=project.generation.image_quality,
                    background="opaque",
                    trace=trace,
                )
            )
        if not generated:
            raise SequenceStateError("image provider returned no sequence candidates")

        profile = self.constraints.get(project_id, category)
        base_frame_png = self._base_frame_content(project_id, run.task)
        candidates = [
            self._process_candidate(
                run,
                candidate_id=f"candidate-{index}",
                raw_strip=image.content,
                base_frame_png=base_frame_png,
                profile=profile,
            )
            for index, image in enumerate(generated[:4])
        ]
        return self._update_run(
            run.model_copy(update={"status": "processed", "candidates": candidates})
        )

    def reprocess(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
    ) -> SequenceRun:
        run = self.get_run(project_id, category, asset_id, run_id)
        if not run.candidates:
            raise SequenceStateError("generate sequence candidates before reprocessing")
        profile = self.constraints.get(project_id, category)
        base_frame_png = self._base_frame_content(project_id, run.task)
        candidates = [
            self._process_candidate(
                run,
                candidate_id=candidate.candidate_id,
                raw_strip=self._read_project_relative(
                    project_id,
                    candidate.raw_strip_relative_path,
                ),
                base_frame_png=base_frame_png,
                profile=profile,
            )
            for candidate in run.candidates
        ]
        return self._update_run(
            run.model_copy(update={"status": "processed", "candidates": candidates})
        )

    def select(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        selection: SequenceSelection,
    ) -> SequenceRun:
        run = self.get_run(project_id, category, asset_id, run_id)
        self._candidate(run, selection.candidate_id)
        return self._update_run(
            run.model_copy(update={"selected_candidate_id": selection.candidate_id})
        )

    def export(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
    ) -> SequenceExportResult:
        run = self.get_run(project_id, category, asset_id, run_id)
        if run.selected_candidate_id is None:
            raise SequenceStateError("select a sequence candidate before export")
        candidate = self._candidate(run, run.selected_candidate_id)
        if candidate.output is None or candidate.output.drift_report is None:
            raise SequenceStateError("process the selected sequence before export")
        if not candidate.output.drift_report.passed:
            raise SequenceStateError("sequence drift constraints must pass before export")

        export_parent = safe_child(
            self.asset_path(project_id, category, asset_id),
            "exports",
        )
        export_path = safe_child(export_parent, run_id)
        if export_path.exists():
            raise ExportConflict(run_id)
        export_parent.mkdir(parents=True, exist_ok=True)
        temporary_path = Path(
            tempfile.mkdtemp(dir=export_parent, prefix=f".{run_id}.")
        )
        try:
            files = self._write_export_files(
                project_id,
                run,
                candidate.output,
                temporary_path,
                export_path,
            )
            os.replace(temporary_path, export_path)
        except BaseException:
            shutil.rmtree(temporary_path, ignore_errors=True)
            raise
        self._update_run(run.model_copy(update={"status": "exported"}))
        return SequenceExportResult(
            project_id=project_id,
            asset_id=asset_id,
            category=category,
            files=files,
            drift_report=candidate.output.drift_report,
        )

    def get_run(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
    ) -> SequenceRun:
        path = safe_child(
            self._run_path_by_ids(project_id, category, asset_id, run_id),
            "run.json",
        )
        if not path.is_file():
            raise FileNotFoundError(run_id)
        run = SequenceRun.model_validate(read_json(path))
        if (
            run.project_id != project_id
            or run.task.category is not category
            or run.task.asset_id != asset_id
            or run.run_id != run_id
        ):
            raise PathViolation("stored sequence run identity does not match its path")
        return run

    def list_runs(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
    ) -> list[SequenceRun]:
        runs_path = safe_child(self.asset_path(project_id, category, asset_id), "runs")
        if not runs_path.is_dir():
            return []
        runs: list[SequenceRun] = []
        for run_file in sorted(runs_path.glob("*/run.json")):
            try:
                runs.append(
                    self.get_run(project_id, category, asset_id, run_file.parent.name)
                )
            except (FileNotFoundError, PathViolation, ValueError):
                continue
        return sorted(runs, key=lambda item: item.created_at, reverse=True)

    def list_project_runs(self, project_id: str) -> list[SequenceRun]:
        project_root = self.workspace.project_path(project_id)
        self.workspace.get_project(project_id)
        runs: list[SequenceRun] = []
        for category in sorted(self.SEQUENCE_CATEGORIES, key=lambda item: item.value):
            category_path = safe_child(project_root, "assets", category.value)
            if not category_path.is_dir():
                continue
            for run_file in sorted(category_path.glob("*/runs/*/run.json")):
                asset_id = run_file.parents[2].name
                run_id = run_file.parent.name
                try:
                    runs.append(self.get_run(project_id, category, asset_id, run_id))
                except (FileNotFoundError, PathViolation, ValueError):
                    continue
        return sorted(runs, key=lambda item: item.updated_at, reverse=True)

    def read_artifact(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
        candidate_id: str,
        kind: Literal["frame", "sprite_sheet", "gif", "webp", "report"],
        *,
        frame_index: int | None = None,
    ) -> bytes:
        run = self.get_run(project_id, category, asset_id, run_id)
        candidate = self._candidate(run, candidate_id)
        if candidate.output is None:
            raise SequenceStateError("sequence candidate has not been processed")
        if kind == "frame":
            if frame_index is None or not 0 <= frame_index < candidate.output.frame_count:
                raise ValueError("frame index is outside the sequence")
            relative_path = candidate.output.frame_relative_paths[frame_index]
        else:
            relative_path = {
                "sprite_sheet": candidate.output.sprite_sheet_relative_path,
                "gif": candidate.output.gif_relative_path,
                "webp": candidate.output.webp_relative_path,
                "report": candidate.output.drift_report_relative_path,
            }[kind]
        return self._read_project_relative(project_id, relative_path)

    def asset_path(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
    ) -> Path:
        self._require_sequence_category(category)
        validate_slug(asset_id)
        self.workspace.get_project(project_id)
        return safe_child(
            self.workspace.project_path(project_id),
            "assets",
            category.value,
            asset_id,
        )

    def run_path(self, run: SequenceRun) -> Path:
        return self._run_path_by_ids(
            run.project_id,
            run.task.category,
            run.task.asset_id,
            run.run_id,
        )

    def _run_path_by_ids(
        self,
        project_id: str,
        category: AssetCategory,
        asset_id: str,
        run_id: str,
    ) -> Path:
        validate_slug(run_id)
        return safe_child(self.asset_path(project_id, category, asset_id), "runs", run_id)

    def _process_candidate(
        self,
        run: SequenceRun,
        *,
        candidate_id: str,
        raw_strip: bytes,
        base_frame_png: bytes | None,
        profile: ConstraintProfile,
    ) -> SequenceCandidate:
        self._validate_candidate_id(candidate_id)
        candidate_path = safe_child(self.run_path(run), candidate_id)
        raw_path = safe_child(candidate_path, "raw-strip.png")
        atomic_write_bytes(raw_path, raw_strip)
        processed = SequenceProcessor.process(
            strip_png=raw_strip,
            task=run.task,
            profile=profile,
            base_frame_png=base_frame_png,
        )
        output = self._write_processed_output(run, candidate_path, processed)
        return SequenceCandidate(
            candidate_id=candidate_id,
            index=int(candidate_id.rsplit("-", 1)[1]),
            raw_strip_relative_path=self._project_relative(run.project_id, raw_path),
            output=output,
        )

    def _write_processed_output(
        self,
        run: SequenceRun,
        candidate_path: Path,
        processed: ProcessedSequence,
    ) -> SequenceOutput:
        frames_path = safe_child(candidate_path, "frames")
        frame_paths: list[str] = []
        records = []
        for index, (content, record) in enumerate(
            zip(processed.frame_pngs, processed.frame_records, strict=True)
        ):
            path = safe_child(frames_path, f"frame-{index:03d}.png")
            atomic_write_bytes(path, content)
            relative_path = self._project_relative(run.project_id, path)
            frame_paths.append(relative_path)
            records.append(record.model_copy(update={"relative_path": relative_path}))

        sprite_path = safe_child(candidate_path, "sprite-sheet.png")
        gif_path = safe_child(candidate_path, "preview.gif")
        webp_path = safe_child(candidate_path, "preview.webp")
        report_path = safe_child(candidate_path, "drift-report.json")
        atomic_write_bytes(sprite_path, processed.sprite_sheet_png)
        atomic_write_bytes(gif_path, processed.gif_preview)
        atomic_write_bytes(webp_path, processed.webp_preview)
        atomic_write_json(
            report_path,
            processed.drift_report.model_dump(mode="json"),
        )
        return SequenceOutput(
            frame_count=run.task.frame_count,
            rows=run.task.rows,
            columns=run.task.columns,
            frame_width=run.task.frame_width,
            frame_height=run.task.frame_height,
            sprite_sheet_width=run.task.columns * run.task.frame_width,
            sprite_sheet_height=run.task.rows * run.task.frame_height,
            frame_relative_paths=frame_paths,
            sprite_sheet_relative_path=self._project_relative(
                run.project_id,
                sprite_path,
            ),
            gif_relative_path=self._project_relative(run.project_id, gif_path),
            webp_relative_path=self._project_relative(run.project_id, webp_path),
            drift_report_relative_path=self._project_relative(
                run.project_id,
                report_path,
            ),
            content_sha256=processed.content_sha256,
            frames=records,
            drift_report=processed.drift_report,
        )

    def _write_export_files(
        self,
        project_id: str,
        run: SequenceRun,
        output: SequenceOutput,
        temporary_path: Path,
        final_path: Path,
    ) -> list[SequenceExportFile]:
        prefix = f"{run.task.asset_id}_{run.task.action}"
        specs: list[tuple[Literal["frame", "sprite_sheet", "gif", "webp", "report"], str, str]] = [
            (
                "frame",
                f"{prefix}_frame-{index:03d}.png",
                relative_path,
            )
            for index, relative_path in enumerate(output.frame_relative_paths)
        ]
        specs.extend(
            [
                ("sprite_sheet", f"{prefix}_sprite-sheet.png", output.sprite_sheet_relative_path),
                ("gif", f"{prefix}_preview.gif", output.gif_relative_path),
                ("webp", f"{prefix}_preview.webp", output.webp_relative_path),
                ("report", f"{prefix}_drift-report.json", output.drift_report_relative_path),
            ]
        )
        files: list[SequenceExportFile] = []
        for kind, filename, source_relative_path in specs:
            content = self._read_project_relative(project_id, source_relative_path)
            temporary_file = safe_child(temporary_path, filename)
            atomic_write_bytes(temporary_file, content)
            digest = hashlib.sha256(content).hexdigest()
            if hashlib.sha256(temporary_file.read_bytes()).hexdigest() != digest:
                raise OSError("sequence export hash does not match source content")
            final_file = safe_child(final_path, filename)
            files.append(
                SequenceExportFile(
                    kind=kind,
                    filename=filename,
                    relative_path=self._project_relative(project_id, final_file),
                    sha256=digest,
                    file_bytes=len(content),
                )
            )
        return files

    def _base_frame_content(
        self,
        project_id: str,
        task: SequenceTask,
    ) -> bytes | None:
        if task.base_frame_workspace_relative_path is None:
            return None
        return self._read_project_relative(
            project_id,
            task.base_frame_workspace_relative_path,
        )

    def _read_project_relative(self, project_id: str, relative_path: str) -> bytes:
        normalized = relative_path.replace("\\", "/")
        relative = Path(normalized)
        if relative.is_absolute() or any(
            part in {"", ".", ".."} for part in relative.parts
        ):
            raise PathViolation("workspace image path must be relative and normalized")
        path = safe_child(self.workspace.project_path(project_id), *relative.parts)
        if not path.is_file():
            raise FileNotFoundError(relative_path)
        return path.read_bytes()

    def _project_relative(self, project_id: str, path: Path) -> str:
        return path.relative_to(self.workspace.project_path(project_id)).as_posix()

    def _write_run(self, run: SequenceRun) -> None:
        atomic_write_json(
            safe_child(self.run_path(run), "run.json"),
            run.model_dump(mode="json"),
        )

    def _update_run(self, run: SequenceRun) -> SequenceRun:
        updated = run.model_copy(update={"updated_at": datetime.now(UTC)})
        self._write_run(updated)
        return updated

    @staticmethod
    def _candidate(run: SequenceRun, candidate_id: str) -> SequenceCandidate:
        candidate = next(
            (item for item in run.candidates if item.candidate_id == candidate_id),
            None,
        )
        if candidate is None:
            raise SequenceCandidateNotFound(candidate_id)
        return candidate

    @classmethod
    def _require_sequence_category(cls, category: AssetCategory) -> None:
        if category not in cls.SEQUENCE_CATEGORIES:
            raise PathViolation("sequence production only supports animation and effect")

    @staticmethod
    def _validate_candidate_id(candidate_id: str) -> None:
        if candidate_id not in {f"candidate-{index}" for index in range(4)}:
            raise PathViolation("invalid sequence candidate id")

    @staticmethod
    def _encode_png(image: Image.Image) -> bytes:
        from app.sequence_processing.output import encode_png

        return encode_png(image)

    @staticmethod
    def _default_prompt(task: SequenceTask) -> str:
        kind = "角色动画" if task.category is AssetCategory.ANIMATION else "透明特效"
        generation_width = task.resolved_generation_frame_width
        generation_height = task.resolved_generation_frame_height
        return (
            f"生成统一 Q 版水墨国风武侠轻量化 2.5D 俯视角{kind}：{task.name}。"
            f"动作 {task.action}，完整序列 {task.frame_count} 帧，"
            f"固定 {task.rows} 行 × {task.columns} 列，模型网格每格严格 "
            f"{generation_width} × {generation_height} px。"
            f"本地管线最终归一化为每帧 {task.frame_width} × {task.frame_height} px。"
            "保持同一身份、比例、调色板和锚点；使用纯色、均匀、可分离背景，"
            "无文字、边框、场景或水印，不要求模型原生透明。"
        )
