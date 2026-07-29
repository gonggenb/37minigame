from __future__ import annotations

from io import BytesIO

import cv2
import numpy as np
from PIL import Image, UnidentifiedImageError

from app.schemas.image_tools import BackgroundRemovalConfig


class ImageDecodeError(ValueError):
    """输入无法被 Pillow 解码。"""


class EmptyAlphaError(ValueError):
    """图片没有任何可见像素。"""


def decode_rgba(content: bytes) -> Image.Image:
    try:
        with Image.open(BytesIO(content)) as image:
            image.load()
            return image.convert("RGBA")
    except (OSError, UnidentifiedImageError) as error:
        raise ImageDecodeError("image content cannot be decoded") from error


def remove_connected_background(
    image: Image.Image,
    config: BackgroundRemovalConfig,
) -> Image.Image:
    rgba = image.convert("RGBA")
    if config.mode == "preserve":
        return rgba.copy()
    pixels = np.asarray(rgba, dtype=np.uint8).copy()
    rgb = pixels[:, :, :3].astype(np.int32)
    height, width = rgb.shape[:2]
    corners = np.stack(
        (
            rgb[0, 0],
            rgb[0, width - 1],
            rgb[height - 1, 0],
            rgb[height - 1, width - 1],
        )
    )
    differences = rgb[:, :, None, :] - corners[None, None, :, :]
    distances = np.sqrt(np.sum(differences * differences, axis=3))
    background_candidates = np.min(distances, axis=2) <= config.color_tolerance
    _, labels = cv2.connectedComponents(
        background_candidates.astype(np.uint8),
        connectivity=4,
    )
    border_labels = np.unique(
        np.concatenate(
            (
                labels[0, :],
                labels[-1, :],
                labels[:, 0],
                labels[:, -1],
            )
        )
    )
    removable_labels = border_labels[border_labels != 0]
    pixels[:, :, 3][np.isin(labels, removable_labels)] = 0
    return Image.fromarray(pixels, mode="RGBA")


def clean_alpha(image: Image.Image, *, low: int, high: int) -> Image.Image:
    if low < 0 or high > 255 or low >= high:
        raise ValueError("alpha thresholds must satisfy 0 <= low < high <= 255")
    pixels = np.asarray(image.convert("RGBA"), dtype=np.uint8).copy()
    alpha = pixels[:, :, 3]
    alpha[alpha <= low] = 0
    alpha[alpha >= high] = 255
    return Image.fromarray(pixels, mode="RGBA")


def alpha_bounds(image: Image.Image) -> tuple[int, int, int, int]:
    alpha = np.asarray(image.convert("RGBA"), dtype=np.uint8)[:, :, 3]
    visible_y, visible_x = np.nonzero(alpha > 0)
    if visible_x.size == 0 or visible_y.size == 0:
        raise EmptyAlphaError("image contains no visible pixels")
    return (
        int(visible_x.min()),
        int(visible_y.min()),
        int(visible_x.max()) + 1,
        int(visible_y.max()) + 1,
    )
