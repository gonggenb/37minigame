from __future__ import annotations

import base64
import json
from typing import Any, Protocol, TypeVar

from pydantic import BaseModel

from app.schemas.core import GenerationPlan, QualityReport

from .audit import ProviderAuditWriter
from .errors import (
    ProviderContentRefusalError,
    ProviderError,
    ProviderResponseFormatError,
    translate_openai_error,
)
from .models import (
    PlanningRequest,
    ProviderOperation,
    ProviderTrace,
    ProviderUsage,
    ReviewRequest,
)

ParsedModel = TypeVar("ParsedModel", bound=BaseModel)


class ResponsesResource(Protocol):
    async def parse(self, **kwargs: Any) -> Any: ...


class ResponsesClient(Protocol):
    responses: ResponsesResource


class OpenAIReviewProvider:
    def __init__(
        self,
        client: ResponsesClient,
        *,
        model: str,
        audit: ProviderAuditWriter | None = None,
    ) -> None:
        self.client = client
        self.model = model
        self.audit = audit

    async def plan(self, request: PlanningRequest) -> GenerationPlan:
        payload = {
            "model": self.model,
            "input": [
                {
                    "role": "system",
                    "content": (
                        "你是 2D 游戏美术生产规划器。严格依据项目、任务和风格信息，"
                        "返回可执行的结构化生成计划。"
                    ),
                },
                {
                    "role": "user",
                    "content": json.dumps(
                        request.model_dump(mode="json", exclude={"trace"}),
                        ensure_ascii=False,
                    ),
                },
            ],
            "text_format": GenerationPlan,
        }
        response = await self._parse(payload)
        result = self._require_parsed(response, GenerationPlan)
        self._record(request.trace, payload, response, operation="plan")
        return result

    async def review(self, request: ReviewRequest) -> QualityReport:
        review_context = request.model_dump(
            mode="json",
            exclude={"candidate_png", "comparison_png", "trace"},
        )
        content: list[dict[str, Any]] = [
            {
                "type": "input_text",
                "text": json.dumps(review_context, ensure_ascii=False),
            },
            {
                "type": "input_text",
                "text": (
                    "第一张图是候选原图。请只引用画面中可见的证据；"
                    "硬约束由本地程序最终裁决。"
                ),
            },
            {
                "type": "input_image",
                "image_url": self._image_url(request.candidate_png),
                "detail": "high",
            },
        ]
        if request.comparison_png is not None:
            content.extend(
                [
                    {
                        "type": "input_text",
                        "text": (
                            "第二张图是候选与项目参考图对比板。"
                            "分别判断身份、配色、线条和构图，并为失败项给出可见证据。"
                        ),
                    },
                    {
                        "type": "input_image",
                        "image_url": self._image_url(request.comparison_png),
                        "detail": "high",
                    },
                ]
            )
        payload = {
            "model": self.model,
            "input": [
                {
                    "role": "system",
                    "content": (
                        "你是 2D 游戏美术质量评审器。硬约束与风格评分必须分离，"
                        "只依据提供的任务、计划、候选图和参考对比板返回固定 Schema 报告。"
                        "身份、配色、线条和构图的失败项必须给出可见证据与局部修复建议；"
                        "无法定位原因时不要建议重新生成。"
                    ),
                },
                {
                    "role": "user",
                    "content": content,
                },
            ],
            "text_format": QualityReport,
        }
        response = await self._parse(payload)
        result = self._require_parsed(response, QualityReport)
        self._record(request.trace, payload, response, operation="review")
        return result

    @staticmethod
    def _image_url(content: bytes) -> str:
        return "data:image/png;base64," + base64.b64encode(content).decode("ascii")

    async def _parse(self, payload: dict[str, Any]) -> Any:
        try:
            return await self.client.responses.parse(**payload)
        except ProviderError:
            raise
        except Exception as error:
            raise translate_openai_error(error) from error

    @staticmethod
    def _require_parsed(response: Any, model_type: type[ParsedModel]) -> ParsedModel:
        parsed = getattr(response, "output_parsed", None)
        if parsed is not None:
            return model_type.model_validate(parsed)
        for output in getattr(response, "output", []):
            for content in getattr(output, "content", []):
                if getattr(content, "type", None) == "refusal":
                    raise ProviderContentRefusalError(getattr(content, "refusal", "model refused"))
        raise ProviderResponseFormatError()

    def _record(
        self,
        trace: ProviderTrace | None,
        payload: dict[str, Any],
        response: Any,
        *,
        operation: ProviderOperation,
    ) -> None:
        if trace is None or self.audit is None:
            return
        self.audit.write_request(trace, payload)
        response_payload = response.model_dump(mode="json")
        self.audit.write_response(trace, response_payload)
        usage = getattr(response, "usage", None)
        raw_usage = usage.model_dump(mode="json") if usage is not None else {}
        self.audit.write_usage(
            trace,
            ProviderUsage(model=self.model, operation=operation, raw=raw_usage),
        )
