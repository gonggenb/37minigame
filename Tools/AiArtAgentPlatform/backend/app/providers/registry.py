from __future__ import annotations

from collections.abc import Callable
from typing import cast

from openai import AsyncOpenAI

from app.config.settings import Settings
from app.schemas.providers import (
    ModelAvailabilityResponse,
    ModelCheckResult,
    ModelStatusResponse,
)
from app.workspace.project_workspace import ProjectWorkspace
from app.workspace.run_workspace import RunWorkspace

from .audit import ProviderAuditWriter
from .errors import ProviderError, ProviderMissingApiKeyError, translate_openai_error
from .models import GenerateRequest
from .openai_client import create_openai_client
from .openai_image import ImagesClient, OpenAIImageProvider
from .openai_review import OpenAIReviewProvider, ResponsesClient

ClientFactory = Callable[[Settings], AsyncOpenAI]


class OpenAIProviderRegistry:
    def __init__(
        self,
        settings: Settings,
        workspace: ProjectWorkspace,
        *,
        client_factory: ClientFactory = create_openai_client,
    ) -> None:
        self.settings = settings
        self.workspace = workspace
        self.client_factory = client_factory
        self._client: AsyncOpenAI | None = None

    @property
    def configured(self) -> bool:
        return self.settings.openai_api_key is not None

    def status(self) -> ModelStatusResponse:
        return ModelStatusResponse(
            api_key_configured=self.configured,
            review_model=self.settings.openai_review_model,
            image_model=self.settings.openai_image_model,
            timeout_seconds=self.settings.openai_timeout_seconds,
            max_retries=self.settings.openai_max_retries,
        )

    def review_provider(self) -> OpenAIReviewProvider:
        return OpenAIReviewProvider(
            cast(ResponsesClient, self._get_client()),
            model=self.settings.openai_review_model,
            audit=self._audit_writer(),
        )

    def image_provider(self) -> OpenAIImageProvider:
        return OpenAIImageProvider(
            cast(ImagesClient, self._get_client()),
            model=self.settings.openai_image_model,
            audit=self._audit_writer(),
        )

    async def check_availability(self, *, include_image: bool) -> ModelAvailabilityResponse:
        if not self.configured:
            raise ProviderMissingApiKeyError()
        client = self._get_client()
        checks = [await self._check_review_model(client)]
        if include_image:
            checks.append(await self._check_image_model())
        return ModelAvailabilityResponse(checks=checks)

    async def _check_review_model(self, client: AsyncOpenAI) -> ModelCheckResult:
        try:
            await client.responses.create(
                model=self.settings.openai_review_model,
                input="Reply with OK.",
                max_output_tokens=8,
            )
            return ModelCheckResult(
                capability="structured_review",
                model=self.settings.openai_review_model,
                available=True,
            )
        except Exception as error:
            provider_error = translate_openai_error(error)
            return self._failed_check(
                "structured_review", self.settings.openai_review_model, provider_error
            )

    async def _check_image_model(self) -> ModelCheckResult:
        try:
            await self.image_provider().generate(
                GenerateRequest(
                    prompt=(
                        "A simple centered gray ink circle on a plain warm white background, "
                        "no text, minimal test image"
                    ),
                    width=1024,
                    height=1024,
                    candidate_count=1,
                    quality="low",
                )
            )
            return ModelCheckResult(
                capability="image_generation",
                model=self.settings.openai_image_model,
                available=True,
                detail="image test generated one low-quality candidate",
            )
        except Exception as error:
            provider_error = (
                error if isinstance(error, ProviderError) else translate_openai_error(error)
            )
            return self._failed_check(
                "image_generation", self.settings.openai_image_model, provider_error
            )

    @staticmethod
    def _failed_check(capability: str, model: str, error: ProviderError) -> ModelCheckResult:
        return ModelCheckResult(
            capability=capability,
            model=model,
            available=False,
            error_code=error.code,
            retryable=error.retryable,
            detail=str(error),
        )

    def _get_client(self) -> AsyncOpenAI:
        if self._client is None:
            self._client = self.client_factory(self.settings)
        return self._client

    def _audit_writer(self) -> ProviderAuditWriter:
        secret_values = (
            [self.settings.openai_api_key.get_secret_value()]
            if self.settings.openai_api_key is not None
            else []
        )
        return ProviderAuditWriter(RunWorkspace(self.workspace), secret_values=secret_values)
