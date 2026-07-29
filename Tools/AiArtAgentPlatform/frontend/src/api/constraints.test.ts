import { afterEach, expect, it, vi } from "vitest";

import {
  fetchConstraints,
  processConstraintPreview,
  saveConstraint,
} from "./constraints";

afterEach(() => {
  vi.unstubAllGlobals();
});

const profile = {
  schema_version: 1 as const,
  profile_id: "wuxia-item",
  category: "item" as const,
  master_width: 1024,
  master_height: 1024,
  output_width: 128,
  output_height: 128,
  require_rgba: true,
  require_transparency: true,
  crop_mode: "alpha_bounds" as const,
  padding_ratio: 0.125,
  occupancy_ratio: 0.75,
  resize_algorithm: "lanczos" as const,
  pivot_x: 0.5,
  pivot_y: 0.5,
  filename_template: "{asset_id}_{variant}.png",
  max_file_bytes: 8388608,
  output_sprite_sheet: false,
  frame_count: null,
  rows: null,
  columns: null,
  frame_width: null,
  frame_height: null,
  preview_fps: null,
  loop: null,
  baseline: null,
  shared_scale: true,
  lock_first_frame: false,
  max_center_drift_px: null,
  max_size_drift_ratio: null,
};

it("loads and saves category constraint profiles", async () => {
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) =>
      Promise.resolve(
        new Response(
          JSON.stringify(options?.method === "PUT" ? profile : { item: profile }),
          { status: 200, headers: { "Content-Type": "application/json" } },
        ),
      ),
  );
  vi.stubGlobal("fetch", fetchMock);

  const loaded = await fetchConstraints("wuxia-demo");
  await saveConstraint("wuxia-demo", "item", profile);

  expect(loaded.item.output_width).toBe(128);
  expect(fetchMock).toHaveBeenLastCalledWith(
    "/api/v1/projects/wuxia-demo/constraints/item",
    expect.objectContaining({
      method: "PUT",
      body: expect.stringContaining('"output_width":128'),
    }),
  );
});

it("requests an offline processed preview with explicit background settings", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(
      JSON.stringify({
        processed_png_base64: "iVBORw0KGgo=",
        metadata: {
          width: 128,
          height: 128,
          mode: "RGBA",
          source_alpha_bounds: [0, 0, 64, 64],
          alpha_bounds: [16, 16, 112, 112],
          scale: 1.5,
          sha256: "a".repeat(64),
          file_bytes: 128,
        },
        hard_constraints: { passed: true, checks: [] },
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await processConstraintPreview("wuxia-demo", "item", {
    workspace_relative_path: "style-pack/references/sword.png",
    asset_id: "sword-001",
    variant: "default",
    background: {
      mode: "corner_flood",
      color_tolerance: 18,
      alpha_low_threshold: 8,
      alpha_high_threshold: 247,
    },
  });

  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/projects/wuxia-demo/constraints/item/process-preview",
    expect.objectContaining({ method: "POST" }),
  );
});
