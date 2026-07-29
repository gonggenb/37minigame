import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import type { AssetCategory } from "../types/core";
import type {
  ExportRecord,
  HardConstraintReport,
  ProcessedImageMetadata,
} from "./constraints";
import { getJson, postJson, putJson } from "./client";

export type StaticAssetCategory = Extract<
  AssetCategory,
  "item" | "ui" | "character" | "scene"
>;

export interface AssetTask {
  asset_id: string;
  category: StaticAssetCategory;
  name: string;
  brief: string;
  usage: string;
  style_pack: string;
  reference_ids: string[];
  constraint_profile: string;
  constraint_overrides: Record<string, unknown>;
  candidate_count: number;
  output_mode: string;
}

export interface StaticAssetRecord {
  schema_version: 1;
  task: AssetTask;
  created_at: string;
  updated_at: string;
}

export interface GenerationPlan {
  asset_type: StaticAssetCategory;
  usage: string;
  selected_reference_ids: string[];
  composition: string;
  camera: string;
  lighting: string;
  identity_constraints: string[];
  prompt: string;
  negative_constraints: string[];
  output_spec: {
    width: number;
    height: number;
    format: "png";
    transparent_required: boolean;
  };
  postprocess_steps: string[];
  quality_checks: string[];
  repair_strategy: string[];
}

export interface StyleReview {
  score: number;
  identity_score: number;
  palette_score: number;
  line_style_score: number;
  composition_score: number;
  issues: string[];
  repair_instruction: string;
  summary: string;
  strengths: string[];
  findings: ReviewFinding[];
  risk_notes: string[];
}

export type ReviewDimension =
  | "hard_constraint"
  | "identity"
  | "palette"
  | "line_style"
  | "composition"
  | "animation";

export interface ReviewFinding {
  dimension: ReviewDimension;
  severity: "info" | "warning" | "error";
  summary: string;
  evidence: string;
  repair_hint: string;
  actionable: boolean;
}

export interface QualityReport {
  hard_constraints: HardConstraintReport;
  style_review: StyleReview;
  animation_review: unknown | null;
  export_allowed: boolean;
  review_basis: string[];
  decision: "pass" | "retry" | "manual_review";
}

export interface RepairPlan {
  action: "none" | "edit" | "reprocess" | "manual";
  reason: string;
  target_dimensions: ReviewDimension[];
  prompt: string;
  retry_allowed: boolean;
  stop_reason:
    | "passed"
    | "retry-limit-reached"
    | "no-actionable-failure"
    | "manual-review-required"
    | "disabled"
    | null;
}

export interface ReviewAttempt {
  attempt_index: number;
  run_id: string;
  candidate_id: string;
  comparison_relative_path: string;
  quality_report: QualityReport | null;
  repair_plan: RepairPlan | null;
  created_at: string;
}

export interface AutoRepairSummary {
  retry_count: number;
  max_retries: number;
  stop_reason: Exclude<RepairPlan["stop_reason"], null>;
  attempts: ReviewAttempt[];
}

export interface ProductionCandidate {
  candidate_id: string;
  index: number;
  raw_relative_path: string;
  processed_relative_path: string;
  metadata: ProcessedImageMetadata;
  hard_constraints: HardConstraintReport;
  revised_prompt: string | null;
  quality_report: QualityReport | null;
  comparison_relative_path: string | null;
}

export interface ProductionRun {
  schema_version: 1;
  run_id: string;
  project_id: string;
  task: AssetTask;
  status:
    | "planned"
    | "generated"
    | "selected"
    | "reviewed"
    | "exported"
    | "failed";
  plan: GenerationPlan | null;
  prompt: string;
  candidates: ProductionCandidate[];
  selected_candidate_id: string | null;
  source_run_id: string | null;
  source_candidate_id: string | null;
  edit_instruction: string;
  review_attempts: ReviewAttempt[];
  auto_repair_summary: AutoRepairSummary | null;
  export: ExportRecord | null;
  created_at: string;
  updated_at: string;
}

export interface ProductionGenerateInput {
  candidate_count: number;
  prompt_override: string | null;
}

