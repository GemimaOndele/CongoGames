# CongoGames

CongoGames est un jeu live interactif concu pour TikTok Live, centre sur la culture generale de la Republique du Congo (capitale: Brazzaville).

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

### 1) Backend

```bash
cd Backend
npm install
npm run dev
```

Par defaut, le serveur essaie `http://localhost:3000` et `ws://localhost:8080`.
Si un port est occupe, le backend bascule automatiquement sur le port suivant (`+1`, `+2`, etc.).

### 2) Unity

- Ouvrir le projet Unity dans `UnityProject/`
- Importer les scripts sous `Assets/Scripts/`
- Ajouter les composants sur les GameObjects de la scene
- Suivre `docs/UNITY_SCENE_SETUP.md` pour le branchement complet

## Variables d'environnement

Copier `Backend/.env.example` vers `Backend/.env` puis renseigner:

- `OPENAI_API_KEY`
- `ELEVENLABS_API_KEY`
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
