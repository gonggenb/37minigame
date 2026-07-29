from __future__ import annotations

from app.schemas.core import JobStatus
from app.workspace.project_workspace import ProjectWorkspace

from .models import JobEvent


def recover_interrupted(workspace: ProjectWorkspace) -> list[str]:
    changed: list[str] = []
    for project in workspace.list_projects():
        jobs_dir = workspace.project_path(project.project_id) / "jobs"
        for job_path in sorted(jobs_dir.glob("*.json")):
            try:
                job = workspace.read_job(project.project_id, job_path.stem)
            except (FileNotFoundError, ValueError):
                continue
            if job.status not in job.ACTIVE_STATUSES:
                continue
            job.transition(JobStatus.INTERRUPTED, message="服务重启，任务已中断")
            workspace.write_job(job)
            previous = workspace.read_job_events(project.project_id, job.job_id)
            workspace.write_job_event(
                JobEvent(
                    sequence=len(previous) + 1,
                    job_id=job.job_id,
                    project_id=job.project_id,
                    status=job.status,
                    progress=job.progress,
                    message=job.message,
                )
            )
            changed.append(job.job_id)
    return changed