export interface CandidateEditInput {
  candidate_id: string;
  instruction: string;
  candidate_count: number;
  mask_workspace_relative_path: string | null;
}

export interface ReviewAndRepairInput {
  candidate_id: string;
  automatic_repair: boolean;
  max_retries: number;
}

export interface CropRectInput {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface CandidateTransformInput {
  candidate_id: string;
  crop: CropRectInput | null;
  output_width: number | null;
  output_height: number | null;
  padding_ratio: number | null;
  remove_background: boolean;
}

export interface CandidateMaskRecord {
  workspace_relative_path: string;
  width: number;
  height: number;
  sha256: string;
}

export interface ProductionExportInput {
  variant: string;
  accept_style_risk: boolean;
}

export interface ProductionExportResult {
  export: ExportRecord;
  style_score: number;
  minimum_style_score: number;
  style_risk_accepted: boolean;
}

interface AssetTarget {
  category: StaticAssetCategory;
  assetId: string;
}

interface RunTarget extends AssetTarget {
  runId: string;
}

const MODEL_TIMEOUT = { timeoutMs: 150_000 };

function assetPath(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
): string {
  return `/api/v1/projects/${projectId}/assets/${category}/${assetId}`;
}

function runPath(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
): string {
  return `${assetPath(projectId, category, assetId)}/runs/${runId}`;
}

export function fetchStaticAssets(projectId: string): Promise<StaticAssetRecord[]> {
  return getJson<StaticAssetRecord[]>(`/api/v1/projects/${projectId}/assets`);
}

export function fetchProductionRuns(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
): Promise<ProductionRun[]> {
  return getJson<ProductionRun[]>(
    `${assetPath(projectId, category, assetId)}/runs`,
  );
}

export function createStaticAsset(
  projectId: string,
  task: AssetTask,
): Promise<StaticAssetRecord> {
  return postJson<StaticAssetRecord>(
    `/api/v1/projects/${projectId}/assets`,
    task,
  );
}

export function updateStaticAsset(
  projectId: string,
  task: AssetTask,
): Promise<StaticAssetRecord> {
  return putJson<StaticAssetRecord>(
    assetPath(projectId, task.category, task.asset_id),
    task,
  );
}

export function planStaticAsset(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
): Promise<ProductionRun> {
  return postJson<ProductionRun>(
    `${assetPath(projectId, category, assetId)}/plan`,
    {},
    MODEL_TIMEOUT,
  );
}

export function generateProductionCandidates(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  input: ProductionGenerateInput,
): Promise<ProductionRun> {
  return postJson<ProductionRun>(
    `${runPath(projectId, category, assetId, runId)}/generate`,
    input,
    MODEL_TIMEOUT,
  );
}

export function selectProductionCandidate(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  candidateId: string,
): Promise<ProductionRun> {
  return postJson<ProductionRun>(
    `${runPath(projectId, category, assetId, runId)}/select`,
    { candidate_id: candidateId },
  );
}

export function editProductionCandidate(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  input: CandidateEditInput,
): Promise<ProductionRun> {
  return postJson<ProductionRun>(
    `${runPath(projectId, category, assetId, runId)}/edit`,
    input,
    MODEL_TIMEOUT,
  );
}

export function reviewProductionCandidate(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  candidateId: string,
): Promise<ProductionRun> {
  return postJson<ProductionRun>(
    `${runPath(projectId, category, assetId, runId)}/review`,
    { candidate_id: candidateId },
    MODEL_TIMEOUT,
  );
}

export function reviewAndRepairProductionCandidate(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  input: ReviewAndRepairInput,
): Promise<ProductionRun> {
  return postJson<ProductionRun>(
    `${runPath(projectId, category, assetId, runId)}/review-and-repair`,
    input,
    { timeoutMs: 450_000 },
  );
}

export function transformProductionCandidate(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  input: CandidateTransformInput,
): Promise<ProductionRun> {
  return postJson<ProductionRun>(
    `${runPath(projectId, category, assetId, runId)}/transform`,
    input,
  );
}

export function saveProductionMask(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  candidateId: string,
  maskPngBase64: string,
): Promise<CandidateMaskRecord> {
  return postJson<CandidateMaskRecord>(
    `${runPath(projectId, category, assetId, runId)}/candidates/${candidateId}/mask`,
    { mask_png_base64: maskPngBase64 },
  );
}

export function exportProductionRun(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  input: ProductionExportInput,
): Promise<ProductionExportResult> {
  return postJson<ProductionExportResult>(
    `${runPath(projectId, category, assetId, runId)}/export`,
    input,
  );
}

export function productionCandidateImageUrl(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  candidateId: string,
): string {
  return `${runPath(projectId, category, assetId, runId)}/candidates/${candidateId}/image`;
}

export function productionCandidateComparisonUrl(
  projectId: string,
  category: StaticAssetCategory,
  assetId: string,
  runId: string,
  candidateId: string,
): string {
  return `${runPath(projectId, category, assetId, runId)}/candidates/${candidateId}/comparison`;
}

export function useStaticAssetsQuery(projectId: string | undefined) {
  return useQuery({
    queryKey: ["static-assets", projectId],
    queryFn: () => fetchStaticAssets(projectId ?? ""),
    enabled: Boolean(projectId),
    retry: false,
  });
}

export function useProductionRunsQuery(
  projectId: string | undefined,
  category: StaticAssetCategory | undefined,
  assetId: string | undefined,
) {
  return useQuery({
    queryKey: ["production-runs", projectId, category, assetId],
    queryFn: () =>
      fetchProductionRuns(projectId ?? "", category ?? "item", assetId ?? ""),
    enabled: Boolean(projectId && category && assetId),
    retry: false,
  });
}

export function useCreateStaticAssetMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (task: AssetTask) => createStaticAsset(projectId ?? "", task),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["static-assets", projectId] }),
  });
}

