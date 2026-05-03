#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
CongoGames — Telecharge et place BGM + videos Theme (URLs publiques directes).

Sources audio : OpenGameArt.org (fichiers /sites/default/files/...), licences sur chaque page.
Sources video : echantillons MP4 publics (chaque mode = fichier different pour eviter doublons SHA256).

Usage (depuis le depot) :
  python nouveauclaudeai/download_audio_video.py

Le projet Unity est deduit : .../Congogame/UnityProject (parent du dossier nouveauclaudeai).
"""

from __future__ import annotations

import argparse
import os
import stat
import sys
import subprocess
import urllib.error
import urllib.request
from pathlib import Path

# Racine du depot = parent de ce dossier
_REPO = Path(__file__).resolve().parent.parent
PROJECT_ROOT = _REPO / "UnityProject"
BGM_DIR = PROJECT_ROOT / "Assets" / "Resources" / "Audio" / "BGM"
VIDEO_ROOT = PROJECT_ROOT / "Assets" / "StreamingAssets" / "Theme"

# User-Agent : certains serveurs refusent le UA Python par defaut
UA = "Mozilla/5.0 (compatible; CongoGames-asset-fetch/1.0; +https://example.local)"

# mp3/ogg/wav — noms finaux = ce que charge GameAudioManager (extension dans le fichier)
# Attributions : voir chaque URL sur opengameart.org (licences CC0 / CC-BY selon page).
AUDIO_SOURCES: dict[str, str] = {
    "quiz_theme.mp3": "https://opengameart.org/sites/default/files/suspense.mp3",
    "battle_theme.mp3": "https://opengameart.org/sites/default/files/battleThemeA.mp3",
    "speed_chrono_theme.mp3": "https://opengameart.org/sites/default/files/Project%202%20marioish_0.mp3",
    "memory_theme.mp3": "https://opengameart.org/sites/default/files/Forest_Ambience.mp3",
    "word_scramble_theme.ogg": "https://opengameart.org/sites/default/files/puppy_0.ogg",
    "crossword_theme.ogg": "https://opengameart.org/sites/default/files/Cunning%20plan_0.ogg",
    "mystery_word_theme.mp3": "https://opengameart.org/sites/default/files/infiltration_2.mp3",
    "semantic_theme.ogg": "https://opengameart.org/sites/default/files/dungeon002_0.ogg",
    "image_to_word_theme.mp3": "https://opengameart.org/sites/default/files/ukele.mp3",
    "lobby_theme.wav": "https://opengameart.org/sites/default/files/bonusgame.wav",
    "blind_test_theme.wav": "https://opengameart.org/sites/default/files/Mysterious_Futuristic_8_bit_Music_loop.wav",
}

# Un fichier MP4 distinct par mode (pas de copie du meme URL)
VIDEO_SOURCES: dict[str, str] = {
    "quiz": "https://www.w3schools.com/html/mov_bbb.mp4",
    "semantic": "https://download.samplelib.com/mp4/sample-5s.mp4",
    "word-scramble": "https://download.samplelib.com/mp4/sample-10s.mp4",
    "crossword-lite": "https://download.samplelib.com/mp4/sample-15s.mp4",
    "blind-test": "https://filesamples.com/samples/video/mp4/sample_640x360.mp4",
    "mystery-word": "https://filesamples.com/samples/video/mp4/sample_960x540.mp4",
    "memory": "https://filesamples.com/samples/video/mp4/sample_1280x720.mp4",
    "speed-chrono": "https://filesamples.com/samples/video/mp4/sample_1920x1080.mp4",
    "image-guess": "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.mp4",
}

GLOBAL_VIDEO = "https://static.videezy.com/system/resources/previews/000/000/168/original/Record.mp4"

EXPECTED_VIDEO_MODES = frozenset(VIDEO_SOURCES.keys())


def setup_directories() -> None:
    print("[dirs] Creation des dossiers...")
    BGM_DIR.mkdir(parents=True, exist_ok=True)
    print(f"       OK {BGM_DIR}")
    for mode in VIDEO_SOURCES:
        d = VIDEO_ROOT / mode
        d.mkdir(parents=True, exist_ok=True)
        print(f"       OK {d}")
    print()


def force_unlock_delete(path: Path) -> None:
    """Supprime un fichier verrouille en lecture seule (souvent sous Windows)."""
    if not path.is_file():
        return
    try:
        path.chmod(stat.S_IWRITE)
    except OSError:
        pass
    try:
        path.unlink()
    except OSError:
        pass


def remove_other_stem_versions(dest_path: Path) -> None:
    """Si on ecrit quiz_theme.mp3, supprime quiz_theme.wav / .ogg pour eviter doublons Resources."""
    stem = dest_path.stem
    for p in BGM_DIR.glob(stem + ".*"):
        if p.resolve() != dest_path.resolve() and p.is_file():
            try:
                p.unlink()
                print(f"       (supprime ancien {p.name})")
            except OSError:
                pass


def download_file(
    url: str,
    dest_path: Path,
    label: str,
    *,
    replace_stem: bool = False,
    overwrite: bool = False,
) -> bool:
    dest_path.parent.mkdir(parents=True, exist_ok=True)
    if replace_stem:
        remove_other_stem_versions(dest_path)
    if overwrite and dest_path.is_file():
        force_unlock_delete(dest_path)
    try:
        print(f"       -> {label} ... ", end="", flush=True)
        req = urllib.request.Request(url, headers={"User-Agent": UA})
        with urllib.request.urlopen(req, timeout=120) as resp:
            data = resp.read()
        tmp = dest_path.with_suffix(dest_path.suffix + ".part")
        tmp.write_bytes(data)
        try:
            tmp.replace(dest_path)
        except OSError:
            if dest_path.is_file():
                force_unlock_delete(dest_path)
            tmp.replace(dest_path)
        mb = dest_path.stat().st_size / (1024 * 1024)
        print(f"OK ({mb:.1f} MB)")
        return True
    except urllib.error.HTTPError as e:
        print(f"HTTP {e.code}")
        return False
    except urllib.error.URLError as e:
        print(f"URL {e.reason}")
        return False
    except OSError as e:
        print(f"IO {e}")
        return False


def prune_extra_bgm() -> None:
    """Ne garde qu'un fichier par stem, celui defini dans AUDIO_SOURCES."""
    allowed = set(AUDIO_SOURCES.keys())
    for name in allowed:
        stem = Path(name).stem
        for p in BGM_DIR.glob(stem + ".*"):
            if p.name not in allowed and p.is_file():
                force_unlock_delete(p)
                print(f"       (retire doublon {p.name})")


