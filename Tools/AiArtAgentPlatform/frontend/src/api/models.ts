import { useMutation, useQuery } from "@tanstack/react-query";

import { getJson, postJson } from "./client";

export type ProviderErrorCode =
  | "timeout"
  | "connection"
  | "rate_limit"
  | "authentication"
  | "permission"
  | "bad_request"
  | "server"
  | "content_refusal"
  | "response_format"
  | "unsupported_capability"
  | "missing_api_key"
  | "unknown";

export interface ModelStatus {
  api_key_configured: boolean;
  review_model: string;
  image_model: string;
  timeout_seconds: number;
  max_retries: number;
}

export interface ModelCheckResult {
  capability: string;
  model: string;
  available: boolean;
  error_code: ProviderErrorCode | null;
  retryable: boolean;
  detail: string;
}

export interface ModelAvailability {
  checks: ModelCheckResult[];
}

export function fetchModelStatus(): Promise<ModelStatus> {
  return getJson<ModelStatus>("/api/v1/models/status");
}

export function testModelAvailability(
  includeImage: boolean,
): Promise<ModelAvailability> {
  return postJson<ModelAvailability>(
    "/api/v1/models/availability",
    { include_image: includeImage },
    { timeoutMs: 180_000 },
  );
}

export function useModelStatusQuery() {
  return useQuery({
    queryKey: ["model-status"],
    queryFn: fetchModelStatus,
    retry: false,
    staleTime: 10_000,
  });
}

export function useModelAvailabilityMutation() {
  return useMutation({
    mutationFn: testModelAvailability,
  });
}
