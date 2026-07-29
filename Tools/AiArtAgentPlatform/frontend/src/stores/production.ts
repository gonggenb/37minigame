import { create } from "zustand";

import type { StaticAssetCategory } from "../api/production";

interface ProductionDraftState {
  category: StaticAssetCategory;
  assetId: string;
  name: string;
  brief: string;
  usage: string;
  candidateCount: number;
  referenceIds: string[];
  setCategory: (category: StaticAssetCategory) => void;
  setField: (
    field: "assetId" | "name" | "brief" | "usage",
    value: string,
  ) => void;
  setCandidateCount: (candidateCount: number) => void;
  setReferenceIds: (referenceIds: string[]) => void;
  reset: () => void;
}

const INITIAL_DRAFT = {
  category: "item",
  assetId: "",
  name: "",
  brief: "",
  usage: "gameplay",
  candidateCount: 4,
  referenceIds: [] as string[],
} satisfies Pick<
  ProductionDraftState,
  | "category"
  | "assetId"
  | "name"
  | "brief"
  | "usage"
  | "candidateCount"
  | "referenceIds"
>;

export const useProductionDraftStore = create<ProductionDraftState>((set) => ({
  ...INITIAL_DRAFT,
  setCategory: (category) => set({ category, referenceIds: [] }),
  setField: (field, value) => set({ [field]: value }),
  setCandidateCount: (candidateCount) => set({ candidateCount }),
  setReferenceIds: (referenceIds) => set({ referenceIds }),
  reset: () => set({ ...INITIAL_DRAFT, referenceIds: [] }),
}));
