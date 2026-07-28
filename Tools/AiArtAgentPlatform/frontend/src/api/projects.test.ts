import { afterEach, expect, it, vi } from "vitest";

import {
  createProject,
  fetchProjectActivity,
  fetchProjects,
  updateProject,
} from "./projects";
import type { ProjectConfig } from "../types/core";

afterEach(() => {
  vi.unstubAllGlobals();
});

const completeProject: ProjectConfig = {
  schema_version: 1,
  project_id: "wuxia-new",
  display_name: "新名称",
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

it("creates updates and reads project activity", async () => {
  const responses: unknown[] = [
    [completeProject],
    { ...completeProject, display_name: "新项目" },
    completeProject,
    {
      schema_version: 1,
      project_id: "wuxia-new",
      reference_count: 12,
      categories: [],
    },
  ];
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify(responses.shift()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await fetchProjects();
  await createProject({
    project_id: "wuxia-new",
    display_name: "新项目",
    visual_type: "wuxia-ink-chibi-topdown-2_5d",
    language: "zh-CN",
  });
  await updateProject("wuxia-new", completeProject);
  const activity = await fetchProjectActivity("wuxia-new");

  expect(activity.reference_count).toBe(12);
  expect(fetchMock.mock.calls[1][1]?.method).toBe("POST");
  expect(fetchMock.mock.calls[2][1]?.method).toBe("PUT");
  expect(fetchMock.mock.calls[3][0]).toBe(
    "/api/v1/projects/wuxia-new/activity",
  );
});
