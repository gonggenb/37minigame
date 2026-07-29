from __future__ import annotations

import re
from pathlib import Path


class PathViolation(ValueError):
    """用户提供的路径越过了允许的工作区边界。"""


SLUG_PATTERN = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*$")


def validate_slug(value: str) -> str:
    if not isinstance(value, str) or not SLUG_PATTERN.fullmatch(value):
        raise PathViolation("value must be a lowercase slug")
    return value


def ensure_within(root: Path, candidate: Path) -> Path:
    root_path = root.resolve()
    candidate_path = candidate.resolve(strict=False)
    try:
        candidate_path.relative_to(root_path)
    except ValueError as error:
        raise PathViolation("path is outside the workspace") from error
    return candidate_path


def safe_child(root: Path, *parts: str) -> Path:
    for part in parts:
        candidate_part = Path(part)
        if candidate_part.is_absolute() or any(
            piece in {"", ".", ".."} for piece in candidate_part.parts
        ):
            raise PathViolation("path segments must be relative and normalized")
    return ensure_within(root, root.joinpath(*parts))


def workspace_root(data_dir: Path) -> Path:
    root = (data_dir / "workspaces").resolve()
    root.mkdir(parents=True, exist_ok=True)
    return root
