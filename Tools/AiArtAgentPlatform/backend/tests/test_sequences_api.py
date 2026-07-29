from io import BytesIO
from pathlib import Path

import httpx
import pytest
from app.config.settings import Settings
from app.main import create_app
from app.providers.models import GeneratedImage, ProviderCapabilities
from app.schemas.core import AssetCategory
from app.schemas.sequence import ACTION_TEMPLATES
from PIL import Image


def _png(image: Image.Image) -> bytes:
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _strip(width: int, height: int) -> bytes:
    frame_count = width // height
    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    for index in range(frame_count):
        subject = Image.new("RGBA", (6, 10), (80 + index * 8, 100, 60, 255))
        image.alpha_composite(subject, (index * height + 5, 4))
    return _png(image)


class FakeImageProvider:
    def __init__(self) -> None:
        self.generate_requests = []
        self.edit_requests = []

    def capabilities(self) -> ProviderCapabilities:
        return ProviderCapabilities(model="fake-sequence")

    async def generate(self, request):
        self.generate_requests.append(request)
        return [GeneratedImage(index=0, content=_strip(request.width, request.height))]

    async def edit(self, request):
        self.edit_requests.append(request)
        return [GeneratedImage(index=0, content=_strip(request.width, request.height))]


class FakeRegistry:
    def __init__(self) -> None:
        self.images = FakeImageProvider()

    def image_provider(self):
        return self.images

    def review_provider(self):
        raise AssertionError("sequence API must not call the review provider")


def _task(
    *,
    asset_id: str,
    category: AssetCategory,
    action: str,
    frame_count: int,
    base_path: str | None,
) -> dict[str, object]:
    return {
        "asset_id": asset_id,
        "category": category.value,
        "name": f"{action} 序列",
        "action": action,
        "frame_count": frame_count,
        "rows": 1,
        "columns": frame_count,
        "frame_width": 16,
        "frame_height": 16,
        "preview_fps": 12,
        "loop": action in {"idle", "move"},
        "baseline": "bottom_center" if category is AssetCategory.ANIMATION else "center",
        "base_frame_workspace_relative_path": base_path,
        "lock_first_frame": category is AssetCategory.ANIMATION,
        "pivot_x": 0.5,
        "pivot_y": 1 if category is AssetCategory.ANIMATION else 0.5,
        "blend_mode_hint": "additive" if category is AssetCategory.EFFECT else "alpha",
    }


@pytest.mark.asyncio
async def test_sequence_api_supports_five_actions_one_effect_and_artifact_reads(
    tmp_path: Path,
) -> None:
    platform_root = Path(__file__).resolve().parents[2]
    registry = FakeRegistry()
    app = create_app(
        Settings(
            data_dir=tmp_path / "data",
            preset_dir=platform_root / "shared/presets",
        ),
        provider_registry=registry,
    )

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        created_project = await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        assert created_project.status_code == httpx.codes.CREATED
        base_path = app.state.workspace.project_path("wuxia-demo") / "assets/hero/base.png"
        base_path.parent.mkdir(parents=True, exist_ok=True)
        base = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        base.paste(Image.new("RGBA", (6, 10), (80, 100, 60, 255)), (5, 4))
        base_path.write_bytes(_png(base))

        run_ids: list[str] = []
        for action, frame_count in ACTION_TEMPLATES.items():
            asset_id = f"hero-{action}"
            created = await client.post(
                "/api/v1/projects/wuxia-demo/sequences",
                json=_task(
                    asset_id=asset_id,
                    category=AssetCategory.ANIMATION,
                    action=action,
                    frame_count=frame_count,
                    base_path="assets/hero/base.png",
                ),
            )
            assert created.status_code == httpx.codes.CREATED
            run_id = created.json()["run_id"]
            run_ids.append(run_id)
            generated = await client.post(
                f"/api/v1/projects/wuxia-demo/sequences/animation/{asset_id}/runs/{run_id}/generate",
                json={"candidate_count": 1},
            )
            assert generated.status_code == httpx.codes.OK
            assert generated.json()["candidates"][0]["output"]["frame_count"] == frame_count

        effect_created = await client.post(
            "/api/v1/projects/wuxia-demo/sequences",
            json=_task(
                asset_id="sword-flash",
                category=AssetCategory.EFFECT,
                action="slash",
                frame_count=6,
                base_path=None,
            ),
        )
        effect_run_id = effect_created.json()["run_id"]
        effect_generated = await client.post(
            f"/api/v1/projects/wuxia-demo/sequences/effect/sword-flash/runs/{effect_run_id}/generate",
            json={"candidate_count": 1},
        )
        assert effect_generated.status_code == httpx.codes.OK
        assert len(registry.images.edit_requests) == 5
        assert len(registry.images.generate_requests) == 1

        asset_id = "hero-idle"
        run_id = run_ids[0]
        selected = await client.post(
            f"/api/v1/projects/wuxia-demo/sequences/animation/{asset_id}/runs/{run_id}/select",
            json={"candidate_id": "candidate-0"},
        )
        assert selected.status_code == httpx.codes.OK
        reprocessed = await client.post(
            f"/api/v1/projects/wuxia-demo/sequences/animation/{asset_id}/runs/{run_id}/reprocess"
        )
        assert reprocessed.status_code == httpx.codes.OK

        for suffix, media_type in (
            ("frames/0", "image/png"),
            ("sprite-sheet", "image/png"),
            ("preview.gif", "image/gif"),
            ("preview.webp", "image/webp"),
            ("drift-report", "application/json"),
        ):
            artifact = await client.get(
                f"/api/v1/projects/wuxia-demo/sequences/animation/{asset_id}/runs/{run_id}/candidates/candidate-0/{suffix}"
            )
            assert artifact.status_code == httpx.codes.OK
            assert artifact.headers["content-type"].startswith(media_type)

        history = await client.get(
            f"/api/v1/projects/wuxia-demo/sequences/animation/{asset_id}/runs"
        )
        assert history.status_code == httpx.codes.OK
        assert history.json()[0]["run_id"] == run_id

        exported = await client.post(
            f"/api/v1/projects/wuxia-demo/sequences/animation/{asset_id}/runs/{run_id}/export"
        )
        assert exported.status_code == httpx.codes.OK
        assert len(exported.json()["files"]) == ACTION_TEMPLATES["idle"] + 4
