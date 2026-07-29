from pathlib import Path

from app.agent.prompt_compiler import PromptCompiler
from app.agent.reference_selector import ReferenceSelector
from app.schemas.core import AssetCategory, AssetTask, ImageOutputSpec, ProjectConfig
from app.schemas.style_pack import (
    CharacterIdentity,
    PromptPreviewRequest,
    ReferenceAsset,
    StyleGuide,
)
from app.style_pack.identity import CharacterIdentityStore
from app.workspace.project_workspace import ProjectWorkspace


def _style_guide(tmp_path: Path) -> StyleGuide:
    return StyleGuide.model_validate(
        {
            "schema_version": 1,
            "style_id": "wuxia-ink-chibi-topdown-2_5d",
            "display_name": "Q版水墨武侠俯视角",
            "reference_source": {"path": str(tmp_path.resolve()), "mode": "read_only"},
            "camera": {
                "projection": "orthographic_like",
                "pitch_semantic_min": 35,
                "pitch_semantic_max": 55,
                "shared_view_required": True,
                "default_facing": "right",
            },
            "palette": {
                "base": ["rice_paper", "ink_gray", "moss_green"],
                "accents": ["vermilion", "dark_gold"],
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
            "ui": {
                "formal_text_baked_in": False,
                "border_language": ["ink_edge", "rice_paper"],
            },
            "forbidden": ["pixel_art", "photorealism", "baked_text"],
        }
    )


def _identity() -> CharacterIdentity:
    return CharacterIdentity(
        asset_id="hero-main",
        display_name="青衣少侠",
        silhouette=["二头身", "短披风"],
        face=["圆脸", "坚定眉眼"],
        hair=["高马尾"],
        costume=["青灰短打", "朱红腰带"],
        palette=["青灰", "朱红"],
        equipment=["木柄长剑"],
        immutable_traits=["左侧发带", "右手持剑"],
    )


def _task() -> AssetTask:
    return AssetTask(
        asset_id="hero-main",
        category=AssetCategory.CHARACTER,
        name="青衣少侠游戏内基准帧",
        brief="2.5D 俯视角站立，轮廓清楚，适合后续逐帧动画",
        usage="gameplay",
        style_pack="wuxia-ink-chibi-topdown-2_5d",
        constraint_profile="character-gameplay",
        output_mode="single-png",
    )


def _reference(
    reference_id: str,
    category: AssetCategory,
    *,
    identities: list[str] | None = None,
    usages: list[str] | None = None,
    viewpoints: list[str] | None = None,
) -> ReferenceAsset:
    return ReferenceAsset(
        reference_id=reference_id,
        source_relative_path=f"source/{reference_id}.png",
        workspace_relative_path=f"style-pack/references/{reference_id}.png",
        thumbnail_relative_path=f"style-pack/thumbnails/{reference_id}.png",
        sha256="a" * 64,
        width=256,
        height=256,
        categories=[category],
        identities=identities or [],
        usages=usages or [],
        viewpoints=viewpoints or [],
    )


def test_character_identity_round_trip_uses_the_character_asset_directory(tmp_path: Path) -> None:
    projects = ProjectWorkspace(tmp_path)
    projects.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    store = CharacterIdentityStore(projects)

    store.save("wuxia-demo", _identity())

    assert store.get("wuxia-demo", "hero-main") == _identity()
    assert (
        tmp_path
        / "workspaces"
        / "wuxia-demo"
        / "assets"
        / "character"
        / "hero-main"
        / "identity.json"
    ).is_file()


def test_reference_selector_is_stable_relevant_and_limited_to_four() -> None:
    task = _task()
    references = [
        _reference("scene", AssetCategory.SCENE, viewpoints=["topdown-45"]),
        _reference("hero-side", AssetCategory.CHARACTER, identities=["hero-main"]),
        _reference(
            "hero-best",
            AssetCategory.CHARACTER,
            identities=["hero-main"],
            usages=["gameplay"],
            viewpoints=["topdown-45"],
        ),
        _reference("hero-gameplay", AssetCategory.CHARACTER, usages=["gameplay"]),
        _reference("hero-view", AssetCategory.CHARACTER, viewpoints=["topdown-45"]),
        _reference("hero-extra", AssetCategory.CHARACTER),
    ]

    first = ReferenceSelector.select(
        task,
        references,
        identity_id="hero-main",
        viewpoint="topdown-45",
    )
    second = ReferenceSelector.select(
        task,
        list(reversed(references)),
        identity_id="hero-main",
        viewpoint="topdown-45",
    )

    assert len(first) == 4
    assert first[0].reference_id == "hero-best"
    assert [item.reference_id for item in first] == [item.reference_id for item in second]
    assert "scene" not in {item.reference_id for item in first}


def test_prompt_compiler_uses_fixed_sections_and_preserves_manual_override(
    tmp_path: Path,
) -> None:
    task = _task()
    references = [
        _reference(
            "hero-best",
            AssetCategory.CHARACTER,
            identities=["hero-main"],
            usages=["gameplay"],
            viewpoints=["topdown-45"],
        )
    ]
    request = PromptPreviewRequest(
        task=task,
        identity=_identity(),
        viewpoint="topdown-45",
        composition="单人全身，底部中心锚点，主体占框 75%",
        lighting="柔和左上主光",
        materials=["宣纸肌理", "哑光布料"],
        output_spec=ImageOutputSpec(width=1024, height=1024, transparent_required=True),
        additional_negative_constraints=["no extra weapon"],
    )

    compiled = PromptCompiler.compile(_style_guide(tmp_path), request, references)
    overridden = PromptCompiler.compile(
        _style_guide(tmp_path),
        request.model_copy(update={"prompt_override": "人工确认后的最终提示词"}),
        references,
    )

    assert [section.key for section in compiled.sections] == [
        "project_style",
        "asset_task",
        "identity",
        "references",
        "composition_camera",
        "lighting_materials",
        "output_spec",
        "forbidden",
        "postprocess",
    ]
    assert compiled.selected_reference_ids == ["hero-best"]
    assert "青衣少侠" in compiled.prompt
    assert "no extra weapon" in compiled.negative_constraints
    assert overridden.prompt == "人工确认后的最终提示词"
    assert overridden.sections == compiled.sections
    assert overridden.selected_reference_ids == compiled.selected_reference_ids
