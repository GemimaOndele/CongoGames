# CongoGames — Générer musiques et vidéos personnalisées

**Tu as déjà les fichiers téléchargés.**  
**Maintenant, crée les tiens : musiques synthétiques animées + vidéos colorées joyeuses.**

---

## 🎵 PARTIE 1 — Générer les MUSIQUES synthétiques

Ces musiques sont **créées par algorithme** (pas téléchargées) :
- Joyeuses, dansantes, énergiques
- Adaptées à chaque mini-jeu
- Format MP3 (prêt pour Unity)

### Prérequis
```powershell
pip install pydub numpy
```

(Nécessite aussi **ffmpeg** : https://ffmpeg.org/download.html)

### Lancer la génération
```powershell
cd C:\Congogame
python generate_music_animated.py
```

### Résultat
```
✅ quiz_theme.mp3 (1.2 MB)
✅ battle_theme.mp3 (1.5 MB)
✅ speed_chrono_theme.mp3 (1.3 MB)
✅ memory_theme.mp3 (0.9 MB)
✅ word_scramble_theme.mp3 (1.1 MB)
✅ crossword_theme.mp3 (0.8 MB)
✅ mystery_word_theme.mp3 (1.4 MB)
✅ semantic_theme.mp3 (1.0 MB)
✅ image_to_word_theme.mp3 (1.2 MB)
✅ lobby_theme.mp3 (1.6 MB)  ← FESTIF CONGOLAIS
✅ blind_test_theme.mp3 (1.1 MB)
```

**Tes musiques remplacent les anciennes** dans `Assets/Resources/Audio/BGM/`

---

## 🎬 PARTIE 2 — Générer les VIDÉOS animées

Ces vidéos sont **créées par algorithme** (pas téléchargées) :
- Colorées, énergiques, dansantes
- Adaptées thématiquement à chaque mode
- Format MP4 1920x1080 @ 30 FPS (prêt pour Unity)

### Prérequis
```powershell
pip install opencv-python numpy
```

### Lancer la génération
```powershell
cd C:\Congogame
python generate_video_animated.py
```

### Résultat
```
✅ quiz/background.mp4 (12 MB)          — Lumières colorées, particules
✅ semantic/background.mp4 (8 MB)       — Réseau de connexions, nœuds
✅ word-scramble/background.mp4 (7 MB)  — Lettres dansantes, arcade
✅ crossword-lite/background.mp4 (9 MB) — Grille puzzle animée
✅ blind-test/background.mp4 (6 MB)     — Notes musicales flottantes
✅ mystery-word/background.mp4 (10 MB)  — Brouillard, révélation
✅ memory/background.mp4 (14 MB)        — Bulles douces flottantes
✅ speed-chrono/background.mp4 (15 MB)  — Particules rapides, urgence
✅ image-guess/background.mp4 (11 MB)   — Confettis festifs, couleurs
✅ show.mp4 (13 MB)                      — Lobby : festif congolais
```

**Tes vidéos remplacent les anciennes** dans `Assets/StreamingAssets/Theme/<mode>/`

---

## 📋 Procédure complète (10 minutes)

### Étape 1 — Installer les dépendances
```powershell
pip install pydub numpy opencv-python
```

(Et ffmpeg depuis https://ffmpeg.org/download.html)

### Étape 2 — Générer les musiques
```powershell
cd C:\Congogame
python generate_music_animated.py
```

Attendre 1-2 minutes. Tu veras les logs afficher chaque musique générée.

### Étape 3 — Générer les vidéos
```powershell
python generate_video_animated.py
```

Attendre 5-10 minutes. C'est plus lourd que les musiques.

### Étape 4 — Vérifier dans Unity
1. Ouvrir Unity
2. Appuyer **Ctrl+R** (refresh)
3. Appuyer **Play** (▶️)

Tu **entends les musiques synthétiques** et **vois les vidéos animées** !

### Étape 5 — Comparaison (optionnel)
- Les fichiers **teléchargés au début** : plus variés, collections professionnelles
- Les fichiers **générés maintenant** : totalement synthétiques, 100% personnalisés

Tu peux les **mélanger** si tu préfères certains sons/vidéos de chaque source.

---

## ✨ Styles musicaux par mini-jeu

| Mini-jeu | Style musical |
|---|---|
| **Quiz** | Suspense Jeopardy — notes montantes/descendantes |
| **Battle** | Combat épique — staccato carré + kick drum agressif |
| **Speed Chrono** | EDM frénétique — mélodies très rapides, urgence |
| **Memory** | Zen doux — mélodie lente, pad ambient, carillon |
| **Word Scramble** | Arcade classique — carrés (8-bit style), bloops joyeux |
| **Crossword** | Chiptune calme — notes carrées, clochettes lointaines |
| **Mystery Word** | Suspense énigmatique — glissements, mystère |
| **Semantic** | Réflexion — notes simples, pad très ambiant |
| **Image-to-Word** | Pop joyeuse — mélodies rapides, claps percussifs |
| **Lobby** | **AFROBEAT CONGOLAIS** 🎶 — rumba/samba, claps syncopés |
| **Blind Test** | Mystère musical — mélodie énigmatique, pad atmosphérique |

---

## 🎨 Styles visuels par vidéo

| Mode | Visuel |
|---|---|
| **Quiz** | Barres lumineuses colorées qui pulsent + particules jaunes |
| **Semantic** | Réseau de points/lignes bleus, connexions pulsantes |
| **Word Scramble** | Lettres de l'alphabet qui dansent, fond coloré arcade |
| **Crossword** | Grille de puzzle 8x8 animée, pulse selon la position |
| **Blind Test** | Notes musicales flottantes, ondes sonores concentriques |
| **Mystery Word** | Brouillard mouvant violet, rectangle qui grandit (révélation) |
| **Memory** | Bulles bleues/vertes qui flottent doucement, zen |
| **Speed Chrono** | Particules avec traînées vertes rapides, barre de vitesse |
| **Image Guess** | **CONFETTIS COLORÉS** 🎉 — fête totale, arc-en-ciel |
| **show.mp4** | Carrés dansants festifs, cercles pulsants, CONGO GAMES |

---

## 🔧 Personnaliser les vidéos (avancé)

Les scripts sont **facilement modifiables** :

### Changer les couleurs
```python
# Dans generate_video_animated.py, cherche :
RED = (0, 0, 255)       # BGR (pas RGB !)
GREEN = (0, 255, 0)
BLUE = (255, 0, 0)
```

### Changer la durée
```python
DURATION_SEC = 30  # Change en 60 pour 60 secondes
```

### Changer la résolution
```python
WIDTH, HEIGHT = 1920, 1080  # Change en 1280, 720 pour HD réduite
```

### Changer les styles vidéo
Édite les fonctions `generate_quiz_video()`, `generate_battle_video()`, etc.
avec tes propres patterns OpenCV.

---

## 🆘 Problèmes courants

### "pydub non installé"
```powershell
pip install pydub
```

### "ffmpeg not found"
Télécharger depuis https://ffmpeg.org/download.html  
Et ajouter au PATH Windows, ou installer via **Windows Package Manager** :
```powershell
choco install ffmpeg
```

### "opencv-python non installé"
```powershell
pip install opencv-python
```

### "Les musiques ne changent pas"
- Vérifier que Unity a bien recharger les fichiers (Ctrl+R)
- Vérifier Console Unity : `Now playing: quiz_theme`
- Les **anciens fichiers** peuvent encore être cached

### "Les vidéos ne changent pas"
- Même problème, Ctrl+R dans Unity
- Fermer Unity, relancer, puis Play

### "Les fichiers générés sont énormes"
- Les vidéos sont normales (~10-15 MB chacune en MP4)
- Si tu veux réduire : changer `DURATION_SEC = 15` au lieu de 30

---

## 📊 Résumé des fichiers créés

```
UnityProject/Assets/Resources/Audio/BGM/
├── quiz_theme.mp3
├── battle_theme.mp3
├── speed_chrono_theme.mp3
├── memory_theme.mp3
├── word_scramble_theme.mp3
├── crossword_theme.mp3
├── mystery_word_theme.mp3
├── semantic_theme.mp3
├── image_to_word_theme.mp3
├── lobby_theme.mp3              ← FESTIF & DANSANT
└── blind_test_theme.mp3

UnityProject/Assets/StreamingAssets/Theme/
├── quiz/background.mp4
├── semantic/background.mp4
├── word-scramble/background.mp4
├── crossword-lite/background.mp4
├── blind-test/background.mp4
├── mystery-word/background.mp4
├── memory/background.mp4
├── speed-chrono/background.mp4
├── image-guess/background.mp4
└── show.mp4
```

---

## 🎮 Une fois que c'est fait

- ✅ 11 musiques **synthétiques joyeuses** (une par mini-jeu)
- ✅ 10 vidéos **animées colorées** (une par mode)
- ✅ Tout **100% personnalisé** et créé par algo
- ✅ Prêt pour CongoGames

**Ouvre Unity et Play** — tu vas voir la différence ! 🚀

Lance les deux scripts maintenant :
```powershell
python generate_music_animated.py
python generate_video_animated.py
```

Dis-moi quand c'est fini ! 🎵🎬
