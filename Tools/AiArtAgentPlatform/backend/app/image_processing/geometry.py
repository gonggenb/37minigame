from __future__ import annotations

from PIL import Image, ImageOps

from app.schemas.core import ConstraintProfile

from .alpha import alpha_bounds


def resample_filter(algorithm: str) -> Image.Resampling:
    if algorithm == "nearest":
        return Image.Resampling.NEAREST
    if algorithm == "lanczos":
        return Image.Resampling.LANCZOS
    raise ValueError(f"unsupported resize algorithm: {algorithm}")


def crop_alpha(image: Image.Image) -> tuple[Image.Image, tuple[int, int, int, int]]:
    bounds = alpha_bounds(image)
    return image.crop(bounds), bounds


def normalize_geometry(
    image: Image.Image,
    profile: ConstraintProfile,
) -> tuple[Image.Image, float]:
    output_size = (profile.output_width, profile.output_height)
    resample = resample_filter(profile.resize_algorithm)
    if profile.crop_mode == "none":
        scale = max(
            profile.output_width / image.width,
            profile.output_height / image.height,
        )
        return ImageOps.fit(image, output_size, method=resample), scale
    if profile.crop_mode == "fixed":
        scale = min(
            profile.output_width / image.width,
            profile.output_height / image.height,
        )
        return image.resize(output_size, resample=resample), scale

    cropped, _ = crop_alpha(image)
    safe_width = profile.output_width * (1 - (2 * profile.padding_ratio))
    safe_height = profile.output_height * (1 - (2 * profile.padding_ratio))
    target_width = max(
        1,
        round(min(safe_width, profile.output_width * profile.occupancy_ratio)),
    )
    target_height = max(
        1,
        round(min(safe_height, profile.output_height * profile.occupancy_ratio)),
    )
    scale = min(target_width / cropped.width, target_height / cropped.height)
    resized_width = max(1, round(cropped.width * scale))
    resized_height = max(1, round(cropped.height * scale))
    resized = cropped.resize((resized_width, resized_height), resample=resample)
    canvas = Image.new("RGBA", output_size, (0, 0, 0, 0))
    safe_left = profile.output_width * profile.padding_ratio
    safe_top = profile.output_height * profile.padding_ratio
    target_anchor_x = safe_left + (safe_width * profile.pivot_x)
    target_anchor_y = safe_top + (safe_height * profile.pivot_y)
    source_anchor_x = resized_width * profile.pivot_x
    source_anchor_y = resized_height * profile.pivot_y
    paste_x = round(target_anchor_x - source_anchor_x)
    paste_y = round(target_anchor_y - source_anchor_y)
    paste_x = min(max(paste_x, 0), profile.output_width - resized_width)
    paste_y = min(max(paste_y, 0), profile.output_height - resized_height)
    canvas.alpha_composite(resized, (paste_x, paste_y))
    return canvas, scale
