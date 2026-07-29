import { useQuery } from "@tanstack/react-query";

import { getJson } from "./client";

export interface HealthResponse {
  status: "ok";
  service: "ai-art-agent-platform";
  schema_version: 1;
}

export function fetchHealth(): Promise<HealthResponse> {
  return getJson<HealthResponse>("/api/v1/health");
}

export function useHealthQuery() {
  return useQuery({
    queryKey: ["health"],
    queryFn: fetchHealth,
    retry: false,
    staleTime: 10_000,
  });
}
