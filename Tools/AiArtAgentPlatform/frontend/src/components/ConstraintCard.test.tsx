import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { ConstraintCard } from "./ConstraintCard";

afterEach(() => {
  vi.unstubAllGlobals();
});

function renderCard(projectId?: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <ConstraintCard projectId={projectId} />
    </QueryClientProvider>,
  );
}

it("waits for an active project before editing constraints", () => {
  renderCard();

  expect(screen.getByText("创建项目后即可配置资产约束。"))
    .toBeInTheDocument();
});

it("edits a profile and shows processed hard checks", async () => {
  const profile = {
    schema_version: 1,
    profile_id: "wuxia-item",
    category: "item",
    master_width: 1024,
    master_height: 1024,
    output_width: 128,
    output_height: 128,
    require_rgba: true,
    require_transparency: true,
    crop_mode: "alpha_bounds",
    padding_ratio: 0.125,
    occupancy_ratio: 0.75,
    resize_algorithm: "lanczos",
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
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (path.endsWith("/constraints") && options?.method !== "PUT") {
        return Promise.resolve(
          new Response(JSON.stringify({ item: profile }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (options?.method === "PUT") {
        return Promise.resolve(
          new Response(String(options.body), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/process-preview")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              processed_png_base64:
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+7xL7WQAAAABJRU5ErkJggg==",
              metadata: {
                width: 192,
                height: 192,
                mode: "RGBA",
                source_alpha_bounds: [0, 0, 64, 64],
                alpha_bounds: [24, 24, 168, 168],
                scale: 2,
                sha256: "a".repeat(64),
                file_bytes: 128,
              },
              hard_constraints: {
                passed: true,
                checks: [
                  { name: "dimensions", passed: true, message: "192x192" },
                ],
              },
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

  const width = await screen.findByLabelText("输出宽度");
  fireEvent.change(width, { target: { value: "192" } });
  fireEvent.click(screen.getByRole("button", { name: "保存约束配置" }));
  await vi.waitFor(() => {
    expect(fetchMock.mock.calls.some(([, options]) => options?.method === "PUT"))
      .toBe(true);
  });

  fireEvent.change(screen.getByLabelText("工作区图片路径"), {
    target: { value: "style-pack/references/sword.png" },
  });
  fireEvent.click(screen.getByRole("button", { name: "处理并预览" }));

  expect(await screen.findByAltText("约束处理预览")).toBeInTheDocument();
  expect(screen.getByText("dimensions：通过")).toBeInTheDocument();
});
