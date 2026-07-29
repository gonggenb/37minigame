import base64
from io import BytesIO
from pathlib import Path

import httpx
import pytest
import yaml
from app.config.settings import Settings
from app.main import create_app
from app.schemas.core import AssetCategory
from PIL import Image, ImageDraw


def _profile(category: AssetCategory) -> dict[str, object]:
    return {
        "schema_version": 1,
        "profile_id": f"wuxia-{category.value}",
        "category": category.value,
        "master_width": 1024,
        "master_height": 1024,
        "output_width": 32,
        "output_height": 32,
        "require_rgba": category is not AssetCategory.SCENE,
        "require_transparency": category is not AssetCategory.SCENE,
        "crop_mode": "none" if category is AssetCategory.SCENE else "alpha_bounds",
        "padding_ratio": 0.125,
        "occupancy_ratio": 0.75,
        "resize_algorithm": "nearest",
        "pivot_x": 0.5,
        "pivot_y": 0.5,
        "filename_template": "{asset_id}_{variant}.png",
        "max_file_bytes": 8388608,
        "output_sprite_sheet": False,
        "shared_scale": True,
        "lock_first_frame": False,
    }


def _write_constraint_presets(preset_dir: Path) -> None:
    target = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "constraints"
    target.mkdir(parents=True)
    for category in AssetCategory:
        (target / f"{category.value}.yaml").write_text(
            yaml.safe_dump(_profile(category), sort_keys=False),
            encoding="utf-8",
        )


@pytest.mark.asyncio
async def test_constraint_configuration_preview_and_export_routes(tmp_path: Path) -> None:
    data_dir = tmp_path / "data"
    preset_dir = tmp_path / "presets"
    _write_constraint_presets(preset_dir)
    app = create_app(Settings(data_dir=data_dir, preset_dir=preset_dir))

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        created = await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        assert created.status_code == httpx.codes.CREATED
        source_path = (
            data_dir
            / "workspaces"
            / "wuxia-demo"
            / "style-pack"
            / "references"
            / "source.png"
        )
        source_path.parent.mkdir(parents=True, exist_ok=True)
        source = Image.new("RGB", (64, 64), (250, 248, 240))
        ImageDraw.Draw(source).rectangle((16, 12, 47, 51), fill=(150, 50, 30))
        source.save(source_path)

        profiles = await client.get("/api/v1/projects/wuxia-demo/constraints")
        assert profiles.status_code == httpx.codes.OK
        assert set(profiles.json()) == {category.value for category in AssetCategory}

        item_profile = profiles.json()["item"]
        item_profile["output_width"] = 48
        item_profile["output_height"] = 48
        updated = await client.put(
            "/api/v1/projects/wuxia-demo/constraints/item",
            json=item_profile,
        )
        assert updated.status_code == httpx.codes.OK
        assert updated.json()["output_width"] == 48

        request_payload = {
            "workspace_relative_path": "style-pack/references/source.png",
            "asset_id": "sword-001",
            "variant": "default",
            "background": {
                "mode": "corner_flood",
                "color_tolerance": 8,
                "alpha_low_threshold": 8,
                "alpha_high_threshold": 247,
            },
        }
        preview = await client.post(
            "/api/v1/projects/wuxia-demo/constraints/item/process-preview",
            json=request_payload,
        )
        assert preview.status_code == httpx.codes.OK
        preview_payload = preview.json()
        assert preview_payload["metadata"]["width"] == 48
        assert preview_payload["hard_constraints"]["passed"] is True
        decoded = base64.b64decode(preview_payload["processed_png_base64"])
        with Image.open(BytesIO(decoded)) as image:
            assert image.size == (48, 48)

        exported = await client.post(
            "/api/v1/projects/wuxia-demo/constraints/item/export",
            json=request_payload,
        )
        assert exported.status_code == httpx.codes.OK
        assert exported.json()["relative_path"].endswith("sword-001_default.png")

        conflict = await client.post(
            "/api/v1/projects/wuxia-demo/constraints/item/export",
            json=request_payload,
        )
        assert conflict.status_code == httpx.codes.CONFLICT

        traversal = await client.post(
            "/api/v1/projects/wuxia-demo/constraints/item/process-preview",
            json={**request_payload, "workspace_relative_path": "../outside.png"},
        )
        assert traversal.status_code == httpx.codes.UNPROCESSABLE_ENTITY
