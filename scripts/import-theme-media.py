#!/usr/bin/env python3
"""
Normalise les médias de fond sous StreamingAssets/Theme.

Fonctions:
- Vérifie les dossiers par mode.
- Renomme automatiquement des vidéos vers les noms attendus:
  background, loop, show.
- Loggue ce qui manque par mode.

Usage:
  python scripts/import-theme-media.py
  python scripts/import-theme-media.py --apply
  python scripts/import-theme-media.py --root "c:/Congogame/UnityProject/Assets/StreamingAssets/Theme" --apply
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from pathlib import Path
from typing import List, Sequence


MODE_IDS: Sequence[str] = (
    "quiz",
    "semantic",
    "word-scramble",
    "crossword-lite",
    "blind-test",
    "mystery-word",
    "memory",
    "speed-chrono",
    "image-guess",
)

GLOBAL_ID = "_global"
EXPECTED_STEMS: Sequence[str] = ("background", "loop", "show")
EXTS: Sequence[str] = (".mp4", ".webm", ".mov", ".m4v")


@dataclass
class ModeReport:
    mode: str
    renamed: List[str]
    missing: List[str]
    existing: List[str]


def list_videos(folder: Path) -> List[Path]:
    if not folder.exists():
        return []
    return sorted(
        [p for p in folder.iterdir() if p.is_file() and p.suffix.lower() in EXTS],
        key=lambda p: p.name.lower(),
    )


def canonical_paths(folder: Path) -> List[Path]:
    return [folder / f"{stem}.mp4" for stem in EXPECTED_STEMS]


def detect_existing(folder: Path) -> List[str]:
    out: List[str] = []
    for stem in EXPECTED_STEMS:
        found = False
        for ext in EXTS:
            p = folder / f"{stem}{ext}"
            if p.exists():
                out.append(p.name)
                found = True
                break
        if not found:
            out.append("-")
    return out


def pick_sources(videos: List[Path], folder: Path) -> List[Path]:
    reserved = set()
    for stem in EXPECTED_STEMS:
        for ext in EXTS:
            reserved.add((folder / f"{stem}{ext}").resolve())
    return [v for v in videos if v.resolve() not in reserved]


def normalize_mode(folder: Path, mode: str, apply: bool) -> ModeReport:
    folder.mkdir(parents=True, exist_ok=True)
    videos = list_videos(folder)
    extras = pick_sources(videos, folder)
    renamed: List[str] = []

    for stem in EXPECTED_STEMS:
        already = None
        for ext in EXTS:
            p = folder / f"{stem}{ext}"
            if p.exists():
                already = p
                break
        if already is not None:
            continue
        if not extras:
            continue
        src = extras.pop(0)
        target = folder / f"{stem}{src.suffix.lower()}"
        if apply:
            src.rename(target)
        renamed.append(f"{src.name} -> {target.name}")

    existing = detect_existing(folder)
    missing = [stem for i, stem in enumerate(EXPECTED_STEMS) if existing[i] == "-"]
    return ModeReport(mode=mode, renamed=renamed, missing=missing, existing=existing)


def print_report(root: Path, reports: List[ModeReport], apply: bool) -> None:
    print(f"[Theme Import] root={root}")
    print(f"[Theme Import] mode={'APPLY' if apply else 'DRY-RUN'}")
    print("")
    for r in reports:
        print(f"== {r.mode} ==")
        print(f"existing: background={r.existing[0]}, loop={r.existing[1]}, show={r.existing[2]}")
        if r.renamed:
            for line in r.renamed:
                print(f"renamed: {line}")
        if r.missing:
            print(f"missing: {', '.join(r.missing)}")
        else:
            print("missing: none")
        print("")

    missing_modes = [r.mode for r in reports if r.missing]
    if missing_modes:
        print("[Theme Import] Modes incomplets:", ", ".join(missing_modes))
    else:
        print("[Theme Import] Tous les modes ont background/loop/show.")


def main() -> int:
    parser = argparse.ArgumentParser(description="Normalise les vidéos de thème CongoGames.")
    parser.add_argument(
        "--root",
        default="c:/Congogame/UnityProject/Assets/StreamingAssets/Theme",
        help="Dossier Theme racine",
    )
    parser.add_argument("--apply", action="store_true", help="Applique les renommages")
    args = parser.parse_args()

    root = Path(args.root)
    modes = [GLOBAL_ID, *MODE_IDS]
    reports: List[ModeReport] = []
    for mode in modes:
        folder = root if mode == GLOBAL_ID else root / mode
        reports.append(normalize_mode(folder, mode, args.apply))

    print_report(root, reports, args.apply)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
