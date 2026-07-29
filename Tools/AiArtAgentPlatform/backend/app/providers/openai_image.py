from __future__ import annotations

import base64
from typing import Any, Never, Protocol

from .audit import ProviderAuditWriter
from .errors import (
    ProviderError,
    ProviderErrorCode,
    ProviderResponseFormatError,
    ProviderUnsupportedCapabilityError,
    translate_openai_error,
)
from .models import (
    EditRequest,
    GeneratedImage,
    GenerateRequest,
    ProviderCapabilities,
    ProviderOperation,
    ProviderTrace,
    ProviderUsage,
)


class ImagesResource(Protocol):
    async def generate(self, **kwargs: Any) -> Any: ...

    async def edit(self, **kwargs: Any) -> Any: ...


class ImagesClient(Protocol):
    images: ImagesResource


class OpenAIImageProvider:
    GPT_IMAGE_2_MIN_PIXELS = 655_360
    GPT_IMAGE_2_MAX_PIXELS = 8_294_400
    GPT_IMAGE_2_MAX_EDGE = 3_840
    GPT_IMAGE_2_MAX_ASPECT_RATIO = 3

    def __init__(
        self,
        client: ImagesClient,
        *,
        model: str,
        audit: ProviderAuditWriter | None = None,
    ) -> None:
        self.client = client
        self.model = model
        self.audit = audit

    def capabilities(self) -> ProviderCapabilities:
        return ProviderCapabilities(
            model=self.model,
            supports_native_transparency=self.model != "gpt-image-2",
        )

    async def generate(self, request: GenerateRequest) -> list[GeneratedImage]:
        self._validate_background(request.background)
        self._validate_canvas(request.width, request.height)
        payload = {
            "model": self.model,
            "prompt": request.prompt,
            "n": request.candidate_count,
            "size": f"{request.width}x{request.height}",
            "quality": request.quality,
            "background": request.background,
            "output_format": "png",
        }
        try:
            response = await self.client.images.generate(**payload)
        except ProviderError:
            raise
        except Exception as error:
            raise translate_openai_error(error) from error
        images = self._decode_images(response)
        self._record(request.trace, payload, response, images, operation="generate")
        return images

    async def edit(self, request: EditRequest) -> list[GeneratedImage]:
        self._validate_background(request.background)
        self._validate_canvas(request.width, request.height)
        payload: dict[str, Any] = {
            "model": self.model,
            "prompt": request.prompt,
            "image": [(item.filename, item.content, item.mime_type) for item in request.images],
            "n": request.candidate_count,
            "size": f"{request.width}x{request.height}",
            "quality": request.quality,
            "background": request.background,
            "output_format": "png",
        }
        if request.mask is not None:
            payload["mask"] = (
                request.mask.filename,
                request.mask.content,
                request.mask.mime_type,
            )
        try:
            response = await self.client.images.edit(**payload)
        except ProviderError:
            raise
        except Exception as error:
            raise translate_openai_error(error) from error
        images = self._decode_images(response)
        audit_payload = {
            **payload,
            "image": [
                {"filename": item.filename, "bytes": len(item.content)} for item in request.images
            ],
        }
        if request.mask is not None:
            audit_payload["mask"] = {
                "filename": request.mask.filename,
                "bytes": len(request.mask.content),
            }
        self._record(request.trace, audit_payload, response, images, operation="edit")
        return images

    def _validate_background(self, background: str) -> None:
        if self.model == "gpt-image-2" and background == "transparent":
            raise ProviderUnsupportedCapabilityError(
                "gpt-image-2 does not support native transparent backgrounds; use postprocess"
            )

    def _validate_canvas(self, width: int, height: int) -> None:
        if self.model != "gpt-image-2":
            return
        if width > self.GPT_IMAGE_2_MAX_EDGE or height > self.GPT_IMAGE_2_MAX_EDGE:
            self._raise_bad_canvas(width, height, "edges must be at most 3840 pixels")
        if width % 16 or height % 16:
            self._raise_bad_canvas(
                width,
                height,
                "width and height must be multiples of 16",
            )
        if max(width, height) / min(width, height) > self.GPT_IMAGE_2_MAX_ASPECT_RATIO:
            self._raise_bad_canvas(width, height, "aspect ratio must not exceed 3:1")
        pixels = width * height
        if pixels < self.GPT_IMAGE_2_MIN_PIXELS:
            self._raise_bad_canvas(
                width,
                height,
                "total pixels must be at least 655360",
            )
        if pixels > self.GPT_IMAGE_2_MAX_PIXELS:
            self._raise_bad_canvas(
                width,
                height,
                "total pixels must be at most 8294400",
            )

    @staticmethod
    def _raise_bad_canvas(width: int, height: int, reason: str) -> Never:
        raise ProviderError(
            ProviderErrorCode.BAD_REQUEST,
            f"invalid gpt-image-2 canvas {width}x{height}: {reason}",
            retryable=False,
            status_code=400,
        )

    @staticmethod
    def _decode_images(response: Any) -> list[GeneratedImage]:
        images: list[GeneratedImage] = []
        for index, item in enumerate(getattr(response, "data", [])):
            encoded = getattr(item, "b64_json", None)
            if not encoded:
                raise ProviderResponseFormatError("image response did not contain b64_json")
            try:
                content = base64.b64decode(encoded, validate=True)
            except ValueError as error:
                raise ProviderResponseFormatError(
                    "image response contained invalid base64"
                ) from error
            images.append(
                GeneratedImage(
                    index=index,
                    content=content,
                    revised_prompt=getattr(item, "revised_prompt", None),
                )
            )
        if not images:
            raise ProviderResponseFormatError("image response did not contain candidates")
        return images

    def _record(
        self,
        trace: ProviderTrace | None,
        payload: dict[str, Any],
        response: Any,
        images: list[GeneratedImage],
        *,
        operation: ProviderOperation,
    ) -> None:
        if trace is None or self.audit is None:
            return
        self.audit.write_request(trace, payload)
        self.audit.write_response(trace, response.model_dump(mode="json"))
        for image in images:
            self.audit.write_image(trace, image.index, image.content)
        usage = getattr(response, "usage", None)
        raw_usage = usage.model_dump(mode="json") if usage is not None else {}
        self.audit.write_usage(
            trace,
            ProviderUsage(model=self.model, operation=operation, raw=raw_usage),
        )
