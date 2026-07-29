import { expect, test } from "@playwright/test";

const PNG = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+X2NDWQAAAABJRU5ErkJggg==",
  "base64",
);

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

function run() {
  return {
    schema_version: 1,
    run_id: "run-1",
    project_id: "wuxia-demo",
    task,
    status: "reviewed",
    plan: {
      asset_type: "item",
      usage: "world-sprite",
      selected_reference_ids: [],
      composition: "主体居中",
      camera: "2.5D 俯视角",
      lighting: "左上柔光",
      identity_constraints: ["朱红剑穗"],
      prompt: "Q 版水墨青锋剑",
      negative_constraints: ["霓虹"],
      output_spec: {
        width: 1024,
        height: 1024,
        format: "png",
        transparent_required: true,
      },
      postprocess_steps: [],
      quality_checks: [],
      repair_strategy: ["只修复失败维度"],
    },
    prompt: "Q 版水墨青锋剑",
    candidates: [
      {
        candidate_id: "candidate-0",
        index: 0,
        raw_relative_path: "raw/candidate-0.png",
        processed_relative_path: "processed/candidate-0.png",
        metadata: {
          width: 128,
          height: 128,
          mode: "RGBA",
          source_alpha_bounds: [0, 0, 128, 128],
          alpha_bounds: [16, 16, 112, 112],
          scale: 1,
          sha256: "a".repeat(64),
          file_bytes: 256,
        },
        hard_constraints: { passed: true, checks: [] },
        revised_prompt: null,
        comparison_relative_path: "reviews/candidate-0/comparison.png",
        quality_report: {
          hard_constraints: { passed: true, checks: [] },
          style_review: {
            score: 68,
            identity_score: 72,
            palette_score: 65,
            line_style_score: 68,
            composition_score: 74,
            issues: ["配色偏紫"],
            repair_instruction: "恢复青绿主体与朱红点缀",
            summary: "候选结构可用，但配色偏离参考",
            strengths: ["轮廓清晰"],
            findings: [
              {
                dimension: "palette",
                severity: "error",
                summary: "高饱和紫色偏离项目配色",
                evidence: "护手出现参考图中没有的霓虹紫",
                repair_hint: "恢复青绿主体和朱红点缀",
                actionable: true,
              },
            ],
            risk_notes: ["与项目 UI 配色不一致"],
          },
          animation_review: null,
          export_allowed: true,
          review_basis: ["候选处理图", "项目参考对比板"],
          decision: "retry",
        },
      },
    ],
    selected_candidate_id: "candidate-0",
    source_run_id: null,
    source_candidate_id: null,
    edit_instruction: "",
    review_attempts: [
      {
        attempt_index: 0,
        run_id: "run-1",
        candidate_id: "candidate-0",
        comparison_relative_path: "reviews/candidate-0/comparison.png",
        quality_report: null,
        repair_plan: {
          action: "edit",
          reason: "配色失败维度可局部修复",
          target_dimensions: ["palette"],
          prompt: "仅恢复青绿主体和朱红点缀，保持其他区域不变",
          retry_allowed: true,
          stop_reason: null,
        },
        created_at: "2026-07-28T12:00:00Z",
      },
    ],
    auto_repair_summary: null,
    export: null,
    created_at: "2026-07-28T12:00:00Z",
    updated_at: "2026-07-28T12:00:00Z",
  };
}

test("offline workbench restores review evidence and applies deterministic edit", async ({
  page,
}) => {
  let transformCalled = false;
  await page.route("**/api/v1/**", async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    if (path.endsWith("/image") || path.endsWith("/comparison")) {
      await route.fulfill({ status: 200, contentType: "image/png", body: PNG });
      return;
    }
    if (method === "POST" && path.endsWith("/transform")) {
      transformCalled = true;
      await route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(run()) });
      return;
    }
    let payload: unknown = [];
    let status = 200;
    if (path === "/api/v1/health") {
      payload = { status: "ok", service: "ai-art-agent-platform", schema_version: 1 };
    } else if (path === "/api/v1/models/status") {
      payload = {
        api_key_configured: false,
        review_model: "gpt-5.6",
        image_model: "gpt-image-2",
        timeout_seconds: 120,
        max_retries: 2,
      };
    } else if (path === "/api/v1/projects") {
      payload = [
        {
          schema_version: 1,
          project_id: "wuxia-demo",
          display_name: "武侠美术",
          visual_type: "wuxia-ink-chibi-topdown-2_5d",
        },
      ];
    } else if (path.endsWith("/jobs") || path.endsWith("/references")) {
      payload = [];
    } else if (path.endsWith("/activity")) {
      payload = {
        schema_version: 1,
        project_id: "wuxia-demo",
        reference_count: 0,
        categories: ["character", "scene", "item", "animation", "effect", "ui"].map(
          (category) => ({ category, task_count: 0, recent: [] }),
        ),
      };
    } else if (path.endsWith("/costs")) {
      payload = {
        project_id: "wuxia-demo",
        request_count: 0,
        known_cost_usd: 0,
        unknown_cost_count: 0,
        invalid_record_count: 0,
        by_model: [],
        by_category: [],
        latest_at: null,
      };
    } else if (path.endsWith("/assets")) {
      payload = [
        {
          schema_version: 1,
          task,
          created_at: "2026-07-28T12:00:00Z",
          updated_at: "2026-07-28T12:00:00Z",
        },
      ];
    } else if (path.endsWith("/runs")) {
      payload = [run()];
    } else if (path.endsWith("/style-guide") || path.endsWith("/constraints")) {
      payload = { detail: "offline e2e fixture" };
      status = 503;
    }
    await route.fulfill({
      status,
      contentType: "application/json",
      body: JSON.stringify(payload),
    });
  });

  await page.goto("/");

  await expect(page.getByText("OpenAI API Key 未配置")).toBeVisible();
  await expect(page.getByAltText("候选 candidate-0")).toBeVisible();
  await expect(page.getByText("风格评分：68")).toBeVisible();
  await expect(page.getByText("可见证据：护手出现参考图中没有的霓虹紫")).toBeVisible();
  await expect(
    page.getByRole("button", { name: "评审并自动定向修复（最多 2 次，调用模型）" }),
  ).toBeVisible();

  await page.reload();
  await expect(page.getByText("风格评分：68")).toBeVisible();

  await page.getByLabel("输出宽度", { exact: true }).fill("192");
  await page.getByRole("button", { name: "应用本地编辑" }).click();
  await expect.poll(() => transformCalled).toBe(true);
});
