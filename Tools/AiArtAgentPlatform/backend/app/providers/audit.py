from __future__ import annotations

from typing import Any

from app.workspace.atomic_store import atomic_write_bytes, atomic_write_json, read_json
from app.workspace.run_workspace import RunWorkspace

from .models import ProviderTrace, ProviderUsage


class ProviderAuditWriter:
    def __init__(self, runs: RunWorkspace, *, secret_values: list[str] | None = None) -> None:
        self.runs = runs
        self.secret_values = [value for value in (secret_values or []) if value]

    def sanitize(self, value: Any) -> Any:
        if isinstance(value, bytes):
            return f"<binary:{len(value)} bytes>"
        if isinstance(value, str):
            sanitized = value
            for secret in self.secret_values:
                sanitized = sanitized.replace(secret, "[REDACTED]")
            return sanitized
        if isinstance(value, dict):
            return {str(key): self.sanitize(item) for key, item in value.items()}
        if isinstance(value, (list, tuple)):
            return [self.sanitize(item) for item in value]
        return value

    def write_request(self, trace: ProviderTrace, payload: dict[str, Any]) -> None:
        path = self.runs.ensure_run(trace) / "provider-request.json"
        atomic_write_json(path, self.sanitize(payload))

    def write_response(self, trace: ProviderTrace, payload: dict[str, Any]) -> None:
        path = self.runs.ensure_run(trace) / "provider-response.json"
        atomic_write_json(path, self.sanitize(payload))

    def write_image(self, trace: ProviderTrace, index: int, content: bytes) -> None:
        path = self.runs.ensure_run(trace) / "raw" / f"candidate-{index + 1:02d}.png"
        atomic_write_bytes(path, content)

    def write_usage(self, trace: ProviderTrace, usage: ProviderUsage) -> None:
        run_path = self.runs.ensure_run(trace)
        payload = usage.model_dump(mode="json")
        atomic_write_json(run_path / "cost.json", payload)
        history_path = run_path / "cost-history.json"
        history: list[object] = []
        if history_path.is_file():
            stored = read_json(history_path)
            if isinstance(stored, list):
                history = stored
        history.append(payload)
        atomic_write_json(history_path, history)
