import { create } from "zustand";
import { persist } from "zustand/middleware";

import type { SequenceCategory } from "../api/sequences";
import { ACTION_TEMPLATES } from "../features/animation-preview/modelCanvas";

export { ACTION_TEMPLATES };

export type SequenceBackground = "checker" | "paper" | "ink";
export type SequenceAction = keyof typeof ACTION_TEMPLATES;

interface SequencePreviewState {
  category: SequenceCategory;
  assetId: string;
  name: string;
  action: SequenceAction;
  frameCount: number;
  rows: number;
  columns: number;
  generationFrameWidth: number;
  generationFrameHeight: number;
  frameWidth: number;
  frameHeight: number;
  previewFps: number;
  loop: boolean;
  baseFramePath: string;
  lockFirstFrame: boolean;
  candidateCount: number;
  background: SequenceBackground;
  currentFrame: number;
  setCategory: (category: SequenceCategory) => void;
  setTextField: (
    field: "assetId" | "name" | "baseFramePath",
    value: string,
  ) => void;
  setNumberField: (
    field:
      | "frameCount"
      | "rows"
      | "columns"
      | "generationFrameWidth"
      | "generationFrameHeight"
      | "frameWidth"
      | "frameHeight"
      | "previewFps"
      | "candidateCount",
    value: number,
  ) => void;
  setAction: (action: SequenceAction) => void;
  setLoop: (loop: boolean) => void;
  setLockFirstFrame: (lockFirstFrame: boolean) => void;
  setBackground: (background: SequenceBackground) => void;
  setCurrentFrame: (currentFrame: number) => void;
  restoreTask: (task: {
    category: SequenceCategory;
    asset_id: string;
    name: string;
    action: string;
    frame_count: number;
    rows: number;
    columns: number;
    generation_frame_width?: number | null;
    generation_frame_height?: number | null;
    frame_width: number;
    frame_height: number;
    preview_fps: number;
    loop: boolean;
    base_frame_workspace_relative_path: string | null;
    lock_first_frame: boolean;
  }) => void;
  reset: () => void;
}

const defaults = {
  category: "animation" as SequenceCategory,
  assetId: "",
  name: "",
  action: "idle" as SequenceAction,
  frameCount: ACTION_TEMPLATES.idle.frameCount,
  rows: ACTION_TEMPLATES.idle.rows,
  columns: ACTION_TEMPLATES.idle.columns,
  generationFrameWidth: 512,
  generationFrameHeight: 512,
  frameWidth: 256,
  frameHeight: 256,
  previewFps: 8,
  loop: true,
  baseFramePath: "",
  lockFirstFrame: true,
  candidateCount: 1,
  background: "checker" as SequenceBackground,
  currentFrame: 0,
};

export const useSequencePreviewStore = create<SequencePreviewState>()(
  persist(
    (set) => ({
      ...defaults,
      setCategory: (category) =>
        set({
          category,
          lockFirstFrame: category === "animation",
          loop: category === "animation",
        }),
      setTextField: (field, value) => set({ [field]: value }),
      setNumberField: (field, value) => set({ [field]: value }),
      setAction: (action) => {
        const template = ACTION_TEMPLATES[action];
        set({
          action,
          frameCount: template.frameCount,
          rows: template.rows,
          columns: template.columns,
          previewFps: template.previewFps,
          loop: template.loop,
          currentFrame: 0,
        });
      },
      setLoop: (loop) => set({ loop }),
      setLockFirstFrame: (lockFirstFrame) => set({ lockFirstFrame }),
      setBackground: (background) => set({ background }),
      setCurrentFrame: (currentFrame) => set({ currentFrame }),
      restoreTask: (task) =>
        set({
          category: task.category,
          assetId: task.asset_id,
          name: task.name,
          action: task.action in ACTION_TEMPLATES
            ? (task.action as SequenceAction)
            : "idle",
          frameCount: task.frame_count,
          rows: task.rows,
          columns: task.columns,
          generationFrameWidth:
            task.generation_frame_width ?? task.frame_width,
          generationFrameHeight:
            task.generation_frame_height ?? task.frame_height,
          frameWidth: task.frame_width,
          frameHeight: task.frame_height,
          previewFps: task.preview_fps,
          loop: task.loop,
          baseFramePath: task.base_frame_workspace_relative_path ?? "",
          lockFirstFrame: task.lock_first_frame,
          currentFrame: 0,
        }),
      reset: () => set(defaults),
    }),
    {
      name: "ai-art-sequence-preview",
      partialize: (state) => ({
        category: state.category,
        assetId: state.assetId,
        name: state.name,
        action: state.action,
        frameCount: state.frameCount,
        rows: state.rows,
        columns: state.columns,
        generationFrameWidth: state.generationFrameWidth,
        generationFrameHeight: state.generationFrameHeight,
        frameWidth: state.frameWidth,
        frameHeight: state.frameHeight,
        previewFps: state.previewFps,
        loop: state.loop,
        baseFramePath: state.baseFramePath,
        lockFirstFrame: state.lockFirstFrame,
        candidateCount: state.candidateCount,
        background: state.background,
        currentFrame: state.currentFrame,
      }),
    },
  ),
);
