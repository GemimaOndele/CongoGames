#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
CongoGames — Musiques de fond synthétiques (numpy + pydub → MP3).
Remplace les fichiers dans UnityProject/Assets/Resources/Audio/BGM/
Prérequis : pip install pydub numpy | ffmpeg sur le PATH
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np

try:
    from pydub import AudioSegment
except ImportError as e:
    raise SystemExit("Installe : pip install pydub numpy") from e

SR = 44100


def ns(sec: float) -> int:
    return max(1, int(SR * sec))


def to_segment(samples: np.ndarray, sr: int = SR) -> AudioSegment:
    samples = np.asarray(samples, dtype=np.float64)
    samples = np.clip(samples, -1.0, 1.0) * 0.92
    pcm = (samples * 32767.0).astype(np.int16)
    return AudioSegment(
        pcm.tobytes(),
        frame_rate=sr,
        sample_width=2,
        channels=1,
    )


def hz(note: str) -> float:
    """Note style 'C4', 'A4', 'F#3'."""
    notes = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]
    if len(note) < 2:
        raise ValueError(note)
    oct_s = int(note[-1])
    name = note[:-1]
    semitone = notes.index(name)
    midi = (oct_s + 1) * 12 + semitone
    return 440.0 * (2.0 ** ((midi - 69) / 12.0))


def sine(t: np.ndarray, f: float, ph: float = 0.0) -> np.ndarray:
    return np.sin(2.0 * math.pi * f * t / SR + ph)


def square(t: np.ndarray, f: float) -> np.ndarray:
    return np.sign(np.sin(2.0 * math.pi * f * t / SR))


