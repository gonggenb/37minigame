import json
from pathlib import Path

from app.providers.audit import ProviderAuditWriter
from app.providers.models import ProviderTrace, ProviderUsage
from app.schemas.core import ProjectConfig
from app.workspace.project_workspace import ProjectWorkspace
from app.workspace.run_workspace import RunWorkspace


def test_audit_writer_saves_request_response_image_and_usage_without_secret(
    tmp_path: Path,
) -> None:
    workspace = ProjectWorkspace(tmp_path)
    workspace.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    runs = RunWorkspace(workspace)
    writer = ProviderAuditWriter(runs, secret_values=["secret-key"])
    trace = ProviderTrace(
        project_id="wuxia-demo",
        category="item",
        asset_id="sword-001",
        run_id="run-001",
    )

    writer.write_request(trace, {"prompt": "secret-key must be hidden"})
    writer.write_response(trace, {"id": "response-001", "token": "secret-key"})
    writer.write_image(trace, 0, b"png-bytes")
    writer.write_usage(trace, ProviderUsage(model="gpt-image-2", raw={"total_tokens": 12}))

    run_path = runs.run_path(trace)
    request_text = (run_path / "provider-request.json").read_text(encoding="utf-8")
    response_text = (run_path / "provider-response.json").read_text(encoding="utf-8")
    usage = json.loads((run_path / "cost.json").read_text(encoding="utf-8"))
    history = json.loads(
        (run_path / "cost-history.json").read_text(encoding="utf-8")
    )

    assert "secret-key" not in request_text
    assert "secret-key" not in response_text
    assert (run_path / "raw" / "candidate-01.png").read_bytes() == b"png-bytes"
    assert usage["model"] == "gpt-image-2"
    assert usage["estimated_cost_usd"] is None
    assert len(history) == 1

    writer.write_usage(
        trace,
        ProviderUsage(model="gpt-5.6", operation="review", raw={"total_tokens": 8}),
    )
    history = json.loads(
        (run_path / "cost-history.json").read_text(encoding="utf-8")
    )
    assert [item["operation"] for item in history] == ["unknown", "review"]
