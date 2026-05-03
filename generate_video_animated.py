#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
CongoGames — Vidéos de fond procédurales (OpenCV + numpy → MP4).
Écrit sous UnityProject/Assets/StreamingAssets/Theme/
Prérequis : pip install opencv-python numpy
Post-Unity : pwsh -File tools/Reencode-ThemeMp4-Baseline.ps1 (profil baseline / WMF)
"""

from __future__ import annotations

from pathlib import Path

import numpy as np

try:
    import cv2
except ImportError as e:
    raise SystemExit("Installe : pip install opencv-python numpy") from e

# Réglages (réduire résolution / durée pour aller plus vite)
WIDTH, HEIGHT = 1280, 720
FPS = 24
DURATION_SEC = 22


def writer_open(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    fourcc = cv2.VideoWriter_fourcc(*"mp4v")
    return cv2.VideoWriter(str(path), fourcc, FPS, (WIDTH, HEIGHT))


def blend(a: np.ndarray, b: np.ndarray, t: float) -> np.ndarray:
    return (a * (1 - t) + b * t).astype(np.uint8)


def frame_quiz(i: int, n: int) -> np.ndarray:
    frame = np.zeros((HEIGHT, WIDTH, 3), dtype=np.uint8)
    t = i / max(1, n - 1)
    hue = int((t * 180 + i * 2) % 180)
    for x in range(0, WIDTH, 90):
        h = int((hue + x // 30) % 180)
        color = cv2.cvtColor(np.uint8([[[h, 220, 255]]]), cv2.COLOR_HSV2BGR)[0, 0]
        bar_h = int(HEIGHT * (0.35 + 0.25 * np.sin(t * 8 + x * 0.01)))
        x0 = x + int(15 * np.sin(t * 6 + x * 0.02))
        cv2.rectangle(frame, (x0, HEIGHT - bar_h), (x0 + 55, HEIGHT), color.tolist(), -1)
    for _ in range(90):
        px = np.random.randint(0, WIDTH)
        py = np.random.randint(0, HEIGHT // 2)
        cv2.circle(frame, (px, py), 2, (0, 220, 255), -1)
    return frame


def frame_semantic(i: int, n: int) -> np.ndarray:
    frame = np.full((HEIGHT, WIDTH, 3), (40, 35, 25), dtype=np.uint8)
    t = i / max(1, n - 1)
    pts = []
    for k in range(18):
        ang = 2 * np.pi * k / 18 + t * 4
        r = 180 + 80 * np.sin(t * 3 + k)
        cx = int(WIDTH / 2 + r * np.cos(ang))
        cy = int(HEIGHT / 2 + r * np.sin(ang) * 0.7)
        pts.append((cx, cy))
        cv2.circle(frame, (cx, cy), 10, (255, 160, 80), -1)
    for a in range(len(pts)):
        b = (a + 1) % len(pts)
        cv2.line(frame, pts[a], pts[b], (200, 120, 60), 2)
    return frame


def frame_word_scramble(i: int, n: int) -> np.ndarray:
    frame = np.zeros((HEIGHT, WIDTH, 3), dtype=np.uint8)
    t = i / max(1, n - 1)
    letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    for k, ch in enumerate(letters):
        x = int(WIDTH * (0.05 + 0.9 * (k / 26)) + 40 * np.sin(t * 5 + k))
        y = int(HEIGHT * 0.4 + 120 * np.sin(t * 4 + k * 0.5))
        color = (40 + (k * 17) % 200, 80 + (k * 31) % 175, 200)
        cv2.putText(
            frame,
            ch,
            (x % (WIDTH - 40), y % (HEIGHT - 40)),
            cv2.FONT_HERSHEY_SIMPLEX,
            1.2,
            color,
            2,
            cv2.LINE_AA,
        )
    cv2.rectangle(frame, (0, 0), (WIDTH - 1, HEIGHT - 1), (0, 255, 200), 4)
    return frame


def frame_crossword(i: int, n: int) -> np.ndarray:
    frame = np.full((HEIGHT, WIDTH, 3), (45, 50, 55), dtype=np.uint8)
    t = i / max(1, n - 1)
    gs = 8
    cw = WIDTH // gs
    ch = HEIGHT // gs
    for gx in range(gs):
        for gy in range(gs):
            pulse = 0.4 + 0.6 * np.sin(t * 6 + gx * 0.7 + gy * 0.5) ** 2
            col = (int(80 * pulse), int(140 * pulse), int(220 * pulse))
            x0, y0 = gx * cw, gy * ch
            cv2.rectangle(frame, (x0, y0), (x0 + cw - 2, y0 + ch - 2), col, -1)
    return frame


def frame_blind_test(i: int, n: int) -> np.ndarray:
    frame = np.zeros((HEIGHT, WIDTH, 3), dtype=np.uint8)
    t = i / max(1, n - 1)
    cx, cy = WIDTH // 2, HEIGHT // 2
    for r in range(40, 420, 35):
        alpha = 0.3 + 0.4 * np.sin(t * 5 + r * 0.02)
        col = (int(200 * alpha), int(80 + 100 * alpha), int(255 * alpha))
        cv2.circle(frame, (cx, cy), r, col, 2)
    cv2.putText(
        frame,
        "~",
        (cx - 20, cy),
        cv2.FONT_HERSHEY_SIMPLEX,
        2.5,
        (200, 220, 255),
        3,
        cv2.LINE_AA,
    )
    return frame


def frame_mystery_word(i: int, n: int) -> np.ndarray:
    t = i / max(1, n - 1)
    y, x = np.ogrid[:HEIGHT, :WIDTH]
    r = (x * 0.15 + y * 0.12 + t * 80.0).astype(np.float32)
    b = 50.0 + 35.0 * np.sin(r * 0.02)
    g = 25.0 + 25.0 * np.sin(r * 0.025 + 1.0)
    red = 70.0 + 40.0 * np.sin(r * 0.018 + 2.0)
    frame = np.dstack(
        [np.clip(b, 0, 255), np.clip(g, 0, 255), np.clip(red, 0, 255)]
    ).astype(np.uint8)
    mx = int(WIDTH * (0.25 + 0.5 * (0.5 + 0.5 * np.sin(t * 2))))
    my = int(HEIGHT * (0.35 + 0.2 * np.sin(t * 3)))
    mw = int(WIDTH * (0.2 + 0.15 * np.sin(t * 4)))
    mh = int(HEIGHT * (0.15 + 0.1 * np.cos(t * 5)))
    cv2.rectangle(frame, (mx, my), (mx + mw, my + mh), (200, 120, 255), 3)
    return frame


def frame_memory(i: int, n: int) -> np.ndarray:
    frame = np.full((HEIGHT, WIDTH, 3), (35, 55, 40), dtype=np.uint8)
    t = i / max(1, n - 1)
    for k in range(28):
        bx = int(WIDTH * (0.1 + 0.8 * np.sin(t * 1.5 + k * 0.7)))
        by = int(HEIGHT * (0.2 + 0.6 * (k / 28.0) + 0.1 * np.sin(t * 2 + k)))
        br = 25 + (k % 7) * 4
        color = (80 + k * 5, 180 - k * 3, 200)
        cv2.circle(frame, (bx % WIDTH, by % HEIGHT), br, color, -1)
        cv2.circle(frame, (bx % WIDTH, by % HEIGHT), br, (255, 255, 255), 2)
    return frame


def frame_speed_chrono(i: int, n: int) -> np.ndarray:
    frame = np.zeros((HEIGHT, WIDTH, 3), dtype=np.uint8)
    t = i / max(1, n - 1)
    # Positions déterministes par traînée (meilleure compression que bruit par frame)
    for k in range(48):
        x = int(WIDTH * (0.1 + 0.8 * ((k * 97 + i * 3) % 1000) / 1000.0))
        y = int(HEIGHT * (0.1 + 0.8 * ((k * 211 + i * 5) % 1000) / 1000.0))
        x2 = int(x + 90 * np.cos(t * 12 + k * 0.4))
        y2 = int(y + 90 * np.sin(t * 12 + k * 0.4))
        cv2.line(frame, (x, y), (x2, y2), (60, 255, 120), 2)
    cv2.rectangle(frame, (40, HEIGHT - 80), (40 + int((WIDTH - 80) * (t % 1)), HEIGHT - 40), (0, 255, 100), -1)
    return frame


def frame_image_guess(i: int, n: int) -> np.ndarray:
    rng = np.random.default_rng(i)
    frame = np.full((HEIGHT, WIDTH, 3), (30, 40, 60), dtype=np.uint8)
    t = i / max(1, n - 1)
    for _ in range(140):
        x = rng.integers(0, WIDTH)
        y = rng.integers(0, HEIGHT)
        col = (int(rng.integers(80, 255)), int(rng.integers(80, 255)), int(rng.integers(100, 255)))
        cv2.circle(frame, (x, y), rng.integers(2, 8), col, -1)
    cv2.putText(
        frame,
        "PARTY",
        (WIDTH // 4, HEIGHT // 2),
        cv2.FONT_HERSHEY_SIMPLEX,
        2.2,
        (80, 200, 255),
        4,
        cv2.LINE_AA,
    )
    return frame


def frame_show(i: int, n: int) -> np.ndarray:
    frame = np.zeros((HEIGHT, WIDTH, 3), dtype=np.uint8)
    t = i / max(1, n - 1)
    for k in range(16):
        ang = 2 * np.pi * k / 16 + t * 3
        r = 100 + 40 * k
        cx = int(WIDTH / 2 + r * np.cos(ang))
        cy = int(HEIGHT / 2 + r * np.sin(ang))
        cv2.rectangle(
            frame,
            (cx - 30, cy - 30),
            (cx + 30, cy + 30),
            (40 + k * 12, 200, 255 - k * 10),
            -1,
        )
    cv2.putText(
        frame,
        "CONGO GAMES",
        (WIDTH // 8, HEIGHT // 2 - 20),
        cv2.FONT_HERSHEY_SIMPLEX,
        2.0,
        (50, 240, 255),
        4,
        cv2.LINE_AA,
    )
    return frame


GENERATORS = {
    "quiz": frame_quiz,
    "semantic": frame_semantic,
    "word-scramble": frame_word_scramble,
    "crossword-lite": frame_crossword,
    "blind-test": frame_blind_test,
    "mystery-word": frame_mystery_word,
    "memory": frame_memory,
    "speed-chrono": frame_speed_chrono,
    "image-guess": frame_image_guess,
}


def write_loop(path: Path, gen_fn) -> None:
    n = int(FPS * DURATION_SEC)
    w = writer_open(path)
    for i in range(n):
        frame = gen_fn(i, n)
        if frame.shape[1] != WIDTH or frame.shape[0] != HEIGHT:
            frame = cv2.resize(frame, (WIDTH, HEIGHT))
        w.write(frame)
    w.release()


def main() -> None:
    repo = Path(__file__).resolve().parent
    theme = repo / "UnityProject" / "Assets" / "StreamingAssets" / "Theme"
    print(f"CongoGames — génération vidéos {WIDTH}x{HEIGHT} @ {FPS} fps, {DURATION_SEC}s")
    print("Destination :", theme)

    for mode, gen in GENERATORS.items():
        out = theme / mode / "background.mp4"
        print(f"  → {mode}/background.mp4 ...", end=" ", flush=True)
        write_loop(out, gen)
        mb = out.stat().st_size / (1024 * 1024)
        print(f"OK ({mb:.1f} MB)")

    show = theme / "show.mp4"
    print("  → show.mp4 ...", end=" ", flush=True)
    write_loop(show, frame_show)
    mb = show.stat().st_size / (1024 * 1024)
    print(f"OK ({mb:.1f} MB)")

    print("\nTerminé. Optionnel : pwsh -File tools/Reencode-ThemeMp4-Baseline.ps1")
    print("Puis Unity : Ctrl+R.")


if __name__ == "__main__":
    main()
