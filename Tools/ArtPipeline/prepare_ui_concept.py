#!/usr/bin/env python3
"""Normalize approved UI component masters and derive consistent button states."""
from pathlib import Path
import re
import uuid
from PIL import Image, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "ArtSource/Raw/UI/Concept_20261006"
NORMALIZED = ROOT / "ArtSource/Normalized/UI/Concept_20261006"
RUNTIME = ROOT / "Assets/Resources/UI/Theme"


def normalize(name, size):
    source = Image.open(RAW / name).convert("RGBA")
    alpha = source.getchannel("A")
    bbox = alpha.point(lambda value: 255 if value > 32 else 0).getbbox()
    if not bbox:
        raise ValueError("Empty component master: " + name)
    cropped = source.crop(bbox)
    cropped.putalpha(cropped.getchannel("A").point(lambda value: 0 if value < 16 else value))
    # Nine-slice assets have fixed corner regions; normalize their base geometry once.
    return cropped.resize(size, Image.Resampling.LANCZOS)


def save(image, name, border):
    NORMALIZED.mkdir(parents=True, exist_ok=True)
    image.save(NORMALIZED / name)
    image.save(RUNTIME / name)
    meta = RUNTIME / (name + ".meta")
    if meta.exists():
        return
    template = (RUNTIME / "tex_ui_panel_default_v02.png.meta").read_text()
    template = re.sub(r"guid: [a-f0-9]+", "guid: " + uuid.uuid4().hex, template, count=1)
    template = re.sub(r"spriteBorder: .*", f"spriteBorder: {{x: {border[0]}, y: {border[1]}, z: {border[0]}, w: {border[1]}}}", template)
    template = re.sub(r"maxTextureSize: \d+", "maxTextureSize: 256", template)
    meta.write_text(template)


def main():
    panel = normalize("panel_default_v03.png", (128, 128))
    save(panel, "tex_ui_panel_default_v03.png", (18, 18))
    boss = ImageEnhance.Brightness(panel.convert("RGB")).enhance(1.08)
    boss.putalpha(panel.getchannel("A"))
    save(boss, "tex_ui_panel_boss_v03.png", (20, 20))
    button = normalize("button_master_v03.png", (256, 64))
    states = {"normal": .88, "hover": 1.08, "pressed": .70,
              "selected": 1.12, "primary": 1.0, "primary_hover": 1.16}
    for name, brightness in states.items():
        alpha = button.getchannel("A")
        variant = ImageEnhance.Brightness(button.convert("RGB")).enhance(brightness)
        variant.putalpha(alpha)
        save(variant, f"tex_ui_button_{name}_v03.png", (20, 16))


if __name__ == "__main__":
    main()
