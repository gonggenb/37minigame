#!/usr/bin/env python3
"""Original synthesized boss cues; no external recordings or model service.

Run from any directory with Python + NumPy. Circular note tails and delay
preserve whole-bar loops. Revisions keep the assigned asset names and GUIDs.
"""
from pathlib import Path
import json
import math
import hashlib
import wave
import argparse
import numpy as np

from generate_wuxia_urgent_bgm import (
    SAMPLE_RATE as SR, equal_power_pan, pluck, erhu, flute, low_drone,
    war_drum, rim_hit, gong, write_wav,
)
from generate_adaptive_wuxia_music import brass

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Audio/Generated/Music"
REPORT = ROOT / "docs/validation/boss_bgm_2026-09-06.json"


def blank(seconds):
    return np.zeros((round(seconds * SR), 2), dtype=np.float64)


def put(mix, signal, seconds, gain=1.0, pan=0.0):
    """Wrap note releases over the loop boundary instead of truncating them."""
    signal = signal.copy()
    edge = min(round(0.006 * SR), len(signal) // 4)
    signal[:edge] *= np.linspace(0, 1, edge)
    signal[-edge:] *= np.linspace(1, 0, edge)
    stereo = signal[:, None] * np.array(equal_power_pan(pan))[None, :] * gain
    start = round(seconds * SR) % len(mix)
    while len(stereo):
        size = min(len(stereo), len(mix) - start)
        mix[start:start + size] += stereo[:size]
        stereo = stereo[size:]
        start = 0


def finish(mix, beat, peak_db=-4.0, loop=True):
    dry = mix.copy()
    for delay, gain in ((0.75 * beat, .12), (1.5 * beat, .045), (.043, .07)):
        shifted = np.roll(dry[:, ::-1], round(delay * SR), axis=0)
        if not loop:
            shifted[:round(delay * SR)] = 0
        mix += shifted * gain
    mix -= mix.mean(axis=0)
    mix = np.tanh(mix * 1.08)
    mix *= 10 ** (peak_db / 20) / max(np.max(np.abs(mix)), 1e-9)
    if not loop:
        size = round(.15 * SR)
        mix[:size] *= np.linspace(0, 1, size)[:, None]
        mix[-size:] *= np.linspace(1, 0, size)[:, None]
    return mix


def gate():
    # 32 bars, 120 BPM: restrained march / answer / blade / return.
    beat = .5
    mix = blank(64)
    roots = (38, 38, 34, 36, 38, 41, 36, 33)
    motifs = (
        ((0, 62, 1.5), (2, 65, .75), (3, 62, .75)),
        ((0, 60, 1), (1.5, 57, .5), (2.5, 55, 1.2)),
        ((0, 58, 1.5), (2, 62, .75), (3, 65, .75)),
        ((0, 60, 1), (1.5, 62, .5), (2, 67, 1.7)),
        ((0, 69, 1.5), (2, 65, .75), (3, 62, .75)),
        ((0, 65, 1), (1.5, 69, .5), (2.5, 72, 1)),
        ((0, 67, 1.5), (2, 64, .75), (3, 60, .75)),
        ((0, 61, .75), (1, 64, .75), (2, 69, 1.5)),
    )
    for bar in range(32):
        t = bar * 4 * beat
        section = bar // 8
        root = roots[bar % 8]
        put(mix, low_drone(root, 2.25), t, .38)
        for offset, accent in ((0, 1), (1.5, .62), (2, .92), (3, .65), (3.5, .5)):
            put(mix, war_drum(accent), t + offset * beat, .40, -.08)
        for offset in (1, 3):
            put(mix, rim_hit(), t + offset * beat, .10, .27)
        notes = (root + 12, root + 19, root + 12, root + 24)
        for step in range(8 if section == 2 else 4):
            offset = step * (.5 if section == 2 else 1)
            put(mix, pluck(notes[step % 4], .5, .65), t + offset * beat, .22, -.3)
        if bar % 8 == 0:
            put(mix, gong(), t, .12)
        if bar % 8 == 7:
            for step in range(4):
                put(mix, war_drum(.50 + step * .09), t + (3 + step * .25) * beat, .19, .15)
        if section != 0 or bar >= 4:
            for offset, note, length in motifs[bar % 8]:
                voice = erhu(note - 12 if section == 0 else note, length * beat)
                put(mix, voice, t + offset * beat, .34, .13)
                if section == 2 and offset == 0:
                    put(mix, brass(note - 12, length * beat, .65), t, .20, -.13)
    return finish(mix, beat)


FOX_ROOTS = (50, 50, 46, 48, 50, 53, 48, 45)


def fox(layer=0):
    # 160 BPM, 32-bar theme / 8-bar phase stems, same harmonic cycle.
    beat = .375
    bars = 32 if layer == 0 else 8
    mix = blank(bars * 4 * beat)
    hook = (
        ((0, 74, .75), (1, 77, .5), (1.75, 81, .5), (2.5, 79, .5), (3.25, 77, .65)),
        ((0, 74, 1.5), (2, 72, .5), (2.75, 69, 1)),
        ((0, 70, .75), (1, 74, .5), (1.75, 77, .5), (2.5, 81, 1)),
        ((0, 79, 1), (1.5, 76, .5), (2.25, 72, 1.4)),
        ((0, 81, .75), (1, 77, .5), (1.75, 74, .5), (2.5, 77, 1)),
        ((0, 81, 1), (1.5, 84, .5), (2.25, 81, 1.4)),
        ((0, 79, .75), (1, 76, .5), (1.75, 72, .5), (2.5, 76, 1)),
        ((0, 73, .75), (1, 76, .5), (1.75, 81, .5), (2.5, 73, 1)),
    )
    for bar in range(bars):
        t = bar * beat * 4
        root = FOX_ROOTS[bar % 8]
        third = 4 if bar % 8 in (3, 6, 7) else 3
        chord = (root, root + 7, root + 12, root + third + 12)
        if layer == 0:
            put(mix, low_drone(root - 12, 1.7), t, .27)
            for step in range(8):
                note = chord[(step + (bar % 2)) % 4]
                put(mix, pluck(note, .36, .85), t + step * beat / 2, .26, -.35 + (step % 3) * .35)
            for offset, strength in ((0, 1), (1.5, .65), (2.5, .85), (3.5, .6)):
                put(mix, war_drum(strength), t + offset * beat, .30, -.06)
            for offset in (.5, 1.5, 2.5, 3.5):
                put(mix, rim_hit(), t + offset * beat, .065, .35)
            for offset, note, length in hook[bar % 8]:
                if bar // 8 == 2:
                    put(mix, erhu(note - 12, length * beat, .25), t + offset * beat, .32, -.14)
                else:
                    put(mix, flute(note, length * beat, .8), t + offset * beat, .34, .16)
            if bar % 8 in (2, 6):
                put(mix, erhu(root + 12, beat * 2, -.8), t + 2 * beat, .12, -.24)
            if bar % 8 == 0:
                put(mix, gong(), t, .075)
        else:
            if layer == 1:
                # Armor: a new low bowed/reed voice and broad half-time accents.
                put(mix, low_drone(root - 12, 1.7), t, .32)
                for offset, note, length in ((0, root + 12, 1.65), (2, root + 7, .8), (3, root + third + 12, .8)):
                    put(mix, erhu(note, length * beat), t + offset * beat, .65, -.12)
                    put(mix, brass(note - 12, length * beat, .65), t + offset * beat, .35, .12)
                for offset, strength in ((0, 1), (1.75, .55), (2, 1), (3.5, .7)):
                    put(mix, war_drum(strength), t + offset * beat, .60)
                for offset in (0, 2):
                    put(mix, rim_hit(), t + offset * beat, .13, .25)
            else:
                # Frenzy: high, brassy calls and constant sixteenth-note motion.
                for step in range(16):
                    put(mix, pluck(chord[step % 4] + 12, .20, .95), t + step * beat / 4,
                        .32, -.35 if step % 2 else .35)
                for offset, note, length in hook[bar % 8]:
                    put(mix, brass(note, length * beat, .85), t + offset * beat, .57, -.08)
                for step in range(8):
                    put(mix, war_drum(1 if step % 2 == 0 else .65), t + step * beat / 2, .46)
                for offset in (.75, 1, 1.75, 2.75, 3, 3.75):
                    put(mix, rim_hit(), t + offset * beat, .17, .24)
                if bar % 2 == 1:
                    for step in range(4):
                        put(mix, war_drum(.55 + step * .1), t + (3 + step * .25) * beat, .3, -.15)
            if bar % 4 == 0:
                put(mix, gong(), t, .12 if layer == 1 else .18)
    return finish(mix, beat, -4)


def phase_sting(frenzy=False):
    # Unpitched cue avoids clashing when a threshold is crossed mid-phrase.
    mix = blank(1.2)
    put(mix, gong()[:round(.95 * SR)], 0, .32 if frenzy else .44)
    for offset, strength in (((0, 1), (.11, .8), (.22, 1), (.36, .8)) if frenzy else ((0, 1), (.18, .7))):
        put(mix, war_drum(strength), offset, .50)
        if frenzy:
            put(mix, rim_hit(), offset, .18, .2)
    return finish(mix, .375, -5, loop=False)


def intro():
    mix = blank(3)
    put(mix, gong(), 0, .20)
    put(mix, erhu(74, 2.5, -1), .1, .28, -.18)
    for step in range(8):
        put(mix, pluck((62, 65, 69, 74)[step % 4], .3, .8), step * .375, .18, .2)
        put(mix, war_drum(.7), step * .375, .2)
    return finish(mix, .375, -5, loop=False)


def phase_preview():
    """Same twelve-second passage under the three runtime mix settings."""
    def read(path):
        with wave.open(str(path), "rb") as stream:
            return np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2").reshape(-1, 2) / 32768

    base = read(OUT / "bgm_boss_fox_demon_moonfire_loop_48s_v04.wav")[:12 * SR]
    armor = read(OUT / "stem_boss_fox_demon_moonfire_armor_12s_v04.wav")
    frenzy = read(OUT / "stem_boss_fox_demon_moonfire_frenzy_12s_v04.wav")
    phases = [base * .38, base * .38 * .68 + armor * .50, base * .38 * .42 + frenzy * .62]
    preview = np.concatenate(phases)
    for index, name in enumerate(("armor", "frenzy"), 1):
        start = index * 12 * SR
        count = round(.15 * SR)
        ramp = np.linspace(0, 1, count)[:, None]
        preview[start:start + count] = phases[index - 1][:count] * (1 - ramp) + phases[index][:count] * ramp
        sting = read(ROOT / "Assets/Resources/Audio/BossTransitions" / f"stg_fox_{name}_transition_v01.wav")
        preview[start:start + len(sting)] += sting * .55 * .8
    preview[:round(.02 * SR)] *= np.linspace(0, 1, round(.02 * SR))[:, None]
    preview[-round(.1 * SR):] *= np.linspace(1, 0, round(.1 * SR))[:, None]
    assert np.max(np.abs(preview)) < 1
    path = ROOT / "docs/validation/boss_phase_mix_preview_36s.wav"
    write_wav(path, preview)
    report = dict(preview=str(path.relative_to(ROOT)), segments_seconds=[[0, 12], [12, 24], [24, 36]],
                  rms_db=[float(20 * np.log10(np.sqrt(np.mean(x * x)))) for x in phases],
                  preview_peak_db=float(20 * np.log10(np.max(np.abs(preview)))))
    path.with_name("boss_phase_mix_preview.json").write_text(json.dumps(report, indent=2) + "\n")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--phases-only", action="store_true")
    args = parser.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    cues = [
        ("bgm_boss_xuanjia_ironpass_loop_64s_v01", gate, 120, True),
        ("bgm_boss_fox_demon_moonfire_loop_48s_v04", fox, 160, True),
        ("stem_boss_fox_demon_moonfire_armor_12s_v04", lambda: fox(1), 160, True),
        ("stem_boss_fox_demon_moonfire_frenzy_12s_v04", lambda: fox(2), 160, True),
        ("stg_boss_fox_demon_moonfire_intro_3s_v04", intro, 160, False),
    ]
    if args.phases_only:
        cues = cues[2:4]
    cues += [
        ("stg_fox_armor_transition_v01", phase_sting, 160, False),
        ("stg_fox_frenzy_transition_v01", lambda: phase_sting(True), 160, False),
    ]
    report = []
    for name, compose, bpm, loop in cues:
        folder = ROOT / "Assets/Resources/Audio/BossTransitions" if "transition" in name else OUT
        folder.mkdir(parents=True, exist_ok=True)
        path = folder / (name + ".wav")
        mix = compose()
        write_wav(path, mix)
        with wave.open(str(path), "rb") as f:
            pcm = np.frombuffer(f.readframes(f.getnframes()), dtype="<i2").reshape(-1, 2) / 32768
            assert f.getframerate() == SR and f.getnchannels() == 2
        assert np.isfinite(pcm).all() and np.max(np.abs(pcm)) < 1
        row = dict(file=str(path.relative_to(ROOT)), bpm=bpm, seconds=len(pcm) / SR,
                   loop=loop, peak_db=float(20 * np.log10(np.max(np.abs(pcm)))),
                   rms_db=float(20 * np.log10(np.sqrt(np.mean(pcm ** 2)))),
                   seam_delta=float(np.max(np.abs(pcm[-1] - pcm[0]))),
                   sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        if loop:
            assert row["seam_delta"] < .02, row
        report.append(row)
        print(json.dumps(row), flush=True)
    report_path = REPORT.with_name("boss_phase_mix_2026-09-06.json") if args.phases_only else REPORT
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2) + "\n")
    phase_preview()


if __name__ == "__main__":
    main()
