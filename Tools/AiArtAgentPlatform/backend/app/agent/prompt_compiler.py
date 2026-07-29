from __future__ import annotations

from app.schemas.style_pack import (
    CharacterIdentity,
    CompiledPrompt,
    PromptPreviewRequest,
    PromptSection,
    ReferenceAsset,
    StyleGuide,
)

from .reference_selector import ReferenceSelector


class PromptCompiler:
    @staticmethod
    def compile(
        guide: StyleGuide,
        request: PromptPreviewRequest,
        references: list[ReferenceAsset],
    ) -> CompiledPrompt:
        identity_id = request.identity.asset_id if request.identity is not None else None
        selected = ReferenceSelector.select(
            request.task,
            references,
            identity_id=identity_id,
            viewpoint=request.viewpoint or None,
        )
        negative_constraints = list(
            dict.fromkeys(
                [*guide.forbidden, *request.additional_negative_constraints]
            )
        )
        sections = [
            PromptSection(
                key="project_style",
                label="项目风格",
                content=PromptCompiler._project_style(guide),
            ),
            PromptSection(
                key="asset_task",
                label="资产任务",
                content=(
                    f"类别 {request.task.category.value}；名称 {request.task.name}；"
                    f"用途 {request.task.usage}；需求 {request.task.brief}。"
                ),
            ),
            PromptSection(
                key="identity",
                label="身份约束",
                content=PromptCompiler._identity(request.identity),
            ),
            PromptSection(
                key="references",
                label="参考图",
                content=PromptCompiler._references(selected),
            ),
            PromptSection(
                key="composition_camera",
                label="构图与视角",
                content=PromptCompiler._composition_camera(guide, request),
            ),
            PromptSection(
                key="lighting_materials",
                label="光照与材质",
                content=PromptCompiler._lighting_materials(guide, request),
            ),
            PromptSection(
                key="output_spec",
                label="输出规格",
                content=PromptCompiler._output_spec(request),
            ),
            PromptSection(
                key="forbidden",
                label="禁止项",
                content="、".join(negative_constraints) or "无额外禁止项。",
            ),
            PromptSection(
                key="postprocess",
                label="后处理目标",
                content=PromptCompiler._postprocess(request),
            ),
        ]
        compiled_text = "\n\n".join(
            f"## {section.label}\n{section.content}" for section in sections
        )
        prompt = request.prompt_override or compiled_text
        return CompiledPrompt(
            task=request.task,
            selected_reference_ids=[item.reference_id for item in selected],
            sections=sections,
            prompt=prompt,
            negative_constraints=negative_constraints,
        )

    @staticmethod
    def _project_style(guide: StyleGuide) -> str:
        base = "、".join(guide.palette.base)
        accents = "、".join(guide.palette.accents)
        return (
            f"{guide.display_name}；{guide.rendering.character_proportion} 比例；"
            f"{guide.rendering.character_outline} 轮廓；{guide.rendering.surface_finish} 质感；"
            f"基础色 {base}；强调色 {accents}。"
        )

    @staticmethod
    def _identity(identity: CharacterIdentity | None) -> str:
        if identity is None:
            return "未提供角色身份摘要；保持任务主体特征稳定。"
        fields = (
            ("轮廓", identity.silhouette),
            ("面部", identity.face),
            ("发型", identity.hair),
            ("服饰", identity.costume),
            ("配色", identity.palette),
            ("装备", identity.equipment),
            ("不可变特征", identity.immutable_traits),
        )
        details = "；".join(
            f"{label}：{'、'.join(values)}" for label, values in fields if values
        )
        return f"角色 {identity.display_name}。{details}。"

    @staticmethod
    def _references(references: list[ReferenceAsset]) -> str:
        if not references:
            return "未选择参考图；仅遵守项目风格圣经。"
        return "；".join(
            f"{item.reference_id}（{item.source_relative_path}）" for item in references
        )

    @staticmethod
    def _composition_camera(guide: StyleGuide, request: PromptPreviewRequest) -> str:
        composition = request.composition or "主体居中，轮廓清楚，保留安全留白"
        viewpoint = request.viewpoint or (
            f"{guide.camera.pitch_semantic_min}°–{guide.camera.pitch_semantic_max}°俯视语义"
        )
        return (
            f"{composition}；近似 {guide.camera.projection}；视角 {viewpoint}；"
            f"默认朝向 {guide.camera.default_facing}。"
        )

    @staticmethod
    def _lighting_materials(guide: StyleGuide, request: PromptPreviewRequest) -> str:
        lighting = request.lighting or "柔和漫射光"
        materials = "、".join(request.materials) or guide.rendering.surface_finish
        return (
            f"{lighting}；阴影方向 {guide.rendering.shadow_direction}；"
            f"材质 {materials}；避免高反射和复杂写实光照。"
        )

    @staticmethod
    def _output_spec(request: PromptPreviewRequest) -> str:
        transparency = (
            "最终需要透明背景"
            if request.output_spec.transparent_required
            else "不要求透明背景"
        )
        return (
            f"{request.output_spec.width}×{request.output_spec.height} PNG；"
            f"{transparency}；候选数不超过 {request.task.candidate_count}。"
        )

    @staticmethod
    def _postprocess(request: PromptPreviewRequest) -> str:
        if request.output_spec.transparent_required:
            return (
                "先在纯色或可分离背景上生成，再执行背景移除、Alpha 边缘去色、"
                "透明残留检查和 RGBA 硬约束验证。"
            )
        return "保留实体背景，执行尺寸、边界、命名和 PNG 硬约束验证。"