def download_audio(force: bool) -> bool:
    print("[audio] Telechargement BGM...")
    ok = 0
    for filename, url in AUDIO_SOURCES.items():
        dest = BGM_DIR / filename
        if dest.exists() and not force:
            print(f"       skip {filename} (deja la)")
            ok += 1
            continue
        if download_file(url, dest, filename, replace_stem=True, overwrite=force):
            ok += 1
    if ok == len(AUDIO_SOURCES):
        prune_extra_bgm()
    print(f"       Resultat : {ok}/{len(AUDIO_SOURCES)} fichiers OK\n")
    return ok == len(AUDIO_SOURCES)


def download_videos(force: bool) -> bool:
    print("[video] Telechargement Theme/.../background.mp4 ...")
    ok = 0
    for mode, url in VIDEO_SOURCES.items():
        dest = VIDEO_ROOT / mode / "background.mp4"
        if dest.exists() and not force:
            print(f"       skip {mode}/background.mp4 (deja la)")
            ok += 1
            continue
        if download_file(url, dest, f"{mode}/background.mp4", overwrite=force):
            ok += 1

    show_path = VIDEO_ROOT / "show.mp4"
    if show_path.exists() and not force:
        print("       skip show.mp4 (deja la)")
        ok += 1
    else:
        if download_file(GLOBAL_VIDEO, show_path, "show.mp4", overwrite=force):
            ok += 1

    need = len(VIDEO_SOURCES) + 1
    print(f"       Resultat : {ok}/{need} fichiers OK\n")

    # Anciens loop.mp4 souvent copies du meme fichier -> doublons SHA256 ; retirer si on refait les fonds.
    if force and ok == need:
        for mode in VIDEO_SOURCES:
            lp = VIDEO_ROOT / mode / "loop.mp4"
            if lp.is_file():
                force_unlock_delete(lp)
                print(f"       (retire ancien {mode}/loop.mp4)")

    return ok == need


