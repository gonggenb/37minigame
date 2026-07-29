import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";

import { coverageMessage, ReferenceLibrary } from "./ReferenceLibrary";
import type { ReferenceAsset } from "../api/stylePack";

afterEach(() => {
  vi.unstubAllGlobals();
});

const reference: ReferenceAsset = {
  reference_id: "hero-main",
  source_relative_path: "角色/hero.png",
  workspace_relative_path: "style-pack/references/hero-main.png",
  thumbnail_relative_path: "style-pack/thumbnails/hero-main.png",
  sha256: "a".repeat(64),
  width: 256,
  height: 256,
  categories: ["character"],
  identities: ["hero-main"],
  usages: ["gameplay"],
  viewpoints: ["topdown-45"],
  materials: ["rice-paper"],
  notes: "批准参考",
};

it("reports the recommended reference coverage ranges", () => {
  expect(coverageMessage(9)).toBe("风格覆盖不足：建议至少导入 10 张参考图");
  expect(coverageMessage(10)).toBe("参考数量处于推荐范围（10–30 张）");
  expect(coverageMessage(31)).toBe("参考数量超过 30 张：建议精简重复参考");
});

it("filters edits and removes project reference copies", async () => {
  const confirmMock = vi.fn().mockReturnValue(true);
  vi.stubGlobal("confirm", confirmMock);
  const fetchMock = vi.fn().mockImplementation(
    (request: RequestInfo | URL, options?: RequestInit) => {
      const path = String(request);
      if (options?.method === "PUT") {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              ...reference,
              ...JSON.parse(String(options.body)),
            }),
            { status: 200, headers: { "Content-Type": "application/json" } },
          ),
        );
      }
      if (options?.method === "DELETE") {
        return Promise.resolve(new Response(null, { status: 204 }));
      }
      if (path.includes("/references")) {
        return Promise.resolve(
          new Response(JSON.stringify([reference]), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          }),
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
      <ReferenceLibrary projectId="wuxia-demo" />
    </QueryClientProvider>,
  );

  expect(
    await screen.findByText("风格覆盖不足：建议至少导入 10 张参考图"),
  ).toBeInTheDocument();
  expect(await screen.findByAltText("hero-main 缩略图")).toHaveAttribute(
    "src",
    "/api/v1/projects/wuxia-demo/references/hero-main/thumbnail?v=" +
      reference.sha256,
  );

  fireEvent.change(screen.getByLabelText("类别筛选"), {
    target: { value: "character" },
  });
  fireEvent.change(screen.getByLabelText("身份筛选"), {
    target: { value: "hero-main" },
  });
  fireEvent.change(screen.getByLabelText("用途筛选"), {
    target: { value: "gameplay" },
  });
  fireEvent.change(screen.getByLabelText("视角筛选"), {
    target: { value: "topdown-45" },
  });
  fireEvent.change(screen.getByLabelText("材质筛选"), {
    target: { value: "rice-paper" },
  });

  await waitFor(() => {
    expect(
      fetchMock.mock.calls.some(([path]) => {
        const url = String(path);
        return (
          url.includes("category=character") &&
          url.includes("identity=hero-main") &&
          url.includes("usage=gameplay") &&
          url.includes("viewpoint=topdown-45") &&
          url.includes("material=rice-paper")
        );
      }),
    ).toBe(true);
  });

  fireEvent.click(
    await screen.findByRole("button", { name: "编辑 hero-main" }),
  );
  fireEvent.change(screen.getByLabelText("材质标签"), {
    target: { value: "rice-paper,silk" },
  });
  fireEvent.click(screen.getByRole("button", { name: "保存标签" }));
  await waitFor(() =>
    expect(
      fetchMock.mock.calls.some(
        ([path, options]) =>
          String(path).endsWith("/references/hero-main") &&
          options?.method === "PUT" &&
          String(options.body).includes('\"materials\":[\"rice-paper\",\"silk\"]'),
      ),
    ).toBe(true),
  );

  fireEvent.click(
    await screen.findByRole("button", {
      name: "移除项目副本 hero-main",
    }),
  );
  await waitFor(() =>
    expect(
      fetchMock.mock.calls.some(
        ([path, options]) =>
          String(path).endsWith("/references/hero-main") &&
          options?.method === "DELETE",
      ),
    ).toBe(true),
  );
  expect(confirmMock).toHaveBeenCalledWith(
    "只移除项目副本，不会删除只读源文件。",
  );
});
