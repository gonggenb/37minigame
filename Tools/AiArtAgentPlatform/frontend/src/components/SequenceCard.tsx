import { useEffect, useMemo, useState } from "react";

import {
  type SequenceDriftReport,
  type SequenceFrameRecord,
  type SequenceRun,
  type SequenceTask,
  sequenceArtifactUrl,
  useCreateSequenceMutation,
  useExportSequenceMutation,
  useGenerateSequenceMutation,
  useReprocessSequenceMutation,
  useSelectSequenceMutation,
  useSequenceRunQuery,
  useSequenceRunsQuery,
} from "../api/sequences";
import {
  ACTION_TEMPLATES,
  type SequenceAction,
  type SequenceBackground,
  useSequencePreviewStore,
} from "../stores/sequencePreview";
import { useTaskNavigationStore } from "../stores/taskNavigation";
import {
  calculateCanvas,
  validateGptImage2Canvas,
} from "../features/animation-preview/modelCanvas";

const ACTION_LABELS: Record<SequenceAction, string> = {
  idle: "待机",
  move: "移动",
  attack: "攻击",
  hit: "受击",
  death: "死亡",
};

interface SequenceCardProps {
  projectId?: string;
}

interface DriftChartProps {
  label: string;
  values: number[];
  color: string;
}

function DriftChart({ label, values, color }: DriftChartProps) {
  const width = 240;
  const height = 72;
  const maximum = Math.max(...values, 1);
  const denominator = Math.max(values.length - 1, 1);
  const points = values
    .map((value, index) => {
      const x = 8 + (index / denominator) * (width - 16);
      const y = height - 8 - (value / maximum) * (height - 16);
      return `${x},${y}`;
    })
    .join(" ");

  return (
    <svg
      className="sequence-drift-chart"
      viewBox={`0 0 ${width} ${height}`}
      role="img"
      aria-label={label}
    >
      <line x1="8" y1={height - 8} x2={width - 8} y2={height - 8} />
      <polyline points={points} fill="none" stroke={color} strokeWidth="2" />
    </svg>
  );
}

function centerSeries(frames: SequenceFrameRecord[]): number[] {
  const first = frames[0];
  if (!first) {
    return [];
  }
  return frames.map((frame) =>
    Math.hypot(frame.center_x - first.center_x, frame.center_y - first.center_y),
  );
}

function sizeSeries(frames: SequenceFrameRecord[]): number[] {
  const first = frames[0];
  if (!first) {
    return [];
  }
  return frames.map((frame) => {
    const width = first.subject_width
      ? Math.abs(frame.subject_width - first.subject_width) / first.subject_width
      : 0;
    const height = first.subject_height
      ? Math.abs(frame.subject_height - first.subject_height) / first.subject_height
      : 0;
    return Math.max(width, height);
  });
}

function baselineSeries(frames: SequenceFrameRecord[]): number[] {
  const first = frames[0];
  return first
    ? frames.map((frame) => Math.abs(frame.baseline_y - first.baseline_y))
    : [];
}

function DriftSummary({ report }: { report: SequenceDriftReport }) {
  return (
    <div className={`sequence-drift-summary ${report.passed ? "is-passed" : "is-failed"}`}>
      <strong>{report.passed ? "漂移检查通过" : "漂移检查需要修复"}</strong>
      <span>
        中心 {report.max_center_drift_px.toFixed(2)} px · 尺寸 {" "}
        {(report.max_size_drift_ratio * 100).toFixed(1)}% · 基线 {" "}
        {report.max_baseline_drift_px.toFixed(2)} px
      </span>
      <span>
        角色阈值：中心 4 px、尺寸 8%、基线 2 px；特效同时检查边界、亮度与首尾连续性。
      </span>
      {report.failed_frames.length ? (
        <span>失败帧：{report.failed_frames.map((index) => index + 1).join("、")}</span>
      ) : null}
      {report.issues.map((issue) => <span key={issue}>{issue}</span>)}
    </div>
  );
}