export function useUpdateStaticAssetMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (task: AssetTask) => updateStaticAsset(projectId ?? "", task),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["static-assets", projectId] }),
  });
}

export function usePlanStaticAssetMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId }: AssetTarget) =>
      planStaticAsset(projectId ?? "", category, assetId),
  });
}

export function useGenerateProductionMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, input }: RunTarget & {
      input: ProductionGenerateInput;
    }) =>
      generateProductionCandidates(
        projectId ?? "",
        category,
        assetId,
        runId,
        input,
      ),
  });
}

export function useSelectProductionMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, candidateId }: RunTarget & {
      candidateId: string;
    }) =>
      selectProductionCandidate(
        projectId ?? "",
        category,
        assetId,
        runId,
        candidateId,
      ),
  });
}

export function useEditProductionMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, input }: RunTarget & {
      input: CandidateEditInput;
    }) =>
      editProductionCandidate(
        projectId ?? "",
        category,
        assetId,
        runId,
        input,
      ),
  });
}

export function useReviewProductionMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, candidateId }: RunTarget & {
      candidateId: string;
    }) =>
      reviewProductionCandidate(
        projectId ?? "",
        category,
        assetId,
        runId,
        candidateId,
      ),
  });
}

export function useReviewAndRepairProductionMutation(
  projectId: string | undefined,
) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, input }: RunTarget & {
      input: ReviewAndRepairInput;
    }) =>
      reviewAndRepairProductionCandidate(
        projectId ?? "",
        category,
        assetId,
        runId,
        input,
      ),
  });
}

export function useTransformProductionMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, input }: RunTarget & {
      input: CandidateTransformInput;
    }) =>
      transformProductionCandidate(
        projectId ?? "",
        category,
        assetId,
        runId,
        input,
      ),
  });
}

export function useSaveProductionMaskMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, candidateId, maskPngBase64 }:
      RunTarget & {
        candidateId: string;
        maskPngBase64: string;
      }) =>
      saveProductionMask(
        projectId ?? "",
        category,
        assetId,
        runId,
        candidateId,
        maskPngBase64,
      ),
  });
}

export function useExportProductionMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, assetId, runId, input }: RunTarget & {
      input: ProductionExportInput;
    }) =>
      exportProductionRun(
        projectId ?? "",
        category,
        assetId,
        runId,
        input,
      ),
  });
}
