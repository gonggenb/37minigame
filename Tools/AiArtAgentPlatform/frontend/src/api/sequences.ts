import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { getJson, postJson } from "./client";

export type SequenceCategory = "animation" | "effect";
export type SequenceBaseline = "bottom_center" | "center" | "custom";
export type SequenceBlendMode = "alpha" | "additive";

export interface SequenceTask {
  schema_version: 1;
  asset_id: string;
  category: SequenceCategory;
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
  baseline: SequenceBaseline;
  base_frame_workspace_relative_path: string | null;
  lock_first_frame: boolean;
  pivot_x: number;
  pivot_y: number;
  blend_mode_hint: SequenceBlendMode;
}

export interface SequenceFrameRecord {
  index: number;
  relative_path: string;
  alpha_bounds: [number, number, number, number];
  center_x: number;
  center_y: number;
  subject_width: number;
  subject_height: number;
  baseline_y: number;
  area_ratio: number;
  mean_rgb: [number, number, number];
  brightness: number;
}

export interface SequenceDriftReport {
  passed: boolean;
  max_center_drift_px: number;
  max_size_drift_ratio: number;
  max_baseline_drift_px: number;
  max_area_drift_ratio: number;
  max_color_drift: number;
  max_brightness_jump: number;
  first_last_difference: number;
  overflow_frames: number[];
  failed_frames: number[];
  issues: string[];
  blend_mode_hint: SequenceBlendMode;
}

export interface SequenceOutput {
  frame_count: number;
  rows: number;
  columns: number;
  frame_width: number;
  frame_height: number;
  sprite_sheet_width: number;
  sprite_sheet_height: number;
  frame_relative_paths: string[];
  sprite_sheet_relative_path: string;
  gif_relative_path: string;
  webp_relative_path: string;
  drift_report_relative_path: string;
  content_sha256: string;
  frames: SequenceFrameRecord[];
  drift_report: SequenceDriftReport | null;
}

export interface SequenceCandidate {
  candidate_id: string;
  index: number;
  raw_strip_relative_path: string;
  output: SequenceOutput | null;
}

export interface SequenceRun {
  schema_version: 1;
  run_id: string;
  project_id: string;
  task: SequenceTask;
  status:
    | "draft"
    | "reference_ready"
    | "generated"
    | "processed"
    | "exported"
    | "failed";
  prompt: string;
  reference_grid_relative_path: string | null;
  candidates: SequenceCandidate[];
  selected_candidate_id: string | null;
  created_at: string;
  updated_at: string;
}

export interface SequenceGenerateInput {
  candidate_count: number;
  prompt_override: string | null;
}

export interface SequenceExportFile {
  kind: "frame" | "sprite_sheet" | "gif" | "webp" | "report";
  filename: string;
  relative_path: string;
  sha256: string;
  file_bytes: number;
}

export interface SequenceExportResult {
  project_id: string;
  asset_id: string;
  category: SequenceCategory;
  files: SequenceExportFile[];
  drift_report: SequenceDriftReport;
}

interface SequenceTarget {
  category: SequenceCategory;
  assetId: string;
}

interface SequenceRunTarget extends SequenceTarget {
  runId: string;
}

export type SequenceArtifactKind =
  | "frame"
  | "sprite-sheet"
  | "gif"
  | "webp"
  | "report";

const MODEL_TIMEOUT = { timeoutMs: 150_000 };

function sequenceAssetPath(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
): string {
  return `/api/v1/projects/${projectId}/sequences/${category}/${assetId}`;
}

function sequenceRunPath(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
  runId: string,
): string {
  return `${sequenceAssetPath(projectId, category, assetId)}/runs/${runId}`;
}

export function createSequence(
  projectId: string,
  task: SequenceTask,
): Promise<SequenceRun> {
  return postJson<SequenceRun>(
    `/api/v1/projects/${projectId}/sequences`,
    task,
  );
}

export function fetchSequenceRuns(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
): Promise<SequenceRun[]> {
  return getJson<SequenceRun[]>(
    `${sequenceAssetPath(projectId, category, assetId)}/runs`,
  );
}

