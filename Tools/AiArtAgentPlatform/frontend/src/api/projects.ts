import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { getJson, postJson, putJson } from "./client";
import type {
  JobRecord,
  ProjectActivitySummary,
  ProjectConfig,
} from "../types/core";

export interface ProjectCreateInput {
  project_id: string;
  display_name: string;
  visual_type: "wuxia-ink-chibi-topdown-2_5d";
  language: "zh-CN" | "en-US";
}

export async function fetchProjects(): Promise<ProjectConfig[]> {
  const response = await getJson<unknown>("/api/v1/projects");
  return Array.isArray(response) ? (response as ProjectConfig[]) : [];
}

export function useProjectsQuery() {
  return useQuery({
    queryKey: ["projects"],
    queryFn: fetchProjects,
    retry: false,
    staleTime: 5_000,
  });
}

export function createProject(input: ProjectCreateInput): Promise<ProjectConfig> {
  return postJson<ProjectConfig>("/api/v1/projects", input);
}

export function updateProject(
  projectId: string,
  project: ProjectConfig,
): Promise<ProjectConfig> {
  return putJson<ProjectConfig>(`/api/v1/projects/${projectId}`, project);
}

export function fetchProjectActivity(
  projectId: string,
): Promise<ProjectActivitySummary> {
  return getJson<ProjectActivitySummary>(
    `/api/v1/projects/${projectId}/activity`,
  );
}

export function useCreateProjectMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createProject,
    onSuccess: async (project) => {
      await queryClient.invalidateQueries({ queryKey: ["projects"] });
      await queryClient.invalidateQueries({
        queryKey: ["project-activity", project.project_id],
      });
    },
  });
}

export function useUpdateProjectMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      projectId,
      project,
    }: {
      projectId: string;
      project: ProjectConfig;
    }) => updateProject(projectId, project),
    onSuccess: async (project) => {
      await queryClient.invalidateQueries({ queryKey: ["projects"] });
      await queryClient.invalidateQueries({
        queryKey: ["project-activity", project.project_id],
      });
    },
  });
}

export function useProjectActivityQuery(projectId: string | undefined) {
  return useQuery({
    queryKey: ["project-activity", projectId],
    queryFn: () => fetchProjectActivity(projectId ?? ""),
    enabled: Boolean(projectId),
    retry: false,
  });
}

export async function fetchProjectJobs(projectId: string): Promise<JobRecord[]> {
  const response = await getJson<unknown>(`/api/v1/projects/${projectId}/jobs`);
  return Array.isArray(response) ? (response as JobRecord[]) : [];
}

export function useProjectJobsQuery(projectId: string | undefined) {
  return useQuery({
    queryKey: ["project-jobs", projectId],
    queryFn: () => fetchProjectJobs(projectId ?? ""),
    enabled: Boolean(projectId),
    retry: false,
    refetchInterval: 2_000,
  });
}
