import base64
from types import SimpleNamespace

import pytest
from app.providers.errors import (
    ProviderError,
    ProviderErrorCode,
    ProviderUnsupportedCapabilityError,
)
from app.providers.models import EditRequest, GenerateRequest, ImageInput
from app.providers.openai_image import OpenAIImageProvider


class FakeImages:
    def __init__(self) -> None:
        self.generate_calls: list[dict] = []
        self.edit_calls: list[dict] = []

    async def generate(self, **kwargs):
        self.generate_calls.append(kwargs)
        return self._response()

    async def edit(self, **kwargs):
        self.edit_calls.append(kwargs)
        return self._response()

    @staticmethod
    def _response():
        encoded = base64.b64encode(b"png-bytes").decode("ascii")
        return SimpleNamespace(
            data=[SimpleNamespace(b64_json=encoded, revised_prompt=None)],
            usage=SimpleNamespace(model_dump=lambda mode="json": {"total_tokens": 12}),
            model_dump=lambda mode="json": {"data": [{"b64_json": encoded}]},
        )


@pytest.mark.asyncio
async def test_generate_decodes_candidates_and_passes_model_options() -> None:
    images = FakeImages()
    provider = OpenAIImageProvider(SimpleNamespace(images=images), model="gpt-image-2")

    result = await provider.generate(
        GenerateRequest(
            prompt="Q 版水墨青锋剑，纯色背景",
            width=1024,
            height=1024,
            candidate_count=1,
            quality="low",
        )
    )

    assert result[0].content == b"png-bytes"
    assert images.generate_calls[0]["model"] == "gpt-image-2"
    assert images.generate_calls[0]["size"] == "1024x1024"
    assert images.generate_calls[0]["n"] == 1
    assert images.generate_calls[0]["output_format"] == "png"


@pytest.mark.asyncio
async def test_edit_passes_image_and_mask_without_input_fidelity() -> None:
    images = FakeImages()
    provider = OpenAIImageProvider(SimpleNamespace(images=images), model="gpt-image-2")

    result = await provider.edit(
        EditRequest(
            prompt="只修改剑穗颜色",
            images=[ImageInput(filename="sword.png", content=b"image")],
            mask=ImageInput(filename="mask.png", content=b"mask"),
            width=1024,
            height=1024,
        )
    )

    assert result[0].content == b"png-bytes"
    assert "mask" in images.edit_calls[0]
    assert "input_fidelity" not in images.edit_calls[0]


@pytest.mark.asyncio
async def test_gpt_image_2_rejects_native_transparency_request() -> None:
    provider = OpenAIImageProvider(SimpleNamespace(images=FakeImages()), model="gpt-image-2")

    with pytest.raises(ProviderUnsupportedCapabilityError):
        await provider.generate(
            GenerateRequest(
                prompt="透明背景武器",
                width=1024,
                height=1024,
                background="transparent",
            )
        )


@pytest.mark.asyncio
@pytest.mark.parametrize(
    ("width", "height", "expected_message"),
    [
        (1025, 1024, "multiples of 16"),
        (3072, 768, "aspect ratio"),
        (768, 768, "at least 655360"),
        (3840, 2304, "at most 8294400"),
    ],
)
async def test_gpt_image_2_rejects_invalid_generate_canvas_before_api_call(
    width: int,
    height: int,
    expected_message: str,
) -> None:
    images = FakeImages()
    provider = OpenAIImageProvider(SimpleNamespace(images=images), model="gpt-image-2")

    with pytest.raises(ProviderError) as raised:
        await provider.generate(
            GenerateRequest(prompt="武侠动画网格", width=width, height=height)
        )

    assert raised.value.code is ProviderErrorCode.BAD_REQUEST
    assert raised.value.retryable is False
    assert raised.value.status_code == 400
    assert expected_message in str(raised.value)
    assert images.generate_calls == []


@pytest.mark.asyncio
async def test_gpt_image_2_rejects_invalid_edit_canvas_before_api_call() -> None:
    images = FakeImages()
    provider = OpenAIImageProvider(SimpleNamespace(images=images), model="gpt-image-2")

    with pytest.raises(ProviderError):
        await provider.edit(
            EditRequest(
                prompt="完整动作网格",
                images=[ImageInput(filename="grid.png", content=b"grid")],
                width=2048,
                height=512,
            )
        )

    assert images.edit_calls == []
