#!/usr/bin/env python3
"""Original calm menu cue; deterministic synthesis, no samples or external services.

60 BPM, sixteen 4/4 bars, D major pentatonic. Muted guqin/guzheng-like
plucks answer a breathy xiao-like lead; no drums, vocals or combat accents.
Circular mixing carries instrument and reverb tails across the loop seam.
"""
from pathlib import Path
import json
import wave

import numpy as np

RATE = 44100
SECONDS = 64
ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/Audio/Generated/Music/bgm_menu_misty_mountains_loop_64s_v01.wav"


def pluck(note, duration=4.8):
    t = np.arange(round(duration * RATE)) / RATE
    hz = 440 * 2 ** ((note - 69) / 12)
    phase = 2 * np.pi * hz * (t + 0.0003 * (1 - np.exp(-t * 6)))
    sound = np.zeros_like(t)
    for harmonic in range(1, 9):
        # A soft attack and faster upper-partial decay avoid a metallic bell timbre.
        sound += (np.sin(phase * harmonic + 0.08 * harmonic)
                  * np.exp(-t * (0.65 + harmonic * 0.23)) / harmonic ** 1.65)
    return sound * (1 - np.exp(-t * 110)) * np.minimum((duration - t) / 0.3, 1)


def xiao(note, duration, seed):
    rng = np.random.default_rng(seed)
    t = np.arange(round(duration * RATE)) / RATE
    hz = 440 * 2 ** ((note - 69) / 12)
    vibrato = 0.0025 * np.sin(2 * np.pi * 4.6 * t) * (1 - np.exp(-t * 2))
    bend = -0.012 * np.exp(-t * 9)
    phase = 2 * np.pi * np.cumsum(hz * (1 + vibrato + bend)) / RATE
    breath = np.convolve(rng.normal(0, 1, len(t)), np.ones(19) / 19, mode="same")
    sound = np.sin(phase) + 0.19 * np.sin(2 * phase) + 0.055 * np.sin(3 * phase)
    sound += 0.045 * breath
    envelope = np.minimum(t / 0.22, 1) * np.minimum((duration - t) / 0.48, 1)
    return sound * np.sin(envelope * np.pi / 2) ** 2 * (0.94 + 0.06 * np.sin(t * 2.1))


def compose():
    mix = np.zeros((RATE * SECONDS, 2), dtype=np.float64)

    def place(sound, start, gain, pan):
        stereo = sound[:, None] * gain * np.array([
            np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)])
        offset = round(start * RATE) % len(mix)
        first = min(len(stereo), len(mix) - offset)
        mix[offset:offset + first] += stereo[:first]
        mix[:len(stereo) - first] += stereo[first:]

    # Open fifths with spacious, irregular answers, not a constant arpeggio ostinato.
    roots = (50, 50, 47, 47, 54, 54, 57, 57, 50, 50, 47, 47, 54, 57, 50, 50)
    answers = (69, 66, 62, 64, 69, 71, 69, 66, 74, 69, 66, 64, 66, 69, 64, 62)
    for bar, (root, answer) in enumerate(zip(roots, answers)):
        start = bar * 4
        place(pluck(root, 6), start + 0.12, 0.19, -0.25)
        place(pluck(root + 7), start + 1.62, 0.09, -0.1)
        place(pluck(answer), start + 2.65, 0.10 if bar % 2 else 0.13, 0.3)
        if bar in (3, 7, 11, 15):
            place(pluck(answer + 12, 3.8), start + 3.45, 0.045, 0.45)

    phrases = (
        (4.4, ((66, 1.5), (69, 1.2), (71, 2.4), (69, 1.8))),
        (14.4, ((66, 2.2), (64, 1.5), (62, 3.0))),
        (24.4, ((64, 1.4), (66, 1.4), (69, 2.5), (66, 2.0))),
        (36.4, ((69, 1.5), (71, 1.4), (74, 2.5), (71, 1.8))),
        (46.4, ((69, 1.8), (66, 1.6), (64, 2.7))),
        (56.4, ((66, 1.5), (64, 1.5), (62, 3.2))),
    )
    for index, (start, phrase) in enumerate(phrases):
        for step, (note, duration) in enumerate(phrase):
            place(xiao(note, duration, index * 10 + step), start, 0.115, 0.12)
            start += duration + 0.16

    # Finite, circular stereo room reflections preserve the exact 64-second form.
    dry = mix.copy()
    for delay, gain in ((0.113, .10), (.197, .08), (.317, .07), (.479, .055),
                        (.733, .04), (1.071, .025), (1.537, .015)):
        mix += np.roll(dry[:, ::-1], round(delay * RATE), axis=0) * gain
    mix -= mix.mean(axis=0)
    mix *= 10 ** (-6 / 20) / np.max(np.abs(mix))
    return mix


def main():
    mix = compose()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    pcm = np.rint(np.clip(mix, -1, 1) * 32767).astype("<i2")
    with wave.open(str(OUTPUT), "wb") as stream:
        stream.setnchannels(2)
        stream.setsampwidth(2)
        stream.setframerate(RATE)
        stream.writeframes(pcm.tobytes())
    print(json.dumps({"asset": str(OUTPUT), "duration_seconds": SECONDS,
                      "sample_rate": RATE, "peak_dbfs": float(20 * np.log10(np.max(np.abs(mix)))),
                      "rms_dbfs": float(20 * np.log10(np.sqrt(np.mean(mix ** 2)))),
                      "seam_delta": float(np.max(np.abs(mix[0] - mix[-1]))),
                      "clipped_samples": int(np.sum(np.abs(mix) >= 1))}, indent=2))


if __name__ == "__main__":
    main()
