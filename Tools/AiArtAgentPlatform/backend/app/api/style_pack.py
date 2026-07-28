from __future__ import annotations

from typing import cast

from fastapi import APIRouter, HTTPException, Query, Request, Response, status

from app.agent.prompt_compiler import PromptCompiler
from app.schemas.core import AssetCategory
from app.schemas.style_pack import (
    CharacterIdentity,
    CompiledPrompt,
    PromptPreviewRequest,
    ReferenceAsset,
    ReferenceFilters,
    ReferenceImportRequest,
    ReferenceUpdateRequest,
    SourceReferenceFile,
    StyleGuide,
)
from app.style_pack.identity import CharacterIdentityStore
from app.style_pack.references import (
    ReferenceAlreadyExists,
    ReferenceCatalog,
    ReferenceNotFound,
)
from app.style_pack.workspace import StylePackWorkspace
from app.workspace.path_guard import PathViolation
from app.workspace.project_workspace import ProjectNotFound

router = APIRouter(prefix="/projects/{project_id}", tags=["style-pack"])


def get_style_packs(request: Request) -> StylePackWorkspace:
    return cast(StylePackWorkspace, request.app.state.style_pack_workspace)


def get_references(request: Request) -> ReferenceCatalog:
    return cast(ReferenceCatalog, request.app.state.reference_catalog)


def get_identities(request: Request) -> CharacterIdentityStore:
    return cast(CharacterIdentityStore, request.app.state.identity_store)


@router.get("/style-guide", response_model=StyleGuide)
def read_style_guide(project_id: str, request: Request) -> StyleGuide:
    try:
        return get_style_packs(request).get_style_guide(project_id)
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or style preset not found",
        ) from error
    except ValueError as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.put("/style-guide", response_model=StyleGuide)
def update_style_guide(
    project_id: str,
    guide: StyleGuide,
    request: Request,
) -> StyleGuide:
    try:
        return get_style_packs(request).update_style_guide(project_id, guide)
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


@router.get("/reference-source", response_model=list[SourceReferenceFile])
def list_reference_source(
    project_id: str,
    request: Request,
    query: str = Query(default="", max_length=200),
    limit: int = Query(default=100, ge=1, le=500),
) -> list[SourceReferenceFile]:
    try:
        return get_references(request).list_source_files(
            project_id,
            query=query,
            limit=limit,
        )
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or reference source not found",
        ) from error


@router.post(
    "/references",
    response_model=ReferenceAsset,
    status_code=status.HTTP_201_CREATED,
)
def import_reference(
    project_id: str,
    request_data: ReferenceImportRequest,
    request: Request,
) -> ReferenceAsset:
    try:
        return get_references(request).import_reference(project_id, request_data)
    except ReferenceAlreadyExists as error:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="reference id already exists",
        ) from error
    except ProjectNotFound as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project not found",
        ) from error
    except FileNotFoundError as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="reference source file not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.get("/references", response_model=list[ReferenceAsset])
def list_references(
    project_id: str,
    request: Request,
    category: AssetCategory | None = None,
    identity: str | None = Query(default=None, max_length=120),
    usage: str | None = Query(default=None, max_length=120),
    viewpoint: str | None = Query(default=None, max_length=120),
    material: str | None = Query(default=None, max_length=120),
    limit: int = Query(default=100, ge=1, le=500),
) -> list[ReferenceAsset]:
    try:
        return get_references(request).list_references(
            project_id,
            ReferenceFilters(
                category=category,
                identity=identity,
                usage=usage,
                viewpoint=viewpoint,
                material=material,
                limit=limit,
            ),
        )
    except ProjectNotFound as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project not found",
        ) from error


@router.put("/references/{reference_id}", response_model=ReferenceAsset)
def update_reference(
    project_id: str,
    reference_id: str,
    request_data: ReferenceUpdateRequest,
    request: Request,
) -> ReferenceAsset:
    try:
        return get_references(request).update_reference(
            project_id,
            reference_id,
            request_data,
        )
    except (ProjectNotFound, ReferenceNotFound) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or reference not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.get("/references/{reference_id}/thumbnail")
def read_reference_thumbnail(
    project_id: str,
    reference_id: str,
    request: Request,
) -> Response:
    try:
        content = get_references(request).read_thumbnail(project_id, reference_id)
    except (ProjectNotFound, ReferenceNotFound) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or reference thumbnail not found",
        ) from error
    return Response(content=content, media_type="image/png")


@router.delete(
    "/references/{reference_id}",
    status_code=status.HTTP_204_NO_CONTENT,
)
def delete_reference(project_id: str, reference_id: str, request: Request) -> Response:
    try:
        get_references(request).delete_reference(project_id, reference_id)
    except (ProjectNotFound, ReferenceNotFound) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or reference not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error
    return Response(status_code=status.HTTP_204_NO_CONTENT)


@router.put("/identities/{asset_id}", response_model=CharacterIdentity)
def save_identity(
    project_id: str,
    asset_id: str,
    identity: CharacterIdentity,
    request: Request,
) -> CharacterIdentity:
    if identity.asset_id != asset_id:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail="identity asset id in path and body must match",
        )
    try:
        return get_identities(request).save(project_id, identity)
    except ProjectNotFound as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.get("/identities/{asset_id}", response_model=CharacterIdentity)
def read_identity(
    project_id: str,
    asset_id: str,
    request: Request,
) -> CharacterIdentity:
    try:
        return get_identities(request).get(project_id, asset_id)
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or identity not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.post("/prompt-preview", response_model=CompiledPrompt)
def preview_prompt(
    project_id: str,
    request_data: PromptPreviewRequest,
    request: Request,
) -> CompiledPrompt:
    try:
        guide = get_style_packs(request).get_style_guide(project_id)
        references = get_references(request).list_references(
            project_id,
            ReferenceFilters(limit=500),
        )
        return PromptCompiler.compile(guide, request_data, references)
    except (ProjectNotFound, FileNotFoundError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or style preset not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error
