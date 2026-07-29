from __future__ import annotations

import hashlib
from dataclasses import dataclass
from io import BytesIO

from PIL import Image

from app.image_processing.alpha import clean_alpha, remove_connected_background
from app.schemas.core import ConstraintProfile
from app.schemas.image_tools import BackgroundRemovalConfig
from app.schemas.sequence import SequenceDriftReport, SequenceFrameRecord, SequenceTask

from .grid import slice_grid
from .metrics import analyze_sequence
from .normalize import normalize_frames_shared_scale
from .output import build_sprite_sheet, encode_gif, encode_png, encode_webp


@dataclass(frozen=True, slots=True)
class ProcessedSequence:
    frame_pngs: tuple[bytes, ...]
    sprite_sheet_png: bytes
    gif_preview: bytes
    webp_preview: bytes
    frame_records: tuple[SequenceFrameRecord, ...]
    drift_report: SequenceDriftReport
    scale: float
    content_sha256: str


def _decode_rgba(content: bytes) -> Image.Image:
    if not content:
        raise ValueError("image content must not be empty")
    with Image.open(BytesIO(content)) as image:
        return image.convert("RGBA")


class SequenceProcessor:
    @staticmethod
    def process(
        *,
        strip_png: bytes,
        task: SequenceTask,
        profile: ConstraintProfile,
        base_frame_png: bytes | None = None,
    ) -> ProcessedSequence:
        if profile.category is not task.category:
            raise ValueError("constraint profile category must match the sequence task")
        frames = slice_grid(
            strip_png,
            rows=task.rows,
            columns=task.columns,
            frame_count=task.frame_count,
            expected_frame_width=task.generation_frame_width,
            expected_frame_height=task.generation_frame_height,
        )
        if task.lock_first_frame:
            if base_frame_png is None:
                raise ValueError("locking the first frame requires base frame content")
            frames[0] = _decode_rgba(base_frame_png)

        if profile.require_transparency:
            background = BackgroundRemovalConfig(mode="corner_flood")
            frames = [
                clean_alpha(
                    remove_connected_background(frame, background),
                    low=background.alpha_low_threshold,
                    high=background.alpha_high_threshold,
                )
                for frame in frames
            ]

        normalized, scale = normalize_frames_shared_scale(
            frames,
            frame_width=task.frame_width,
            frame_height=task.frame_height,
            occupancy_ratio=profile.occupancy_ratio,
            padding_ratio=profile.padding_ratio,
            baseline=task.baseline,
            pivot_x=task.pivot_x,
            pivot_y=task.pivot_y,
            resize_algorithm=profile.resize_algorithm,
        )
        frame_pngs = tuple(encode_png(frame) for frame in normalized)
        sprite_sheet_png = encode_png(
            build_sprite_sheet(
                normalized,
                rows=task.rows,
                columns=task.columns,
                frame_width=task.frame_width,
                frame_height=task.frame_height,
            )
        )
        gif_preview = encode_gif(normalized, fps=task.preview_fps, loop=task.loop)
        webp_preview = encode_webp(normalized, fps=task.preview_fps, loop=task.loop)
        records, drift_report = analyze_sequence(
            normalized,
            task=task,
            profile=profile,
        )
        digest = hashlib.sha256()
        for content in (*frame_pngs, sprite_sheet_png, gif_preview, webp_preview):
            digest.update(content)
        return ProcessedSequence(
            frame_pngs=frame_pngs,
            sprite_sheet_png=sprite_sheet_png,
            gif_preview=gif_preview,
            webp_preview=webp_preview,
            frame_records=tuple(records),
            drift_report=drift_report,
            scale=scale,
            content_sha256=digest.hexdigest(),
        )
