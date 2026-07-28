from __future__ import annotations

from typing import cast

from fastapi import APIRouter, HTTPException, Request, status

from app.jobs.models import JobRecord
from app.jobs.queue import JobQueue
from app.schemas.activity import ProjectActivitySummary
from app.schemas.core import ProjectConfig
from app.schemas.workspace import JobCreateRequest
from app.workspace.project_activity import ProjectActivityService
from app.workspace.project_workspace import ProjectAlreadyExists, ProjectNotFound, ProjectWorkspace

router = APIRouter(prefix="/projects", tags=["projects"])


def get_workspace(request: Request) -> ProjectWorkspace:
    return cast(ProjectWorkspace, request.app.state.workspace)


def get_queue(request: Request) -> JobQueue:
    return cast(JobQueue, request.app.state.job_queue)


def get_activity_service(request: Request) -> ProjectActivityService:
    return cast(
        ProjectActivityService,
        request.app.state.project_activity_service,
    )


@router.post("", response_model=ProjectConfig, status_code=status.HTTP_201_CREATED)
def create_project(project: ProjectConfig, request: Request) -> ProjectConfig:
    try:
        return get_workspace(request).create_project(project)
    except ProjectAlreadyExists as error:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT, detail="project already exists"
        ) from error


@router.get("", response_model=list[ProjectConfig])
def list_projects(request: Request) -> list[ProjectConfig]:
    return get_workspace(request).list_projects()


@router.get("/{project_id}", response_model=ProjectConfig)
def read_project(project_id: str, request: Request) -> ProjectConfig:
    try:
        return get_workspace(request).get_project(project_id)
    except (ProjectNotFound, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="project not found"
        ) from error


@router.put("/{project_id}", response_model=ProjectConfig)
def update_project(project_id: str, project: ProjectConfig, request: Request) -> ProjectConfig:
    try:
        return get_workspace(request).update_project(project_id, project)
    except ProjectNotFound as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="project not found"
        ) from error
    except ValueError as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT, detail=str(error)
        ) from error


@router.get("/{project_id}/activity", response_model=ProjectActivitySummary)
def read_project_activity(
    project_id: str,
    request: Request,
) -> ProjectActivitySummary:
    try:
        return get_activity_service(request).summarize(project_id)
    except (ProjectNotFound, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project not found",
        ) from error


@router.get("/{project_id}/jobs", response_model=list[JobRecord])
def list_jobs(project_id: str, request: Request) -> list[JobRecord]:
    try:
        return get_workspace(request).list_jobs(project_id)
    except (ProjectNotFound, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="project not found"
        ) from error


@router.post("/{project_id}/jobs", response_model=JobRecord, status_code=status.HTTP_202_ACCEPTED)
async def enqueue_job(
    project_id: str, request_data: JobCreateRequest, request: Request
) -> JobRecord:
    try:
        job = await get_queue(request).enqueue(
            project_id,
            request_data.kind,
            payload=request_data.payload,
            max_attempts=request_data.max_attempts,
        )
    except (ProjectNotFound, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="project not found"
        ) from error
    return job
