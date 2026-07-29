import { useEffect, useMemo, useState } from "react";

import type { KonvaEventObject } from "konva/lib/Node";
import { Image as KonvaImage, Layer, Line, Rect, Stage } from "react-konva";

import type { CandidateTransformInput } from "../api/production";

type MaskTool = "brush" | "rectangle";

interface MaskRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface CandidateEditorProps {
  imageUrl: string;
  width: number;
  height: number;
  pending: boolean;
  onTransform: (input: Omit<CandidateTransformInput, "candidate_id">) => void;
  onRepaint: (maskPngBase64: string, instruction: string) => void | Promise<void>;
}

export function CandidateEditor({
  imageUrl,
  width,
  height,
  pending,
  onTransform,
  onRepaint,
}: CandidateEditorProps) {
  const [image, setImage] = useState<HTMLImageElement | null>(null);
  const [tool, setTool] = useState<MaskTool>("brush");
  const [brushes, setBrushes] = useState<number[][]>([]);
  const [rectangles, setRectangles] = useState<MaskRect[]>([]);
  const [activePoints, setActivePoints] = useState<number[] | null>(null);
  const [activeRect, setActiveRect] = useState<MaskRect | null>(null);
  const [inverted, setInverted] = useState(false);
  const [cropEnabled, setCropEnabled] = useState(false);
  const [crop, setCrop] = useState({ x: 0, y: 0, width, height });
  const [outputWidth, setOutputWidth] = useState(String(width));
  const [outputHeight, setOutputHeight] = useState(String(height));
  const [paddingRatio, setPaddingRatio] = useState("0.125");
  const [removeBackground, setRemoveBackground] = useState(false);
  const [instruction, setInstruction] = useState("");
  const stageWidth = 320;
  const stageHeight = Math.max(180, Math.round((height / width) * stageWidth));
  const scaleX = stageWidth / width;
  const scaleY = stageHeight / height;

  useEffect(() => {
    const next = new window.Image();
    next.crossOrigin = "anonymous";
    next.onload = () => setImage(next);
    next.src = imageUrl;
    return () => {
      next.onload = null;
    };
  }, [imageUrl]);

  useEffect(() => {
    setCrop({ x: 0, y: 0, width, height });
    setOutputWidth(String(width));
    setOutputHeight(String(height));
  }, [height, width]);

  const allBrushes = useMemo(
    () => (activePoints ? [...brushes, activePoints] : brushes),
    [activePoints, brushes],
  );
  const allRectangles = useMemo(
    () => (activeRect ? [...rectangles, activeRect] : rectangles),
    [activeRect, rectangles],
  );

  const pointer = (event: KonvaEventObject<MouseEvent | TouchEvent>) =>
    event.target.getStage()?.getPointerPosition() ?? null;

  const beginMask = (event: KonvaEventObject<MouseEvent | TouchEvent>) => {
    const point = pointer(event);
    if (!point) return;
    if (tool === "brush") {
      setActivePoints([point.x, point.y]);
    } else {
      setActiveRect({ x: point.x, y: point.y, width: 0, height: 0 });
    }
  };

  const moveMask = (event: KonvaEventObject<MouseEvent | TouchEvent>) => {
    const point = pointer(event);
    if (!point) return;
    if (activePoints) {
      setActivePoints([...activePoints, point.x, point.y]);
    } else if (activeRect) {
      setActiveRect({
        ...activeRect,
        width: point.x - activeRect.x,
        height: point.y - activeRect.y,
      });
    }
  };

  const finishMask = () => {
    if (activePoints && activePoints.length >= 4) {
      setBrushes([...brushes, activePoints]);
    }
    if (activeRect && Math.abs(activeRect.width) >= 2 && Math.abs(activeRect.height) >= 2) {
      setRectangles([...rectangles, activeRect]);
    }
    setActivePoints(null);
    setActiveRect(null);
  };

  const applyTransform = () => {
    const nextWidth = Number(outputWidth);
    const nextHeight = Number(outputHeight);
    const nextPadding = Number(paddingRatio);
    if (!Number.isFinite(nextWidth) || !Number.isFinite(nextHeight)) return;
    onTransform({
      crop: cropEnabled ? crop : null,
      output_width: nextWidth,
      output_height: nextHeight,
      padding_ratio: nextPadding,
      remove_background: removeBackground,
    });
  };

  const maskBase64 = (): string | null => {
    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext("2d");
    if (!context) return null;
    if (inverted) {
      context.fillStyle = "rgba(220, 30, 30, 1)";
      context.fillRect(0, 0, width, height);
      context.globalCompositeOperation = "destination-out";
    } else {
      context.fillStyle = "rgba(220, 30, 30, 1)";
      context.strokeStyle = "rgba(220, 30, 30, 1)";
    }
    context.scale(width / stageWidth, height / stageHeight);
    context.lineWidth = 20;
    context.lineCap = "round";
    context.lineJoin = "round";
    for (const points of brushes) {
      context.beginPath();
      context.moveTo(points[0], points[1]);
      for (let index = 2; index < points.length; index += 2) {
        context.lineTo(points[index], points[index + 1]);
      }
      context.stroke();
    }
    for (const rectangle of rectangles) {
      context.fillRect(rectangle.x, rectangle.y, rectangle.width, rectangle.height);
    }
    return canvas.toDataURL("image/png").split(",", 2)[1] ?? null;
  };

  const repaint = async () => {
    const encoded = maskBase64();
    if (!encoded || !instruction.trim()) return;
    await onRepaint(encoded, instruction.trim());
  };

  const clearMask = () => {
    setBrushes([]);
    setRectangles([]);
    setActivePoints(null);
    setActiveRect(null);
    setInverted(false);
  };

  return (
    <section className="candidate-editor">
      <div className="candidate-editor__canvas">
        <Stage
          width={stageWidth}
          height={stageHeight}
          onMouseDown={beginMask}
          onMouseMove={moveMask}
          onMouseUp={finishMask}
          onTouchStart={beginMask}
          onTouchMove={moveMask}
          onTouchEnd={finishMask}
        >
          <Layer>
            <Rect width={stageWidth} height={stageHeight} fill="#efe8d3" />
            {image ? (
              <KonvaImage
                image={image}
                width={stageWidth}
                height={stageHeight}
              />
            ) : null}
            {cropEnabled ? (
              <Rect
                x={crop.x * scaleX}
                y={crop.y * scaleY}
                width={crop.width * scaleX}
                height={crop.height * scaleY}
                stroke="#8f2f2b"
                dash={[6, 4]}
              />
            ) : null}
            {allBrushes.map((points, index) => (
              <Line
                key={`brush-${index}`}
                points={points}
                stroke="rgba(180, 25, 25, 0.55)"
                strokeWidth={20}
                lineCap="round"
                lineJoin="round"
              />
            ))}
            {allRectangles.map((rectangle, index) => (
              <Rect
                key={`rect-${index}`}
                {...rectangle}
                fill="rgba(180, 25, 25, 0.45)"
              />
            ))}
          </Layer>
        </Stage>
      </div>

      <div className="candidate-editor__tools">
        <div className="candidate-editor__tool-row">
          <button type="button" onClick={() => setTool("brush")}>画笔蒙版</button>
          <button type="button" onClick={() => setTool("rectangle")}>矩形蒙版</button>
          <button type="button" onClick={() => setInverted(!inverted)}>反选蒙版</button>
          <button type="button" onClick={clearMask}>清空蒙版</button>
        </div>

        <label className="candidate-editor__check">
          <input
            type="checkbox"
            checked={cropEnabled}
            onChange={(event) => setCropEnabled(event.target.checked)}
          />
          启用裁切框
        </label>
        <div className="candidate-editor__numbers">
          {(["x", "y", "width", "height"] as const).map((field) => (
            <label key={field}>
              裁切 {field}
              <input
                type="number"
                min="0"
                value={crop[field]}
                onChange={(event) =>
                  setCrop({ ...crop, [field]: Number(event.target.value) })
                }
              />
            </label>
          ))}
          <label>
            输出宽度
            <input
              type="number"
              min="1"
              max="3840"
              value={outputWidth}
              onChange={(event) => setOutputWidth(event.target.value)}
            />
          </label>
          <label>
            输出高度
            <input
              type="number"
              min="1"
              max="3840"
              value={outputHeight}
              onChange={(event) => setOutputHeight(event.target.value)}
            />
          </label>
          <label>
            透明留白比例
            <input
              type="number"
              min="0"
              max="0.49"
              step="0.025"
              value={paddingRatio}
              onChange={(event) => setPaddingRatio(event.target.value)}
            />
          </label>
        </div>
        <label className="candidate-editor__check">
          <input
            type="checkbox"
            checked={removeBackground}
            onChange={(event) => setRemoveBackground(event.target.checked)}
          />
          重新移除背景
        </label>
        <button type="button" disabled={pending} onClick={applyTransform}>
          应用本地编辑
        </button>

        <label>
          局部重绘指令
          <textarea
            rows={3}
            value={instruction}
            onChange={(event) => setInstruction(event.target.value)}
          />
        </label>
        <button
          type="button"
          disabled={pending || !instruction.trim()}
          onClick={() => void repaint()}
        >
          使用蒙版局部重绘（调用模型）
        </button>
      </div>
    </section>
  );
}
