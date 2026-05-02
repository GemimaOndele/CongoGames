# Sources de médias libres — CongoGames (référence)

Ce fichier liste **uniquement des portails et familles de licences** pour musiques, SFX et vidéos de fond (pas de scraping automatique, pas d’URL d’assets précis inventés). **Chaque fichier téléchargé garde sa propre licence** : vérifie toujours la fiche sur le site avant usage commercial.

## Musique & SFX (jeux)

| Source | Notes |
|--------|--------|
| [OpenGameArt.org](https://opengameart.org) | Jeux — filtrer CC0 / CC-BY selon besoin. |
| [Freesound.org](https://freesound.org) | Effets réalistes — **licence par son** (CC0, CC-BY, etc.). |
| [Mixkit.co — sound effects](https://mixkit.co/free-sound-effects/) | Gratuit, usage large selon [conditions Mixkit](https://mixkit.co/license/). |
| [Mixkit.co — stock video](https://mixkit.co/free-stock-video/) | Vidéos de fond / loops — vérifier la page licence Mixkit. |
| [Looperman](https://www.looperman.com/) | Boucles — **licence par loop** (auteur du loop). |
| [itch.io — game assets](https://itch.io/game-assets) | Packs — lire la licence sur chaque page produit. |

## Vidéos de fond (boucles, ambiances)

| Source | Notes |
|--------|--------|
| [Pixabay — vidéos](https://pixabay.com/videos/) | Contenu sous [licence Pixabay](https://pixabay.com/service/license-summary/) pour les médias « Pixabay ». |

## Déjà référencé dans le dépôt Unity

- `UnityProject/Assets/Resources/Audio/ATTRIBUTION.md` — crédits pour des fichiers **déjà importés** (Kenney, Wikimedia, etc.).

## Intégration projet

- **BGM / SFX Unity** : `Assets/Resources/Audio/BGM/`, `Assets/Resources/Audio/SFX/` — voir `files/README_AUDIO_INTEGRATION.md`.
- **Vidéos de thème** : `Assets/StreamingAssets/Theme/<mode>/` — noms attendus : `background.mp4`, `loop.mp4`, etc. (`ThemeModeCatalog.cs`).
- **WebSocket `metric`** : `POST /events/metric` sur le backend (`action`, `value`) → client `GameAudioManager.OnWsMetric`.

## Métriques TikTok (automatique)

Quand `TIKTOK_BRIDGE_ENABLED` est actif, le bridge (`tiktok-live-connector`) envoie aussi des messages **`type: "metric"`** vers Unity :

| Événement TikTok | `action` (approx.) | Notes |
|------------------|---------------------|--------|
| `like` | `like_burst` ou `engagement` | Seuil : `TIKTOK_METRIC_LIKE_BURST_MIN` (défaut 7 likes / batch). |
| `member` | `viewer_milestone` | Entrée spectateur (anti-spam : `TIKTOK_METRIC_MEMBER_MS`). |
| `roomUser` | `pulse` | `value` = nombre de viewers ; intervalle min : `TIKTOK_METRIC_ROOM_MS`. |
| `social` | `engagement` | Follow / partage (selon `displayType`). |
| `subscribe` | `engagement` | Abonnement chaîne. |

Variables optionnelles (ms ou entiers) : `TIKTOK_METRIC_LIKE_MS`, `TIKTOK_METRIC_MEMBER_MS`, `TIKTOK_METRIC_ROOM_MS`, `TIKTOK_METRIC_SOCIAL_MS`, `TIKTOK_METRIC_SUBSCRIBE_MS`, `TIKTOK_METRIC_LIKE_BURST_MIN`.

Les envois manuels restent possibles via **`POST /events/metric`**.
