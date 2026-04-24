# Fonds d’écran : vidéo par mode + 3D animé (show TV)

Ce document finalise le flux **un fond par mini-jeu** : d’abord la **vidéo** (si vous la fournissez), sinon le **plateau 3D** procédural animé en temps réel, puis repli 2D.

## Modes reconnus

`quiz`, `semantic`, `word-scramble`, `crossword-lite`, `blind-test`, `mystery-word`, `memory`, `speed-chrono`, `image-guess` — liste dans [`ThemeModeCatalog.cs`](../UnityProject/Assets/Scripts/Presentation/ThemeModeCatalog.cs).

## Ordre de priorité (fond plein écran)

1. **URL** `backgroundVideoUrl` (session debug F10 ou `RemoteThemeMediaConfig` / `StreamingAssets/Theme/remote_media.json`) — le fichier doit être une **URL HTTPS directe** vers un `.mp4` / `.webm` (pas une page YouTube).
2. **Fichier local** dans `UnityProject/Assets/StreamingAssets/Theme/<modeId>/` — le premier nom existant gagne, dans l’ordre :
   - `background.mp4`, `background.webm`, `loop.mp4`, `loop.webm`, `theatre.mp4`, `theatre.webm`, `show.mp4`, `show.webm`
3. Puis le même ordre de noms dans **`Theme/`** à la racine (fond **global** pour tous les modes).
4. **Plateau 3D animé** (`VirtualShowStage3D`) si `PlayerPrefs` **`CongoUseVirtual3D`** = 1 (défaut) — lumière + primitives + légère caméra, **piloté par `modeId`** (palette distincte par jeu).
5. **Slides** optionnelles si `use_slides.flag` (voir [ROADMAP_UI_3D](ROADMAP_UI_3D.md)).
6. **Fond 2D synthétique** (`SyntheticVideoBackground`) — bandes animées.

## Fichier `remote_media.json`

- **Fichier versionné** [`Theme/remote_media.json`](../UnityProject/Assets/StreamingAssets/Theme/remote_media.json) : contient des **URL de démo** (fichier média en **HTTPS direct**) pour que les previews fonctionnent sans placer de vidéos en local. Modes alimentés avec une rotation de courtes sources **CC0 / exemples pédagogiques** (ex. MDN *flower*, W3Schools *mov_bbb*). **Connexion Internet requise** au moment du `VideoPlayer` ; pour le **hors-ligne** ou des fonds 100 % locaux, renommez ce fichier (ex. `remote_media.json.off`) ou videz `backgroundVideoUrl` dans chaque entrée — le jeu repassera sur la **vidéo locale** `StreamingAssets/Theme/...` puis sur le **plateau 3D**.
- **Modèle vierge** (copie pour repartir de zéro) : [`remote_media.example.json`](../UnityProject/Assets/StreamingAssets/Theme/remote_media.example.json).
- Les URLs doivent pointer vers un **fichier** (`.mp4`, `.webm`, etc.), pas une page YouTube. Vous restez responsable des droits et de la bande passante côté hébergeur.

## Créer les dossiers par mode dans le projet

Dans l’éditeur Unity : **CongoGames → Thème → Créer dossiers fond vidéo par mode (StreamingAssets)**.  
Cela crée un sous-dossier par mode + un fichier `LISEZMOI_fond_video.txt` (consignes).

## Réglages joueur

- **F9** : fond 3D activé / désactivé + **Appliquer** (recharge le thème courant).  
- **F10** : surcharges d’URL musique / bandeau / fond plein écran (session).

## Voir aussi

- [THEME_YOUTUBE_AND_STREAMING.md](THEME_YOUTUBE_AND_STREAMING.md) — pourquoi les liens YouTube ne vont pas dans `VideoPlayer`, solutions **HTTPS directe**, **CDN** ou import local **hors dépôt**, références inspiration.  
- [ROADMAP_UI_3D.md](ROADMAP_UI_3D.md) — RenderTexture, qualité, limitations socle.  
- [3D_PRODUCTION_ITERATION.md](3D_PRODUCTION_ITERATION.md) — itération vers assets / scènes.  
- [UNITY_SCENE_SETUP.md](UNITY_SCENE_SETUP.md) §6 — scène perso vs bootstrap.