def env_adsr(n: int, a=0.05, d=0.1, s=0.7, r=0.15) -> np.ndarray:
    """Envelope length n samples."""
    out = np.ones(n, dtype=np.float64)
    a_n = min(int(SR * a), n // 4)
    d_n = min(int(SR * d), n // 4)
    r_n = min(int(SR * r), n // 4)
    if a_n > 0:
        out[:a_n] *= np.linspace(0, 1, a_n)
    if d_n > 0 and a_n + d_n < n:
        out[a_n : a_n + d_n] *= np.linspace(1, s, d_n)
        out[a_n + d_n : -r_n if r_n else None] *= s
    if r_n > 0:
        out[-r_n:] *= np.linspace(s if d_n else 1, 0, r_n)
    return out


def beat_click(t: np.ndarray, bpm: float, accent_pattern: list[float]) -> np.ndarray:
    """Impulsions par mesure (pattern longueur arbitraire)."""
    spb = 60.0 / bpm / 4.0  # double croches
    out = np.zeros_like(t, dtype=np.float64)
    step = int(SR * spb)
    if step < 1:
        return out
    i = 0
    p = 0
    while i < len(t):
        amp = accent_pattern[p % len(accent_pattern)]
        if amp > 0:
            k = min(800, len(t) - i)
            burst = np.sin(2 * math.pi * 120 * np.arange(k) / SR) * amp * 0.4
            out[i : i + k] += burst * np.hanning(k)
        i += step
        p += 1
    return out


def layer_melody(
    duration_sec: float,
    notes: list[tuple[float, float, float]],
    waveform="sine",
) -> np.ndarray:
    """notes: (freq_hz, start_sec, dur_sec)."""
    n = ns(duration_sec)
    t = np.arange(n, dtype=np.float64)
    acc = np.zeros(n, dtype=np.float64)
    for f, st, du in notes:
        i0 = int(st * SR)
        ln = int(du * SR)
        if i0 >= n or ln <= 0:
            continue
        seg = t[i0 : i0 + ln] - i0
        if waveform == "square":
            w = square(seg, f)
        else:
            w = sine(seg, f)
        e = env_adsr(len(w))
        acc[i0 : i0 + len(w)] += w * e * 0.35
    return acc


def render_quiz() -> np.ndarray:
    """Suspense type jeu TV — arpèges montants."""
    dur = 36.0
    t = np.arange(ns(dur), dtype=np.float64)
    base = [hz("C3"), hz("E3"), hz("G3"), hz("B3")]
    acc = np.zeros_like(t)
    for k in range(0, len(t), int(SR * 0.45)):
        idx = (k // int(SR * 0.45)) % 8
        f = base[idx % 4] * (1.02 ** (idx // 4))
        seg_len = min(int(SR * 0.35), len(t) - k)
        if seg_len <= 0:
            break
        seg = t[k : k + seg_len] - k
        acc[k : k + seg_len] += sine(seg, f) * env_adsr(seg_len) * 0.45
    acc += beat_click(t, 112, [1, 0.3, 0.5, 0.3, 1, 0.3, 0.5, 0.3]) * 0.25
    return acc


def render_battle() -> np.ndarray:
    """Staccato + kick — combat."""
    dur = 34.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = beat_click(t, 138, [1, 0, 0.8, 0, 1, 0, 0.7, 0]) * 0.55
    for k in range(0, len(t), int(SR * 0.25)):
        f = hz("D3") if (k // int(SR * 0.25)) % 2 == 0 else hz("A2")
        ln = min(int(SR * 0.12), len(t) - k)
        if ln <= 0:
            break
        seg = t[k : k + ln] - k
        acc[k : k + ln] += square(seg, f) * env_adsr(ln, a=0.01, d=0.05, s=0.5, r=0.05) * 0.5
    return acc


def render_speed_chrono() -> np.ndarray:
    """EDM rapide."""
    dur = 32.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = beat_click(t, 152, [1, 0.4, 0.6, 0.4, 0.8, 0.4, 0.6, 0.4]) * 0.4
    for k in range(0, len(t), int(SR * 0.125)):
        f = 220.0 * (1.06 ** ((k // int(SR * 0.125)) % 12))
        ln = min(int(SR * 0.1), len(t) - k)
        seg = t[k : k + ln] - k
        acc[k : k + ln] += sine(seg, f) * env_adsr(ln, a=0.005, r=0.08) * 0.28
    return acc


def render_memory() -> np.ndarray:
    """Zen, lent."""
    dur = 40.0
    t = np.arange(ns(dur), dtype=np.float64)
    notes = []
    st = 0.0
    while st < dur - 1.0:
        notes.append((hz("E4"), st, 0.95))
        notes.append((hz("B4"), st + 0.5, 0.75))
        st += 2.5
    acc = layer_melody(dur, notes, "sine")
    acc += sine(t, hz("E3")) * 0.08
    return acc


def render_word_scramble() -> np.ndarray:
    """Arcade bloops."""
    dur = 33.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = beat_click(t, 120, [0.8, 0.2, 0.6, 0.2]) * 0.35
    for k in range(0, len(t), int(SR * 0.2)):
        f = 330 + (k % 7) * 40
        ln = min(int(SR * 0.08), len(t) - k)
        seg = t[k : k + ln] - k
        acc[k : k + ln] += square(seg, f) * env_adsr(ln, r=0.04) * 0.32
    return acc


def render_crossword() -> np.ndarray:
    """Chiptune calme."""
    dur = 38.0
    scale = [hz("C4"), hz("D4"), hz("E4"), hz("G4")]
    t = np.arange(ns(dur), dtype=np.float64)
    acc = np.zeros_like(t)
    st = 0.0
    i = 0
    while st < dur - 0.5:
        f = scale[i % len(scale)]
        ln = int(SR * 0.22)
        k = int(st * SR)
        if k + ln > len(t):
            break
        seg = t[k : k + ln] - k
        acc[k : k + ln] += square(seg, f) * env_adsr(ln) * 0.3
        st += 0.28
        i += 1
    return acc


def render_mystery_word() -> np.ndarray:
    """Mystère — glissando lent."""
    dur = 36.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = np.zeros_like(t)
    for k in range(len(t)):
        f = 180 + 120 * math.sin(k / SR * 0.15)
        acc[k] = math.sin(2 * math.pi * f * k / SR) * 0.22
    acc += sine(t, hz("A2")) * env_adsr(len(t), a=2, r=3) * 0.15
    return acc


def render_semantic() -> np.ndarray:
    """Ambiant réflexif."""
    dur = 40.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = sine(t, hz("F3")) * 0.12 + sine(t, hz("C4")) * 0.1
    phase = 2 * math.pi * hz("G3") * t / SR + 0.4 * np.sin(2 * math.pi * 0.25 * t / SR)
    acc += np.sin(phase) * 0.08
    return acc


def render_image_to_word() -> np.ndarray:
    """Pop joyeux — claps + motif rapide."""
    dur = 34.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = beat_click(t, 126, [1, 0.15, 0.7, 0.15, 0.9, 0.15, 0.7, 0.15]) * 0.42
    pattern = [hz("C5"), hz("E5"), hz("G5"), hz("C6")]
    st = 0.0
    i = 0
    while st < dur:
        f = pattern[i % 4]
        ln = int(SR * 0.15)
        k = int(st * SR)
        if k + ln > len(t):
            break
        seg = t[k : k + ln] - k
        acc[k : k + ln] += sine(seg, f) * env_adsr(ln, r=0.06) * 0.38
        st += 0.18
        i += 1
    return acc


def render_lobby() -> np.ndarray:
    """Afrobeat simplifié — syncopes + graves."""
    dur = 42.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = beat_click(t, 108, [1, 0.2, 0.65, 0.35, 0.85, 0.2, 0.75, 0.35]) * 0.48
    chord_t = [hz("G3"), hz("B3"), hz("D4")]
    for k in range(0, len(t), int(SR * 0.5)):
        for j, f in enumerate(chord_t):
            ln = min(int(SR * 0.25), len(t) - k)
            seg = t[k : k + ln] - k
            acc[k : k + ln] += sine(seg, f) * env_adsr(ln, r=0.12) * (0.18 - j * 0.03)
    acc += sine(t, hz("G2")) * 0.12
    return acc


def render_blind_test() -> np.ndarray:
    """Atmosphère énigmatique."""
    dur = 35.0
    t = np.arange(ns(dur), dtype=np.float64)
    acc = np.zeros_like(t)
    for k in range(len(t)):
        f = 200 + 80 * math.sin(k / float(SR) * 0.08)
        acc[k] += math.sin(2 * math.pi * f * k / SR) * 0.18
    acc += sine(t, hz("D4")) * 0.1 * (0.5 + 0.5 * np.sin(t / SR * 0.3))
    return acc


TRACKS: dict[str, callable] = {
    "quiz_theme": render_quiz,
    "battle_theme": render_battle,
    "speed_chrono_theme": render_speed_chrono,
    "memory_theme": render_memory,
    "word_scramble_theme": render_word_scramble,
    "crossword_theme": render_crossword,
    "mystery_word_theme": render_mystery_word,
    "semantic_theme": render_semantic,
    "image_to_word_theme": render_image_to_word,
    "lobby_theme": render_lobby,
    "blind_test_theme": render_blind_test,
}


def remove_other_extensions(bgm: Path, stem: str, keep_ext: str) -> None:
    for p in bgm.glob(stem + ".*"):
        if p.suffix.lower() != keep_ext.lower():
            try:
                p.unlink()
            except OSError:
                pass


def main() -> None:
    repo = Path(__file__).resolve().parent
    bgm = repo / "UnityProject" / "Assets" / "Resources" / "Audio" / "BGM"
    bgm.mkdir(parents=True, exist_ok=True)

    print("CongoGames — génération musiques synthétiques →", bgm)
    for stem, fn in TRACKS.items():
        print(f"  → {stem}.mp3 ...", end=" ", flush=True)
        samples = fn()
        seg = to_segment(samples)
        out = bgm / f"{stem}.mp3"
        seg.export(str(out), format="mp3", bitrate="192k")
        remove_other_extensions(bgm, stem, ".mp3")
        mb = out.stat().st_size / (1024 * 1024)
        print(f"OK ({mb:.2f} MB)")

    print("\nTerminé. Unity : Ctrl+R pour recharger les Assets.")


if __name__ == "__main__":
    main()
