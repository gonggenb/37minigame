from openai import AsyncOpenAI

from app.config.settings import Settings

from .errors import ProviderMissingApiKeyError


def create_openai_client(settings: Settings) -> AsyncOpenAI:
    if settings.openai_api_key is None:
        raise ProviderMissingApiKeyError()
    return AsyncOpenAI(
        api_key=settings.openai_api_key.get_secret_value(),
        timeout=settings.openai_timeout_seconds,
        max_retries=settings.openai_max_retries,
    )