def verify_structure() -> bool:
    print("[check] Fichiers presents...")
    exts = (".mp3", ".wav", ".ogg", ".m4a")
    bgm = [f for f in os.listdir(BGM_DIR) if f.lower().endswith(exts)]
    print(f"       BGM : {len(bgm)}/{len(AUDIO_SOURCES)}")
    missing_modes = []
    for mode in sorted(EXPECTED_VIDEO_MODES):
        p = VIDEO_ROOT / mode / "background.mp4"
        if not p.is_file():
            missing_modes.append(mode)
    print(f"       Videos par mode : {len(EXPECTED_VIDEO_MODES) - len(missing_modes)}/{len(EXPECTED_VIDEO_MODES)}")
    if missing_modes:
        print(f"       Manquants : {', '.join(missing_modes)}")
    show_ok = (VIDEO_ROOT / "show.mp4").is_file()
    print(f"       show.mp4 : {'OK' if show_ok else 'MANQUANT'}")
    ok = (
        len(bgm) >= len(AUDIO_SOURCES)
        and not missing_modes
        and show_ok
    )
    if ok:
        print("       Structure OK.\n")
    else:
        print("       Encore des fichiers manquants.\n")
    return ok


def main() -> int:
    ap = argparse.ArgumentParser(description="Telecharge BGM + videos Theme pour CongoGames.")
    ap.add_argument(
        "--force",
        action="store_true",
        help="Re-telecharge meme si les fichiers existent (corrige les doublons).",
    )
    args = ap.parse_args()

    print("\n" + "=" * 60)
    print("  CongoGames — Telechargement audio / video")
    print("=" * 60 + "\n")
    print(f"  PROJECT_ROOT = {PROJECT_ROOT}\n")
    if args.force:
        print("  Mode --force : ecrasement des fichiers existants.\n")

    if not PROJECT_ROOT.is_dir():
        print(f"ERREUR : UnityProject introuvable : {PROJECT_ROOT}")
        print("  Place ce script sous Congogame/nouveauclaudeai/ ou definis le depot.")
        return 1

    setup_directories()
    audio_ok = download_audio(args.force)
    video_ok = download_videos(args.force)
    struct_ok = verify_structure()

    print("=" * 60)
    if audio_ok and video_ok and struct_ok:
        print("SUCCES.")
        print("\n  Etapes suivantes :")
        print("  1. Unity : Ctrl+R (refresh)")
        print("  2. Racine depot : pwsh -File tools/Verify-AudioFiles.ps1")
        print("")
        ps1 = _REPO / "tools" / "Verify-AudioFiles.ps1"
        if ps1.is_file():
            print("  Lancement Verify-AudioFiles.ps1 ...\n")
            try:
                subprocess.run(
                    ["pwsh", "-NoProfile", "-File", str(ps1)],
                    cwd=str(_REPO),
                    check=False,
                )
            except FileNotFoundError:
                print("  (pwsh absent — lance manuellement la commande ci-dessus)\n")
    else:
        print("TERMINE AVEC ERREURS OU FICHIERS MANQUANTS.")
        print("  Verifie la connexion, relance, ou importe les pistes manuellement")
        print("  (voir docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md).\n")
    print("=" * 60 + "\n")

    return 0 if (audio_ok and video_ok and struct_ok) else 2


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        print("\nAnnule.\n")
        raise SystemExit(1)
