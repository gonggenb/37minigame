from __future__ import annotations

from app.schemas.core import QualityReport, ReviewDimension, ReviewFinding
from app.schemas.production import RepairPlan


class RepairPlanner:
    MAX_RETRIES = 2
    DIMENSION_LABELS: dict[ReviewDimension, str] = {
        "hard_constraint": "硬约束",
        "identity": "身份特征",
        "palette": "项目配色",
        "line_style": "线条与水墨质感",
        "composition": "构图与俯视语义",
        "animation": "动画一致性",
    }

    @classmethod
    def plan(
        cls,
        report: QualityReport,
        *,
        minimum_style_score: int,
        retry_index: int,
    ) -> RepairPlan:
        if report.hard_constraints.passed is False:
            messages = [
                check.message or check.name
                for check in report.hard_constraints.checks
                if not check.passed
            ]
            return RepairPlan(
                action="manual",
                reason="硬约束失败应先由确定性处理或人工检查解决："
                + "；".join(messages),
                target_dimensions=["hard_constraint"],
                retry_allowed=False,
                stop_reason="manual-review-required",
            )

        if report.style_review.score >= minimum_style_score:
            return RepairPlan(
                action="none",
                reason="候选已达到项目风格阈值。",
                retry_allowed=False,
                stop_reason="passed",
            )

        if retry_index >= cls.MAX_RETRIES:
            return RepairPlan(
                action="none",
                reason="已达到两次自动定向修复上限。",
                retry_allowed=False,
                stop_reason="retry-limit-reached",
            )

        findings = [
            finding
            for finding in report.style_review.findings
            if finding.actionable and finding.severity in {"warning", "error"}
        ]
        if not findings and report.style_review.issues:
            findings = cls._legacy_findings(report)
        if not findings:
            return RepairPlan(
                action="none",
                reason="评分低于阈值，但评审没有提供可定位的失败证据。",
                retry_allowed=False,
                stop_reason="no-actionable-failure",
            )

        dimensions = cls._unique_dimensions(findings)
        bullets = [cls._finding_prompt(finding) for finding in findings]
        instruction = report.style_review.repair_instruction.strip()
        if instruction:
            bullets.append(f"- 评审补充：{instruction}")
        prompt = (
            "仅修复以下明确失败维度，保持主体身份、整体轮廓、2.5D 俯视角、"
            "尺寸占比和未提及区域不变；不要重新设计整张图：\n"
            + "\n".join(bullets)
        )
        if len(prompt) > 3900:
            prompt = prompt[:3897] + "…"
        return RepairPlan(
            action="edit",
            reason="存在带可见证据的风格失败维度，可进行一次局部定向编辑。",
            target_dimensions=dimensions,
            prompt=prompt,
            retry_allowed=True,
        )

    @classmethod
    def _legacy_findings(cls, report: QualityReport) -> list[ReviewFinding]:
        scores: list[tuple[ReviewDimension, int]] = [
            ("identity", report.style_review.identity_score),
            ("palette", report.style_review.palette_score),
            ("line_style", report.style_review.line_style_score),
            ("composition", report.style_review.composition_score),
        ]
        dimension = min(scores, key=lambda item: item[1])[0]
        return [
            ReviewFinding(
                dimension=dimension,
                severity="warning",
                summary=issue,
                evidence=issue,
                repair_hint=report.style_review.repair_instruction,
            )
            for issue in report.style_review.issues
            if issue.strip()
        ]

    @classmethod
    def _finding_prompt(cls, finding: ReviewFinding) -> str:
        label = cls.DIMENSION_LABELS[finding.dimension]
        hint = f"；修复方式：{finding.repair_hint}" if finding.repair_hint else ""
        return f"- [{label}] {finding.summary}；证据：{finding.evidence}{hint}"

    @staticmethod
    def _unique_dimensions(findings: list[ReviewFinding]) -> list[ReviewDimension]:
        result: list[ReviewDimension] = []
        for finding in findings:
            if finding.dimension not in result:
                result.append(finding.dimension)
        return result
