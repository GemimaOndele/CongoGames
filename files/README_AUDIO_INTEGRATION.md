# CongoGames — Intégration Audio Complète

Ce fichier est le **résultat consolidé** de la spec audio (conversations Claude + alignement code). Les copies `files/GameAudioManager.cs` et `*_AudioPatch.cs` servent de **référence** ; l’exécution se fait via `UnityProject/Assets/Scripts/Audio/GameAudioManager.cs` (`CongoGames.Audio`).

**Télécharger des pistes / vidéos libres et les placer dans Unity :** **`docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md`** (détail) ; variante courte + script batch **`newclaudeai/QUICK_START_DOWNLOAD.md`**. Vérifier les doublons : `pwsh -File tools/Verify-AudioFiles.ps1`. **`claudeai/`** renvoie vers `docs/` ; **`newclaudeai/`** = quick start + `download-and-organize-audio-video.bat`.

## État d’intégration (projet réel `UnityProject/`)

Ces fichiers (`GameAudioManager.cs` à la racine de `files/`, patches `*_AudioPatch.cs`) sont une **référence** générée par Claude. **Ne remplacez pas** le script du dépôt par la copie `files/GameAudioManager.cs` (namespace global, API différente) : le code actif est :

- `UnityProject/Assets/Scripts/Audio/GameAudioManager.cs` (`CongoGames.Audio`)

Le **`RuntimeBootstrap`** attache `GameAudioManager` sur l’objet `ThemeMusic` **sans** saisie Inspector. Pour que les BGM / SFX listés plus bas soient pris en **jeu dès l’export** sans assigner chaque clip à la main, placez les assets importés dans Unity sous :

- **BGM (Resources)** : `Assets/Resources/Audio/BGM/` — noms de fichier **sans** extension pour `Resources.Load` : `quiz_theme`, `lobby_theme`, etc. (voir tableau ci-dessous, colonne *nom logique*).
- **SFX (Resources)** : `Assets/Resources/Audio/SFX/` — ex. `correct_answer.wav` → ressource `Audio/SFX/correct_answer`.

Au **Awake**, le `GameAudioManager` du dépôt appelle `EnsureAudioClipsFromResources()` : tout clip encore `null` est chargé si le fichier existe dans `Resources/Audio/...`.

**BGM par mode (pistes vraiment différentes)** : les fichiers sous `Resources/Audio/BGM/` (`*_theme`, sans extension dans `Resources.Load`) proviennent de **`StreamingAssets/Theme/<mode>/`** ou du thème global (`track01.ogg`, `ambient_*.wav`, etc.) — **pas** de `Theme/playlist/`. Ce dernier dossier est réservé au **blind test** et **image-guess** (logique `ThemeMusicPlayer` / playlist dédiée).

L’API **`DuckForRobot` / `RestoreFromRobot`** du patch `AIHostManager_AudioPatch.cs` est **implémentée** sur ce gestionnaire (atténue Theme + SFX + BGM inspector). En parallèle, le jeu utilise souvent **`BroadcastAudioMixCoordinator`** : évitez de cumuler TTS + appels manuels si les niveaux se battent.

**Rappel spec (hors `playlist/`)** : `StreamingAssets/Theme/playlist/` alimente **uniquement** le blind test / image-guess via `ThemeMusicPlayer`. La BGM « générale » des autres modes vient de `Theme/<mode>/`, `Resources/Audio/BGM/`, etc. — **jamais** de la playlist pour remplacer toute la BGM des mini-jeux.

**`preferDedicatedBgm`** : dans le dépôt, la valeur par défaut du **code** est **`true`** (les clips `Resources/Audio/BGM` remplacent la boucle Theme quand un clip existe pour le mode, selon la logique du script). Si un vieux message dit le contraire, c’est obsolète.

---

## Fichiers fournis

| Fichier | Rôle | Où le placer |
|---|---|---|
| `GameAudioManager.cs` | Gestionnaire central (cross-fade, duck, SFX) | `Assets/Scripts/Audio/` |
| `LiveEventClient_AudioPatch.cs` | Guide d'intégration pour les events TikTok | Référence — lire et copier dans `LiveEventClient.cs` |
| `AIHostManager_AudioPatch.cs` | Duck/restore pour la voix robot IA | Référence — lire et copier dans `AIHostManager.cs` |

