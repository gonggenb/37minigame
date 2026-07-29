from __future__ import annotations

from typing import cast

from fastapi import APIRouter, HTTPException, Request, Response, status

from app.constraints.exporter import ExportBlocked, ExportConflict
from app.production.service import (
    CandidateNotFound,
    ProductionStateError,
    StaticProductionService,
)
from app.production.workspace import AssetAlreadyExists, ProductionWorkspace
from app.providers.errors import ProviderError, ProviderErrorCode
from app.schemas.core import AssetCategory, AssetTask
from app.schemas.editor import (
    CandidateMaskRecord,
    CandidateMaskRequest,
    CandidateTransformRequest,
)
from app.schemas.production import (
    CandidateEditRequest,
    CandidateReviewAndRepairRequest,
    CandidateReviewRequest,
    CandidateSelection,
    ProductionExportRequest,
    ProductionExportResult,
    ProductionGenerateRequest,
    ProductionRun,
    StaticAssetRecord,
)
from app.workspace.path_guard import PathViolation
from app.workspace.project_workspace import ProjectNotFound

router = APIRouter(prefix="/projects/{project_id}/assets", tags=["production"])


def get_workspace(request: Request) -> ProductionWorkspace:
    return cast(ProductionWorkspace, request.app.state.production_workspace)


def get_service(request: Request) -> StaticProductionService:
    return cast(StaticProductionService, request.app.state.static_production_service)


@router.post("", response_model=StaticAssetRecord, status_code=status.HTTP_201_CREATED)
def create_asset(
    project_id: str,
    task: AssetTask,
    request: Request,
) -> StaticAssetRecord:
    try:
        return get_workspace(request).create_asset(
            project_id,
            StaticAssetRecord(task=task),
        )
    except Exception as error:
        raise _map_error(error) from error


@router.get("", response_model=list[StaticAssetRecord])
def list_assets(project_id: str, request: Request) -> list[StaticAssetRecord]:
    try:
        return get_workspace(request).list_assets(project_id)
    except Exception as error:
        raise _map_error(error) from error


@router.get("/{category}/{asset_id}", response_model=StaticAssetRecord)
def read_asset(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    request: Request,
) -> StaticAssetRecord:
    try:
        return get_workspace(request).get_asset(project_id, category, asset_id)
    except Exception as error:
        raise _map_error(error) from error


@router.put("/{category}/{asset_id}", response_model=StaticAssetRecord)
def update_asset(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    task: AssetTask,
    request: Request,
) -> StaticAssetRecord:
    try:
        current = get_workspace(request).get_asset(project_id, category, asset_id)
        return get_workspace(request).update_asset(
            project_id,
            category,
            asset_id,
            current.model_copy(update={"task": task}),
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post("/{category}/{asset_id}/plan", response_model=ProductionRun)
async def plan_asset(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    request: Request,
) -> ProductionRun:
    try:
        return await get_service(request).plan_asset(project_id, category, asset_id)
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/generate",
    response_model=ProductionRun,
)
async def generate_candidates(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: ProductionGenerateRequest,
    request: Request,
) -> ProductionRun:
    try:
        return await get_service(request).generate_candidates(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.get("/{category}/{asset_id}/runs", response_model=list[ProductionRun])
def list_runs(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    request: Request,
) -> list[ProductionRun]:
    try:
        return get_workspace(request).list_runs(project_id, category, asset_id)
    except Exception as error:
        raise _map_error(error) from error


@router.get(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/image"
)
def read_candidate_image(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    request: Request,
) -> Response:
    try:
        run = get_workspace(request).get_run(
            project_id,
            category,
            asset_id,
            run_id,
        )
        get_service(request)._candidate(run, candidate_id)
        content = get_workspace(request).read_candidate_image(
            run,
            candidate_id=candidate_id,
            stage="processed",
        )
        return Response(content=content, media_type="image/png")
    except Exception as error:
        raise _map_error(error) from error


@router.get(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/comparison"
)
def read_candidate_comparison(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    request: Request,
) -> Response:
    try:
        run = get_workspace(request).get_run(
            project_id,
            category,
            asset_id,
            run_id,
        )
        get_service(request)._candidate(run, candidate_id)
        content = get_workspace(request).read_review_image(
            run,
            candidate_id=candidate_id,
        )
        return Response(content=content, media_type="image/png")
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/select",
    response_model=ProductionRun,
)
def select_candidate(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: CandidateSelection,
    request: Request,
) -> ProductionRun:
    try:
        return get_service(request).select_candidate(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/edit",
    response_model=ProductionRun,
)
async def edit_candidate(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: CandidateEditRequest,
    request: Request,
) -> ProductionRun:
    try:
        return await get_service(request).edit_candidate(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/transform",
    response_model=ProductionRun,
)
def transform_candidate(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: CandidateTransformRequest,
    request: Request,
) -> ProductionRun:
    try:
        return get_service(request).transform_candidate(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/mask",
    response_model=CandidateMaskRecord,
    status_code=status.HTTP_201_CREATED,
)
def save_candidate_mask(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    candidate_id: str,
    request_data: CandidateMaskRequest,
    request: Request,
) -> CandidateMaskRecord:
    try:
        return get_service(request).save_candidate_mask(
            project_id,
            category,
            asset_id,
            run_id,
            candidate_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/review",
    response_model=ProductionRun,
)
async def review_candidate(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: CandidateReviewRequest,
    request: Request,
) -> ProductionRun:
    try:
        return await get_service(request).review_candidate(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/review-and-repair",
    response_model=ProductionRun,
)
async def review_and_repair_candidate(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: CandidateReviewAndRepairRequest,
    request: Request,
) -> ProductionRun:
    try:
        return await get_service(request).review_and_repair(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


@router.post(
    "/{category}/{asset_id}/runs/{run_id}/export",
    response_model=ProductionExportResult,
)
def export_candidate(
    project_id: str,
    category: AssetCategory,
    asset_id: str,
    run_id: str,
    request_data: ProductionExportRequest,
    request: Request,
) -> ProductionExportResult:
    try:
        return get_service(request).export_selected(
            project_id,
            category,
            asset_id,
            run_id,
            request_data,
        )
    except Exception as error:
        raise _map_error(error) from error


def _map_error(error: Exception) -> HTTPException:
    if isinstance(error, (AssetAlreadyExists, ExportConflict, FileExistsError)):
        return HTTPException(status_code=status.HTTP_409_CONFLICT, detail=str(error))
    if isinstance(
        error,
        (
            CandidateNotFound,
            FileNotFoundError,
            ProjectNotFound,
        ),
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
    if isinstance(
        error,
        (
            ExportBlocked,
            PathViolation,
            ProductionStateError,
            ValueError,
        ),
    ):
        return HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        )
    return HTTPException(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        detail="static production operation failed",
    )
