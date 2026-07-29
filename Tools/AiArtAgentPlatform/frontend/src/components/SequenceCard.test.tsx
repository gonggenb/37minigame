import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { useSequencePreviewStore } from "../stores/sequencePreview";
import { useTaskNavigationStore } from "../stores/taskNavigation";
import { SequenceCard } from "./SequenceCard";

afterEach(() => {
  vi.unstubAllGlobals();
  window.localStorage.clear();
  useSequencePreviewStore.getState().reset();
  useTaskNavigationStore.getState().clear();
});

function renderCard(projectId?: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <SequenceCard projectId={projectId} />
    </QueryClientProvider>,
  );
}

const task = {
  schema_version: 1,
  asset_id: "hero-idle",
  category: "animation",
  name: "少侠待机",
  action: "idle",
  frame_count: 4,
  rows: 2,
  columns: 2,
  generation_frame_width: 512,
  generation_frame_height: 512,
  frame_width: 256,
  frame_height: 256,
  preview_fps: 8,
  loop: true,
  baseline: "bottom_center",
  base_frame_workspace_relative_path: "assets/hero/base.png",
  lock_first_frame: true,
  pivot_x: 0.5,
  pivot_y: 1,
  blend_mode_hint: "alpha",
};

const driftReport = {
  passed: true,
  max_center_drift_px: 1,
  max_size_drift_ratio: 0.04,
  max_baseline_drift_px: 1,
  max_area_drift_ratio: 0.05,
  max_color_drift: 12,
  max_brightness_jump: 8,
  first_last_difference: 4,
  overflow_frames: [],
  failed_frames: [],
  issues: [],
  blend_mode_hint: "alpha",
};

const frames = Array.from({ length: 4 }, (_, index) => ({
  index,
  relative_path: `assets/animation/hero-idle/frame-${index}.png`,
  alpha_bounds: [48 + index % 2, 32, 208 + index % 2, 240],
  center_x: 128 + index * 0.25,
  center_y: 136,
  subject_width: 160,
  subject_height: 208,
  baseline_y: 240,
  area_ratio: 0.4,
  mean_rgb: [80, 100, 60],
  brightness: 90,
}));

const output = {
  frame_count: 4,
  rows: 2,
  columns: 2,
  frame_width: 256,
  frame_height: 256,
  sprite_sheet_width: 512,
  sprite_sheet_height: 512,
  frame_relative_paths: frames.map((frame) => frame.relative_path),
  sprite_sheet_relative_path: "assets/animation/hero-idle/sprite-sheet.png",
  gif_relative_path: "assets/animation/hero-idle/preview.gif",
  webp_relative_path: "assets/animation/hero-idle/preview.webp",
  drift_report_relative_path: "assets/animation/hero-idle/drift-report.json",
  content_sha256: "a".repeat(64),
  frames,
  drift_report: driftReport,
};

function run(status = "processed", selected = false) {
  return {
    schema_version: 1,
    run_id: "run-1",
    project_id: "wuxia-demo",
    task,
    status,
    prompt: "统一 Q 版水墨武侠待机序列",
    reference_grid_relative_path: "assets/animation/hero-idle/reference-grid.png",
    candidates: status === "reference_ready"
      ? []
      : [
          {
            candidate_id: "candidate-0",
            index: 0,
            raw_strip_relative_path: "assets/animation/hero-idle/raw-strip.png",
            output,
          },
        ],
    selected_candidate_id: selected ? "candidate-0" : null,
    created_at: "2026-07-28T00:00:00Z",
    updated_at: "2026-07-28T00:00:00Z",
  };
}

it("waits for an active project before creating sequences", () => {
  renderCard();

  expect(screen.getByText("创建项目后即可生产动画与特效序列。"))
    .toBeInTheDocument();
});

