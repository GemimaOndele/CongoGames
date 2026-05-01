import math
import os
import random
import struct
import wave

SAMPLE_RATE = 44100
DURATION_SEC = 24
MASTER_GAIN = 0.42

MODE_BASES = {
    "quiz": [220.0, 277.18, 329.63],
    "semantic": [196.0, 246.94, 293.66],
    "word-scramble": [174.61, 220.0, 261.63],
    "crossword-lite": [164.81, 207.65, 246.94],
    "memory": [233.08, 293.66, 349.23],
    "mystery-word": [146.83, 185.0, 220.0],
    "speed-chrono": [261.63, 329.63, 392.0],
}


def clamp(v: float) -> float:
    return -1.0 if v < -1.0 else (1.0 if v > 1.0 else v)


def tone_sample(t: float, base: list[float], variant: int, aggressive: bool) -> float:
    bpm = 122.0 + variant * 4.0 + (6.0 if aggressive else 0.0)
    beat = (t * bpm / 60.0) % 1.0
    bar4 = int((t * bpm / 60.0) // 4) % 4
    root = base[bar4 % len(base)]

    slow_lfo = 0.5 + 0.5 * math.sin(2 * math.pi * (0.05 + 0.01 * variant) * t)
    pad = 0.0
    for i, f in enumerate((root, root * 1.26, root * 1.5)):
        detune = 1.0 + (0.0022 * (i + 1) * math.sin(2 * math.pi * (0.02 + i * 0.004) * t))
        amp = (0.19 - i * 0.03) * (0.55 + 0.45 * slow_lfo)
        pad += math.sin(2 * math.pi * f * detune * t) * amp

    bass_env = 1.0 - min(1.0, beat * (3.4 if aggressive else 2.6))
    bass = math.sin(2 * math.pi * (root * 0.5) * t) * (0.18 if aggressive else 0.14) * bass_env

    arp_step = int((t * bpm / 60.0) * 2) % 8
    arp_seq = [root, root * 1.26, root * 1.5, root * 2.0, root * 1.5, root * 1.26, root, root * 2.0]
    arp_f = arp_seq[arp_step]
    arp_gate = 1.0 if ((t * bpm / 60.0) * 4) % 1.0 < 0.46 else 0.0
    arp = math.sin(2 * math.pi * arp_f * 2.0 * t) * (0.06 + 0.03 * slow_lfo) * arp_gate

    hat = 0.0
    if beat < 0.08:
        hat = (random.random() * 2.0 - 1.0) * (0.1 if aggressive else 0.07) * (1.0 - beat / 0.08)

    snare = 0.0
    sn_step = ((t * bpm / 60.0) % 2.0)
    if abs(sn_step - 1.0) < 0.05:
        x = 1.0 - abs(sn_step - 1.0) / 0.05
        snare = (random.random() * 2.0 - 1.0) * (0.12 if aggressive else 0.09) * x

    shimmer = math.sin(2 * math.pi * (root * (2.8 if aggressive else 2.2)) * t) * (0.03 + 0.02 * slow_lfo)
    return (pad + bass + arp + hat + snare + shimmer) * MASTER_GAIN


def write_loop(path: str, base: list[float], variant: int, aggressive: bool) -> None:
    n = SAMPLE_RATE * DURATION_SEC
    fade = int(SAMPLE_RATE * 0.08)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        frames = bytearray()
        for i in range(n):
            t = i / SAMPLE_RATE
            s = tone_sample(t, base, variant, aggressive)
            if i < fade:
                s *= i / max(1, fade)
            elif i > n - fade:
                s *= (n - i) / max(1, fade)
            s = clamp(s)
            v = int(s * 32767.0)
            frames.extend(struct.pack("<hh", v, v))
        wf.writeframes(frames)


def main() -> None:
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    theme_root = os.path.join(repo, "UnityProject", "Assets", "StreamingAssets", "Theme")
    for mode, base in MODE_BASES.items():
        target = os.path.join(theme_root, mode, "ambient_gameplay_01.wav")
        write_loop(target, base, 0, aggressive=False)
        alt = os.path.join(theme_root, "Gameplay", mode, "ambient_gameplay_02.wav")
        write_loop(alt, [b * 1.05946 for b in base], 1, aggressive=True)
        soft = os.path.join(theme_root, mode, "ambient_live_soft_01.wav")
        write_loop(soft, base, 2, aggressive=False)
        aggr = os.path.join(theme_root, mode, "ambient_live_aggressive_01.wav")
        write_loop(aggr, [b * 1.12246 for b in base], 3, aggressive=True)
        print(f"[ok] {mode}")


if __name__ == "__main__":
    main()

