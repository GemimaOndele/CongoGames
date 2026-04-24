# TTS local et gratuit (CongoGames)

## Déblocage rapide (≈5 minutes) — **déjà intégré**

Le backend utilise par défaut le **TTS Microsoft Edge** (service « Read Aloud »), **sans clé API** : l’audio est converti en **PCM 16 bits mono 24 kHz** côté serveur, ce qu’Unity lit correctement (évite « Unable to read data » sur certains MP3).

1. `git pull` puis à la racine : `npm run start-all` (ou `cd Backend && npm start`).
2. Vérifie : `GET http://127.0.0.1:3000/tts/status` → `enabled: true`, `edge: true`.
3. Redémarre le backend si le jeu était lancé avant le pull, puis **Play** dans Unity.

Variables optionnelles dans `Backend/.env` :

- `TTS_EDGE_ENABLED=1` (défaut) — mettre `0` pour n’utiliser **que** OpenAI / ElevenLabs.
- `TTS_EDGE_VOICE=fr-FR-DeniseNeural` — autre voix [liste des noms de voix Edge](https://github.com/ericc-ch/edge-tts#readme).

**Conditions** : le serveur doit pouvoir joindre le service public Microsoft (réseau sortant). En prod, prévois un fallback (audio pré-générés, autre moteur) si l’hébergeur bloque ce trafic.

---

## Niveau intermédiaire — **Coqui TTS + script HTTP**

1. **Python 3.10+**, GPU optionnel.  
2. `pip install TTS` (projet [Coqui TTS](https://github.com/coqui-ai/TTS)) — l’install peut être lourde (PyTorch, etc.).

Exemple de micro-serveur (à lancer en local, ex. port **3020**) qui imite le contrat de CongoGames : `POST /tts` avec `text=...&prefer_pcm=1` et JSON `{ ok, format:"pcm", sampleRate, pcmBase64, ... }`. Tu peux t’inspirer de `Backend/src/services/edgeTtsService.js` pour le format PCM, et remplacer la synthèse par `TTS` Coqui, puis enregistrer en WAV, lire le PCM 16 bit et renvoyer en base64.

3. **Unity** : sur l’`AIHostManager`, fixe **Tts Http Base** sur `http://127.0.0.1:3020` **ou** laisse le scan automatique 3000–3010 (ajoute ton port côté code si 3020 n’est pas scanné).

4. Avantage : entièrement **offline** après installation ; coût : complexité d’**ops** (modèles, GPU, latence).

---

## Ultime simplicité Windows — `System.Speech` (hors scope Unity actuel)

Sur **Windows**, `System.Speech.Synthesis` peut parler sans API ; c’est propre à l’**éditeur / build Windows** et ne s’applique pas facilement à un build cross‑platform. Utile pour un outil d’**authoring** (pré-générer des `.wav`), pas pour remplacer le pipeline HTTP actuel partout.

---

## Pré-générer des phrases (Zéro latence en jeu)

Enregistre des `.wav` / `.ogg` pour les annonces récurrentes ; le jeu a déjà un chemin **Oza zoba** et des **SFX** — pour le live TikTok, c’est souvent le plus fiable (pas de coupure, pas de quota).
