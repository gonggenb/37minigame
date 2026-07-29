from __future__ import annotations

from app.schemas.core import AssetTask
from app.schemas.style_pack import ReferenceAsset


class ReferenceSelector:
    @staticmethod
    def select(
        task: AssetTask,
        references: list[ReferenceAsset],
        *,
        identity_id: str | None = None,
        viewpoint: str | None = None,
        max_references: int = 4,
    ) -> list[ReferenceAsset]:
        if max_references < 1 or max_references > 4:
            raise ValueError("reference selection limit must be between 1 and 4")
        manual_order = {
            reference_id: index for index, reference_id in enumerate(task.reference_ids)
        }
        scored: list[tuple[int, int, str, ReferenceAsset]] = []
        for reference in references:
            is_manual = reference.reference_id in manual_order
            if task.category not in reference.categories and not is_manual:
                continue
            score = 100 if task.category in reference.categories else 0
            if identity_id and ReferenceSelector._contains(reference.identities, identity_id):
                score += 60
            if ReferenceSelector._contains(reference.usages, task.usage):
                score += 30
            if viewpoint and ReferenceSelector._contains(reference.viewpoints, viewpoint):
                score += 20
            manual_rank = manual_order.get(reference.reference_id, len(manual_order))
            if is_manual:
                score += 1000
            scored.append((-score, manual_rank, reference.reference_id, reference))
        scored.sort(key=lambda item: (item[0], item[1], item[2]))
        return [item[3] for item in scored[:max_references]]

    @staticmethod
    def _contains(values: list[str], expected: str) -> bool:
        expected_value = expected.casefold()
        return expected_value in {value.casefold() for value in values}
