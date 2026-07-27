#!/usr/bin/env python3
"""Generate the adaptive wuxia music set used by MainPrototype."""

from __future__ import annotations

import argparse
import math
from pathlib import Path

import numpy as np

from generate_wuxia_urgent_bgm import (
    SAMPLE_RATE,
    add_echo,
    add_mono,
    erhu,
    flute,
    gong,
    low_drone,
    pluck,
    rim_hit,
    rising_wind,
    war_drum,
    write_wav,
)


def empty_mix(duration: float) -> np.ndarray:
    return np.zeros((int(round(duration * SAMPLE_RATE)), 2), dtype=np.float64)


def finalize(
    mix: np.ndarray,
    peak_db: float = -3.0,
    loopable: bool = False,
    echo_delay: float | None = None,
    echo_feedback: float = 0.1,
) -> np.ndarray:
    if echo_delay is not None:
        add_echo(mix, echo_delay, echo_feedback)

    mix -= np.mean(mix, axis=0, keepdims=True)
    mix = np.tanh(mix * 1.08)

    if loopable:
        # A very short equal-power edge prevents a click while keeping the
        # loop's rhythmic downbeat intact.
        seam = min(int(0.008 * SAMPLE_RATE), len(mix) // 8)
        phase = np.linspace(0.0, math.pi * 0.5, seam, dtype=np.float64)[:, None]
        mix[:seam] *= np.sin(phase)
        mix[-seam:] *= np.cos(phase)
    else:
        fade_in = min(int(0.025 * SAMPLE_RATE), len(mix) // 8)
        fade_out = min(int(0.12 * SAMPLE_RATE), len(mix) // 6)
        mix[:fade_in] *= np.linspace(0.0, 1.0, fade_in)[:, None]
        mix[-fade_out:] *= np.linspace(1.0, 0.0, fade_out)[:, None]

    target_peak = 10.0 ** (peak_db / 20.0)
    peak = float(np.max(np.abs(mix)))
    if peak > 0.0:
        mix *= target_peak / peak
    return mix


def compose_normal_battle_stem() -> np.ndarray:
    duration = 15.0  # Eight bars at 128 BPM.
    beat = 60.0 / 128.0
    mix = empty_mix(duration)

    for beat_index in range(32):
        time = beat_index * beat
        accent = 1.0 if beat_index % 4 in (0, 2) else 0.62
        add_mono(mix, war_drum(accent), time, 0.38 if accent == 1.0 else 0.24, -0.08)
        add_mono(mix, rim_hit(), time + beat * 0.5, 0.12, 0.28)
        if beat_index % 2 == 1:
            add_mono(mix, pluck(50 if beat_index % 4 == 1 else 55, 0.25, 0.75), time, 0.13, -0.25)

    for bar in range(8):
        if bar in (0, 4):
            add_mono(mix, gong(), bar * beat * 4.0, 0.075, 0.0)

    return finalize(mix, peak_db=-4.5, loopable=True, echo_delay=0.117, echo_feedback=0.07)


def compose_cave_music() -> np.ndarray:
    duration = 32.0  # Eight slow bars at 60 BPM.
    bar = 4.0
    mix = empty_mix(duration)

    # A restrained low foundation leaves room for cave ambience and UI sounds.
    for section in range(4):
        note = 38 if section in (0, 3) else 33
        add_mono(mix, low_drone(note, 8.4), section * 8.0, 0.10, -0.06)

    # One muted guqin-like note per bar; silence is part of the cue.
    cave_pattern = (50, 55, 53, 48, 50, 57, 55, 50)
    for bar_index, note in enumerate(cave_pattern):
        start = bar_index * bar
        pan = -0.18 if bar_index % 2 == 0 else 0.18
        add_mono(mix, pluck(note, 1.8, 0.34), start + 0.72, 0.085, pan)

    # Two distant answers provide mystery without turning into a melody lead.
    add_mono(mix, pluck(62, 2.2, 0.28), 10.6, 0.045, 0.24)
    add_mono(mix, pluck(60, 2.2, 0.28), 26.4, 0.045, -0.24)

    return finalize(mix, peak_db=-9.0, loopable=True, echo_delay=0.72, echo_feedback=0.07)


def compose_cave_combat_stem() -> np.ndarray:
    duration = 16.0  # Six bars at 90 BPM.
    beat = 60.0 / 90.0
    mix = empty_mix(duration)

    for beat_index in range(24):
        time = beat_index * beat
        if beat_index % 4 in (0, 2):
            add_mono(mix, war_drum(0.78 if beat_index % 4 == 0 else 0.62), time, 0.24, -0.06)
        add_mono(mix, rim_hit(), time + beat * 0.5, 0.075, 0.34)
        if beat_index % 4 == 3:
            add_mono(mix, pluck(45, 0.34, 0.62), time, 0.11, -0.3)

    return finalize(mix, peak_db=-6.0, loopable=True, echo_delay=0.19, echo_feedback=0.07)


def compose_boss_intro() -> np.ndarray:
    mix = empty_mix(4.0)
    add_mono(mix, gong(), 0.0, 0.44, 0.0)
    add_mono(mix, rising_wind(3.6), 0.0, 0.38, 0.0)
    add_mono(mix, erhu(69, 3.7, -2.0), 0.15, 0.33, -0.18)
    for index, time in enumerate((0.0, 1.45, 2.5, 3.25)):
        add_mono(mix, war_drum(1.0 if index in (0, 3) else 0.72), time, 0.34, 0.08)
    return finalize(mix, peak_db=-2.8, loopable=False, echo_delay=0.21, echo_feedback=0.09)


def compose_boss_loop() -> np.ndarray:
    duration = 48.0  # Twenty-four bars at 120 BPM.
    beat = 0.5
    bar = 2.0
    mix = empty_mix(duration)

    for section in range(6):
        add_mono(mix, low_drone(38 if section % 2 == 0 else 45, 8.2), section * 8.0, 0.29, -0.08)

    ostinato = (50, 57, 53, 60, 55, 62, 57, 53)
    for bar_index in range(24):
        for step, note in enumerate(ostinato):
            time = bar_index * bar + step * (bar / 8.0)
            gain = 0.30 + 0.035 * (bar_index // 8)
            add_mono(mix, pluck(note, 0.34, 0.95), time, gain, -0.34 + (step % 4) * 0.20)

        for beat_index in range(4):
            time = bar_index * bar + beat_index * beat
            accent = 1.0 if beat_index in (0, 2) else 0.64
            add_mono(mix, war_drum(accent), time, 0.34 if accent == 1.0 else 0.20, 0.05)
            add_mono(mix, rim_hit(), time + beat * 0.5, 0.085, 0.34)

        if bar_index in (0, 8, 16):
            add_mono(mix, gong(), bar_index * bar, 0.13 + 0.025 * (bar_index // 8), 0.0)

    erhu_motif = (
        (0.0, 69, 2.0, 0.0),
        (2.0, 72, 1.0, 2.0),
        (3.0, 74, 1.0, 0.0),
        (4.0, 77, 2.0, -2.0),
        (6.0, 74, 2.0, 0.0),
    )
    for phrase_start in (8.0, 24.0, 40.0):
        for offset, note, beats, bend in erhu_motif:
            add_mono(mix, erhu(note, beats * beat * 0.95, bend), phrase_start + offset * beat, 0.24, -0.22)

    flute_motif = ((0.0, 81, 1.0), (1.0, 79, 1.0), (2.0, 77, 2.0), (4.0, 84, 2.0))
    for phrase_start in (16.0, 32.0):
        for offset, note, beats in flute_motif:
            add_mono(mix, flute(note, beats * beat * 0.92, 0.9), phrase_start + offset * beat, 0.17, 0.28)

    return finalize(mix, peak_db=-3.0, loopable=True, echo_delay=0.1875, echo_feedback=0.10)


def brass(note: float, duration: float, expression: float = 1.0) -> np.ndarray:
    """A compact suona/brass-like lead for heroic accents."""
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    frequency = 440.0 * (2.0 ** ((note - 69.0) / 12.0))
    vibrato = 1.0 + 0.0038 * np.sin(math.tau * 5.0 * t) * np.minimum(1.0, t / 0.15)
    phase = math.tau * np.cumsum(frequency * vibrato) / SAMPLE_RATE
    tone = (
        np.sin(phase)
        + 0.58 * np.sin(2.0 * phase + 0.18)
        + 0.34 * np.sin(3.0 * phase + 0.41)
        + 0.18 * np.sin(4.0 * phase + 0.67)
        + 0.08 * np.sin(5.0 * phase)
    )
    attack = np.minimum(1.0, t / 0.045)
    release = np.minimum(1.0, np.maximum(0.0, (duration - t) / 0.12))
    return np.tanh(tone * 0.20 * expression) * attack * release


def compose_boss_bloodfire_loop() -> np.ndarray:
    """Hot-blooded 140 BPM boss loop: danger first, heroic momentum second."""
    duration = 48.0  # Twenty-eight bars at 140 BPM, so the rhythmic seam is exact.
    beat = 60.0 / 140.0
    bar = beat * 4.0
    mix = empty_mix(duration)

    roots = (38, 38, 36, 33, 38, 41, 36)
    patterns = (
        (50, 57, 53, 57, 55, 57, 60, 57),
        (50, 57, 55, 60, 57, 62, 60, 57),
        (48, 55, 53, 57, 55, 60, 57, 53),
        (45, 52, 50, 57, 53, 57, 55, 52),
    )

    for bar_index in range(28):
        section = min(3, bar_index // 7)
        start = bar_index * bar
        add_mono(mix, low_drone(roots[bar_index % len(roots)], bar + 0.22), start, 0.25, -0.08)

        # Fast pipa/guqin ostinato gives the player a readable forward pulse.
        pattern = patterns[bar_index % len(patterns)]
        for step, note in enumerate(pattern):
            gain = 0.32 + section * 0.035 + (0.035 if step in (0, 4) else 0.0)
            pan = -0.34 + 0.68 * ((step % 4) / 3.0)
            add_mono(mix, pluck(note, 0.30, 1.05), start + step * bar / 8.0, gain, pan)

        # Full four-beat war-drum pattern, with double hits as the battle escalates.
        for beat_index in range(4):
            time = start + beat_index * beat
            heavy = beat_index in (0, 2)
            add_mono(mix, war_drum(1.0 if heavy else 0.72), time, 0.39 if heavy else 0.25, 0.04)
            add_mono(mix, rim_hit(), time + beat * 0.5, 0.10 + section * 0.012, 0.34)
        if section >= 1:
            add_mono(mix, war_drum(0.58), start + beat * 3.5, 0.17, -0.12)
        if section >= 2:
            add_mono(mix, rim_hit(), start + beat * 3.75, 0.09, -0.32)

        if bar_index in (0, 7, 14, 21):
            add_mono(mix, gong(), start, 0.13 + section * 0.018, 0.0)

    # A rising pentatonic hook answers the threatening rhythm with heroic resolve.
    heroic_motif = (
        (0.0, 69, 1.0, 0.0),
        (1.0, 72, 1.0, 0.0),
        (2.0, 74, 2.0, 2.0),
        (4.0, 77, 1.0, 0.0),
        (5.0, 74, 1.0, 0.0),
        (6.0, 72, 2.0, -2.0),
    )
    for phrase_bar in (2, 6, 10, 14, 18, 22):
        phrase_start = phrase_bar * bar
        lift = 0.01 * (phrase_bar // 7)
        for offset, note, beats, bend in heroic_motif:
            add_mono(
                mix,
                erhu(note, beats * beat * 0.88, bend),
                phrase_start + offset * beat,
                0.27 + lift,
                -0.20,
            )

    # Short suona-like calls mark the two late peaks without masking combat SFX.
    for phrase_bar in (16, 24):
        phrase_start = phrase_bar * bar
        for offset, note, beats in ((0.0, 74, 1.0), (1.0, 77, 1.0), (2.0, 81, 2.0)):
            add_mono(
                mix,
                brass(note, beats * beat * 0.82, 0.92),
                phrase_start + offset * beat,
                0.15,
                0.24,
            )

    # The final bar is a compact drum fill resolving directly into bar one's downbeat.
    final_bar = 27 * bar
    for step in range(8):
        add_mono(
            mix,
            war_drum(0.58 + step * 0.045),
            final_bar + step * bar / 8.0,
            0.13 + step * 0.015,
            -0.20 + step * 0.05,
        )

    return finalize(mix, peak_db=-2.4, loopable=True, echo_delay=beat * 0.5, echo_feedback=0.075)


def compose_boss_enrage_stem() -> np.ndarray:
    duration = 16.0
    beat = 0.5
    mix = empty_mix(duration)

    for step in range(64):
        time = step * beat * 0.5
        if step % 2 == 0:
            accent = 1.0 if step % 8 in (0, 4) else 0.58
            add_mono(mix, war_drum(accent), time, 0.30 if accent == 1.0 else 0.16, -0.04)
        add_mono(mix, rim_hit(), time, 0.07, 0.36)
        if step % 4 == 3:
            add_mono(mix, pluck(74 if step % 8 == 3 else 77, 0.22, 0.85), time, 0.12, -0.28)

    add_mono(mix, rising_wind(3.6), 4.0, 0.14, 0.0)
    add_mono(mix, rising_wind(3.6), 12.0, 0.14, 0.0)
    return finalize(mix, peak_db=-5.5, loopable=True, echo_delay=0.125, echo_feedback=0.06)


def compose_boss_bloodfire_enrage_stem() -> np.ndarray:
    """Seven 140 BPM bars of extra percussion for the boss's final 40% HP."""
    duration = 12.0
    beat = 60.0 / 140.0
    bar = beat * 4.0
    mix = empty_mix(duration)

    for step in range(56):
        time = step * bar / 8.0
        if step % 2 == 0:
            accent = 1.0 if step % 8 in (0, 4) else 0.68
            add_mono(mix, war_drum(accent), time, 0.27 if accent == 1.0 else 0.16, -0.05)
        add_mono(mix, rim_hit(), time, 0.075, 0.36 if step % 2 == 0 else -0.30)
        if step % 4 == 3:
            note = 74 if step % 8 == 3 else 77
            add_mono(mix, pluck(note, 0.20, 1.05), time, 0.13, -0.24)

    add_mono(mix, rising_wind(2.2), 4.65, 0.12, 0.0)
    add_mono(mix, rising_wind(2.2), 9.75, 0.12, 0.0)
    return finalize(mix, peak_db=-5.0, loopable=True, echo_delay=beat * 0.25, echo_feedback=0.055)


def compose_result_stinger(victory: bool) -> np.ndarray:
    mix = empty_mix(2.6)
    if victory:
        notes = (62, 67, 69, 74)
        for index, note in enumerate(notes):
            add_mono(mix, pluck(note, 1.2, 0.92), index * 0.22, 0.34, -0.22 + index * 0.14)
        add_mono(mix, flute(86, 1.55, 0.9), 0.78, 0.19, 0.22)
        add_mono(mix, gong(), 0.0, 0.13, 0.0)
    else:
        add_mono(mix, gong(), 0.0, 0.18, 0.0)
        add_mono(mix, erhu(62, 2.35, -5.0), 0.08, 0.28, -0.12)
        add_mono(mix, pluck(38, 1.8, 0.55), 0.0, 0.22, 0.15)
    return finalize(mix, peak_db=-3.2, loopable=False, echo_delay=0.23, echo_feedback=0.09)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output-root",
        type=Path,
        default=Path("Assets/Audio/Generated/Music"),
    )
    args = parser.parse_args()

    assets = {
        "stem_normalbattle_wuxia_percussion_15s_v01.wav": compose_normal_battle_stem(),
        "bgm_cave_mystery_loop_32s_v01.wav": compose_cave_music(),
        "stem_cave_combat_tension_16s_v01.wav": compose_cave_combat_stem(),
        "stg_boss_fox_demon_intro_4s_v01.wav": compose_boss_intro(),
        "bgm_boss_fox_demon_loop_48s_v01.wav": compose_boss_loop(),
        "stem_boss_fox_demon_enrage_16s_v01.wav": compose_boss_enrage_stem(),
        "bgm_boss_fox_demon_bloodfire_loop_48s_v02.wav": compose_boss_bloodfire_loop(),
        "stem_boss_fox_demon_bloodfire_enrage_12s_v02.wav": compose_boss_bloodfire_enrage_stem(),
        "stg_result_victory_v01.wav": compose_result_stinger(True),
        "stg_result_defeat_v01.wav": compose_result_stinger(False),
    }

    args.output_root.mkdir(parents=True, exist_ok=True)
    for name, audio in assets.items():
        path = args.output_root / name
        write_wav(path, audio)
        rms = float(np.sqrt(np.mean(audio**2)))
        peak = float(np.max(np.abs(audio)))
        print(
            f"{path}: duration={len(audio) / SAMPLE_RATE:.3f}s "
            f"peak={20.0 * math.log10(max(peak, 1e-12)):.2f}dBFS "
            f"rms={20.0 * math.log10(max(rms, 1e-12)):.2f}dBFS"
        )


if __name__ == "__main__":
    main()
