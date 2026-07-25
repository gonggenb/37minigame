#!/usr/bin/env python3
"""Generate a deterministic 60-second urgent wuxia background track.

The result is an original, dependency-light prototype cue intended for the
main-map countdown. It uses NumPy synthesis only and writes a Unity-ready WAV.
"""

from __future__ import annotations

import argparse
import math
import wave
from pathlib import Path

import numpy as np


SAMPLE_RATE = 44_100
BPM = 128.0
BEAT = 60.0 / BPM
BAR = BEAT * 4.0
DURATION = BAR * 32.0  # Exactly 60 seconds.
TAU = math.tau


def midi_frequency(note: float) -> float:
    return 440.0 * (2.0 ** ((note - 69.0) / 12.0))


def equal_power_pan(pan: float) -> tuple[float, float]:
    angle = (np.clip(pan, -1.0, 1.0) + 1.0) * math.pi / 4.0
    return math.cos(angle), math.sin(angle)


def add_mono(
    mix: np.ndarray,
    signal: np.ndarray,
    start_seconds: float,
    gain: float = 1.0,
    pan: float = 0.0,
) -> None:
    start = max(0, int(round(start_seconds * SAMPLE_RATE)))
    if start >= len(mix):
        return
    end = min(len(mix), start + len(signal))
    left, right = equal_power_pan(pan)
    clipped = signal[: end - start] * gain
    mix[start:end, 0] += clipped * left
    mix[start:end, 1] += clipped * right


def exp_envelope(length: int, attack: float, decay: float) -> np.ndarray:
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    attack_curve = np.minimum(1.0, t / max(attack, 1.0 / SAMPLE_RATE))
    return attack_curve * np.exp(-t / max(decay, 1.0 / SAMPLE_RATE))


def pluck(note: float, duration: float = 0.42, brightness: float = 1.0) -> np.ndarray:
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    frequency = midi_frequency(note)
    body = np.zeros(length, dtype=np.float64)
    partials = ((1, 1.0), (2, 0.46), (3, 0.26), (4, 0.14), (5, 0.08))
    for harmonic, amplitude in partials:
        detune = 1.0 + (harmonic - 1) * 0.0008
        body += (
            amplitude
            * np.sin(TAU * frequency * harmonic * detune * t + harmonic * 0.21)
            * np.exp(-t * (4.2 + harmonic * 1.35))
        )
    pick = np.random.default_rng(int(note * 97 + duration * 1000)).normal(0.0, 1.0, length)
    pick *= np.exp(-t * 95.0) * 0.055 * brightness
    return np.tanh((body * 0.42 * brightness + pick) * 1.25)


def flute(note: float, duration: float, expression: float = 1.0) -> np.ndarray:
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    frequency = midi_frequency(note)
    vibrato = 1.0 + 0.0045 * np.sin(TAU * 5.6 * t) * np.minimum(1.0, t / 0.18)
    phase = TAU * np.cumsum(frequency * vibrato) / SAMPLE_RATE
    tone = np.sin(phase) + 0.21 * np.sin(2.0 * phase + 0.15) + 0.07 * np.sin(3.0 * phase)
    rng = np.random.default_rng(int(note * 131 + duration * 100))
    breath = rng.normal(0.0, 1.0, length)
    breath = np.concatenate(([0.0], np.diff(breath))) * 0.018
    env = np.minimum(1.0, t / 0.055) * np.minimum(1.0, np.maximum(0.0, (duration - t) / 0.10))
    return np.tanh((tone * 0.28 + breath) * env * expression)


def erhu(note: float, duration: float, bend_to: float = 0.0) -> np.ndarray:
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    frequency = midi_frequency(note)
    bend = 2.0 ** ((bend_to * np.minimum(1.0, t / max(0.1, duration * 0.65))) / 12.0)
    vibrato = 1.0 + 0.0065 * np.sin(TAU * 5.2 * t) * np.minimum(1.0, t / 0.2)
    phase = TAU * np.cumsum(frequency * bend * vibrato) / SAMPLE_RATE
    reed = (
        np.sin(phase)
        + 0.53 * np.sin(2.0 * phase + 0.35)
        + 0.31 * np.sin(3.0 * phase + 0.62)
        + 0.14 * np.sin(4.0 * phase)
    )
    bow = np.random.default_rng(int(note * 211 + duration * 100)).normal(0.0, 1.0, length) * 0.012
    env = np.minimum(1.0, t / 0.09) * np.minimum(1.0, np.maximum(0.0, (duration - t) / 0.16))
    return np.tanh((reed * 0.18 + bow) * env * 1.3)


