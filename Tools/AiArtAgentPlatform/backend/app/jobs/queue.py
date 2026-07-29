from __future__ import annotations

import asyncio
import inspect
import uuid
from collections.abc import Awaitable, Callable
from dataclasses import dataclass
from typing import Any

from app.schemas.core import JobStatus
from app.workspace.project_workspace import ProjectWorkspace

from .events import EventBroker, JobEventStore
from .models import JobEvent, JobRecord


class JobCancelled(Exception):
    """任务执行器检测到取消请求。"""


JobHandler = Callable[["JobContext"], Awaitable[None]]


@dataclass(slots=True)
class _QueuedJob:
    job_id: str
    project_id: str
    handler: JobHandler


class JobContext:
    def __init__(self, queue: JobQueue, job_id: str, project_id: str) -> None:
        self.queue = queue
        self.job_id = job_id
        self.project_id = project_id

    async def update(self, status: JobStatus, progress: int, message: str = "") -> JobRecord:
        return await self.queue.update(self.job_id, self.project_id, status, progress, message)

    async def wait_if_cancelled(self) -> None:
        job = self.queue.get(self.job_id, self.project_id)
        if job.cancel_requested or job.status == JobStatus.CANCELLED:
            raise JobCancelled(self.job_id)


class JobQueue:
    def __init__(self, workspace: ProjectWorkspace) -> None:
        self.workspace = workspace
        self.events = JobEventStore(workspace)
        self.broker = EventBroker()
        self._queue: asyncio.Queue[_QueuedJob] = asyncio.Queue()
        self._worker: asyncio.Task[None] | None = None
        self._closing = False

    async def enqueue(
        self,
        project_id: str,
        kind: str,
        *,
        payload: dict[str, Any] | None = None,
        handler: JobHandler | None = None,
        max_attempts: int = 2,
    ) -> JobRecord:
        self.workspace.get_project(project_id)
        job = JobRecord(
            job_id=f"job-{uuid.uuid4().hex[:12]}",
            project_id=project_id,
            kind=kind,
            payload=payload or {},
            max_attempts=max_attempts,
        )
        self.workspace.write_job(job)
        self._append_event(job)
        await self._ensure_worker()
        await self._queue.put(_QueuedJob(job.job_id, project_id, handler or self._default_handler))
        return job

    async def _ensure_worker(self) -> None:
        if self._worker is None or self._worker.done():
            self._closing = False
            self._worker = asyncio.create_task(self._worker_loop())

    async def _worker_loop(self) -> None:
        while not self._closing:
            try:
                request = await asyncio.wait_for(self._queue.get(), timeout=0.1)
            except TimeoutError:
                continue
            try:
                await self._run(request)
            finally:
                self._queue.task_done()

    async def _run(self, request: _QueuedJob) -> None:
        job = self.get(request.job_id, request.project_id)
        if job.status == JobStatus.CANCELLED:
            return
        context = JobContext(self, request.job_id, request.project_id)
        try:
            if job.status == JobStatus.DRAFT:
                await context.update(JobStatus.PROCESSING, 1, "任务开始")
            elif job.status == JobStatus.PLANNING:
                await context.update(JobStatus.GENERATING, 5, "准备重试")
                await context.update(JobStatus.PROCESSING, 20, "任务开始")
            elif job.status == JobStatus.GENERATING:
                await context.update(JobStatus.PROCESSING, 20, "任务开始")
            result = request.handler(context)
            if inspect.isawaitable(result):
                await result
            job = self.get(request.job_id, request.project_id)
            if job.cancel_requested or job.status == JobStatus.CANCELLED:
                return
            if job.status not in JobRecord.TERMINAL_STATUSES:
                await context.update(JobStatus.READY, 100, "任务完成")
        except JobCancelled:
            job = self.get(request.job_id, request.project_id)
            if job.status != JobStatus.CANCELLED:
                await self.cancel(request.job_id, request.project_id)
        except Exception as error:  # noqa: BLE001 - queue must persist handler failures
            job = self.get(request.job_id, request.project_id)
            if job.status not in JobRecord.TERMINAL_STATUSES:
                await self.update(
                    request.job_id,
                    request.project_id,
                    JobStatus.FAILED,
                    job.progress,
                    "任务失败",
                    error=str(error),
                )

    @staticmethod
    async def _default_handler(context: JobContext) -> None:
        await context.update(JobStatus.READY, 100, "任务完成")

    def get(self, job_id: str, project_id: str | None = None) -> JobRecord:
        if project_id is not None:
            return self.workspace.read_job(project_id, job_id)
        for project in self.workspace.list_projects():
            try:
                return self.workspace.read_job(project.project_id, job_id)
            except FileNotFoundError:
                continue
        raise FileNotFoundError(job_id)

    async def update(
        self,
        job_id: str,
        project_id: str,
        status: JobStatus,
        progress: int,
        message: str = "",
        *,
        error: str | None = None,
    ) -> JobRecord:
        job = self.get(job_id, project_id)
        if job.cancel_requested and status != JobStatus.CANCELLED:
            raise JobCancelled(job_id)
        job.transition(status, progress=progress, message=message, error=error)
        self.workspace.write_job(job)
        self._append_event(job)
        return job

    async def cancel(self, job_id: str, project_id: str | None = None) -> JobRecord:
        job = self.get(job_id, project_id)
        if job.status in JobRecord.TERMINAL_STATUSES:
            return job
        job.cancel_requested = True
        job.transition(JobStatus.CANCELLED, message="任务已取消")
        self.workspace.write_job(job)
        self._append_event(job)
        return job

    async def retry(self, job_id: str, project_id: str | None = None) -> JobRecord:
        job = self.get(job_id, project_id)
        if job.status not in {JobStatus.FAILED, JobStatus.INTERRUPTED}:
            raise ValueError("only failed or interrupted jobs can be retried")
        if job.attempt >= job.max_attempts:
            raise ValueError("maximum retry count reached")
        job.attempt += 1
        job.cancel_requested = False
        job.error = None
        job.transition(JobStatus.PLANNING, progress=0, message="等待重试")
        self.workspace.write_job(job)
        self._append_event(job)
        await self._ensure_worker()
        await self._queue.put(_QueuedJob(job.job_id, job.project_id, self._default_handler))
        return job

    async def wait_for_terminal(self, job_id: str, *, timeout: float = 10) -> JobRecord:
        async def wait() -> JobRecord:
            while True:
                job = self.get(job_id)
                if job.status in JobRecord.TERMINAL_STATUSES:
                    return job
                await asyncio.sleep(0.01)

        return await asyncio.wait_for(wait(), timeout=timeout)

    def _append_event(self, job: JobRecord) -> None:
        previous = self.events.read(job.project_id, job.job_id)
        event = JobEvent(
            sequence=len(previous) + 1,
            job_id=job.job_id,
            project_id=job.project_id,
            status=job.status,
            progress=job.progress,
            message=job.message,
            error=job.error,
        )
        self.events.append(event)
        self.broker.publish(event)

    async def close(self) -> None:
        self._closing = True
        if self._worker is not None:
            await self._queue.join()
            await self._worker
            self._worker = None
