import { afterEach, expect, it } from "vitest";

import { useTaskNavigationStore } from "./taskNavigation";

afterEach(() => {
  useTaskNavigationStore.getState().clear();
});

it("stores and clears the requested production target", () => {
  const target = {
    projectId: "wuxia-demo",
    workflow: "static" as const,
    category: "item" as const,
    assetId: "green-sword",
    runId: "run-2",
  };

  useTaskNavigationStore.getState().requestOpen(target);
  expect(useTaskNavigationStore.getState().target).toEqual(target);

  useTaskNavigationStore.getState().clear();
  expect(useTaskNavigationStore.getState().target).toBeNull();
});
