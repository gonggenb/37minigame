from pathlib import Path

import httpx
import pytest
from app.config.settings import Settings
from app.main import create_app


@pytest.mark.asyncio
async def test_model_status_never_returns_api_key(tmp_path: Path) -> None:
    app = create_app(Settings(data_dir=tmp_path, openai_api_key="secret-key"))

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        response = await client.get("/api/v1/models/status")

    assert response.status_code == httpx.codes.OK
    assert response.json()["api_key_configured"] is True
    assert "secret-key" not in response.text
    assert "openai_api_key" not in response.text


@pytest.mark.asyncio
async def test_availability_requires_explicitly_configured_key(tmp_path: Path) -> None:
    app = create_app(Settings(data_dir=tmp_path))

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        response = await client.post("/api/v1/models/availability", json={"include_image": False})

    assert response.status_code == httpx.codes.CONFLICT
    assert "API key" in response.json()["detail"]
