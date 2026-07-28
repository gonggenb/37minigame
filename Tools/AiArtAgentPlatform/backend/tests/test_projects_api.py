from pathlib import Path

import httpx
import pytest
from app.config.settings import Settings
from app.main import create_app


@pytest.mark.asyncio
async def test_project_and_job_routes_support_sse(tmp_path: Path) -> None:
    app = create_app(Settings(data_dir=tmp_path))

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        created = await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        assert created.status_code == httpx.codes.CREATED

        listed = await client.get("/api/v1/projects")
        assert listed.status_code == httpx.codes.OK
        assert listed.json()[0]["project_id"] == "wuxia-demo"

        asset = await client.post(
            "/api/v1/projects/wuxia-demo/assets",
            json={
                "asset_id": "green-sword",
                "category": "item",
                "name": "青锋剑",
                "brief": "水墨青锋剑",
                "usage": "world-sprite",
                "style_pack": "wuxia-ink-chibi-topdown-2_5d",
                "reference_ids": [],
                "constraint_profile": "wuxia-item",
                "constraint_overrides": {},
                "candidate_count": 4,
                "output_mode": "single-png",
            },
        )
        assert asset.status_code == httpx.codes.CREATED

        activity = await client.get("/api/v1/projects/wuxia-demo/activity")
        assert activity.status_code == httpx.codes.OK
        activity_payload = activity.json()
        assert activity_payload["project_id"] == "wuxia-demo"
        assert [item["category"] for item in activity_payload["categories"]] == [
            "character",
            "scene",
            "item",
            "animation",
            "effect",
            "ui",
        ]
        assert activity_payload["categories"][2]["recent"][0]["asset_id"] == (
            "green-sword"
        )

        missing_activity = await client.get("/api/v1/projects/missing/activity")
        assert missing_activity.status_code == httpx.codes.NOT_FOUND

        queued = await client.post(
            "/api/v1/projects/wuxia-demo/jobs",
            json={"kind": "preview", "payload": {"asset_id": "sword-001"}},
        )
        assert queued.status_code == httpx.codes.ACCEPTED
        job_id = queued.json()["job_id"]

        for _ in range(20):
            current = await client.get(f"/api/v1/jobs/{job_id}")
            if current.json()["status"] in {"ready", "failed", "cancelled"}:
                break
            await __import__("asyncio").sleep(0.01)

        events = await client.get(f"/api/v1/jobs/{job_id}/events")
        assert events.status_code == httpx.codes.OK
        assert events.headers["content-type"].startswith("text/event-stream")
        assert "event: job" in events.text
        assert '"status":"ready"' in events.text
