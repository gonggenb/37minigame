from __future__ import annotations

from typing import Literal, cast

from fastapi import APIRouter, HTTPException, Request, Response, status

from app.constraints.exporter import ExportConflict
from app.production.sequence_service import (
    SequenceCandidateNotFound,
    SequenceProductionService,
    SequenceStateError,
)
from app.providers.errors import ProviderError, ProviderErrorCode
from app.schemas.core import AssetCategory
from app.schemas.sequence import (
    SequenceExportResult,
    SequenceGenerationRequest,
    SequenceRun,
    SequenceSelection,
    SequenceTask,
)
from app.workspace.path_guard import PathViolation
from app.workspace.project_workspace import ProjectNotFound

router = APIRouter(prefix="/projects/{project_id}/sequences", tags=["sequences"])


def get_service(request: Request) -> SequenceProductionService:
    return cast(SequenceProductionService, request.app.state.sequence_production_service)


@router.post("", response_model=SequenceRun, status_code=status.HTTP_201_CREATED)
def create_sequence(
    project_id: str,
    task: SequenceTask,
    request: Request,
) -> SequenceRun:
    try:
        return get_service(request).create_reference(project_id, task)
    except Exception as error:
        raise _map_error(error) from error


@router.get(
    "/{category}/{asset_id}/runs",
    response_model=list[SequenceRun],
)
def list_runs(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    request: Request,
) -> list[SequenceRun]:
    try:
        return get_service(request).list_runs(project_id, category, asset_id)
    except Exception as error:
        raise _map_error(error) from error


@router.get(
    "/{category}/{asset_id}/runs/{run_id}",
    response_model=SequenceRun,
)
def read_run(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request: Request,
) -> SequenceRun:
    try:
        return get_service(request).get_run(project_id, category, asset_id, run_id)
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/generate",
    response_model=SequenceRun,
)
async def generate_sequence(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: SequenceGenerationRequest,
    request: Request,
) -> SequenceRun:
    try:
        return await get_service(request).generate(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/reprocess",
    response_model=SequenceRun,
)
def reprocess_sequence(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request: Request,
) -> SequenceRun:
    try:
        return get_service(request).reprocess(project_id, category, asset_id, run_id)
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/select",
    response_model=SequenceRun,
)
def select_candidate(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    selection: SequenceSelection,
    request: Request,
) -> SequenceRun:
    try:
        return get_service(request).select(
            project_id,
            category,
            asset_id,
            run_id,
            selection,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/export",
    response_model=SequenceExportResult,
)
def export_sequence(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request: Request,
) -> SequenceExportResult:
    try:
        return get_service(request).export(project_id, category, asset_id, run_id)
    except Exception as error:
        raise _map_error(error) from error


def _artifact_response(
    request: Request,
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    kind: Literal["frame", "sprite_sheet", "gif", "webp", "report"],
    media_type: str,
    *,
    frame_index: int | None = None,
) -> Response:
    try:
        content = get_service(request).read_artifact(
            project_id,
            category,
            asset_id,
            run_id,
            candidate_id,
            kind,
            frame_index=frame_index,
        )
        return Response(content=content, media_type=media_type)
    except Exception as error:
        raise _map_error(error) from error


@router.get(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/frames/{frame_index}"
)
def read_frame(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    frame_index: int,
    request: Request,
) -> Response:
    return _artifact_response(
        request,
        project_id,
        category,
        asset_id,
        run_id,
        candidate_id,
        "frame",
        "image/png",
        frame_index=frame_index,
    )


@router.get(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/sprite-sheet"
)
def read_sprite_sheet(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    request: Request,
) -> Response:
    return _artifact_response(
        request,
        project_id,
        category,
        asset_id,
        run_id,
        candidate_id,
        "sprite_sheet",
        "image/png",
    )


@router.get(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/preview.gif"
)
def read_gif(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    request: Request,
) -> Response:
    return _artifact_response(
        request,
        project_id,
        category,
        asset_id,
        run_id,
        candidate_id,
        "gif",
        "image/gif",
    )


@router.get(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/preview.webp"
)
def read_webp(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    request: Request,
) -> Response:
    return _artifact_response(
        request,
        project_id,
        category,
        asset_id,
        run_id,
        candidate_id,
        "webp",
        "image/webp",
    )


@router.get(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/drift-report"
)
def read_report(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    request: Request,
) -> Response:
    return _artifact_response(
        request,
        project_id,
        category,
        asset_id,
        run_id,
        candidate_id,
        "report",
        "application/json",
    )


def _map_error(error: Exception) -> HTTPException:
    if isinstance(error, (ExportConflict, FileExistsError)):
        return HTTPException(status_code=status.HTTP_409_CONFLICT, detail=str(error))
    if isinstance(
        error,
        (SequenceCandidateNotFound, FileNotFoundError, ProjectNotFound),
    ):
        return HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=str(error))
    if isinstance(error, ProviderError):
        provider_status = (
            status.HTTP_409_CONFLICT
            if error.code is ProviderErrorCode.MISSING_API_KEY
            else error.status_code or status.HTTP_503_SERVICE_UNAVAILABLE
        )
        return HTTPException(
            status_code=provider_status,
            detail={
                "code": error.code.value,
                "message": str(error),
                "retryable": error.retryable,
            },
        )
    if isinstance(error, (PathViolation, SequenceStateError, ValueError)):
        return HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        )
    return HTTPException(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        detail="sequence production operation failed",
    )
