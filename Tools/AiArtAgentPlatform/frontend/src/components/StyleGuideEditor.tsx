import { useEffect, useState } from "react";

import type { StyleGuide } from "../api/stylePack";

export interface StyleGuideEditorProps {
  guide: StyleGuide;
  pending: boolean;
  errorMessage?: string;
  onSave: (guide: StyleGuide) => void;
}

function cloneGuide(guide: StyleGuide): StyleGuide {
  return {
    ...guide,
    reference_source: { ...guide.reference_source },
    camera: { ...guide.camera },
    palette: {
      base: [...guide.palette.base],
      accents: [...guide.palette.accents],
    },
    rendering: { ...guide.rendering },
    readability: { ...guide.readability },
    ui: {
      ...guide.ui,
      border_language: [...guide.ui.border_language],
    },
    forbidden: [...guide.forbidden],
  };
}

function joinLines(values: string[]): string {
  return values.join("\n");
}

function splitLines(value: string): string[] {
  return value
    .split(/\r?\n/)
    .map((item) => item.trim())
    .filter(Boolean);
}

export function StyleGuideEditor({
  guide,
  pending,
  errorMessage,
  onSave,
}: StyleGuideEditorProps) {
  const [draft, setDraft] = useState(() => cloneGuide(guide));
  const [basePalette, setBasePalette] = useState(() =>
    joinLines(guide.palette.base),
  );
  const [accentPalette, setAccentPalette] = useState(() =>
    joinLines(guide.palette.accents),
  );
  const [borderLanguage, setBorderLanguage] = useState(() =>
    joinLines(guide.ui.border_language),
  );
  const [forbidden, setForbidden] = useState(() => joinLines(guide.forbidden));
  const [validationError, setValidationError] = useState("");

  useEffect(() => {
    setDraft(cloneGuide(guide));
    setBasePalette(joinLines(guide.palette.base));
    setAccentPalette(joinLines(guide.palette.accents));
    setBorderLanguage(joinLines(guide.ui.border_language));
    setForbidden(joinLines(guide.forbidden));
    setValidationError("");
  }, [guide]);

  const submit = () => {
    if (draft.camera.pitch_semantic_min > draft.camera.pitch_semantic_max) {
      setValidationError("最小俯视角不能大于最大俯视角");
      return;
    }
    setValidationError("");
    onSave({
      ...draft,
      palette: {
        base: splitLines(basePalette),
        accents: splitLines(accentPalette),
      },
      ui: {
        ...draft.ui,
        border_language: splitLines(borderLanguage),
      },
      forbidden: splitLines(forbidden),
    });
  };

  return (
    <div className="style-guide-editor">
      <h3>完整风格圣经</h3>
      <div className="style-guide-editor__grid">
        <label>
          风格 ID
          <input value={draft.style_id} disabled />
        </label>
        <label>
          风格名称
          <input
            value={draft.display_name}
            onChange={(event) =>
              setDraft({ ...draft, display_name: event.target.value })
            }
          />
        </label>
        <label>
          参考源路径
          <input
            value={draft.reference_source.path}
            onChange={(event) =>
              setDraft({
                ...draft,
                reference_source: {
                  ...draft.reference_source,
                  path: event.target.value,
                },
              })
            }
          />
        </label>
        <label>
          参考源模式
          <input value={draft.reference_source.mode} disabled />
        </label>
        <label>
          投影语义
          <input
            value={draft.camera.projection}
            onChange={(event) =>
              setDraft({
                ...draft,
                camera: { ...draft.camera, projection: event.target.value },
              })
            }
          />
        </label>
        <label>
          最小俯视角
          <input
            type="number"
            value={draft.camera.pitch_semantic_min}
            onChange={(event) =>
              setDraft({
                ...draft,
                camera: {
                  ...draft.camera,
                  pitch_semantic_min: Number(event.target.value),
                },
              })
            }
          />
        </label>
        <label>
          最大俯视角
          <input
            type="number"
            value={draft.camera.pitch_semantic_max}
            onChange={(event) =>
              setDraft({
                ...draft,
                camera: {
                  ...draft.camera,
                  pitch_semantic_max: Number(event.target.value),
                },
              })
            }
          />
        </label>
        <label>
          默认朝向
          <input
            value={draft.camera.default_facing}
            onChange={(event) =>
              setDraft({
                ...draft,
                camera: { ...draft.camera, default_facing: event.target.value },
              })
            }
          />
        </label>
        <label className="project-checkbox">
          <input
            type="checkbox"
            checked={draft.camera.shared_view_required}
            onChange={(event) =>
              setDraft({
                ...draft,
                camera: {
                  ...draft.camera,
                  shared_view_required: event.target.checked,
                },
              })
            }
          />
          统一视角
        </label>
        <label>
          基础色（每行一项）
          <textarea
            value={basePalette}
            onChange={(event) => setBasePalette(event.target.value)}
            rows={4}
          />
        </label>
        <label>
          强调色（每行一项）
          <textarea
            value={accentPalette}
            onChange={(event) => setAccentPalette(event.target.value)}
            rows={4}
          />
        </label>
        <label>
          角色比例
          <input
            value={draft.rendering.character_proportion}
            onChange={(event) =>
              setDraft({
                ...draft,
                rendering: {
                  ...draft.rendering,
                  character_proportion: event.target.value,
                },
              })
            }
          />
        </label>
        <label>
          角色轮廓
          <input
            value={draft.rendering.character_outline}
            onChange={(event) =>
              setDraft({
                ...draft,
                rendering: {
                  ...draft.rendering,
                  character_outline: event.target.value,
                },
              })
            }
          />
        </label>
        <label>
          环境细节
          <input
            value={draft.rendering.environment_detail}
            onChange={(event) =>
              setDraft({
                ...draft,
                rendering: {
                  ...draft.rendering,
                  environment_detail: event.target.value,
                },
              })
            }
          />
        </label>
        <label>
          表面质感
          <input
            value={draft.rendering.surface_finish}
            onChange={(event) =>
              setDraft({
                ...draft,
                rendering: {
                  ...draft.rendering,
                  surface_finish: event.target.value,
                },
              })
            }
          />
        </label>
        <label>
          阴影方向
          <input
            value={draft.rendering.shadow_direction}
            onChange={(event) =>
              setDraft({
                ...draft,
                rendering: {
                  ...draft.rendering,
                  shadow_direction: event.target.value,
                },
              })
            }
          />
        </label>
        <label className="project-checkbox">
          <input
            type="checkbox"
            checked={draft.readability.protect_playfield}
            onChange={(event) =>
              setDraft({
                ...draft,
                readability: {
                  ...draft.readability,
                  protect_playfield: event.target.checked,
                },
              })
            }
          />
          保护玩法区域
        </label>
        <label className="project-checkbox">
          <input
            type="checkbox"
            checked={draft.readability.character_contrast_above_environment}
            onChange={(event) =>
              setDraft({
                ...draft,
                readability: {
                  ...draft.readability,
                  character_contrast_above_environment: event.target.checked,
                },
              })
            }
          />
          角色对比高于环境
        </label>
        <label className="project-checkbox">
          <input
            type="checkbox"
            checked={draft.readability.preserve_clear_silhouette}
            onChange={(event) =>
              setDraft({
                ...draft,
                readability: {
                  ...draft.readability,
                  preserve_clear_silhouette: event.target.checked,
                },
              })
            }
          />
          保持清晰剪影
        </label>
        <label className="project-checkbox">
          <input
            type="checkbox"
            checked={draft.readability.avoid_high_frequency_ground_noise}
            onChange={(event) =>
              setDraft({
                ...draft,
                readability: {
                  ...draft.readability,
                  avoid_high_frequency_ground_noise: event.target.checked,
                },
              })
            }
          />
          避免高频地面噪点
        </label>
        <label className="project-checkbox">
          <input
            type="checkbox"
            checked={draft.ui.formal_text_baked_in}
            onChange={(event) =>
              setDraft({
                ...draft,
                ui: { ...draft.ui, formal_text_baked_in: event.target.checked },
              })
            }
          />
          正式文字烘焙进图片
        </label>
        <label>
          UI 边框语言（每行一项）
          <textarea
            value={borderLanguage}
            onChange={(event) => setBorderLanguage(event.target.value)}
            rows={4}
          />
        </label>
        <label>
          禁止项（每行一项）
          <textarea
            value={forbidden}
            onChange={(event) => setForbidden(event.target.value)}
            rows={5}
          />
        </label>
      </div>
      <button
        type="button"
        disabled={pending || !draft.display_name.trim()}
        onClick={submit}
      >
        {pending ? "正在保存…" : "保存风格圣经"}
      </button>
      {validationError ? (
        <p className="model-test-error">{validationError}</p>
      ) : null}
      {errorMessage ? <p className="model-test-error">{errorMessage}</p> : null}
    </div>
  );
}
