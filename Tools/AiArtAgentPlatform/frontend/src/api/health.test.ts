import { afterEach, describe, expect, it, vi } from "vitest";

import { ApiError, getJson } from "./client";
import { fetchHealth } from "./health";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("fetchHealth", () => {
  it("requests the versioned local health endpoint", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          status: "ok",
          service: "ai-art-agent-platform",
          schema_version: 1,
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        },
      ),
    );
    vi.stubGlobal("fetch", fetchMock);

    const result = await fetchHealth();

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/v1/health",
      expect.objectContaining({ headers: { Accept: "application/json" } }),
    );
    expect(result.status).toBe("ok");
    expect(result.schema_version).toBe(1);
  });

  it("reports the HTTP status when the local service rejects a request", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response("unavailable", { status: 503 })),
    );

    try {
      await fetchHealth();
      expect.fail("fetchHealth should reject non-success responses");
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      expect((error as ApiError).status).toBe(503);
    }
  });

  it("aborts local API requests after the configured timeout", async () => {
    const fetchMock = vi.fn((_input: RequestInfo | URL, init?: RequestInit) => {
      return new Promise<Response>((_resolve, reject) => {
        if (!init?.signal) {
          reject(new Error("missing abort signal"));
          return;
        }
        init.signal.addEventListener("abort", () => {
          reject(new DOMException("aborted", "AbortError"));
        });
      });
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(getJson("/api/v1/slow", { timeoutMs: 1 })).rejects.toThrow(
      "Local API request timed out",
    );
    expect(fetchMock.mock.calls[0]?.[1]?.signal).toBeInstanceOf(AbortSignal);
  });
});
