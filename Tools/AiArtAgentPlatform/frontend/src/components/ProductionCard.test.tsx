import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { ProductionCard } from "./ProductionCard";

vi.mock("./CandidateEditor", () => ({
  CandidateEditor: () => <div data-testid="candidate-editor" />,
}));

afterEach(() => {
  vi.unstubAllGlobals();
});

function renderCard(projectId?: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <ProductionCard projectId={projectId} />
    </QueryClientProvider>,
  );
}

const task = {
  asset_id: "green-sword",
  category: "item",
  name: "青锋剑",
  brief: "Q 版水墨武侠青锋剑",
  usage: "world-sprite",
  style_pack: "wuxia-ink-chibi-topdown-2-5d",
  reference_ids: [],
  constraint_profile: "wuxia-item",
  constraint_overrides: {},
  candidate_count: 1,
  output_mode: "single-png",
};

const itemReference = {
  reference_id: "item-ref",
  source_relative_path: "物品/item-ref.png",
  workspace_relative_path: "style-pack/references/item-ref.png",
  thumbnail_relative_path: "style-pack/thumbnails/item-ref.png",
  sha256: "b".repeat(64),
  width: 256,
  height: 256,
  categories: ["item"],
  identities: [],
  usages: ["gameplay"],
  viewpoints: ["topdown-45"],
  materials: ["rice-paper"],
  notes: "",
};

const sceneReference = {
  ...itemReference,
  reference_id: "scene-ref",
  source_relative_path: "场景/scene-ref.png",
  workspace_relative_path: "style-pack/references/scene-ref.png",
  thumbnail_relative_path: "style-pack/thumbnails/scene-ref.png",
  categories: ["scene"],
};

const candidate = {
  candidate_id: "candidate-0",
  index: 0,
  raw_relative_path: "assets/item/green-sword/runs/run-1/raw/candidate-0.png",
  processed_relative_path:
    "assets/item/green-sword/runs/run-1/processed/candidate-0.png",
  metadata: {
    width: 128,
    height: 128,
    mode: "RGBA",
    source_alpha_bounds: [0, 0, 64, 64],
    alpha_bounds: [16, 16, 112, 112],
    scale: 1.5,
    sha256: "a".repeat(64),
    file_bytes: 256,
  },
  hard_constraints: { passed: true, checks: [] },
  revised_prompt: null,
  quality_report: null,
  comparison_relative_path: null,
};

function run(status: string, quality = false) {
  return {
    schema_version: 1,
    run_id: "run-1",
    project_id: "wuxia-demo",
    task,
    status,
    plan: {
      asset_type: "item",
      usage: "world-sprite",
      selected_reference_ids: [],
      composition: "主体居中",
      camera: "2.5D 俯视角",
      lighting: "左上柔光",
      identity_constraints: [],
      prompt: "模型生成的青锋剑提示词",
      negative_constraints: [],
      output_spec: {
        width: 1024,
        height: 1024,
        format: "png",
        transparent_required: true,
      },
      postprocess_steps: [],
      quality_checks: [],
      repair_strategy: ["定向修复"],
    },
    prompt: "模型生成的青锋剑提示词",
    candidates: status === "planned" ? [] : [
      quality
        ? {
            ...candidate,
            comparison_relative_path:
              "assets/item/green-sword/runs/run-1/reviews/candidate-0/comparison.png",
            quality_report: {
              hard_constraints: { passed: true, checks: [] },
              style_review: {
                score: 70,
                identity_score: 75,
                palette_score: 70,
                line_style_score: 68,
                composition_score: 72,
                issues: ["描边偏弱"],
                repair_instruction: "加强水墨描边",
                summary: "主体可辨认，但描边与参考有差异",
                strengths: ["轮廓清晰"],
                findings: [
                  {
                    dimension: "line_style",
                    severity: "error",
                    summary: "描边偏弱",
                    evidence: "候选外轮廓比参考图更细",
                    repair_hint: "加强墨色外轮廓",
                    actionable: true,
                  },
                ],
                risk_notes: ["缩略图下可能失去辨识度"],
              },
              animation_review: null,
              export_allowed: true,
              review_basis: ["候选处理图", "项目参考对比板"],
              decision: "retry",
            },
          }
        : candidate,
    ],
    selected_candidate_id: status === "generated" ? null : "candidate-0",
    source_run_id: null,
    source_candidate_id: null,
    edit_instruction: "",
    review_attempts: quality
      ? [
          {
            attempt_index: 0,
            run_id: "run-1",
            candidate_id: "candidate-0",
            comparison_relative_path: "reviews/candidate-0/comparison.png",
            quality_report: null,
            repair_plan: {
              action: "edit",
              reason: "描边失败维度可局部修复",
              target_dimensions: ["line_style"],
              prompt: "仅加强墨色外轮廓，保持其他区域不变",
              retry_allowed: true,
              stop_reason: null,
            },
            created_at: "2026-07-28T00:00:00Z",
          },
        ]
      : [],
    auto_repair_summary: quality
      ? {
          retry_count: 0,
          max_retries: 2,
          stop_reason: "no-actionable-failure",
          attempts: [],
        }
      : null,
    export: null,
    created_at: "2026-07-28T00:00:00Z",
    updated_at: "2026-07-28T00:00:00Z",
  };
}

