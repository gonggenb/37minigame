from __future__ import annotations

import math

import numpy as np
from PIL import Image

from app.schemas.core import AssetCategory, ConstraintProfile
from app.schemas.sequence import (
    SequenceDriftReport,
    SequenceFrameRecord,
    SequenceTask,
)

BASELINE_DRIFT_LIMIT_PX = 2.0
EFFECT_BRIGHTNESS_JUMP_LIMIT = 96.0
EFFECT_LOOP_DIFFERENCE_LIMIT = 64.0


def _frame_record(image: Image.Image, index: int) -> SequenceFrameRecord:
    rgba = np.asarray(image.convert("RGBA"), dtype=np.uint8)
    alpha = rgba[:, :, 3]
    ys, xs = np.nonzero(alpha)
    if xs.size == 0:
        return SequenceFrameRecord(
            index=index,
            relative_path=f"frames/frame-{index:03d}.png",
            alpha_bounds=(0, 0, 0, 0),
            center_x=0,
            center_y=0,
            subject_width=0,
            subject_height=0,
            baseline_y=0,
            area_ratio=0,
            mean_rgb=(0, 0, 0),
            brightness=0,
        )

    left = int(xs.min())
    top = int(ys.min())
    right = int(xs.max()) + 1
    bottom = int(ys.max()) + 1
    opaque = alpha > 0
    mean_values = rgba[:, :, :3][opaque].mean(axis=0)
    mean_rgb = (
        int(round(float(mean_values[0]))),
        int(round(float(mean_values[1]))),
        int(round(float(mean_values[2]))),
    )
    red, green, blue = mean_rgb
    brightness = (0.2126 * red) + (0.7152 * green) + (0.0722 * blue)
    return SequenceFrameRecord(
        index=index,
        relative_path=f"frames/frame-{index:03d}.png",
        alpha_bounds=(left, top, right, bottom),
        center_x=(left + right) / 2,
        center_y=(top + bottom) / 2,
        subject_width=right - left,
        subject_height=bottom - top,
        baseline_y=bottom,
        area_ratio=float(np.count_nonzero(opaque) / opaque.size),
        mean_rgb=mean_rgb,
        brightness=brightness,
    )


def _relative_change(current: float, reference: float) -> float:
    if reference == 0:
        return 0 if current == 0 else 1
    return abs(current - reference) / reference


def _frame_difference(first: Image.Image, last: Image.Image) -> float:
    first_rgba = np.asarray(first.convert("RGBA"), dtype=np.float32)
    last_rgba = np.asarray(last.convert("RGBA"), dtype=np.float32)
    return float(np.mean(np.abs(first_rgba - last_rgba)))


def analyze_sequence(
    frames: list[Image.Image],
    *,
    task: SequenceTask,
    profile: ConstraintProfile,
) -> tuple[list[SequenceFrameRecord], SequenceDriftReport]:
    if len(frames) != task.frame_count:
        raise ValueError("frame count must match the sequence task")
    if profile.category is not task.category:
        raise ValueError("constraint profile category must match the sequence task")
    if any(frame.size != (task.frame_width, task.frame_height) for frame in frames):
        raise ValueError("metrics require normalized frame dimensions")

    records = [_frame_record(frame, index) for index, frame in enumerate(frames)]
    reference = records[0]
    center_drift_limit = (
        task.max_center_drift_px
        if task.max_center_drift_px is not None
        else profile.max_center_drift_px
    )
    size_drift_limit = (
        task.max_size_drift_ratio
        if task.max_size_drift_ratio is not None
        else profile.max_size_drift_ratio
    )
    baseline_drift_limit = (
        task.max_baseline_drift_px
        if task.max_baseline_drift_px is not None
        else BASELINE_DRIFT_LIMIT_PX
    )
    center_drifts: list[float] = []
    size_drifts: list[float] = []
    baseline_drifts: list[float] = []
    area_drifts: list[float] = []
    color_drifts: list[float] = []
    overflow_frames: list[int] = []
    failed_frames: set[int] = set()

    for record in records:
        center_drift = math.hypot(
            record.center_x - reference.center_x,
            record.center_y - reference.center_y,
        )
        size_drift = max(
            _relative_change(record.subject_width, reference.subject_width),
            _relative_change(record.subject_height, reference.subject_height),
        )
        baseline_drift = abs(record.baseline_y - reference.baseline_y)
        area_drift = _relative_change(record.area_ratio, reference.area_ratio)
        color_drift = math.sqrt(
            sum(
                (current - base) ** 2
                for current, base in zip(record.mean_rgb, reference.mean_rgb, strict=True)
            )
        )
        center_drifts.append(center_drift)
        size_drifts.append(size_drift)
        baseline_drifts.append(baseline_drift)
        area_drifts.append(area_drift)
        color_drifts.append(color_drift)

        left, top, right, bottom = record.alpha_bounds
        if record.subject_width > 0 and (
            left == 0
            or top == 0
            or right == task.frame_width
            or bottom == task.frame_height
        ):
            overflow_frames.append(record.index)
        if center_drift_limit is not None and center_drift > center_drift_limit:
            failed_frames.add(record.index)
        if size_drift_limit is not None and size_drift > size_drift_limit:
            failed_frames.add(record.index)
        if (
            task.category is AssetCategory.ANIMATION
            and baseline_drift > baseline_drift_limit
        ):
            failed_frames.add(record.index)

    brightness_jumps = [
        abs(current.brightness - previous.brightness)
        for previous, current in zip(records, records[1:], strict=False)
    ]
    max_brightness_jump = max(brightness_jumps, default=0.0)
    first_last_difference = _frame_difference(frames[0], frames[-1])
    issues: list[str] = []
    if failed_frames:
        issues.append("序列中心、尺寸或脚底基线漂移超过约束")
    if task.category is AssetCategory.EFFECT and overflow_frames:
        failed_frames.update(overflow_frames)
        issues.append("特效存在触碰画布边界的帧")
    if (
        task.category is AssetCategory.EFFECT
        and max_brightness_jump > EFFECT_BRIGHTNESS_JUMP_LIMIT
    ):
        issues.append("特效相邻帧亮度突变过大")
    if (
        task.category is AssetCategory.EFFECT
        and task.loop
        and first_last_difference > EFFECT_LOOP_DIFFERENCE_LIMIT
    ):
        issues.append("循环特效首尾连续性不足")

    passed = not failed_frames and not issues
    return records, SequenceDriftReport(
        passed=passed,
        max_center_drift_px=max(center_drifts, default=0.0),
        max_size_drift_ratio=max(size_drifts, default=0.0),
        max_baseline_drift_px=max(baseline_drifts, default=0.0),
        max_area_drift_ratio=max(area_drifts, default=0.0),
        max_color_drift=max(color_drifts, default=0.0),
        max_brightness_jump=max_brightness_jump,
        first_last_difference=first_last_difference,
        overflow_frames=overflow_frames,
        failed_frames=sorted(failed_frames),
        issues=issues,
        blend_mode_hint=task.blend_mode_hint,
    )
