import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";

import { deleteRequest, getJson, postJson, putJson } from "./client";
import type { AssetCategory } from "../types/core";

export interface StyleGuide {
  schema_version: 1;
  style_id: string;
  display_name: string;
  reference_source: {
    path: string;
    mode: "read_only";
  };
  camera: {
    projection: string;
    pitch_semantic_min: number;
    pitch_semantic_max: number;
    shared_view_required: boolean;
    default_facing: string;
  };
  palette: {
    base: string[];
    accents: string[];
  };
  rendering: {
    character_proportion: string;
    character_outline: string;
    environment_detail: string;
    surface_finish: string;
    shadow_direction: string;
  };
  readability: {
    protect_playfield: boolean;
    character_contrast_above_environment: boolean;
    preserve_clear_silhouette: boolean;
    avoid_high_frequency_ground_noise: boolean;
  };
  ui: {
    formal_text_baked_in: boolean;
    border_language: string[];
  };
  forbidden: string[];
}

export interface ReferenceAsset {
  reference_id: string;
  source_relative_path: string;
  workspace_relative_path: string;
  thumbnail_relative_path: string;
  sha256: string;
  width: number;
  height: number;
  categories: AssetCategory[];
  identities: string[];
  usages: string[];
  viewpoints: string[];
  materials: string[];
  notes: string;
}

export interface ReferenceImportInput {
  reference_id: string;
  source_relative_path: string;
  categories: AssetCategory[];
  identities: string[];
  usages: string[];
  viewpoints: string[];
  materials: string[];
  notes: string;
}

export type ReferenceUpdateInput = Pick<
  ReferenceAsset,
  | "categories"
  | "identities"
  | "usages"
  | "viewpoints"
  | "materials"
  | "notes"
>;

export interface SourceReferenceFile {
  relative_path: string;
  size_bytes: number;
}

export interface ReferenceFilters {
  category?: AssetCategory;
  identity?: string;
  usage?: string;
  viewpoint?: string;
  material?: string;
  limit?: number;
}

