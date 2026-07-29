from collections.abc import Iterator
from io import BytesIO
from pathlib import Path

import pytest
from app.constraints.exporter import ExportConflict
from app.constraints.workspace import ConstraintWorkspace
from app.production.sequence_service import SequenceProductionService
from app.providers.models import GeneratedImage, ProviderCapabilities
from app.schemas.core import AssetCategory, ProjectConfig
from app.schemas.sequence import (
    SequenceGenerationRequest,
    SequenceSelection,
    SequenceTask,
)
from app.workspace.project_workspace import ProjectWorkspace
from PIL import Image


def _png(image: Image.Image) -> bytes:
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _base_frame() -> bytes:
    image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    image.paste(Image.new("RGBA", (6, 10), (90, 120, 70, 255)), (5, 4))
    return _png(image)


def _generated_grid(width: int, height: int) -> bytes:
    frame_size = 32 if height >= 32 else height
    rows = height // frame_size
    columns = width // frame_size
    frame_count = rows * columns
    strip = Image.new("RGBA", (width, height), (245, 238, 218, 255))
    for index in range(frame_count):
        color = (70 + index * 10, 100, 60 + index * 5, 255)
        subject_width = 6
        subject_height = 10
        subject = Image.new("RGBA", (subject_width, subject_height), color)
        left = (index % columns) * frame_size + (frame_size - subject_width) // 2
        top = (index // columns) * frame_size + frame_size - subject_height
        strip.alpha_composite(subject, (left, top))
    return _png(strip)


class FakeImageProvider:
    def __init__(self) -> None:
        self.generate_requests = []
        self.edit_requests = []

    def capabilities(self) -> ProviderCapabilities:
        return ProviderCapabilities(model="fake-sequence", max_candidates=4)

    async def generate(self, request):
        self.generate_requests.append(request)
        return [
            GeneratedImage(index=0, content=_generated_grid(request.width, request.height))
        ]

    async def edit(self, request):
        self.edit_requests.append(request)
        return [
            GeneratedImage(index=0, content=_generated_grid(request.width, request.height))
        ]


class FakeRegistry:
    def __init__(self) -> None:
        self.images = FakeImageProvider()

    def image_provider(self):
        return self.images

    def review_provider(self):
        raise AssertionError("sequence production does not use the review provider")


def _ids() -> Iterator[str]:
    yield "run-animation"
    yield "run-effect"


def _animation_task() -> SequenceTask:
    return SequenceTask(
        asset_id="hero-idle",
        category=AssetCategory.ANIMATION,
        name="少侠待机",
        action="idle",
        frame_count=4,
        rows=2,
        columns=2,
        generation_frame_width=32,
        generation_frame_height=32,
        frame_width=16,
        frame_height=16,
        preview_fps=8,
        loop=True,
        baseline="bottom_center",
        base_frame_workspace_relative_path="assets/character/hero/selected/base.png",
        lock_first_frame=True,
        pivot_x=0.5,
        pivot_y=1,
    )


def _effect_task() -> SequenceTask:
    return SequenceTask(
        asset_id="sword-flash",
        category=AssetCategory.EFFECT,
        name="剑光特效",
        action="slash",
        frame_count=6,
        rows=2,
        columns=3,
        generation_frame_width=32,
        generation_frame_height=32,
        frame_width=16,
        frame_height=16,
        preview_fps=20,
        loop=False,
        baseline="center",
        blend_mode_hint="additive",
    )


def _service(tmp_path: Path):
    platform_root = Path(__file__).resolve().parents[2]
    workspace = ProjectWorkspace(tmp_path / "data")
    workspace.create_project(ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"))
    base_path = workspace.project_path("wuxia-demo") / "assets/character/hero/selected/base.png"
    base_path.parent.mkdir(parents=True, exist_ok=True)
    base_path.write_bytes(_base_frame())
    registry = FakeRegistry()
    ids = _ids()
    service = SequenceProductionService(
        workspace,
        ConstraintWorkspace(workspace, platform_root / "shared/presets"),
        registry,
        id_factory=lambda: next(ids),
    )
    return service, workspace, registry


@pytest.mark.asyncio
async def test_animation_uses_one_edit_call_and_persists_all_sequence_outputs(
    tmp_path: Path,
) -> None:
    service, workspace, registry = _service(tmp_path)
    created = service.create_reference("wuxia-demo", _animation_task())

    assert created.status == "reference_ready"
    reference_path = workspace.project_path("wuxia-demo") / created.reference_grid_relative_path
    assert reference_path.is_file()
    with Image.open(reference_path) as reference:
        assert reference.size == (64, 64)
        assert reference.crop((0, 0, 32, 32)).getchannel("A").getbbox() is not None
        assert reference.crop((32, 0, 64, 64)).getchannel("A").getbbox() is None

    assert "模型网格每格严格 32 × 32 px" in created.prompt
    assert "最终归一化为每帧 16 × 16 px" in created.prompt
    assert "透明背景" not in created.prompt

    generated = await service.generate(
        "wuxia-demo",
        AssetCategory.ANIMATION,
        "hero-idle",
        created.run_id,
        SequenceGenerationRequest(prompt_override="保持少侠身份并生成完整待机条带"),
    )

    assert generated.status == "processed"
    assert len(registry.images.edit_requests) == 1
    assert len(registry.images.generate_requests) == 0
    assert registry.images.edit_requests[0].background == "opaque"
    assert registry.images.edit_requests[0].width == 64
    assert registry.images.edit_requests[0].height == 64
    assert len(generated.candidates) == 1
    candidate = generated.candidates[0]
    assert candidate.output is not None
    assert candidate.output.frame_count == 4
    assert candidate.output.frame_width == 16
    assert candidate.output.frame_height == 16
    assert candidate.output.sprite_sheet_width == 32
    assert candidate.output.sprite_sheet_height == 32
    for relative_path in (
        candidate.raw_strip_relative_path,
        *candidate.output.frame_relative_paths,
        candidate.output.sprite_sheet_relative_path,
        candidate.output.gif_relative_path,
        candidate.output.webp_relative_path,
        candidate.output.drift_report_relative_path,
    ):
        assert (workspace.project_path("wuxia-demo") / relative_path).is_file()
    run_path = service.run_path(generated)
    assert (run_path / "sequence-task.json").is_file()
    assert (run_path / "run.json").is_file()

    selected = service.select(
        "wuxia-demo",
        AssetCategory.ANIMATION,
        "hero-idle",
        generated.run_id,
        SequenceSelection(candidate_id="candidate-0"),
    )
    exported = service.export(
        "wuxia-demo",
        AssetCategory.ANIMATION,
        "hero-idle",
        selected.run_id,
    )
    assert len(exported.files) == 8
    assert all(
        (workspace.project_path("wuxia-demo") / item.relative_path).is_file()
        for item in exported.files
    )
    with pytest.raises(ExportConflict):
        service.export(
            "wuxia-demo",
            AssetCategory.ANIMATION,
            "hero-idle",
            selected.run_id,
        )


@pytest.mark.asyncio
async def test_effect_uses_one_generate_call_and_reprocesses_without_model_calls(
    tmp_path: Path,
) -> None:
    service, _, registry = _service(tmp_path)
    created = service.create_reference("wuxia-demo", _effect_task())
    generated = await service.generate(
        "wuxia-demo",
        AssetCategory.EFFECT,
        "sword-flash",
        created.run_id,
        SequenceGenerationRequest(),
    )

    assert len(registry.images.generate_requests) == 1
    assert len(registry.images.edit_requests) == 0
    assert registry.images.generate_requests[0].background == "opaque"
    assert registry.images.generate_requests[0].width == 96
    assert registry.images.generate_requests[0].height == 64
    assert generated.candidates[0].output.sprite_sheet_width == 48
    assert generated.candidates[0].output.sprite_sheet_height == 32
    before_hash = generated.candidates[0].output.content_sha256
    reprocessed = service.reprocess(
        "wuxia-demo",
        AssetCategory.EFFECT,
        "sword-flash",
        generated.run_id,
    )
    assert reprocessed.candidates[0].output.content_sha256 == before_hash
    assert len(registry.images.generate_requests) == 1
    assert len(registry.images.edit_requests) == 0


def test_legacy_sequence_task_uses_final_frame_size_for_reference_grid(
    tmp_path: Path,
) -> None:
    service, workspace, _ = _service(tmp_path)
    legacy = SequenceTask(
        asset_id="hero-idle",
        category=AssetCategory.ANIMATION,
        name="旧版少侠待机",
        action="idle",
        frame_count=4,
        rows=1,
        columns=4,
        frame_width=16,
        frame_height=16,
        preview_fps=8,
        loop=True,
        baseline="bottom_center",
        base_frame_workspace_relative_path="assets/character/hero/selected/base.png",
        lock_first_frame=True,
        pivot_x=0.5,
        pivot_y=1,
    )

    created = service.create_reference("wuxia-demo", legacy)
    reference_path = workspace.project_path("wuxia-demo") / created.reference_grid_relative_path
    with Image.open(reference_path) as reference:
        assert reference.size == (64, 16)
