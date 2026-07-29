from types import SimpleNamespace

import pytest
from app.providers.errors import ProviderContentRefusalError, ProviderResponseFormatError
from app.providers.models import PlanningRequest, ReviewRequest
from app.providers.openai_review import OpenAIReviewProvider
from app.schemas.core import AssetTask, GenerationPlan, ProjectConfig, QualityReport


def make_task() -> AssetTask:
    return AssetTask(
        asset_id="sword-001",
        category="item",
        name="青锋剑",
        brief="Q 版水墨武侠长剑",
        usage="world_sprite",
        style_pack="wuxia-ink-chibi-topdown-2_5d",
        constraint_profile="item",
        output_mode="rgba_png",
    )


def make_plan() -> GenerationPlan:
    return GenerationPlan.model_validate(
        {
            "asset_type": "item",
            "usage": "world_sprite",
            "selected_reference_ids": [],
            "composition": "主体居中",
            "camera": "45 度俯视",
            "lighting": "左上柔光",
            "identity_constraints": ["青色剑穗"],
            "prompt": "Q 版水墨武侠青锋剑",
            "negative_constraints": ["无文字"],
            "output_spec": {"width": 1024, "height": 1024},
            "postprocess_steps": ["remove_background"],
            "quality_checks": ["alpha_channel"],
            "repair_strategy": ["保持剑身身份特征后局部修复"],
        }
    )


def make_report() -> QualityReport:
    return QualityReport.model_validate(
        {
            "hard_constraints": {"passed": True, "checks": []},
            "style_review": {
                "score": 85,
                "identity_score": 88,
                "palette_score": 84,
                "line_style_score": 82,
                "composition_score": 86,
                "issues": [],
                "repair_instruction": "",
            },
            "animation_review": None,
            "export_allowed": True,
        }
    )


class FakeResponses:
    def __init__(self, parsed) -> None:
        self.parsed = parsed
        self.calls: list[dict] = []

    async def parse(self, **kwargs):
        self.calls.append(kwargs)
        return SimpleNamespace(
            id="resp-001",
            output_parsed=self.parsed,
            output=[],
            usage=SimpleNamespace(model_dump=lambda mode="json": {"total_tokens": 42}),
            model_dump=lambda mode="json": {"id": "resp-001", "output": []},
        )


@pytest.mark.asyncio
async def test_plan_uses_responses_structured_output() -> None:
    responses = FakeResponses(make_plan())
    provider = OpenAIReviewProvider(SimpleNamespace(responses=responses), model="gpt-5.6")
    request = PlanningRequest(
        project=ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"),
        task=make_task(),
        style_guide="Q 版水墨武侠，2.5D 俯视角",
    )

    result = await provider.plan(request)

    assert result == make_plan()
    assert responses.calls[0]["model"] == "gpt-5.6"
    assert responses.calls[0]["text_format"] is GenerationPlan


@pytest.mark.asyncio
async def test_review_uses_image_input_and_quality_schema() -> None:
    responses = FakeResponses(make_report())
    provider = OpenAIReviewProvider(SimpleNamespace(responses=responses), model="gpt-5.6")
    request = ReviewRequest(
        project=ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"),
        task=make_task(),
        plan=make_plan(),
        candidate_png=b"candidate-png",
        comparison_png=b"comparison-png",
    )

    result = await provider.review(request)

    assert result == make_report()
    assert responses.calls[0]["text_format"] is QualityReport
    image_inputs = responses.calls[0]["input"][1]["content"]
    assert [item["type"] for item in image_inputs].count("input_image") == 2
    assert "候选原图" in image_inputs[1]["text"]


@pytest.mark.asyncio
async def test_empty_structured_output_reports_format_error() -> None:
    responses = FakeResponses(None)
    provider = OpenAIReviewProvider(SimpleNamespace(responses=responses), model="gpt-5.6")

    with pytest.raises(ProviderResponseFormatError):
        await provider.plan(
            PlanningRequest(
                project=ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"),
                task=make_task(),
            )
        )


@pytest.mark.asyncio
async def test_refusal_is_reported_without_retrying() -> None:
    class RefusalResponses(FakeResponses):
        async def parse(self, **kwargs):
            self.calls.append(kwargs)
            return SimpleNamespace(
                output_parsed=None,
                output=[
                    SimpleNamespace(content=[SimpleNamespace(type="refusal", refusal="内容被拒绝")])
                ],
            )

    provider = OpenAIReviewProvider(
        SimpleNamespace(responses=RefusalResponses(None)), model="gpt-5.6"
    )

    with pytest.raises(ProviderContentRefusalError) as captured:
        await provider.plan(
            PlanningRequest(
                project=ProjectConfig(project_id="wuxia-demo", display_name="武侠美术"),
                task=make_task(),
            )
        )

    assert captured.value.retryable is False
