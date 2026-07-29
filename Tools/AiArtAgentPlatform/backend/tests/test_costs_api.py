from pathlib import Path

import httpx
import pytest
from app.config.settings import Settings
from app.main import create_app
from app.providers.audit import ProviderAuditWriter
from app.providers.models import ProviderTrace, ProviderUsage
from app.schemas.core import ProjectConfig
from app.workspace.project_workspace import ProjectWorkspace
from app.workspace.run_workspace import RunWorkspace


def _trace(category: str, asset_id: str, run_id: str) -> ProviderTrace:
    return ProviderTrace(
        project_id="wuxia-demo",
        category=category,
        asset_id=asset_id,
        run_id=run_id,
    )


@pytest.mark.asyncio
async def test_costs_api_summarizes_known_and_unknown_usage(tmp_path: Path) -> None:
    workspace = ProjectWorkspace(tmp_path / "data")
    workspace.create_project(
        ProjectConfig(project_id="wuxia-demo", display_name="武侠美术")
    )
    writer = ProviderAuditWriter(RunWorkspace(workspace))
    item_trace = _trace("item", "sword-demo", "run-item")
    ui_trace = _trace("ui", "button-demo", "run-ui")
    writer.write_usage(
        item_trace,
        ProviderUsage(
            model="gpt-5.6",
            operation="plan",
            raw={"total_tokens": 100},
            estimated_cost_usd=0.05,
        ),
    )
    writer.write_usage(
        item_trace,
        ProviderUsage(
            model="gpt-image-2",
            operation="generate",
            raw={"images": 1},
            estimated_cost_usd=0.10,
        ),
    )
    writer.write_usage(
        ui_trace,
        ProviderUsage(
            model="gpt-5.6",
            operation="review",
            raw={"total_tokens": 80},
        ),
    )

    platform_root = Path(__file__).resolve().parents[2]
    app = create_app(
        Settings(
            data_dir=tmp_path / "data",
            preset_dir=platform_root / "shared" / "presets",
        )
    )
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        response = await client.get("/api/v1/projects/wuxia-demo/costs")

    assert response.status_code == httpx.codes.OK
    payload = response.json()
    assert payload["request_count"] == 3
    assert payload["known_cost_usd"] == pytest.approx(0.15)
    assert payload["unknown_cost_count"] == 1
    assert {item["key"] for item in payload["by_model"]} == {
        "gpt-5.6",
        "gpt-image-2",
    }
    assert {item["key"] for item in payload["by_category"]} == {"item", "ui"}


@pytest.mark.asyncio
async def test_costs_api_returns_zero_without_model_calls(tmp_path: Path) -> None:
    platform_root = Path(__file__).resolve().parents[2]
    app = create_app(
        Settings(
            data_dir=tmp_path / "data",
            preset_dir=platform_root / "shared" / "presets",
        )
    )
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        response = await client.get("/api/v1/projects/wuxia-demo/costs")

    assert response.status_code == httpx.codes.OK
    assert response.json()["request_count"] == 0
    assert response.json()["known_cost_usd"] == 0
    assert response.json()["unknown_cost_count"] == 0
