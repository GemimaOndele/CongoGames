# Tester CongoGames (boucle complète)

Ce guide finalise le flux **backend + WebSocket + TTS + Unity** décrit dans le README.

**Unity — cache `Library`, premier lancement, ordre des étapes** : voir le guide dédié [UNITY_LIBRARY_AND_LAUNCH.md](UNITY_LIBRARY_AND_LAUNCH.md).

## 0. Tester en local **sans** TikTok

Tu n’as **pas** besoin d’un live TikTok pour valider le jeu.

1. **Backend** : `npm run start-all` ou `.\start-all.ps1` — par défaut **`TIKTOK_BRIDGE_ENABLED=false`** (voir `package.json`) : pas de pont TikTok, seulement HTTP + WebSocket + TTS.
2. **Variables** : `Backend/.env` avec au minimum ce qui active le TTS / les questions (souvent `OPENAI_API_KEY`) ; `TIKTOK_USERNAME` peut rester vide pour ce parcours.
3. **Unity** : **Play** sur une scène vide ou la tienne ; le **RuntimeBootstrap** monte le HUD si besoin.
4. **Simulation chat / scores** : dans un second terminal, `npm run demo:local` — le script envoie des événements comme un chat de test ; tu dois voir questions, scores, TTS si le backend l’expose.

Pour un flux **avec** TikTok plus tard : `npm run start-all:live` et les identifiants documentés dans `TESTER.md` / `.env.example` (hors dépôt pour les secrets).

## Rappel : cahier des charges vs écran actuel

Le `Cahier_de_charges.md` décrit la **vision complète** (plateau 3D, robot modélisé, effets, monétisation avancée). L’écran Unity que tu vois aujourd’hui est une **phase MVP** : **interface TV lisible pour le live** (texte, chrono, classement, liaison WS/TTS), générée au runtime pour tester la boucle technique. Les assets 3D et le niveau « jeu télé » final s’ajoutent dans Unity (scènes, prefabs) sans casser ces scripts.

## 1. Variables `Backend/.env`

Copie `Backend/.env.example` vers `Backend/.env` si besoin, puis renseigne au minimum :

| Variable | Rôle |
|----------|------|
| `OPENAI_API_KEY` | **TTS local recommandé** : active `POST /tts` via OpenAI en **PCM** (`response_format: pcm`, pas de MP3 côté Unity). Sert aussi à `POST /question/generate`. |
| `ELEVENLABS_API_KEY` + `ELEVENLABS_VOICE_ID` | Optionnel : ElevenLabs si les deux sont définis (sinon repli OpenAI). Avec Unity, garde **`ELEVENLABS_OUTPUT_FORMAT=pcm_22050`** dans `.env` pour limiter le MP3 (le client envoie déjà `prefer_pcm=1`). Compte gratuit : souvent erreur 402 sur les voix bibliothèque. |
| `ELEVENLABS_OUTPUT_FORMAT` | Défaut dans `.env.example` : `pcm_22050`. Évite le chemin MP3 dans Unity (décodage fragile sous Windows). |
| `TIKTOK_USERNAME` / `TIKTOK_USERNAMES` | Pour le pont TikTok (désactivé en `npm run start-all` par défaut). |
| `PUBLIC_HTTP_BASE` | **Production** : URL HTTPS publique du même backend (ex. `https://ton-api.railway.app`). Unity utilisera cette base pour le TTS quand le message système WS la contient. **Local** : laisser vide. |

Ne commite jamais `.env` (déjà dans `.gitignore`).

## 2. Démarrer le backend

À la racine du dépôt :

```bash
npm run start-all
```

Sous Windows PowerShell :

```powershell
.\start-all.ps1
```

Le serveur affiche les ports réels si `3000` / `8080` sont occupés. Le fichier de log console indique notamment le **port HTTP** réel.

## 3. Vérifier HTTP et TTS (sans Unity)

- **Navigateur** : ouvre `http://127.0.0.1:PORT/` (même port que le log `HTTP server on…`). Tu dois voir une **page d’aide API** (plus de simple « Cannot GET / »).

- Santé + ports :

```bash
curl -s http://127.0.0.1:3000/health
```

Réponse attendue : `ok`, `httpPort`, `wsPort`, `ttsEnabled`.

Si le HTTP a basculé (ex. `3001`), utilise cette URL pour les tests curl suivants.

- Statut TTS :

```bash
curl -s http://127.0.0.1:3000/tts/status
```

`enabled: true` si `OPENAI_API_KEY` **ou** la paire ElevenLabs (`ELEVENLABS_API_KEY` + `ELEVENLABS_VOICE_ID`) est configurée. Le JSON indique aussi `openAi` et `elevenLabs`.

- **Test TTS comme Unity** (PCM demandé, même corps que le jeu) :

