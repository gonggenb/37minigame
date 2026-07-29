import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { ModelStatusCard } from "./ModelStatusCard";

afterEach(() => {
  vi.unstubAllGlobals();
});

it("disables paid model tests when the API key is missing", async () => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          api_key_configured: false,
          review_model: "gpt-5.6",
          image_model: "gpt-image-2",
          timeout_seconds: 120,
          max_retries: 2,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    ),
  );
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(
    <QueryClientProvider client={client}>
      <ModelStatusCard />
    </QueryClientProvider>,
  );

  expect(await screen.findByText("OpenAI API Key 未配置")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "测试模型连接" })).toBeDisabled();
  expect(screen.getByText(/图像模型测试会产生 API 费用/)).toBeInTheDocument();
});

it("runs the default planning check and reports retry guidance", async () => {
  const fetchMock = vi
    .fn()
    .mockResolvedValueOnce(
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
    )
    .mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          checks: [
            {
              capability: "structured_review",
              model: "gpt-5.6",
              available: false,
              error_code: "timeout",
              retryable: true,
              detail: "OpenAI request timed out",
            },
          ],
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );
  vi.stubGlobal("fetch", fetchMock);
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(
    <QueryClientProvider client={client}>
      <ModelStatusCard />
    </QueryClientProvider>,
  );

  expect(await screen.findByText("OpenAI API Key 已配置")).toBeInTheDocument();
  const includeImage = screen.getByRole("checkbox", {
    name: /同时测试图像模型/,
  });
  expect(includeImage).not.toBeChecked();

  fireEvent.click(screen.getByRole("button", { name: "测试模型连接" }));

  expect(await screen.findByText("错误码：timeout")).toBeInTheDocument();
  expect(screen.getByText("可重试")).toBeInTheDocument();
  expect(fetchMock).toHaveBeenLastCalledWith(
    "/api/v1/models/availability",
    expect.objectContaining({
      body: JSON.stringify({ include_image: false }),
    }),
  );
});
