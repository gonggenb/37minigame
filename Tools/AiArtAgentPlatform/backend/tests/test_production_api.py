import base64
from io import BytesIO
from pathlib import Path

import httpx
import pytest
from app.config.settings import Settings
from app.main import create_app
from app.providers.models import GeneratedImage, ProviderCapabilities
from app.schemas.core import (
    AssetCategory,
    GenerationPlan,
    HardConstraintReport,
    ImageOutputSpec,
    QualityReport,
    StyleReview,
)
from PIL import Image, ImageDraw


def _png() -> bytes:
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    ImageDraw.Draw(image).rectangle((12, 8, 51, 55), fill=(60, 110, 85, 255))
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


class FakeImageProvider:
    def capabilities(self) -> ProviderCapabilities:
        return ProviderCapabilities(model="fake-image")

    async def generate(self, request):
        return [
            GeneratedImage(index=index, content=_png())
            for index in range(request.candidate_count)
        ]

    async def edit(self, request):
        return [GeneratedImage(index=0, content=_png())]


class FakeReviewProvider:
    async def plan(self, request):
        return GenerationPlan(
            asset_type=request.task.category,
            usage=request.task.usage,
            selected_reference_ids=[],
            composition="主体清晰",
            camera="Q 版水墨武侠 2.5D 俯视角",
            lighting="左上柔光",
            identity_constraints=["保持任务身份"],
            prompt=f"生成{request.task.name}，不含文字",
            negative_constraints=["写实照片", "霓虹"],
            output_spec=ImageOutputSpec(width=1024, height=1024),
            postprocess_steps=["确定性约束处理"],
            quality_checks=["硬约束", "风格一致性"],
            repair_strategy=["只修复失败维度"],
        )

    async def review(self, request):
        return QualityReport(
            hard_constraints=HardConstraintReport(passed=True, checks=[]),
            style_review=StyleReview(
                score=82,
                identity_score=84,
                palette_score=82,
                line_style_score=80,
                composition_score=83,
                issues=[],
            ),
            export_allowed=True,
        )


class FakeRegistry:
    def __init__(self) -> None:
        self.images = FakeImageProvider()
        self.reviews = FakeReviewProvider()

    def image_provider(self):
        return self.images

    def review_provider(self):
        return self.reviews


def _task(category: AssetCategory, asset_id: str) -> dict[str, object]:
    return {
        "asset_id": asset_id,
        "category": category.value,
        "name": f"{category.value} 测试资产",
        "brief": "统一 Q 版水墨武侠轻量化风格",
        "usage": "gameplay",
        "style_pack": "wuxia-ink-chibi-topdown-2-5d",
        "reference_ids": [],
        "constraint_profile": f"wuxia-{category.value}",
        "constraint_overrides": {},
        "candidate_count": 1,
        "output_mode": "single-png",
    }


@pytest.mark.asyncio
async def test_static_asset_api_completes_all_four_category_loops(
    tmp_path: Path,
) -> None:
    platform_root = Path(__file__).resolve().parents[2]
    app = create_app(
        Settings(
            data_dir=tmp_path / "data",
            preset_dir=platform_root / "shared" / "presets",
        ),
        provider_registry=FakeRegistry(),
    )

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        created_project = await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        assert created_project.status_code == httpx.codes.CREATED

        for category in (
            AssetCategory.ITEM,
            AssetCategory.UI,
            AssetCategory.CHARACTER,
            AssetCategory.SCENE,
        ):
            asset_id = f"{category.value}-demo"
            created = await client.post(
                "/api/v1/projects/wuxia-demo/assets",
                json=_task(category, asset_id),
            )
            assert created.status_code == httpx.codes.CREATED

            planned = await client.post(
                f"/api/v1/projects/wuxia-demo/assets/{category.value}/{asset_id}/plan"
            )
            assert planned.status_code == httpx.codes.OK
            run_id = planned.json()["run_id"]

            generated = await client.post(
                f"/api/v1/projects/wuxia-demo/assets/{category.value}/{asset_id}/runs/{run_id}/generate",
                json={"candidate_count": 1},
            )
            assert generated.status_code == httpx.codes.OK
            assert generated.json()["candidates"][0]["hard_constraints"]["passed"] is True

            image = await client.get(
                f"/api/v1/projects/wuxia-demo/assets/{category.value}/{asset_id}/runs/{run_id}/candidates/candidate-0/image"
            )
            assert image.status_code == httpx.codes.OK
            assert image.headers["content-type"] == "image/png"

            selected = await client.post(
                f"/api/v1/projects/wuxia-demo/assets/{category.value}/{asset_id}/runs/{run_id}/select",
                json={"candidate_id": "candidate-0"},
            )
            assert selected.status_code == httpx.codes.OK

            reviewed = await client.post(
                f"/api/v1/projects/wuxia-demo/assets/{category.value}/{asset_id}/runs/{run_id}/review",
                json={"candidate_id": "candidate-0"},
            )
            assert reviewed.status_code == httpx.codes.OK
            assert reviewed.json()["candidates"][0]["quality_report"]["style_review"]["score"] == 82

            comparison = await client.get(
                f"/api/v1/projects/wuxia-demo/assets/{category.value}/{asset_id}/runs/{run_id}/candidates/candidate-0/comparison"
            )
            assert comparison.status_code == httpx.codes.OK
            assert comparison.headers["content-type"] == "image/png"

            exported = await client.post(
                f"/api/v1/projects/wuxia-demo/assets/{category.value}/{asset_id}/runs/{run_id}/export",
                json={"variant": "default", "accept_style_risk": False},
            )
            assert exported.status_code == httpx.codes.OK
            assert exported.json()["export"]["category"] == category.value

        assets = await client.get("/api/v1/projects/wuxia-demo/assets")
        assert assets.status_code == httpx.codes.OK
        assert len(assets.json()) == 4


