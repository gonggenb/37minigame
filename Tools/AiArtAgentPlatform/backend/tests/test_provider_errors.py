import httpx
from app.providers.errors import (
    ProviderErrorCode,
    translate_openai_error,
)
from openai import APITimeoutError, BadRequestError, RateLimitError


def test_timeout_and_rate_limit_are_retryable() -> None:
    request = httpx.Request("POST", "https://api.openai.com/v1/responses")
    timeout = translate_openai_error(APITimeoutError(request=request))
    rate_limit = translate_openai_error(
        RateLimitError(
            "rate limited",
            response=httpx.Response(429, request=request),
            body={"error": {"message": "rate limited"}},
        )
    )

    assert timeout.code == ProviderErrorCode.TIMEOUT
    assert timeout.retryable is True
    assert rate_limit.code == ProviderErrorCode.RATE_LIMIT
    assert rate_limit.retryable is True


def test_bad_request_is_not_retryable() -> None:
    request = httpx.Request("POST", "https://api.openai.com/v1/images/generations")
    error = BadRequestError(
        "invalid image request",
        response=httpx.Response(400, request=request),
        body={"error": {"message": "invalid image request"}},
    )

    translated = translate_openai_error(error)

    assert translated.code == ProviderErrorCode.BAD_REQUEST
    assert translated.retryable is False
