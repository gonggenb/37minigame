import { useState } from "react";

import {
  type PromptPreviewInput,
  usePromptPreviewMutation,
  useReferencesQuery,
  useStyleGuideQuery,
  useUpdateStyleGuideMutation,
} from "../api/stylePack";
import type { AssetCategory } from "../types/core";
import { ReferenceLibrary } from "./ReferenceLibrary";
import { ReferenceSourceBrowser } from "./ReferenceSourceBrowser";
import { StyleGuideEditor } from "./StyleGuideEditor";

const CATEGORY_OPTIONS: Array<{ value: AssetCategory; label: string }> = [
  { value: "character", label: "角色" },
  { value: "scene", label: "场景" },
  { value: "item", label: "物品" },
  { value: "animation", label: "角色动画" },
  { value: "effect", label: "特效" },
  { value: "ui", label: "UI" },
];

export interface StylePackCardProps {
  projectId?: string;
}

export function StylePackCard({ projectId }: StylePackCardProps) {
  const guide = useStyleGuideQuery(projectId);
  const updateGuide = useUpdateStyleGuideMutation(projectId);
  const references = useReferencesQuery(projectId);
  const previewMutation = usePromptPreviewMutation(projectId);
  const [category, setCategory] = useState<AssetCategory>("character");
  const [brief, setBrief] = useState("");
  const [promptText, setPromptText] = useState("");
  const [lastCompiledPrompt, setLastCompiledPrompt] = useState("");

  if (!projectId) {
    return (
      <section className="paper-card paper-card--style-pack">
        <p className="paper-card__label">风格包</p>
        <h2>武侠美术上下文</h2>
        <p className="empty-state">创建项目后即可管理武侠风格包。</p>
      </section>
    );
  }

  const submitPreview = () => {
    if (!guide.data || !brief.trim()) {
      return;
    }
    const input: PromptPreviewInput = {
      task: {
        asset_id: "preview-asset",
        category,
        name: `${CATEGORY_OPTIONS.find((item) => item.value === category)?.label ?? category}预览资产`,
        brief: brief.trim(),
        usage: category === "scene" ? "battle-background" : "gameplay",
        style_pack: guide.data.style_id,
        reference_ids: [],
        constraint_profile: `${category}-default`,
        constraint_overrides: {},
        candidate_count: 4,
        output_mode: "single-png",
      },
      identity: null,
      viewpoint: category === "ui" ? "ui-flat" : "topdown-45",
      composition: "主体轮廓清楚，保留安全留白和底部中心锚点",
      lighting: "柔和左上主光",
      materials: ["宣纸肌理", "哑光手绘质感"],
      output_spec: {
        width: 1024,
        height: 1024,
        format: "png",
        transparent_required: category !== "scene",
      },
      additional_negative_constraints: [],
      prompt_override:
        promptText.trim() && promptText !== lastCompiledPrompt
          ? promptText.trim()
          : null,
    };
    previewMutation.mutate(input, {
      onSuccess: (result) => {
        setPromptText(result.prompt);
        setLastCompiledPrompt(result.prompt);
      },
    });
  };

  return (
    <section className="paper-card paper-card--style-pack">
      <p className="paper-card__label">风格包与提示词</p>
      <h2>{guide.data?.display_name ?? "正在读取武侠风格包…"}</h2>

      {guide.isError ? <p className="model-test-error">风格圣经读取失败。</p> : null}
      {guide.data ? (
        <div className="style-pack-summary">
          <div>
            <strong>只读来源</strong>
            <span title={guide.data.reference_source.path}>
              {guide.data.reference_source.path}
            </span>
          </div>
          <div>
            <strong>参考索引</strong>
            <span>已索引 {references.data?.length ?? 0} 张参考图</span>
          </div>
          <div>
            <strong>固定视角</strong>
            <span>
              {guide.data.camera.pitch_semantic_min}°–
              {guide.data.camera.pitch_semantic_max}° 俯视语义
            </span>
          </div>
        </div>
      ) : null}

      {guide.data ? (
        <StyleGuideEditor
          guide={guide.data}
          pending={updateGuide.isPending}
          errorMessage={
            updateGuide.isError ? "风格圣经保存失败。" : undefined
          }
          onSave={(nextGuide) => updateGuide.mutate(nextGuide)}
        />
      ) : null}

      <ReferenceSourceBrowser projectId={projectId} />
      <ReferenceLibrary projectId={projectId} />

      <div className="style-pack-panel style-pack-panel--prompt">
          <h3>提示词编译预览</h3>
          <label>
            资产类别
            <select
              value={category}
              onChange={(event) => setCategory(event.target.value as AssetCategory)}
            >
              {CATEGORY_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            资产需求
            <textarea
              value={brief}
              onChange={(event) => setBrief(event.target.value)}
              placeholder="例如：俯视角青衣少侠游戏内基准帧"
              rows={3}
            />
          </label>
          <button
            type="button"
            disabled={!guide.data || !brief.trim() || previewMutation.isPending}
            onClick={submitPreview}
          >
            {previewMutation.isPending ? "正在编译…" : "编译提示词预览"}
          </button>
          <label>
            提示词预览与人工修改
            <textarea
              value={promptText}
              onChange={(event) => setPromptText(event.target.value)}
              placeholder="编译后可在这里人工调整，再次点击编译以确认覆盖。"
              rows={12}
            />
          </label>
          {previewMutation.data?.selected_reference_ids.length ? (
            <p className="reference-selection">
              已选择参考：{previewMutation.data.selected_reference_ids.join("、")}
            </p>
          ) : null}
          {previewMutation.isError ? (
            <p className="model-test-error">提示词编译失败，请检查任务字段。</p>
          ) : null}
      </div>
    </section>
  );
}
