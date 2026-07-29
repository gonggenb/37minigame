import { useEffect } from "react";

import { useHealthQuery } from "../api/health";
import { useProjectActivityQuery, useProjectsQuery } from "../api/projects";
import {
  ConnectionStatus,
  type ConnectionState,
} from "../components/ConnectionStatus";
import { ConstraintCard } from "../components/ConstraintCard";
import { CostSummaryCard } from "../components/CostSummaryCard";
import { ModelStatusCard } from "../components/ModelStatusCard";
import { ProductionCard } from "../components/ProductionCard";
import { ProjectActivityCard } from "../components/ProjectActivityCard";
import { ProjectWorkspaceCard } from "../components/ProjectWorkspaceCard";
import { SequenceCard } from "../components/SequenceCard";
import { StylePackCard } from "../components/StylePackCard";
import {
  resolveActiveProjectId,
  useProjectWorkspaceStore,
} from "../stores/projectWorkspace";
import "./styles.css";

function resolveConnectionState(
  isPending: boolean,
  isError: boolean,
): ConnectionState {
  if (isPending) {
    return "loading";
  }
  return isError ? "error" : "connected";
}

export function App() {
  const health = useHealthQuery();
  const projects = useProjectsQuery();
  const activeProjectId = useProjectWorkspaceStore(
    (state) => state.activeProjectId,
  );
  const setActiveProjectId = useProjectWorkspaceStore(
    (state) => state.setActiveProjectId,
  );
  const activeProject = projects.data?.find(
    (project) => project.project_id === activeProjectId,
  );
  const activity = useProjectActivityQuery(activeProject?.project_id);
  const connectionState = resolveConnectionState(health.isPending, health.isError);

  useEffect(() => {
    if (!projects.data) return;
    const resolved = resolveActiveProjectId(activeProjectId, projects.data);
    if (resolved !== activeProjectId) {
      setActiveProjectId(resolved);
    }
  }, [activeProjectId, projects.data, setActiveProjectId]);

  return (
    <main className="workbench-shell">
      <header className="hero-panel">
        <div className="hero-panel__seal" aria-hidden="true">
          艺
        </div>
        <div>
          <p className="eyebrow">LOCAL ART PRODUCTION</p>
          <h1>2D 小游戏 AI 美术生产工作台</h1>
          <p className="hero-panel__summary">
            为角色、场景、物品、逐帧动画、特效和 UI 建立统一、可追踪的本地生产流程。
          </p>
        </div>
      </header>

      <div className="workbench-grid">
        <section className="paper-card">
          <p className="paper-card__label">当前风格包</p>
          <h2>Q 版水墨武侠俯视角</h2>
          <p>
            近似正交的 2.5D 俯视构图，使用宣纸米白、墨灰、青绿、朱红与暗金形成轻量武侠画面。
          </p>
          <dl className="style-facts">
            <div>
              <dt>视角</dt>
              <dd>35°–55° 俯视语义</dd>
            </div>
            <div>
              <dt>生成模型</dt>
              <dd>gpt-image-2</dd>
            </div>
            <div>
              <dt>透明策略</dt>
              <dd>生成后抠图与 Alpha 校验</dd>
            </div>
          </dl>
        </section>

        <section className="paper-card paper-card--status">
          <p className="paper-card__label">平台状态</p>
          <h2>本地连接</h2>
          <ConnectionStatus
            state={connectionState}
            onRetry={() => void health.refetch()}
          />
          <p className="cost-notice">
            启动与状态查询不会调用模型；只有主动测试、生成或评审才会产生 API 用量。
          </p>
        </section>

        <ModelStatusCard />

        <CostSummaryCard projectId={activeProject?.project_id} />

        <ProjectWorkspaceCard
          projects={projects.data ?? []}
          activeProject={activeProject ?? null}
          activity={activity.data}
          onSelect={setActiveProjectId}
        />

        <ProjectActivityCard
          projectId={activeProject?.project_id}
          activity={activity.data}
        />

        <StylePackCard projectId={activeProject?.project_id} />

        <ProductionCard projectId={activeProject?.project_id} />

        <SequenceCard projectId={activeProject?.project_id} />

        <ConstraintCard projectId={activeProject?.project_id} />
      </div>
    </main>
  );
}
