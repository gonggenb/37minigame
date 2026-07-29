# Noto Sans CJK SC subset

This folder contains project-specific TrueType subsets of:

- `Noto Sans SC Regular`
- `Noto Sans SC Bold`

Source:

- https://fonts.google.com/noto/specimen/Noto+Sans+SC
- https://developers.google.com/fonts/docs/css2

License:

- SIL Open Font License 1.1
- See `OFL-NotoSansCJK.txt` in this folder.

The subset files retain the Latin, punctuation, symbol, and CJK characters
currently referenced by the runtime scripts, scenes, Resources, and configured
data assets. The existing subset character set is also retained when the files
are refreshed, so an unrelated text cleanup does not remove previously shipped
glyphs.

Regenerate both subsets whenever new runtime text introduces characters that
are not already covered. WebGL cannot rely on the operating system to supply
missing Chinese glyphs.
