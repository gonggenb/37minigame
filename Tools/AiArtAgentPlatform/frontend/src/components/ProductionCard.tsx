import { useEffect, useState } from "react";

import {
  type CandidateTransformInput,
  type ProductionRun,
  type StaticAssetCategory,
  type StaticAssetRecord,
  productionCandidateComparisonUrl,
  productionCandidateImageUrl,
  useCreateStaticAssetMutation,
  useEditProductionMutation,
  useExportProductionMutation,
  useGenerateProductionMutation,
  usePlanStaticAssetMutation,
  useReviewAndRepairProductionMutation,
  useReviewProductionMutation,
  useSaveProductionMaskMutation,
  useSelectProductionMutation,
  useStaticAssetsQuery,
  useProductionRunsQuery,
  useTransformProductionMutation,
  useUpdateStaticAssetMutation,
} from "../api/production";
import { useReferencesQuery } from "../api/stylePack";
import { useProductionDraftStore } from "../stores/production";
import { useTaskNavigationStore } from "../stores/taskNavigation";
import { CandidateEditor } from "./CandidateEditor";
import { ReferencePicker } from "./ReferencePicker";

const CATEGORY_OPTIONS: Array<{ value: StaticAssetCategory; label: string }> = [
  { value: "item", label: "物品" },
  { value: "ui", label: "UI 图标与按钮" },
  { value: "character", label: "角色原画/基准帧" },
  { value: "scene", label: "2.5D 俯视场景" },
];

export interface ProductionCardProps {
  projectId?: string;
}

function stopReasonLabel(reason: string): string {
  const labels: Record<string, string> = {
    passed: "已达到项目阈值",
    "retry-limit-reached": "已达到自动修复上限",
    "no-actionable-failure": "没有可定位的失败原因",
    "manual-review-required": "需要人工检查",
    disabled: "本次未启用自动修复",
  };
  return labels[reason] ?? reason;
}

