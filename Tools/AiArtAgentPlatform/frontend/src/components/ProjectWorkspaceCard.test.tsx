import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { ProjectWorkspaceCard } from "./ProjectWorkspaceCard";
import type {
  ProjectActivitySummary,
  ProjectConfig,
} from "../types/core";

afterEach(() => {
  vi.unstubAllGlobals();
});

const projectA: ProjectConfig = {
  schema_version: 1,
  project_id: "project-a",
  display_name: "项目甲",
  visual_type: "wuxia-ink-chibi-topdown-2_5d",
  language: "zh-CN",
  models: {
    planner_model: "gpt-5.6",
    review_model: "gpt-5.6",
    image_model: "gpt-image-2",
  },
  generation: {
    candidate_count: 4,
    automatic_retry_count: 2,
    image_quality: "high",
    transparency_mode: "postprocess",
  },
  review: {
    enabled: true,
    minimum_style_score: 75,
    hard_constraints_required: true,
  },
};

const projectB: ProjectConfig = {
  ...projectA,
  project_id: "project-b",
  display_name: "项目乙",
};

const activity: ProjectActivitySummary = {
  schema_version: 1,
  project_id: "project-a",
  reference_count: 12,
  categories: [
    "character",
    "scene",
    "item",
    "animation",
    "effect",
    "ui",
  ].map((category, index) => ({
    category: category as ProjectActivitySummary["categories"][number]["category"],
    task_count: index,
    recent: [],
  })),
};

function renderCard(onSelect = vi.fn()) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={client}>
      <ProjectWorkspaceCard
        projects={[projectA, projectB]}
        activeProject={projectA}
        activity={activity}
        onSelect={onSelect}
      />
    </QueryClientProvider>,
  );
  return onSelect;
}

it("switches projects and creates a new workspace", async () => {
  const created = {
    ...projectA,
    project_id: "wuxia-new",
    display_name: "新武侠项目",
  };
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(JSON.stringify(created), {
      status: 201,
      headers: { "Content-Type": "application/json" },
    }),
  );
  vi.stubGlobal("fetch", fetchMock);
  const onSelect = renderCard();

  fireEvent.change(screen.getByLabelText("当前项目"), {
    target: { value: "project-b" },
  });
  expect(onSelect).toHaveBeenCalledWith("project-b");

  fireEvent.change(screen.getByLabelText("新项目 ID"), {
    target: { value: "wuxia-new" },
  });
  fireEvent.change(screen.getByLabelText("新项目名称"), {
    target: { value: "新武侠项目" },
  });
  fireEvent.click(screen.getByRole("button", { name: "创建并切换" }));

  await waitFor(() => expect(onSelect).toHaveBeenCalledWith("wuxia-new"));
  expect(String(fetchMock.mock.calls[0][1]?.body)).toContain(
    '"visual_type"',
  );
  expect(screen.getByText("参考图 12 张")).toBeInTheDocument();
});

it("edits safe project settings while keeping identity fields locked", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(JSON.stringify({ ...projectA, display_name: "项目甲新版" }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }),
  );
  vi.stubGlobal("fetch", fetchMock);
  renderCard();

  expect(screen.getByLabelText("项目 ID")).toBeDisabled();
  expect(screen.getByLabelText("视觉预设")).toBeDisabled();
  fireEvent.change(screen.getByLabelText("项目名称"), {
    target: { value: "项目甲新版" },
  });
  fireEvent.change(screen.getByLabelText("候选数量"), {
    target: { value: "3" },
  });
  fireEvent.change(screen.getByLabelText("最低风格分"), {
    target: { value: "80" },
  });
  fireEvent.click(screen.getByRole("button", { name: "保存项目配置" }));

  await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
  const body = String(fetchMock.mock.calls[0][1]?.body);
  expect(body).toContain('"project_id":"project-a"');
  expect(body).toContain('"candidate_count":3');
  expect(body).toContain('"minimum_style_score":80');
});
