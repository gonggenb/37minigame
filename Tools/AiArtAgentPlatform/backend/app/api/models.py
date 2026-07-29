from typing import cast

from fastapi import APIRouter, HTTPException, Request, status

from app.providers.errors import ProviderMissingApiKeyError
from app.providers.registry import OpenAIProviderRegistry
from app.schemas.providers import (
    ModelAvailabilityRequest,
    ModelAvailabilityResponse,
    ModelStatusResponse,
)

router = APIRouter(prefix="/models", tags=["models"])


def get_registry(request: Request) -> OpenAIProviderRegistry:
    return cast(OpenAIProviderRegistry, request.app.state.provider_registry)


@router.get("/status", response_model=ModelStatusResponse)
def model_status(request: Request) -> ModelStatusResponse:
    return get_registry(request).status()


@router.post("/availability", response_model=ModelAvailabilityResponse)
async def model_availability(
    request_data: ModelAvailabilityRequest, request: Request
) -> ModelAvailabilityResponse:
    try:
        return await get_registry(request).check_availability(
            include_image=request_data.include_image
        )
    except ProviderMissingApiKeyError as error:
        raise HTTPException(
            status_code=status.HTTP_409_CONFLICT,
            detail="OpenAI API key is not configured",
        ) from error
