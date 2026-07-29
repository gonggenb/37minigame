import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import type { ProjectActivitySummary } from "../types/core";
import { useTaskNavigationStore } from "../stores/taskNavigation";
import { ProjectActivityCard } from "./ProjectActivityCard";

const categories: ProjectActivitySummary["categories"] = [
  {
    category: "character",
    task_count: 0,
    recent: [],
  },
  {
    category: "scene",
    task_count: 0,
    recent: [],
  },
  {
    category: "item",
    task_count: 1,
    recent: [
      {
        workflow: "static",
        category: "item",
        asset_id: "green-sword",
        name: "青锋剑",
        status: "reviewed",
        run_id: "run-item",
        updated_at: "2026-07-28T00:00:00Z",
      },
    ],
  },
  {
    category: "animation",
    task_count: 0,
    recent: [],
  },
  {
    category: "effect",
    task_count: 1,
    recent: [
      {
        workflow: "sequence",
        category: "effect",
        asset_id: "sword-flash",
        name: "剑光",
        status: "processed",
        run_id: "run-effect",
        updated_at: "2026-07-28T00:00:00Z",
      },
    ],
  },
  {
    category: "ui",
    task_count: 0,
    recent: [],
  },
];

afterEach(() => {
  useTaskNavigationStore.getState().clear();
  vi.restoreAllMocks();
});

it("keeps all six categories visible and opens static or sequence activity", () => {
  const scrollIntoView = vi.fn();
  Object.defineProperty(HTMLElement.prototype, "scrollIntoView", {
    configurable: true,
    value: scrollIntoView,
  });
  render(
    <>
      <div id="static-production" />
      <div id="sequence-production" />
      <ProjectActivityCard
        projectId="wuxia-demo"
        activity={{
          schema_version: 1,
          project_id: "wuxia-demo",
          reference_count: 4,
          categories,
        }}
      />
    </>,
  );

  for (const label of ["角色", "场景", "物品", "动画", "特效", "UI"]) {
    expect(screen.getByRole("heading", { name: label })).toBeInTheDocument();
  }

  fireEvent.click(screen.getByRole("button", { name: "打开 青锋剑" }));
  expect(useTaskNavigationStore.getState().target).toEqual({
    projectId: "wuxia-demo",
    workflow: "static",
    category: "item",
    assetId: "green-sword",
    runId: "run-item",
  });

  fireEvent.click(screen.getByRole("button", { name: "打开 剑光" }));
  expect(useTaskNavigationStore.getState().target).toEqual({
    projectId: "wuxia-demo",
    workflow: "sequence",
    category: "effect",
    assetId: "sword-flash",
    runId: "run-effect",
  });
  expect(scrollIntoView).toHaveBeenCalledTimes(2);
});
