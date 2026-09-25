#!/usr/bin/env python3
"""Original 128 BPM wuxia/electro-house menu cue, 32 bars / 60 seconds.

Pentatonic zither and flute hooks, distorted string stabs, four-on-the-floor
drums, offbeat bass and sidechain pumping. Deterministic local synthesis;
no third-party recordings. Instrument and delay tails wrap at the loop seam.
"""
from pathlib import Path
import json
import wave

import numpy as np

from generate_menu_wuxia_music import pluck, xiao
from generate_adaptive_wuxia_music import electronic_kick, electronic_snare, hi_hat, power_chord

RATE = 44100
BPM = 128
BEAT = 60 / BPM
BAR = BEAT * 4
SECONDS = BAR * 32
ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/Resources/Audio/Menu/bgm_menu_wuxia_punk_dj_60s_v02.wav"


def envelope(sound, attack=.005, release=.025):
    sound = sound.copy()
    a, r = min(len(sound), round(attack * RATE)), min(len(sound), round(release * RATE))
    sound[:a] *= np.linspace(0, 1, a)
    sound[-r:] *= np.linspace(1, 0, r)
    return sound


def bass(note, duration):
    t = np.arange(round(duration * RATE)) / RATE
    hz = 440 * 2 ** ((note - 69) / 12)
    phase = 2 * np.pi * hz * t
    # Fundamental plus a moving upper layer: weight on speakers, definition on phones.
    tone = np.sin(phase) + .32 * np.sin(phase * 2 + .7 * np.sin(phase))
    tone += .17 * np.sin(phase * 3) * np.exp(-t * 10)
    return envelope(np.tanh(tone * 1.35) * np.exp(-t * 2.2), .006, .035)


def neon_lead(note, duration):
    t = np.arange(round(duration * RATE)) / RATE
    hz = 440 * 2 ** ((note - 69) / 12)
    signal = np.zeros_like(t)
    for detune in (-.003, 0, .003):
        phase = 2 * np.pi * hz * (1 + detune) * t
        for harmonic in range(1, 8):
            signal += np.sin(phase * harmonic) / harmonic ** 1.6
    return envelope(np.tanh(signal * .45), .015, .055)


def sweep(duration, seed):
    rng = np.random.default_rng(seed)
    t = np.arange(round(duration * RATE)) / RATE
    noise = rng.normal(0, .25, len(t))
    noise -= np.convolve(noise, np.ones(25) / 25, mode="same")
    phase = 2 * np.pi * (170 * t + 95 * t * t)
    pulse = .7 + .3 * np.sin(2 * np.pi * t / (BEAT * .5))
    return envelope((noise + .10 * np.sin(phase)) * (t / duration) ** 1.6 * pulse, .05, .02)


