import { useState } from "react";

import {
  referenceThumbnailUrl,
  type ReferenceAsset,
  useDeleteReferenceMutation,
  useReferencesQuery,
  useUpdateReferenceMutation,
} from "../api/stylePack";
import type { AssetCategory } from "../types/core";

const CATEGORY_OPTIONS: Array<{ value: "" | AssetCategory; label: string }> = [
  { value: "", label: "全部类别" },
  { value: "character", label: "角色" },
  { value: "scene", label: "场景" },
  { value: "item", label: "物品" },
  { value: "animation", label: "动画" },
  { value: "effect", label: "特效" },
  { value: "ui", label: "UI" },
];

interface EditDraft {
  categories: string;
  identities: string;
  usages: string;
  viewpoints: string;
  materials: string;
  notes: string;
}

export interface ReferenceLibraryProps {
  projectId: string;
}

export function coverageMessage(count: number): string {
  if (count < 10) return "风格覆盖不足：建议至少导入 10 张参考图";
  if (count <= 30) return "参考数量处于推荐范围（10–30 张）";
  return "参考数量超过 30 张：建议精简重复参考";
}

function joinTags(values: string[]): string {
  return values.join(",");
}

function splitTags(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function createDraft(reference: ReferenceAsset): EditDraft {
  return {
    categories: joinTags(reference.categories),
    identities: joinTags(reference.identities),
    usages: joinTags(reference.usages),
    viewpoints: joinTags(reference.viewpoints),
    materials: joinTags(reference.materials),
    notes: reference.notes,
  };
}

export function ReferenceLibrary({ projectId }: ReferenceLibraryProps) {
  const [category, setCategory] = useState<"" | AssetCategory>("");
  const [identity, setIdentity] = useState("");
  const [usage, setUsage] = useState("");
  const [viewpoint, setViewpoint] = useState("");
  const [material, setMaterial] = useState("");
  const references = useReferencesQuery(projectId, {
    category: category || undefined,
    identity: identity || undefined,
    usage: usage || undefined,
    viewpoint: viewpoint || undefined,
    material: material || undefined,
    limit: 100,
  });
  const updateReference = useUpdateReferenceMutation(projectId);
  const deleteReference = useDeleteReferenceMutation(projectId);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editDraft, setEditDraft] = useState<EditDraft | null>(null);

  const startEdit = (reference: ReferenceAsset) => {
    setEditingId(reference.reference_id);
    setEditDraft(createDraft(reference));
  };

  const saveEdit = () => {
    if (!editingId || !editDraft) return;
    updateReference.mutate(
      {
        referenceId: editingId,
        input: {
          categories: splitTags(editDraft.categories) as AssetCategory[],
          identities: splitTags(editDraft.identities),
          usages: splitTags(editDraft.usages),
          viewpoints: splitTags(editDraft.viewpoints),
          materials: splitTags(editDraft.materials),
          notes: editDraft.notes.trim(),
        },
      },
      {
        onSuccess: () => {
          setEditingId(null);
          setEditDraft(null);
        },
      },
    );
  };

  const remove = (referenceId: string) => {
    if (!window.confirm("只移除项目副本，不会删除只读源文件。")) return;
    deleteReference.mutate(referenceId);
  };

  return (
    <div className="reference-library">
      <div className="reference-library__header">
        <div>
          <h3>项目参考库</h3>
          <p>{coverageMessage(references.data?.length ?? 0)}</p>
        </div>
        <div className="reference-filters">
          <label>
            类别筛选
            <select
              value={category}
              onChange={(event) =>
                setCategory(event.target.value as "" | AssetCategory)
              }
            >
              {CATEGORY_OPTIONS.map((option) => (
                <option key={option.value || "all"} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            身份筛选
            <input
              value={identity}
              onChange={(event) => setIdentity(event.target.value.trim())}
            />
          </label>
          <label>
            用途筛选
            <input
              value={usage}
              onChange={(event) => setUsage(event.target.value.trim())}
            />
          </label>
          <label>
            视角筛选
            <input
              value={viewpoint}
              onChange={(event) => setViewpoint(event.target.value.trim())}
            />
          </label>
          <label>
            材质筛选
            <input
              value={material}
              onChange={(event) => setMaterial(event.target.value.trim())}
            />
          </label>
        </div>
      </div>

      {references.isError ? (
        <p className="model-test-error">参考库读取失败。</p>
      ) : null}
      <div className="reference-grid">
        {(references.data ?? []).map((reference) => (
          <article className="reference-card" key={reference.reference_id}>
            <div className="reference-card__thumbnail">
              <img
                src={referenceThumbnailUrl(projectId, reference)}
                alt={`${reference.reference_id} 缩略图`}
              />
            </div>
            <div className="reference-card__body">
              <h4>{reference.reference_id}</h4>
              <p>
                {reference.width} × {reference.height} · {reference.categories.join("、")}
              </p>
              <dl>
                <dt>身份</dt>
                <dd>{reference.identities.join("、") || "未标注"}</dd>
                <dt>用途</dt>
                <dd>{reference.usages.join("、") || "未标注"}</dd>
                <dt>视角</dt>
                <dd>{reference.viewpoints.join("、") || "未标注"}</dd>
                <dt>材质</dt>
                <dd>{reference.materials.join("、") || "未标注"}</dd>
              </dl>
              <div className="reference-card__actions">
                <button
                  type="button"
                  aria-label={`编辑 ${reference.reference_id}`}
                  onClick={() => startEdit(reference)}
                >
                  编辑标签
                </button>
                <button
                  type="button"
                  aria-label={`移除项目副本 ${reference.reference_id}`}
                  onClick={() => remove(reference.reference_id)}
                >
                  移除副本
                </button>
              </div>
            </div>
          </article>
        ))}
      </div>

      {editingId && editDraft ? (
        <div className="reference-editor">
          <h4>编辑 {editingId}</h4>
          <div className="reference-editor__grid">
            <label>
              类别标签
              <input
                value={editDraft.categories}
                onChange={(event) =>
                  setEditDraft({ ...editDraft, categories: event.target.value })
                }
              />
            </label>
            <label>
              身份标签
              <input
                value={editDraft.identities}
                onChange={(event) =>
                  setEditDraft({ ...editDraft, identities: event.target.value })
                }
              />
            </label>
            <label>
              用途标签
              <input
                value={editDraft.usages}
                onChange={(event) =>
                  setEditDraft({ ...editDraft, usages: event.target.value })
                }
              />
            </label>
            <label>
              视角标签
              <input
                value={editDraft.viewpoints}
                onChange={(event) =>
                  setEditDraft({ ...editDraft, viewpoints: event.target.value })
                }
              />
            </label>
            <label>
              材质标签
              <input
                value={editDraft.materials}
                onChange={(event) =>
                  setEditDraft({ ...editDraft, materials: event.target.value })
                }
              />
            </label>
            <label>
              备注
              <textarea
                value={editDraft.notes}
                onChange={(event) =>
                  setEditDraft({ ...editDraft, notes: event.target.value })
                }
                rows={3}
              />
            </label>
          </div>
          <div className="reference-card__actions">
            <button
              type="button"
              disabled={updateReference.isPending}
              onClick={saveEdit}
            >
              保存标签
            </button>
            <button
              type="button"
              onClick={() => {
                setEditingId(null);
                setEditDraft(null);
              }}
            >
              取消
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
