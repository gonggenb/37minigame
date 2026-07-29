import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";

import type { AssetCategory } from "../types/core";
import { getJson, postJson, putJson } from "./client";

export interface ConstraintProfile {
  schema_version: 1;
  profile_id: string;
  category: AssetCategory;
  master_width: number;
  master_height: number;
  output_width: number;
  output_height: number;
  require_rgba: boolean;
  require_transparency: boolean;
  crop_mode: "alpha_bounds" | "fixed" | "none";
  padding_ratio: number;
  occupancy_ratio: number;
  resize_algorithm: "lanczos" | "nearest";
  pivot_x: number;
  pivot_y: number;
  filename_template: string;
  max_file_bytes: number;
  output_sprite_sheet: boolean;
  frame_count: number | null;
  rows: number | null;
  columns: number | null;
  frame_width: number | null;
  frame_height: number | null;
  preview_fps: number | null;
  loop: boolean | null;
  baseline: "bottom_center" | "center" | "custom" | null;
  shared_scale: boolean;
  lock_first_frame: boolean;
  max_center_drift_px: number | null;
  max_size_drift_ratio: number | null;
}

export interface BackgroundRemovalConfig {
  mode: "preserve" | "corner_flood";
  color_tolerance: number;
  alpha_low_threshold: number;
  alpha_high_threshold: number;
}

export interface WorkspaceImageRequest {
  workspace_relative_path: string;
  asset_id: string;
  variant: string;
  background: BackgroundRemovalConfig;
}

export interface ProcessedImageMetadata {
  width: number;
  height: number;
  mode: "RGBA";
  source_alpha_bounds: [number, number, number, number];
  alpha_bounds: [number, number, number, number];
  scale: number;
  sha256: string;
  file_bytes: number;
}

export interface HardConstraintCheck {
  name: string;
  passed: boolean;
  message: string;
}

export interface HardConstraintReport {
  passed: boolean;
  checks: HardConstraintCheck[];
}

export interface ProcessPreviewResponse {
  processed_png_base64: string;
  metadata: ProcessedImageMetadata;
  hard_constraints: HardConstraintReport;
}

export interface ExportRecord {
  project_id: string;
  asset_id: string;
  category: AssetCategory;
  variant: string;
  filename: string;
  relative_path: string;
  sha256: string;
  written_sha256: string;
  file_bytes: number;
  hard_constraints: HardConstraintReport;
}

export type ConstraintProfiles = Record<AssetCategory, ConstraintProfile>;

export interface CategoryImageRequest {
  category: AssetCategory;
  request: WorkspaceImageRequest;
}

export interface SaveConstraintInput {
  category: AssetCategory;
  profile: ConstraintProfile;
}

export function fetchConstraints(projectId: string): Promise<ConstraintProfiles> {
  return getJson<ConstraintProfiles>(
    `/api/v1/projects/${projectId}/constraints`,
  );
}

export function saveConstraint(
  projectId: string,
  category: AssetCategory,
  profile: ConstraintProfile,
): Promise<ConstraintProfile> {
  return putJson<ConstraintProfile>(
    `/api/v1/projects/${projectId}/constraints/${category}`,
    profile,
  );
}

export function processConstraintPreview(
  projectId: string,
  category: AssetCategory,
  request: WorkspaceImageRequest,
): Promise<ProcessPreviewResponse> {
  return postJson<ProcessPreviewResponse>(
    `/api/v1/projects/${projectId}/constraints/${category}/process-preview`,
    request,
  );
}

export function exportConstrainedImage(
  projectId: string,
  category: AssetCategory,
  request: WorkspaceImageRequest,
): Promise<ExportRecord> {
  return postJson<ExportRecord>(
    `/api/v1/projects/${projectId}/constraints/${category}/export`,
    request,
  );
}

export function useConstraintsQuery(projectId: string | undefined) {
  return useQuery({
    queryKey: ["constraints", projectId],
    queryFn: () => fetchConstraints(projectId ?? ""),
    enabled: Boolean(projectId),
    retry: false,
  });
}

export function useSaveConstraintMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ category, profile }: SaveConstraintInput) =>
      saveConstraint(projectId ?? "", category, profile),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["constraints", projectId] }),
  });
}

export function useProcessConstraintMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, request }: CategoryImageRequest) =>
      processConstraintPreview(projectId ?? "", category, request),
  });
}

export function useExportConstraintMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: ({ category, request }: CategoryImageRequest) =>
      exportConstrainedImage(projectId ?? "", category, request),
  });
}
