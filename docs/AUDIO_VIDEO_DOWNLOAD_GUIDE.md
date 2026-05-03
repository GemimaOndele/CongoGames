# CongoGames — Guide téléchargement audio & vidéo (clé en main)

**Objectif :** 9+ musiques **réellement différentes** + 9 fonds vidéo **par mode** + repli global.  
**Durée :** ~20–40 min (téléchargements, renommage, copie, vérification).

**Chemins :** tout part de `UnityProject/Assets/`.

**Code actif (ne pas remplacer) :** `GameAudioManager` = `Assets/Scripts/Audio/GameAudioManager.cs` — les copies dans `files/GameAudioManager.cs` sont **référence seulement** (voir `files/README_AUDIO_INTEGRATION.md`).

**Démarrage ultra court + batch de copie :** [`newclaudeai/QUICK_START_DOWNLOAD.md`](../newclaudeai/QUICK_START_DOWNLOAD.md) et [`newclaudeai/download-and-organize-audio-video.bat`](../newclaudeai/download-and-organize-audio-video.bat) (après avoir placé les fichiers dans `C:\Downloads\CongoGames`).

**Téléchargement auto (Python) :** [`nouveauclaudeai/download_audio_video.py`](../nouveauclaudeai/download_audio_video.py) — ex. `python nouveauclaudeai/download_audio_video.py` (fermer Unity si `Permission denied` sur les MP4). Voir [`nouveauclaudeai/RUN_THIS_FIRST.md`](../nouveauclaudeai/RUN_THIS_FIRST.md).

**Dossier `claudeai/` :** renvoie vers ce guide ; **source détaillée** = **ce fichier** (`docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md`).

---

## Règles Unity (à respecter)

| Type | Dossier | Noms de fichiers |
|------|---------|------------------|
| BGM | `Assets/Resources/Audio/BGM/` | `quiz_theme`, `battle_theme`, `lobby_theme`, etc. + extension `.wav` / `.ogg` / `.mp3` |
| Vidéo | `Assets/StreamingAssets/Theme/<modeId>/` | `background.mp4`, `loop.mp4`, `show.mp4`, `theatre.mp4` (voir `ThemeModeCatalog.cs`) |
| Repli | `Theme/_global/` ou `Theme/show.mp4` | optionnel |

- **`Theme/playlist/`** : réservé **blind-test** + **image-guess** (playlist) — ne sert pas de BGM unique pour tout le jeu.
- **F9 en jeu** : activer **« Alterner 3D ↔ vidéos »** pour enchaîner **MP4 Theme + plateau 3D** (défaut recommandé dans le code récent).

---

## PARTIE 1 — Musiques (liens + renommage)

Télécharge depuis chaque page (bouton **Download**). Vérifie la **licence** sur la fiche (CC0, CC-BY, etc.).

