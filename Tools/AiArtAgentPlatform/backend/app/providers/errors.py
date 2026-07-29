from __future__ import annotations

from enum import StrEnum

from openai import (
    APIConnectionError,
    APIStatusError,
    APITimeoutError,
    AuthenticationError,
    BadRequestError,
    InternalServerError,
    PermissionDeniedError,
    RateLimitError,
)


class ProviderErrorCode(StrEnum):
    TIMEOUT = "timeout"
    CONNECTION = "connection"
    RATE_LIMIT = "rate_limit"
    AUTHENTICATION = "authentication"
    PERMISSION = "permission"
    BAD_REQUEST = "bad_request"
    SERVER = "server"
    CONTENT_REFUSAL = "content_refusal"
    RESPONSE_FORMAT = "response_format"
    UNSUPPORTED_CAPABILITY = "unsupported_capability"
    MISSING_API_KEY = "missing_api_key"
    UNKNOWN = "unknown"


class ProviderError(RuntimeError):
    def __init__(
        self,
        code: ProviderErrorCode,
        message: str,
        *,
        retryable: bool,
        status_code: int | None = None,
    ) -> None:
        super().__init__(message)
        self.code = code
        self.retryable = retryable
        self.status_code = status_code


class ProviderContentRefusalError(ProviderError):
    def __init__(self, message: str = "model refused the request") -> None:
        super().__init__(ProviderErrorCode.CONTENT_REFUSAL, message, retryable=False)


class ProviderResponseFormatError(ProviderError):
    def __init__(self, message: str = "model returned an invalid structured response") -> None:
        super().__init__(ProviderErrorCode.RESPONSE_FORMAT, message, retryable=False)


class ProviderUnsupportedCapabilityError(ProviderError):
    def __init__(self, message: str) -> None:
        super().__init__(ProviderErrorCode.UNSUPPORTED_CAPABILITY, message, retryable=False)


class ProviderMissingApiKeyError(ProviderError):
    def __init__(self) -> None:
        super().__init__(
            ProviderErrorCode.MISSING_API_KEY,
            "OpenAI API key is not configured",
            retryable=False,
        )


def translate_openai_error(error: Exception) -> ProviderError:
    if isinstance(error, ProviderError):
        return error
    if isinstance(error, APITimeoutError):
        return ProviderError(ProviderErrorCode.TIMEOUT, "OpenAI request timed out", retryable=True)
    if isinstance(error, RateLimitError):
        return ProviderError(
            ProviderErrorCode.RATE_LIMIT,
            "OpenAI rate limit reached",
            retryable=True,
            status_code=429,
        )
    if isinstance(error, AuthenticationError):
        return ProviderError(
            ProviderErrorCode.AUTHENTICATION,
            "OpenAI authentication failed",
            retryable=False,
            status_code=401,
        )
    if isinstance(error, PermissionDeniedError):
        return ProviderError(
            ProviderErrorCode.PERMISSION,
            "OpenAI permission denied",
            retryable=False,
            status_code=403,
        )
    if isinstance(error, BadRequestError):
        return ProviderError(
            ProviderErrorCode.BAD_REQUEST,
            "OpenAI rejected the request parameters",
            retryable=False,
            status_code=400,
        )
    if isinstance(error, APIConnectionError):
        return ProviderError(
            ProviderErrorCode.CONNECTION,
            "Unable to connect to OpenAI",
            retryable=True,
        )
    if isinstance(error, InternalServerError):
        return ProviderError(
            ProviderErrorCode.SERVER,
            "OpenAI service returned a server error",
            retryable=True,
            status_code=error.status_code,
        )
    if isinstance(error, APIStatusError):
        retryable = error.status_code in {408, 409, 429} or error.status_code >= 500
        return ProviderError(
            ProviderErrorCode.SERVER if retryable else ProviderErrorCode.BAD_REQUEST,
            "OpenAI request failed",
            retryable=retryable,
            status_code=error.status_code,
        )
    return ProviderError(ProviderErrorCode.UNKNOWN, "OpenAI request failed", retryable=False)
