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
except ImportError as exc:
    raise SystemExit(
        "fontTools is required. See Assets/Resources/Fonts/README.md for setup."
    ) from exc


PROJECT_ROOT = Path(__file__).resolve().parents[1]
FONT_DIRECTORY = PROJECT_ROOT / "Assets/Resources/Fonts"
CACHE_DIRECTORY = PROJECT_ROOT / "Library/FontToolsCache"
NOTO_CJK_COMMIT = "f8d157532fbfaeda587e826d4cd5b21a49186f7c"
SOURCE_FONT_SPECS = {
    400: (
        CACHE_DIRECTORY / "NotoSansCJKsc-Regular.otf",
        "https://raw.githubusercontent.com/notofonts/noto-cjk/"
        f"{NOTO_CJK_COMMIT}/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf",
        "2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b",
    ),
    700: (
        CACHE_DIRECTORY / "NotoSansCJKsc-Bold.otf",
        "https://raw.githubusercontent.com/notofonts/noto-cjk/"
        f"{NOTO_CJK_COMMIT}/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Bold.otf",
        "b5f0d1a190a7f9b43c310a8850630af12553df32c4c050543f9059732d9b4c0a",
    ),
}

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


def ensure_source_font(weight: int) -> Path:
    source_font, source_url, source_sha256 = SOURCE_FONT_SPECS[weight]
    CACHE_DIRECTORY.mkdir(parents=True, exist_ok=True)
    if source_font.exists() and sha256(source_font) == source_sha256:
        return source_font

    temporary = source_font.with_suffix(".download")
    if temporary.exists():
        temporary.unlink()
    print(f"Downloading pinned Noto Sans CJK SC source to {source_font.relative_to(PROJECT_ROOT)}")
    urllib.request.urlretrieve(source_url, temporary)
    actual_hash = sha256(temporary)
    if actual_hash != source_sha256:
        temporary.unlink()
        raise SystemExit(
            f"Source font checksum mismatch: expected {source_sha256}, got {actual_hash}"
        )
    temporary.replace(source_font)
    return source_font


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
    font = TTFont(source_path, recalcTimestamp=False)

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
    rename_for_unity(font, weight)

    temporary = target_path.with_suffix(".tmp.ttf")
    font.save(temporary)
    font.close()
    temporary.replace(target_path)
    update_unity_import_fingerprint(target_path)
    print(f"Updated {target_path.relative_to(PROJECT_ROOT)} ({target_path.stat().st_size:,} bytes)")


def rename_for_unity(font: TTFont, weight: int) -> None:
    """Give rebuilt subsets a project-owned identity so Unity drops stale native caches."""
    style = "Bold" if weight >= 700 else "Regular"
    family = "Wuxia Sans SC"
    full_name = f"{family} {style}"
    postscript_name = f"WuxiaSansSC-{style}"
    name_table = font["name"]
    for platform_id, encoding_id, language_id in ((3, 1, 0x409), (1, 0, 0)):
        name_table.setName(family, 1, platform_id, encoding_id, language_id)
        name_table.setName(style, 2, platform_id, encoding_id, language_id)
        name_table.setName(full_name, 4, platform_id, encoding_id, language_id)
        name_table.setName(postscript_name, 6, platform_id, encoding_id, language_id)
        name_table.setName(family, 16, platform_id, encoding_id, language_id)
        name_table.setName(style, 17, platform_id, encoding_id, language_id)
    # CFF has a second identity table. Native font renderers can consult it
    # instead of OpenType's name table, so never leave the source Noto name here.
    if "CFF " in font:
        cff = font["CFF "].cff
        cff.fontNames = [postscript_name]
        top = cff.topDictIndex[0]
        top.FullName = full_name
        top.FamilyName = family
        if hasattr(top, "FDArray"):
            for index, dictionary in enumerate(top.FDArray):
                dictionary.FontName = f"{postscript_name}-FD{index}"


def update_unity_import_fingerprint(target_path: Path) -> None:
    meta_path = target_path.with_suffix(target_path.suffix + ".meta")
    if not meta_path.is_file():
        raise SystemExit(f"Unity font metadata is missing: {meta_path}")

    fingerprint = f"wuxia-font-sha256={sha256(target_path)}"
    meta_text = meta_path.read_text(encoding="utf-8")
    meta_text, font_name_count = re.subn(
        r"^  fontNames:\n(?:  - [^\n]*\n)+",
        "  fontNames:\n  - Wuxia Sans SC\n",
        meta_text,
        count=1,
        flags=re.MULTILINE,
    )
    if font_name_count != 1:
        raise SystemExit(f"Unity font metadata has no fontNames block: {meta_path}")

    updated_text, replacement_count = re.subn(
        r"^  userData:.*$",
        f"  userData: {fingerprint}",
        meta_text,
        count=1,
        flags=re.MULTILINE,
    )
    if replacement_count != 1:
        raise SystemExit(f"Unity font metadata has no userData field: {meta_path}")

    meta_path.write_text(updated_text, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--regular-source", type=Path, help="Use a local static Regular OTF.")
    parser.add_argument("--bold-source", type=Path, help="Use a local static Bold OTF.")
    args = parser.parse_args()

    if bool(args.regular_source) != bool(args.bold_source):
        parser.error("--regular-source and --bold-source must be supplied together")

    source_paths = {
        400: args.regular_source.resolve() if args.regular_source else ensure_source_font(400),
        700: args.bold_source.resolve() if args.bold_source else ensure_source_font(700),
    }
    for source_path in source_paths.values():
        if not source_path.is_file():
            parser.error(f"source font does not exist: {source_path}")

    characters = collect_runtime_characters()
    characters.update(collect_gb2312_characters())
    characters.update(collect_existing_characters())

    for weight, source_path in source_paths.items():
        with TTFont(source_path, lazy=True) as source_font:
            supported = set((source_font.getBestCmap() or {}).keys())
        unsupported = characters - supported
        if unsupported:
            preview = " ".join(
                f"{chr(codepoint)}(U+{codepoint:04X})" for codepoint in sorted(unsupported)
            )
            raise SystemExit(
                f"Pinned Noto Sans CJK SC weight {weight} is missing required characters: {preview}"
            )

    print(f"Building Regular and Bold subsets with {len(characters)} Unicode characters")
    for weight, target in FONT_TARGETS:
        build_subset(source_paths[weight], weight, target, characters)
    return 0


if __name__ == "__main__":
    sys.exit(main())
