import base64
from io import BytesIO

import pytest
from app.image_processing.editor import CandidateImageEditor
from app.schemas.editor import CropRect
from PIL import Image, ImageDraw


def _candidate_png() -> bytes:
    image = Image.new("RGBA", (80, 64), (242, 236, 218, 255))
    ImageDraw.Draw(image).rectangle((20, 12, 59, 55), fill=(65, 110, 82, 255))
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def _painted_mask_png() -> bytes:
    image = Image.new("RGBA", (80, 64), (0, 0, 0, 0))
    ImageDraw.Draw(image).rectangle((24, 16, 40, 36), fill=(220, 30, 30, 180))
    stream = BytesIO()
    image.save(stream, format="PNG")
    return stream.getvalue()


def test_candidate_editor_crops_pixels_and_preserves_rgba() -> None:
    cropped = CandidateImageEditor.crop(
        _candidate_png(),
        CropRect(x=10, y=8, width=50, height=48),
    )

    with Image.open(BytesIO(cropped)) as image:
        assert image.mode == "RGBA"
        assert image.size == (50, 48)


def test_candidate_editor_rejects_crop_outside_source() -> None:
    with pytest.raises(ValueError, match="crop rectangle"):
        CandidateImageEditor.crop(
            _candidate_png(),
            CropRect(x=70, y=50, width=20, height=20),
        )


def test_candidate_editor_normalizes_painted_mask_for_image_edit() -> None:
    normalized = CandidateImageEditor.normalize_mask(
        _painted_mask_png(),
        expected_size=(80, 64),
    )

    with Image.open(BytesIO(normalized)) as image:
        assert image.mode == "RGBA"
        assert image.getpixel((30, 20))[3] == 0
        assert image.getpixel((5, 5))[3] == 255


def test_candidate_editor_decodes_bounded_base64() -> None:
    encoded = base64.b64encode(_painted_mask_png()).decode("ascii")
    assert CandidateImageEditor.decode_base64_png(encoded).startswith(b"\x89PNG")

    with pytest.raises(ValueError, match="base64"):
        CandidateImageEditor.decode_base64_png("not-base64")
