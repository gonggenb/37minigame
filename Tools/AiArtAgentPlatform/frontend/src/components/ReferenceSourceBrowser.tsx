import { useState } from "react";

import {
  useImportReferenceMutation,
  useReferenceSourceQuery,
} from "../api/stylePack";
import type { AssetCategory } from "../types/core";

const CATEGORY_OPTIONS: Array<{ value: AssetCategory; label: string }> = [
  { value: "character", label: "角色" },
  { value: "scene", label: "场景" },
  { value: "item", label: "物品" },
  { value: "animation", label: "动画" },
  { value: "effect", label: "特效" },
  { value: "ui", label: "UI" },
];

export interface ReferenceSourceBrowserProps {
  projectId: string;
}

function splitTags(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

export function ReferenceSourceBrowser({ projectId }: ReferenceSourceBrowserProps) {
  const [queryInput, setQueryInput] = useState("");
  const [query, setQuery] = useState("");
  const source = useReferenceSourceQuery(projectId, query, 100);
  const importReference = useImportReferenceMutation(projectId);
  const [selectedPath, setSelectedPath] = useState("");
  const [referenceId, setReferenceId] = useState("");
  const [category, setCategory] = useState<AssetCategory>("character");
  const [identities, setIdentities] = useState("");
  const [usages, setUsages] = useState("gameplay");
  const [viewpoints, setViewpoints] = useState("topdown-45");
  const [materials, setMaterials] = useState("");
  const [notes, setNotes] = useState("");

  const submit = () => {
    if (!selectedPath || !referenceId.trim()) return;
    importReference.mutate(
      {
        reference_id: referenceId.trim(),
        source_relative_path: selectedPath,
        categories: [category],
        identities: splitTags(identities),
        usages: splitTags(usages),
        viewpoints: splitTags(viewpoints),
        materials: splitTags(materials),
        notes: notes.trim(),
      },
      {
        onSuccess: () => {
          setSelectedPath("");
          setReferenceId("");
          setIdentities("");
          setMaterials("");
          setNotes("");
        },
      },
    );
  };

  return (
    <div className="source-browser">
      <div className="source-browser__header">
        <div>
          <h3>素材源浏览</h3>
          <p>源目录只读；导入操作只会复制文件到当前项目参考库。</p>
        </div>
        <div className="source-browser__search">
          <label>
            搜索素材源
            <input
              value={queryInput}
              onChange={(event) => setQueryInput(event.target.value)}
            />
          </label>
          <button type="button" onClick={() => setQuery(queryInput.trim())}>
            搜索
          </button>
        </div>
      </div>

      {source.isError ? (
        <p className="model-test-error">素材源读取失败。</p>
      ) : null}
      <div className="source-browser__results">
        {(source.data ?? []).map((file) => (
          <button
            key={file.relative_path}
            type="button"
            className={
              selectedPath === file.relative_path
                ? "source-browser__result source-browser__result--selected"
                : "source-browser__result"
            }
            onClick={() => setSelectedPath(file.relative_path)}
          >
            <strong>选择 {file.relative_path}</strong>
            <span>{Math.max(1, Math.round(file.size_bytes / 1024))} KB</span>
          </button>
        ))}
        {source.data && !source.data.length ? (
          <p className="empty-state">没有匹配的源文件。</p>
        ) : null}
      </div>

      {selectedPath ? (
        <div className="source-browser__import">
          <p>
            已选择：<strong>{selectedPath}</strong>
          </p>
          <div className="source-browser__form">
            <label>
              参考 ID
              <input
                value={referenceId}
                onChange={(event) => setReferenceId(event.target.value)}
              />
            </label>
            <label>
              类别
              <select
                value={category}
                onChange={(event) =>
                  setCategory(event.target.value as AssetCategory)
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
              身份标签
              <input
                value={identities}
                onChange={(event) => setIdentities(event.target.value)}
                placeholder="hero-main,young-swordsman"
              />
            </label>
            <label>
              用途标签
              <input
                value={usages}
                onChange={(event) => setUsages(event.target.value)}
              />
            </label>
            <label>
              视角标签
              <input
                value={viewpoints}
                onChange={(event) => setViewpoints(event.target.value)}
              />
            </label>
            <label>
              材质标签
              <input
                value={materials}
                onChange={(event) => setMaterials(event.target.value)}
              />
            </label>
            <label className="source-browser__wide">
              备注
              <textarea
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                rows={3}
              />
            </label>
          </div>
          <button
            type="button"
            disabled={!referenceId.trim() || importReference.isPending}
            onClick={submit}
          >
            {importReference.isPending
              ? "正在复制…"
              : "复制到项目参考库"}
          </button>
          {importReference.isError ? (
            <p className="model-test-error">参考复制失败，请检查 ID 和标签。</p>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