@pytest.mark.asyncio
async def test_static_asset_api_supports_traced_candidate_edit(tmp_path: Path) -> None:
    platform_root = Path(__file__).resolve().parents[2]
    app = create_app(
        Settings(
            data_dir=tmp_path / "data",
            preset_dir=platform_root / "shared" / "presets",
        ),
        provider_registry=FakeRegistry(),
    )
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        await client.post(
            "/api/v1/projects/wuxia-demo/assets",
            json=_task(AssetCategory.ITEM, "sword-demo"),
        )
        planned = await client.post(
            "/api/v1/projects/wuxia-demo/assets/item/sword-demo/plan"
        )
        run_id = planned.json()["run_id"]
        await client.post(
            f"/api/v1/projects/wuxia-demo/assets/item/sword-demo/runs/{run_id}/generate",
            json={"candidate_count": 1},
        )

        edited = await client.post(
            f"/api/v1/projects/wuxia-demo/assets/item/sword-demo/runs/{run_id}/edit",
            json={
                "candidate_id": "candidate-0",
                "instruction": "只修改剑穗为朱红色",
                "candidate_count": 1,
                "mask_workspace_relative_path": None,
            },
        )

        assert edited.status_code == httpx.codes.OK
        assert edited.json()["source_run_id"] == run_id
        assert edited.json()["edit_instruction"] == "只修改剑穗为朱红色"


@pytest.mark.asyncio
async def test_static_asset_api_exposes_explicit_review_and_repair(tmp_path: Path) -> None:
    platform_root = Path(__file__).resolve().parents[2]
    app = create_app(
        Settings(
            data_dir=tmp_path / "data",
            preset_dir=platform_root / "shared" / "presets",
        ),
        provider_registry=FakeRegistry(),
    )
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        await client.post(
            "/api/v1/projects/wuxia-demo/assets",
            json=_task(AssetCategory.ITEM, "sword-demo"),
        )
        planned = await client.post(
            "/api/v1/projects/wuxia-demo/assets/item/sword-demo/plan"
        )
        run_id = planned.json()["run_id"]
        await client.post(
            f"/api/v1/projects/wuxia-demo/assets/item/sword-demo/runs/{run_id}/generate",
            json={"candidate_count": 1},
        )

        reviewed = await client.post(
            f"/api/v1/projects/wuxia-demo/assets/item/sword-demo/runs/{run_id}/review-and-repair",
            json={
                "candidate_id": "candidate-0",
                "automatic_repair": True,
                "max_retries": 2,
            },
        )

        assert reviewed.status_code == httpx.codes.OK
        assert reviewed.json()["auto_repair_summary"]["stop_reason"] == "passed"
        assert reviewed.json()["auto_repair_summary"]["retry_count"] == 0


@pytest.mark.asyncio
async def test_static_asset_api_supports_deterministic_transform_and_mask(
    tmp_path: Path,
) -> None:
    platform_root = Path(__file__).resolve().parents[2]
    app = create_app(
        Settings(
            data_dir=tmp_path / "data",
            preset_dir=platform_root / "shared" / "presets",
        ),
        provider_registry=FakeRegistry(),
    )
    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        await client.post(
            "/api/v1/projects/wuxia-demo/assets",
            json=_task(AssetCategory.ITEM, "sword-demo"),
        )
        planned = await client.post(
            "/api/v1/projects/wuxia-demo/assets/item/sword-demo/plan"
        )
        run_id = planned.json()["run_id"]
        await client.post(
            f"/api/v1/projects/wuxia-demo/assets/item/sword-demo/runs/{run_id}/generate",
            json={"candidate_count": 1},
        )

        transformed = await client.post(
            f"/api/v1/projects/wuxia-demo/assets/item/sword-demo/runs/{run_id}/transform",
            json={
                "candidate_id": "candidate-0",
                "crop": {"x": 4, "y": 4, "width": 56, "height": 56},
                "output_width": 96,
                "output_height": 80,
                "padding_ratio": 0.2,
                "remove_background": False,
            },
        )
        assert transformed.status_code == httpx.codes.OK
        transformed_run = transformed.json()
        assert transformed_run["candidates"][0]["metadata"]["width"] == 96
        assert transformed_run["candidates"][0]["metadata"]["height"] == 80
        assert transformed_run["task"]["constraint_overrides"]["output_width"] == 96

        mask = Image.new("RGBA", (96, 80), (0, 0, 0, 0))
        ImageDraw.Draw(mask).rectangle((20, 20, 40, 40), fill=(255, 0, 0, 180))
        stream = BytesIO()
        mask.save(stream, format="PNG")
        saved_mask = await client.post(
            "/api/v1/projects/wuxia-demo/assets/item/sword-demo/"
            f"runs/{transformed_run['run_id']}/candidates/candidate-0/mask",
            json={"mask_png_base64": base64.b64encode(stream.getvalue()).decode("ascii")},
        )

        assert saved_mask.status_code == httpx.codes.CREATED
        assert saved_mask.json()["workspace_relative_path"].endswith(".png")
