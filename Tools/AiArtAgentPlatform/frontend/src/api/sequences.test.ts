import { afterEach, expect, it, vi } from "vitest";

import {
  createSequence,
  exportSequence,
  fetchSequenceRun,
  fetchSequenceRuns,
  generateSequence,
  reprocessSequence,
  selectSequenceCandidate,
  sequenceArtifactUrl,
} from "./sequences";

afterEach(() => {
  vi.unstubAllGlobals();
});

const task = {
  schema_version: 1 as const,
  asset_id: "hero-idle",
  category: "animation" as const,
  name: "少侠待机",
  action: "idle",
  frame_count: 4,
  rows: 1,
  columns: 4,
  generation_frame_width: 512,
  generation_frame_height: 512,
  frame_width: 256,
  frame_height: 256,
  preview_fps: 8,
  loop: true,
  baseline: "bottom_center" as const,
  base_frame_workspace_relative_path: "assets/hero/base.png",
  lock_first_frame: true,
  pivot_x: 0.5,
  pivot_y: 1,
  blend_mode_hint: "alpha" as const,
};

it("creates, generates, reprocesses and lists sequence runs", async () => {
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify({}), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await createSequence("wuxia-demo", task);
  await generateSequence(
    "wuxia-demo",
    "animation",
    "hero-idle",
    "run-1",
    { candidate_count: 1, prompt_override: "人工修改待机提示词" },
  );
  await reprocessSequence(
    "wuxia-demo",
    "animation",
    "hero-idle",
    "run-1",
  );
  await fetchSequenceRuns("wuxia-demo", "animation", "hero-idle");
  await fetchSequenceRun(
    "wuxia-demo",
    "animation",
    "hero-idle",
    "run-1",
  );

  expect(fetchMock).toHaveBeenNthCalledWith(
    1,
    "/api/v1/projects/wuxia-demo/sequences",
    expect.objectContaining({
      method: "POST",
      body: expect.stringContaining('"generation_frame_width":512'),
    }),
  );
  expect(fetchMock).toHaveBeenNthCalledWith(
    2,
    "/api/v1/projects/wuxia-demo/sequences/animation/hero-idle/runs/run-1/generate",
    expect.objectContaining({
      method: "POST",
      body: expect.stringContaining('"prompt_override":"人工修改待机提示词"'),
    }),
  );
  expect(fetchMock).toHaveBeenNthCalledWith(
    3,
    "/api/v1/projects/wuxia-demo/sequences/animation/hero-idle/runs/run-1/reprocess",
    expect.objectContaining({ method: "POST" }),
  );
  expect(fetchMock).toHaveBeenNthCalledWith(
    4,
    "/api/v1/projects/wuxia-demo/sequences/animation/hero-idle/runs",
    expect.objectContaining({ headers: { Accept: "application/json" } }),
  );
  expect(fetchMock).toHaveBeenLastCalledWith(
    "/api/v1/projects/wuxia-demo/sequences/animation/hero-idle/runs/run-1",
    expect.objectContaining({ headers: { Accept: "application/json" } }),
  );
});

it("selects, exports and builds artifact URLs without downloading binary data", async () => {
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify({}), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await selectSequenceCandidate(
    "wuxia-demo",
    "animation",
    "hero-idle",
    "run-1",
    "candidate-0",
  );
  await exportSequence(
    "wuxia-demo",
    "animation",
    "hero-idle",
    "run-1",
  );

  expect(fetchMock).toHaveBeenLastCalledWith(
    "/api/v1/projects/wuxia-demo/sequences/animation/hero-idle/runs/run-1/export",
    expect.objectContaining({ method: "POST" }),
  );
  expect(
    sequenceArtifactUrl(
      "wuxia-demo",
      "animation",
      "hero-idle",
      "run-1",
      "candidate-0",
      "frame",
      3,
    ),
  ).toBe(
    "/api/v1/projects/wuxia-demo/sequences/animation/hero-idle/runs/run-1/candidates/candidate-0/frames/3",
  );
  expect(
    sequenceArtifactUrl(
      "wuxia-demo",
      "animation",
      "hero-idle",
      "run-1",
      "candidate-0",
      "sprite-sheet",
    ),
  ).toContain("/candidates/candidate-0/sprite-sheet");
});