```bash
curl -s -X POST http://127.0.0.1:3000/tts -H "Content-Type: application/x-www-form-urlencoded" --data "text=Bonjour&prefer_pcm=1" | head -c 200
```

Réponse JSON : `ok`, `format` en principe **`pcm`**, champs `pcmBase64` / `sampleRate`. Si tu vois surtout `mp3` / `mp3Base64`, vérifie `.env` (OpenAI ou `ELEVENLABS_OUTPUT_FORMAT=pcm_22050`).

## 4. Unity (Play)

### Avertissement « Input Manager is deprecated » (jaune)

Ce message est un **simple avertissement** Unity : il recommande le package *Input System*. **Ce n’est pas une erreur de compilation** et **il ne suffit pas** à bloquer le bouton Play. Tu peux l’ignorer tant que le projet reste sur **Input Manager** (réglage actuel du dépôt).

Si tu vois en même temps **« All compiler errors have to be fixed before you can enter playmode »**, la cause est **ailleurs** (vraie erreur CS, assembly cassée, ou cache `Library` incohérent) — pas ce bandeau jaune. Voir la sous-section *Play grisé…* plus bas.

1. Ouvre `UnityProject/` dans l’éditeur.
2. **Play** sur une scène vide ou la tienne : le **RuntimeBootstrap** crée `CongoGames_Services` + HUD si aucun `LiveEventClient` n’existe déjà. Il ajoute un **AudioListener** s’il n’y en a pas dans la scène (sinon aucun son : TTS, musique d’ambiance).
3. **Découverte automatique du TTS** : `AIHostManager` scan `http://127.0.0.1:3000` … `3010` via `GET /health` et lit `httpPort`. Tu n’as plus à régler **Tts Http Base** en local sauf cas particulier. Le client Unity envoie **`prefer_pcm=1`** sur chaque `POST /tts` pour favoriser le PCM côté backend.
4. **Message WebSocket** : à la connexion, le serveur envoie un `system` avec `httpPort` et `httpApiBase` (`PUBLIC_HTTP_BASE`). Unity met à jour la base TTS (utile en **prod** avec HTTPS).
5. **Désactiver le scan** (scène perso) : décoche **Auto Discover Local Http** sur `AIHostManager` et renseigne **Tts Http Base** à la main.

### Play grisé ou message « corrigez les erreurs » (Console qui semble vide)

1. **Safe Mode** : si la barre du haut indique *SAFE MODE*, Unity n’a pas fini de compiler ou il reste des erreurs. Corrige-les, puis clique **Exit Safe Mode** quand le bouton est actif. Tant que tu es en Safe Mode, le jeu ne tourne pas normalement.
2. **Console** : menu **Window → General → Console**. En bas à droite, vérifie les compteurs **!** (erreurs). Clique sur l’icône **!** pour n’afficher que les erreurs. Désactive **Collapse** si tu ne vois qu’une ligne. Menu **⋮** de la Console : assure-toi que rien ne masque les erreurs.
3. **Fichier log** : ferme Unity, ouvre `%LOCALAPPDATA%\Unity\Editor\Editor.log` (Windows), fais **Ctrl+F** sur `error` ou `Exception` : souvent le détail y est même si la Console a été vidée.
4. **Patches projet** : avec Unity **fermé**, exécute `.\prepare-unity.ps1` (menus URP Unity 6.4), puis rouvre le projet. *(Si tu réintroduis le package **Input System** : après la 1re résolution des paquets, Unity fermé, lance `npm run unity:patch-input-uxml` pour éviter `TypeLoadException` sur `InputActionAsset`.)*
5. **« All compiler errors have to be fixed before you can enter playmode »** alors que la Console affiche **0 erreurs** : souvent un **décalage** entre l’état réel de compilation et l’UI. Fais dans l’ordre : désactive **Collapse** dans la Console ; menu **⋮** → afficher toutes les catégories ; **Assets → Reimport All** (long) ; ou **ferme Unity**, supprime **`UnityProject/Library`** **et** **`UnityProject/Temp`**, rouvre. Ouvre **`%LOCALAPPDATA%\Unity\Editor\Editor.log`**, cherche **`error CS`** : la vraie erreur y figure parfois alors que la Console est vide. Vérifie **Edit > Project Settings > Player > Active Input Handling = Input Manager** si le manifest **ne** contient pas `com.unity.inputsystem`.
6. **Scène** : une scène vide avec *Main Camera* suffit ; le **RuntimeBootstrap** recrée le HUD au **Play**. Aucune scène n’a besoin d’être dans *Build Settings* pour tester dans l’éditeur.

## 5. Simulation chat / questions

Toujours avec le backend lancé :

```bash
npm run demo:local
```

