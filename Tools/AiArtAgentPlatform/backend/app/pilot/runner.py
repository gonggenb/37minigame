from __future__ import annotations

import argparse
import hashlib
from collections.abc import Iterable
from io import BytesIO
from pathlib import Path

import yaml
from PIL import Image, ImageEnhance, ImageOps

from app.constraints.validator import ConstraintValidator
from app.image_processing.pipeline import ImageProcessor
from app.schemas.core import AssetCategory, ConstraintProfile
from app.schemas.image_tools import BackgroundRemovalConfig
from app.schemas.pilot import (
    OfflinePilotManifest,
    OfflinePilotReport,
    PilotActionSpec,
    PilotArtifactRecord,
    PilotStaticAssetSpec,
)
from app.schemas.sequence import SequenceTask
from app.sequence_processing.pipeline import ProcessedSequence, SequenceProcessor
from app.workspace.atomic_store import (
    atomic_write_bytes,
    atomic_write_json,
    atomic_write_text,
    read_yaml,
)


class OfflinePilotRunner:
    def __init__(self, *, preset_dir: Path) -> None:
        self.preset_dir = preset_dir.resolve()

    @staticmethod
    def load_manifest(path: Path) -> OfflinePilotManifest:
        payload = read_yaml(path)
        if not isinstance(payload, dict):
            raise ValueError("pilot manifest must contain a mapping")
        return OfflinePilotManifest.model_validate(payload)

    def run(
        self,
        manifest: OfflinePilotManifest,
        *,
        output_root: Path,
    ) -> OfflinePilotReport:
        source_root = Path(manifest.source_root).resolve()
        output_root = output_root.resolve()
        if not source_root.is_dir():
            raise FileNotFoundError(source_root)
        if output_root.exists():
            raise FileExistsError(output_root)
        output_root.mkdir(parents=True)
        source_paths = self._all_source_paths(manifest, source_root)
        hashes_before = self._hashes(source_paths, source_root)
        artifacts: list[PilotArtifactRecord] = []

        self._copy_references(manifest, source_root, output_root)
        for static in manifest.static_assets:
            artifacts.append(
                self._process_static(static, source_root=source_root, output_root=output_root)
            )
        for action in manifest.actions:
            artifacts.extend(
                self._process_action(
                    manifest.character_id,
                    action,
                    source_root=source_root,
                    output_root=output_root,
                )
            )
        artifacts.extend(
            self._process_effect(manifest, source_root=source_root, output_root=output_root)
        )

        hashes_after = self._hashes(source_paths, source_root)
        source_unchanged = hashes_before == hashes_after
        if not source_unchanged:
            raise RuntimeError("source reference files changed during offline pilot")
        report = OfflinePilotReport(
            pilot_id=manifest.pilot_id,
            display_name=manifest.display_name,
            source_root=str(source_root),
            source_unchanged=True,
            reference_count=len(manifest.references),
            categories=list(AssetCategory),
            actions=[item.action for item in manifest.actions],
            artifacts=artifacts,
            limitations=[
                "未配置 OPENAI_API_KEY，未执行真实模型生成或视觉评审。",
                "受击动作由基准帧位移与红闪确定性生成，只用于验证五动作管线。",
                "移动、攻击和死亡在原始帧不足时使用循环或末帧保持补足模板帧数。",
                "未调用 Unity MCP、未进入 Unity Editor 或 Play Mode，仍需人工验收。",
            ],
        )
        atomic_write_json(
            output_root / "pilot-manifest.snapshot.json",
            manifest.model_dump(mode="json"),
        )
        atomic_write_json(
            output_root / "source-hashes.json",
            {"before": hashes_before, "after": hashes_after},
        )
        atomic_write_json(output_root / "pilot-report.json", report.model_dump(mode="json"))
        atomic_write_text(output_root / "pilot-report.md", self._report_markdown(report))
        atomic_write_text(output_root / "unity-acceptance.md", self._unity_acceptance(report))
        atomic_write_text(output_root / "pilot-rework-log.md", self._rework_log())
        return report

    def _copy_references(
        self,
        manifest: OfflinePilotManifest,
        source_root: Path,
        output_root: Path,
    ) -> None:
        index: list[dict[str, object]] = []
        for reference in manifest.references:
            source = self._source_path(source_root, reference.source_relative_path)
            content = source.read_bytes()
            suffix = source.suffix.casefold() or ".png"
            target = output_root / "references" / f"{reference.reference_id}{suffix}"
            atomic_write_bytes(target, content)
            with Image.open(BytesIO(content)) as opened:
                preview = ImageOps.contain(
                    opened.convert("RGBA"),
                    (256, 256),
                    method=Image.Resampling.LANCZOS,
                )
                width, height = opened.size
            thumbnail = Image.new("RGBA", (256, 256), (239, 232, 211, 255))
            thumbnail.alpha_composite(
                preview,
                ((256 - preview.width) // 2, (256 - preview.height) // 2),
            )
            thumbnail_stream = BytesIO()
            thumbnail.save(thumbnail_stream, format="PNG", compress_level=6)
            atomic_write_bytes(
                output_root / "thumbnails" / f"{reference.reference_id}.png",
                thumbnail_stream.getvalue(),
            )
            index.append(
                {
                    **reference.model_dump(mode="json"),
                    "width": width,
                    "height": height,
                    "sha256": hashlib.sha256(content).hexdigest(),
                }
            )
        atomic_write_json(output_root / "reference-index.json", {"references": index})

    def _process_static(
        self,
        spec: PilotStaticAssetSpec,
        *,
        source_root: Path,
        output_root: Path,
    ) -> PilotArtifactRecord:
        category = AssetCategory(spec.category)
        profile = self._profile(category)
        source = self._source_path(source_root, spec.source_relative_path)
        background = BackgroundRemovalConfig(
            mode="corner_flood" if profile.require_transparency else "preserve"
        )
        processed = ImageProcessor.process(source.read_bytes(), profile, background)
        filename = ConstraintValidator.expected_filename(
            profile,
            asset_id=spec.asset_id,
            variant="pilot",
        )
        hard = ConstraintValidator.validate(
            processed.content,
            profile,
            asset_id=spec.asset_id,
            variant="pilot",
            filename=filename,
        )
        target = output_root / "outputs" / category.value / spec.asset_id / filename
        atomic_write_bytes(target, processed.content)
        atomic_write_json(
            target.parent / "hard-constraints.json",
            hard.model_dump(mode="json"),
        )
        return PilotArtifactRecord(
            category=category,
            asset_id=spec.asset_id,
            kind="static_png",
            relative_path=target.relative_to(output_root).as_posix(),
            width=processed.metadata.width,
            height=processed.metadata.height,
            hard_constraints=hard,
        )

    def _process_action(
        self,
        character_id: str,
        spec: PilotActionSpec,
        *,
        source_root: Path,
        output_root: Path,
    ) -> list[PilotArtifactRecord]:
        images = [
            self._open_rgba(self._source_path(source_root, relative))
            for relative in spec.source_relative_paths
        ]
        frames = (
            self._derive_hit_frames(images[0], spec.frame_count)
            if spec.derive_hit_proxy
            else self._expand_frames(
                images,
                spec.frame_count,
                hold_last=spec.action == "death",
            )
        )
        strip = self._build_strip(frames, rows=1, columns=spec.frame_count)
        task = SequenceTask(
            asset_id=character_id,
            category=AssetCategory.ANIMATION,
            name=f"{character_id}-{spec.action}",
            action=spec.action,
            frame_count=spec.frame_count,
            rows=1,
            columns=spec.frame_count,
            frame_width=256,
            frame_height=256,
            preview_fps=spec.preview_fps,
            loop=spec.loop,
            baseline="bottom_center",
            base_frame_workspace_relative_path=spec.source_relative_paths[0],
            lock_first_frame=False,
            pivot_x=0.5,
            pivot_y=1.0,
            max_center_drift_px=spec.max_center_drift_px,
            max_size_drift_ratio=spec.max_size_drift_ratio,
            max_baseline_drift_px=spec.max_baseline_drift_px,
        )
        processed = SequenceProcessor.process(
            strip_png=self._encode_png(strip),
            task=task,
            profile=self._profile(AssetCategory.ANIMATION),
        )
        target = output_root / "outputs" / "animation" / character_id / spec.action
        self._write_sequence(target, processed)
        return self._sequence_artifacts(
            processed,
            category=AssetCategory.ANIMATION,
            asset_id=character_id,
            target=target,
            output_root=output_root,
        )

    def _process_effect(
        self,
        manifest: OfflinePilotManifest,
        *,
        source_root: Path,
        output_root: Path,
    ) -> list[PilotArtifactRecord]:
        spec = manifest.effect
        task = SequenceTask(
            asset_id=spec.asset_id,
            category=AssetCategory.EFFECT,
            name=spec.asset_id,
            action="effect",
            frame_count=spec.frame_count,
            rows=spec.rows,
            columns=spec.columns,
            frame_width=256,
            frame_height=256,
            preview_fps=spec.preview_fps,
            loop=spec.loop,
            baseline="center",
            blend_mode_hint=spec.blend_mode_hint,
        )
        processed = SequenceProcessor.process(
            strip_png=self._source_path(source_root, spec.source_relative_path).read_bytes(),
            task=task,
            profile=self._profile(AssetCategory.EFFECT),
        )
        target = output_root / "outputs" / "effect" / spec.asset_id
        self._write_sequence(target, processed)
        return self._sequence_artifacts(
            processed,
            category=AssetCategory.EFFECT,
            asset_id=spec.asset_id,
            target=target,
            output_root=output_root,
        )

    @staticmethod
    def _write_sequence(target: Path, processed: ProcessedSequence) -> None:
        for index, content in enumerate(processed.frame_pngs):
            atomic_write_bytes(target / "frames" / f"frame-{index:03d}.png", content)
        atomic_write_bytes(target / "sprite-sheet.png", processed.sprite_sheet_png)
        atomic_write_bytes(target / "preview.gif", processed.gif_preview)
        atomic_write_bytes(target / "preview.webp", processed.webp_preview)
        atomic_write_json(
            target / "drift-report.json",
            processed.drift_report.model_dump(mode="json"),
        )

    @staticmethod
    def _sequence_artifacts(
        processed: ProcessedSequence,
        *,
        category: AssetCategory,
        asset_id: str,
        target: Path,
        output_root: Path,
    ) -> list[PilotArtifactRecord]:
        return [
            PilotArtifactRecord(
                category=category,
                asset_id=asset_id,
                kind="sprite_sheet",
                relative_path=(target / "sprite-sheet.png").relative_to(output_root).as_posix(),
                drift_report=processed.drift_report,
            ),
            PilotArtifactRecord(
                category=category,
                asset_id=asset_id,
                kind="gif_preview",
                relative_path=(target / "preview.gif").relative_to(output_root).as_posix(),
                drift_report=processed.drift_report,
            ),
        ]

    def _profile(self, category: AssetCategory) -> ConstraintProfile:
        path = (
            self.preset_dir
            / "wuxia-ink-chibi-topdown-2_5d"
            / "constraints"
            / f"{category.value}.yaml"
        )
        payload = yaml.safe_load(path.read_text(encoding="utf-8"))
        if not isinstance(payload, dict):
            raise ValueError(f"invalid constraint profile: {category.value}")
        return ConstraintProfile.model_validate(payload)

    @staticmethod
    def _open_rgba(path: Path) -> Image.Image:
        with Image.open(path) as opened:
            return opened.convert("RGBA")

    @staticmethod
    def _expand_frames(
        images: list[Image.Image],
        frame_count: int,
        *,
        hold_last: bool,
    ) -> list[Image.Image]:
        if not images:
            raise ValueError("animation action requires source frames")
        if hold_last:
            return [
                images[min(index, len(images) - 1)].copy()
                for index in range(frame_count)
            ]
        if len(images) == 1:
            return [images[0].copy() for _ in range(frame_count)]
        ping_pong = [*images, *images[-2:0:-1]]
        return [ping_pong[index % len(ping_pong)].copy() for index in range(frame_count)]

    @staticmethod
    def _derive_hit_frames(base: Image.Image, frame_count: int) -> list[Image.Image]:
        offsets = (-3, 3, -1, 0)
        tint_strength = (0.45, 0.3, 0.12, 0.0)
        frames: list[Image.Image] = []
        for index in range(frame_count):
            offset = offsets[index % len(offsets)]
            strength = tint_strength[index % len(tint_strength)]
            canvas = Image.new("RGBA", base.size, (0, 0, 0, 0))
            canvas.alpha_composite(base, (offset, 0))
            if strength > 0:
                red = Image.new("RGBA", canvas.size, (180, 35, 25, 0))
                red.putalpha(
                    canvas.getchannel("A").point(
                        lambda value, strength=strength: round(value * strength)
                    )
                )
                canvas = Image.alpha_composite(canvas, red)
                canvas = ImageEnhance.Contrast(canvas).enhance(1.08)
            frames.append(canvas)
        return frames

    @staticmethod
    def _build_strip(
        frames: list[Image.Image],
        *,
        rows: int,
        columns: int,
    ) -> Image.Image:
        frame_width = max(frame.width for frame in frames)
        frame_height = max(frame.height for frame in frames)
        strip = Image.new(
            "RGBA",
            (columns * frame_width, rows * frame_height),
            (0, 0, 0, 0),
        )
        for index, frame in enumerate(frames):
            x = (index % columns) * frame_width + (frame_width - frame.width) // 2
            y = (index // columns) * frame_height + (frame_height - frame.height) // 2
            strip.alpha_composite(frame, (x, y))
        return strip

    @staticmethod
    def _encode_png(image: Image.Image) -> bytes:
        stream = BytesIO()
        image.save(stream, format="PNG", compress_level=6)
        return stream.getvalue()

    def _all_source_paths(
        self,
        manifest: OfflinePilotManifest,
        source_root: Path,
    ) -> list[Path]:
        relatives = {
            *(item.source_relative_path for item in manifest.references),
            *(item.source_relative_path for item in manifest.static_assets),
            *(path for action in manifest.actions for path in action.source_relative_paths),
            manifest.effect.source_relative_path,
        }
        return [self._source_path(source_root, relative) for relative in sorted(relatives)]

    @staticmethod
    def _source_path(source_root: Path, relative_path: str) -> Path:
        candidate = source_root.joinpath(*Path(relative_path).parts).resolve()
        try:
            candidate.relative_to(source_root)
        except ValueError as error:
            raise ValueError("pilot source path escaped the source root") from error
        if not candidate.is_file():
            raise FileNotFoundError(relative_path)
        return candidate

    @staticmethod
    def _hashes(paths: Iterable[Path], source_root: Path) -> dict[str, str]:
        return {
            path.relative_to(source_root).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in paths
        }

    @staticmethod
    def _report_markdown(report: OfflinePilotReport) -> str:
        lines = [
            f"# {report.display_name}",
            "",
            f"- 参考图：{report.reference_count} 张",
            f"- 六类覆盖：{'、'.join(item.value for item in report.categories)}",
            f"- 五动作：{'、'.join(report.actions)}",
            f"- 源文件未改变：{'是' if report.source_unchanged else '否'}",
            "",
            "## 输出",
            "",
        ]
        lines.extend(
            f"- `{item.category.value}` / `{item.asset_id}` / `{item.kind}`：`{item.relative_path}`"
            for item in report.artifacts
        )
        lines.extend(["", "## 限制", ""])
        lines.extend(f"- {item}" for item in report.limitations)
        return "\n".join(lines) + "\n"

    @staticmethod
    def _unity_acceptance(report: OfflinePilotReport) -> str:
        return f"""# Unity 6 人工验收清单

本文件由 `{report.pilot_id}` 离线试点生成。
本轮没有调用 Unity MCP，也没有进入 Unity Editor 或 Play Mode。

## 手动导入

1. 在 Unity 项目 `Assets/Art/Pilot/{report.pilot_id}/` 下创建临时验收目录。
2. 仅复制本 Pilot 的 `outputs/` 内容，不复制 `references/`。
3. 静态角色、物品、UI：Texture Type 设为 Sprite (2D and UI)，
   Alpha Is Transparency 开启，Compression 先设 None。
4. 场景：Texture Type 设为 Default 或 Sprite，保持 1920×1080，检查裁切是否损失关键路径和地标。
5. 动画 Sprite Sheet：Sprite Mode 设为 Multiple，按 256×256 Grid by Cell Size 切分；
   Pivot 设 Bottom Center。
6. 特效 Sprite Sheet：按 256×256、4×4 网格切分；Pivot 设 Center，
   分别人工观察 Alpha 与 Additive 材质效果。
7. 为待机、移动、攻击、受击、死亡各创建临时 Animation Clip，按报告 FPS 设置采样；
   死亡不循环，其余按 manifest 设置。

## 观察指标

- 角色脚底是否贴地，五动作是否有明显尺寸跳变或锚点漂移。
- 透明边缘是否出现白边、黑边、色溢或不透明底色。
- 场景在 2.5D 俯视相机下是否保留可走区域、层级和主体可读性。
- 物品与 UI 在 64–128 px 缩略显示下是否仍可辨认。
- 特效边界是否溢出单帧，循环是否跳变，混合模式是否符合亮度预期。
- 六类素材是否可识别为同一 Q 版水墨武侠体系。

## 人工结论

- [ ] 尺寸通过
- [ ] Alpha 通过
- [ ] 五动作节奏通过
- [ ] 锚点/基线通过
- [ ] 整体风格通过
- [ ] 记录返工原因到 `pilot-rework-log.md`
"""

    @staticmethod
    def _rework_log() -> str:
        return """# Pilot 返工原因记录

每次人工验收追加一行；不要覆盖历史记录。

| 日期 | 资产类别 | 资产/动作 | 问题维度 | 可见证据 | 修复方式 | 是否需要模型重做 | 复验结果 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 待填写 | 待填写 | 待填写 | 尺寸/Alpha/锚点/节奏/风格 | 待填写 | 待填写 | 是/否 | 待填写 |
"""


def main() -> None:
    parser = argparse.ArgumentParser(description="运行只读武侠素材离线试点")
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--preset-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    manifest = OfflinePilotRunner.load_manifest(args.manifest)
    OfflinePilotRunner(preset_dir=args.preset_dir).run(manifest, output_root=args.output)


if __name__ == "__main__":
    main()
