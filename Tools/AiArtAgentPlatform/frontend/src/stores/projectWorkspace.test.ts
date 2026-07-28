import { beforeEach, expect, it } from "vitest";

import {
  resolveActiveProjectId,
  useProjectWorkspaceStore,
} from "./projectWorkspace";

beforeEach(() => {
  localStorage.clear();
  useProjectWorkspaceStore.setState({ activeProjectId: null });
});

it("keeps a valid project and falls back from a stale one", () => {
  const projects = [{ project_id: "alpha" }, { project_id: "beta" }];

  expect(resolveActiveProjectId("beta", projects)).toBe("beta");
  expect(resolveActiveProjectId("missing", projects)).toBe("alpha");
  expect(resolveActiveProjectId("alpha", [])).toBeNull();
});

it("persists the selected project id", () => {
  useProjectWorkspaceStore.getState().setActiveProjectId("beta");

  const stored = JSON.parse(
    localStorage.getItem("ai-art-project-workspace") ?? "{}",
  ) as { state?: { activeProjectId?: string } };
  expect(stored.state?.activeProjectId).toBe("beta");
});
