# CongoGames

CongoGames est un jeu live interactif concu pour TikTok Live, centre sur la culture generale du Congo (capitale : Brazzaville).

## Objectif

- Jouer en direct avec le chat TikTok
- Apprendre en s'amusant (quiz, mini-jeux, battles)
- Utiliser un robot IA animateur (voix + reactions)

## Langues par defaut

- Francais
- Lingala
- Kituba

## Mini-jeux inclus (architecture prete)

- Quiz
- Semantic challenge
- Word scramble
- Crossword-lite
- Blind test
- Mystery word
- Memory game
- Speed chrono
- Image-to-word guessing
- Battle mode

## Architecture

- `UnityProject/` : client 3D Unity
- `Backend/` : serveur live Node.js (WebSocket + events TikTok)
- `docs/` : documentation produit et technique

## Demarrage rapide

Guide pas à pas (`.env`, ports, Unity, dépannage) : **`docs/TESTER.md`**.

- **Unity** : dossier `Library`, script `prepare-unity`, ordre d’ouverture (ce qui se régénère, dépannage) : **`docs/UNITY_LIBRARY_AND_LAUNCH.md`**.

### 1) Backend

```bash
cd Backend
npm install
npm run dev
```

Par defaut, le serveur essaie `http://localhost:3000` et `ws://localhost:8080`.
Si un port est occupe, le backend bascule automatiquement sur le port suivant (`+1`, `+2`, etc.).

### 2) Unity

- **Même version d’éditeur** : ouvrez le projet avec la version indiquée dans `UnityProject/ProjectSettings/ProjectVersion.txt` (via **Unity Hub**). Ouvrir avec une autre 6000.x (ex. 6000.0.x au lieu de 6000.4.x) peut casser la résolution des paquets URP / `Library` et bloquer le **Play** sans erreur visible dans la Console — voir **`docs/UNITY_TROUBLESHOOTING_PLAY.md`**. Le code sous `Assets/Scripts` compile en **`Assembly-CSharp`** (défaut), ce qui référence correctement **tous** les modules Unity (UGUI, WebRequest, Video, etc.) sans `.asmdef` manuel.
- **Avant la première ouverture** (ou après avoir supprimé `UnityProject/Library`) : avec Unity **fermé**, exécuter `.\prepare-unity.ps1` ou `npm run unity:prepare` : correctif **URP** (menus Unity 6.4, voir checklist). Le projet utilise l’**Input Manager** classique (**Active Input Handling = Input Manager**) et `UnityEngine.Input` pour la démo clavier — **sans** package *Input System*, afin d’éviter les erreurs d’éditeur (`TypeLoadException` sur les UXML du paquet) et de garantir un **Play** stable. *(Option avancée : réintroduire `com.unity.inputsystem`, migrer le code + UI, puis `npm run unity:patch-input-uxml`.)* Unity peut avertir sur des packages **immutables** modifiés (URP) : attendu ; relancez `prepare-unity` après mise à jour d’URP.
- Ouvrir le projet Unity dans `UnityProject/`
- **URP** : menu **Window → CongoGames → Créer et assigner URP** (ou **Tools → CongoGames**), si vous ne voyez pas le menu **CongoGames** en barre du haut.
- Lancer **Play** : un HUD CongoGames (drapeau vert/jaune, chrono, classement, robot) est créé automatiquement si aucun `LiveEventClient` n’est déjà dans la scène.
- Voix : `AIHostManager` appelle `http://127.0.0.1:3000/tts` (même machine que `npm run dev`). Si le port HTTP n’est pas 3000, ajuste le champ **Tts Http Base** sur le composant **AI Host Manager** dans l’inspecteur (objet `CongoGames_Services` en mode Play, ou une scène perso).

## Variables d'environnement

Copier `Backend/.env.example` vers `Backend/.env` puis renseigner:

