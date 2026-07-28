import { fireEvent, render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";

import { StyleGuideEditor } from "./StyleGuideEditor";
import type { StyleGuide } from "../api/stylePack";

const guide: StyleGuide = {
  schema_version: 1,
  style_id: "wuxia-ink-chibi-topdown-2_5d",
  display_name: "Q版水墨武侠俯视角",
  reference_source: { path: "D:/reference", mode: "read_only" },
  camera: {
    projection: "orthographic_like",
    pitch_semantic_min: 35,
    pitch_semantic_max: 55,
    shared_view_required: true,
    default_facing: "right",
  },
  palette: { base: ["rice_paper"], accents: ["vermilion"] },
  rendering: {
    character_proportion: "chibi_wuxia",
    character_outline: "clean_ink",
    environment_detail: "restrained",
    surface_finish: "matte_painted_2d",
    shadow_direction: "lower_right",
  },
  readability: {
    protect_playfield: true,
    character_contrast_above_environment: true,
    preserve_clear_silhouette: true,
    avoid_high_frequency_ground_noise: true,
  },
  ui: { formal_text_baked_in: false, border_language: ["ink_edge"] },
  forbidden: ["pixel_art"],
};

it("validates camera angles and saves every style-guide field", () => {
  const onSave = vi.fn();
  render(<StyleGuideEditor guide={guide} pending={false} onSave={onSave} />);

  expect(screen.getByLabelText("风格 ID")).toBeDisabled();
  expect(screen.getByLabelText("参考源模式")).toBeDisabled();

  fireEvent.change(screen.getByLabelText("风格名称"), {
    target: { value: "新版水墨武侠" },
  });
  fireEvent.change(screen.getByLabelText("基础色（每行一项）"), {
    target: { value: "rice_paper\nink_gray\nmoss_green" },
  });
  fireEvent.change(screen.getByLabelText("最小俯视角"), {
    target: { value: "60" },
  });
  fireEvent.change(screen.getByLabelText("最大俯视角"), {
    target: { value: "40" },
  });
  fireEvent.click(screen.getByRole("button", { name: "保存风格圣经" }));

  expect(screen.getByText("最小俯视角不能大于最大俯视角")).toBeInTheDocument();
  expect(onSave).not.toHaveBeenCalled();

  fireEvent.change(screen.getByLabelText("最大俯视角"), {
    target: { value: "65" },
  });
  fireEvent.change(screen.getByLabelText("强调色（每行一项）"), {
    target: { value: "vermilion\njade_green" },
  });
  fireEvent.change(screen.getByLabelText("UI 边框语言（每行一项）"), {
    target: { value: "ink_edge\nscroll_corner" },
  });
  fireEvent.change(screen.getByLabelText("禁止项（每行一项）"), {
    target: { value: "pixel_art\nphotorealistic" },
  });
  fireEvent.click(screen.getByRole("button", { name: "保存风格圣经" }));

  expect(onSave).toHaveBeenCalledWith({
    ...guide,
    display_name: "新版水墨武侠",
    camera: {
      ...guide.camera,
      pitch_semantic_min: 60,
      pitch_semantic_max: 65,
    },
    palette: {
      base: ["rice_paper", "ink_gray", "moss_green"],
      accents: ["vermilion", "jade_green"],
    },
    ui: {
      ...guide.ui,
      border_language: ["ink_edge", "scroll_corner"],
    },
    forbidden: ["pixel_art", "photorealistic"],
  });
});

it("renders all editable rendering and readability controls", () => {
  render(<StyleGuideEditor guide={guide} pending={false} onSave={vi.fn()} />);

  expect(screen.getByLabelText("投影语义")).toHaveValue("orthographic_like");
  expect(screen.getByLabelText("默认朝向")).toHaveValue("right");
  expect(screen.getByLabelText("角色比例")).toHaveValue("chibi_wuxia");
  expect(screen.getByLabelText("角色轮廓")).toHaveValue("clean_ink");
  expect(screen.getByLabelText("环境细节")).toHaveValue("restrained");
  expect(screen.getByLabelText("表面质感")).toHaveValue("matte_painted_2d");
  expect(screen.getByLabelText("阴影方向")).toHaveValue("lower_right");
  expect(screen.getByLabelText("统一视角")).toBeChecked();
  expect(screen.getByLabelText("保护玩法区域")).toBeChecked();
  expect(screen.getByLabelText("角色对比高于环境")).toBeChecked();
  expect(screen.getByLabelText("保持清晰剪影")).toBeChecked();
  expect(screen.getByLabelText("避免高频地面噪点")).toBeChecked();
  expect(screen.getByLabelText("正式文字烘焙进图片")).not.toBeChecked();
});
