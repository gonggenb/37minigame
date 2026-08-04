#!/usr/bin/env python3
"""Split generated icon atlases, remove chroma key, and build Unity runtime icons."""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image


ATLASES = {
    "arts_core.png": (5, 2, [
        "art_sword_qi", "art_swift_sword", "art_armor_break", "art_shadow_chain_sword", "art_venom_palm",
        "art_hundred_venoms", "art_star_drain", "art_poison_mist", "art_iron_shirt", "art_golden_bell",
    ]),
    "arts_advanced.png": (5, 2, [
        "art_retaliation", "art_immovable_king", "art_snowless_step", "art_swan_strike", "art_cloud_step",
        "art_formless_shadow", "art_blood_drinking_blade", "art_bloody_battle", "art_boiling_blood", "art_asura_domain",
    ]),
    "secrets_store.png": (5, 2, [
        "secret_poisoned_edge", "secret_poison_blood", "secret_blood_armor", "secret_shadow_bell", "secret_wind_pursuit",
        "store_upgrade", "store_equipment", "store_consumable", "store_refresh", "store_transform",
    ]),
    "equipment.png": (5, 3, [
        "equipment_qinggang_sword", "equipment_light_scale", "equipment_practice_bracer", "equipment_black_iron_ring", "equipment_wanderer_cloak",
        "equipment_poison_dart_pouch", "equipment_wind_chaser_sword", "equipment_bone_rot_gloves", "equipment_black_tortoise_armor", "equipment_nightwalker_cloak",
        "equipment_blood_drinking_blade", "equipment_poison_needle_case", "equipment_mountain_bracer", "equipment_swallow_boots", "equipment_crimson_heart_pendant",
    ]),
    "relics_consumables.png": (5, 3, [
        "relic_compass", "relic_abacus", "relic_meditation_mat", "relic_broken_sword_tassel", "relic_toad_jade",
        "relic_mountain_bell", "relic_shadow_jade", "relic_blood_marrow_pearl", "consumable_healing_salve", "consumable_tiger_bone_pill",
        "consumable_lightness_powder", "consumable_red_sun_pill", "consumable_foundation_pill", "consumable_insight_incense",
    ]),
    "caves.png": (4, 3, [
        "cave_enemy", "cave_merchant", "cave_treasure", "cave_altar",
        "cave_trial", "cave_healer", "cave_library", "cave_forge",
        "cave_gambler", "cave_herb_garden", "cave_relic_shrine",
    ]),
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--atlas-dir", type=Path, required=True)
    parser.add_argument("--master-dir", type=Path, required=True)
    parser.add_argument("--runtime-dir", type=Path, required=True)
    parser.add_argument("--key-helper", type=Path, required=True)
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    args.master_dir.mkdir(parents=True, exist_ok=True)
    args.runtime_dir.mkdir(parents=True, exist_ok=True)
    created = []

    with tempfile.TemporaryDirectory(prefix="wuxia-icons-") as temp_dir_value:
        temp_dir = Path(temp_dir_value)
        for atlas_name, (columns, rows, icon_names) in ATLASES.items():
            source_path = args.atlas_dir / atlas_name
            atlas = Image.open(source_path).convert("RGB")
            cell_width = atlas.width / columns
            cell_height = atlas.height / rows
            for index, icon_name in enumerate(icon_names):
                column = index % columns
                row = index // columns
                left = round(column * cell_width)
                top = round(row * cell_height)
                right = round((column + 1) * cell_width)
                bottom = round((row + 1) * cell_height)
                keyed_path = temp_dir / f"{icon_name}-key.png"
                master_path = args.master_dir / f"{icon_name}.png"
                runtime_path = args.runtime_dir / f"{icon_name}.png"
                atlas.crop((left, top, right, bottom)).resize((256, 256), Image.Resampling.LANCZOS).save(keyed_path)
                subprocess.run([
                    sys.executable, str(args.key_helper),
                    "--input", str(keyed_path),
                    "--out", str(master_path),
                    "--auto-key", "border",
                    "--tolerance", "42",
                    "--edge-contract", "1",
                    "--despill",
                    "--force",
                ], check=True, capture_output=True, text=True)

                master = Image.open(master_path).convert("RGBA")
                alpha = master.getchannel("A")
                alpha_values = list(alpha.getdata())
                transparent_ratio = sum(value == 0 for value in alpha_values) / len(alpha_values)
                opaque_ratio = sum(value >= 245 for value in alpha_values) / len(alpha_values)
                visible_ratio = sum(value >= 32 for value in alpha_values) / len(alpha_values)
                if transparent_ratio < 0.08 or visible_ratio < 0.05:
                    raise RuntimeError(
                        f"Alpha validation failed for {icon_name}: "
                        f"transparent={transparent_ratio:.3f}, opaque={opaque_ratio:.3f}, visible={visible_ratio:.3f}")

                master.resize((128, 128), Image.Resampling.LANCZOS).save(runtime_path)
                created.append(icon_name)

    print(f"Created {len(created)} transparent icons")
    print("\n".join(created))


if __name__ == "__main__":
    main()