def low_drone(note: float, duration: float) -> np.ndarray:
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    frequency = midi_frequency(note)
    slow = 0.88 + 0.12 * np.sin(TAU * 0.17 * t)
    signal = (
        np.sin(TAU * frequency * t)
        + 0.28 * np.sin(TAU * frequency * 2.0 * t + 0.2)
        + 0.10 * np.sin(TAU * frequency * 3.0 * t + 0.4)
    )
    env = np.minimum(1.0, t / 0.65) * np.minimum(1.0, np.maximum(0.0, (duration - t) / 0.8))
    return np.tanh(signal * 0.25 * slow) * env


def war_drum(accent: float = 1.0) -> np.ndarray:
    duration = 0.68
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    frequency = 69.0 * np.exp(-t * 7.0) + 47.0
    phase = TAU * np.cumsum(frequency) / SAMPLE_RATE
    rng = np.random.default_rng(int(accent * 1009))
    skin = rng.normal(0.0, 1.0, length) * np.exp(-t * 32.0) * 0.11
    body = np.sin(phase) * np.exp(-t * 5.3)
    return np.tanh((body * 0.82 + skin) * accent * 1.5)


def rim_hit() -> np.ndarray:
    duration = 0.19
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    rng = np.random.default_rng(713)
    noise = rng.normal(0.0, 1.0, length)
    metallic = np.sin(TAU * 1820.0 * t) + 0.5 * np.sin(TAU * 2470.0 * t)
    return np.tanh((noise * 0.18 + metallic * 0.38) * np.exp(-t * 34.0))


def gong() -> np.ndarray:
    duration = 4.8
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    signal = np.zeros(length, dtype=np.float64)
    for frequency, amplitude, wobble in (
        (109.0, 1.0, 0.7),
        (167.0, 0.55, 0.9),
        (243.0, 0.34, 1.1),
        (389.0, 0.20, 1.7),
        (612.0, 0.10, 2.1),
    ):
        signal += amplitude * np.sin(TAU * frequency * t + 0.02 * np.sin(TAU * wobble * t))
    env = np.minimum(1.0, t / 0.035) * np.exp(-t * 0.72)
    return np.tanh(signal * 0.34) * env


def rising_wind(duration: float = 1.8) -> np.ndarray:
    length = int(duration * SAMPLE_RATE)
    t = np.arange(length, dtype=np.float64) / SAMPLE_RATE
    rng = np.random.default_rng(20260725)
    noise = rng.normal(0.0, 1.0, length)
    # A simple differentiator emphasizes the airy high-frequency edge.
    bright = np.concatenate(([0.0], np.diff(noise)))
    env = (t / duration) ** 1.8 * np.minimum(1.0, np.maximum(0.0, (duration - t) / 0.08))
    return np.tanh(bright * 0.16) * env


def add_echo(mix: np.ndarray, delay_seconds: float, feedback: float) -> None:
    delay = int(delay_seconds * SAMPLE_RATE)
    dry = mix.copy()
    for repeat in range(1, 4):
        shift = delay * repeat
        if shift >= len(mix):
            break
        amount = feedback**repeat
        mix[shift:, 0] += dry[:-shift, 1] * amount
        mix[shift:, 1] += dry[:-shift, 0] * amount


