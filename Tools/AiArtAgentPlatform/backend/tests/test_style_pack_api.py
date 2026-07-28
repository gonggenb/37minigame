import hashlib
from pathlib import Path

import httpx
import pytest
import yaml
from app.config.settings import Settings
from app.main import create_app
from PIL import Image


def _write_preset(preset_dir: Path, source_root: Path) -> None:
    preset_path = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "style-guide.yaml"
    preset_path.parent.mkdir(parents=True)
    preset_path.write_text(
        yaml.safe_dump(
            {
                "schema_version": 1,
                "style_id": "wuxia-ink-chibi-topdown-2_5d",
                "display_name": "Q版水墨武侠俯视角",
                "reference_source": {"path": str(source_root), "mode": "read_only"},
                "camera": {
                    "projection": "orthographic_like",
                    "pitch_semantic_min": 35,
                    "pitch_semantic_max": 55,
                    "shared_view_required": True,
                    "default_facing": "right",
                },
                "palette": {
                    "base": ["rice_paper", "ink_gray"],
                    "accents": ["vermilion"],
                },
                "rendering": {
                    "character_proportion": "chibi_wuxia",
                    "character_outline": "clean_ink",
                    "environment_detail": "restrained",
                    "surface_finish": "matte_painted_2d",
                    "shadow_direction": "lower_right",
                },
                "readability": {
                    "protect_playfield": True,
                    "character_contrast_above_environment": True,
                    "preserve_clear_silhouette": True,
                    "avoid_high_frequency_ground_noise": True,
                },
                "ui": {"formal_text_baked_in": False, "border_language": ["ink_edge"]},
                "forbidden": ["pixel_art", "photorealism", "baked_text"],
            },
            allow_unicode=True,
        ),
        encoding="utf-8",
    )


