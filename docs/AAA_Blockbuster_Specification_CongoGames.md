# Spécification type « blockbuster AAA » — CongoGames (live TikTok + expérience console)

Document de référence : vision production grande échelle, calibrée sur un **jeu culturel interactif** (République du Congo), **diffusion live** (TikTok / streaming), avec **présentation 3D** digne d’un titre console — tout en gardant des contraintes réalistes (budget, délais, équipe réduite possible en phase 1).

**Version :** 1.0  
**Public :** direction créative, prod, technique, audio, art, marketing live.

---

## Table des matières

1. [Résumé exécutif](#1-résumé-exécutif)
2. [Piliers créatifs et promesse joueur](#2-piliers-créatifs-et-promesse-joueur)
3. [Périmètre « AAA » : ce que ça implique](#3-périmètre-aaa--ce-que-ça-implique)
4. [Organisation type grande production](#4-organisation-type-grande-production)
5. [Phases et jalons (années 0 → 3+)](#5-phases-et-jalons-années-0--3)
6. [Pipeline art & cinématiques](#6-pipeline-art--cinématiques)
7. [Pipeline audio & musique](#7-pipeline-audio--musique)
8. [Pipeline technique & moteur (Unity 6)](#8-pipeline-technique--moteur-unity-6)
9. [Design jeu & méta live (TikTok)](#9-design-jeu--méta-live-tiktok)
10. [Qualité, tests, certification](#10-qualité-tests-certification)
11. [Annexe A — Profil URP « broadcast » & ambiance 3D](#annexe-a--profil-urp-broadcast--ambiance-3d)
12. [Annexe B — Mix audio live (bus, ducking, loudness)](#annexe-b--mix-audio-live-bus-ducking-loudness)
13. [Annexe C — Livrables documentaires (liste type studio)](#annexe-c--livrables-documentaires-liste-type-studio)

---

## 1. Résumé exécutif

**Produit :** CongoGames — plateforme de mini-jeux et quiz (culture congolaise : histoire, musique, langues, personnalités, géographie), animée par un **hôte 3D / IA** (TTS, réactions), pensée pour **spectacle live** (chat, scores, effets, cadeaux éventuels).

**Ambition visuelle :** qualité **console / haut de gamme PC** sur un **plateau 3D lisible en stream** (pas nécessairement un monde ouvert géant : la clé est la **lisibilité**, le **rythme**, le **lighting**, les **cinématiques courtes**).

**Durée type « blockbuster » :** 24 à 48 mois jusqu’à un produit très poli, avec équipe complète ; un **MVP streamable** peut exister en 3–9 mois avec une équipe réduite (voir phase 0).

---

## 2. Piliers créatifs et promesse joueur

| Pilier | Description | Critères de succès mesurables |
|--------|-------------|-------------------------------|
| **Culture** | Contenu vérifié, respectueux, varié (quiz + mini-jeux) | Banque de questions versionnée, sources, modération |
| **Spectacle live** | Lisibilité 1080p/720p, timing 5–15 s, feedback immédiat | Rétention live, taux de participation chat |
| **Présence hôte** | Personnage 3D crédible, voix, lip-sync, émotions | NPS créateurs, cohérence audio/vidéo |
| **Fierté locale** | Congo (officiel), Brazzaville, drapeau, langues | Adoption audience diaspora + locale |
| **Technique** | 60 FPS stabilité stream, latence maîtrisée | Crash-free sessions, CPU/GPU budget |

---

## 3. Périmètre « AAA » : ce que ça implique

### 3.1 Contenus

- **Campagne / modes** : quiz classique, modes speed, blind test, mots croisés légers, devinettes image, etc. (aligné cahier des charges existant).
- **Cinématiques** : séquences courtes (intro, transitions de manche, victoires) — storyboard, préviz, motion capture optionnelle, facial pour hôte.
- **Localisation** : français, lingala/kituba (selon priorité), sous-titres live-safe.

### 3.2 Production d’assets

- **Modèles** : hôte, accessoires plateau, décor studio « culture + moderne ».
- **Textures** : PBR, trims, atlasing pour perf stream.
- **Animation** : idle, emphase, succès/échec, transitions.
- **VFX** : confettis, flashes réponses, transitions non agressives pour compression vidéo.

### 3.3 Ingénierie

- **Render pipeline** : URP recommandé (large compatibilité + itération rapide) ; HDRP si cible quasi exclusivement PC haut de gamme.
- **Éclairage** : baked où possible, temps réel maîtrisé pour personnage vedette.
- **Build** : profils Dev / Staging / Live ; journalisation erreurs ; télémetrie optionnelle.

---

## 4. Organisation type grande production

### 4.1 Équipe cible (≈ 80–120 FTE au pic, sur 2–3 ans)

| Domaine | Rôles indicatifs |
|---------|------------------|
| **Direction** | Creative Director, Game Director, EP |
| **Design** | Lead GD, mode designers, économie live |
| **Art** | Lead artist, concept, 3D enviro, 3D chars, tech art, VFX, UI motion |
| **Animation** | Lead anim, cinematics, rigging |
| **Audio** | Lead audio, sound design, music supervisor, intégration Wwise/FMOD ou Unity |
| **Narration / écriture** | Writers, consultant culturel, relecture |
| **Tech** | Lead programmer, gameplay, UI, réseau/live, outils, build |
| **QA** | Lead QA, embedded QA, perf specialist |
| **Marketing / com** | Community, partenariats créateurs |
| **Prod** | PM, coordinateurs, externalisation |

*(Une petite équipe 5–15 personnes peut livrer une verticale « stream MVP » en réduisant cinématiques et scope art.)*

### 4.2 Gouvernance

- **Greenlight** : prototype fun + perf + premier pack contenu.
- **Vertical slice** : une manche complète « comme en live », qualité finale cible.
- **Alpha / Beta** : stabilité, contenu, équilibrage, accessibilité.

---

## 5. Phases et jalons (années 0 → 3+)

| Phase | Durée indicative | Jalons |
|-------|------------------|--------|
| **0 — Proto stream** | 1–3 mois | Loop quiz + HUD + capture OBS + audio lisible |
| **1 — Vertical slice** | 3–6 mois | Hôte 3D + TTS + 2–3 modes + lighting pro |
| **2 — Production** | 12–24 mois | Contenu massif, cinématiques, outils modération |
| **3 — Polish & live ops** | 6–12 mois | Saisons, événements, optimisations, plateformes |

---

## 6. Pipeline art & cinématiques

1. **Concept** → validation culturelle + lisibilité stream.  
2. **Blocout 3D** → caméras type « plateau TV ».  
3. **Production haute rés** → LODs 0–2 minimum pour héros.  
4. **Materials URP/HDRP** → tests sur cible stream (bitrate).  
5. **Lighting pass** → key/fill/rim ; éviter micro-contraste illisible en H.264.  
6. **Cine** : storyboard → animatic → layout → rendu temps réel ou offline selon besoin.  
7. **Intégration** : Addressables, versioning, hot fix contenu si besoin.

**Livrables :** bible art, palette, specs personnage, nomenclature assets, guidelines VFX « stream-safe ».

---

## 7. Pipeline audio & musique

- **Design sonore** : UI, feedback quiz, ambiances courtes, stingers.  
- **Musique** : stems (drums / bass / melody) pour ducking sous voix.  
- **Voix** : TTS + option voix enregistrée pour phrases clés ; loudness cohérente.  
- **Mix** : bus Master → Music / SFX / Voice / UI ; side-chain ou attenuation scriptée sous parole.  
- **Cible loudness** : viser niveaux stables pour éviter clipping encodage stream (voir annexe B).

---

## 8. Pipeline technique & moteur (Unity 6)

- **Version cible** : Unity 6.4.x (ex. 6000.4.x), Input System, URP.  
- **Perf** : budget CPU/GPU pour capture logicielle ; VSync / frame pacing selon setup OBS.  
- **CI** : build automatique, tests smoke, profilage régulier.  
- **Sécurité / backend** : API questions, quotas TTS, pas de secrets client.

---

## 9. Design jeu & méta live (TikTok)

- Boucles courtes, récompenses visibles (pseudo, classement).  
- Mécaniques donation / likes **uniquement si conformes** plateforme et légalité locale.  
- Outils modération : filtres, blocklist, rate limit.

---

## 10. Qualité, tests, certification

- Matrice plateformes (PC prioritaire pour live).  
- Tests réseau, stress chat simulé, fuites mémoire audio.  
- Accessibilité : contrastes UI, taille texte, pas de dépendance couleur seule.

---

## Annexe A — Profil URP « broadcast » & ambiance 3D

**Objectif :** image **nette**, **stable**, **peu bruitée** après encodage stream ; personnage **isolé** du fond.

### A.1 Choix pipeline

- Installer **Universal RP** via le **Render Pipeline Converter** / assistant Unity (recommandé plutôt qu’édition manuelle YAML).  
- **PC live** : URP **High** ou équivalent ; activer **HDR** si écran et workflow le permettent ; sinon LDR soigné.

### A.2 Caméra & post-processing (indicatif)

- **Anti-aliasing** : SMA ou TAA (éviter sureté excessive qui « bouge » en stream).  
- **Bloom** : faible ; éviter halos sur texte UI.  
- **Color adjustments** : contraste modéré ; saturation maîtrisée (compression).  
- **Depth of field** : léger sur plans portrait hôte si besoin ; pas de DOF agressif sur HUD.

### A.3 Éclairage plateau

- **Key** principale (soft), **fill** pour lifted shadows, **rim** pour séparation fond.  
- **Lightmaps** pour décor statique ; personnage en temps réel.  
- Tester capture **OBS** (x264/NVENC) à 6000–8000 kbps pour valider le look.

### A.4 Ambiance 3D « première passe »

1. Sol + cyclorama ou écran LED stylisé (géométrie simple).  
2. Props 2–3 max (logo, carte stylisée, instruments référence culture).  
3. **Sky / backdrop** : gradient ou texture peu contrastée.  
4. **Une caméra hero** + **une caméra wide** pour transitions.

### A.5 Intégration projet CongoGames

- Le dépôt inclut un composant **`BroadcastPresentationBootstrap`** (runtime) : cadence d’images et options utiles capture ; à placer sur un `GameObject` persistant de bootstrap.  
- **URP** : package `com.unity.render-pipelines.universal` dans `Packages/manifest.json` ; menu **CongoGames > Rendering > Créer et assigner URP (multi-plateforme)** pour générer `Assets/Settings/URP/`.  
- Checklist détaillée (matériaux, WebGL, mobile) : **`docs/URP_Migration_Checklist_CrossPlatform.md`**.

---

## Annexe B — Mix audio live (bus, ducking, loudness)

### B.1 Structure de bus recommandée

```
Master
├── Bus_Music
├── Bus_Sfx
├── Bus_Voice_Host
└── Bus_UI
```

- **Exposer** des paramètres (ex. `MusicAttenuation`, `SfxAttenuation`) pour automation sous TTS.  
- Dans l’éditeur : créer un **Audio Mixer** ; assigner les `AudioMixerGroup` aux `AudioSource` (musique, SFX, voix).

### B.2 Ducking sous la voix

- Lorsque l’hôte parle : baisser **Music** (−6 à −12 dB relatif) et légèrement **SFX** si nécessaire.  
- Le script **`BroadcastAudioMixCoordinator`** (composant sur un `GameObject` actif) s’abonne à **`AIHostManager.OnSpeakingChanged`** et applique un ducking par **multiplicateur de volume** sur **`ThemeMusicPlayer`** et **`GameSfxHub`**. Les champs **`musicOutputGroup` / `sfxOutputGroup`** sur ces lecteurs restent optionnels pour router vers un **Audio Mixer** créé dans l’éditeur.

### B.3 Loudness stream

- Éviter saturation sur le Master ; laisser **1–2 dB de marge** avant 0 dBFS.  
- Tester avec encodeur type OBS ; ajuster **limiteur** léger si besoin.

---

## Annexe C — Livrables documentaires (liste type studio)

- GDD (game design document) + fiches par mode.  
- TDD (technical design) : réseau, live, TTS, build.  
- Art bible + style guide UI.  
- Audio bible + feuille de route musique / VO.  
- Plan de tests + critères d’acceptation.  
- Roadmap live ops + calendrier saisonnier.

---

## Synthèse

Ce document décrit une **spécification de niveau blockbuster** (organisation, pipelines, jalons) **adaptée** à CongoGames : la « profondeur AAA » réside surtout dans la **cohérence culturelle**, la **qualité du spectacle live**, le **personnage hôte**, le **son**, et la **stabilité technique** — pas uniquement dans la taille du monde 3D. La suite naturelle en production est : **vertical slice URP + plateau 3D + mix audio** validés en capture réelle OBS/TikTok.
