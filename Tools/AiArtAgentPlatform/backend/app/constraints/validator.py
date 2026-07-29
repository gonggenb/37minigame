from __future__ import annotations

import hashlib
import math
from io import BytesIO
from pathlib import Path
from typing import cast

from PIL import Image, UnidentifiedImageError

from app.image_processing.alpha import EmptyAlphaError, alpha_bounds
from app.schemas.core import ConstraintProfile, HardConstraintCheck, HardConstraintReport


class ConstraintValidator:
    @staticmethod
    def validate(
        content: bytes,
        profile: ConstraintProfile,
        *,
        asset_id: str,
        variant: str,
        filename: str,
    ) -> HardConstraintReport:
        image: Image.Image | None = None
        image_format = ""
        decode_error = ""
        try:
            opened = Image.open(BytesIO(content))
            opened.load()
            image_format = opened.format or ""
            image = opened.copy()
            opened.close()
        except (OSError, UnidentifiedImageError) as error:
            decode_error = str(error)

        checks = [
            HardConstraintCheck(
                name="decode",
                passed=image is not None,
                message=decode_error,
            ),
            HardConstraintCheck(
                name="png_format",
                passed=image is not None and image_format == "PNG",
                message=f"detected format: {image_format or 'unknown'}",
            ),
            HardConstraintCheck(
                name="dimensions",
                passed=(
                    image is not None
                    and image.size == (profile.output_width, profile.output_height)
                ),
                message=(
                    f"expected {profile.output_width}x{profile.output_height}; "
                    f"actual {image.width}x{image.height}"
                    if image is not None
                    else "image unavailable"
                ),
            ),
            ConstraintValidator._rgba_check(image, profile),
            ConstraintValidator._alpha_check(image, profile),
            ConstraintValidator._subject_bounds_check(image, profile),
            ConstraintValidator._filename_check(
                profile,
                asset_id=asset_id,
                variant=variant,
                filename=filename,
            ),
            HardConstraintCheck(
                name="file_size",
                passed=0 < len(content) <= profile.max_file_bytes,
                message=f"{len(content)} / {profile.max_file_bytes} bytes",
            ),
            ConstraintValidator._grid_check(profile),
            HardConstraintCheck(
                name="content_hash",
                passed=bool(content),
                message=hashlib.sha256(content).hexdigest() if content else "empty content",
            ),
        ]
        return HardConstraintReport(
            passed=all(check.passed for check in checks),
            checks=checks,
        )

    @staticmethod
    def expected_filename(
        profile: ConstraintProfile,
        *,
        asset_id: str,
        variant: str,
    ) -> str:
        filename = profile.filename_template.format(
            asset_id=asset_id,
            variant=variant,
            category=profile.category.value,
        )
        if Path(filename).name != filename or not filename.casefold().endswith(".png"):
            raise ValueError("filename template must produce a plain PNG filename")
        return filename

    @staticmethod
    def _rgba_check(
        image: Image.Image | None,
        profile: ConstraintProfile,
    ) -> HardConstraintCheck:
        passed = not profile.require_rgba or (image is not None and image.mode == "RGBA")
        return HardConstraintCheck(
            name="rgba",
            passed=passed,
            message=f"mode: {image.mode if image is not None else 'unknown'}",
        )

    @staticmethod
    def _alpha_check(
        image: Image.Image | None,
        profile: ConstraintProfile,
    ) -> HardConstraintCheck:
        if not profile.require_transparency:
            return HardConstraintCheck(
                name="alpha_channel",
                passed=True,
                message="transparency not required",
            )
        if image is None or "A" not in image.getbands():
            return HardConstraintCheck(
                name="alpha_channel",
                passed=False,
                message="RGBA alpha channel is required",
            )
        minimum, maximum = cast(
            tuple[int, int], image.getchannel("A").getextrema()
        )
        return HardConstraintCheck(
            name="alpha_channel",
            passed=minimum < 255 and maximum > 0,
            message=f"alpha range: {minimum}-{maximum}",
        )

    @staticmethod
    def _subject_bounds_check(
        image: Image.Image | None,
        profile: ConstraintProfile,
    ) -> HardConstraintCheck:
        if image is None:
            return HardConstraintCheck(
                name="subject_bounds",
                passed=False,
                message="image unavailable",
            )
        if not profile.require_transparency:
            return HardConstraintCheck(
                name="subject_bounds",
                passed=True,
                message="opaque canvas uses the full frame",
            )
        try:
            left, top, right, bottom = alpha_bounds(image)
        except EmptyAlphaError as error:
            return HardConstraintCheck(
                name="subject_bounds",
                passed=False,
                message=str(error),
            )
        border = math.floor(
            min(profile.output_width, profile.output_height) * profile.padding_ratio
        )
        passed = (
            left >= border
            and top >= border
            and right <= profile.output_width - border
            and bottom <= profile.output_height - border
        )
        return HardConstraintCheck(
            name="subject_bounds",
            passed=passed,
            message=f"bbox={left},{top},{right},{bottom}; border={border}",
        )

    @staticmethod
    def _filename_check(
        profile: ConstraintProfile,
        *,
        asset_id: str,
        variant: str,
        filename: str,
    ) -> HardConstraintCheck:
        try:
            expected = ConstraintValidator.expected_filename(
                profile,
                asset_id=asset_id,
                variant=variant,
            )
        except (KeyError, ValueError) as error:
            return HardConstraintCheck(
                name="filename",
                passed=False,
                message=str(error),
            )
        return HardConstraintCheck(
            name="filename",
            passed=filename == expected,
            message=f"expected {expected}; actual {filename}",
        )

    @staticmethod
    def _grid_check(profile: ConstraintProfile) -> HardConstraintCheck:
        if not profile.output_sprite_sheet:
            return HardConstraintCheck(
                name="sprite_sheet_grid",
                passed=True,
                message="sprite sheet output not required",
            )
        values = (
            profile.frame_count,
            profile.rows,
            profile.columns,
            profile.frame_width,
            profile.frame_height,
        )
        if any(value is None for value in values):
            return HardConstraintCheck(
                name="sprite_sheet_grid",
                passed=False,
                message="sprite sheet grid fields are incomplete",
            )
        assert profile.frame_count is not None
        assert profile.rows is not None
        assert profile.columns is not None
        assert profile.frame_width is not None
        assert profile.frame_height is not None
        passed = (
            profile.rows * profile.columns == profile.frame_count
            and profile.columns * profile.frame_width == profile.output_width
            and profile.rows * profile.frame_height == profile.output_height
        )
        return HardConstraintCheck(
            name="sprite_sheet_grid",
            passed=passed,
            message=(
                f"frames={profile.frame_count}; grid={profile.rows}x{profile.columns}; "
                f"frame={profile.frame_width}x{profile.frame_height}"
            ),
        )
