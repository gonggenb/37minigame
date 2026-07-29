import { afterEach, expect, it, vi } from "vitest";

import { fetchModelStatus, testModelAvailability } from "./models";

afterEach(() => {
  vi.unstubAllGlobals();
});

it("loads public model status without a secret field", async () => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          api_key_configured: true,
          review_model: "gpt-5.6",
          image_model: "gpt-image-2",
          timeout_seconds: 120,
          max_retries: 2,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    ),
  );

  const status = await fetchModelStatus();

  expect(status.api_key_configured).toBe(true);
  expect(status.review_model).toBe("gpt-5.6");
  expect(status).not.toHaveProperty("openai_api_key");
});

it("tests image availability only when explicitly requested", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(
      JSON.stringify({
        checks: [
          {
            capability: "structured_review",
            model: "gpt-5.6",
            available: true,
            error_code: null,
            retryable: false,
            detail: "",
          },
        ],
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await testModelAvailability(false);

  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/models/availability",
    expect.objectContaining({
      method: "POST",
      body: JSON.stringify({ include_image: false }),
    }),
  );
});

it("includes the image model only after explicit opt-in", async () => {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(
      JSON.stringify({
        checks: [
          {
            capability: "image_generation",
            model: "gpt-image-2",
            available: false,
            error_code: "rate_limit",
            retryable: true,
            detail: "OpenAI rate limit reached",
          },
        ],
      }),
      { status: 200, headers: { "Content-Type": "application/json" } },
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  const result = await testModelAvailability(true);

  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/models/availability",
    expect.objectContaining({
      method: "POST",
      body: JSON.stringify({ include_image: true }),
    }),
  );
  expect(result.checks[0]).toMatchObject({
    error_code: "rate_limit",
    retryable: true,
  });
});
