import { useEffect, useState } from "react";

import {
  type ConstraintProfile,
  type WorkspaceImageRequest,
  useConstraintsQuery,
  useExportConstraintMutation,
  useProcessConstraintMutation,
  useSaveConstraintMutation,
} from "../api/constraints";
import type { AssetCategory } from "../types/core";

const CATEGORY_OPTIONS: Array<{ value: AssetCategory; label: string }> = [
  { value: "item", label: "物品" },
  { value: "ui", label: "UI" },
  { value: "character", label: "角色" },
  { value: "scene", label: "场景" },
  { value: "animation", label: "角色动画" },
  { value: "effect", label: "特效" },
];

const DEFAULT_IMAGE_REQUEST: WorkspaceImageRequest = {
  workspace_relative_path: "",
  asset_id: "preview-asset",
  variant: "default",
  background: {
    mode: "corner_flood",
    color_tolerance: 18,
    alpha_low_threshold: 8,
    alpha_high_threshold: 247,
  },
};

export interface ConstraintCardProps {
  projectId?: string;
}

export function ConstraintCard({ projectId }: ConstraintCardProps) {
  const constraints = useConstraintsQuery(projectId);
  const saveMutation = useSaveConstraintMutation(projectId);
  const processMutation = useProcessConstraintMutation(projectId);
  const exportMutation = useExportConstraintMutation(projectId);
  const [category, setCategory] = useState<AssetCategory>("item");
  const [draft, setDraft] = useState<ConstraintProfile | null>(null);
  const [imageRequest, setImageRequest] =
    useState<WorkspaceImageRequest>(DEFAULT_IMAGE_REQUEST);

  const selectedProfile = constraints.data?.[category];

  useEffect(() => {
    if (selectedProfile) {
      setDraft(selectedProfile);
    }
  }, [selectedProfile]);

  if (!projectId) {
    return (
      <section className="paper-card paper-card--constraints">
        <p className="paper-card__label">资产约束器</p>
        <h2>规范 PNG 处理</h2>
        <p className="empty-state">创建项目后即可配置资产约束。</p>
      </section>
    );
  }

  const updateNumber = (
    field: keyof Pick<
      ConstraintProfile,
      "output_width" | "output_height" | "padding_ratio" | "occupancy_ratio"
    >,
    rawValue: string,
  ) => {
    const value = Number(rawValue);
    if (!draft || !Number.isFinite(value)) {
      return;
    }
    setDraft({ ...draft, [field]: value });
  };

  const submitSave = () => {
    if (!draft) {
      return;
    }
    saveMutation.mutate({ category, profile: draft });
  };

  const submitProcess = () => {
    if (!imageRequest.workspace_relative_path.trim()) {
      return;
    }
    processMutation.mutate({ category, request: imageRequest });
  };

  const submitExport = () => {
    if (!imageRequest.workspace_relative_path.trim()) {
      return;
    }
    exportMutation.mutate({ category, request: imageRequest });
  };

  const preview = processMutation.data;

  return (
    <section className="paper-card paper-card--constraints">
      <p className="paper-card__label">资产约束器</p>
      <h2>规范 PNG 处理与硬检查</h2>
      <p>
        在工作区副本上完成背景移除、Alpha 清理、裁切、缩放、留白和锚点对齐；参考素材源始终保持只读。
      </p>

      <div className="constraint-layout">
        <div className="constraint-panel">
          <h3>类别规格</h3>
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

          {constraints.isPending || !draft ? (
            <p className="empty-state">正在读取类别约束…</p>
          ) : (
            <>
              <div className="constraint-field-grid">
                <label>
                  输出宽度
                  <input
                    type="number"
                    min="1"
                    max="3840"
                    value={draft.output_width}
                    onChange={(event) =>
                      updateNumber("output_width", event.target.value)
                    }
                  />
                </label>
                <label>
                  输出高度
                  <input
                    type="number"
                    min="1"
                    max="3840"
                    value={draft.output_height}
                    onChange={(event) =>
                      updateNumber("output_height", event.target.value)
                    }
                  />
                </label>
                <label>
                  留白比例
                  <input
                    type="number"
                    min="0"
                    max="0.49"
                    step="0.01"
                    value={draft.padding_ratio}
                    onChange={(event) =>
                      updateNumber("padding_ratio", event.target.value)
                    }
                  />
                </label>
                <label>
                  主体占框
                  <input
                    type="number"
                    min="0.01"
                    max="1"
                    step="0.01"
                    value={draft.occupancy_ratio}
                    onChange={(event) =>
                      updateNumber("occupancy_ratio", event.target.value)
                    }
                  />
                </label>
              </div>
              <label className="constraint-check-option">
                <input
                  type="checkbox"
                  checked={draft.require_transparency}
                  onChange={(event) =>
                    setDraft({
                      ...draft,
                      require_transparency: event.target.checked,
                    })
                  }
                />
                <span>要求透明背景与有效 Alpha</span>
              </label>
              <button
                type="button"
                disabled={saveMutation.isPending}
                onClick={submitSave}
              >
                {saveMutation.isPending ? "正在保存…" : "保存约束配置"}
              </button>
              {saveMutation.isSuccess ? (
                <p className="constraint-success">类别约束已保存。</p>
              ) : null}
            </>
          )}
          {constraints.isError || saveMutation.isError ? (
            <p className="model-test-error">约束配置读取或保存失败。</p>
          ) : null}
        </div>

        <div className="constraint-panel">
          <h3>工作区图片</h3>
          <label>
            工作区图片路径
            <input
              value={imageRequest.workspace_relative_path}
              onChange={(event) =>
                setImageRequest({
                  ...imageRequest,
                  workspace_relative_path: event.target.value,
                })
              }
              placeholder="style-pack/references/sword.png"
            />
          </label>
          <div className="constraint-field-grid">
            <label>
              资产 ID
              <input
                value={imageRequest.asset_id}
                onChange={(event) =>
                  setImageRequest({
                    ...imageRequest,
                    asset_id: event.target.value,
                  })
                }
              />
            </label>
            <label>
              变体
              <input
                value={imageRequest.variant}
                onChange={(event) =>
                  setImageRequest({
                    ...imageRequest,
                    variant: event.target.value,
                  })
                }
              />
            </label>
          </div>
          <label>
            背景模式
            <select
              value={imageRequest.background.mode}
              onChange={(event) =>
                setImageRequest({
                  ...imageRequest,
                  background: {
                    ...imageRequest.background,
                    mode: event.target.value as "preserve" | "corner_flood",
                  },
                })
              }
            >
              <option value="corner_flood">移除四角连通背景</option>
              <option value="preserve">保留原背景</option>
            </select>
          </label>
          <label>
            背景颜色容差
            <input
              type="number"
              min="0"
              max="255"
              value={imageRequest.background.color_tolerance}
              onChange={(event) =>
                setImageRequest({
                  ...imageRequest,
                  background: {
                    ...imageRequest.background,
                    color_tolerance: Number(event.target.value),
                  },
                })
              }
            />
          </label>
          <div className="constraint-actions">
            <button
              type="button"
              disabled={
                !draft ||
                !imageRequest.workspace_relative_path.trim() ||
                processMutation.isPending
              }
              onClick={submitProcess}
            >
              {processMutation.isPending ? "正在处理…" : "处理并预览"}
            </button>
            <button
              type="button"
              className="constraint-secondary-button"
              disabled={
                !preview?.hard_constraints.passed || exportMutation.isPending
              }
              onClick={submitExport}
            >
              {exportMutation.isPending ? "正在导出…" : "导出规范 PNG"}
            </button>
          </div>
          {processMutation.isError ? (
            <p className="model-test-error">图片处理失败，请检查路径与约束。</p>
          ) : null}
          {exportMutation.isError ? (
            <p className="model-test-error">导出失败；同名文件不会被覆盖。</p>
          ) : null}
          {exportMutation.data ? (
            <p className="constraint-success">
              已导出：{exportMutation.data.relative_path}
            </p>
          ) : null}
        </div>
      </div>

      {preview ? (
        <div className="constraint-result">
          <figure className="constraint-preview">
            <img
              src={`data:image/png;base64,${preview.processed_png_base64}`}
              alt="约束处理预览"
            />
            <figcaption>
              {preview.metadata.width}×{preview.metadata.height} · RGBA ·
              {preview.metadata.file_bytes} bytes
            </figcaption>
          </figure>
          <div>
            <h3>
              硬约束{preview.hard_constraints.passed ? "全部通过" : "存在失败"}
            </h3>
            <ul className="constraint-check-list">
              {preview.hard_constraints.checks.map((check) => (
                <li
                  key={check.name}
                  className={check.passed ? "constraint-check--ok" : ""}
                >
                  <strong>
                    {check.name}：{check.passed ? "通过" : "未通过"}
                  </strong>
                  <span>{check.message}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      ) : null}
    </section>
  );
}
