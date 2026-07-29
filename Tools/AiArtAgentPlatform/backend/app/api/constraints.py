from __future__ import annotations

import base64
from pathlib import Path
from typing import cast

from fastapi import APIRouter, HTTPException, Request, status

from app.constraints.exporter import ExportBlocked, ExportConflict, ImageExporter
from app.constraints.validator import ConstraintValidator
from app.constraints.workspace import ConstraintWorkspace
from app.image_processing.pipeline import ImageProcessor
from app.schemas.core import AssetCategory, ConstraintProfile, HardConstraintReport
from app.schemas.image_tools import (
    ExportRecord,
    ProcessPreviewResponse,
    WorkspaceImageRequest,
)
from app.workspace.path_guard import PathViolation, safe_child
from app.workspace.project_workspace import ProjectNotFound, ProjectWorkspace

router = APIRouter(prefix="/projects/{project_id}/constraints", tags=["constraints"])


def get_constraints(request: Request) -> ConstraintWorkspace:
    return cast(ConstraintWorkspace, request.app.state.constraint_workspace)


def get_projects(request: Request) -> ProjectWorkspace:
    return cast(ProjectWorkspace, request.app.state.workspace)


def get_exporter(request: Request) -> ImageExporter:
    return cast(ImageExporter, request.app.state.image_exporter)


@router.get("", response_model=dict[AssetCategory, ConstraintProfile])
def list_constraints(
    project_id: str,
    request: Request,
) -> dict[AssetCategory, ConstraintProfile]:
    try:
        return get_constraints(request).get_all(project_id)
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or constraint preset not found",
        ) from error
    except ValueError as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.put("/{category}", response_model=ConstraintProfile)
def update_constraint(
    project_id: str,
    category: AssetCategory,
    profile: ConstraintProfile,
    request: Request,
) -> ConstraintProfile:
    try:
        return get_constraints(request).update(project_id, category, profile)
    except ProjectNotFound as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project not found",
        ) from error
    except ValueError as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.post("/{category}/process-preview", response_model=ProcessPreviewResponse)
def process_preview(
    project_id: str,
    category: AssetCategory,
    request_data: WorkspaceImageRequest,
    request: Request,
) -> ProcessPreviewResponse:
    try:
        profile = get_constraints(request).get(project_id, category)
        source = _read_workspace_file(
            get_projects(request),
            project_id,
            request_data.workspace_relative_path,
        )
        processed = ImageProcessor.process(source, profile, request_data.background)
        filename = ConstraintValidator.expected_filename(
            profile,
            asset_id=request_data.asset_id,
            variant=request_data.variant,
        )
        report = ConstraintValidator.validate(
            processed.content,
            profile,
            asset_id=request_data.asset_id,
            variant=request_data.variant,
            filename=filename,
        )
        return ProcessPreviewResponse(
            processed_png_base64=base64.b64encode(processed.content).decode("ascii"),
            metadata=processed.metadata,
            hard_constraints=report,
        )
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project, constraint profile, or source image not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.post("/{category}/validate", response_model=HardConstraintReport)
def validate_image(
    project_id: str,
    category: AssetCategory,
    request_data: WorkspaceImageRequest,
    request: Request,
) -> HardConstraintReport:
    try:
        profile = get_constraints(request).get(project_id, category)
        content = _read_workspace_file(
            get_projects(request),
            project_id,
            request_data.workspace_relative_path,
        )
        filename = ConstraintValidator.expected_filename(
            profile,
            asset_id=request_data.asset_id,
            variant=request_data.variant,
        )
        return ConstraintValidator.validate(
            content,
            profile,
            asset_id=request_data.asset_id,
            variant=request_data.variant,
            filename=filename,
        )
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project, constraint profile, or source image not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.post("/{category}/export", response_model=ExportRecord)
def export_image(
    project_id: str,
    category: AssetCategory,
    request_data: WorkspaceImageRequest,
    request: Request,
) -> ExportRecord:
    try:
        profile = get_constraints(request).get(project_id, category)
        source = _read_workspace_file(
            get_projects(request),
            project_id,
            request_data.workspace_relative_path,
        )
        processed = ImageProcessor.process(source, profile, request_data.background)
        return get_exporter(request).export(
            project_id,
            category,
            request_data.asset_id,
            request_data.variant,
            processed.content,
            profile,
        )
    except ExportConflict as error:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="export file already exists",
        ) from error
    except ExportBlocked as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=error.report.model_dump(mode="json"),
        ) from error
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project, constraint profile, or source image not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


def _read_workspace_file(
    projects: ProjectWorkspace,
    project_id: str,
    relative_path: str,
) -> bytes:
    normalized = relative_path.replace("\\", "/")
    candidate_part = Path(normalized)
    if candidate_part.is_absolute() or any(
        part in {"", ".", ".."} for part in candidate_part.parts
    ):
        raise PathViolation("workspace image path must be relative and normalized")
    projects.get_project(project_id)
    path = safe_child(projects.project_path(project_id), *candidate_part.parts)
    if not path.is_file():
        raise FileNotFoundError(relative_path)
    return path.read_bytes()
