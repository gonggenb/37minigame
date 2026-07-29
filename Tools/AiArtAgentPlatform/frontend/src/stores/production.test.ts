import { beforeEach, expect, it } from "vitest";

import { useProductionDraftStore } from "./production";

beforeEach(() => {
  useProductionDraftStore.getState().reset();
});

it("tracks task references and clears them when the category changes", () => {
  useProductionDraftStore.getState().setReferenceIds(["item-ref"]);
  expect(useProductionDraftStore.getState().referenceIds).toEqual(["item-ref"]);

  useProductionDraftStore.getState().setCategory("scene");
  expect(useProductionDraftStore.getState().referenceIds).toEqual([]);
});

it("resets all draft fields for a project switch", () => {
  const draft = useProductionDraftStore.getState();
  draft.setField("assetId", "green-sword");
  draft.setField("name", "青锋剑");
  draft.setReferenceIds(["item-ref"]);
  draft.reset();

  expect(useProductionDraftStore.getState()).toMatchObject({
    category: "item",
    assetId: "",
    name: "",
    brief: "",
    usage: "gameplay",
    candidateCount: 4,
    referenceIds: [],
  });
});
