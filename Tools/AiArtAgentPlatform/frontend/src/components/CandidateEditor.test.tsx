import type { PropsWithChildren } from "react";

import { fireEvent, render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";

import { CandidateEditor } from "./CandidateEditor";

vi.mock("react-konva", () => ({
  Stage: ({ children }: PropsWithChildren) => (
    <div data-testid="konva-stage">{children}</div>
  ),
  Layer: ({ children }: PropsWithChildren) => <div>{children}</div>,
  Rect: () => null,
  Line: () => null,
  Image: () => null,
}));

it("exposes crop, scale, padding, background and mask tools", () => {
  const onTransform = vi.fn();
  render(
    <CandidateEditor
      imageUrl="/candidate.png"
      width={128}
      height={128}
      pending={false}
      onTransform={onTransform}
      onRepaint={vi.fn()}
    />,
  );

  expect(screen.getByTestId("konva-stage")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "画笔蒙版" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "矩形蒙版" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "反选蒙版" })).toBeInTheDocument();

  fireEvent.change(screen.getByLabelText("输出宽度"), {
    target: { value: "192" },
  });
  fireEvent.change(screen.getByLabelText("透明留白比例"), {
    target: { value: "0.2" },
  });
  fireEvent.click(screen.getByLabelText("重新移除背景"));
  fireEvent.click(screen.getByRole("button", { name: "应用本地编辑" }));

  expect(onTransform).toHaveBeenCalledWith(
    expect.objectContaining({
      output_width: 192,
      output_height: 128,
      padding_ratio: 0.2,
      remove_background: true,
    }),
  );
});
