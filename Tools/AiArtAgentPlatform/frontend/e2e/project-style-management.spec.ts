import { expect, test } from "@playwright/test";

const PNG = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+X2NDWQAAAABJRU5ErkJggg==",
  "base64",
);

const styleGuide = {
  schema_version: 1,
  style_id: "wuxia-ink-chibi-topdown-2_5d",
  display_name: "Q版水墨武侠俯视角",
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

function project(projectId: string, displayName: string) {
  return {
    schema_version: 1,
    project_id: projectId,
    display_name: displayName,
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
}

function reference(referenceId: string, index: number) {
  return {
    reference_id: referenceId,
    source_relative_path: `items/ref-${index}.png`,
    workspace_relative_path: `style-pack/references/${referenceId}.png`,
    thumbnail_relative_path: `style-pack/thumbnails/${referenceId}.png`,
    sha256: String(index).repeat(64),
    width: 256,
    height: 256,
    categories: ["item"],
    identities: [],
    usages: ["gameplay"],
    viewpoints: ["topdown-45"],
    materials: [],
    notes: "",
  };
}

function sequenceRun() {
  const task = {
    schema_version: 1,
    asset_id: "sword-flash",
    category: "effect",
    name: "剑光",
    action: "effect",
    frame_count: 4,
    rows: 2,
    columns: 2,
    generation_frame_width: 512,
    generation_frame_height: 512,
    frame_width: 256,
    frame_height: 256,
    preview_fps: 8,
    loop: false,
    baseline: "center",
    base_frame_workspace_relative_path: null,
    lock_first_frame: false,
    pivot_x: 0.5,
    pivot_y: 0.5,
    blend_mode_hint: "additive",
  };
  return {
    schema_version: 1,
    run_id: "run-effect",
    project_id: "project-b",
    task,
    status: "reference_ready",
    prompt: "Q版水墨剑光特效",
    reference_grid_relative_path: null,
    candidates: [],
    selected_candidate_id: null,
    created_at: "2026-07-28T00:00:00Z",
    updated_at: "2026-07-28T00:00:00Z",
  };
}

test("completes project and style management offline without model operations", async ({
  page,
}) => {
  test.setTimeout(90_000);
  let projects: ReturnType<typeof project>[] = [];
  let references: ReturnType<typeof reference>[] = [];
  let assets: Array<{
    schema_version: 1;
    task: Record<string, unknown>;
    created_at: string;
    updated_at: string;
  }> = [];
  let currentStyleGuide = structuredClone(styleGuide);
  let createdAssetBody: Record<string, unknown> | null = null;
  const requestPaths: string[] = [];

  const activity = () => ({
    schema_version: 1,
    project_id: "project-b",
    reference_count: references.length,
    categories: [
      { category: "character", task_count: 0, recent: [] },
      { category: "scene", task_count: 0, recent: [] },
      {
        category: "item",
        task_count: assets.length,
        recent: assets.map((record) => ({
          workflow: "static" as const,
          category: "item" as const,
          asset_id: String(record.task.asset_id),
          name: String(record.task.name),
          status: "draft",
          run_id: null,
          updated_at: record.updated_at,
        })),
      },
      { category: "animation", task_count: 0, recent: [] },
      {
        category: "effect",
        task_count: 1,
        recent: [
          {
            workflow: "sequence" as const,
            category: "effect" as const,
            asset_id: "sword-flash",
            name: "剑光",
            status: "reference_ready",
            run_id: "run-effect",
            updated_at: "2026-07-28T00:00:00Z",
          },
        ],
      },
      { category: "ui", task_count: 0, recent: [] },
    ],
  });

  await page.route("**/api/v1/**", async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    requestPaths.push(path);

    if (path.endsWith("/thumbnail")) {
      await route.fulfill({ status: 200, contentType: "image/png", body: PNG });
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
    } else if (path === "/api/v1/projects" && method === "GET") {
      payload = projects;
    } else if (path === "/api/v1/projects" && method === "POST") {
      const body = request.postDataJSON() as { project_id: string; display_name: string };
      const created = project(body.project_id, body.display_name);
      projects = [...projects, created];
      payload = created;
      status = 201;
    } else if (path.match(/\/projects\/[^/]+$/) && method === "PUT") {
      const projectId = path.split("/").at(-1) ?? "";
      const body = request.postDataJSON() as ReturnType<typeof project>;
      projects = projects.map((item) => item.project_id === projectId ? body : item);
      payload = body;
    } else if (path.endsWith("/activity")) {
      payload = activity();
    } else if (path.endsWith("/style-guide") && method === "GET") {
      payload = currentStyleGuide;
    } else if (path.endsWith("/style-guide") && method === "PUT") {
      currentStyleGuide = request.postDataJSON() as typeof styleGuide;
      payload = currentStyleGuide;
    } else if (path.endsWith("/reference-source")) {
      payload = [1, 2, 3, 4].map((index) => ({
        relative_path: `items/ref-${index}.png`,
        size_bytes: 1024,
      }));
    } else if (path.endsWith("/references") && method === "GET") {
      payload = references;
    } else if (path.endsWith("/references") && method === "POST") {
      const body = request.postDataJSON() as { reference_id: string; source_relative_path: string };
      const index = Number(body.reference_id.replace("ref-", ""));
      const created = reference(body.reference_id, index);
      references = [...references.filter((item) => item.reference_id !== created.reference_id), created];
      payload = created;
      status = 201;
    } else if (path.match(/\/references\/[^/]+$/) && method === "PUT") {
      const referenceId = path.split("/").at(-1) ?? "";
      const body = request.postDataJSON() as Record<string, unknown>;
      references = references.map((item) => item.reference_id === referenceId ? { ...item, ...body } : item);
      payload = references.find((item) => item.reference_id === referenceId);
    } else if (path.endsWith("/assets") && method === "GET") {
      payload = assets;
    } else if (path.endsWith("/assets") && method === "POST") {
      createdAssetBody = request.postDataJSON() as Record<string, unknown>;
      const task = createdAssetBody;
      const record = {
        schema_version: 1 as const,
        task,
        created_at: "2026-07-28T00:00:00Z",
        updated_at: "2026-07-28T00:00:00Z",
      };
      assets = [...assets, record];
      payload = record;
      status = 201;
    } else if (path.includes("/assets/") && path.endsWith("/runs")) {
      payload = [];
    } else if (path.endsWith("/sequences/effect/sword-flash/runs")) {
      payload = [sequenceRun()];
    } else if (path.endsWith("/sequences/effect/sword-flash/runs/run-effect")) {
      payload = sequenceRun();
    } else if (path.endsWith("/costs")) {
      payload = {
        project_id: "project-b",
        request_count: 0,
        known_cost_usd: 0,
        unknown_cost_count: 0,
        invalid_record_count: 0,
        by_model: [],
        by_category: [],
        latest_at: null,
      };
    } else if (path.endsWith("/constraints")) {
      payload = {};
    }

    await route.fulfill({
      status,
      contentType: "application/json",
      body: JSON.stringify(payload),
    });
  });

  await page.goto("/");
  await expect(page.getByLabel("当前项目")).toHaveValue("");

  await page.getByLabel("新项目 ID", { exact: true }).fill("project-a");
  await page.getByLabel("新项目名称", { exact: true }).fill("项目甲");
  await page.getByRole("button", { name: "创建并切换" }).click();
  await expect(page.getByLabel("当前项目")).toHaveValue("project-a");

  await page.getByLabel("新项目 ID", { exact: true }).fill("project-b");
  await page.getByLabel("新项目名称", { exact: true }).fill("项目乙");
  await page.getByRole("button", { name: "创建并切换" }).click();
  await expect(page.getByLabel("当前项目")).toHaveValue("project-b");

  await page.reload();
  await expect(page.getByLabel("当前项目")).toHaveValue("project-b");

  await page.getByLabel("项目名称", { exact: true }).fill("项目乙·武侠美术");
  await page.getByRole("button", { name: "保存项目配置" }).click();
  await expect(page.getByText("项目配置已保存。")).toBeVisible();

  await page.getByLabel("风格名称").fill("Q版水墨武侠俯视角·项目版");
  await page.getByRole("button", { name: "保存风格圣经" }).click();
  await expect(page.getByText("风格圣经")).toBeVisible();

  await page.getByLabel("搜索素材源").fill("ref");
  await page.getByRole("button", { name: "搜索" }).click();
  await expect(page.getByRole("button", { name: "选择 items/ref-1.png" })).toBeVisible();

  for (let index = 1; index <= 4; index += 1) {
    await page.getByRole("button", { name: `选择 items/ref-${index}.png` }).click();
    await page.getByLabel("参考 ID", { exact: true }).fill(`ref-${index}`);
    await page.locator(".source-browser__import select").selectOption("item");
    await page.getByRole("button", { name: "复制到项目参考库" }).click();
    await expect.poll(() => references.length).toBe(index);
  }

  await page.getByRole("button", { name: "编辑 ref-1" }).click();
  const editor = page.locator(".reference-editor");
  await editor.getByLabel("材质标签").fill("rice-paper");
  await editor.getByRole("button", { name: "保存标签" }).click();
  await expect(page.getByText("rice-paper")).toBeVisible();

  const staticProduction = page.locator("#static-production");
  for (let index = 1; index <= 4; index += 1) {
    await staticProduction.getByLabel(`选择 ref-${index}`).check();
  }
  await staticProduction.getByLabel("资产 ID", { exact: true }).fill("green-sword");
  await staticProduction.getByLabel("资产名称", { exact: true }).fill("青锋剑");
  await staticProduction.getByLabel("自然语言需求", { exact: true }).fill("Q版水墨武侠青锋剑");
  await staticProduction.getByRole("button", { name: "保存资产任务" }).click();
  await expect.poll(() => createdAssetBody?.reference_ids).toHaveLength(4);

  await page.reload();
  await expect(page.getByRole("button", { name: "打开 青锋剑" })).toBeVisible();
  await page.getByRole("button", { name: "打开 青锋剑" }).click();
  await expect(page.locator(".production-stage").getByText("青锋剑")).toBeVisible();

  await page.getByRole("button", { name: "打开 剑光" }).click();
  const sequenceProduction = page.locator("#sequence-production");
  await expect(sequenceProduction.getByText("剑光", { exact: true })).toBeVisible();
  await expect(sequenceProduction.getByText(/run-effect · reference_ready/)).toBeVisible();

  expect(
    requestPaths.some((path) => /\/(plan|generate|review|edit)(?:\/|$)/.test(path)),
  ).toBe(false);
});
