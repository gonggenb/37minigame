from __future__ import annotations

from collections.abc import Sequence

from PIL import Image

from app.image_processing.geometry import resample_filter


def _anchor_points(
    *,
    baseline: str,
    frame_width: int,
    frame_height: int,
    subject_width: int,
    subject_height: int,
    padding_ratio: float,
    pivot_x: float,
    pivot_y: float,
) -> tuple[tuple[float, float], tuple[float, float]]:
    if baseline == "bottom_center":
        return (
            (frame_width / 2, frame_height * (1 - padding_ratio)),
            (subject_width / 2, float(subject_height)),
        )
    if baseline == "center":
        return (
            (frame_width / 2, frame_height / 2),
            (subject_width / 2, subject_height / 2),
        )
    if baseline == "custom":
        return (
            (frame_width * pivot_x, frame_height * pivot_y),
            (subject_width * pivot_x, subject_height * pivot_y),
        )
    raise ValueError(f"unsupported sequence baseline: {baseline}")


def normalize_frames_shared_scale(
    frames: Sequence[Image.Image],
    *,
    frame_width: int,
    frame_height: int,
    occupancy_ratio: float,
    padding_ratio: float,
    baseline: str,
    pivot_x: float,
    pivot_y: float,
    resize_algorithm: str,
) -> tuple[list[Image.Image], float]:
    """Normalize every frame using one scale and a stable sequence anchor."""

    if not frames:
        raise ValueError("at least one sequence frame is required")
    if frame_width <= 0 or frame_height <= 0:
        raise ValueError("frame dimensions must be positive")
    if not 0 < occupancy_ratio <= 1:
        raise ValueError("occupancy ratio must be greater than zero and at most one")
    if not 0 <= padding_ratio < 0.5:
        raise ValueError("padding ratio must be at least zero and less than one half")
    if not 0 <= pivot_x <= 1 or not 0 <= pivot_y <= 1:
        raise ValueError("pivot coordinates must be between zero and one")

    rgba_frames = [frame.convert("RGBA") for frame in frames]
    bounds = [frame.getchannel("A").getbbox() for frame in rgba_frames]
    non_empty_bounds = [bound for bound in bounds if bound is not None]
    if not non_empty_bounds:
        return (
            [
                Image.new("RGBA", (frame_width, frame_height), (0, 0, 0, 0))
                for _ in rgba_frames
            ],
            1.0,
        )

    max_subject_width = max(right - left for left, _, right, _ in non_empty_bounds)
    max_subject_height = max(bottom - top for _, top, _, bottom in non_empty_bounds)
    safe_width = frame_width * (1 - 2 * padding_ratio)
    safe_height = frame_height * (1 - 2 * padding_ratio)
    target_width = max(1.0, min(safe_width, frame_width * occupancy_ratio))
    target_height = max(1.0, min(safe_height, frame_height * occupancy_ratio))
    scale = min(
        target_width / max_subject_width,
        target_height / max_subject_height,
    )
    resample = resample_filter(resize_algorithm)

    normalized: list[Image.Image] = []
    for frame, bound in zip(rgba_frames, bounds, strict=True):
        canvas = Image.new("RGBA", (frame_width, frame_height), (0, 0, 0, 0))
        if bound is None:
            normalized.append(canvas)
            continue

        cropped = frame.crop(bound)
        resized_width = max(1, round(cropped.width * scale))
        resized_height = max(1, round(cropped.height * scale))
        resized = cropped.resize((resized_width, resized_height), resample=resample)
        target_anchor, source_anchor = _anchor_points(
            baseline=baseline,
            frame_width=frame_width,
            frame_height=frame_height,
            subject_width=resized_width,
            subject_height=resized_height,
            padding_ratio=padding_ratio,
            pivot_x=pivot_x,
            pivot_y=pivot_y,
        )
        paste_x = round(target_anchor[0] - source_anchor[0])
        paste_y = round(target_anchor[1] - source_anchor[1])
        paste_x = min(max(paste_x, 0), frame_width - resized_width)
        paste_y = min(max(paste_y, 0), frame_height - resized_height)
        canvas.alpha_composite(resized, (paste_x, paste_y))
        normalized.append(canvas)

    return normalized, scale
