import asyncio
from pathlib import Path

import pytest
from app.jobs.models import InvalidJobTransition, JobEvent, JobRecord
from app.jobs.queue import JobQueue
from app.jobs.recovery import recover_interrupted
from app.schemas.core import JobStatus, ProjectConfig
from app.workspace.project_workspace import ProjectWorkspace


def test_job_status_transition_rejects_terminal_to_active() -> None:
    job = JobRecord(job_id="job-001", project_id="wuxia-demo", kind="preview")
    job.transition(JobStatus.PROCESSING, progress=20, message="处理中")
    job.transition(JobStatus.READY, progress=100, message="完成")

    with pytest.raises(InvalidJobTransition):
        job.transition(JobStatus.PROCESSING)


@pytest.mark.asyncio
async def test_queue_runs_handler_and_persists_events(tmp_path: Path) -> None:
    workspace = ProjectWorkspace(tmp_path)
    workspace.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    queue = JobQueue(workspace)

    async def handler(context) -> None:
        await context.update(JobStatus.PROCESSING, 40, "处理中")
        await context.update(JobStatus.READY, 100, "完成")

    job = await queue.enqueue("wuxia-demo", "preview", handler=handler)
    finished = await queue.wait_for_terminal(job.job_id, timeout=2)

    assert finished.status == JobStatus.READY
    assert finished.progress == 100
    events = queue.events.read("wuxia-demo", job.job_id)
    assert [event.status for event in events][-1] == JobStatus.READY
    assert (tmp_path / "workspaces" / "wuxia-demo" / "jobs" / f"{job.job_id}.json").exists()
    await queue.close()


@pytest.mark.asyncio
async def test_cancelled_handler_cannot_mark_job_ready(tmp_path: Path) -> None:
    workspace = ProjectWorkspace(tmp_path)
    workspace.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    queue = JobQueue(workspace)
    started = asyncio.Event()
    release = asyncio.Event()

    async def handler(context) -> None:
        started.set()
        await release.wait()
        await context.wait_if_cancelled()
        await context.update(JobStatus.READY, 100, "不应到达")

    job = await queue.enqueue("wuxia-demo", "preview", handler=handler)
    await asyncio.wait_for(started.wait(), timeout=2)
    cancelled = await queue.cancel(job.job_id)
    release.set()
    finished = await queue.wait_for_terminal(job.job_id, timeout=2)

    assert cancelled.status == JobStatus.CANCELLED
    assert finished.status == JobStatus.CANCELLED
    await queue.close()


@pytest.mark.asyncio
async def test_failed_job_can_be_retried_once(tmp_path: Path) -> None:
    workspace = ProjectWorkspace(tmp_path)
    workspace.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    queue = JobQueue(workspace)

    async def failing_handler(context) -> None:
        raise RuntimeError("provider unavailable")

    job = await queue.enqueue(
        "wuxia-demo", "preview", handler=failing_handler, max_attempts=1
    )
    failed = await queue.wait_for_terminal(job.job_id, timeout=2)
    retried = await queue.retry(job.job_id)
    finished = await queue.wait_for_terminal(job.job_id, timeout=2)

    assert failed.status == JobStatus.FAILED
    assert retried.status == JobStatus.PLANNING
    assert finished.status == JobStatus.READY
    assert finished.attempt == 1
    await queue.close()


def test_recovery_marks_active_jobs_interrupted(tmp_path: Path) -> None:
    workspace = ProjectWorkspace(tmp_path)
    workspace.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    job = JobRecord(
        job_id="job-running",
        project_id="wuxia-demo",
        kind="preview",
        status=JobStatus.PROCESSING,
        progress=50,
    )
    workspace.write_job(job)

    changed = recover_interrupted(workspace)

    assert changed == ["job-running"]
    recovered = workspace.read_job("wuxia-demo", "job-running")
    assert recovered.status == JobStatus.INTERRUPTED
    assert queue_event_status(workspace, "job-running") == JobStatus.INTERRUPTED


def queue_event_status(workspace: ProjectWorkspace, job_id: str) -> JobStatus:
    path = workspace.job_events_path("wuxia-demo", job_id)
    event = JobEvent.model_validate_json(path.read_text(encoding="utf-8").splitlines()[-1])
    return event.status
