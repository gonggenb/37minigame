from __future__ import annotations

import asyncio
from collections.abc import AsyncIterator
from typing import cast

from fastapi import APIRouter, HTTPException, Request, status
from fastapi.responses import StreamingResponse

from app.jobs.events import EventBroker
from app.jobs.models import JobEvent, JobRecord
from app.jobs.queue import JobQueue
from app.workspace.path_guard import PathViolation

router = APIRouter(prefix="/jobs", tags=["jobs"])


def get_queue(request: Request) -> JobQueue:
    return cast(JobQueue, request.app.state.job_queue)


def event_frame(event: JobEvent) -> str:
    return f"id: {event.sequence}\nevent: job\ndata: {event.model_dump_json()}\n\n"


@router.get("/{job_id}", response_model=JobRecord)
def read_job(job_id: str, request: Request) -> JobRecord:
    try:
        return get_queue(request).get(job_id)
    except (FileNotFoundError, PathViolation) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="job not found"
        ) from error


@router.post("/{job_id}/cancel", response_model=JobRecord)
async def cancel_job(job_id: str, request: Request) -> JobRecord:
    try:
        return await get_queue(request).cancel(job_id)
    except (FileNotFoundError, PathViolation) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="job not found"
        ) from error


@router.post("/{job_id}/retry", response_model=JobRecord)
async def retry_job(job_id: str, request: Request) -> JobRecord:
    try:
        return await get_queue(request).retry(job_id)
    except FileNotFoundError as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="job not found"
        ) from error
    except PathViolation as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="job not found"
        ) from error
    except ValueError as error:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail=str(error)) from error


async def _event_stream(queue: JobQueue, job_id: str) -> AsyncIterator[str]:
    job = queue.get(job_id)
    live_queue = queue.broker.subscribe(job_id)
    last_sequence = 0
    try:
        for event in queue.events.read(job.project_id, job_id):
            last_sequence = max(last_sequence, event.sequence)
            yield event_frame(event)
        if EventBroker.is_terminal(job.status):
            return
        while True:
            try:
                event = await asyncio.wait_for(live_queue.get(), timeout=15)
            except TimeoutError:
                yield ": keep-alive\n\n"
                continue
            if event.sequence <= last_sequence:
                continue
            last_sequence = event.sequence
            yield event_frame(event)
            if EventBroker.is_terminal(event.status):
                return
    finally:
        queue.broker.unsubscribe(job_id, live_queue)


@router.get("/{job_id}/events")
async def job_events(job_id: str, request: Request) -> StreamingResponse:
    try:
        queue = get_queue(request)
        queue.get(job_id)
    except (FileNotFoundError, PathViolation) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND, detail="job not found"
        ) from error
    return StreamingResponse(
        _event_stream(queue, job_id),
        media_type="text/event-stream",
        headers={"Cache-Control": "no-cache", "X-Accel-Buffering": "no"},
    )
