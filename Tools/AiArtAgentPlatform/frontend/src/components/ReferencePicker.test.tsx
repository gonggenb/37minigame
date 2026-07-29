import { fireEvent, render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";

import { ReferencePicker } from "./ReferencePicker";
import type { ReferenceAsset } from "../api/stylePack";

function reference(id: string): ReferenceAsset {
  return {
    reference_id: id,
    source_relative_path: `${id}.png`,
    workspace_relative_path: `style-pack/references/${id}.png`,
    thumbnail_relative_path: `style-pack/thumbnails/${id}.png`,
    sha256: id.padEnd(64, "a"),
    width: 256,
    height: 256,
    categories: ["item"],
    identities: [],
    usages: ["gameplay"],
    viewpoints: ["topdown-45"],
    materials: [],
    notes: "",
  };
}

it("limits a task to four references while allowing replacement", () => {
  const onChange = vi.fn();
  render(
    <ReferencePicker
      references={[1, 2, 3, 4, 5].map((index) => reference(`ref-${index}`))}
      selectedIds={["ref-1", "ref-2", "ref-3", "ref-4"]}
      onChange={onChange}
    />,
  );

  expect(screen.getByLabelText("选择 ref-5")).toBeDisabled();
  fireEvent.click(screen.getByLabelText("选择 ref-1"));
  expect(onChange).toHaveBeenLastCalledWith(["ref-2", "ref-3", "ref-4"]);
});

it("adds a newly available reference after one is removed", () => {
  const onChange = vi.fn();
  const references = [1, 2, 3, 4, 5].map((index) => reference(`ref-${index}`));
  const { rerender } = render(
    <ReferencePicker
      references={references}
      selectedIds={["ref-1", "ref-2", "ref-3", "ref-4"]}
      onChange={onChange}
    />,
  );

  rerender(
    <ReferencePicker
      references={references}
      selectedIds={["ref-2", "ref-3", "ref-4"]}
      onChange={onChange}
    />,
  );
  expect(screen.getByLabelText("选择 ref-5")).not.toBeDisabled();
  fireEvent.click(screen.getByLabelText("选择 ref-5"));
  expect(onChange).toHaveBeenLastCalledWith([
    "ref-2",
    "ref-3",
    "ref-4",
    "ref-5",
  ]);
});
