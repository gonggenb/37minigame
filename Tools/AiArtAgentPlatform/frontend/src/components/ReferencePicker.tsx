import type { ReferenceAsset } from "../api/stylePack";

export interface ReferencePickerProps {
  references: ReferenceAsset[];
  selectedIds: string[];
  maxSelected?: number;
  onChange: (referenceIds: string[]) => void;
}

export function ReferencePicker({
  references,
  selectedIds,
  maxSelected = 4,
  onChange,
}: ReferencePickerProps) {
  const toggle = (referenceId: string) => {
    if (selectedIds.includes(referenceId)) {
      onChange(selectedIds.filter((item) => item !== referenceId));
      return;
    }
    if (selectedIds.length < maxSelected) {
      onChange([...selectedIds, referenceId]);
    }
  };

  return (
    <div className="reference-picker">
      <div className="reference-picker__heading">
        <strong>任务参考图</strong>
        <span>
          已选 {selectedIds.length}/{maxSelected}
        </span>
      </div>
      <div className="reference-picker__grid">
        {references.map((reference) => {
          const selected = selectedIds.includes(reference.reference_id);
          return (
            <label
              className={
                selected
                  ? "reference-picker__item reference-picker__item--selected"
                  : "reference-picker__item"
              }
              key={reference.reference_id}
            >
              <input
                type="checkbox"
                aria-label={`选择 ${reference.reference_id}`}
                checked={selected}
                disabled={!selected && selectedIds.length >= maxSelected}
                onChange={() => toggle(reference.reference_id)}
              />
              <span>
                <strong>{reference.reference_id}</strong>
                <small>
                  {reference.width} × {reference.height} ·{" "}
                  {reference.viewpoints.join("、") || "未标注视角"}
                </small>
              </span>
            </label>
          );
        })}
      </div>
      {!references.length ? (
        <p className="empty-state">当前类别还没有可用参考图。</p>
      ) : null}
    </div>
  );
}
