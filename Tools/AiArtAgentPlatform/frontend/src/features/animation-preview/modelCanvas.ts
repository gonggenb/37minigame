export const ACTION_TEMPLATES = {
  idle: { frameCount: 4, rows: 2, columns: 2, previewFps: 8, loop: true },
  move: { frameCount: 8, rows: 2, columns: 4, previewFps: 12, loop: true },
  attack: { frameCount: 6, rows: 2, columns: 3, previewFps: 12, loop: false },
  hit: { frameCount: 4, rows: 2, columns: 2, previewFps: 12, loop: false },
  death: { frameCount: 8, rows: 2, columns: 4, previewFps: 12, loop: false },
} as const;

export function calculateCanvas(
  rows: number,
  columns: number,
  frameWidth: number,
  frameHeight: number,
) {
  return { width: columns * frameWidth, height: rows * frameHeight };
}

export function validateGptImage2Canvas(
  width: number,
  height: number,
): string | null {
  if (width > 3840 || height > 3840) {
    return `模型画布 ${width} × ${height}：单边不得超过 3840 px。`;
  }
  if (width % 16 !== 0 || height % 16 !== 0) {
    return `模型画布 ${width} × ${height}：宽高必须是 16 的倍数。`;
  }
  if (Math.max(width, height) / Math.min(width, height) > 3) {
    return `模型画布 ${width} × ${height}：长短边比例不得超过 3:1。`;
  }
  const pixels = width * height;
  if (pixels < 655_360) {
    return `模型画布 ${width} × ${height}：总像素不得低于 655,360。`;
  }
  if (pixels > 8_294_400) {
    return `模型画布 ${width} × ${height}：总像素不得超过 8,294,400。`;
  }
  return null;
}
