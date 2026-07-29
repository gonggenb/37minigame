import { useQuery } from "@tanstack/react-query";

import { getJson } from "./client";

export interface CostBreakdown {
  key: string;
  request_count: number;
  known_cost_usd: number;
  unknown_cost_count: number;
}

export interface ProjectCostSummary {
  project_id: string;
  request_count: number;
  known_cost_usd: number;
  unknown_cost_count: number;
  invalid_record_count: number;
  by_model: CostBreakdown[];
  by_category: CostBreakdown[];
  latest_at: string | null;
}

export function fetchProjectCosts(projectId: string): Promise<ProjectCostSummary> {
  return getJson<ProjectCostSummary>(`/api/v1/projects/${projectId}/costs`);
}

export function useProjectCostsQuery(projectId: string | undefined) {
  return useQuery({
    queryKey: ["project-costs", projectId],
    queryFn: () => fetchProjectCosts(projectId ?? ""),
    enabled: Boolean(projectId),
    retry: false,
  });
}
