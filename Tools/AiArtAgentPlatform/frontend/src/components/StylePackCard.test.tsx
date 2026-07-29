import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { StylePackCard } from "./StylePackCard";

afterEach(() => {
  vi.unstubAllGlobals();
});

function renderCard(projectId?: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <StylePackCard projectId={projectId} />
    </QueryClientProvider>,
  );
}

it("shows a project-first guide when no workspace is active", () => {
  renderCard();

  expect(screen.getByText("创建项目后即可管理武侠风格包。"))
    .toBeInTheDocument();
});

it("shows the read-only style pack and preserves manual prompt edits", async () => {
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (path.endsWith("/style-guide")) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
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
            }),
            { status: 200, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      if (path.includes("/reference-source?")) {
        return Promise.resolve(
          new Response("[]", {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/references") && options?.method !== "POST") {
        return Promise.resolve(
          new Response(
            JSON.stringify([
              {
                reference_id: "hero-main",
                source_relative_path: "hero.png",
                workspace_relative_path: "style-pack/references/hero-main.png",
                thumbnail_relative_path: "style-pack/thumbnails/hero-main.png",
                sha256: "a".repeat(64),
                width: 256,
                height: 256,
                categories: ["character"],
                identities: ["hero-main"],
                usages: ["gameplay"],
                viewpoints: ["topdown-45"],
                materials: [],
                notes: "",
              },
            ]),
            { status: 200, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      if (path.endsWith("/prompt-preview")) {
        const body = JSON.parse(String(options?.body)) as { prompt_override?: string | null };
        return Promise.resolve(
          new Response(
            JSON.stringify({
              task: { asset_id: "hero-main" },
              selected_reference_ids: ["hero-main"],
              sections: [
                { key: "project_style", label: "项目风格", content: "水墨武侠" },
              ],
              prompt: body.prompt_override || "## 项目风格\n水墨武侠",
              negative_constraints: ["pixel_art"],
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

  expect(await screen.findByText("Q版水墨武侠俯视角")).toBeInTheDocument();
  expect(screen.getByText("只读来源")).toBeInTheDocument();
  expect(screen.getByLabelText("搜索素材源")).toBeInTheDocument();
  expect(screen.getByText(/源目录只读/)).toBeInTheDocument();
  expect(screen.getByText("已索引 1 张参考图")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /修改源文件/ })).not.toBeInTheDocument();

  fireEvent.change(screen.getByLabelText("资产需求"), {
    target: { value: "俯视角青衣少侠游戏内基准帧" },
  });
  fireEvent.click(screen.getByRole("button", { name: "编译提示词预览" }));
  const prompt = await screen.findByLabelText("提示词预览与人工修改");
  expect(prompt).toHaveValue("## 项目风格\n水墨武侠");

  fireEvent.change(prompt, { target: { value: "人工修订提示词" } });
  fireEvent.click(screen.getByRole("button", { name: "编译提示词预览" }));

  await vi.waitFor(() => {
    const previewCalls = fetchMock.mock.calls.filter(([path]) =>
      String(path).endsWith("/prompt-preview"),
    );
    expect(previewCalls).toHaveLength(2);
    expect(String(previewCalls[1][1]?.body)).toContain(
      '"prompt_override":"人工修订提示词"',
    );
  });
});

it("updates the complete style guide from the editor", async () => {
  const guide = {
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
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (path.endsWith("/style-guide")) {
        return Promise.resolve(
          new Response(
            JSON.stringify(
              options?.method === "PUT"
                ? JSON.parse(String(options.body))
                : guide,
            ),
            { status: 200, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      if (path.includes("/reference-source?")) {
        return Promise.resolve(
          new Response("[]", {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      if (path.endsWith("/references")) {
        return Promise.resolve(
          new Response("[]", {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
        );
      }
      throw new Error(`Unexpected request: ${path}`);
    },
  );
  vi.stubGlobal("fetch", fetchMock);
  renderCard("wuxia-demo");

  fireEvent.change(await screen.findByLabelText("风格名称"), {
    target: { value: "项目专属水墨风格" },
  });
  fireEvent.click(screen.getByRole("button", { name: "保存风格圣经" }));

  await waitFor(() => {
    const updateCall = fetchMock.mock.calls.find(
      ([path, options]) =>
        String(path).endsWith("/style-guide") && options?.method === "PUT",
    );
    expect(updateCall).toBeDefined();
    expect(String(updateCall?.[1]?.body)).toContain(
      '\"display_name\":\"项目专属水墨风格\"',
    );
  });
});