Tu dois voir dans Unity : questions, scores, éventuellement la voix (si `POST /tts` fonctionne — OpenAI ou ElevenLabs).

## 6. Limites connues (rappel)

| Sujet | Détail |
|-------|--------|
| **Scène / robot 3D** | Le bootstrap fournit une **silhouette UI** + `SimpleRobotPulse`. Un mesh Blender / Asset Store se branche sur les mêmes scripts. |
| **Vercel / serverless** | `POST /tts` peut être trop long ou coupé. En prod, garde le TTS sur **Railway / VPS** avec le même code Node. |
| **TikTok / monétisation** | Chat + cadeaux sont câblés côté protocole ; conformité produit / TikTok hors dépôt. |

## 7. Dépannage rapide

| Symptôme | Piste |
|----------|--------|
| Bip au lieu de la voix | `GET /tts/status` → `enabled: false` (ajoute `OPENAI_API_KEY`), ou **quota OpenAI** (429) : crédits / facturation sur [platform.openai.com](https://platform.openai.com). Après 429, Unity suspend les appels TTS quelques minutes pour éviter le spam. |
| Console Unity : erreur TTS / « Unable to read data » | Privilégie le **PCM** : `OPENAI_API_KEY` (TTS OpenAI en PCM) ou ElevenLabs avec `ELEVENLABS_OUTPUT_FORMAT=pcm_22050` + `prefer_pcm=1` (déjà envoyé par Unity). Le MP3 temporaire n’est plus supprimé trop tôt côté client. Si ça échoue encore, regarde la **console Node** (`[tts] …`). Vérifie les clés sans **espace** en fin de ligne dans `.env`. |
| Play impossible / « fix errors » sans message clair | Safe Mode, compteurs d’erreurs dans la Console, `Editor.log`, puis `.\prepare-unity.ps1` avec Unity fermé (voir §4, sous-section *Play grisé…*). |
| Unity ne trouve pas le HTTP | Vérifie le pare-feu ; augmente **Discover Port Max** sur `AIHostManager` ; ou désactive l’auto et mets l’URL exacte du log serveur. |
| WS OK mais pas le TTS en cloud | Définis `PUBLIC_HTTP_BASE` sur le backend déployé (HTTPS) pour que le message `system` pointe vers la bonne API HTTP. |
| **Unity : « immutable packages were unexpectedly altered »** (URP, ou Input System si tu as lancé `unity:patch-input-uxml`) | **Souvent normal** : patch volontaire dans `Library/PackageCache`. Tu peux **fermer** l’avertissement. Après mise à jour du paquet ou suppression de `Library`, **ferme Unity** et relance `.\prepare-unity.ps1` (et `unity:patch-input-uxml` seulement si tu utilises encore Input System). |
| **Editor.log : `Failed because this command failed to write the following output files`** (souvent `Library\Bee\artifacts\...\*.pdb` ou `.dll`) | Le compilateur **Bee** n’a pas pu écrire dans `Library/Bee` → la compilation des assemblies échoue et **Play** reste bloqué **sans** `error CS` clair dans la Console. **Pistes :** (1) Chemin du projet **sans caractères spéciaux** : évite un dossier du type `Congogame🇨🇬` (emoji / Unicode) ; clone ou copie le dépôt vers ex. `C:\dev\Congogames`. (2) **Antivirus** : exclusion du dossier `UnityProject\Library` (Windows Defender, etc.). (3) Ferme tout Unity, supprime `Library` **et** `Temp`, rouvre. (4) Vérifie l’espace disque et les droits d’écriture. |

---

## 8. Mini-checklist produit (local → Windows → TikTok futur → UI)

Ordre suggéré pour avancer sans tout mélanger :

1. **Validation locale** : `start-all` + Unity **Play** + `demo:local` ; parcourir les modes (**1–9**, hors champ texte) et **F10** (barre URL) ; vérifier TTS, chrono, classement, pas d’erreurs rouges dans la Console.
2. **Polish UI** : lisibilité 1080p, contrastes, réactions SFX/VFX, textes qui ne débordent pas ; noter les écrans à ajuster dans une scène dédiée si tu quittes le bootstrap seul.
3. **Build Windows** : **File > Build Settings** — ajouter au moins une **scène de démarrage** ; générer un **Standalone** ; smoke test (exe + backend sur la même machine ou URL dans `.env`).
4. **Prépa TikTok (sans live obligatoire)** : lire `TIKTOK_*` dans `Backend/.env.example` ; quand tu es prêt, tester `start-all:live` dans un environnement isolé ; conformité produit / TikTok hors dépôt.
5. **Contenu thème** : fichiers sous `StreamingAssets/Theme`, `remote_media.json` / URLs ; modes audio ou image avec médias réels pour un ressenti proche du live.
