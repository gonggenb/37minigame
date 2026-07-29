import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { useProjectWorkspaceStore } from "../stores/projectWorkspace";
import { App } from "./App";

afterEach(() => {
  vi.unstubAllGlobals();
  window.localStorage.clear();
  useProjectWorkspaceStore.setState({ activeProjectId: null });
});

it("shows the workbench, local service, and model configuration state", async () => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockImplementation((request: RequestInfo | URL) => {
      const path = String(request);
      const payload = path.endsWith("/api/v1/health")
        ? {
            status: "ok",
            service: "ai-art-agent-platform",
            schema_version: 1,
          }
        : path.endsWith("/api/v1/models/status")
          ? {
              api_key_configured: false,
              review_model: "gpt-5.6",
              image_model: "gpt-image-2",
              timeout_seconds: 120,
              max_retries: 2,
            }
          : [];

      return Promise.resolve(
        new Response(JSON.stringify(payload), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );
    }),
  );
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>,
  );

  expect(
    screen.getByRole("heading", { name: "2D 小游戏 AI 美术生产工作台" }),
  ).toBeInTheDocument();
  expect(screen.getByText("Q 版水墨武侠俯视角")).toBeInTheDocument();
  expect(
    await screen.findByText("本地服务已连接"),
  ).toBeInTheDocument();
  expect(
    screen.getByText(/只有主动测试、生成或评审才会产生 API 用量/),
  ).toBeInTheDocument();
  expect(await screen.findByText("OpenAI API Key 未配置")).toBeInTheDocument();
  expect(screen.getByRole("option", { name: "尚无项目" })).toBeInTheDocument();
});

const projectA = {
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

const projectB = { ...projectA, project_id: "project-b", display_name: "项目乙" };

const styleGuide = {
  schema_version: 1,
  style_id: "wuxia-ink-chibi-topdown-2_5d",
  display_name: "Q 版水墨武侠俯视角",
  reference_source: { path: "D:/reference", mode: "read_only" },
  camera: {
    projection: "orthographic_like",
    pitch_semantic_min: 35,
    pitch_semantic_max: 55,
    shared_view_required: true,
    default_facing: "right",
  },
  palette: { base: ["rice_paper"], accents: ["vermilion"] },
  rendering: {
    character_proportion: "chibi_wuxia",
    character_outline: "clean_ink",
    environment_detail: "restrained",
    surface_finish: "matte_painted_2d",
    shadow_direction: "lower_right",
  },
  readability: {
    protect_playfield: true,
    character_contrast_above_environment: true,
    preserve_clear_silhouette: true,
    avoid_high_frequency_ground_noise: true,
  },
  ui: { formal_text_baked_in: false, border_language: ["ink_edge"] },
  forbidden: ["pixel_art"],
};

function activity(projectId: string) {
  return {
    schema_version: 1,
    project_id: projectId,
    reference_count: 0,
    categories: ["character", "scene", "item", "animation", "effect", "ui"].map(
      (category) => ({ category, task_count: 0, recent: [] }),
    ),
  };
}

function renderProjectApp() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>,
  );
}

it("uses the persisted project for all project-scoped requests", async () => {
  window.localStorage.setItem(
    "ai-art-project-workspace",
    JSON.stringify({ state: { activeProjectId: "project-b" }, version: 0 }),
  );
  await useProjectWorkspaceStore.persist.rehydrate();
  const fetchMock = vi.fn().mockImplementation((request: RequestInfo | URL) => {
    const path = String(request);
    let payload: unknown = [];
    if (path.endsWith("/api/v1/health")) {
      payload = { status: "ok", service: "ai-art-agent-platform", schema_version: 1 };
    } else if (path.endsWith("/api/v1/models/status")) {
      payload = {
        api_key_configured: false,
        review_model: "gpt-5.6",
        image_model: "gpt-image-2",
        timeout_seconds: 120,
        max_retries: 2,
      };
    } else if (path.endsWith("/api/v1/projects")) {
      payload = [projectA, projectB];
    } else if (path.includes("/activity")) {
      payload = activity(path.includes("project-b") ? "project-b" : "project-a");
    } else if (path.includes("/style-guide")) {
      payload = styleGuide;
    } else if (path.includes("/costs")) {
      payload = {
        project_id: "project-b",
        known_cost_usd: 0,
        unknown_cost_count: 0,
        request_count: 0,
        by_model: [],
      };
    }
    return Promise.resolve(
      new Response(JSON.stringify(payload), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );
  });
  vi.stubGlobal("fetch", fetchMock);

  renderProjectApp();

  await waitFor(() =>
    expect(screen.getByLabelText("当前项目")).toHaveValue("project-b"),
  );
  await waitFor(() =>
    expect(
      fetchMock.mock.calls.some(([path]) =>
        String(path).includes("/projects/project-b/activity"),
      ),
    ).toBe(true),
  );

  fireEvent.change(screen.getByLabelText("当前项目"), {
    target: { value: "project-a" },
  });
  await waitFor(() =>
    expect(
      fetchMock.mock.calls.some(([path]) =>
        String(path).includes("/projects/project-a/activity"),
      ),
    ).toBe(true),
  );
  expect(
    fetchMock.mock.calls.some(([path]) =>
      String(path).includes("/projects/project-a/style-guide"),
    ),
  ).toBe(true);
  expect(
    fetchMock.mock.calls.some(([path]) =>
      String(path).includes("/projects/project-a/references"),
    ),
  ).toBe(true);
  expect(
    fetchMock.mock.calls.some(([path]) =>
      String(path).includes("/projects/project-a/assets"),
    ),
  ).toBe(true);
});

it("falls back to the first project when the persisted id is missing", async () => {
  window.localStorage.setItem(
    "ai-art-project-workspace",
    JSON.stringify({ state: { activeProjectId: "missing" }, version: 0 }),
  );
  await useProjectWorkspaceStore.persist.rehydrate();
  vi.stubGlobal(
    "fetch",
    vi.fn().mockImplementation((request: RequestInfo | URL) => {
      const path = String(request);
      const payload = path.endsWith("/api/v1/projects")
        ? [projectA, projectB]
        : path.endsWith("/api/v1/health")
          ? { status: "ok", service: "ai-art-agent-platform", schema_version: 1 }
          : path.endsWith("/api/v1/models/status")
            ? {
                api_key_configured: false,
                review_model: "gpt-5.6",
                image_model: "gpt-image-2",
                timeout_seconds: 120,
                max_retries: 2,
              }
            : path.includes("/style-guide")
              ? styleGuide
              : path.includes("/activity")
                ? activity("project-a")
              : path.includes("/costs")
                ? {
                    project_id: "project-a",
                    known_cost_usd: 0,
                    unknown_cost_count: 0,
                    request_count: 0,
                    by_model: [],
                  }
                : [];
      return Promise.resolve(
        new Response(JSON.stringify(payload), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );
    }),
  );

  renderProjectApp();

  await waitFor(() =>
    expect(screen.getByLabelText("当前项目")).toHaveValue("project-a"),
  );
});