export function SequenceCard({ projectId }: SequenceCardProps) {
  const draft = useSequencePreviewStore();
  const navigationTarget = useTaskNavigationStore((state) => state.target);
  const clearNavigation = useTaskNavigationStore((state) => state.clear);
  const [run, setRun] = useState<SequenceRun | null>(null);
  const [prompt, setPrompt] = useState("");
  const [isPlaying, setIsPlaying] = useState(false);
  const runs = useSequenceRunsQuery(
    projectId,
    draft.category,
    draft.assetId || undefined,
  );
  const sequenceTarget =
    navigationTarget?.workflow === "sequence" &&
    navigationTarget.projectId === projectId &&
    (navigationTarget.category === "animation" ||
      navigationTarget.category === "effect")
      ? navigationTarget
      : null;
  const sequenceCategory =
    sequenceTarget?.category === "animation" ||
    sequenceTarget?.category === "effect"
      ? sequenceTarget.category
      : undefined;
  const openedRun = useSequenceRunQuery(
    projectId,
    sequenceCategory,
    sequenceTarget?.assetId,
    sequenceTarget?.runId ?? undefined,
  );
  const createSequence = useCreateSequenceMutation(projectId);
  const generateSequence = useGenerateSequenceMutation(projectId);
  const selectCandidate = useSelectSequenceMutation(projectId);
  const reprocess = useReprocessSequenceMutation(projectId);
  const exportRun = useExportSequenceMutation(projectId);

  useEffect(() => {
    setRun(null);
    setPrompt("");
    setIsPlaying(false);
    if (navigationTarget && navigationTarget.projectId !== projectId) {
      clearNavigation();
    }
  }, [clearNavigation, projectId]);

  useEffect(() => {
    if (!sequenceTarget || !openedRun.data) return;
    setRun(openedRun.data);
    setPrompt(openedRun.data.prompt);
    draft.restoreTask(openedRun.data.task);
    clearNavigation();
  }, [clearNavigation, draft, openedRun.data, sequenceTarget]);

  useEffect(() => {
    const latest = runs.data?.[0];
    if (latest && !run) {
      setRun(latest);
      setPrompt(latest.prompt);
      draft.restoreTask(latest.task);
    }
  }, [draft, run, runs.data]);

  const candidate = useMemo(() => {
    if (!run?.candidates.length) {
      return null;
    }
    return run.candidates.find(
      (item) => item.candidate_id === run.selected_candidate_id,
    ) ?? run.candidates[0];
  }, [run]);
  const output = candidate?.output ?? null;
  const currentFrame = output
    ? Math.min(draft.currentFrame, output.frame_count - 1)
    : 0;
  const currentRecord = output?.frames[currentFrame];
  const activeRows = run?.task.rows ?? draft.rows;
  const activeColumns = run?.task.columns ?? draft.columns;
  const activeGenerationFrameWidth = run
    ? run.task.generation_frame_width ?? run.task.frame_width
    : draft.generationFrameWidth;
  const activeGenerationFrameHeight = run
    ? run.task.generation_frame_height ?? run.task.frame_height
    : draft.generationFrameHeight;
  const activeFrameWidth = run?.task.frame_width ?? draft.frameWidth;
  const activeFrameHeight = run?.task.frame_height ?? draft.frameHeight;
  const modelCanvas = calculateCanvas(
    activeRows,
    activeColumns,
    activeGenerationFrameWidth,
    activeGenerationFrameHeight,
  );
  const finalCanvas = calculateCanvas(
    activeRows,
    activeColumns,
    activeFrameWidth,
    activeFrameHeight,
  );
  const canvasError = validateGptImage2Canvas(
    modelCanvas.width,
    modelCanvas.height,
  );

  useEffect(() => {
    if (!isPlaying || !output) {
      return;
    }
    const timer = window.setInterval(() => {
      const state = useSequencePreviewStore.getState();
      const next = state.currentFrame + 1;
      if (next >= output.frame_count) {
        if (draft.loop) {
          state.setCurrentFrame(0);
        } else {
          setIsPlaying(false);
        }
      } else {
        state.setCurrentFrame(next);
      }
    }, Math.max(16, Math.round(1000 / draft.previewFps)));
    return () => window.clearInterval(timer);
  }, [draft.loop, draft.previewFps, isPlaying, output]);

  if (!projectId) {
    return (
      <section id="sequence-production" className="paper-card paper-card--sequence">
        <p className="paper-card__label">动画与特效</p>
        <h2>逐帧序列生产</h2>
        <p className="empty-state">创建项目后即可生产动画与特效序列。</p>
      </section>
    );
  }

  const buildTask = (): SequenceTask => ({
    schema_version: 1,
    asset_id: draft.assetId,
    category: draft.category,
    name: draft.name,
    action: draft.category === "animation" ? draft.action : "effect",
    frame_count: draft.frameCount,
    rows: draft.rows,
    columns: draft.columns,
    generation_frame_width: draft.generationFrameWidth,
    generation_frame_height: draft.generationFrameHeight,
    frame_width: draft.frameWidth,
    frame_height: draft.frameHeight,
    preview_fps: draft.previewFps,
    loop: draft.loop,
    baseline: draft.category === "animation" ? "bottom_center" : "center",
    base_frame_workspace_relative_path:
      draft.category === "animation" ? draft.baseFramePath : null,
    lock_first_frame:
      draft.category === "animation" && draft.lockFirstFrame,
    pivot_x: 0.5,
    pivot_y: draft.category === "animation" ? 1 : 0.5,
    blend_mode_hint: draft.category === "animation" ? "alpha" : "additive",
  });

  const createReference = () => {
    createSequence.mutate(buildTask(), {
      onSuccess: (created) => {
        setRun(created);
        setPrompt(created.prompt);
      },
    });
  };

  const mutateRun = (
    action: "generate" | "reprocess" | "export",
  ) => {
    if (!run) {
      return;
    }
    const target = {
      category: run.task.category,
      assetId: run.task.asset_id,
      runId: run.run_id,
    };
    if (action === "generate") {
      generateSequence.mutate(
        {
          ...target,
          input: {
            candidate_count: draft.candidateCount,
            prompt_override: prompt || null,
          },
        },
        { onSuccess: setRun },
      );
    } else if (action === "reprocess") {
      reprocess.mutate(target, { onSuccess: setRun });
    } else {
      exportRun.mutate(target);
    }
  };

  const select = (candidateId: string) => {
    if (!run) {
      return;
    }
    selectCandidate.mutate(
      {
        category: run.task.category,
        assetId: run.task.asset_id,
        runId: run.run_id,
        candidateId,
      },
      { onSuccess: setRun },
    );
  };

  const frameUrl = candidate && run
    ? sequenceArtifactUrl(
        projectId,
        run.task.category,
        run.task.asset_id,
        run.run_id,
        candidate.candidate_id,
        "frame",
        currentFrame,
      )
    : "";
  const sequenceMutationError = createSequence.error ?? generateSequence.error;
  const sequenceMutationErrorMessage = sequenceMutationError instanceof Error
    ? sequenceMutationError.message
    : "序列创建或模型生成失败，请检查路径、网格与模型状态。";

  return (
    <section id="sequence-production" className="paper-card paper-card--sequence">
      <p className="paper-card__label">动画与特效</p>
      <h2>整条序列生成与漂移预览</h2>
      <p className="sequence-cost-notice">
        每个候选只调用一次图像模型生成完整条带；切分、共享缩放、锚点对齐和预览全部在本地完成。
      </p>

      <div className="sequence-form">
        <label>
          序列类型
          <select
            value={draft.category}
            onChange={(event) =>
              draft.setCategory(event.target.value as "animation" | "effect")
            }
          >
            <option value="animation">角色动画</option>
            <option value="effect">透明特效</option>
          </select>
        </label>
        <label>
          动作模板
          <select
            value={draft.action}
            disabled={draft.category === "effect"}
            onChange={(event) => draft.setAction(event.target.value as SequenceAction)}
          >
            {Object.entries(ACTION_LABELS).map(([value, label]) => (
              <option value={value} key={value}>
                {label} · {ACTION_TEMPLATES[value as SequenceAction].frameCount} 帧
              </option>
            ))}
          </select>
        </label>
        <label>
          序列资产 ID
          <input
            value={draft.assetId}
            onChange={(event) => draft.setTextField("assetId", event.target.value)}
          />
        </label>
        <label>
          序列名称
          <input
            value={draft.name}
            onChange={(event) => draft.setTextField("name", event.target.value)}
          />
        </label>
        {([
          ["帧数", "frameCount", draft.frameCount, 1, 32],
          ["行数", "rows", draft.rows, 1, 8],
          ["列数", "columns", draft.columns, 1, 8],
          [
            "模型单格宽度",
            "generationFrameWidth",
            draft.generationFrameWidth,
            1,
            3840,
          ],
          [
            "模型单格高度",
            "generationFrameHeight",
            draft.generationFrameHeight,
            1,
            3840,
          ],
          ["单帧宽度", "frameWidth", draft.frameWidth, 1, 1024],
          ["单帧高度", "frameHeight", draft.frameHeight, 1, 1024],
          ["预览 FPS", "previewFps", draft.previewFps, 1, 60],
          ["候选数量", "candidateCount", draft.candidateCount, 1, 4],
        ] as const).map(([label, field, value, min, max]) => (
          <label key={field}>
            {label}
            <input
              type="number"
              min={min}
              max={max}
              value={value}
              onChange={(event) =>
                draft.setNumberField(field, Number(event.target.value))
              }
            />
          </label>
        ))}
        <label className="sequence-form__wide">
          角色基准帧路径
          <input
            value={draft.baseFramePath}
            disabled={draft.category === "effect"}
            placeholder="assets/character/hero/selected/base.png"
            onChange={(event) =>
              draft.setTextField("baseFramePath", event.target.value)
            }
          />
        </label>
        <label className="sequence-check-option">
          <input
            type="checkbox"
            checked={draft.lockFirstFrame}
            disabled={draft.category === "effect"}
            onChange={(event) => draft.setLockFirstFrame(event.target.checked)}
          />
          锁定第一帧为已批准基准帧
        </label>
        <label className="sequence-check-option">
          <input
            type="checkbox"
            checked={draft.loop}
            onChange={(event) => draft.setLoop(event.target.checked)}
          />
          循环播放
        </label>
        <button
          type="button"
          disabled={
            !draft.assetId ||
            !draft.name ||
            (draft.category === "animation" && !draft.baseFramePath) ||
            createSequence.isPending
          }
          onClick={createReference}
        >
          创建参考网格
        </button>
      </div>
      <div className="sequence-canvas-facts">
        <span>模型请求画布：{modelCanvas.width} × {modelCanvas.height}</span>
        <span>最终 Sprite Sheet：{finalCanvas.width} × {finalCanvas.height}</span>
      </div>
      {canvasError ? <p className="model-test-error">{canvasError}</p> : null}

      {run ? (
        <div className="sequence-stage">
          <div className="sequence-stage__header">
            <div>
              <span>{run.task.category === "animation" ? "角色动画" : "透明特效"}</span>
              <strong>{run.task.name}</strong>
              <small>{run.run_id} · {run.status}</small>
            </div>
            <span>{run.task.rows} × {run.task.columns} 网格 · {run.task.frame_count} 帧</span>
          </div>
          <label>
            序列生成提示词
            <textarea
              rows={5}
              value={prompt}
              onChange={(event) => setPrompt(event.target.value)}
            />
          </label>
          <div className="sequence-actions">
            <button
              type="button"
              disabled={Boolean(canvasError) || generateSequence.isPending}
              onClick={() => mutateRun("generate")}
            >
              生成完整序列（调用模型）
            </button>
            <button
              type="button"
              disabled={!run.candidates.length || reprocess.isPending}
              onClick={() => mutateRun("reprocess")}
            >
              重新离线处理
            </button>
          </div>
        </div>
      ) : null}

      {run?.candidates.length ? (
        <div className="sequence-candidates">
          {run.candidates.map((item) => (
            <article
              className={
                item.candidate_id === run.selected_candidate_id
                  ? "sequence-candidate is-selected"
                  : "sequence-candidate"
              }
              key={item.candidate_id}
            >
              <img
                src={sequenceArtifactUrl(
                  projectId,
                  run.task.category,
                  run.task.asset_id,
                  run.run_id,
                  item.candidate_id,
                  "sprite-sheet",
                )}
                alt={`序列候选 ${item.candidate_id}`}
              />
              <button type="button" onClick={() => select(item.candidate_id)}>
                选择 {item.candidate_id}
              </button>
            </article>
          ))}
        </div>
      ) : null}

      {output && candidate && run && currentRecord ? (
        <div className="sequence-preview-layout">
          <div>
            <div className="sequence-preview-toolbar">
              <button type="button" onClick={() => setIsPlaying((value) => !value)}>
                {isPlaying ? "暂停序列" : "播放序列"}
              </button>
              <label>
                预览背景
                <select
                  value={draft.background}
                  onChange={(event) =>
                    draft.setBackground(event.target.value as SequenceBackground)
                  }
                >
                  <option value="checker">透明棋盘</option>
                  <option value="paper">宣纸米白</option>
                  <option value="ink">墨灰</option>
                </select>
              </label>
              <input
                aria-label="当前帧"
                type="range"
                min={0}
                max={output.frame_count - 1}
                value={currentFrame}
                onChange={(event) => draft.setCurrentFrame(Number(event.target.value))}
              />
            </div>
            <div
              className={`sequence-preview sequence-preview--${draft.background}`}
              data-testid="sequence-preview"
            >
              <img src={frameUrl} alt={`序列帧 ${currentFrame + 1} / ${output.frame_count}`} />
              <span
                className="sequence-alpha-bounds"
                style={{
                  left: `${(currentRecord.alpha_bounds[0] / output.frame_width) * 100}%`,
                  top: `${(currentRecord.alpha_bounds[1] / output.frame_height) * 100}%`,
                  width: `${((currentRecord.alpha_bounds[2] - currentRecord.alpha_bounds[0]) / output.frame_width) * 100}%`,
                  height: `${((currentRecord.alpha_bounds[3] - currentRecord.alpha_bounds[1]) / output.frame_height) * 100}%`,
                }}
              />
              <span
                className="sequence-anchor"
                style={{
                  left: `${run.task.pivot_x * 100}%`,
                  top: `${run.task.pivot_y * 100}%`,
                }}
              />
              <span
                className="sequence-baseline"
                style={{ top: `${(currentRecord.baseline_y / output.frame_height) * 100}%` }}
              />
            </div>
            <div className="sequence-frame-facts">
              <span>当前帧：{currentFrame + 1} / {output.frame_count}</span>
              <span>锚点：({run.task.pivot_x}, {run.task.pivot_y})</span>
              <span>Alpha 边界：{currentRecord.alpha_bounds.join(", ")}</span>
              <span>脚底基线：{currentRecord.baseline_y} px</span>
            </div>
          </div>

          <div className="sequence-drift-panel">
            {output.drift_report ? <DriftSummary report={output.drift_report} /> : null}
            <div>
              <strong>中心漂移</strong>
              <DriftChart
                label="中心漂移曲线"
                values={centerSeries(output.frames)}
                color="#8a4b37"
              />
            </div>
            <div>
              <strong>尺寸漂移</strong>
              <DriftChart
                label="尺寸漂移曲线"
                values={sizeSeries(output.frames)}
                color="#77703f"
              />
            </div>
            <div>
              <strong>基线漂移</strong>
              <DriftChart
                label="基线漂移曲线"
                values={baselineSeries(output.frames)}
                color="#4f7149"
              />
            </div>
          </div>
        </div>
      ) : null}

      {output && candidate && run ? (
        <div className="sequence-output-actions">
          <a href={frameUrl} download>下载当前帧 PNG</a>
          <a
            href={sequenceArtifactUrl(
              projectId,
              run.task.category,
              run.task.asset_id,
              run.run_id,
              candidate.candidate_id,
              "sprite-sheet",
            )}
            download
          >
            下载 Sprite Sheet
          </a>
          <a
            href={sequenceArtifactUrl(
              projectId,
              run.task.category,
              run.task.asset_id,
              run.run_id,
              candidate.candidate_id,
              "gif",
            )}
            download
          >
            下载 GIF
          </a>
          <a
            href={sequenceArtifactUrl(
              projectId,
              run.task.category,
              run.task.asset_id,
              run.run_id,
              candidate.candidate_id,
              "webp",
            )}
            download
          >
            下载 WebP
          </a>
          <button
            type="button"
            disabled={
              !output.drift_report?.passed ||
              !candidate ||
              exportRun.isPending
            }
            onClick={() => mutateRun("export")}
          >
            无覆盖导出全部文件
          </button>
        </div>
      ) : null}

      {exportRun.data ? (
        <p className="production-export-success">
          已导出 {exportRun.data.files.length} 个序列文件。
        </p>
      ) : null}
      {createSequence.isError || generateSequence.isError ? (
        <p className="model-test-error">{sequenceMutationErrorMessage}</p>
      ) : null}
      {reprocess.isError || selectCandidate.isError || exportRun.isError ? (
        <p className="model-test-error">序列处理、选择或导出失败，请检查漂移报告与同名文件。</p>
      ) : null}
    </section>
  );
}
