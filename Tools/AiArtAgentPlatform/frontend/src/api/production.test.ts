import { afterEach, expect, it, vi } from "vitest";

import {
  createStaticAsset,
  editProductionCandidate,
  exportProductionRun,
  fetchProductionRuns,
  generateProductionCandidates,
  planStaticAsset,
  reviewAndRepairProductionCandidate,
  reviewProductionCandidate,
  saveProductionMask,
  selectProductionCandidate,
  transformProductionCandidate,
} from "./production";

afterEach(() => {
  vi.unstubAllGlobals();
});

const task = {
  asset_id: "green-sword",
  category: "item" as const,
  name: "青锋剑",
  brief: "Q 版水墨武侠青锋剑",
  usage: "world-sprite",
  style_pack: "wuxia-ink-chibi-topdown-2-5d",
  reference_ids: [],
  constraint_profile: "wuxia-item",
  constraint_overrides: {},
  candidate_count: 4,
  output_mode: "single-png",
};

it("creates, plans and generates a static asset with a prompt override", async () => {
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify({}), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await createStaticAsset("wuxia-demo", task);
  await planStaticAsset("wuxia-demo", "item", "green-sword");
  await generateProductionCandidates(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    { candidate_count: 4, prompt_override: "人工修改提示词" },
  );
  await fetchProductionRuns("wuxia-demo", "item", "green-sword");

  expect(fetchMock).toHaveBeenNthCalledWith(
    1,
    "/api/v1/projects/wuxia-demo/assets",
    expect.objectContaining({ method: "POST" }),
  );
  expect(fetchMock).toHaveBeenNthCalledWith(
    2,
    "/api/v1/projects/wuxia-demo/assets/item/green-sword/plan",
    expect.objectContaining({ method: "POST" }),
  );
  expect(fetchMock).toHaveBeenNthCalledWith(
    3,
    "/api/v1/projects/wuxia-demo/assets/item/green-sword/runs/run-1/generate",
    expect.objectContaining({
      method: "POST",
      body: expect.stringContaining('"prompt_override":"人工修改提示词"'),
    }),
  );
  expect(fetchMock).toHaveBeenLastCalledWith(
    "/api/v1/projects/wuxia-demo/assets/item/green-sword/runs",
    expect.objectContaining({ headers: { Accept: "application/json" } }),
  );
});

it("selects, edits, reviews and exports a production candidate", async () => {
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify({}), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await selectProductionCandidate(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    "candidate-0",
  );
  await editProductionCandidate(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    {
      candidate_id: "candidate-0",
      instruction: "只修改剑穗颜色",
      candidate_count: 1,
      mask_workspace_relative_path: null,
    },
  );
  await reviewProductionCandidate(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    "candidate-0",
  );
  await reviewAndRepairProductionCandidate(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    { candidate_id: "candidate-0", automatic_repair: true, max_retries: 2 },
  );
  await transformProductionCandidate(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    {
      candidate_id: "candidate-0",
      crop: null,
      output_width: 128,
      output_height: 128,
      padding_ratio: 0.125,
      remove_background: true,
    },
  );
  await saveProductionMask(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    "candidate-0",
    "cG5n",
  );
  await exportProductionRun(
    "wuxia-demo",
    "item",
    "green-sword",
    "run-1",
    { variant: "default", accept_style_risk: true },
  );

  expect(fetchMock).toHaveBeenNthCalledWith(
    4,
    "/api/v1/projects/wuxia-demo/assets/item/green-sword/runs/run-1/review-and-repair",
    expect.objectContaining({
      method: "POST",
      body: expect.stringContaining('"max_retries":2'),
    }),
  );
  expect(fetchMock).toHaveBeenLastCalledWith(
    "/api/v1/projects/wuxia-demo/assets/item/green-sword/runs/run-1/export",
    expect.objectContaining({
      method: "POST",
      body: expect.stringContaining('"accept_style_risk":true'),
    }),
  );
});
