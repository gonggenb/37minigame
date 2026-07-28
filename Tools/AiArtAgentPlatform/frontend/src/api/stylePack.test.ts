import { afterEach, expect, it, vi } from "vitest";

import {
  deleteReference,
  fetchReferenceSource,
  fetchReferences,
  fetchStyleGuide,
  importReference,
  previewPrompt,
  referenceThumbnailUrl,
  updateReference,
  updateStyleGuide,
} from "./stylePack";

afterEach(() => {
  vi.unstubAllGlobals();
});

it("loads the project style guide and reference index", async () => {
  const fetchMock = vi.fn().mockImplementation((request: RequestInfo | URL) => {
    const path = String(request);
    const payload = path.endsWith("/style-guide")
      ? {
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
        }
      : [
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
        ];
    return Promise.resolve(
      new Response(JSON.stringify(payload), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );
  });
  vi.stubGlobal("fetch", fetchMock);

  const [guide, references] = await Promise.all([
    fetchStyleGuide("wuxia-demo"),
    fetchReferences("wuxia-demo"),
  ]);

  expect(guide.reference_source.mode).toBe("read_only");
  expect(references[0].reference_id).toBe("hero-main");
});

it("imports a tagged read-only reference through the project API", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(
      JSON.stringify({
        reference_id: "hero-main",
        source_relative_path: "角色/hero.png",
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
      }),
      { status: 201, headers: { "Content-Type": "application/json" } },
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await importReference("wuxia-demo", {
    reference_id: "hero-main",
    source_relative_path: "角色/hero.png",
    categories: ["character"],
    identities: ["hero-main"],
    usages: ["gameplay"],
    viewpoints: ["topdown-45"],
    materials: [],
    notes: "",
  });

  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/projects/wuxia-demo/references",
    expect.objectContaining({
      method: "POST",
      body: expect.stringContaining('"source_relative_path":"角色/hero.png"'),
    }),
  );
});

it("sends manual prompt edits back as an explicit override", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(
      JSON.stringify({
        task: { asset_id: "hero-main" },
        selected_reference_ids: ["hero-main"],
        sections: [],
        prompt: "人工修订提示词",
        negative_constraints: [],
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await previewPrompt("wuxia-demo", {
    task: {
      asset_id: "hero-main",
      category: "character",
      name: "青衣少侠",
      brief: "俯视角游戏内基准帧",
      usage: "gameplay",
      style_pack: "wuxia-ink-chibi-topdown-2_5d",
      reference_ids: [],
      constraint_profile: "character-gameplay",
      constraint_overrides: {},
      candidate_count: 4,
      output_mode: "single-png",
    },
    identity: null,
    viewpoint: "topdown-45",
    composition: "底部中心锚点",
    lighting: "柔和左上主光",
    materials: [],
    output_spec: {
      width: 1024,
      height: 1024,
      format: "png",
      transparent_required: true,
    },
    additional_negative_constraints: [],
    prompt_override: "人工修订提示词",
  });

  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/projects/wuxia-demo/prompt-preview",
    expect.objectContaining({
      body: expect.stringContaining('"prompt_override":"人工修订提示词"'),
    }),
  );
});

it("updates the guide and manages filtered reference metadata", async () => {
  const styleGuide = {
    schema_version: 1 as const,
    style_id: "wuxia-ink-chibi-topdown-2_5d",
    display_name: "Q版水墨武侠俯视角",
    reference_source: { path: "D:/reference", mode: "read_only" as const },
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
  const reference = {
    reference_id: "hero-main",
    source_relative_path: "角色/hero.png",
    workspace_relative_path: "style-pack/references/hero-main.png",
    thumbnail_relative_path: "style-pack/thumbnails/hero-main.png",
    sha256: "a".repeat(64),
    width: 256,
    height: 256,
    categories: ["character" as const],
    identities: ["hero-main"],
    usages: ["gameplay"],
    viewpoints: ["topdown-45"],
    materials: ["rice-paper"],
    notes: "批准参考",
  };
  const responses: unknown[] = [
    styleGuide,
    [{ relative_path: "角色/hero.png", size_bytes: 1024 }],
    [reference],
    reference,
    null,
  ];
  const fetchMock = vi.fn().mockImplementation(() => {
    const payload = responses.shift();
    return Promise.resolve(
      payload === null
        ? new Response(null, { status: 204 })
        : new Response(JSON.stringify(payload), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
    );
  });
  vi.stubGlobal("fetch", fetchMock);

  await updateStyleGuide("wuxia-demo", styleGuide);
  await fetchReferenceSource("wuxia-demo", "hero", 100);
  await fetchReferences("wuxia-demo", {
    category: "character",
    material: "rice-paper",
    limit: 50,
  });
  await updateReference("wuxia-demo", "hero-main", {
    categories: ["character"],
    identities: ["hero-main"],
    usages: ["gameplay"],
    viewpoints: ["topdown-45"],
    materials: ["rice-paper"],
    notes: "批准参考",
  });
  await deleteReference("wuxia-demo", "hero-main");

  expect(fetchMock.mock.calls[0][1]?.method).toBe("PUT");
  expect(String(fetchMock.mock.calls[1][0])).toContain("query=hero");
  expect(String(fetchMock.mock.calls[2][0])).toContain("material=rice-paper");
  expect(fetchMock.mock.calls[3][1]?.method).toBe("PUT");
  expect(fetchMock.mock.calls[4][1]?.method).toBe("DELETE");
  expect(referenceThumbnailUrl("wuxia-demo", reference)).toBe(
    "/api/v1/projects/wuxia-demo/references/hero-main/thumbnail?v=" +
      reference.sha256,
  );
});
