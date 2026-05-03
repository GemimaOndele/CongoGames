# CongoGames — Démarrage rapide (clique → télécharge → copie auto)

**Problème confirmé :** des **doublons** (même fichier renommé) → une seule musique / un seul fond.  
**But :** pistes et vidéos **réellement différentes**, puis copie vers Unity via le batch.

**Durée :** ~20–30 min (téléchargements manuels obligatoires — pas de binaires dans le dépôt).

---

## Étape 0 — Dossier temporaire

Crée par exemple :

```
C:\Downloads\CongoGames
```

Tous les fichiers renommés vont **ici** avant le batch.

---

## Étape 1 — Musiques (11 stems = ce que charge `GameAudioManager`)

Clique **Download** sur chaque page, renomme **exactement** (extension `.mp3`, `.wav` ou `.ogg` — le batch prend la première trouvée).

| # | Fichier final | Lien | Note |
|---|---------------|------|------|
| 1 | `quiz_theme` | [OpenGameArt — Suspense](https://opengameart.org/content/suspense) | |
| 2 | `battle_theme` | [OpenGameArt — Battle Theme A](https://opengameart.org/content/battle-theme-a) | |
| 3 | `speed_chrono_theme` | [Looperman — recherche](https://www.looperman.com/loops/search?keywords=game+edm) ou [Pixabay — game](https://pixabay.com/sound-effects/search/game/) | boucle rapide |
| 4 | `memory_theme` | [OpenGameArt — CC0 Calm](https://opengameart.org/content/cc0-calm-relaxing-music-0) | |
| 5 | `word_scramble_theme` | [Mixkit — game](https://mixkit.co/free-sound-effects/game/) | style puzzle |
| 6 | `crossword_theme` | [OpenGameArt — CC0 Retro](https://opengameart.org/content/cc0-retro-music) | |
| 7 | `mystery_word_theme` | [OpenGameArt — Infiltration](https://opengameart.org/content/infiltration) | |
| 8 | `semantic_theme` | [Pixabay — ambient](https://pixabay.com/sound-effects/search/ambient/) | doux |
| 9 | `image_to_word_theme` | [OpenGameArt — Happy Ukelele](https://opengameart.org/content/happy-ukelele-island-surfing-theme) | |
| 10 | `lobby_theme` | [Freesound — djembe CC0](https://freesound.org/search/?q=djembe+loop&f=license%3A%22Creative+Commons+0%22) | **≠** `blind_test` |
| 11 | `blind_test_theme` | Piste distincte (ex. autre boucle OGA / Pixabay) | éviter copier-collé de `lobby_theme` |

Guide détaillé + licences : **[`../docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md`](../docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md)**.

---

## Étape 2 — Vidéos (9 modes + `show` optionnel)

Même dossier `C:\Downloads\CongoGames`. Noms **exacts** (le batch les mappe vers `Theme/<mode>/background.mp4`) :

| Fichier dans Downloads | Dossier Unity cible |
|------------------------|----------------------|
| `background_quiz.mp4` | `Theme/quiz/background.mp4` |
| `background_semantic.mp4` | `Theme/semantic/background.mp4` |
| `background_word-scramble.mp4` | `Theme/word-scramble/background.mp4` |
| `background_crossword-lite.mp4` | `Theme/crossword-lite/background.mp4` |
| `background_blind-test.mp4` | `Theme/blind-test/background.mp4` |
| `background_mystery-word.mp4` | `Theme/mystery-word/background.mp4` |
| `background_memory.mp4` | `Theme/memory/background.mp4` |
| `background_speed-chrono.mp4` | `Theme/speed-chrono/background.mp4` |
| `background_image-guess.mp4` | `Theme/image-guess/background.mp4` |
| `show.mp4` *(optionnel)* | `Theme/show.mp4` |

**Liens pour choisir des clips différents :** [Pixabay vidéos](https://pixabay.com/videos/), [Mixkit](https://mixkit.co/free-stock-video/). Varie le sujet par ligne pour que le script PowerShell ne signale plus de **DOUBLON** entre modes.

---

## Étape 3 — Lancer le batch

1. Ouvre **`download-and-organize-audio-video.bat`** si besoin et ajuste en tête (optionnel) :
   - `DOWNLOADS` — défaut `C:\Downloads\CongoGames`
   - Le script déduit `PROJECT` : repo `\UnityProject` (à côté de `newclaudeai\`)
2. Double-clique le `.bat` ou en invite de commandes :

```bat
cd /d C:\Congogame\newclaudeai
download-and-organize-audio-video.bat
```

Les musiques sont copiées vers `UnityProject\Assets\Resources\Audio\BGM\`, les vidéos vers `...\StreamingAssets\Theme\<mode>\`.

---

## Étape 4 — Unity + vérification

1. Unity : **Ctrl+R** (refresh).
2. À la racine du dépôt :

```powershell
pwsh -File tools\Verify-AudioFiles.ps1
```

Sortie **0** et aucune ligne **DOUBLON** → OK.

---

## Fichiers dans ce dossier

| Fichier | Rôle |
|---------|------|
| `QUICK_START_DOWNLOAD.md` | Ce guide court |
| `download-and-organize-audio-video.bat` | Copie Downloads → Unity |

Doc canonique longue : **`docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md`**. Intégration code : **`files/README_AUDIO_INTEGRATION.md`**.

---

**Je ne peux pas télécharger les MP3/MP4 à ta place** ; une fois les fichiers dans `C:\Downloads\CongoGames`, le batch fait le reste.