def compose() -> np.ndarray:
    total_samples = int(DURATION * SAMPLE_RATE)
    mix = np.zeros((total_samples, 2), dtype=np.float64)

    # Continuous low foundation: D2 and A2, changing weight by section.
    for bar in range(32):
        root = 38 if bar % 4 != 3 else 45
        add_mono(mix, low_drone(root, BAR + 0.12), bar * BAR, 0.22, -0.08)

    # Guqin-like sixteenth/eighth ostinato in D minor pentatonic.
    patterns = (
        (50, 57, 53, 57, 55, 57, 53, 57),
        (50, 57, 55, 60, 57, 62, 60, 57),
        (50, 53, 55, 57, 60, 57, 55, 53),
        (48, 55, 53, 57, 50, 57, 53, 55),
    )
    for bar in range(32):
        pattern = patterns[bar % len(patterns)]
        density = 4 if bar < 4 else 8
        for step in range(density):
            note = pattern[step * 2] if density == 4 else pattern[step]
            time = bar * BAR + step * (BAR / density)
            section_gain = (0.34, 0.41, 0.48, 0.56)[min(3, bar // 8)]
            human = 0.012 * math.sin((bar * 8 + step) * 1.7)
            pan = -0.24 + 0.48 * ((step % 4) / 3.0)
            add_mono(mix, pluck(note, 0.46, 0.9 + 0.1 * (step % 2)), time + human, section_gain, pan)

    # War drums become denser as the one-minute countdown advances.
    for bar in range(32):
        beat_indices = [0, 2]
        if bar >= 8:
            beat_indices += [3]
        if bar >= 16:
            beat_indices += [1]
        if bar >= 24:
            beat_indices += [0.5, 1.5, 2.5, 3.5]
        for index in beat_indices:
            accent = 1.0 if index in (0, 2) else 0.62
            gain = 0.24 + 0.035 * (bar // 8)
            add_mono(mix, war_drum(accent), bar * BAR + index * BEAT, gain, -0.05 if index < 2 else 0.05)

        if bar >= 6:
            subdivisions = 2 if bar < 20 else 4
            for step in range(subdivisions * 4):
                if step % subdivisions == 0 and bar < 16:
                    continue
                add_mono(
                    mix,
                    rim_hit(),
                    bar * BAR + step * (BEAT / subdivisions),
                    0.065 + 0.012 * (bar // 8),
                    0.34,
                )

    # Dizi motif: short, breathy calls that leave space for gameplay SFX.
    flute_motif = (
        (0.0, 74, 1.0),
        (1.0, 77, 0.5),
        (1.5, 79, 0.5),
        (2.0, 81, 1.0),
        (3.0, 79, 0.5),
        (3.5, 77, 0.5),
        (4.0, 74, 1.5),
        (6.0, 72, 0.5),
        (6.5, 74, 1.5),
    )
    for section_bar in (8, 16, 24):
        octave = 0 if section_bar < 24 else 12
        gain = 0.20 if section_bar < 24 else 0.235
        for offset, note, beats in flute_motif:
            start = section_bar * BAR + offset * BEAT
            add_mono(mix, flute(note + octave, beats * BEAT * 0.92, 1.0), start, gain, 0.28)

    # Erhu-like answering line begins halfway and grows toward the deadline.
    erhu_phrases = (
        (16, ((0.0, 62, 2.0, 0.0), (2.0, 65, 1.0, 2.0), (3.0, 67, 1.0, 0.0), (4.0, 69, 2.0, -2.0), (6.0, 67, 2.0, 0.0))),
        (20, ((0.0, 62, 1.0, 0.0), (1.0, 65, 1.0, 0.0), (2.0, 67, 2.0, 2.0), (4.0, 69, 1.0, 0.0), (5.0, 72, 3.0, -2.0))),
        (28, ((0.0, 69, 1.0, 0.0), (1.0, 72, 1.0, 0.0), (2.0, 74, 2.0, 2.0), (4.0, 77, 2.0, -2.0), (6.0, 74, 2.0, 0.0))),
    )
    for phrase_bar, notes in erhu_phrases:
        for offset, note, beats, bend in notes:
            add_mono(
                mix,
                erhu(note, beats * BEAT * 0.96, bend),
                phrase_bar * BAR + offset * BEAT,
                0.20 if phrase_bar < 28 else 0.23,
                -0.25,
            )

    # Structural accents at 15, 30, 45 and 58 seconds.
    for bar, gain in ((0, 0.13), (8, 0.16), (16, 0.18), (24, 0.22)):
        add_mono(mix, gong(), bar * BAR, gain, 0.0)
    add_mono(mix, rising_wind(1.8), 14 * BAR + 0.1, 0.22, 0.0)
    add_mono(mix, rising_wind(1.8), 22 * BAR + 0.1, 0.25, 0.0)
    add_mono(mix, rising_wind(1.6), 30 * BAR + 0.1, 0.30, 0.0)
    add_mono(mix, gong(), DURATION - 1.65, 0.26, 0.0)

    # Short stereo ambience without washing out transient combat feedback.
    add_echo(mix, 0.1875, 0.14)

    # Gentle high-pass-like DC removal, soft limiting, and fade boundaries.
    mix -= np.mean(mix, axis=0, keepdims=True)
    mix = np.tanh(mix * 1.08)
    fade_in = int(0.035 * SAMPLE_RATE)
    fade_out = int(0.055 * SAMPLE_RATE)
    mix[:fade_in] *= np.linspace(0.0, 1.0, fade_in)[:, None]
    mix[-fade_out:] *= np.linspace(1.0, 0.0, fade_out)[:, None]

    peak = float(np.max(np.abs(mix)))
    if peak > 0.0:
        mix *= 0.78 / peak  # About -2.2 dBFS peak; Unity mixer can place it lower.
    return mix


def write_wav(path: Path, audio: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.clip(audio * 32767.0, -32768, 32767).astype("<i2")
    with wave.open(str(path), "wb") as wav_file:
        wav_file.setnchannels(2)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        wav_file.writeframes(pcm.tobytes())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("Assets/Audio/Generated/Music/bgm_mainmap_wuxia_urgent_60s_v01.wav"),
    )
    args = parser.parse_args()
    audio = compose()
    write_wav(args.output, audio)
    rms = float(np.sqrt(np.mean(audio**2)))
    peak = float(np.max(np.abs(audio)))
    print(f"Wrote {args.output}")
    print(f"duration={len(audio) / SAMPLE_RATE:.3f}s sample_rate={SAMPLE_RATE} channels=2")
    print(f"peak={20.0 * math.log10(max(peak, 1e-12)):.2f} dBFS rms={20.0 * math.log10(max(rms, 1e-12)):.2f} dBFS")


if __name__ == "__main__":
    main()
