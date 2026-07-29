from __future__ import annotations

from pathlib import Path

from pydantic import ValidationError

from app.jobs.models import JobEvent, JobRecord
from app.schemas.core import ProjectConfig

from .atomic_store import atomic_write_json, atomic_write_yaml, read_json, read_yaml
from .path_guard import PathViolation, safe_child, validate_slug, workspace_root


class ProjectNotFound(FileNotFoundError):
    """项目不存在。"""


class ProjectAlreadyExists(FileExistsError):
    """项目已经存在。"""


PROJECT_DIRECTORIES = (
    "style-pack/references",
    "style-pack/thumbnails",
    "constraints",
    "assets",
    "jobs",
    "logs",
)


class ProjectWorkspace:
    def __init__(self, data_dir: Path) -> None:
        self.data_dir = data_dir.resolve()
        self.root = workspace_root(self.data_dir)

    def project_path(self, project_id: str) -> Path:
        validate_slug(project_id)
        return safe_child(self.root, project_id)

    def project_file(self, project_id: str) -> Path:
        return safe_child(self.project_path(project_id), "project.yaml")

    def create_project(self, project: ProjectConfig) -> ProjectConfig:
        project_path = self.project_path(project.project_id)
        if project_path.exists():
            raise ProjectAlreadyExists(project.project_id)
        project_path.mkdir(parents=True)
        for directory in PROJECT_DIRECTORIES:
            safe_child(project_path, *directory.split("/")).mkdir(parents=True, exist_ok=True)
        atomic_write_yaml(self.project_file(project.project_id), project.model_dump(mode="json"))
        return project

    def get_project(self, project_id: str) -> ProjectConfig:
        project_file = self.project_file(project_id)
        if not project_file.is_file():
            raise ProjectNotFound(project_id)
        data = read_yaml(project_file)
        if not isinstance(data, dict):
            raise ValueError("project.yaml must contain a mapping")
        return ProjectConfig.model_validate(data)

    def update_project(self, project_id: str, project: ProjectConfig) -> ProjectConfig:
        if project.project_id != project_id:
            raise PathViolation("project id in path and body must match")
        if not self.project_file(project_id).is_file():
            raise ProjectNotFound(project_id)
        atomic_write_yaml(self.project_file(project_id), project.model_dump(mode="json"))
        return project

    def list_projects(self) -> list[ProjectConfig]:
        projects: list[ProjectConfig] = []
        for project_file in sorted(self.root.glob("*/project.yaml")):
            try:
                projects.append(self.get_project(project_file.parent.name))
            except (ProjectNotFound, ValueError):
                continue
        return projects

    def job_path(self, project_id: str, job_id: str) -> Path:
        validate_slug(job_id)
        project_path = self.project_path(project_id)
        if not project_path.is_dir():
            raise ProjectNotFound(project_id)
        return safe_child(project_path, "jobs", f"{job_id}.json")

    def job_events_path(self, project_id: str, job_id: str) -> Path:
        validate_slug(job_id)
        project_path = self.project_path(project_id)
        if not project_path.is_dir():
            raise ProjectNotFound(project_id)
        return safe_child(project_path, "jobs", f"{job_id}.events.jsonl")

    def write_job(self, job: JobRecord) -> None:
        atomic_write_json(self.job_path(job.project_id, job.job_id), job.model_dump(mode="json"))

    def read_job(self, project_id: str, job_id: str) -> JobRecord:
        path = self.job_path(project_id, job_id)
        if not path.is_file():
            raise FileNotFoundError(job_id)
        return JobRecord.model_validate(read_json(path))

    def list_jobs(self, project_id: str) -> list[JobRecord]:
        project_path = self.project_path(project_id)
        if not project_path.is_dir():
            raise ProjectNotFound(project_id)
        jobs: list[JobRecord] = []
        for job_path in sorted((project_path / "jobs").glob("*.json")):
            try:
                jobs.append(self.read_job(project_id, job_path.stem))
            except (FileNotFoundError, ValueError):
                continue
        return sorted(jobs, key=lambda item: item.updated_at, reverse=True)

    def write_job_event(self, event: JobEvent) -> None:
        path = self.job_events_path(event.project_id, event.job_id)
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("a", encoding="utf-8", newline="\n") as stream:
            stream.write(event.model_dump_json() + "\n")
            stream.flush()

    def read_job_events(self, project_id: str, job_id: str) -> list[JobEvent]:
        path = self.job_events_path(project_id, job_id)
        if not path.is_file():
            return []
        events: list[JobEvent] = []
        for line in path.read_text(encoding="utf-8").splitlines():
            try:
                events.append(JobEvent.model_validate_json(line))
            except ValidationError:
                continue
        return events