---

## Étape 1 — Où placer les fichiers (deux chemins valides)

**Chemin A — recommandé pour l’export sans toucher l’Inspector** (utilisé par le bootstrap) :

- `Assets/Resources/Audio/BGM/` — noms logiques : `lobby_theme`, `quiz_theme`, `battle_theme`, `speed_chrono_theme`, `memory_theme`, `word_scramble_theme`, `crossword_theme`, `mystery_word_theme`, `semantic_theme`, `image_to_word_theme`, `blind_test_theme` (extensions `.wav` / `.ogg` / `.mp3` acceptées par Unity).
- `Assets/Resources/Audio/SFX/` — ex. `correct_answer`, `wrong_answer`, etc.

**Chemin B — optionnel** : `Assets/Audio/BGM/` et `Assets/Audio/SFX/` si tu préfères glisser les clips à la main sur le `GameAudioManager` (l’objet est tout de même créé par le runtime sur `ThemeMusic`).

> ⚠️ `Theme/playlist/` (StreamingAssets) reste **réservé** au blind test + image-guess. Ne pas s’en servir comme seule source BGM pour quiz, battle, etc.

---

## Étape 2 — Télécharger les sons

### BGM (musiques de fond)

| Fichier à créer | Source | Recherche |
|---|---|---|
| `lobby_theme.mp3` | freesound.org | `djembe loop afrobeat` |
| `quiz_theme.mp3` | opengameart.org | `Suspense` (CC0) |
| `battle_theme.mp3` | opengameart.org | `Battle Theme A` (CC0) |
| `speed_chrono_theme.mp3` | looperman.com | `EDM fast game loop` |
| `memory_theme.mp3` | opengameart.org | `CC0 Calm Relaxing Music` |
| `word_scramble_theme.mp3` | mixkit.co | `puzzle game music` |
| `crossword_theme.mp3` | opengameart.org | `CC0 Retro Music chiptune` |
| `mystery_word_theme.mp3` | opengameart.org | `Infiltration` (CC0) |
| `semantic_theme.mp3` | pixabay.com | `soft ambient game loop` |
| `image_to_word_theme.mp3` | opengameart.org | `Happy Ukelele Island` (CC0) |

### SFX (effets sonores)

| Fichier | Source | Recherche |
|---|---|---|
| `correct_answer.wav` | mixkit.co | `game correct answer` |
| `wrong_answer.wav` | mixkit.co | `game wrong buzzer` |
| `gift_received.wav` | freesound.org | `coins collect` |
| `new_viewer.wav` | freesound.org | `notification chime` |
| `battle_start.wav` | freesound.org | `battle horn` |
| `round_win.wav` | mixkit.co | `game win fanfare` |
| `timer_tick.wav` | freesound.org | `tick clock` |
| `timer_urgent.wav` | freesound.org | `countdown beep` |
| `crowd_cheer.wav` | freesound.org | `crowd applause short` |

### Format recommandé Unity
- **BGM** : `.mp3` ou `.ogg` (compression : Vorbis, qualité 70%)
- **SFX** : `.wav` (non compressé pour réactivité)

---

## Étape 3 — Setup Unity

1. Le **`RuntimeBootstrap`** crée en général l’objet **`ThemeMusic`** avec `GameAudioManager` + `ThemeMusicPlayer` — pas besoin de dupliquer à la main sauf scène de test isolée.
2. Au premier lancement, **`EnsureAudioClipsFromResources()`** remplit les slots vides depuis `Resources/Audio/...`.
3. Pour régler finement : sélectionner **`ThemeMusic`** en Play, vérifier **`Prefer Dedicated Bgm`**, **`Blend Dedicated Clips With Streaming Music`**, volumes — conformément au comportement voulu (spec Claude : BGM dédiée qui remplace Theme quand coché sans blend).

---

## Fonds vidéo (`ThemeBackgroundController` / `ThemeModeCatalog`)

Les noms de fichiers prioritaires sont notamment **`background.mp4`**, **`loop.mp4`**, **`show.mp4`**, **`theatre.mp4`** (voir `ThemeModeCatalog.BackgroundVideoFileNames`). Structure type :