| # | Fichier cible `Resources/Audio/BGM/` | Lien direct |
|---|--------------------------------------|-------------|
| 1 | `quiz_theme` | [OpenGameArt — Suspense](https://opengameart.org/content/suspense) |
| 2 | `battle_theme` | [OpenGameArt — Battle Theme A](https://opengameart.org/content/battle-theme-a) |
| 3 | `speed_chrono_theme` | [Looperman — recherche](https://www.looperman.com/loops/search?keywords=game+edm) **ou** [Pixabay — sound game](https://pixabay.com/sound-effects/search/game/) (boucle courte énergique) |
| 4 | `memory_theme` | [OpenGameArt — CC0 Calm Relaxing](https://opengameart.org/content/cc0-calm-relaxing-music-0) |
| 5 | `word_scramble_theme` | [Mixkit — game SFX / musique](https://mixkit.co/free-sound-effects/game/) (choisir une boucle « puzzle / light ») |
| 6 | `crossword_theme` | [OpenGameArt — CC0 Retro Music](https://opengameart.org/content/cc0-retro-music) |
| 7 | `mystery_word_theme` | [OpenGameArt — Infiltration](https://opengameart.org/content/infiltration) |
| 8 | `semantic_theme` | [Pixabay — ambient](https://pixabay.com/sound-effects/search/ambient/) (boucle douce) |
| 9 | `image_to_word_theme` | [OpenGameArt — Happy Ukelele Island](https://opengameart.org/content/happy-ukelele-island-surfing-theme) |
| — | `lobby_theme` | [Freesound — djembe loop CC0](https://freesound.org/search/?q=djembe+loop&f=license%3A%22Creative+Commons+0%22) |
| — | `blind_test_theme` | (option) piste distincte ; le mode blind utilise aussi `Theme/playlist/` côté streaming. |

**Copie :** renommer en `quiz_theme.mp3` (ou `.wav` / `.ogg`) et placer dans  
`UnityProject/Assets/Resources/Audio/BGM/`

---

## PARTIE 2 — Vidéos par mode

Pour chaque ligne : une vidéo **.mp4** (ou **.webm**), renommée en **`background.mp4`** et/ou **`loop.mp4`** (les deux = plus de rotation).

| Dossier `StreamingAssets/Theme/...` | Recherche / thème | Lien pour parcourir & télécharger |
|------------------------------------|-------------------|-------------------------------------|
| `quiz/` | game show, lumières | [Pixabay — game show](https://pixabay.com/videos/search/game%20show/) |
| `semantic/` | abstract, particules | [Pixabay — abstract](https://pixabay.com/videos/search/abstract/) |
| `word-scramble/` | lettres, mots | [Pixabay — letters](https://pixabay.com/videos/search/letters/) |
| `crossword-lite/` | puzzle, grille | [Mixkit — puzzle video](https://mixkit.co/free-stock-video/search/puzzle/) |
| `blind-test/` | musique, notes | [Pixabay — music](https://pixabay.com/videos/search/music/) |
| `mystery-word/` | mystère, fog | [Pixabay — mystery](https://pixabay.com/videos/search/mystery/) |
| `memory/` | bulles, calme | [Pixabay — bubbles](https://pixabay.com/videos/search/bubbles/) |
| `speed-chrono/` | vitesse | [Pixabay — speed](https://pixabay.com/videos/search/speed/) |
| `image-guess/` | confetti, couleur | [Mixkit — confetti](https://mixkit.co/free-stock-video/search/confetti/) |

**Repli global (optionnel) :** `StreamingAssets/Theme/show.mp4` ou `Theme/_global/background.mp4`.

Exemple complet :  
`UnityProject/Assets/StreamingAssets/Theme/quiz/background.mp4`

---

## PARTIE 3 — Vérifier les doublons (obligatoire)

À la racine du dépôt `Congogame` :

```powershell
pwsh -File tools/Verify-AudioFiles.ps1
```

Ou :

```powershell
pwsh -File tools/Verify-AudioFiles.ps1 -ProjectRoot ".\UnityProject"
```

**Sortie :** si **DOUBLON**, c’est le **même fichier** renommé → remplace par une autre piste / une autre vidéo.  
(Script analogique : `claudeai/Verify-AudioFiles.ps1` appelle le même outil.)

---

## PARTIE 4 — Unity

1. **Refresh** les assets (Projet Unity → clic droit → Refresh, ou réouverture).
2. **Play** → changer de mini-jeu → la BGM doit suivre (`GameAudioManager` + `ThemeMusicPlayer`).
3. **F9** → confirmer alternance 3D / vidéos si tu as des MP4.
4. **Test clavier (éditeur)** : composant `AudioLiveSmokeTest` sur un GameObject (`Assets/Scripts/Debug/`).

---

## Avertissements Unity sur les MP4 (H.264 / couleurs)

Messages du type **« Unexpected timestamp values detected… not encoded with the baseline profile »** ou **« Color primaries 0 is unknown… »** viennent souvent de fichiers MP4 **H.264 High/Main** ou sans métadonnées de couleur lisibles par **Windows Media Foundation**.

À la racine du dépôt, avec **ffmpeg** installé :

```powershell
pwsh -File tools/Reencode-ThemeMp4-Baseline.ps1
```

Le script réencode les `.mp4` sous `StreamingAssets/Theme/` en **profil baseline**, **yuv420p**, couleurs **bt709**, puis tu fais **Ctrl+R** dans Unity. Ferme Unity avant si un fichier est verrouillé.

---

## Si « ça ne change pas »

Sans fichiers **distincts** sur le disque, le jeu ne peut pas inventer 9 ambiances différentes.

Fournir pour diagnostic :

1. Sortie complète de `tools/Verify-AudioFiles.ps1`
2. Liste des fichiers dans `Resources/Audio/BGM/` et `StreamingAssets/Theme/` (noms + tailles)
3. Extraits Console Unity (erreurs rouges)

---

## Liens avec les autres dossiers du dépôt

| Emplacement | Rôle |
|-------------|------|
| **`docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md`** (ce fichier) | Procédure téléchargement + chemins Unity |
| **`docs/FREE_THEME_MEDIA_SOURCES.md`** | Portails légaux (sans URLs d’assets précis) |
| **`docs/AUDIO_ASSETS_FR.md`** | Rappel FR court |
| **`files/README_AUDIO_INTEGRATION.md`** | Spec code, Resources.Load, API — **ne pas** remplacer le `GameAudioManager` du projet par `files/GameAudioManager.cs` |
| **`claudeai/`** | Snapshot instructions Claude ; renvoie ici |

*Nous ne pouvons pas pousser les binaires MP3/MP4 depuis l’outil IA : tu dois télécharger depuis les sites ci-dessus et copier localement.*
