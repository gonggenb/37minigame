from __future__ import annotations

from io import BytesIO

from PIL import Image, ImageDraw, ImageOps

from app.providers.models import ImageInput


class ComparisonBoardBuilder:
    CARD_SIZE = (320, 320)
    IMAGE_SIZE = (280, 248)
    GAP = 20
    MAX_COLUMNS = 3
    BACKGROUND = (239, 232, 211, 255)
    CARD_BACKGROUND = (250, 247, 236, 255)
    BORDER = (71, 70, 60, 255)
    LABEL = (45, 50, 43, 255)

    @classmethod
    def build(cls, candidate_png: bytes, references: list[ImageInput]) -> bytes:
        items = [("CANDIDATE", candidate_png), *cls._reference_items(references)]
        columns = min(cls.MAX_COLUMNS, len(items))
        rows = (len(items) + columns - 1) // columns
        width = cls.GAP + columns * (cls.CARD_SIZE[0] + cls.GAP)
        height = cls.GAP + rows * (cls.CARD_SIZE[1] + cls.GAP)
        board = Image.new("RGBA", (width, height), cls.BACKGROUND)
        draw = ImageDraw.Draw(board)

        for index, (label, content) in enumerate(items):
            column = index % columns
            row = index // columns
            x = cls.GAP + column * (cls.CARD_SIZE[0] + cls.GAP)
            y = cls.GAP + row * (cls.CARD_SIZE[1] + cls.GAP)
            cls._draw_card(board, draw, x, y, label, content)

        stream = BytesIO()
        board.save(stream, format="PNG", compress_level=9, optimize=False)
        return stream.getvalue()

    @staticmethod
    def _reference_items(references: list[ImageInput]) -> list[tuple[str, bytes]]:
        return [
            (f"REFERENCE {index + 1}", reference.content)
            for index, reference in enumerate(references[:4])
        ]

    @classmethod
    def _draw_card(
        cls,
        board: Image.Image,
        draw: ImageDraw.ImageDraw,
        x: int,
        y: int,
        label: str,
        content: bytes,
    ) -> None:
        x2 = x + cls.CARD_SIZE[0]
        y2 = y + cls.CARD_SIZE[1]
        draw.rounded_rectangle(
            (x, y, x2, y2),
            radius=12,
            fill=cls.CARD_BACKGROUND,
            outline=cls.BORDER,
            width=2,
        )
        with Image.open(BytesIO(content)) as source:
            image = ImageOps.exif_transpose(source).convert("RGBA")
        fitted = ImageOps.contain(image, cls.IMAGE_SIZE, method=Image.Resampling.LANCZOS)
        image_x = x + (cls.CARD_SIZE[0] - fitted.width) // 2
        image_y = y + 18 + (cls.IMAGE_SIZE[1] - fitted.height) // 2
        board.alpha_composite(fitted, (image_x, image_y))
        draw.line((x + 16, y + 278, x2 - 16, y + 278), fill=cls.BORDER, width=1)
        draw.text((x + 18, y + 290), label, fill=cls.LABEL)