def compose():
    shape = (round(RATE * SECONDS), 2)
    drums, low, acoustic, electric = [np.zeros(shape) for _ in range(4)]

    def add(bus, sound, start, gain, pan=0):
        angle = (pan + 1) * np.pi / 4
        stereo = sound[:, None] * gain * np.array((np.cos(angle), np.sin(angle)))
        offset = round(start * RATE) % len(bus)
        first = min(len(stereo), len(bus) - offset)
        bus[offset:offset + first] += stereo[:first]
        bus[:len(stereo) - first] += stereo[first:]

    kick = envelope(electronic_kick(), .002, .025)
    snare = envelope(electronic_snare(), .001, .025)
    closed = envelope(hi_hat(), .001, .015)
    opened = envelope(hi_hat(True), .001, .025)
    roots = (38, 38, 41, 41, 36, 36, 38, 38)
    patterns = ((62, 69, 65, 69, 72, 69, 67, 65),
                (65, 72, 69, 72, 77, 72, 69, 67),
                (60, 67, 65, 67, 72, 67, 65, 62),
                (62, 65, 67, 69, 72, 69, 65, 62))
    for bar in range(32):
        start, root = bar * BAR, roots[bar % 8]
        breakdown = 16 <= bar < 20
        drop = 4 <= bar < 16 or bar >= 20
        # Intro carries the identity immediately; full bass drop arrives at 7.5 s.
        for beat in range(4):
            if not breakdown or (bar == 19 and beat == 0):
                add(drums, kick, start + beat * BEAT, .64 if drop else .43)
            if not breakdown and beat in (1, 3):
                add(drums, snare, start + beat * BEAT, .26, .03)
            if not breakdown:
                add(drums, opened if drop else closed, start + (beat + .5) * BEAT, .19, .26)
                if drop:
                    add(drums, closed, start + beat * BEAT, .08, -.27)
            if drop:
                note = root + (12 if beat == 3 and bar % 2 else 0)
                add(low, bass(note, BEAT * .43), start + (beat + .5) * BEAT, .47)
                if beat in (1, 3):
                    add(low, bass(root, BEAT * .19), start + (beat + .25) * BEAT, .21)

        pattern = patterns[(bar % 8) // 2]
        for step, note in enumerate(pattern):
            if breakdown and step % 2:
                continue
            # Zither remains audible above the electronic groove through both drops.
            add(acoustic, pluck(note, 1.6), start + step * BEAT * .5,
                .24 if step in (0, 4) else .17, -.30 + .15 * (step % 4))
        if drop:
            for beat in (.75, 2.75):
                add(electric, power_chord(root + 12, BEAT * .40, .85),
                    start + beat * BEAT, .105, -.2 if beat < 2 else .2)
        if bar in (3, 19, 31):
            add(electric, sweep(BAR * 2, 400 + bar), start - BAR, .18)
            for step in range(8):
                add(drums, snare, start + step * BEAT * .5, .05 + step * .012, -.12)

    hook = ((0, 69, .75), (1, 72, .5), (1.75, 74, 1), (3, 72, .6),
            (4, 69, .75), (5, 67, .5), (5.75, 65, .75), (7, 62, .8))
    for phrase_bar in (0, 4, 8, 12, 16, 20, 24, 28):
        for step, (offset, note, beats) in enumerate(hook):
            start, duration = phrase_bar * BAR + offset * BEAT, beats * BEAT
            add(acoustic, xiao(note, max(duration, .3), 700 + phrase_bar + step), start, .14, .18)
            if phrase_bar in (8, 12, 24, 28):
                add(electric, neon_lead(note, duration), start, .105, -.05)

    # Circular ping-pong delays, so the final phrase resolves directly into the first bar.
    dry = acoustic.copy()
    for delay, gain in ((BEAT * .75, .17), (BEAT * 1.5, .09), (BEAT * 2.25, .045)):
        acoustic += np.roll(dry[:, ::-1], round(delay * RATE), axis=0) * gain
    t = np.arange(shape[0]) / RATE
    phase = (t % BEAT) / BEAT
    pump = 1 - .72 * np.exp(-phase * 7) * (1 - np.exp(-phase * 120))
    # Retain breathing space in the flute break; stronger pumping during the full groove.
    pump[(t >= BAR * 16) & (t < BAR * 20)] = 1
    mix = drums + low * pump[:, None] + acoustic * (.70 + .30 * pump[:, None]) + electric * pump[:, None]
    mix -= mix.mean(axis=0)
    mix = np.tanh(mix * 1.15)
    # Microfades remove the section-change discontinuity without shortening the beat grid.
    seam = round(.003 * RATE)
    edge = np.sin(np.linspace(0, np.pi / 2, seam))[:, None]
    mix[:seam] *= edge
    mix[-seam:] *= edge[::-1]
    mix *= 10 ** (-3 / 20) / np.max(np.abs(mix))
    return mix


def main():
    mix = compose()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUTPUT), "wb") as stream:
        stream.setnchannels(2)
        stream.setsampwidth(2)
        stream.setframerate(RATE)
        stream.writeframes(np.rint(mix * 32767).astype("<i2").tobytes())
    stats = {"asset": str(OUTPUT.relative_to(ROOT)), "bpm": BPM, "duration_seconds": SECONDS,
             "peak_dbfs": float(20 * np.log10(np.max(np.abs(mix)))),
             "rms_dbfs": float(20 * np.log10(np.sqrt(np.mean(mix ** 2)))),
             "loop_seam_step": float(np.max(np.abs(mix[0] - mix[-1]))),
             "clipped_samples": int(np.sum(np.abs(mix) >= 1)),
             "sections": {"hook_groove": [0, 7.5], "drop_1": [7.5, 30],
                          "flute_break_build": [30, 37.5], "drop_2": [37.5, 60]}}
    print(json.dumps(stats, indent=2))
    return stats


if __name__ == "__main__":
    main()
