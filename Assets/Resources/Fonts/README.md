# Wuxia Sans SC subsets

This folder contains project-specific OpenType/CFF subsets of:

- `Noto Sans CJK SC Regular`
- `Noto Sans CJK SC Bold`

Source:

- https://fonts.google.com/noto/specimen/Noto+Sans+SC
- https://developers.google.com/fonts/docs/css2

License:

- SIL Open Font License 1.1
- See `OFL-NotoSansCJK.txt` in this folder.

The subset files retain the Latin, punctuation, symbol, and CJK characters
currently referenced by runtime scripts and serialized assets. They also include
the GB2312 baseline character set, so ordinary Simplified Chinese copy does not
require a font rebuild every time. Existing subset characters are retained when
the files are refreshed, so unrelated text cleanup does not remove previously
shipped glyphs.

Regenerate both subsets whenever new runtime text introduces characters that
are not already covered. No target may rely on the operating system to supply
missing Chinese glyphs; WebGL has no such fallback at all.

From the project root:

```bash
python3 -m venv /tmp/wuxia-font-tools
/tmp/wuxia-font-tools/bin/pip install "fonttools==4.59.0"
/tmp/wuxia-font-tools/bin/python Tools/update_chinese_font_subset.py
```

The update tool downloads checksum-pinned official static `Noto Sans CJK SC`
Regular and Bold fonts from a pinned repository commit into
`Library/FontToolsCache`, then rewrites both project subsets. Do not switch this
pipeline back to a variable-font instance: Unity's legacy `Font` importer can
accept that file while rendering incorrect CJK outlines. Generated fonts use
compact glyph IDs and the project-owned internal family name `Wuxia Sans SC`;
this prevents Unity's native font cache from reusing an older Noto subset under
the same internal identity. The tool also synchronizes that family name and the
TTF SHA-256 fingerprint into each matching Unity `.meta` file so an out-of-date
import is rejected. Source fonts are not added to the build.

After a Unity script reload, `WebGLChineseFontBuildValidator` checks runtime C#
strings plus serialized scenes, Prefabs, assets, Resources, UI Toolkit files,
and configured data. It reports missing glyphs immediately. Every platform build
also fails when either font subset is missing a required non-ASCII glyph or when
`Include Font Data` is disabled, or when a font binary no longer matches its
Unity import fingerprint. Run `37 MiniGame/Validate Chinese Fonts` from the Unity
menu for an explicit check before Play Mode or packaging.
