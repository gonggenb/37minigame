from __future__ import annotations

from typing import Protocol

from app.schemas.core import GenerationPlan, QualityReport

from .models import (
    EditRequest,
    GeneratedImage,
    GenerateRequest,
    PlanningRequest,
    ProviderCapabilities,
    ReviewRequest,
)


class ImageProvider(Protocol):
    async def generate(self, request: GenerateRequest) -> list[GeneratedImage]: ...

    async def edit(self, request: EditRequest) -> list[GeneratedImage]: ...

    def capabilities(self) -> ProviderCapabilities: ...


class ReviewProvider(Protocol):
    async def plan(self, request: PlanningRequest) -> GenerationPlan: ...

    async def review(self, request: ReviewRequest) -> QualityReport: ...
