import { create } from "zustand";
import { persist } from "zustand/middleware";

interface ProjectLike {
  project_id: string;
}

interface ProjectWorkspaceState {
  activeProjectId: string | null;
  setActiveProjectId: (projectId: string | null) => void;
}

export function resolveActiveProjectId(
  current: string | null,
  projects: ProjectLike[],
): string | null {
  if (current && projects.some((project) => project.project_id === current)) {
    return current;
  }
  return projects[0]?.project_id ?? null;
}

export const useProjectWorkspaceStore = create<ProjectWorkspaceState>()(
  persist(
    (set) => ({
      activeProjectId: null,
      setActiveProjectId: (activeProjectId) => set({ activeProjectId }),
    }),
    {
      name: "ai-art-project-workspace",
      partialize: (state) => ({ activeProjectId: state.activeProjectId }),
    },
  ),
);