@pytest.mark.asyncio
async def test_style_pack_reference_identity_and_prompt_preview_routes(tmp_path: Path) -> None:
    data_dir = tmp_path / "data"
    preset_dir = tmp_path / "presets"
    source_root = tmp_path / "source"
    source_root.mkdir()
    _write_preset(preset_dir, source_root)
    Image.new("RGBA", (320, 480), (70, 90, 70, 255)).save(source_root / "hero.png")
    app = create_app(Settings(data_dir=data_dir, preset_dir=preset_dir))

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        created = await client.post(
            "/api/v1/projects",
            json={"project_id": "wuxia-demo", "display_name": "武侠美术"},
        )
        assert created.status_code == httpx.codes.CREATED

        guide = await client.get("/api/v1/projects/wuxia-demo/style-guide")
        assert guide.status_code == httpx.codes.OK
        assert guide.json()["display_name"] == "Q版水墨武侠俯视角"

        source_files = await client.get("/api/v1/projects/wuxia-demo/reference-source")
        assert source_files.status_code == httpx.codes.OK
        assert source_files.json()[0]["relative_path"] == "hero.png"

        imported = await client.post(
            "/api/v1/projects/wuxia-demo/references",
            json={
                "reference_id": "hero-main",
                "source_relative_path": "hero.png",
                "categories": ["character"],
                "identities": ["hero-main"],
                "usages": ["gameplay"],
                "viewpoints": ["topdown-45"],
                "materials": ["ink-cloth"],
            },
        )
        assert imported.status_code == httpx.codes.CREATED
        imported_payload = imported.json()
        assert imported_payload["workspace_relative_path"].startswith(
            "style-pack/references/"
        )
        assert str(data_dir) not in str(imported_payload)

        source_hash = hashlib.sha256((source_root / "hero.png").read_bytes()).hexdigest()
        updated = await client.put(
            "/api/v1/projects/wuxia-demo/references/hero-main",
            json={
                "categories": ["character", "animation"],
                "identities": ["hero-main"],
                "usages": ["gameplay", "animation-seed"],
                "viewpoints": ["topdown-45"],
                "materials": ["rice-paper"],
                "notes": "批准参考",
            },
        )
        assert updated.status_code == httpx.codes.OK
        assert updated.json()["materials"] == ["rice-paper"]

        thumbnail = await client.get(
            "/api/v1/projects/wuxia-demo/references/hero-main/thumbnail"
        )
        assert thumbnail.status_code == httpx.codes.OK
        assert thumbnail.headers["content-type"] == "image/png"
        assert thumbnail.content.startswith(b"\x89PNG")

        filtered_by_material = await client.get(
            "/api/v1/projects/wuxia-demo/references",
            params={"material": "rice-paper"},
        )
        assert [item["reference_id"] for item in filtered_by_material.json()] == [
            "hero-main"
        ]
        assert hashlib.sha256((source_root / "hero.png").read_bytes()).hexdigest() == source_hash

        duplicate = await client.post(
            "/api/v1/projects/wuxia-demo/references",
            json={
                "reference_id": "hero-main",
                "source_relative_path": "hero.png",
                "categories": ["character"],
            },
        )
        assert duplicate.status_code == httpx.codes.CONFLICT

        filtered = await client.get(
            "/api/v1/projects/wuxia-demo/references",
            params={
                "category": "character",
                "identity": "hero-main",
                "usage": "gameplay",
                "viewpoint": "topdown-45",
            },
        )
        assert [item["reference_id"] for item in filtered.json()] == ["hero-main"]

        identity_payload = {
            "asset_id": "hero-main",
            "display_name": "青衣少侠",
            "silhouette": ["二头身"],
            "face": ["圆脸"],
            "hair": ["高马尾"],
            "costume": ["青灰短打"],
            "palette": ["青灰", "朱红"],
            "equipment": ["木柄长剑"],
            "immutable_traits": ["左侧发带"],
        }
        saved_identity = await client.put(
            "/api/v1/projects/wuxia-demo/identities/hero-main",
            json=identity_payload,
        )
        assert saved_identity.status_code == httpx.codes.OK
        loaded_identity = await client.get(
            "/api/v1/projects/wuxia-demo/identities/hero-main"
        )
        assert loaded_identity.json()["display_name"] == "青衣少侠"

        preview = await client.post(
            "/api/v1/projects/wuxia-demo/prompt-preview",
            json={
                "task": {
                    "asset_id": "hero-main",
                    "category": "character",
                    "name": "青衣少侠基准帧",
                    "brief": "2.5D 俯视角站立，轮廓清楚",
                    "usage": "gameplay",
                    "style_pack": "wuxia-ink-chibi-topdown-2_5d",
                    "constraint_profile": "character-gameplay",
                    "candidate_count": 4,
                    "output_mode": "single-png",
                },
                "identity": identity_payload,
                "viewpoint": "topdown-45",
                "composition": "单人全身，底部中心锚点",
                "lighting": "柔和左上主光",
                "materials": ["宣纸肌理"],
                "output_spec": {
                    "width": 1024,
                    "height": 1024,
                    "format": "png",
                    "transparent_required": True,
                },
            },
        )
        assert preview.status_code == httpx.codes.OK
        assert preview.json()["selected_reference_ids"] == ["hero-main"]
        assert preview.json()["sections"][0]["key"] == "project_style"
        assert preview.json()["sections"][-1]["key"] == "postprocess"

        traversal = await client.post(
            "/api/v1/projects/wuxia-demo/references",
            json={
                "reference_id": "escape",
                "source_relative_path": "../outside.png",
                "categories": ["character"],
            },
        )
        assert traversal.status_code == httpx.codes.UNPROCESSABLE_ENTITY

        removed = await client.delete(
            "/api/v1/projects/wuxia-demo/references/hero-main"
        )
        assert removed.status_code == httpx.codes.NO_CONTENT


@pytest.mark.asyncio
async def test_style_pack_routes_report_missing_projects_and_identities(tmp_path: Path) -> None:
    preset_dir = tmp_path / "presets"
    source_root = tmp_path / "source"
    source_root.mkdir()
    _write_preset(preset_dir, source_root)
    app = create_app(Settings(data_dir=tmp_path / "data", preset_dir=preset_dir))

    async with httpx.AsyncClient(
        transport=httpx.ASGITransport(app=app), base_url="http://testserver"
    ) as client:
        missing_project = await client.get("/api/v1/projects/missing/style-guide")
        missing_identity = await client.get(
            "/api/v1/projects/missing/identities/hero-main"
        )
        missing_reference = await client.put(
            "/api/v1/projects/missing/references/hero-main",
            json={"categories": ["character"]},
        )
        missing_thumbnail = await client.get(
            "/api/v1/projects/missing/references/hero-main/thumbnail"
        )

    assert missing_project.status_code == httpx.codes.NOT_FOUND
    assert missing_identity.status_code == httpx.codes.NOT_FOUND
    assert missing_reference.status_code == httpx.codes.NOT_FOUND
    assert missing_thumbnail.status_code == httpx.codes.NOT_FOUND
