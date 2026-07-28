import { useEffect, useState } from "react";

import {
  useCreateProjectMutation,
  useUpdateProjectMutation,
} from "../api/projects";
import { ApiError } from "../api/client";
import type {
  AssetCategory,
  ProjectActivitySummary,
  ProjectConfig,
} from "../types/core";

const VISUAL_PRESET = "wuxia-ink-chibi-topdown-2_5d" as const;
const CATEGORY_LABELS: Record<AssetCategory, string> = {
  character: "角色",
  scene: "场景",
  item: "物品",
  animation: "动画",
  effect: "特效",
  ui: "UI",
};

export interface ProjectWorkspaceCardProps {
  projects: ProjectConfig[];
  activeProject: ProjectConfig | null;
  activity?: ProjectActivitySummary;
  onSelect: (projectId: string) => void;
}

function cloneProject(project: ProjectConfig): ProjectConfig {
  return {
    ...project,
    models: { ...project.models },
    generation: { ...project.generation },
    review: { ...project.review },
  };
}

export function ProjectWorkspaceCard({
  projects,
  activeProject,
  activity,
  onSelect,
}: ProjectWorkspaceCardProps) {
  const createProject = useCreateProjectMutation();
  const updateProject = useUpdateProjectMutation();
  const [createId, setCreateId] = useState("");
  const [createName, setCreateName] = useState("");
  const [createLanguage, setCreateLanguage] = useState<"zh-CN" | "en-US">(
    "zh-CN",
  );
  const [draft, setDraft] = useState<ProjectConfig | null>(
    activeProject ? cloneProject(activeProject) : null,
  );
  const [message, setMessage] = useState("");

  useEffect(() => {
    setDraft(activeProject ? cloneProject(activeProject) : null);
    setMessage("");
  }, [activeProject]);

  const createError = createProject.isError
    ? createProject.error instanceof ApiError && createProject.error.status === 409
      ? "项目 ID 已存在，请换一个 ID。"
      : "项目创建失败，请检查 ID 和本地服务。"
    : "";

  const submitCreate = () => {
    if (!createId.trim() || !createName.trim()) return;
    createProject.mutate(
      {
        project_id: createId.trim(),
        display_name: createName.trim(),
        visual_type: VISUAL_PRESET,
        language: createLanguage,
      },
      {
        onSuccess: (project) => {
          setCreateId("");
          setCreateName("");
          setMessage("项目已创建并切换。");
          onSelect(project.project_id);
        },
      },
    );
  };

  const submitUpdate = () => {
    if (!draft) return;
    updateProject.mutate(
      { projectId: draft.project_id, project: draft },
      { onSuccess: () => setMessage("项目配置已保存。") },
    );
  };

  return (
    <section className="paper-card paper-card--projects">
      <p className="paper-card__label">项目工作区</p>
      <h2>创建、选择与配置</h2>

      <label>
        当前项目
        <select
          value={activeProject?.project_id ?? ""}
          onChange={(event) => onSelect(event.target.value)}
        >
          {!projects.length ? <option value="">尚无项目</option> : null}
          {projects.map((project) => (
            <option key={project.project_id} value={project.project_id}>
              {project.display_name}（{project.project_id}）
            </option>
          ))}
        </select>
      </label>

      <div className="project-management-grid">
        <div className="project-management-panel">
          <h3>新建项目</h3>
          <label>
            新项目 ID
            <input
              value={createId}
              onChange={(event) => setCreateId(event.target.value)}
              placeholder="wuxia-art-project"
            />
          </label>
          <label>
            新项目名称
            <input
              value={createName}
              onChange={(event) => setCreateName(event.target.value)}
              placeholder="武侠美术项目"
            />
          </label>
          <label>
            新项目语言
            <select
              value={createLanguage}
              onChange={(event) =>
                setCreateLanguage(event.target.value as "zh-CN" | "en-US")
              }
            >
              <option value="zh-CN">简体中文</option>
              <option value="en-US">English</option>
            </select>
          </label>
          <label>
            新项目视觉预设
            <input value={VISUAL_PRESET} disabled />
          </label>
          <button
            type="button"
            disabled={
              !createId.trim() || !createName.trim() || createProject.isPending
            }
            onClick={submitCreate}
          >
            {createProject.isPending ? "正在创建…" : "创建并切换"}
          </button>
          {createError ? <p className="model-test-error">{createError}</p> : null}
        </div>

        {draft ? (
          <div className="project-management-panel">
            <h3>编辑当前项目</h3>
            <label>
              项目 ID
              <input value={draft.project_id} disabled />
            </label>
            <label>
              视觉预设
              <input value={draft.visual_type} disabled />
            </label>
            <label>
              项目名称
              <input
                value={draft.display_name}
                onChange={(event) =>
                  setDraft({ ...draft, display_name: event.target.value })
                }
              />
            </label>
            <label>
              项目语言
              <select
                value={draft.language}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    language: event.target.value as "zh-CN" | "en-US",
                  })
                }
              >
                <option value="zh-CN">简体中文</option>
                <option value="en-US">English</option>
              </select>
            </label>
            <label>
              规划模型
              <input
                value={draft.models.planner_model}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    models: { ...draft.models, planner_model: event.target.value },
                  })
                }
              />
            </label>
            <label>
              评审模型
              <input
                value={draft.models.review_model}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    models: { ...draft.models, review_model: event.target.value },
                  })
                }
              />
            </label>
            <label>
              图像模型
              <input
                value={draft.models.image_model}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    models: { ...draft.models, image_model: event.target.value },
                  })
                }
              />
            </label>
            <label>
              候选数量
              <input
                type="number"
                min="1"
                max="4"
                value={draft.generation.candidate_count}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    generation: {
                      ...draft.generation,
                      candidate_count: Number(event.target.value),
                    },
                  })
                }
              />
            </label>
            <label>
              自动修复次数
              <input
                type="number"
                min="0"
                max="2"
                value={draft.generation.automatic_retry_count}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    generation: {
                      ...draft.generation,
                      automatic_retry_count: Number(event.target.value),
                    },
                  })
                }
              />
            </label>
            <label>
              图片质量
              <select
                value={draft.generation.image_quality}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    generation: {
                      ...draft.generation,
                      image_quality: event.target.value as ProjectConfig["generation"]["image_quality"],
                    },
                  })
                }
              >
                <option value="low">low</option>
                <option value="medium">medium</option>
                <option value="high">high</option>
                <option value="auto">auto</option>
              </select>
            </label>
            <label>
              透明策略
              <select
                value={draft.generation.transparency_mode}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    generation: {
                      ...draft.generation,
                      transparency_mode: event.target.value as ProjectConfig["generation"]["transparency_mode"],
                    },
                  })
                }
              >
                <option value="postprocess">生成后处理</option>
                <option value="opaque">不透明</option>
              </select>
            </label>
            <label className="project-checkbox">
              <input
                type="checkbox"
                checked={draft.review.enabled}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    review: { ...draft.review, enabled: event.target.checked },
                  })
                }
              />
              启用视觉评审
            </label>
            <label>
              最低风格分
              <input
                type="number"
                min="0"
                max="100"
                value={draft.review.minimum_style_score}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    review: {
                      ...draft.review,
                      minimum_style_score: Number(event.target.value),
                    },
                  })
                }
              />
            </label>
            <label className="project-checkbox">
              <input
                type="checkbox"
                checked={draft.review.hard_constraints_required}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    review: {
                      ...draft.review,
                      hard_constraints_required: event.target.checked,
                    },
                  })
                }
              />
              强制硬约束通过
            </label>
            <button
              type="button"
              disabled={updateProject.isPending || !draft.display_name.trim()}
              onClick={submitUpdate}
            >
              {updateProject.isPending ? "正在保存…" : "保存项目配置"}
            </button>
            {updateProject.isError ? (
              <p className="model-test-error">项目配置保存失败。</p>
            ) : null}
          </div>
        ) : null}
      </div>

      {activity ? (
        <div className="project-activity-summary">
          <strong>参考图 {activity.reference_count} 张</strong>
          {activity.categories.map((category) => (
            <span key={category.category}>
              {CATEGORY_LABELS[category.category]} {category.task_count}
            </span>
          ))}
        </div>
      ) : null}
      {message ? <p className="production-export-success">{message}</p> : null}
    </section>
  );
}
