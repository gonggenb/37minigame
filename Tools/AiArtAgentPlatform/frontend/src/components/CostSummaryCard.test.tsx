import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { CostSummaryCard } from "./CostSummaryCard";

afterEach(() => {
  vi.unstubAllGlobals();
});

it("shows known cost and preserves unknown cost records", async () => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          project_id: "wuxia-demo",
          request_count: 3,
          known_cost_usd: 0.15,
          unknown_cost_count: 1,
          invalid_record_count: 0,
          by_model: [
            {
              key: "gpt-image-2",
              request_count: 1,
              known_cost_usd: 0.1,
              unknown_cost_count: 0,
            },
          ],
          by_category: [
            {
              key: "item",
              request_count: 3,
              known_cost_usd: 0.15,
              unknown_cost_count: 1,
            },
          ],
          latest_at: "2026-07-28T12:00:00Z",
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    ),
  );
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(
    <QueryClientProvider client={client}>
      <CostSummaryCard projectId="wuxia-demo" />
    </QueryClientProvider>,
  );

  expect(await screen.findByText("$0.1500")).toBeInTheDocument();
  expect(screen.getByText("1 条费用未知记录")).toBeInTheDocument();
  expect(screen.getByText(/不会伪造精确金额/)).toBeInTheDocument();
});
