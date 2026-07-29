import { describe, expect, it } from "vitest";

import {
  ACTION_TEMPLATES,
  calculateCanvas,
  validateGptImage2Canvas,
} from "./modelCanvas";

describe("sequence model canvas", () => {
  it("uses legal two-dimensional grids for all five actions", () => {
    expect(ACTION_TEMPLATES.idle).toMatchObject({ frameCount: 4, rows: 2, columns: 2 });
    expect(ACTION_TEMPLATES.move).toMatchObject({ frameCount: 8, rows: 2, columns: 4 });
    expect(ACTION_TEMPLATES.attack).toMatchObject({ frameCount: 6, rows: 2, columns: 3 });
    expect(ACTION_TEMPLATES.hit).toMatchObject({ frameCount: 4, rows: 2, columns: 2 });
    expect(ACTION_TEMPLATES.death).toMatchObject({ frameCount: 8, rows: 2, columns: 4 });
  });

  it("calculates model and final canvases independently", () => {
    expect(calculateCanvas(2, 4, 512, 512)).toEqual({ width: 2048, height: 1024 });
    expect(calculateCanvas(2, 4, 256, 256)).toEqual({ width: 1024, height: 512 });
  });

  it.each([
    [1025, 1024, "16"],
    [3072, 768, "3:1"],
    [768, 768, "655,360"],
    [3840, 2304, "8,294,400"],
  ])("reports an actionable invalid canvas reason", (width, height, reason) => {
    expect(validateGptImage2Canvas(width, height)).toContain(reason);
  });

  it("accepts the default model canvas", () => {
    expect(validateGptImage2Canvas(2048, 1024)).toBeNull();
  });
});