- `OPENAI_API_KEY`
- `ELEVENLABS_API_KEY` (synthèse vocale `/tts` pour Unity ; la clé ne doit pas être dans le build jeu)
- `ELEVENLABS_VOICE_ID`, `ELEVENLABS_MODEL_ID`, `ELEVENLABS_OUTPUT_FORMAT` (optionnels, voir `.env.example`)
- `TIKTOK_USERNAME`
- `TIKTOK_USERNAMES` (liste separee par virgules, ex: `congogame,je_suis_gemima`)
- `TIKTOK_RETRY_MS` (ex: `15000`)
- `PORT`, `WS_PORT`

## Deploiement cloud (Vercel)

Le dossier `Backend/` est deployable sur Vercel avec:

```bash
cd Backend
vercel --prod
```

Endpoints disponibles sur l'URL Vercel:

- `GET /health`
- `GET /metrics`
- `GET /tts/status` — indique si la synthèse ElevenLabs est prête (clé API serveur)
- `POST /tts` — corps `application/x-www-form-urlencoded` avec champ `text=` ; réponse JSON PCM (base64) pour Unity
- `POST /events/chat`
- `POST /events/gift`
- `POST /round/reset`
- `POST /question/generate`

Note: la version Vercel expose l'API HTTP stable. Le WebSocket live reste gere par le mode serveur Node local/dedie.

## WebSocket 24/7 (Railway)

Le dossier `Backend/` est deja prepare pour Railway (`Dockerfile` + `railway.json`).

### 1) Login Railway

```bash
railway login
```

### 2) Lier et deployer

```bash
cd Backend
railway link
railway up
```

### 3) Variables Railway

- `OPENAI_API_KEY`
- `ELEVENLABS_API_KEY`
- `TIKTOK_USERNAME`
- `TIKTOK_USERNAMES=congogame,je_suis_gemima`
- `TIKTOK_RETRY_MS=15000`
- `PORT=3000`
- `WS_PORT=8080`

### 4) Unity

Dans `LiveEventClient`, mettre `wsUrl` sur ton endpoint Railway:

`wss://<ton-service>.up.railway.app`

Le script tente ensuite automatiquement les fallbacks locaux (`8080/8081/8082`) si le cloud WS est indisponible.

### Endpoints cloud actifs (CongoGames)

- API HTTP (Vercel): `https://congogames-backend-cg.vercel.app`
- WS 24/7 (Railway): `wss://congogames-ws-production.up.railway.app`
- Health WS service: `https://congogames-ws-production.up.railway.app/health`

## Script de lancement unique

Depuis la racine:

```bash
npm run start-all
```

Modes:

- `npm run start-all` : mode test local (TikTok bridge desactive)
- `npm run start-all:live` : mode live TikTok (bridge actif)
- `npm run start-all:prod` : mode production backend

Ou sous PowerShell:

```powershell
.\start-all.ps1 -Mode dev
```

PowerShell:

- `.\start-all.ps1 -Mode dev` -> test local sans TikTok
- `.\start-all.ps1 -Mode live` -> connexion TikTok active
- `.\start-all.ps1 -Mode prod` -> backend start standard

## Verification production (1 commande)

Depuis la racine:

```bash
npm run smoke:live
```

Le test valide:

- API cloud (`/health` + `/question/generate`)
- WS cloud (connexion + reception d'au moins un message)
- Flux d'evenement minimal live

## Demo automatique (sans TikTok live)

Pour voir le jeu bouger tout de suite sans attendre un live TikTok:

1. Lancer le backend local:

```bash
npm run start-all
```

1. Dans un second terminal:

```bash
npm run demo:local
```

Ce script envoie 20 evenements (questions, chats, gifts, battle trigger) et anime l'UI en direct.

## Notes performance

- Object pooling pour effets/UI repetitifs
- Cache audio pour phrases robot frequentes
- Limitation + debouncing des evenements chat
- Buffer circulaire pour telemetry

## Securite

- Repo GitHub en prive recommande
- Ne jamais commiter de cles API
- Utiliser uniquement `.env` local/secret manager CI
