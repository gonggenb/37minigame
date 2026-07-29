from __future__ import annotations

import base64
import binascii
from io import BytesIO

from PIL import Image

from app.schemas.editor import CropRect


class CandidateImageEditor:
    @staticmethod
    def crop(content: bytes, crop: CropRect) -> bytes:
        with Image.open(BytesIO(content)) as source:
            image = source.convert("RGBA")
        right = crop.x + crop.width
        bottom = crop.y + crop.height
        if right > image.width or bottom > image.height:
            raise ValueError("crop rectangle must stay inside the source image")
        result = image.crop((crop.x, crop.y, right, bottom))
        return CandidateImageEditor._encode(result)

    @staticmethod
    def normalize_mask(content: bytes, *, expected_size: tuple[int, int]) -> bytes:
        with Image.open(BytesIO(content)) as source:
            painted = source.convert("RGBA")
        if painted.size != expected_size:
            raise ValueError("mask dimensions must match the candidate image")
        source_alpha = painted.getchannel("A")
        edit_alpha = source_alpha.point(lambda value: 0 if value > 0 else 255)
        normalized = Image.new("RGBA", painted.size, (255, 255, 255, 255))
        normalized.putalpha(edit_alpha)
        return CandidateImageEditor._encode(normalized)

    @staticmethod
    def decode_base64_png(encoded: str) -> bytes:
        try:
            content = base64.b64decode(encoded, validate=True)
        except (ValueError, binascii.Error) as error:
            raise ValueError("mask must be valid base64") from error
        try:
            with Image.open(BytesIO(content)) as image:
                if image.format != "PNG":
                    raise ValueError("mask must be a PNG image")
                image.verify()
        except OSError as error:
            raise ValueError("mask must be a valid PNG image") from error
        return content

    @staticmethod
    def size(content: bytes) -> tuple[int, int]:
        with Image.open(BytesIO(content)) as image:
            return image.size

    @staticmethod
    def _encode(image: Image.Image) -> bytes:
        stream = BytesIO()
        image.save(stream, format="PNG", optimize=False, compress_level=6)
        return stream.getvalue()
