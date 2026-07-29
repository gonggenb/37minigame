from __future__ import annotations

import asyncio
from collections import defaultdict

from app.schemas.core import JobStatus
from app.workspace.project_workspace import ProjectWorkspace

from .models import JobEvent


class JobEventStore:
    def __init__(self, workspace: ProjectWorkspace) -> None:
        self.workspace = workspace

    def read(self, project_id: str, job_id: str) -> list[JobEvent]:
        return self.workspace.read_job_events(project_id, job_id)

    def append(self, event: JobEvent) -> JobEvent:
        self.workspace.write_job_event(event)
        return event


class EventBroker:
    def __init__(self) -> None:
        self._subscribers: dict[str, set[asyncio.Queue[JobEvent]]] = defaultdict(set)

    def subscribe(self, job_id: str) -> asyncio.Queue[JobEvent]:
        queue: asyncio.Queue[JobEvent] = asyncio.Queue()
        self._subscribers[job_id].add(queue)
        return queue

    def unsubscribe(self, job_id: str, queue: asyncio.Queue[JobEvent]) -> None:
        subscribers = self._subscribers.get(job_id)
        if subscribers is None:
            return
        subscribers.discard(queue)
        if not subscribers:
            self._subscribers.pop(job_id, None)

    def publish(self, event: JobEvent) -> None:
        for queue in tuple(self._subscribers.get(event.job_id, ())):
            queue.put_nowait(event)

    @staticmethod
    def is_terminal(status: JobStatus) -> bool:
        return status in {
            JobStatus.READY,
            JobStatus.EXPORTED,
            JobStatus.FAILED,
            JobStatus.CANCELLED,
            JobStatus.INTERRUPTED,
        }
