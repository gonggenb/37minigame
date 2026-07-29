import { create } from "zustand";

import type { AssetCategory } from "../types/core";

export interface TaskNavigationTarget {
  projectId: string;
  workflow: "static" | "sequence";
  category: AssetCategory;
  assetId: string;
  runId: string | null;
}

interface TaskNavigationState {
  target: TaskNavigationTarget | null;
  requestOpen: (target: TaskNavigationTarget) => void;
  clear: () => void;
}

export const useTaskNavigationStore = create<TaskNavigationState>((set) => ({
  target: null,
  requestOpen: (target) => set({ target }),
  clear: () => set({ target: null }),
}));