it("waits for an active project before producing assets", () => {
  renderCard();

  expect(screen.getByText("创建项目后即可生产静态资产。"))
    .toBeInTheDocument();
});

it("runs the item candidate comparison, review and export workflow", async () => {
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (path.includes("/references?")) {
        return Promise.resolve(
          new Response(JSON.stringify([itemReference, sceneReference]), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/assets") && options?.method !== "POST") {
        return Promise.resolve(
          new Response(JSON.stringify([]), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/runs") && options?.method !== "POST") {
        return Promise.resolve(
          new Response(JSON.stringify([]), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/assets") && options?.method === "POST") {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              schema_version: 1,
              task,
              created_at: "2026-07-28T00:00:00Z",
              updated_at: "2026-07-28T00:00:00Z",
            }),
            { status: 201, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      if (path.endsWith("/plan")) {
        return Promise.resolve(
          new Response(JSON.stringify(run("planned")), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/generate")) {
        return Promise.resolve(
          new Response(JSON.stringify(run("generated")), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/select")) {
        return Promise.resolve(
          new Response(JSON.stringify(run("selected")), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/review")) {
        return Promise.resolve(
          new Response(JSON.stringify(run("reviewed", true)), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/export")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              export: {
                project_id: "wuxia-demo",
                asset_id: "green-sword",
                category: "item",
                variant: "default",
                filename: "green-sword_default.png",
                relative_path:
                  "assets/item/green-sword/exports/green-sword_default.png",
                sha256: "a".repeat(64),
                written_sha256: "a".repeat(64),
                file_bytes: 256,
                hard_constraints: { passed: true, checks: [] },
              },
              style_score: 70,
              minimum_style_score: 75,
              style_risk_accepted: true,
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

  await screen.findByLabelText("资产 ID");
  fireEvent.click(await screen.findByLabelText("选择 item-ref"));
  fireEvent.change(screen.getByLabelText("资产 ID"), {
    target: { value: "green-sword" },
  });
  fireEvent.change(screen.getByLabelText("资产名称"), {
    target: { value: "青锋剑" },
  });
  fireEvent.change(screen.getByLabelText("自然语言需求"), {
    target: { value: "Q 版水墨武侠青锋剑" },
  });
  fireEvent.click(screen.getByRole("button", { name: "保存资产任务" }));

  await waitFor(() => {
    const createCall = fetchMock.mock.calls.find(
      ([path, options]) =>
        String(path).endsWith("/assets") && options?.method === "POST",
    );
    expect(String(createCall?.[1]?.body)).toContain(
      '\"reference_ids\":[\"item-ref\"]',
    );
  });

  fireEvent.click(
    await screen.findByRole("button", { name: "生成结构化计划（调用模型）" }),
  );
  const prompt = await screen.findByLabelText("生成提示词");
  fireEvent.change(prompt, { target: { value: "人工修改的水墨青锋剑提示词" } });
  fireEvent.click(
    screen.getByRole("button", { name: "生成候选（调用模型）" }),
  );

  expect(await screen.findByAltText("候选 candidate-0")).toBeInTheDocument();
  fireEvent.click(
    screen.getByRole("button", { name: "选择 candidate-0" }),
  );
  fireEvent.click(
    await screen.findByRole("button", { name: "执行视觉评审（调用模型）" }),
  );

  expect(await screen.findByText("风格评分：70")).toBeInTheDocument();
  fireEvent.click(screen.getByLabelText("接受低分风格风险"));
  fireEvent.click(screen.getByRole("button", { name: "导出规范 PNG" }));

  expect(
    await screen.findByText(
      "已导出：assets/item/green-sword/exports/green-sword_default.png",
    ),
  ).toBeInTheDocument();
});

it("restores the latest saved run after a page refresh", async () => {
  const assetRecord = {
    schema_version: 1,
    task,
    created_at: "2026-07-28T00:00:00Z",
    updated_at: "2026-07-28T00:00:00Z",
  };
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL) => {
      const path = String(request);
      if (path.includes("/references?")) {
        return Promise.resolve(
          new Response(JSON.stringify([itemReference]), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      const payload = path.endsWith("/runs")
        ? [run("reviewed", true)]
        : [assetRecord];
      return Promise.resolve(
        new Response(JSON.stringify(payload), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }),
      );
    },
  );
  vi.stubGlobal("fetch", fetchMock);

  renderCard("wuxia-demo");

  expect(await screen.findByAltText("候选 candidate-0")).toBeInTheDocument();
  expect(screen.getByText("风格评分：70")).toBeInTheDocument();
  expect(screen.getByAltText("候选与项目参考对比图")).toBeInTheDocument();
  expect(screen.getByText("可见证据：候选外轮廓比参考图更细")).toBeInTheDocument();
  expect(screen.getByText("没有可定位的失败原因")).toBeInTheDocument();
});

it("switches between saved assets and their production runs", async () => {
  const sceneTask = {
    ...task,
    asset_id: "mist-valley",
    category: "scene",
    name: "云雾山谷",
    reference_ids: ["scene-ref"],
  };
  const itemRecord = {
    schema_version: 1,
    task,
    created_at: "2026-07-28T00:00:00Z",
    updated_at: "2026-07-28T00:00:00Z",
  };
  const sceneRecord = {
    schema_version: 1,
    task: sceneTask,
    created_at: "2026-07-28T00:00:00Z",
    updated_at: "2026-07-28T00:00:00Z",
  };
  const itemRunOne = run("reviewed", true);
  const itemRunTwo = {
    ...run("selected"),
    run_id: "run-2",
    candidates: [
      { ...candidate, candidate_id: "candidate-2", index: 2 },
    ],
    selected_candidate_id: "candidate-2",
    prompt: "第二次运行",
  };
  const sceneRun = {
    ...run("selected"),
    run_id: "run-scene",
    task: sceneTask,
    candidates: [
      { ...candidate, candidate_id: "scene-candidate", index: 0 },
    ],
    selected_candidate_id: "scene-candidate",
    prompt: "场景运行",
  };
  const fetchMock = vi.fn().mockImplementation((request: RequestInfo | URL) => {
    const path = String(request);
    let payload: unknown;
    if (path.includes("/references?")) {
      payload = path.includes("category=scene")
        ? [sceneReference]
        : [itemReference];
    } else if (path.endsWith("/assets")) {
      payload = [itemRecord, sceneRecord];
    } else if (path.includes("/assets/scene/mist-valley/runs")) {
      payload = [sceneRun];
    } else if (path.includes("/assets/item/green-sword/runs")) {
      payload = [itemRunOne, itemRunTwo];
    } else {
      throw new Error(`Unexpected request: ${path}`);
    }
    return Promise.resolve(
      new Response(JSON.stringify(payload), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );
  });
  vi.stubGlobal("fetch", fetchMock);

  renderCard("wuxia-demo");

  expect(await screen.findByText("青锋剑")).toBeInTheDocument();
  await screen.findByRole("option", { name: "run-2 · selected" });
  fireEvent.change(screen.getByLabelText("运行记录"), {
    target: { value: "run-2" },
  });
  expect(screen.getByLabelText("运行记录")).toHaveValue("run-2");
  expect(await screen.findByAltText("候选 candidate-2")).toBeInTheDocument();

  fireEvent.change(screen.getByLabelText("已有资产"), {
    target: { value: "scene:mist-valley" },
  });
  expect(await screen.findByText("云雾山谷")).toBeInTheDocument();
  expect(await screen.findByAltText("候选 scene-candidate")).toBeInTheDocument();
});