export interface AssetTaskInput {
  asset_id: string;
  category: AssetCategory;
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

export interface CharacterIdentityInput {
  asset_id: string;
  display_name: string;
  silhouette: string[];
  face: string[];
  hair: string[];
  costume: string[];
  palette: string[];
  equipment: string[];
  immutable_traits: string[];
}

export interface PromptPreviewInput {
  task: AssetTaskInput;
  identity: CharacterIdentityInput | null;
  viewpoint: string;
  composition: string;
  lighting: string;
  materials: string[];
  output_spec: {
    width: number;
    height: number;
    format: "png";
    transparent_required: boolean;
  };
  additional_negative_constraints: string[];
  prompt_override: string | null;
}

export interface PromptSection {
  key: string;
  label: string;
  content: string;
}

export interface CompiledPrompt {
  task: AssetTaskInput;
  selected_reference_ids: string[];
  sections: PromptSection[];
  prompt: string;
  negative_constraints: string[];
}

export function fetchStyleGuide(projectId: string): Promise<StyleGuide> {
  return getJson<StyleGuide>(`/api/v1/projects/${projectId}/style-guide`);
}

export function updateStyleGuide(
  projectId: string,
  guide: StyleGuide,
): Promise<StyleGuide> {
  return putJson<StyleGuide>(
    `/api/v1/projects/${projectId}/style-guide`,
    guide,
  );
}

export function fetchReferenceSource(
  projectId: string,
  query = "",
  limit = 100,
): Promise<SourceReferenceFile[]> {
  const params = new URLSearchParams({ query, limit: String(limit) });
  return getJson<SourceReferenceFile[]>(
    `/api/v1/projects/${projectId}/reference-source?${params.toString()}`,
  );
}

export function fetchReferences(
  projectId: string,
  filters: ReferenceFilters = {},
): Promise<ReferenceAsset[]> {
  const params = new URLSearchParams();
  if (filters.category) params.set("category", filters.category);
  if (filters.identity) params.set("identity", filters.identity);
  if (filters.usage) params.set("usage", filters.usage);
  if (filters.viewpoint) params.set("viewpoint", filters.viewpoint);
  if (filters.material) params.set("material", filters.material);
  if (filters.limit) params.set("limit", String(filters.limit));
  const suffix = params.size ? `?${params.toString()}` : "";
  return getJson<ReferenceAsset[]>(
    `/api/v1/projects/${projectId}/references${suffix}`,
  );
}

export function importReference(
  projectId: string,
  input: ReferenceImportInput,
): Promise<ReferenceAsset> {
  return postJson<ReferenceAsset>(
    `/api/v1/projects/${projectId}/references`,
    input,
  );
}

export function updateReference(
  projectId: string,
  referenceId: string,
  input: ReferenceUpdateInput,
): Promise<ReferenceAsset> {
  return putJson<ReferenceAsset>(
    `/api/v1/projects/${projectId}/references/${referenceId}`,
    input,
  );
}

export function deleteReference(
  projectId: string,
  referenceId: string,
): Promise<void> {
  return deleteRequest(
    `/api/v1/projects/${projectId}/references/${referenceId}`,
  );
}

export function referenceThumbnailUrl(
  projectId: string,
  reference: Pick<ReferenceAsset, "reference_id" | "sha256">,
): string {
  return (
    `/api/v1/projects/${projectId}/references/${reference.reference_id}` +
    `/thumbnail?v=${reference.sha256}`
  );
}

export function previewPrompt(
  projectId: string,
  input: PromptPreviewInput,
): Promise<CompiledPrompt> {
  return postJson<CompiledPrompt>(
    `/api/v1/projects/${projectId}/prompt-preview`,
    input,
  );
}

export function useStyleGuideQuery(projectId: string | undefined) {
  return useQuery({
    queryKey: ["style-guide", projectId],
    queryFn: () => fetchStyleGuide(projectId ?? ""),
    enabled: Boolean(projectId),
    retry: false,
    staleTime: 10_000,
  });
}

export function useUpdateStyleGuideMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (guide: StyleGuide) => updateStyleGuide(projectId ?? "", guide),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["style-guide", projectId] });
      await queryClient.invalidateQueries({
        queryKey: ["reference-source", projectId],
      });
    },
  });
}

export function useReferenceSourceQuery(
  projectId: string | undefined,
  query: string,
  limit: number,
) {
  return useQuery({
    queryKey: ["reference-source", projectId, query, limit],
    queryFn: () => fetchReferenceSource(projectId ?? "", query, limit),
    enabled: Boolean(projectId),
    retry: false,
  });
}

export function useReferencesQuery(
  projectId: string | undefined,
  filters: ReferenceFilters = {},
) {
  return useQuery({
    queryKey: ["references", projectId, filters],
    queryFn: () => fetchReferences(projectId ?? "", filters),
    enabled: Boolean(projectId),
    retry: false,
  });
}

export function useImportReferenceMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ReferenceImportInput) =>
      importReference(projectId ?? "", input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["references", projectId] });
      await queryClient.invalidateQueries({
        queryKey: ["project-activity", projectId],
      });
    },
  });
}

export function useUpdateReferenceMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      referenceId,
      input,
    }: {
      referenceId: string;
      input: ReferenceUpdateInput;
    }) => updateReference(projectId ?? "", referenceId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["references", projectId] });
      await queryClient.invalidateQueries({
        queryKey: ["project-activity", projectId],
      });
    },
  });
}

export function useDeleteReferenceMutation(projectId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (referenceId: string) =>
      deleteReference(projectId ?? "", referenceId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["references", projectId] });
      await queryClient.invalidateQueries({
        queryKey: ["project-activity", projectId],
      });
    },
  });
}

export function usePromptPreviewMutation(projectId: string | undefined) {
  return useMutation({
    mutationFn: (input: PromptPreviewInput) =>
      previewPrompt(projectId ?? "", input),
  });
}
