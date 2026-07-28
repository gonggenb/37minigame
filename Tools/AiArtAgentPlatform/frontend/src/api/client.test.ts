import { afterEach, expect, it, vi } from "vitest";

import { deleteRequest, postJson } from "./client";

afterEach(() => {
  vi.unstubAllGlobals();
});

it("preserves a structured FastAPI provider error message", async () => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          detail: {
            code: "bad_request",
            message:
              "invalid gpt-image-2 canvas 2048x512: aspect ratio must not exceed 3:1",
            retryable: false,
          },
        }),
        { status: 400, headers: { "Content-Type": "application/json" } },
      ),
    ),
  );

  await expect(postJson("/api/test", {})).rejects.toThrow(
    "invalid gpt-image-2 canvas 2048x512",
  );
});

it("sends a DELETE request without parsing an empty response", async () => {
  const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
  vi.stubGlobal("fetch", fetchMock);

  await deleteRequest("/api/v1/projects/wuxia-demo/references/hero-main");

  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/projects/wuxia-demo/references/hero-main",
    expect.objectContaining({ method: "DELETE" }),
  );
});
