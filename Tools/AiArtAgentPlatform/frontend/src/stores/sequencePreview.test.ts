import { afterEach, expect, it } from "vitest";

import { ACTION_TEMPLATES, useSequencePreviewStore } from "./sequencePreview";

afterEach(() => {
  window.localStorage.clear();
  useSequencePreviewStore.getState().reset();
});

it("uses a legal two-dimensional default action grid", () => {
  const state = useSequencePreviewStore.getState() as ReturnType<
    typeof useSequencePreviewStore.getState
  > & {
    generationFrameWidth: number;
    generationFrameHeight: number;
  };

  expect(ACTION_TEMPLATES.idle).toMatchObject({ frameCount: 4, rows: 2, columns: 2 });
  expect(state.rows).toBe(2);
  expect(state.columns).toBe(2);
  expect(state.generationFrameWidth).toBe(512);
  expect(state.generationFrameHeight).toBe(512);
  expect(state.frameWidth).toBe(256);
  expect(state.frameHeight).toBe(256);
});

it("updates the full grid when switching action templates", () => {
  useSequencePreviewStore.getState().setAction("move");
  const state = useSequencePreviewStore.getState();

  expect(state.frameCount).toBe(8);
  expect(state.rows).toBe(2);
  expect(state.columns).toBe(4);
  expect(state.previewFps).toBe(12);
  expect(state.loop).toBe(true);
});

it("restores legacy tasks by falling back to the final frame size", () => {
  useSequencePreviewStore.getState().restoreTask({
    category: "animation",
    asset_id: "legacy-idle",
    name: "旧版待机",
    action: "idle",
    frame_count: 4,
    rows: 1,
    columns: 4,
    frame_width: 128,
    frame_height: 96,
    preview_fps: 8,
    loop: true,
    base_frame_workspace_relative_path: "assets/hero/base.png",
    lock_first_frame: true,
  });
  const state = useSequencePreviewStore.getState() as ReturnType<
    typeof useSequencePreviewStore.getState
  > & {
    generationFrameWidth: number;
    generationFrameHeight: number;
  };

  expect(state.generationFrameWidth).toBe(128);
  expect(state.generationFrameHeight).toBe(96);
});
