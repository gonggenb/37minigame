import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { ReferenceSourceBrowser } from "./ReferenceSourceBrowser";

afterEach(() => {
  vi.unstubAllGlobals();
});

it("searches the read-only source and copies a tagged reference", async () => {
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (path.includes("/reference-source?")) {
        return Promise.resolve(
          new Response(
            JSON.stringify([
              { relative_path: "角色/hero.png", size_bytes: 2048 },
            ]),
            { status: 200, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      if (path.endsWith("/references") && options?.method === "POST") {
        const body = JSON.parse(String(options.body));
        return Promise.resolve(
          new Response(
            JSON.stringify({
              ...body,
              workspace_relative_path: "style-pack/references/hero-main.png",
              thumbnail_relative_path: "style-pack/thumbnails/hero-main.png",
              sha256: "a".repeat(64),
              width: 256,
              height: 256,
            }),
            { status: 201, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      throw new Error(`Unexpected request: ${path}`);
    },
  );
  vi.stubGlobal("fetch", fetchMock);
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={client}>
      <ReferenceSourceBrowser projectId="wuxia-demo" />
    </QueryClientProvider>,
  );

  expect(screen.getByText(/源目录只读/)).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("搜索素材源"), {
    target: { value: "hero" },
  });
  fireEvent.click(screen.getByRole("button", { name: "搜索" }));
  fireEvent.click(
    await screen.findByRole("button", { name: /角色\/hero.png/ }),
  );
  fireEvent.change(screen.getByLabelText("参考 ID"), {
    target: { value: "hero-main" },
  });
  fireEvent.change(screen.getByLabelText("身份标签"), {
    target: { value: "hero-main,young-swordsman" },
  });
  fireEvent.click(screen.getByRole("button", { name: "复制到项目参考库" }));

  await waitFor(() =>
    expect(fetchMock).toHaveBeenCalledWith(
      "/api/v1/projects/wuxia-demo/references",
      expect.objectContaining({
        method: "POST",
        body: expect.stringContaining(
          '\"source_relative_path\":\"角色/hero.png\"',
        ),
      }),
    ),
  );
  expect(
    fetchMock.mock.calls.some(([path]) => String(path).includes("query=hero")),
  ).toBe(true);
});