export function ProductionCard({ projectId }: ProductionCardProps) {
  const assets = useStaticAssetsQuery(projectId);
  const createAsset = useCreateStaticAssetMutation(projectId);
  const updateAsset = useUpdateStaticAssetMutation(projectId);
  const planAsset = usePlanStaticAssetMutation(projectId);
  const generate = useGenerateProductionMutation(projectId);
  const selectCandidate = useSelectProductionMutation(projectId);
  const editCandidate = useEditProductionMutation(projectId);
  const reviewCandidate = useReviewProductionMutation(projectId);
  const reviewAndRepair = useReviewAndRepairProductionMutation(projectId);
  const transformCandidate = useTransformProductionMutation(projectId);
  const saveMask = useSaveProductionMaskMutation(projectId);
  const exportRun = useExportProductionMutation(projectId);
  const draft = useProductionDraftStore();
  const navigationTarget = useTaskNavigationStore((state) => state.target);
  const clearNavigation = useTaskNavigationStore((state) => state.clear);
  const [asset, setAsset] = useState<StaticAssetRecord | null>(null);
  const [run, setRun] = useState<ProductionRun | null>(null);
  const [prompt, setPrompt] = useState("");
  const [editInstruction, setEditInstruction] = useState("");
  const [acceptStyleRisk, setAcceptStyleRisk] = useState(false);
  const [outputWidthOverride, setOutputWidthOverride] = useState("");
  const [outputHeightOverride, setOutputHeightOverride] = useState("");
  const [paddingOverride, setPaddingOverride] = useState("");
  const [exportVariant, setExportVariant] = useState("default");
  const draftReferences = useReferencesQuery(projectId, {
    category: draft.category,
    limit: 100,
  });
  const assetReferences = useReferencesQuery(asset ? projectId : undefined, {
    category: asset?.task.category,
    limit: 100,
  });
  const runs = useProductionRunsQuery(
    projectId,
    asset?.task.category,
    asset?.task.asset_id,
  );

  useEffect(() => {
    setAsset(null);
    setRun(null);
    setPrompt("");
    setEditInstruction("");
    setAcceptStyleRisk(false);
    setExportVariant("default");
    draft.reset();
    if (navigationTarget && navigationTarget.projectId !== projectId) {
      clearNavigation();
    }
  }, [clearNavigation, projectId, draft.reset]);

  useEffect(() => {
    if (!assets.data) return;
    if (
      navigationTarget?.workflow === "static" &&
      navigationTarget.projectId === projectId
    ) {
      const requested = assets.data.find(
        (record) =>
          record.task.category === navigationTarget.category &&
          record.task.asset_id === navigationTarget.assetId,
      );
      if (requested) {
        setAsset(requested);
        setRun(null);
        if (!navigationTarget.runId) {
          clearNavigation();
        }
        return;
      }
    }
    setAsset((current) => {
      if (current) {
        const refreshed = assets.data.find(
          (record) =>
            record.task.category === current.task.category &&
            record.task.asset_id === current.task.asset_id,
        );
        if (refreshed) return refreshed;
      }
      return assets.data[0] ?? null;
    });
  }, [assets.data, clearNavigation, navigationTarget, projectId]);

  useEffect(() => {
    if (!runs.data) return;
    if (
      navigationTarget?.workflow === "static" &&
      navigationTarget.projectId === projectId &&
      asset?.task.category === navigationTarget.category &&
      asset.task.asset_id === navigationTarget.assetId
    ) {
      const requested = navigationTarget.runId
        ? runs.data.find((item) => item.run_id === navigationTarget.runId)
        : null;
      setRun(requested ?? runs.data[0] ?? null);
      clearNavigation();
      return;
    }
    setRun((current) =>
      (current && runs.data.find((item) => item.run_id === current.run_id)) ??
      runs.data[0] ??
      null,
    );
  }, [asset, clearNavigation, navigationTarget, projectId, runs.data]);

  useEffect(() => {
    setPrompt(run?.prompt ?? "");
  }, [run?.run_id]);

  if (!projectId) {
    return (
      <section id="static-production" className="paper-card paper-card--production">
        <p className="paper-card__label">静态资产生产</p>
        <h2>生成、比较、编辑、评审与导出</h2>
        <p className="empty-state">创建项目后即可生产静态资产。</p>
      </section>
    );
  }

  const task = asset?.task;
  const selected = run?.candidates.find(
    (candidate) => candidate.candidate_id === run.selected_candidate_id,
  );

  const submitAsset = () => {
    if (!draft.assetId.trim() || !draft.name.trim() || !draft.brief.trim()) {
      return;
    }
    const constraintOverrides: Record<string, number> = {};
    const outputWidth = Number(outputWidthOverride);
    const outputHeight = Number(outputHeightOverride);
    const padding = Number(paddingOverride);
    if (outputWidth > 0 && outputHeight > 0) {
      constraintOverrides.output_width = outputWidth;
      constraintOverrides.output_height = outputHeight;
    }
    if (paddingOverride.trim() && padding >= 0 && padding < 0.5) {
      constraintOverrides.padding_ratio = padding;
    }
    createAsset.mutate(
      {
        asset_id: draft.assetId.trim(),
        category: draft.category,
        name: draft.name.trim(),
        brief: draft.brief.trim(),
        usage: draft.usage.trim() || "gameplay",
        style_pack: "wuxia-ink-chibi-topdown-2_5d",
        reference_ids: draft.referenceIds,
        constraint_profile: "wuxia-" + draft.category,
        constraint_overrides: constraintOverrides,
        candidate_count: draft.candidateCount,
        output_mode: "single-png",
      },
      {
        onSuccess: (record) => {
          setAsset(record);
          setRun(null);
          setPrompt("");
        },
      },
    );
  };

  const submitPlan = () => {
    if (!task) return;
    planAsset.mutate(
      { category: task.category, assetId: task.asset_id },
      {
        onSuccess: (nextRun) => {
          setRun(nextRun);
          setPrompt(nextRun.prompt);
        },
      },
    );
  };

  const selectAsset = (value: string) => {
    const nextAsset = assets.data?.find(
      (record) => `${record.task.category}:${record.task.asset_id}` === value,
    );
    setAsset(nextAsset ?? null);
    setRun(null);
    setPrompt("");
    setEditInstruction("");
    setAcceptStyleRisk(false);
    setExportVariant("default");
  };

  const selectRun = (runId: string) => {
    const nextRun = runs.data?.find((item) => item.run_id === runId) ?? null;
    setRun(nextRun);
    setPrompt(nextRun?.prompt ?? "");
    setEditInstruction("");
    setAcceptStyleRisk(false);
    setExportVariant("default");
  };

  const updateAssetReferences = (referenceIds: string[]) => {
    if (!asset || run || runs.data?.length) return;
    updateAsset.mutate(
      { ...asset.task, reference_ids: referenceIds },
      { onSuccess: setAsset },
    );
  };

  const submitGeneration = () => {
    if (!task || !run || !prompt.trim()) return;
    generate.mutate(
      {
        category: task.category,
        assetId: task.asset_id,
        runId: run.run_id,
        input: {
          candidate_count: task.candidate_count,
          prompt_override: prompt.trim(),
        },
      },
      { onSuccess: setRun },
    );
  };

  const submitSelection = (candidateId: string) => {
    if (!task || !run) return;
    selectCandidate.mutate(
      {
        category: task.category,
        assetId: task.asset_id,
        runId: run.run_id,
        candidateId,
      },
      { onSuccess: setRun },
    );
  };

  const submitReview = () => {
    if (!task || !run?.selected_candidate_id) return;
    reviewCandidate.mutate(
      {
        category: task.category,
        assetId: task.asset_id,
        runId: run.run_id,
        candidateId: run.selected_candidate_id,
      },
      { onSuccess: setRun },
    );
  };

  const submitReviewAndRepair = () => {
    if (!task || !run?.selected_candidate_id) return;
    reviewAndRepair.mutate(
      {
        category: task.category,
        assetId: task.asset_id,
        runId: run.run_id,
        input: {
          candidate_id: run.selected_candidate_id,
          automatic_repair: true,
          max_retries: 2,
        },
      },
      {
        onSuccess: (nextRun) => {
          setRun(nextRun);
          setPrompt(nextRun.prompt);
          setAcceptStyleRisk(false);
        },
      },
    );
  };

  const submitTransform = (
    input: Omit<CandidateTransformInput, "candidate_id">,
  ) => {
    if (!task || !run?.selected_candidate_id) return;
    transformCandidate.mutate(
      {
        category: task.category,
        assetId: task.asset_id,
        runId: run.run_id,
        input: { ...input, candidate_id: run.selected_candidate_id },
      },
      {
        onSuccess: (nextRun) => {
          setRun(nextRun);
          setPrompt(nextRun.prompt);
          setAcceptStyleRisk(false);
        },
      },
    );
  };

  const submitMaskedRepaint = async (
    maskPngBase64: string,
    instruction: string,
  ) => {
    if (!task || !run?.selected_candidate_id) return;
    const mask = await saveMask.mutateAsync({
      category: task.category,
      assetId: task.asset_id,
      runId: run.run_id,
      candidateId: run.selected_candidate_id,
      maskPngBase64,
    });
    const nextRun = await editCandidate.mutateAsync({
      category: task.category,
      assetId: task.asset_id,
      runId: run.run_id,
      input: {
        candidate_id: run.selected_candidate_id,
        instruction,
        candidate_count: 1,
        mask_workspace_relative_path: mask.workspace_relative_path,
      },
    });
    setRun(nextRun);
    setPrompt(nextRun.prompt);
    setAcceptStyleRisk(false);
  };

  const submitEdit = () => {
    if (!task || !run?.selected_candidate_id || !editInstruction.trim()) return;
    editCandidate.mutate(
      {
        category: task.category,
        assetId: task.asset_id,
        runId: run.run_id,
        input: {
          candidate_id: run.selected_candidate_id,
          instruction: editInstruction.trim(),
          candidate_count: 1,
          mask_workspace_relative_path: null,
        },
      },
      {
        onSuccess: (nextRun) => {
          setRun(nextRun);
          setPrompt(nextRun.prompt);
          setEditInstruction("");
          setAcceptStyleRisk(false);
        },
      },
    );
  };

  const submitExport = () => {
    if (!task || !run) return;
    exportRun.mutate({
      category: task.category,
      assetId: task.asset_id,
      runId: run.run_id,
      input: {
        variant: exportVariant.trim() || "default",
        accept_style_risk: acceptStyleRisk,
      },
    });
  };

  return (
    <section id="static-production" className="paper-card paper-card--production">
      <p className="paper-card__label">静态资产生产</p>
      <h2>物品 → UI → 角色 → 场景纵向闭环</h2>
      <p>
        规划、生成、局部重绘和视觉评审会调用模型并产生 API 用量；保存任务、候选比较、约束和导出均在本地完成。
      </p>

      <div className="production-form">
        <label>
          资产类别
          <select
            value={draft.category}
            onChange={(event) =>
              draft.setCategory(event.target.value as StaticAssetCategory)
            }
          >
            {CATEGORY_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label>
          资产 ID
          <input
            value={draft.assetId}
            onChange={(event) => draft.setField("assetId", event.target.value)}
            placeholder="green-sword"
          />
        </label>
        <label>
          资产名称
          <input
            value={draft.name}
            onChange={(event) => draft.setField("name", event.target.value)}
            placeholder="青锋剑"
          />
        </label>
        <label>
          用途
          <input
            value={draft.usage}
            onChange={(event) => draft.setField("usage", event.target.value)}
          />
        </label>
        <label className="production-form__brief">
          自然语言需求
          <textarea
            rows={3}
            value={draft.brief}
            onChange={(event) => draft.setField("brief", event.target.value)}
          />
        </label>
        <label>
          候选数量
          <input
            type="number"
            min="1"
            max="4"
            value={draft.candidateCount}
            onChange={(event) =>
              draft.setCandidateCount(Number(event.target.value))
            }
          />
        </label>
        <label>
          输出宽度覆盖
          <input
            type="number"
            min="1"
            max="3840"
            value={outputWidthOverride}
            onChange={(event) => setOutputWidthOverride(event.target.value)}
            placeholder="使用类别预设"
          />
        </label>
        <label>
          输出高度覆盖
          <input
            type="number"
            min="1"
            max="3840"
            value={outputHeightOverride}
            onChange={(event) => setOutputHeightOverride(event.target.value)}
            placeholder="使用类别预设"
          />
        </label>
        <label>
          透明留白覆盖
          <input
            type="number"
            min="0"
            max="0.49"
            step="0.025"
            value={paddingOverride}
            onChange={(event) => setPaddingOverride(event.target.value)}
            placeholder="使用类别预设"
          />
        </label>
        <div className="production-form__wide">
          <ReferencePicker
            references={draftReferences.data ?? []}
            selectedIds={draft.referenceIds}
            onChange={draft.setReferenceIds}
          />
        </div>
        <button type="button" disabled={createAsset.isPending} onClick={submitAsset}>
          {createAsset.isPending ? "正在保存…" : "保存资产任务"}
        </button>
      </div>

      {assets.data?.length ? (
        <div className="production-history-controls">
          <p className="production-history-note">
            当前项目已有 {assets.data.length} 个静态资产任务。
          </p>
          <label>
            已有资产
            <select
              value={asset ? `${asset.task.category}:${asset.task.asset_id}` : ""}
              onChange={(event) => selectAsset(event.target.value)}
            >
              {assets.data.map((record) => (
                <option
                  key={`${record.task.category}:${record.task.asset_id}`}
                  value={`${record.task.category}:${record.task.asset_id}`}
                >
                  {record.task.name}（{record.task.category}/{record.task.asset_id}）
                </option>
              ))}
            </select>
          </label>
          <label>
            运行记录
            <select
              value={run?.run_id ?? ""}
              disabled={!runs.data?.length}
              onChange={(event) => selectRun(event.target.value)}
            >
              {!runs.data?.length ? <option value="">尚无运行记录</option> : null}
              {(runs.data ?? []).map((record) => (
                <option key={record.run_id} value={record.run_id}>
                  {record.run_id} · {record.status}
                </option>
              ))}
            </select>
          </label>
        </div>
      ) : null}

      {asset ? (
        <div className="production-stage">
          <div className="production-stage__header">
            <div>
              <span>当前资产</span>
              <strong>{asset.task.name}</strong>
              <small>{asset.task.asset_id}</small>
            </div>
            <button type="button" disabled={planAsset.isPending} onClick={submitPlan}>
              {planAsset.isPending
                ? "正在规划…"
                : "生成结构化计划（调用模型）"}
            </button>
          </div>
          <fieldset
            className="production-reference-context"
            disabled={Boolean(run || runs.data?.length)}
          >
            <ReferencePicker
              references={assetReferences.data ?? []}
              selectedIds={asset.task.reference_ids}
              onChange={updateAssetReferences}
            />
          </fieldset>
          {run || runs.data?.length ? (
            <p className="production-history-note">
              该任务已进入生产，参考上下文已锁定。
            </p>
          ) : null}
          {run?.plan ? (
            <div className="production-plan">
              <p>
                {run.plan.composition} · {run.plan.camera} · {run.plan.lighting}
              </p>
              <label>
                生成提示词
                <textarea
                  rows={7}
                  value={prompt}
                  onChange={(event) => setPrompt(event.target.value)}
                />
              </label>
              <button
                type="button"
                disabled={generate.isPending}
                onClick={submitGeneration}
              >
                {generate.isPending ? "正在生成…" : "生成候选（调用模型）"}
              </button>
            </div>
          ) : null}
        </div>
      ) : null}

      {run?.candidates.length ? (
        <div className="candidate-grid">
          {run.candidates.map((candidate) => (
            <article
              key={candidate.candidate_id}
              className={
                candidate.candidate_id === run.selected_candidate_id
                  ? "candidate-card candidate-card--selected"
                  : "candidate-card"
              }
            >
              <img
                src={productionCandidateImageUrl(
                  projectId,
                  run.task.category,
                  run.task.asset_id,
                  run.run_id,
                  candidate.candidate_id,
                )}
                alt={"候选 " + candidate.candidate_id}
              />
              <strong>{candidate.candidate_id}</strong>
              <span>
                {candidate.metadata.width}×{candidate.metadata.height} ·{" "}
                {candidate.hard_constraints.passed ? "硬约束通过" : "硬约束失败"}
              </span>
              <button
                type="button"
                disabled={selectCandidate.isPending}
                onClick={() => submitSelection(candidate.candidate_id)}
              >
                选择 {candidate.candidate_id}
              </button>
            </article>
          ))}
        </div>
      ) : null}

      {selected && run ? (
        <div className="production-review-panel">
          <div>
            <h3>定向编辑与评审</h3>
            <label>
              局部编辑指令
              <textarea
                rows={3}
                value={editInstruction}
                onChange={(event) => setEditInstruction(event.target.value)}
              />
            </label>
            <div className="production-actions">
              <button
                type="button"
                disabled={!editInstruction.trim() || editCandidate.isPending}
                onClick={submitEdit}
              >
                {editCandidate.isPending
                  ? "正在重绘…"
                  : "局部重绘（调用模型）"}
              </button>
              <button
                type="button"
                disabled={reviewCandidate.isPending}
                onClick={submitReview}
              >
                {reviewCandidate.isPending
                  ? "正在评审…"
                  : "执行视觉评审（调用模型）"}
              </button>
              <button
                type="button"
                disabled={
                  reviewAndRepair.isPending ||
                  (Boolean(selected.quality_report) &&
                    selected.quality_report?.decision !== "retry" &&
                    (selected.quality_report?.style_review.findings?.length ?? 0) === 0 &&
                    (selected.quality_report?.style_review.issues?.length ?? 0) === 0)
                }
                onClick={submitReviewAndRepair}
              >
                {reviewAndRepair.isPending
                  ? "正在评审与修复…"
                  : "评审并自动定向修复（最多 2 次，调用模型）"}
              </button>
            </div>

            <CandidateEditor
              imageUrl={productionCandidateImageUrl(
                projectId,
                run.task.category,
                run.task.asset_id,
                run.run_id,
                selected.candidate_id,
              )}
              width={selected.metadata.width}
              height={selected.metadata.height}
              pending={
                transformCandidate.isPending ||
                saveMask.isPending ||
                editCandidate.isPending
              }
              onTransform={submitTransform}
              onRepaint={submitMaskedRepaint}
            />
          </div>

          {selected.quality_report ? (
            <div className="quality-summary">
              <h3>质量报告</h3>
              <strong>风格评分：{selected.quality_report.style_review.score}</strong>
              <span>
                身份 {selected.quality_report.style_review.identity_score} · 配色{" "}
                {selected.quality_report.style_review.palette_score} · 线条{" "}
                {selected.quality_report.style_review.line_style_score} · 构图{" "}
                {selected.quality_report.style_review.composition_score}
              </span>
              {selected.quality_report.style_review.issues.map((issue) => (
                <p key={issue}>{issue}</p>
              ))}
              {selected.comparison_relative_path ? (
                <img
                  className="quality-comparison"
                  src={productionCandidateComparisonUrl(
                    projectId,
                    run.task.category,
                    run.task.asset_id,
                    run.run_id,
                    selected.candidate_id,
                  )}
                  alt="候选与项目参考对比图"
                />
              ) : null}
              {selected.quality_report.review_basis?.length ? (
                <div className="quality-evidence-block">
                  <strong>评审依据</strong>
                  {selected.quality_report.review_basis.map((basis) => (
                    <span key={basis}>{basis}</span>
                  ))}
                </div>
              ) : null}
              {selected.quality_report.style_review.findings?.map((finding) => (
                <article
                  className="quality-finding"
                  key={finding.dimension + "-" + finding.summary}
                >
                  <strong>{finding.dimension} · {finding.severity}</strong>
                  <span>{finding.summary}</span>
                  <small>可见证据：{finding.evidence}</small>
                  {finding.repair_hint ? (
                    <small>修复建议：{finding.repair_hint}</small>
                  ) : null}
                </article>
              ))}
              {run.review_attempts?.at(-1)?.repair_plan ? (
                <div className="quality-repair-plan">
                  <strong>定向修复计划</strong>
                  <span>{run.review_attempts.at(-1)?.repair_plan?.reason}</span>
                  {run.review_attempts.at(-1)?.repair_plan?.prompt ? (
                    <pre>{run.review_attempts.at(-1)?.repair_plan?.prompt}</pre>
                  ) : null}
                </div>
              ) : null}
              {run.auto_repair_summary ? (
                <div className="auto-repair-history">
                  <strong>
                    自动修复：{run.auto_repair_summary.retry_count}/
                    {run.auto_repair_summary.max_retries}
                  </strong>
                  <span>{stopReasonLabel(run.auto_repair_summary.stop_reason)}</span>
                  <ol>
                    {run.auto_repair_summary.attempts.map((attempt) => (
                      <li key={attempt.run_id + "-" + attempt.attempt_index}>
                        第 {attempt.attempt_index + 1} 次评审 · {attempt.run_id} · 风格分{" "}
                        {attempt.quality_report?.style_review.score ?? "-"}
                      </li>
                    ))}
                  </ol>
                </div>
              ) : null}
              {selected.quality_report.style_review.score < 75 ? (
                <label className="production-risk-option">
                  <input
                    type="checkbox"
                    checked={acceptStyleRisk}
                    onChange={(event) => setAcceptStyleRisk(event.target.checked)}
                  />
                  <span>接受低分风格风险</span>
                </label>
              ) : null}
              <label>
                导出命名变体
                <input
                  value={exportVariant}
                  onChange={(event) => setExportVariant(event.target.value)}
                />
              </label>
              <button
                type="button"
                disabled={
                  exportRun.isPending ||
                  (selected.quality_report.style_review.score < 75 &&
                    !acceptStyleRisk)
                }
                onClick={submitExport}
              >
                {exportRun.isPending ? "正在导出…" : "导出规范 PNG"}
              </button>
            </div>
          ) : null}
        </div>
      ) : null}

      {createAsset.isError ? (
        <p className="model-test-error">资产任务保存失败，请检查 ID 或重复记录。</p>
      ) : null}
      {planAsset.isError ||
      generate.isError ||
      reviewCandidate.isError ||
      reviewAndRepair.isError ? (
        <p className="model-test-error">
          模型操作失败；请检查 API Key、模型状态和本地运行记录。
        </p>
      ) : null}
      {transformCandidate.isError || saveMask.isError ? (
        <p className="model-test-error">
          本地编辑失败；请检查裁切范围、输出尺寸和蒙版是否与候选一致。
        </p>
      ) : null}
      {exportRun.isError ? (
        <p className="model-test-error">
          导出失败；请处理硬约束、风格风险或同名文件。
        </p>
      ) : null}
      {exportRun.data ? (
        <p className="production-export-success">
          已导出：{exportRun.data.export.relative_path}
        </p>
      ) : null}
    </section>
  );
}
