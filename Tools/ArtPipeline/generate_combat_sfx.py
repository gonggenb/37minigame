#!/usr/bin/env python3
"""Generate the small deterministic combat SFX pack used by the prototype."""

from __future__ import annotations

import math
import random
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 44_100
OUTPUT_DIR = Path(__file__).resolve().parents[2] / "Assets/Audio/Generated/Combat"


def envelope(progress: float, attack: float, decay_power: float) -> float:
    fade_in = min(1.0, progress / max(attack, 0.0001))
    return fade_in * math.pow(max(0.0, 1.0 - progress), decay_power)


def noise_sample(rng: random.Random) -> float:
    return rng.random() * 2.0 - 1.0


def synthesize(kind: str, duration: float) -> list[float]:
    count = max(1, int(SAMPLE_RATE * duration))
    rng = random.Random(3700 + sum(ord(char) for char in kind))
    samples: list[float] = []
    previous_noise = 0.0

    for index in range(count):
        time = index / SAMPLE_RATE
        progress = index / max(1, count - 1)
        raw_noise = noise_sample(rng)
        smooth_noise = previous_noise * 0.64 + raw_noise * 0.36
        previous_noise = smooth_noise

        if kind == "swing":
            frequency = 1500.0 - 1050.0 * progress
            phase = 2.0 * math.pi * frequency * time
            body = math.sin(phase) * 0.18 + smooth_noise * 0.72
            sample = body * math.sin(math.pi * progress) * (1.0 - progress * 0.55)
        elif kind == "impact":
            env = envelope(progress, 0.015, 3.3)
            thump = math.sin(2.0 * math.pi * (145.0 - 70.0 * progress) * time) * 0.76
            blade = math.sin(2.0 * math.pi * 920.0 * time) * 0.18
            crack = raw_noise * math.pow(max(0.0, 1.0 - progress), 7.0) * 0.48
            sample = (thump + blade + crack) * env
        elif kind == "critical":
            env = envelope(progress, 0.01, 2.45)
            thump = math.sin(2.0 * math.pi * (92.0 - 30.0 * progress) * time) * 0.78
            metal = (
                math.sin(2.0 * math.pi * 690.0 * time) * 0.24
                + math.sin(2.0 * math.pi * 1110.0 * time) * 0.13
            )
            crack = raw_noise * math.pow(max(0.0, 1.0 - progress), 5.0) * 0.42
            sample = (thump + metal + crack) * env
        else:
            frequency = 1120.0 + 1050.0 * progress
            phase = 2.0 * math.pi * frequency * time
            air = math.sin(phase) * 0.16 + smooth_noise * 0.58
            sample = air * math.sin(math.pi * progress) * (1.0 - progress * 0.35)

        samples.append(sample)

    peak = max(max(abs(value) for value in samples), 0.001)
    gain = 0.88 / peak
    fade_samples = max(1, int(SAMPLE_RATE * 0.008))
    for index in range(len(samples)):
        tail = min(1.0, (len(samples) - 1 - index) / fade_samples)
        samples[index] *= gain * tail
    return samples


def write_wav(path: Path, samples: list[float]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    pcm = bytearray()
    for sample in samples:
        clamped = max(-1.0, min(1.0, sample))
        pcm.extend(struct.pack("<h", round(clamped * 32767.0)))

    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(pcm)


def main() -> None:
    specs = {
        "sfx_combat_sword_swing_v01.wav": ("swing", 0.18),
        "sfx_combat_impact_light_v01.wav": ("impact", 0.16),
        "sfx_combat_impact_critical_v01.wav": ("critical", 0.28),
        "sfx_combat_dodge_v01.wav": ("dodge", 0.20),
    }
    for filename, (kind, duration) in specs.items():
        output = OUTPUT_DIR / filename
        write_wav(output, synthesize(kind, duration))
        print(output)


if __name__ == "__main__":
    main()
