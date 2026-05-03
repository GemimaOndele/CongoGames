# CongoGames — Script automatique audio / vidéo

Le script **`download_audio_video.py`** télécharge des fichiers **réellement différents** (plus de FreePD : site fermé) depuis :

- **Musique :** liens directs **OpenGameArt.org** (voir la licence sur chaque page).
- **Vidéo :** échantillons MP4 publics **distincts** par mode (évite les doublons SHA256).

## Prérequis

- **Python 3** (`python --version`).
- Connexion Internet.

## Commande (à la racine du dépôt `Congogame`)

```powershell
cd C:\Congogame
python nouveauclaudeai\download_audio_video.py
```

Pour **écraser** les anciens fichiers (corriger des doublons déjà présents dans Unity) :

```powershell
python nouveauclaudeai\download_audio_video.py --force
```

- Ferme **Unity** (et l’explorateur sur un MP4 verrouillé) si tu vois `Permission denied` ou `IO [Errno 13]`.
- Avec `--force`, d’anciens `Theme/.../loop.mp4` dupliqués sont **supprimés** pour éviter les faux doublons au script PowerShell.

Le script déduit automatiquement : `Congogame\UnityProject`.

## Après exécution

1. Unity — **Ctrl+R** (refresh).
2. Vérification doublons :

```powershell
pwsh -File tools\Verify-AudioFiles.ps1
```

## Documentation liée

| Fichier | Contenu |
|---------|---------|
| [`README.md`](README.md) | Ce dossier |
| [`../docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md`](../docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md) | Guide manuel + liens |
| [`../newclaudeai/QUICK_START_DOWNLOAD.md`](../newclaudeai/QUICK_START_DOWNLOAD.md) | Procédure Downloads + batch |

---

**Note :** les URLs vidéo sont des échantillons techniques (schéma **un fichier ≠ un mode**). Tu peux les remplacer plus tard par des boucles thématiques (Pixabay, Mixkit, etc.) en gardant les noms `background.mp4` par dossier `Theme/<mode>/`.