it("creates an animation, previews drift and exposes server-side outputs", async () => {
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (path.endsWith("/runs") && options?.method !== "POST") {
        return Promise.resolve(
          new Response(JSON.stringify([]), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/sequences") && options?.method === "POST") {
        return Promise.resolve(
          new Response(JSON.stringify(run("reference_ready")), {
            status: 201,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/generate")) {
        return Promise.resolve(
          new Response(JSON.stringify(run()), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/select")) {
        return Promise.resolve(
          new Response(JSON.stringify(run("processed", true)), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/reprocess")) {
        return Promise.resolve(
          new Response(JSON.stringify(run("processed", true)), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/export")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              project_id: "wuxia-demo",
              asset_id: "hero-idle",
              category: "animation",
              files: Array.from({ length: 8 }, (_, index) => ({
                kind: index < 4 ? "frame" : "sprite_sheet",
                filename: `output-${index}.png`,
                relative_path: `assets/animation/hero-idle/exports/output-${index}.png`,
                sha256: "b".repeat(64),
                file_bytes: 128,
              })),
              drift_report: driftReport,
            }),
            { status: 200, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      throw new Error(`Unexpected request: ${path}`);
    },
  );
  vi.stubGlobal("fetch", fetchMock);
  renderCard("wuxia-demo");

  fireEvent.change(screen.getByLabelText("序列资产 ID"), {
    target: { value: "hero-idle" },
  });
  fireEvent.change(screen.getByLabelText("序列名称"), {
    target: { value: "少侠待机" },
  });
  fireEvent.change(screen.getByLabelText("角色基准帧路径"), {
    target: { value: "assets/hero/base.png" },
  });
  fireEvent.click(screen.getByRole("button", { name: "创建参考网格" }));

  const prompt = await screen.findByLabelText("序列生成提示词");
  const createCall = fetchMock.mock.calls.find(([path, options]) =>
    String(path).endsWith("/sequences") && options?.method === "POST"
  );
  expect(JSON.parse(String(createCall?.[1]?.body))).toMatchObject({
    rows: 2,
    columns: 2,
    generation_frame_width: 512,
    generation_frame_height: 512,
    frame_width: 256,
    frame_height: 256,
  });
  expect(screen.getByText("模型请求画布：1024 × 1024")).toBeInTheDocument();
  expect(screen.getByText("最终 Sprite Sheet：512 × 512")).toBeInTheDocument();
  fireEvent.change(prompt, { target: { value: "人工调整后的待机序列提示词" } });
  fireEvent.click(screen.getByRole("button", { name: "生成完整序列（调用模型）" }));

  expect(await screen.findByAltText("序列帧 1 / 4")).toBeInTheDocument();
  expect(screen.getByText("当前帧：1 / 4")).toBeInTheDocument();
  expect(screen.getByText("脚底基线：240 px")).toBeInTheDocument();
  expect(screen.getByLabelText("中心漂移曲线")).toBeInTheDocument();
  expect(screen.getByLabelText("尺寸漂移曲线")).toBeInTheDocument();
  expect(screen.getByLabelText("基线漂移曲线")).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("预览背景"), {
    target: { value: "ink" },
  });
  expect(screen.getByTestId("sequence-preview")).toHaveClass(
    "sequence-preview--ink",
  );

  fireEvent.click(screen.getByRole("button", { name: "选择 candidate-0" }));
  fireEvent.click(screen.getByRole("button", { name: "重新离线处理" }));
  fireEvent.click(screen.getByRole("button", { name: "无覆盖导出全部文件" }));
  expect(await screen.findByText("已导出 8 个序列文件。"))
    .toBeInTheDocument();
  expect(screen.getByRole("link", { name: "下载 Sprite Sheet" }))
    .toHaveAttribute("href", expect.stringContaining("sprite-sheet"));
  expect(screen.getByRole("link", { name: "下载 GIF" })).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "下载 WebP" })).toBeInTheDocument();
});

it("restores the latest sequence run after refresh", async () => {
  useSequencePreviewStore.setState({
    assetId: "hero-idle",
    category: "animation",
  });
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify([run("processed", true)]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  renderCard("wuxia-demo");

  expect(await screen.findByAltText("序列帧 1 / 4")).toBeInTheDocument();
  expect(screen.getByText("漂移检查通过")).toBeInTheDocument();
});

it("opens a requested sequence run from project activity", async () => {
  const effectRun = {
    ...run("processed", true),
    run_id: "run-effect",
    task: {
      ...task,
      asset_id: "sword-flash",
      category: "effect",
      name: "剑光",
      action: "effect",
      baseline: "center",
      base_frame_workspace_relative_path: null,
      lock_first_frame: false,
      pivot_y: 0.5,
      blend_mode_hint: "additive",
    },
    prompt: "Q 版水墨剑光特效",
  };
  useTaskNavigationStore.getState().requestOpen({
    projectId: "wuxia-demo",
    workflow: "sequence",
    category: "effect",
    assetId: "sword-flash",
    runId: "run-effect",
  });
  const fetchMock = vi.fn().mockImplementation((request: RequestInfo | URL) => {
    const path = String(request);
    if (path.endsWith("/runs/run-effect")) {
      return Promise.resolve(
        new Response(JSON.stringify(effectRun), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );
    }
    if (path.endsWith("/runs")) {
      return Promise.resolve(
        new Response(JSON.stringify([effectRun]), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );
    }
    throw new Error(`Unexpected request: ${path}`);
  });
  vi.stubGlobal("fetch", fetchMock);

  renderCard("wuxia-demo");

  expect(await screen.findByText("剑光")).toBeInTheDocument();
  expect(screen.getByText(/run-effect · processed/)).toBeInTheDocument();
  expect(useSequencePreviewStore.getState().assetId).toBe("sword-flash");
  expect(useTaskNavigationStore.getState().target).toBeNull();
});

it("blocks an invalid paid canvas but keeps offline reprocessing available", async () => {
  useSequencePreviewStore.setState({
    assetId: "hero-idle",
    category: "animation",
  });
  const invalidRun = {
    ...run("processed", true),
    task: {
      ...task,
      generation_frame_width: 256,
      generation_frame_height: 256,
    },
  };
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      new Response(JSON.stringify([invalidRun]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );

  renderCard("wuxia-demo");

  expect(await screen.findByText(/总像素不得低于 655,360/)).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "生成完整序列（调用模型）" }))
    .toBeDisabled();
  expect(screen.getByRole("button", { name: "重新离线处理" }))
    .toBeEnabled();
});

it("shows the provider error message returned by the local API", async () => {
  useSequencePreviewStore.setState({
    assetId: "hero-idle",
    category: "animation",
  });
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (path.endsWith("/runs") && options?.method !== "POST") {
        return Promise.resolve(
          new Response(JSON.stringify([run("reference_ready")]), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/generate")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              detail: {
                code: "bad_request",
                message: "invalid gpt-image-2 canvas 1024x1024: rejected for test",
                retryable: false,
              },
            }),
            { status: 400, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      throw new Error(`Unexpected request: ${path}`);
    },
  );
  vi.stubGlobal("fetch", fetchMock);
  renderCard("wuxia-demo");

  const generate = await screen.findByRole("button", {
    name: "生成完整序列（调用模型）",
  });
  fireEvent.click(generate);

  expect(await screen.findByText(/invalid gpt-image-2 canvas 1024x1024/))
    .toBeInTheDocument();
});
