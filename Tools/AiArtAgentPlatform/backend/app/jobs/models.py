from __future__ import annotations

from datetime import UTC, datetime
from typing import Any, ClassVar, Literal

from pydantic import ConfigDict, Field

from app.schemas.core import JobStatus, StrictModel


class InvalidJobTransition(ValueError):
    """任务状态转换不符合状态机。"""


def utc_now() -> datetime:
    return datetime.now(UTC)


class JobRecord(StrictModel):
    model_config = ConfigDict(extra="forbid")

    schema_version: Literal[1] = 1
    job_id: str = Field(pattern=r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
    project_id: str = Field(pattern=r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
    kind: str = Field(min_length=1, max_length=80)
    status: JobStatus = JobStatus.DRAFT
    progress: int = Field(default=0, ge=0, le=100)
    message: str = ""
    payload: dict[str, Any] = Field(default_factory=dict)
    attempt: int = Field(default=0, ge=0)
    max_attempts: int = Field(default=2, ge=0, le=5)
    cancel_requested: bool = False
    error: str | None = None
    created_at: datetime = Field(default_factory=utc_now)
    updated_at: datetime = Field(default_factory=utc_now)

    ACTIVE_STATUSES: ClassVar[frozenset[JobStatus]] = frozenset(
        {
            JobStatus.PLANNING,
            JobStatus.GENERATING,
            JobStatus.PROCESSING,
            JobStatus.REVIEWING,
            JobStatus.EXPORTING,
        }
    )
    TERMINAL_STATUSES: ClassVar[frozenset[JobStatus]] = frozenset(
        {
            JobStatus.READY,
            JobStatus.EXPORTED,
            JobStatus.FAILED,
            JobStatus.CANCELLED,
            JobStatus.INTERRUPTED,
        }
    )
    ALLOWED_TRANSITIONS: ClassVar[dict[JobStatus, frozenset[JobStatus]]] = {
        JobStatus.DRAFT: frozenset(
            {JobStatus.PLANNING, JobStatus.GENERATING, JobStatus.PROCESSING, JobStatus.CANCELLED}
        ),
        JobStatus.PLANNING: frozenset(
            {JobStatus.GENERATING, JobStatus.FAILED, JobStatus.CANCELLED, JobStatus.INTERRUPTED}
        ),
        JobStatus.PLANNED: frozenset(
            {JobStatus.GENERATING, JobStatus.CANCELLED, JobStatus.INTERRUPTED}
        ),
        JobStatus.GENERATING: frozenset(
            {JobStatus.PROCESSING, JobStatus.FAILED, JobStatus.CANCELLED, JobStatus.INTERRUPTED}
        ),
        JobStatus.PROCESSING: frozenset(
            {
                JobStatus.REVIEWING,
                JobStatus.READY,
                JobStatus.FAILED,
                JobStatus.CANCELLED,
                JobStatus.INTERRUPTED,
            }
        ),
        JobStatus.REVIEWING: frozenset(
            {
                JobStatus.READY,
                JobStatus.NEEDS_INPUT,
                JobStatus.FAILED,
                JobStatus.CANCELLED,
                JobStatus.INTERRUPTED,
            }
        ),
        JobStatus.NEEDS_INPUT: frozenset({JobStatus.PLANNING, JobStatus.CANCELLED}),
        JobStatus.EXPORTING: frozenset(
            {JobStatus.EXPORTED, JobStatus.FAILED, JobStatus.CANCELLED, JobStatus.INTERRUPTED}
        ),
        JobStatus.FAILED: frozenset({JobStatus.PLANNING, JobStatus.CANCELLED}),
        JobStatus.INTERRUPTED: frozenset({JobStatus.PLANNING, JobStatus.CANCELLED}),
        JobStatus.READY: frozenset({JobStatus.EXPORTING}),
        JobStatus.EXPORTED: frozenset(),
        JobStatus.CANCELLED: frozenset(),
    }

    def transition(
        self,
        status: JobStatus,
        *,
        progress: int | None = None,
        message: str | None = None,
        error: str | None = None,
    ) -> None:
        if status != self.status and status not in self.ALLOWED_TRANSITIONS[self.status]:
            raise InvalidJobTransition(f"{self.status.value} -> {status.value} is not allowed")
        self.status = status
        if progress is not None:
            self.progress = progress
        if message is not None:
            self.message = message
        if error is not None:
            self.error = error
        self.updated_at = utc_now()


class JobEvent(StrictModel):
    schema_version: Literal[1] = 1
    sequence: int = Field(gt=0)
    job_id: str
    project_id: str
    status: JobStatus
    progress: int = Field(ge=0, le=100)
    message: str = ""
    timestamp: datetime = Field(default_factory=utc_now)
    error: str | None = None
