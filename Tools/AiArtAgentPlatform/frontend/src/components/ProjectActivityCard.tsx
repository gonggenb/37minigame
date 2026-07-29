import type {
  AssetCategory,
  ProjectActivityItem,
  ProjectActivitySummary,
} from "../types/core";
import { useTaskNavigationStore } from "../stores/taskNavigation";

const CATEGORIES: Array<{ category: AssetCategory; label: string }> = [
  { category: "character", label: "角色" },
  { category: "scene", label: "场景" },
  { category: "item", label: "物品" },
  { category: "animation", label: "动画" },
  { category: "effect", label: "特效" },
  { category: "ui", label: "UI" },
];

interface ProjectActivityCardProps {
  projectId?: string;
  activity?: ProjectActivitySummary;
}

export function ProjectActivityCard({
  projectId,
  activity,
}: ProjectActivityCardProps) {
  const requestOpen = useTaskNavigationStore((state) => state.requestOpen);

  const openItem = (item: ProjectActivityItem) => {
    if (!projectId) return;
    requestOpen({
      projectId,
      workflow: item.workflow,
      category: item.category,
      assetId: item.asset_id,
      runId: item.run_id,
    });
    document
      .getElementById(
        item.workflow === "static"
          ? "static-production"
          : "sequence-production",
      )
      ?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  return (
    <section className="paper-card project-activity-card">
      <p className="paper-card__label">最近任务</p>
      <h2>六类美术活动</h2>
      <div className="project-activity-grid">
        {CATEGORIES.map(({ category, label }) => {
          const summary = activity?.categories.find(
            (item) => item.category === category,
          );
          const recent = summary?.recent ?? [];
          return (
            <article className="project-activity-column" key={category}>
              <div className="project-activity-column__header">
                <h3>{label}</h3>
                <span>{summary?.task_count ?? 0}</span>
              </div>
              {recent.length ? (
                recent.map((item) => (
                  <button
                    type="button"
                    className="project-activity-item"
                    aria-label={`打开 ${item.name}`}
                    key={`${item.workflow}:${item.category}:${item.asset_id}:${item.run_id ?? "draft"}`}
                    onClick={() => openItem(item)}
                  >
                    <strong>{item.name}</strong>
                    <span>{item.status}</span>
                    <small>{item.asset_id}</small>
                  </button>
                ))
              ) : (
                <p className="empty-state">暂无最近任务</p>
              )}
            </article>
          );
        })}
      </div>
    </section>
  );
}
