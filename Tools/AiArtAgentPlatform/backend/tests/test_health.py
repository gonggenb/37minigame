import httpx
from app.main import create_app


def create_client() -> httpx.AsyncClient:
    transport = httpx.ASGITransport(app=create_app())
    return httpx.AsyncClient(transport=transport, base_url="http://testserver")


async def test_health_returns_local_service_metadata() -> None:
    async with create_client() as client:
        response = await client.get("/api/v1/health")

    assert response.status_code == httpx.codes.OK
    assert response.json() == {
        "status": "ok",
        "service": "ai-art-agent-platform",
        "schema_version": 1,
    }


async def test_cors_allows_only_local_frontend_origin() -> None:
    async with create_client() as client:
        allowed = await client.options(
            "/api/v1/health",
            headers={
                "Origin": "http://127.0.0.1:5173",
                "Access-Control-Request-Method": "GET",
            },
        )
        denied = await client.options(
            "/api/v1/health",
            headers={
                "Origin": "http://192.168.1.10:5173",
                "Access-Control-Request-Method": "GET",
            },
        )

    assert allowed.headers["access-control-allow-origin"] == "http://127.0.0.1:5173"
    assert "access-control-allow-origin" not in denied.headers