```
Assets/StreamingAssets/Theme/
├── quiz/background.mp4
├── semantic/…
├── word-scramble/…
├── crossword-lite/…
├── blind-test/…
├── mystery-word/…
├── memory/…
├── speed-chrono/…
├── image-guess/…
├── track01.ogg (global audio possible)
└── _global/ ou Theme/show.mp4 (replis selon catalogue)
```

Dossiers additionnels reconnus : `Theme/Gameplay/<modeId>/`, `Theme/_dev_import/<modeId>/`.  
Si le **plateau 3D** masque ou remplace le fond : menu **F9** (`PlayerPrefsGui`) → désactiver **`CongoUseVirtual3D`** (`PresentationConfig.PrefsUseVirtual3D`) ou activer le mix vidéo/3D selon **`CongoMix3DWithVideo`**.

---

## Étape 4 — Intégrer dans LiveEventClient.cs

Ouvrir `LiveEventClient_AudioPatch.cs` et copier les appels dans ta méthode
qui reçoit les messages WebSocket (cherche `HandleMessage`, `OnMessage`, ou
la méthode qui parse les events TikTok).

Exemple minimal :
```csharp
// Dans ta méthode de réception d'events :
case "quiz": GameAudioManager.Instance?.OnQuizStart(); break;
case "battle": GameAudioManager.Instance?.OnBattleStart(); break;
case "gift": GameAudioManager.Instance?.OnGiftReceived(); break;
case "correct": GameAudioManager.Instance?.OnCorrectAnswer(); break;
// etc.
```

---

## Étape 5 — Intégrer dans AIHostManager.cs

Ouvrir `AIHostManager_AudioPatch.cs` et ajouter les 2 lignes dans ta
méthode TTS :

```csharp
// AVANT de jouer la voix :
GameAudioManager.Instance?.DuckForRobot(0.3f);

// ... ton code TTS ...

// APRÈS que la voix a fini :
GameAudioManager.Instance?.RestoreFromRobot(0.6f);
```

---

## API complète — référence rapide

```csharp
// Mini-jeux
GameAudioManager.Instance.OnLobby();
GameAudioManager.Instance.OnQuizStart();
GameAudioManager.Instance.OnBattleStart();
GameAudioManager.Instance.OnSpeedChronoStart();
GameAudioManager.Instance.OnMemoryStart();
GameAudioManager.Instance.OnWordScrambleStart();
GameAudioManager.Instance.OnCrosswordStart();
GameAudioManager.Instance.OnMysteryWordStart();
GameAudioManager.Instance.OnSemanticStart();
GameAudioManager.Instance.OnImageToWordStart();
GameAudioManager.Instance.OnBlindTestRound();

// Events TikTok Live
GameAudioManager.Instance.OnCorrectAnswer();
GameAudioManager.Instance.OnWrongAnswer();
GameAudioManager.Instance.OnGiftReceived();
GameAudioManager.Instance.OnNewViewer();
GameAudioManager.Instance.OnRoundWin();
GameAudioManager.Instance.OnTimerTick();
GameAudioManager.Instance.OnTimerUrgent();

// Duck / Restore pour le robot IA
GameAudioManager.Instance.DuckForRobot(0.3f);
GameAudioManager.Instance.RestoreFromRobot(0.6f);

// Contrôle manuel
GameAudioManager.Instance.PlayBGM(monClip, fadeDuration: 2f);
GameAudioManager.Instance.StopBGM(1.0f);
GameAudioManager.Instance.PlaySFX(monSFX);
```

---

## Notes importantes

- **Blind Test et Guess Image** : pistes `Theme/playlist/` + logique playlist dans `ThemeMusicPlayer`. Pour le reste des modes, ne pas dériver la BGM depuis cette playlist.
- **Blind test — API BGM** : le dépôt expose plutôt `OnBlindTestRound()` / thème `blind_test_theme` que des `OnQuizStart()` génériques pour la playlist.
- Le **cross-fade** BGM utilise les courbes prévues dans `GameAudioManager` (dont `SmoothStep` là où c’est codé).
- Le **duck** robot (`DuckForRobot`) coordonne Theme + BGM selon l’implémentation actuelle — vérifier l’absence de double duck avec `BroadcastAudioMixCoordinator`.
- Le singleton survit aux changements de scène (`DontDestroyOnLoad`) lorsque c’est ainsi configuré sur l’objet audio racine.
