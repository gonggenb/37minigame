from __future__ import annotations

import hashlib
from dataclasses import dataclass
from io import BytesIO

from app.schemas.core import ConstraintProfile
from app.schemas.image_tools import BackgroundRemovalConfig, ProcessedImageMetadata

from .alpha import alpha_bounds, clean_alpha, decode_rgba, remove_connected_background
from .geometry import normalize_geometry


@dataclass(frozen=True, slots=True)
class ProcessedImage:
    content: bytes
    metadata: ProcessedImageMetadata


class ImageProcessor:
    @staticmethod
    def process(
        content: bytes,
        profile: ConstraintProfile,
        background: BackgroundRemovalConfig | None = None,
    ) -> ProcessedImage:
        resolved_background = background or BackgroundRemovalConfig()
        decoded = decode_rgba(content)
        background_processed = remove_connected_background(decoded, resolved_background)
        alpha_processed = clean_alpha(
            background_processed,
            low=resolved_background.alpha_low_threshold,
            high=resolved_background.alpha_high_threshold,
        )
        source_bounds = alpha_bounds(alpha_processed)
        normalized, scale = normalize_geometry(alpha_processed, profile)
        final_bounds = alpha_bounds(normalized)
        stream = BytesIO()
        normalized.save(
            stream,
            format="PNG",
            optimize=False,
            compress_level=6,
        )
        png_content = stream.getvalue()
        metadata = ProcessedImageMetadata(
            width=normalized.width,
            height=normalized.height,
            source_alpha_bounds=source_bounds,
            alpha_bounds=final_bounds,
            scale=scale,
            sha256=hashlib.sha256(png_content).hexdigest(),
            file_bytes=len(png_content),
        )
        return ProcessedImage(content=png_content, metadata=metadata)