export function fetchSequenceRun(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
  runId: string,
): Promise<SequenceRun> {
  return getJson<SequenceRun>(
    sequenceRunPath(projectId, category, assetId, runId),
  );
}

export function generateSequence(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
  runId: string,
  input: SequenceGenerateInput,
): Promise<SequenceRun> {
  return postJson<SequenceRun>(
    `${sequenceRunPath(projectId, category, assetId, runId)}/generate`,
    input,
    MODEL_TIMEOUT,
  );
}

export function reprocessSequence(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
  runId: string,
): Promise<SequenceRun> {
  return postJson<SequenceRun>(
    `${sequenceRunPath(projectId, category, assetId, runId)}/reprocess`,
    {},
  );
}

export function selectSequenceCandidate(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
  runId: string,
  candidateId: string,
): Promise<SequenceRun> {
  return postJson<SequenceRun>(
    `${sequenceRunPath(projectId, category, assetId, runId)}/select`,
    { candidate_id: candidateId },
  );
}

export function exportSequence(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
  runId: string,
): Promise<SequenceExportResult> {
  return postJson<SequenceExportResult>(
    `${sequenceRunPath(projectId, category, assetId, runId)}/export`,
    {},
  );
}

export function sequenceArtifactUrl(
  projectId: string,
  category: SequenceCategory,
  assetId: string,
  runId: string,
  candidateId: string,
  kind: SequenceArtifactKind,
  frameIndex = 0,
): string {
  const base = `${sequenceRunPath(
    projectId,
    category,
    assetId,
    runId,
  )}/candidates/${candidateId}`;
  if (kind === "frame") {
    return `${base}/frames/${frameIndex}`;
  }
  if (kind === "sprite-sheet") {
    return `${base}/sprite-sheet`;
  }
  if (kind === "gif") {
    return `${base}/preview.gif`;
  }
  if (kind === "webp") {
    return `${base}/preview.webp`;
  }
  return `${base}/drift-report`;
}

export function useSequenceRunsQuery(
  projectId: string | undefined,
  category: SequenceCategory,
  assetId: string | undefined,
) {
  return useQuery({
    queryKey: ["sequence-runs", projectId, category, assetId],
    queryFn: () =>
      fetchSequenceRuns(projectId ?? "", category, assetId ?? ""),
    enabled: Boolean(projectId && assetId),
    retry: false,
  });
}

export function useSequenceRunQuery(
  projectId: string | undefined,
  category: SequenceCategory | undefined,
  assetId: string | undefined,
  runId: string | undefined,
) {
  return useQuery({
    queryKey: ["sequence-run", projectId, category, assetId, runId],
    queryFn: () =>
      fetchSequenceRun(
        projectId ?? "",
        category ?? "animation",
        assetId ?? "",
        runId ?? "",
      ),
    enabled: Boolean(projectId && category && assetId && runId),
    retry: false,
  });
}

export function useCreateSequenceMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (task: SequenceTask) => createSequence(projectId ?? "", task),
    onSuccess: (run) =>
      queryClient.invalidateQueries({
        queryKey: [
          "sequence-runs",
          projectId,
          run.task.category,
          run.task.asset_id,
        ],
      }),
  });
}

export function useGenerateSequenceMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, input }: SequenceRunTarget & {
      input: SequenceGenerateInput;
    }) =>
      generateSequence(
        projectId ?? "",
        category,
        assetId,
        runId,
        input,
      ),
  });
}

export function useReprocessSequenceMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId }: SequenceRunTarget) =>
      reprocessSequence(projectId ?? "", category, assetId, runId),
  });
}

export function useSelectSequenceMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, candidateId }: SequenceRunTarget & {
      candidateId: string;
    }) =>
      selectSequenceCandidate(
        projectId ?? "",
        category,
        assetId,
        runId,
        candidateId,
      ),
  });
}

export function useExportSequenceMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId }: SequenceRunTarget) =>
      exportSequence(projectId ?? "", category, assetId, runId),
  });
}
