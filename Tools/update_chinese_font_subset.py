#!/usr/bin/env python3
"""Regenerate the bundled Noto Sans SC Regular/Bold runtime font subsets."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
import urllib.request
from pathlib import Path

try:
    from fontTools import subset
    from fontTools.ttLib import TTFont
    from fontTools.varLib.instancer import instantiateVariableFont
except ImportError as exc:
    raise SystemExit(
        "fontTools is required. See Assets/Resources/Fonts/README.md for setup."
    ) from exc


PROJECT_ROOT = Path(__file__).resolve().parents[1]
FONT_DIRECTORY = PROJECT_ROOT / "Assets/Resources/Fonts"
CACHE_DIRECTORY = PROJECT_ROOT / "Library/FontToolsCache"
SOURCE_FONT = CACHE_DIRECTORY / "NotoSansSC-variable.ttf"
SOURCE_URL = (
    "https://raw.githubusercontent.com/google/fonts/"
    "cf9c1365d6c23d557af4a7f7d1186bafb73f6567/"
    "ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf"
)
SOURCE_SHA256 = "a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da"

FONT_TARGETS = (
    (400, FONT_DIRECTORY / "NotoSansCJKsc-Regular-Subset.ttf"),
    (700, FONT_DIRECTORY / "NotoSansCJKsc-Bold-Subset.ttf"),
)

SERIALIZED_EXTENSIONS = {".asset", ".json", ".prefab", ".txt", ".unity", ".uss", ".uxml"}
CSHARP_STRING_LITERAL = re.compile(r'(?:\$?@|@\$)?"(?:""|\\.|[^"])*"', re.DOTALL)
ESCAPED_UNICODE = re.compile(r"\\u([0-9a-fA-F]{4})")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def ensure_source_font() -> Path:
    CACHE_DIRECTORY.mkdir(parents=True, exist_ok=True)
    if SOURCE_FONT.exists() and sha256(SOURCE_FONT) == SOURCE_SHA256:
        return SOURCE_FONT

    temporary = SOURCE_FONT.with_suffix(".download")
    if temporary.exists():
        temporary.unlink()
    print(f"Downloading pinned Noto Sans SC source to {SOURCE_FONT.relative_to(PROJECT_ROOT)}")
    urllib.request.urlretrieve(SOURCE_URL, temporary)
    actual_hash = sha256(temporary)
    if actual_hash != SOURCE_SHA256:
        temporary.unlink()
        raise SystemExit(
            f"Source font checksum mismatch: expected {SOURCE_SHA256}, got {actual_hash}"
        )
    temporary.replace(SOURCE_FONT)
    return SOURCE_FONT


def is_editor_only(path: Path) -> bool:
    relative_parts = path.relative_to(PROJECT_ROOT / "Assets").parts
    return any(part.casefold() == "editor" for part in relative_parts[:-1])


def collect_visible_characters(text: str, characters: set[int]) -> None:
    characters.update(ord(character) for character in text if ord(character) > 127)
    characters.update(int(match.group(1), 16) for match in ESCAPED_UNICODE.finditer(text))


def collect_runtime_characters() -> set[int]:
    characters: set[int] = set(range(0x20, 0x7F))
    for path in (PROJECT_ROOT / "Assets").rglob("*"):
        if not path.is_file() or is_editor_only(path):
            continue

        extension = path.suffix.casefold()
        if extension != ".cs" and extension not in SERIALIZED_EXTENSIONS:
            continue

        try:
            text = path.read_text(encoding="utf-8-sig")
        except (UnicodeDecodeError, OSError):
            continue

        if extension == ".cs":
            for match in CSHARP_STRING_LITERAL.finditer(text):
                collect_visible_characters(match.group(0), characters)
        else:
            collect_visible_characters(text, characters)
    return characters


def collect_gb2312_characters() -> set[int]:
    characters: set[int] = set()
    for lead in range(0xA1, 0xF8):
        for trail in range(0xA1, 0xFF):
            try:
                decoded = bytes((lead, trail)).decode("gb2312")
            except UnicodeDecodeError:
                continue
            if len(decoded) == 1:
                characters.add(ord(decoded))
    return characters


def collect_existing_characters() -> set[int]:
    characters: set[int] = set()
    for _, target in FONT_TARGETS:
        if not target.exists():
            continue
        with TTFont(target, lazy=True) as font:
            characters.update((font.getBestCmap() or {}).keys())
    return characters


def build_subset(source_path: Path, weight: int, target_path: Path, characters: set[int]) -> None:
    font = TTFont(source_path)
    instantiateVariableFont(
        font,
        {"wght": weight},
        inplace=True,
        optimize=True,
        updateFontNames=True,
    )

    options = subset.Options()
    options.glyph_names = True
    options.layout_features = ["*"]
    options.name_IDs = [0, 1, 2, 3, 4, 5, 6, 13, 14, 16, 17]
    options.name_legacy = True
    options.name_languages = ["*"]
    options.notdef_glyph = True
    options.notdef_outline = True
    options.recommended_glyphs = True

    subsetter = subset.Subsetter(options=options)
    subsetter.populate(unicodes=characters)
    subsetter.subset(font)

    temporary = target_path.with_suffix(".tmp.ttf")
    font.save(temporary)
    font.close()
    temporary.replace(target_path)
    print(f"Updated {target_path.relative_to(PROJECT_ROOT)} ({target_path.stat().st_size:,} bytes)")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--source",
        type=Path,
        help="Use a local Noto Sans SC variable TTF instead of the pinned download.",
    )
    args = parser.parse_args()

    source_path = args.source.resolve() if args.source else ensure_source_font()
    if not source_path.is_file():
        parser.error(f"source font does not exist: {source_path}")

    characters = collect_runtime_characters()
    characters.update(collect_gb2312_characters())
    characters.update(collect_existing_characters())

    with TTFont(source_path, lazy=True) as source_font:
        supported = set((source_font.getBestCmap() or {}).keys())
    unsupported = characters - supported
    if unsupported:
        preview = " ".join(f"{chr(codepoint)}(U+{codepoint:04X})" for codepoint in sorted(unsupported))
        raise SystemExit(f"Pinned Noto Sans SC source is missing required characters: {preview}")

    print(f"Building Regular and Bold subsets with {len(characters)} Unicode characters")
    for weight, target in FONT_TARGETS:
        build_subset(source_path, weight, target, characters)
    return 0


if __name__ == "__main__":
    sys.exit(main())
