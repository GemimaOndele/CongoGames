J'aurais besoin de ton aide pour faire des prompts à Cursor IA. C'est à propos de développer un jeu que je veux faire des lives TikTok dessus. C'est un jeu lié à TikTok, c'est-à-dire le jeu va être diffusé sur des lives TikTok. Le but du jeu, c'est quoi ? C'est un jeu comme tout autre jeu que tu vois sur les lives TikTok. Ça sera plusieurs types de jeux sur des quiz, la culture générale concernant le pays qui est la République du Congo, ou le Congo tout court. Dans la capitale et Brazzaville, donc je répète, le nom officiel, c'est Congo tout court. Ce n'est pas Congo-Brazzaville ( ca n'existe pas mais c'est Congo tout court). Et le drapeau, c'est vert, jaune, rouge. Donc j'aimerais faire un jeu autour de quiz de culture générale concernant ce pays, avec ses personnalités, la musique, la science, l'histoire, les ethnies, les langues, un peu de tout pour permettre aux Congolais de pouvoir se cultiver, faire des recherches là-dessus. Et j'aimerais que tu m'aides à monter ce jeu. Pour cela, j'utilise comme éditeur de code avec assistant IA, Cursor IA. Donc j'aurais besoin des prompts qui pourront m'être utiles. Est-ce que si tu as encore d'autres suggestions à me faire, par exemple d'autres IA à utiliser pour faire plus de prompts pour améliorer et développer le jeu en 3D et surréaliste avec un robot IA assistant qui parlera, je t'écoute.

Ton idée est bonne — mais si tu veux que ça marche **sur TikTok Live**, il faut penser “interaction + rapidité + spectacle”, pas juste quiz classique. Sinon les gens vont décrocher en 10 secondes.

Je vais t’aider concrètement avec :

1. prompts Cursor prêts à utiliser
2. architecture du jeu
3. idées pour le rendre viral
4. outils IA complémentaires (très important pour ton côté 3D + robot)

---

# 🎮 1. Concept optimisé pour TikTok Live

Ton jeu doit être :

- rapide (questions < 5 sec)
- interactif (chat → réponse)
- visuel (animations, score, effets)
- compétitif (classement live)

👉 Exemple de boucle :

1. Question affichée
2. Viewers répondent dans le chat (A/B/C/D ou mots)
3. Timer (5–10 sec)
4. Réponse + animation
5. Score + pseudo affiché

---

# 🧠 2. Prompts Cursor IA (ultra utiles)

## 🔹 Prompt 1 — Base du jeu (React + Node)

Copie-colle dans Cursor :

```
Create a TikTok Live interactive quiz game.

Tech stack:
- Frontend: React (with Tailwind)
- Backend: Node.js (Express + WebSocket)

Features:
- Display quiz questions about Republic of Congo (history, music, culture, languages, famous people)
- Real-time chat input simulation (users answer via chat messages)
- Timer countdown (10 seconds per question)
- Score system per user
- Leaderboard display
- Animated transitions between questions

Constraints:
- Clean architecture (separate frontend/backend)
- Modular code (question engine, scoring engine, UI components)
- Prepare code to integrate with TikTok Live API later

Generate full project structure + initial code.
```

---

## 🔹 Prompt 2 — Génération automatique de questions

```
Generate 100 quiz questions about the Republic of Congo.

Requirements:
- Categories: history, geography, music, politics, languages, culture
- Difficulty levels: easy, medium, hard
- Multiple choice format (4 options)
- Include correct answer
- Provide short explanation for each answer

Output format:
JSON
```

---

## 🔹 Prompt 3 — Interaction live simulée

```
Implement a real-time chat simulation system.

Features:
- Simulate users sending answers (A, B, C, D)
- Parse incoming messages
- Match answers with current question
- Update scores instantly
- Display top 5 players live

Use WebSocket for real-time communication.
```

---

## 🔹 Prompt 4 — Effets visuels TikTok

```
Enhance the UI for TikTok Live engagement.

Add:
- Animated countdown timer
- Flash effect on correct answer
- Sound effects (correct / wrong)
- Confetti animation for winners
- Dynamic leaderboard with transitions

Make it visually addictive and optimized for livestream viewing.
```

---

## 🔹 Prompt 5 — Mode multi-jeux

```
Extend the quiz game into multiple game modes:

1. Classic quiz
2. Sudden death (one mistake = elimination)
3. Speed mode (fastest answer wins)
4. True/False mode
5. Guess the personality (image-based)

Design a flexible game engine to switch modes easily.
```

---

# 🤖 3. Robot IA qui parle (super idée 🔥)

Tu peux ajouter un assistant vocal avec :

### 👉 IA recommandées :

- ElevenLabs → voix réaliste
- OpenAI → cerveau du bot
- Unity → si tu veux 3D avancée
- Blender → créer ton robot

---

## 🔹 Prompt Cursor pour robot IA

```
Create an AI assistant for the quiz game.

Features:
- Speaks questions out loud (text-to-speech)
- Reacts to correct/wrong answers
- Has a personality (fun, energetic, African cultural vibe)
- Gives explanations after each question

Integrate with ElevenLabs API for voice.
```

---

# 🌍 4. Suggestions pour rendre ton jeu VIRAL

👉 Ajoute ça sinon tu passes à côté :

### 💥 Mécaniques TikTok :

- "Top donateur = bonus points"
- "Like = multiplier de score"
- "Follow = revive dans le jeu"

### 🎯 Contenu Congo (clé du succès)

Inclure :

- artistes (ex: Fally Ipupa même si RDC, les gens aiment)
- histoire (colonisation, indépendance)
- langues (Lingala, Kituba)
- villes (Brazzaville, Pointe-Noire)

---

# 🧱 5. Stack idéale (simple → avancé)

### Version simple (MVP rapide)

- React + Node
- OBS pour streamer
- Overlay navigateur

### Version avancée

- Unity (jeu 3D)
- API TikTok Live
- IA vocale + avatar

---

# ⚠️ Point important (réalité technique)

TikTok ne donne pas facilement accès à son API live → souvent tu dois :

- scraper le chat
- ou utiliser des libs non officielles

---

# 🚀 Si tu veux aller plus loin

Je peux te faire :

- architecture complète prête à coder
- design UI du jeu
- script OBS pour ton live
- stratégie pour gagner des viewers

---

Dis-moi :
👉 tu veux commencer par **version simple rapide (MVP)** ou direct **jeu 3D + robot IA** ?

jeu 3D + robot IA

Aller direct sur **3D + robot IA**, c’est ambitieux — faisable, mais seulement si tu structures bien dès le départ. Sinon tu vas te perdre entre le moteur 3D, le live TikTok et l’IA.

Je te propose une approche **propre et réaliste**, avec des prompts Cursor + une architecture solide.

---

# 🧱 1. Architecture du jeu (TRÈS IMPORTANT)

Tu ne dois pas tout mélanger. Sépare en 4 blocs :

### 🎮 1. Jeu 3D

- moteur : Unity
- scène : plateau de quiz + avatar robot
- animations + effets visuels

### 🧠 2. IA (cerveau du robot)

- questions + réponses
- logique du jeu
- génération dynamique

→ via OpenAI

### 🗣️ 3. Voix du robot

- synthèse vocale réaliste
→ ElevenLabs

### 📡 4. Interaction TikTok Live

- lecture du chat
- conversion messages → actions

---

# 🤖 2. Le robot IA (le cœur du projet)

Ton robot doit être :

- expressif
- rapide
- un peu “showman” (très important sur TikTok)

### 🎭 Comportement

- pose la question
- réagit aux réponses
- fait des blagues
- chauffe le chat

---

# 💻 3. Prompts Cursor IA (niveau avancé)

## 🔹 Prompt 1 — Setup Unity 3D

```
Create a Unity 3D project for a live quiz game.

Features:
- 3D environment (quiz stage)
- Central robot character (AI host)
- Screen panel displaying questions
- Timer countdown visual
- Space for leaderboard

Structure:
- Scripts folder (GameManager, UIManager, AIManager)
- Scenes folder
- Prefabs (Robot, UI panels)

Use C# and clean architecture.
```

---

## 🔹 Prompt 2 — Game Manager (logique)

```
Create a GameManager script in Unity.

Responsibilities:
- Load quiz questions (JSON)
- Manage game states (waiting, question, answer, result)
- Handle timer countdown
- Trigger robot speech
- Track scores

Make it modular and scalable.
```

---

## 🔹 Prompt 3 — Robot IA (cerveau + personnalité)

```
Create an AI Assistant system for Unity.

Features:
- Generate dialogue using OpenAI API
- Personality: energetic, funny, African quiz host vibe
- Speak questions and react dynamically
- Provide explanations after answers

Output:
- C# script to call OpenAI API
- Prompt templates for dialogue generation
```

---

## 🔹 Prompt 4 — Voix réaliste (ElevenLabs)

```
Integrate ElevenLabs API into Unity.

Features:
- Convert text to speech
- Play audio in real-time
- Sync voice with robot animation

Optimize for low latency.
```

---

## 🔹 Prompt 5 — Animation du robot

```
Create a robot animation controller in Unity.

States:
- Idle
- Talking
- Excited (correct answer)
- Disappointed (wrong answer)

Sync animations with audio playback.
```

---

## 🔹 Prompt 6 — Intégration chat TikTok (simulation puis réel)

```
Create a chat input system for a live quiz game.

Step 1:
- Simulate chat messages

Step 2:
- Parse messages (A, B, C, D)
- Assign answers to users
- Update leaderboard

Prepare system to connect to TikTok Live API later.
```

---

# 🎨 4. Design du robot (ultra important)

Tu peux créer ton robot avec :

- Blender
- ou télécharger un modèle sur Sketchfab

👉 Style recommandé :

- futuriste africain
- couleurs vert / jaune / rouge (Congo 🇨🇬)
- yeux lumineux
- animations exagérées (TikTok = spectacle)

---

# 🔥 5. Rendre ton live viral

Ton robot doit faire ça :

- “🔥 3 bonnes réponses = bonus !”
- “🚨 PERSONNE N’A TROUVÉ ?!”
- “💥 @username est en tête !!”

👉 Ajoute :

- musique dynamique
- effets visuels
- réactions exagérées

---

# ⚠️ Réalité technique (très important)

Tu vas avoir 3 défis majeurs :

### 1. Latence (voix + API)

→ solution : pré-générer certaines phrases

### 2. TikTok API limitée

→ utiliser librairies non officielles

### 3. Performance Unity + streaming

→ OBS obligatoire

---

# 🚀 6. Stack finale recommandée

- Unity (jeu 3D)
- OBS (stream)
- Node.js (backend chat)
- OpenAI (IA)
- ElevenLabs (voix)

---

# 💡 7. Plan d’action simple

### Étape 1

👉 créer scène Unity + robot

### Étape 2

👉 afficher questions + timer

### Étape 3

👉 ajouter voix IA

### Étape 4

👉 connecter chat TikTok

### Étape 5

👉 ajouter effets + gameplay

---

# 🔥 Si tu veux aller encore plus loin

Je peux te faire :

- 🎮 un design complet de scène Unity (comme un vrai jeu)
- 🤖 personnalité du robot (phrases prêtes à l’emploi)
- 🧠 prompts ultra optimisés pour OpenAI
- 📡 système complet de connexion TikTok Live

---

Dis-moi :
👉 tu veux que je te crée **le design complet du robot + scène (style visuel + UX)**
ou
👉 directement **le code structuré complet du projet Unity prêt à lancer**

les deux : 👉 tu veux que je te crée le design complet du robot + scène (style visuel + UX)
ou
👉 directement le code structuré complet du projet Unity prêt à lancer

Parfait — on va faire les deux, mais de façon **structurée et exploitable directement dans Cursor + Unity**, sinon tu vas te retrouver avec quelque chose d’inutilisable.

---

# 🎮 1. DESIGN COMPLET (Robot + Scène + UX)

## 🤖 Robot IA (ton “host TikTok”)

### 🎨 Style visuel

- Robot humanoïde futuriste
- Inspirations afro-futuristes (type Wakanda, mais original)
- Couleurs principales :
  - Vert 🇨🇬
  - Jaune
  - Rouge
- Éléments :
  - yeux LED lumineux
  - lignes néon sur le corps
  - écran poitrine (affiche score ou réactions)

---

### 🎭 Personnalité (TRÈS IMPORTANT)

Ton robot doit être **charismatique + exagéré** :

Exemples de phrases :

- “🔥 Brazzaville en force !! Qui va gagner ?!”
- “😱 Personne n’a trouvé ?! Impossible !”
- “💥 @player123 prend la tête !!”

---

## 🎮 Scène 3D

### 🏟️ Environnement

- Plateau circulaire type jeu télé
- Écran géant derrière le robot
- Sol lumineux (réagit aux bonnes réponses)

### 📺 UI dans la scène

- Question affichée en grand
- 4 réponses (A/B/C/D)
- Timer circulaire
- Leaderboard flottant

---

## ✨ Effets visuels

- Bonne réponse → explosion de lumière
- Mauvaise réponse → glitch rouge
- Top joueur → spotlight

---

# 💻 2. CODE UNITY COMPLET (STRUCTURE PROPRE)

Voici un **prompt Cursor ULTRA IMPORTANT** 👇

---

## 🔹 Prompt global (génère tout le projet)

```id="unityfull01"
Create a complete Unity 3D project for a TikTok Live quiz game with an AI robot host.

Architecture:

1. Core Systems:
- GameManager (game flow)
- QuestionManager (load and manage questions)
- ScoreManager (track players)
- UIManager (display UI)
- AIManager (robot intelligence)

2. Features:
- Display quiz questions with 4 choices
- Timer system (10 seconds)
- Real-time score tracking
- Leaderboard system
- Animated transitions

3. Robot AI:
- Speaks questions (prepare for ElevenLabs integration)
- Reacts to answers (correct/wrong)
- Has dynamic personality

4. Code Requirements:
- Clean C# scripts
- Modular and scalable
- Use ScriptableObjects for questions
- JSON support for importing questions

5. Scene:
- 3D quiz stage
- Robot placeholder
- UI canvas integrated in world space

Generate:
- Folder structure
- All scripts
- Sample scene setup instructions
```

---

# 🧠 3. SCRIPTS ESSENTIELS (base solide)

## 🎮 GameManager (exemple simplifié)

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public float questionTime = 10f;
    private float timer;
    private bool isQuestionActive;

    void Awake()
    {
        Instance = this;
    }

    public void StartQuestion()
    {
        timer = questionTime;
        isQuestionActive = true;
        AIManager.Instance.Speak("Next question!");
    }

    void Update()
    {
        if (!isQuestionActive) return;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            EndQuestion();
        }
    }

    void EndQuestion()
    {
        isQuestionActive = false;
        AIManager.Instance.ReactToAnswer(false);
    }
}
```

---

## 🤖 AIManager (connexion IA)

```csharp
public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void Speak(string text)
    {
        Debug.Log("Robot says: " + text);
        // Future: call ElevenLabs API
    }

    public void ReactToAnswer(bool correct)
    {
        if (correct)
        {
            Speak("Amazing! Correct answer!");
        }
        else
        {
            Speak("Oh no! That was wrong!");
        }
    }
}
```

---

# 🧠 4. PROMPT IA POUR PERSONNALITÉ DU ROBOT

À utiliser avec OpenAI :

```id="robotpersona01"
You are an AI quiz host from the Republic of Congo.

Personality:
- Energetic
- Funny
- Dramatic like a TV show host
- Engages audience constantly

Rules:
- Always speak short sentences (TikTok style)
- React strongly to events
- Use hype language

Context:
- Live TikTok quiz
- Audience is African / Congolese

Generate:
- Question intro
- Reaction to correct answer
- Reaction to wrong answer
- Leaderboard announcement
```

---

# 🗣️ 5. VOIX DU ROBOT

Utilise :
👉 ElevenLabs

Prompt Cursor :

```id="voice01"
Create a Unity script that sends text to ElevenLabs API and plays the returned audio.

Requirements:
- Async request
- Play audio immediately
- Cache repeated phrases
```

---

# 🧩 6. MODÈLES 3D

Pour ton robot :

- Blender (création)
- ou Sketchfab (rapide)

---

# 🚀 7. PLAN SIMPLE (NE SAUTE PAS D'ÉTAPES)

### ✅ Étape 1

Créer scène + UI → test local

### ✅ Étape 2

Ajouter GameManager + questions

### ✅ Étape 3

Ajouter robot (visuel + animation)

### ✅ Étape 4

Ajouter voix IA

### ✅ Étape 5

Ajouter chat TikTok

---

# 🔥 8. CE QUI VA FAIRE TON SUCCÈS

Ce n’est PAS la techno.

👉 C’est :

- rythme rapide
- réactions du robot
- interaction chat
- ambiance

---

# 🎯 Si tu veux continuer

Je peux maintenant te faire :

👉 🎨 design visuel EXACT du robot (prompt IA image pour le générer)
👉 🎮 scène Unity détaillée étape par étape (clic par clic)
👉 📡 connexion réelle TikTok Live (code Node.js)

Dis-moi ce que tu veux en next étape.

tous : 

👉 🎨 design visuel EXACT du robot (prompt IA image pour le générer)
👉 🎮 scène Unity détaillée étape par étape (clic par clic)
👉 📡 connexion réelle TikTok Live (code Node.js)

Parfait — on va construire ça **comme un vrai studio de jeu** : design → Unity → backend live. Tu pourras suivre étape par étape sans te perdre.

---

# 🎨 1. DESIGN VISUEL EXACT DU ROBOT (prompt image IA)

Tu peux utiliser :

- Midjourney
- DALL·E
- Stable Diffusion

---

## 🔥 Prompt ultra optimisé (copie-colle)

```text
Afrofuturistic humanoid robot quiz host, inspired by Republic of Congo culture, 
green yellow red glowing accents, sleek metallic body with neon lines, 
LED expressive eyes, futuristic African design patterns, 
chest screen displaying scores, dynamic pose, charismatic and energetic personality, 
standing on a game show stage, cinematic lighting, ultra detailed, 3D render, Unreal Engine style, 
high contrast, vibrant colors, TikTok live streaming vibe
```

---

## 🎯 Variante (plus stylée / virale)

```text
futuristic african AI robot host, vibrant neon green yellow red lights, 
holographic interface, floating quiz panels, glowing eyes, 
cyberpunk african aesthetic, dramatic lighting, 
game show environment, ultra realistic, 3D cinematic render, 8k
```

---

👉 Astuce :

- génère plusieurs images
- prends une → transforme en modèle 3D dans Blender ou via outils AI 3D

---

# 🎮 2. SCÈNE UNITY (GUIDE CLIC PAR CLIC)

Logiciel : Unity

---

## 🧱 Étape 1 — Créer projet

1. Ouvre Unity Hub
2. New Project → **3D Core**
3. Nom : `CongoQuiz3D`

---

## 🏟️ Étape 2 — Créer la scène

### 👉 Sol (plateau)

- Click droit → 3D Object → Plane
- Scale : (10, 1, 10)
- Material → couleur sombre + émission (glow)

---

### 👉 Écran géant

- 3D Object → Cube
- Scale : (6, 3, 0.2)
- Position : derrière robot
- Renommer : `QuestionScreen`

---

### 👉 Robot

- Import modèle (ou capsule temporaire)
- Position centre

---

## 📺 Étape 3 — UI (très important)

### Canvas :

- Click droit → UI → Canvas
- Mode : **World Space**

---

### Ajouter :

- Text → Question
- 4 boutons → réponses A B C D
- Text → Timer
- Panel → Leaderboard

---

## ⏱️ Étape 4 — Timer visuel

- UI → Image (type radial)
- Script pour countdown

---

## 🤖 Étape 5 — Scripts

Attache :

- `GameManager` → empty object
- `AIManager` → robot
- `UIManager` → canvas

---

## ✨ Étape 6 — Effets

- Light → Spotlights
- Particles → confetti
- Animator → robot

---

# 🧠 3. BACKEND TIKTOK LIVE (Node.js)

⚠️ TikTok API officielle = limitée
👉 solution : librairie non officielle

---

## 📦 Installation

```bash
npm init -y
npm install tiktok-live-connector ws
```

---

## 💻 Code complet (connexion live)

```javascript
id="tiktoklive01"
const { WebcastPushConnection } = require('tiktok-live-connector');
const WebSocket = require('ws');

// Remplace par ton username TikTok
let tiktokUsername = "your_username";

// WebSocket server (Unity connection)
const wss = new WebSocket.Server({ port: 8080 });

const tiktok = new WebcastPushConnection(tiktokUsername);

tiktok.connect().then(state => {
    console.log(`Connected to TikTok Live`);
}).catch(err => {
    console.error('Connection failed', err);
});

// Quand quelqu’un envoie un message
tiktok.on('chat', data => {
    console.log(`${data.nickname}: ${data.comment}`);

    // envoyer à Unity
    const message = JSON.stringify({
        user: data.nickname,
        message: data.comment
    });

    wss.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(message);
        }
    });
});
```

---

# 🎮 4. CÔTÉ UNITY (recevoir chat)

Script C# :

```csharp id="unityws01"
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class WebSocketClient : MonoBehaviour
{
    private ClientWebSocket ws;

    async void Start()
    {
        ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None);
        Receive();
    }

    async void Receive()
    {
        var buffer = new byte[1024];

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);

            Debug.Log("Chat: " + msg);

            // ici tu traites A B C D
        }
    }
}
```

---

# 🔥 5. LOGIQUE GAMEPLAY (chat → réponse)

Exemple :

- Viewer tape "A"
- Unity reçoit message
- Compare avec bonne réponse
- Update score

---

# 🚀 6. PIPELINE FINAL

### 🎥 Stream

- Unity → OBS → TikTok Live

### 📡 Data flow

TikTok → Node.js → Unity → affichage

---

# 💡 7. AMÉLIORATIONS (TRÈS PUISSANT)

Ajoute :

- 🎁 dons TikTok → bonus points
- ❤️ likes → multiplier score
- 👑 top viewer affiché en 3D

---

# 🔥 8. PROCHAIN NIVEAU

Si tu veux aller encore plus loin, je peux te faire :

👉 🤖 robot qui bouge les lèvres avec la voix
👉 🎮 UI ultra stylée (niveau jeu TV pro)
👉 🧠 génération automatique de questions en live
👉 💰 stratégie pour monétiser sur TikTok

---

Dis-moi :
👉 tu veux que je te fasse maintenant **le système complet de scoring + leaderboard temps réel**
ou
👉 **les animations + expressions du robot (niveau ultra réaliste)**

tous : 

👉 tu veux que je te fasse maintenant le système complet de scoring + leaderboard temps réel
ou
👉 les animations + expressions du robot (niveau ultra réaliste)

Parfait — là on passe au niveau “jeu pro”. Je te donne **les deux blocs complets** :

1. scoring + leaderboard temps réel
2. animations + expressions réalistes du robot

Tu peux intégrer directement dans ton projet Unity.

---

# 🧠 1. SYSTÈME COMPLET DE SCORING (temps réel)

## 🎯 Objectif

- chaque viewer = un joueur
- réponse via chat (A/B/C/D)
- points attribués en live
- leaderboard mis à jour instantanément

---

## 🧱 Structure recommandée

Créer 3 scripts :

- `PlayerData.cs`
- `ScoreManager.cs`
- `LeaderboardUI.cs`

---

## 👤 PlayerData

```csharp
using System;

[Serializable]
public class PlayerData
{
    public string username;
    public int score;
    public int streak;

    public PlayerData(string name)
    {
        username = name;
        score = 0;
        streak = 0;
    }
}
```

---

## 🧮 ScoreManager (cœur du système)

```csharp
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private Dictionary<string, PlayerData> players = new Dictionary<string, PlayerData>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterAnswer(string username, string answer, string correctAnswer)
    {
        if (!players.ContainsKey(username))
            players[username] = new PlayerData(username);

        PlayerData player = players[username];

        if (answer == correctAnswer)
        {
            player.streak++;
            int points = 10 + (player.streak * 2);
            player.score += points;

            AIManager.Instance.ReactToAnswer(true);
        }
        else
        {
            player.streak = 0;
        }

        LeaderboardUI.Instance.UpdateLeaderboard(GetTopPlayers());
    }

    public List<PlayerData> GetTopPlayers()
    {
        return players.Values
            .OrderByDescending(p => p.score)
            .Take(5)
            .ToList();
    }
}
```

---

## 🏆 Leaderboard UI

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance;

    public Text leaderboardText;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateLeaderboard(List<PlayerData> topPlayers)
    {
        leaderboardText.text = "";

        int rank = 1;
        foreach (var player in topPlayers)
        {
            leaderboardText.text += $"{rank}. {player.username} - {player.score}\n";
            rank++;
        }
    }
}
```

---

## 🔗 Connexion avec le chat (IMPORTANT)

Dans ton script WebSocket Unity :

```csharp
void ProcessMessage(string json)
{
    var data = JsonUtility.FromJson<ChatMessage>(json);

    string answer = data.message.ToUpper();

    if (answer == "A" || answer == "B" || answer == "C" || answer == "D")
    {
        ScoreManager.Instance.RegisterAnswer(
            data.user,
            answer,
            QuestionManager.Instance.correctAnswer
        );
    }
}
```

---

# 🔥 BONUS GAMEPLAY (à ajouter)

- 🔥 streak = multiplicateur
- ⚡ fastest answer = bonus
- 💎 dons TikTok = points boost

---

# 🤖 2. ANIMATIONS + EXPRESSIONS ULTRA RÉALISTES

## 🎯 Objectif

Ton robot doit :

- parler
- bouger
- réagir émotionnellement

---

## 🧱 Setup Unity Animator

Créer Animator avec états :

- Idle
- Talking
- Happy
- Angry
- Hype

---

## 🎬 Script Animation Controller

```csharp
using UnityEngine;

public class RobotAnimator : MonoBehaviour
{
    public Animator animator;

    public void SetTalking(bool isTalking)
    {
        animator.SetBool("isTalking", isTalking);
    }

    public void ReactCorrect()
    {
        animator.SetTrigger("happy");
    }

    public void ReactWrong()
    {
        animator.SetTrigger("angry");
    }

    public void Hype()
    {
        animator.SetTrigger("hype");
    }
}
```

---

## 🗣️ Synchronisation voix + animation

Dans ton AIManager :

```csharp
public RobotAnimator robotAnimator;

public void Speak(string text)
{
    robotAnimator.SetTalking(true);

    Debug.Log("Robot: " + text);

    // ici ElevenLabs API

    Invoke("StopTalking", 2f);
}

void StopTalking()
{
    robotAnimator.SetTalking(false);
}
```

---

# 🎭 EXPRESSIONS FACIALES (niveau avancé)

Si ton modèle le permet :

👉 utilise **BlendShapes**

Exemples :

- sourire
- surprise
- colère

---

## Script BlendShapes

```csharp
using UnityEngine;

public class FacialController : MonoBehaviour
{
    public SkinnedMeshRenderer faceMesh;

    public void SetSmile(float value)
    {
        faceMesh.SetBlendShapeWeight(0, value);
    }

    public void SetAngry(float value)
    {
        faceMesh.SetBlendShapeWeight(1, value);
    }
}
```

---

# 🔥 3. RENDU ULTRA PRO (ce qui fait la diff)

Ajoute :

### 🎥 Caméra dynamique

- zoom quand réponse correcte
- shake quand mauvaise réponse

---

### 💡 Lumières dynamiques

- vert = correct
- rouge = wrong

---

### 🎆 Effets

- particules
- glow
- explosion visuelle

---

# 🚀 4. PIPELINE FINAL (PRODUCTION)

1. Unity (jeu)
2. Node.js (TikTok)
3. WebSocket
4. IA voix
5. OBS → TikTok Live

---

# 💰 5. CE QUI VA FAIRE EXPLOSER TON JEU

- réactions exagérées du robot
- leaderboard visible en permanence
- interaction constante

---

# 🔥 PROCHAINE ÉTAPE (TRÈS PUISSANTE)

Je peux maintenant te faire :

👉 🤖 synchronisation lèvres + voix (lip sync réaliste)
👉 🎮 système complet de questions dynamiques générées par IA
👉 💸 système de monétisation TikTok (gifts = gameplay)

---

Dis-moi ce que tu veux en next niveau.

tous et pour terminer ca sera sur github en mode privée (faut éviter de surcharger la mémoire donc le projet doit être bien structuré ) : Je peux maintenant te faire : 👉 🤖 synchronisation lèvres + voix (lip sync réaliste) 👉 🎮 système complet de questions dynamiques générées par IA 👉 💸 système de monétisation TikTok (gifts = gameplay)

Parfait — là on finalise ton projet en mode **pro + scalable + prêt GitHub privé**.
Je te donne les 3 briques demandées + une **structure propre pour éviter de surcharger la mémoire** (très important pour Unity + live).

---

# 🧱 0. STRUCTURE PROJET (optimisée GitHub privé)

Organise ton repo comme ça :

```
CongoQuiz3D/
│
├── UnityProject/
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Core/
│   │   │   ├── AI/
│   │   │   ├── UI/
│   │   │   ├── Network/
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   ├── ScriptableObjects/
│
├── Backend/
│   ├── server.js
│   ├── tiktok.js
│
├── AI/
│   ├── prompts/
│   ├── question_generator.js
│
├── README.md
├── .gitignore
```

👉 `.gitignore` important :

```
Library/
Temp/
Build/
Logs/
```

---

# 🤖 1. LIP SYNC (robot qui parle réaliste)

## 🎯 Objectif

Synchroniser :

- voix (ElevenLabs)
- bouche (animation Unity)

---

## 🧠 Principe simple (efficace)

On ne fait PAS du vrai phonème complexe (trop lourd)
👉 on simule avec intensité audio

---

## 🔊 Script LipSync

```csharp id="lipsync01"
using UnityEngine;

public class LipSync : MonoBehaviour
{
    public SkinnedMeshRenderer faceMesh;
    public AudioSource audioSource;

    public int mouthOpenBlendShapeIndex = 0;

    void Update()
    {
        float[] spectrum = new float[256];
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Blackman);

        float intensity = 0f;

        foreach (var s in spectrum)
            intensity += s;

        float value = Mathf.Clamp(intensity * 1000, 0, 100);

        faceMesh.SetBlendShapeWeight(mouthOpenBlendShapeIndex, value);
    }
}
```

---

## 🎯 Résultat

- le robot “ouvre la bouche” selon la voix
- léger mais efficace pour TikTok

---

# 🧠 2. QUESTIONS DYNAMIQUES (IA)

## 🎯 Objectif

- générer des questions en live
- éviter base statique
- contenu infini

---

## 🧠 Backend IA (Node.js)

Utilise OpenAI

---

## 🔹 Script génération

```javascript id="aiquestions01"
const OpenAI = require("openai");

const client = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

async function generateQuestion() {
    const response = await client.chat.completions.create({
        model: "gpt-4o-mini",
        messages: [
            {
                role: "system",
                content: "You generate quiz questions about Republic of Congo."
            },
            {
                role: "user",
                content: "Generate 1 multiple choice question with 4 answers and indicate the correct one."
            }
        ]
    });

    return response.choices[0].message.content;
}

module.exports = { generateQuestion };
```

---

## 🔗 Envoi vers Unity

Dans ton serveur :

```javascript id="sendunity01"
const { generateQuestion } = require('./question_generator');

async function sendQuestion(ws) {
    const q = await generateQuestion();
    ws.send(JSON.stringify({ type: "question", data: q }));
}
```

---

# 💸 3. MONÉTISATION TIKTOK (Gifts → gameplay)

## 🎯 Objectif

Transformer les dons en actions dans le jeu

---

## 💡 Idées gameplay

- 🎁 cadeau → +50 points
- 💎 gros don → multiplier score x2
- 👑 top donateur → bonus spécial

---

## 🔌 Backend TikTok gifts

```javascript id="gift01"
tiktok.on('gift', data => {
    console.log(`${data.nickname} sent ${data.giftName}`);

    const payload = {
        type: "gift",
        user: data.nickname,
        gift: data.giftName
    };

    wss.clients.forEach(client => {
        client.send(JSON.stringify(payload));
    });
});
```

---

## 🎮 Unity réception

```csharp id="giftunity01"
void HandleGift(string user, string gift)
{
    int bonus = 50;

    ScoreManager.Instance.AddPoints(user, bonus);

    AIManager.Instance.Speak(user + " just boosted the game!");
}
```

---

# ⚡ 4. OPTIMISATION MÉMOIRE (CRUCIAL)

Sinon ton jeu va lag en live.

---

## ✅ À faire absolument

### 🔹 1. Pooling objets

- ne recrée pas UI/effets
- réutilise

---

### 🔹 2. Audio cache

- phrases répétées → stocker audio

---

### 🔹 3. Questions limitées

- garde max 20 en mémoire

---

### 🔹 4. JSON léger

- évite gros fichiers

---

# 🧠 5. PROMPTS CURSOR (FINAL BUNDLE)

---

## 🔥 Prompt global optimisation

```id="optiproject01"
Refactor my Unity project for performance and scalability.

Goals:
- Reduce memory usage
- Optimize scripts
- Use object pooling
- Improve real-time responsiveness
- Clean architecture

Output:
- Improved scripts
- Suggestions
- Folder organization
```

---

## 🔥 Prompt lip sync avancé

```id="lipsyncpro01"
Improve lip sync system in Unity.

Add:
- smoother mouth movement
- better audio analysis
- optional phoneme-based system

Keep it lightweight.
```

---

## 🔥 Prompt gameplay viral

```id="viral01"
Enhance my quiz game for TikTok virality.

Add:
- hype events
- random bonuses
- audience interaction triggers
- dramatic AI reactions
```

---

# 🚀 6. PIPELINE FINAL (TON JEU COMPLET)

```
TikTok Live
   ↓
Node.js (chat + gifts)
   ↓
WebSocket
   ↓
Unity 3D
   ↓
AI Robot (voix + réactions)
   ↓
OBS → Stream
```

---

# 💥 7. DERNIER CONSEIL (TRÈS HONNÊTE)

Ton succès ne dépend pas de :
❌ Unity
❌ IA
❌ code

👉 mais de :
✔ rythme
✔ personnalité du robot
✔ interaction

---

# 🎯 NEXT LEVEL (si tu veux aller encore plus loin)

Je peux te faire :

- 🤖 avatar robot ULTRA réaliste style MetaHuman
- 🎮 système multi-room (plusieurs parties live)
- 📊 dashboard analytics viewers

---

Dis-moi :
👉 tu veux maintenant que je te fasse un **plan pour lancer et percer sur TikTok avec ce jeu (growth + stratégie)**
ou
👉 un **pack final prêt à upload sur GitHub (README + doc + scripts clean)**

Pour terminer je veux en plus l'introduction (du lingala et kituba comme langue du Congo dont la capitale est Brazzaville en plus du français comme langues par défaut avec des mini jeux proposées dedans (quiz, sémantique, mot melé, mot croisé, blind test , mot mystere, jeu de memoire, jeu de chrono, devinette des mots par rapport à l'image associée qui est affiché, des battles lancés (les 2 joueur ayant le plus marqué ou battle lancé par des cadeaux) ) et tout ça : 

👉 tu veux maintenant que je te fasse un plan pour lancer et percer sur TikTok avec ce jeu (growth + stratégie)
ou
👉 un pack final prêt à upload sur GitHub (README + doc + scripts clean)

Parfait — on clôture ton projet avec **une intro immersive + multi-langues + mini-jeux + battles + pack GitHub prêt** + **stratégie TikTok pour percer**. Tu auras quelque chose de **cohérent, scalable et publiable**.

---

# 🎬 1. INTRO DU JEU (robot + langues 🇨🇬)

👉 Langues :

- Français (par défaut)
- Lingala
- Kituba

---

## 🤖 Script d’intro (voix robot)

🎵 (musique dynamique)

🤖 FR :
“Bienvenue dans Congo Quiz Live ! Le jeu interactif en direct depuis Brazzaville 🇨🇬 !
Teste ta culture, affronte les autres joueurs et deviens le numéro 1 !”

🤖 Lingala :
“Boyei malamu na Congo Quiz Live !
Luka koyeba, pesa eyano noki, mpe koma champion !”

🤖 Kituba :
“Mbote na Congo Quiz Live !
Tala mayele na nge, pesa mvutu, mpe kuma n°1 !”

🤖 HYPE :
“🔥 Le jeu commence MAINTENANT !!”

---

## 🌍 Gestion des langues (Unity)

### Script LanguageManager

```csharp id="lang01"
public enum Language { FR, LINGALA, KITUBA }

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;
    public Language currentLanguage = Language.FR;

    void Awake()
    {
        Instance = this;
    }

    public string GetText(string key)
    {
        switch (currentLanguage)
        {
            case Language.LINGALA:
                return LingalaDict[key];
            case Language.KITUBA:
                return KitubaDict[key];
            default:
                return FrenchDict[key];
        }
    }

    private Dictionary<string, string> FrenchDict = new Dictionary<string, string>()
    {
        {"welcome", "Bienvenue !"}
    };

    private Dictionary<string, string> LingalaDict = new Dictionary<string, string>()
    {
        {"welcome", "Boyei malamu !"}
    };

    private Dictionary<string, string> KitubaDict = new Dictionary<string, string>()
    {
        {"welcome", "Mbote !"}
    };
}
```

---

# 🎮 2. MINI-JEUX (système modulaire)

👉 Tu dois créer un **GameModeManager**

---

## 🧠 Prompt Cursor (multi-jeux)

```id="multigame01"
Create a modular game system in Unity.

Game modes:
- Quiz
- Word scramble
- Crossword
- Memory game
- Guess image
- Blind test (audio)
- Mystery word
- Speed challenge

Requirements:
- Each mode is a separate script
- Switch modes dynamically
- Shared scoring system
```

---

## 🎯 Modes à implémenter

### 1. Quiz

→ déjà fait

---

### 2. Mot mélangé

- afficher mot mélangé
- chat doit deviner

---

### 3. Mot croisé (simplifié)

- grille + indices

---

### 4. Blind test 🎵

- jouer musique
- chat devine artiste

---

### 5. Image guessing 🖼️

- afficher image
- chat répond

---

### 6. Mémoire 🧠

- retourner cartes

---

### 7. Chrono ⏱️

- répondre le plus vite

---

### 8. Devinette

- “Je suis une ville du Congo…”

---

# ⚔️ 3. SYSTÈME DE BATTLE (ULTRA VIRAL)

## 🎯 Fonctionnement

### Cas 1 : top 2 joueurs

- duel automatique

### Cas 2 : via cadeaux 🎁

- viewer lance battle

---

## 💻 Script BattleManager

```csharp id="battle01"
public class BattleManager : MonoBehaviour
{
    public void StartBattle(string player1, string player2)
    {
        AIManager.Instance.Speak(player1 + " VS " + player2 + " 🔥");

        // reset scores temporaires
    }

    public void EndBattle(string winner)
    {
        AIManager.Instance.Speak(winner + " wins the battle!");
    }
}
```

---

## 🎁 Trigger via TikTok gift

```javascript id="battleGift01"
if (data.giftName === "Rose") {
    // lancer battle
}
```

---

# 📦 4. PACK FINAL GITHUB (PRÊT)

👉 Plateforme : GitHub

---

## 📄 README.md (pro)

# 🇨🇬 Congo Quiz Live 3D

Interactive TikTok Live game built with Unity + AI.

## 🎮 Features

- 3D quiz game
- AI robot host
- Multi-language (French, Lingala, Kituba)
- Real-time TikTok interaction
- Multiple mini-games
- Leaderboard & battles

## 🧠 Tech Stack

- Unity 3D
- Node.js
- WebSocket
- OpenAI API
- ElevenLabs

## 🚀 Run

### Backend

npm install
node server.js

### Unity

Open project → Run scene

## 🔥 Gameplay

- Answer in chat (A/B/C/D)
- Gifts = bonuses
- Top players = battles

## 📡 Stream

Use OBS → TikTok Live

---

# 🚀 5. STRATÉGIE POUR PERCER SUR TIKTOK

👉 Là tu fais la différence.

---

## 🎯 Format live

- sessions de 1h
- rythme rapide
- zéro temps mort

---

## 🔥 Hooks (début live)

- “⚠️ Personne ne trouve cette question !”
- “🔥 100 points à gagner MAINTENANT !”

---

## 💡 Engagement

- répondre aux pseudos
- afficher leaderboard
- provoquer les viewers

---

## 🎁 Monétisation

- dons = pouvoir
- top donateur affiché
- battle sponsorisée

---

## 📈 Croissance

- poster extraits sur TikTok
- moments drôles du robot
- fails viewers

---

# 🧠 6. DERNIER CONSEIL (clé réelle)

Ton projet est déjà **très avancé**.

Mais ce qui fera la diff :

👉 le robot doit être une STAR
👉 pas juste un programme

---

# 🔥 SI TU VEUX CONTINUER

Je peux encore te faire :

👉 🎤 voix du robot ultra naturelle (accent africain stylé)
👉 🎮 UI digne d’un jeu TV pro
👉 💰 système complet de revenus TikTok optimisé

---

👉 Dis-moi : tu veux maintenant
**“optimiser pour 10k viewers”**
ou
**“transformer ça en startup / app rentable”**

Oui je veux tous ça et pour terminer le nom du jeu c'est CongoGames: 

👉 🎤 voix du robot ultra naturelle (accent africain stylé)
👉 🎮 UI digne d’un jeu TV pro
👉 💰 système complet de revenus TikTok optimisé  

“optimiser pour 10k viewers”
et
“transformer ça en startup / app rentable”

Parfait — on clôture avec une version **pro, cohérente et prête à scaler** pour **CongoGames**.
Je te livre les 4 blocs finaux : voix du robot, UI TV pro, monétisation optimisée, stratégie 10k viewers + transformation en startup.

---

# 🎤 1. VOIX DU ROBOT (naturelle + vibe africaine)

## 🎯 Objectif

- voix **chaleureuse, énergique**
- légère musicalité africaine (sans caricature)
- phrases courtes (TikTok)

👉 Outil : ElevenLabs

---

## 🔥 Prompt voix (à utiliser dans ElevenLabs)

```text id="voicefinal01"
Voice style:
- African French accent (subtle, natural)
- Energetic game show host
- Warm, charismatic, engaging

Delivery:
- Fast-paced
- Expressive reactions
- Short punchy sentences

Examples:
- “🔥 Attention ! Nouvelle question !”
- “💥 INCROYABLE ! Bonne réponse !”
- “😱 Personne n’a trouvé ?!”
```

---

## 🧠 Optimisation pro

- pré-génère :
  - réactions
  - phrases hype
- garde cache audio (évite latence)

---

# 🎮 2. UI NIVEAU JEU TV PRO

## 🎯 Style visuel

- inspiré jeux TV (Questions pour un champion vibe)
- lisible sur mobile (ultra important TikTok)

---

## 🎨 Éléments clés

### 📺 Écran principal

- question = GRAND format
- réponses = couleurs distinctes

---

### 🏆 Leaderboard

- toujours visible
- animé (montée / descente)

---

### ⏱️ Timer

- cercle animé
- couleur change (vert → rouge)

---

## 💻 Prompt Cursor UI

```id="uipro01"
Create a professional TV-style UI for a quiz game.

Requirements:
- Large readable text (mobile-first)
- Animated leaderboard
- Circular countdown timer
- Smooth transitions
- Bright contrasting colors

Style:
- Game show aesthetic
- High energy
- Clean layout
```

---

## 🔥 Bonus visuel

- glow sur bonnes réponses
- shake caméra sur erreurs
- spotlight sur top player

---

# 💰 3. MONÉTISATION TIKTOK (optimisée)

## 🎯 Objectif

transformer viewers → payeurs → joueurs engagés

---

## 💡 Système complet

### 🎁 Gifts → effets


| Cadeau    | Effet                 |
| --------- | --------------------- |
| Rose      | +50 points            |
| Lion      | x2 score              |
| Galaxy    | lance battle          |
| Don élevé | freeze autres joueurs |


---

## 💻 Script logique

```csharp id="monet01"
public void ApplyGift(string user, string gift)
{
    switch (gift)
    {
        case "Rose":
            ScoreManager.Instance.AddPoints(user, 50);
            break;

        case "Galaxy":
            BattleManager.Instance.StartRandomBattle();
            break;
    }
}
```

---

## 🧠 Astuce PRO

Annonce toujours :
👉 “🎁 Les gifts donnent des BONUS !”

---

# 🚀 4. STRATÉGIE POUR 10K VIEWERS

## 🎯 3 piliers

---

## 🔥 1. HOOK (les 5 premières secondes)

Exemples :

- “⚠️ 99% des gens échouent à cette question”
- “🔥 200 points à gagner MAINTENANT”

---

## ⚡ 2. Rythme

- question toutes les 8–10 secondes
- aucune pause
- robot parle constamment

---

## 👥 3. Interaction

- citer les pseudos
- afficher classement
- provoquer :

👉 “@user tu es dernier 😭”

---

## 📈 4. Croissance

- clip tes lives
- poste sur TikTok
- recycler moments drôles

---

# 💼 5. TRANSFORMER EN STARTUP

## 🎯 Vision

👉 CongoGames = plateforme interactive live gaming

---

## 💡 Étapes

### 1. MVP (ce que tu fais maintenant)

- jeu live TikTok

---

### 2. V2

- app mobile
- multi-room

---

### 3. V3

- marques sponsor
- quiz sponsorisés

---

## 💰 Revenus

- TikTok gifts
- sponsors
- pub intégrée
- abonnements premium

---

## 🧠 Différenciation

- contenu africain 🇨🇬
- multi-langues
- IA robot

👉 c’est ça ta force

---

# 📦 6. VERSION FINALE (résumé clair)

**CongoGames =**

- 🎮 jeu 3D Unity
- 🤖 robot IA
- 🗣️ voix naturelle
- 📡 TikTok live connecté
- 💰 monétisation intégrée
- 🌍 multi-langues

---

# 🔥 DERNIER CONSEIL (important)

Tu as un projet très fort.
Mais pour réussir :

👉 fais SIMPLE au début
👉 lance vite
👉 améliore avec les viewers

---

# 🚀 SI TU REVIENS PLUS TARD

Je peux t’aider à :

- lever des fonds 💰
- créer une vraie startup tech
- scaler à 100k viewers

---

🎯 Tu as maintenant un blueprint complet pour lancer **CongoGames**.

Et ici, dans le développement de mon jeu que je suis en train de faire avec le sort IA, je suis sur la partie qui concerne l'enrichissement des datasets, parce qu'il a exemple plusieurs types de jeux. Et moi, ce que j'aimerais, c'est recueillir de la donnée pour qu'elle reste en local et soit directement sur le GitHub. Mais en tout cas, recueillir de la donnée, de la donnée de type image. Par exemple, il y a des types de jeux où on doit deviner des images. Il y a des types de jeux où il y a la musique qui se lance et on me pose la question en fonction de cette musique, par exemple le type de la musique, l'histoire autour de cette musique, le nom des chanteurs ou des chanteurs de la musique, des choses comme ça. Et moi, j'aimerais tout ça justement dans mon jeu. Et avec le sort IA, j'ai du mal de parler des droits d'auteur, mais normalement, je sais que c'est possible de récupérer de la musique, des images sans discuter parce que j'ai déjà eu à le faire sur Internet là. Et du coup, moi, j'aimerais besoin d'avoir ton aide pour que tu m'aides à recueillir toutes ces datas, les structurer et tout et me donner un prompt avec des liens et tout cela où on peut recueillir la data. Tout cela, je veux d'énormes datas pour enrichir, enrichir, renforcer, améliorer tous les types de jeux. Je dis bien tous les types de jeux.Côté dépôt (déjà sur origin/master, commit 922c371)
GridThemeBank : 2 thèmes en plus (histoire, éco/vie courante) + mots 4–7 car. pour grilles 7×7.
CongoLocalQuizBank : 5 questions en plus (stade de Kintélé, cathédrale Sainte-Anne, live lisible, Congo vs RDC, etc.) — vérification factuelle faite.
MiniGameDemoBanks : 3 manches de blind + 2 manches image (Kintélé avec kintele optionnel, code IATA BZV).
Playlist audio : fichier Theme/blind-test/track01.ogg et track02.ogg (Wikimedia Commons, mêmes sources que ATTRIBUTION.md) + méta Unity. ThemeMusicPlayer enchaînera ces pistes en mode blind-test.
ATTRIBUTION.md : note sur le dossier blind-test / URLs.
NOMS_FICHIERS.txt : entrée optionnelle kintele.
docs/WORKFLOW_LOCAL.md : ordre pull → npm run start-all → Play Unity ; README mis à jour avec le lien.
Sur ton PC : C:\Congogame
Ouvre un terminal dans le dossier du clone (pas l’environnement cloud) :
git pull origin master
Backend (comme d’habitude) : npm run start-all à la racine, avec un Backend/.env correct.
Unity : ouvre le projet, Play (une scène avec RuntimeBootstrap ou ta scène habituelle).
Les .ogg arrivent avec le pull (≈4,4 Mo au total) : si le clone met du temps, c’est surtout ça.

Pas encore “tout le pack” (honnête)
Enrichissement “massif” (centaines d’items) : c’est itératif ; ce lot en ajoute un bon lot, pas une base exhaustive.
Images “réelles” : seulement des fichiers que tu mets (brazzaville, kintele, etc.) + licences ; le jeu garde le flou 15 s + secours s’il manque le fichier.
Blind test “titre, date, chanteur” calés sur la piste : seulement si chaque piste a sa fiche (fichier ou banque) — pas d’invention automatique à partir d’Internet.
Look PS5 / plateau 3D : toujours un projet art + scène Unity ; voir docs/AAA_Blockbuster_Specification_CongoGames.md + ROADMAP_UI_3D.md.
En une phrase : tu peux pull, lancer le backend, Play : la playlist blind-test est fournie légalement (Commons) et les banques sont un peu plus grosses ; le reste, c’est contenu et 3D en lots suivants.

Je vais être direct avec toi :
👉 récupérer “énormément de data images + musiques sans discuter des droits”, c’est risqué et peut faire sauter ton projet (TikTok + GitHub + monétisation).

Mais bonne nouvelle :
👉 tu peux construire une **grosse base de données propre, locale, scalable et monétisable** sans problème juridique.

Je te donne **une méthode PRO complète** 👇

---

# 🧠 1. STRATÉGIE INTELLIGENTE (ce que font les vrais projets)

Au lieu de “scraper tout Internet”, fais :

### ✅ 3 sources SAFE

1. contenus libres (open data)
2. contenus générés par IA
3. contenus créés par ta communauté

👉 ça te donne :

- volume énorme
- zéro problème légal
- contenu unique (très important)

---

# 🌍 2. SOURCES DE DATA (IMAGES + AUDIO)

## 🖼️ IMAGES LIBRES

### 🔥 Source principale

- Wikimedia Commons

👉 parfait pour :

- villes (Brazzaville, Pointe-Noire)
- monuments
- personnalités historiques

---

### 🔥 Autres sources

- Unsplash
- Pexels

---

## 🎵 MUSIQUE / AUDIO

### ✅ SAFE

- Free Music Archive
- Wikimedia Commons

👉 idéal pour blind test (instrumental, ambiance)

---

## ⚠️ IMPORTANT

❌ Évite :

- Spotify / YouTube (copyright)
- téléchargement sauvage

👉 sinon TikTok peut te shadowban

---

# 🤖 3. DATA GÉNÉRÉE PAR IA (TRÈS PUISSANT)

👉 Tu peux créer :

- images (quiz)
- questions
- descriptions
- indices

avec :

- OpenAI
- Stable Diffusion

---

## 🔥 Prompt génération dataset images

```id="datasetimg01"
Generate 50 quiz image ideas about Republic of Congo.

For each item:
- Title
- Description of the image
- Category (city, culture, music, history)
- Difficulty level
- Accepted answers (multiple variations)

Format: JSON
```

---

# 🧱 4. STRUCTURE DATASET (TRÈS IMPORTANT)

👉 organise ton repo comme ça :

```id="dataset01"
Datasets/
│
├── images/
│   ├── brazzaville.jpg
│   ├── kintele.jpg
│
├── audio/
│   ├── track01.ogg
│
├── quiz/
│   ├── questions.json
│
├── blindtest/
│   ├── tracks.json
│
├── metadata/
│   ├── attribution.json
```

---

## 📄 Exemple JSON (image quiz)

```json id="jsonimg01"
{
  "id": "img_001",
  "file": "brazzaville.jpg",
  "question": "Quelle est cette ville ?",
  "answers": ["Brazzaville"],
  "hints": ["Capitale du Congo"],
  "difficulty": "easy"
}
```

---

## 🎵 Exemple JSON (blind test)

```json id="jsonaudio01"
{
  "id": "audio_01",
  "file": "track01.ogg",
  "question": "Quel est le style musical ?",
  "answers": ["rumba", "afro"],
  "artist": "Unknown",
  "source": "Wikimedia Commons"
}
```

---

# 🔗 5. PROMPT CURSOR POUR SCRAPER PROPREMENT

👉 (respect des licences inclus)

```id="scraper01"
Create a Node.js script to collect media data from open sources.

Requirements:
- Fetch images from Wikimedia Commons API
- Download files locally
- Save metadata (title, author, license)
- Store everything in JSON format
- Avoid duplicates
- Limit requests to avoid blocking

Output:
- Clean dataset ready for a quiz game
```

---

# ⚡ 6. AUTOMATISATION (TRÈS PRO)

👉 pipeline :

1. script récupère data
2. nettoie
3. stocke local
4. commit GitHub

---

# 💾 7. GITHUB (IMPORTANT)

👉 garde :

- JSON léger
- images compressées
- audio .ogg (comme tu fais 👍)

👉 évite :

- fichiers lourds → utilise Git LFS si besoin

---

# 🔥 8. IDÉE ULTRA PUISSANTE (DIFFÉRENCIATION)

👉 ajoute :

### 🧑‍🤝‍🧑 contenu communautaire

- viewers envoient images
- viewers proposent questions

👉 ça te crée :

- data infinie
- engagement énorme

---

# ⚠️ 9. RÉALITÉ (très important)

Ton objectif :
❌ “énormes datasets”
✔ “datasets intelligents + évolutifs”

---

# 🚀 10. CE QUE TU AS DÉJÀ FAIT (très bien)

Tu es déjà en mode PRO :

- .ogg optimisé ✅
- attribution ✅
- structuration dataset ✅
- workflow Git propre ✅

👉 honnêtement : t’es au-dessus de beaucoup de projets étudiants

---

# 🎯 SI TU VEUX ALLER ENCORE PLUS LOIN

Je peux te faire :

👉 script complet qui génère 500+ questions Congo
👉 dataset initial prêt à injecter dans Unity
👉 système IA qui enrichit automatiquement ton jeu en live

---

Dis-moi :
👉génère directement un **gros dataset Congo prêt à utiliser (images + quiz + blind test)**
et
👉 un **script automatisé complet qui build ton dataset tout seul**

Pour récupérer des sons musicaux, par exemple de YouTube ou d'autres sites, ce qui concerne le Congo s'appelle des sons de chants du type traditionnel, toi tu connais déjà la réponse, par exemple poser la question dans quelle langue il s'agit, est-ce qu'il s'agit du kongo/lari (kilombo et tout le reste), du mbochi (vocal mbantou et autres ), du teke et autres langues ethniques par exemple dans quelle langue cette chanson est-elle chantée, par exemple. 🎯 SI TU VEUX ALLER ENCORE PLUS LOIN Je peux te faire : 👉 script complet qui génère 500+ questions Congo 👉 dataset initial prêt à injecter dans Unity 👉 système IA qui enrichit automatiquement ton jeu en live Dis-moi : 👉 tu veux que je te génère directement un gros dataset Congo prêt à utiliser (images + quiz + blind test) ou 👉 un script automatisé complet qui build ton dataset tout seul

Je comprends exactement ce que tu veux faire — et ton idée de quiz sur les **langues des chants (Kongo/Lari, Mbochi, Téké, etc.)** est excellente 🔥

Mais je vais être clair pour t’éviter un problème futur :

👉 **Télécharger directement des sons depuis YouTube = risqué (copyright + TikTok peut bloquer ton live).**
👉 Par contre, tu peux faire **EXACTEMENT le même jeu** avec une approche **pro + légale + scalable**.

---

# 🎯 STRATÉGIE PRO POUR TON BLIND TEST (LANGUES 🇨🇬)

## ✅ Option 1 (SAFE + RECOMMANDÉE)

Utiliser des sons libres (licence ouverte)

👉 Exemple :

- Wikimedia Commons
- Free Music Archive

👉 Tu cherches :

- “African traditional music”
- “Congo folk song”
- “drums Africa”

---

## ⚠️ Option 2 (RISQUÉE)

YouTube download → ❌
👉 à éviter si tu veux monétiser

---

## 💡 Option 3 (ULTRA INTELLIGENTE)

👉 créer TES propres audios (ou dataset IA)

- générer sons traditionnels (IA audio)
- ou enregistrer toi-même / communauté

👉 ça te donne :
✔ zéro copyright
✔ contenu unique
✔ avantage énorme

---

# 🚀 CE QUE TU M’AS DEMANDÉ → JE TE DONNE LES DEUX

👉 dataset prêt + script automatisé

---

# 📦 1. DATASET CONGO (exemple prêt à utiliser)

## 🎵 Blind test (langues)

```json id="dataset_blindtest_congo"
[
  {
    "id": "audio_001",
    "file": "kongo_song_01.ogg",
    "question_fr": "Dans quelle langue est chantée cette musique ?",
    "question_ln": "Loyembo oyo ezali na lokota nini ?",
    "question_kg": "Nkunga yayi ke na ndinga yinki ?",
    "choices": ["Kongo/Lari", "Mbochi", "Téké", "Lingala"],
    "answer": "Kongo/Lari",
    "category": "langue",
    "difficulty": "medium",
    "hint": "Langue parlée au sud du Congo"
  },
  {
    "id": "audio_002",
    "file": "mboshi_song_01.ogg",
    "question_fr": "Quelle langue entends-tu ici ?",
    "choices": ["Mbochi", "Téké", "Kongo", "Lingala"],
    "answer": "Mbochi",
    "difficulty": "hard"
  }
]
```

---

## 🖼️ Image guessing

```json id="dataset_images_congo"
[
  {
    "id": "img_001",
    "file": "brazzaville.jpg",
    "question": "Quelle est cette ville ?",
    "answers": ["Brazzaville"],
    "category": "geography",
    "difficulty": "easy"
  }
]
```

---

# ⚙️ 2. SCRIPT AUTOMATISÉ (Node.js)

👉 récupère + structure dataset

```javascript id="dataset_builder"
const fs = require("fs");

function generateQuestion(id, lang) {
    return {
        id: "auto_" + id,
        question_fr: "Dans quelle langue est chantée cette musique ?",
        choices: ["Kongo/Lari", "Mbochi", "Téké", "Lingala"],
        answer: lang,
        difficulty: "medium"
    };
}

let dataset = [];

const languages = ["Kongo/Lari", "Mbochi", "Téké", "Lingala"];

for (let i = 0; i < 100; i++) {
    let lang = languages[Math.floor(Math.random() * languages.length)];
    dataset.push(generateQuestion(i, lang));
}

fs.writeFileSync("blindtest_dataset.json", JSON.stringify(dataset, null, 2));

console.log("Dataset généré !");
```

---

# 🤖 3. IA QUI ENRICHIT EN LIVE (ULTRA PUISSANT)

👉 avec OpenAI

## Prompt :

```text id="ai_live_dataset"
Generate a quiz question about Republic of Congo music.

Include:
- language of the song
- cultural context
- 4 answer choices
- correct answer

Keep it short and TikTok friendly.
```

---

# 🧠 4. IDÉE GAMEPLAY (TRÈS FORTE)

👉 Ton jeu peut faire :

🎧 musique →
❓ “Quelle langue ?”
💬 chat répond
🔥 robot réagit

---

## 💥 Variante avancée

- “De quelle ethnie vient ce chant ?”
- “Quel type de rythme ?”
- “Nord ou Sud Congo ?”

---

# ⚠️ 5. POINT IMPORTANT (vérité)

👉 reconnaître automatiquement une langue à partir d’un audio = très difficile (IA avancée)

👉 donc :
✔ soit tu tags toi-même
✔ soit tu fais dataset propre

---

# 🚀 6. MEILLEURE APPROCHE (RECOMMANDÉE)

👉 mix :

- audio libre (Commons)
- audio créé
- metadata manuelle

---

# 🔥 7. CE QUI VA FAIRE TON SUCCÈS

Pas la quantité brute.

👉 mais :

- qualité des questions
- fun
- interaction

---

# 🎯 CONCLUSION

Tu es en train de construire :

👉 un vrai système de dataset gaming IA
👉 pas juste un jeu

---

# 🚀 NEXT STEP

Je peux te faire maintenant :

👉 🎵 un **pack de 50 questions blind test Congo ultra quali (langues + culture)**
👉 🧠 un **dataset complet multi-jeux prêt Unity (500+ items)**
👉 ⚙️ un **pipeline auto qui enrichit ton jeu tous les jours**

---

👉 génère **direct un dataset massif prêt à importer dans Unity**
et
👉 un **système automatisé complet (niveau startup)**

J'ai ce résultat dans Unity. C'est vrai, j'utilise une clé API pour OpenAI, mais je ne veux pas payer des trucs, je veux des trucs gratuits. Le but, c'était d'avoir une voix qui parle, mais je ne sais pas est-ce que c'est possible de demander à CursorIA de créer sa propre voix, donc entraîner cette voix pour que lui-même, il puisse juste lire les questions. résultat (que des warning rien de grave) : TTS: Unable to read data
UnityEngine.Debug:LogWarning (object)
CongoGames.AI.AIHostManager:LogTtsFailureThrottled (string) (at Assets/Scripts/AI/AIHostManager.cs:313)
CongoGames.AI.AIHostManager/d__33:MoveNext () (at Assets/Scripts/AI/AIHostManager.cs:184)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

CongoGames — quota OpenAI TTS dépassé : la voix est suspendue quelques minutes. Ajoute des crédits sur [https://platform.openai.com](https://platform.openai.com) — le jeu continue avec des bips.
UnityEngine.Debug:LogWarning (object)
CongoGames.AI.AIHostManager:LogTtsFailureThrottled (string) (at Assets/Scripts/AI/AIHostManager.cs:307)
CongoGames.AI.AIHostManager/d__33:MoveNext () (at Assets/Scripts/AI/AIHostManager.cs:184)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)

Tu as bien identifié le vrai problème : 
👉 ce n’est pas un bug Unity, c’est **le quota TTS OpenAI dépassé**.
Et tu as raison de vouloir une solution **gratuite et autonome** 👍

Je vais être direct :
❌ Cursor ne peut PAS “créer sa propre voix” tout seul
👉 mais
✅ tu peux avoir une **voix 100% gratuite, offline, intégrée dans Unity**

---

# 🎤 ✅ SOLUTION 1 (RECOMMANDÉE) — TTS LOCAL GRATUIT

👉 Utilise un moteur TTS offline comme :

- Coqui TTS
- eSpeak

---

## 🧠 Le meilleur choix pour toi

👉 **Coqui TTS**
✔ gratuit
✔ voix naturelles
✔ fonctionne sans internet
✔ pas de quota

---

## ⚙️ Architecture simple

```
Unity → envoie texte → Python TTS → génère .wav → Unity joue le son
```

---

## 💻 Script Python (TTS local)

```python
# tts_local.py
from TTS.api import TTS

tts = TTS(model_name="tts_models/fr/css10/vits")

text = "Bienvenue dans CongoGames !"
tts.tts_to_file(text=text, file_path="output.wav")
```

---

## 🎮 Unity (lecture audio)

```csharp
public AudioSource audioSource;

public IEnumerator PlayTTS(string path)
{
    using (WWW www = new WWW("file://" + path))
    {
        yield return www;
        audioSource.clip = www.GetAudioClip();
        audioSource.Play();
    }
}
```

---

# 🚀 SOLUTION 2 — WINDOWS VOIX GRATUITE (ULTRA SIMPLE)

👉 Unity peut utiliser la voix Windows directement

```csharp
using System.Speech.Synthesis;

public void Speak(string text)
{
    SpeechSynthesizer synth = new SpeechSynthesizer();
    synth.SpeakAsync(text);
}
```

✔ 100% gratuit
✔ instantané
❌ moins stylé (mais suffisant pour MVP)

---

# 🔥 SOLUTION 3 — VOIX PRÉ-GÉNÉRÉES (OPTIMISÉ TIKTOK)

👉 meilleure stratégie en réalité :

1. génère une fois les phrases
2. stocke en `.wav`
3. rejoue dans Unity

---

## 🎯 Exemple

```text
audio/
- correct.wav
- wrong.wav
- next_question.wav
```

---

## 🎮 Avantage

✔ zéro latence
✔ zéro coût
✔ fluide en live

---

# 🧠 SOLUTION 4 — MIX INTELLIGENT (RECOMMANDÉ)

👉 combine :

- phrases fixes → audio pré-généré
- questions dynamiques → TTS local

---

# ⚠️ TON ERREUR ACTUELLE

Ton message :

> quota OpenAI TTS dépassé

👉 donc :

- API fonctionne ✔
- mais tu n’as plus de crédits ❌

---

# 💡 CE QUE TU DOIS FAIRE MAINTENANT

## ✅ Étape simple

1. désactive OpenAI TTS
2. ajoute TTS local
3. fallback → bips (déjà OK)

---

# 🔥 PROMPT CURSOR (solution complète)

```id="ttsfix01"
Replace OpenAI TTS with a free local text-to-speech system.

Requirements:
- Use offline TTS (Coqui or system voice)
- Generate audio files dynamically
- Play audio in Unity
- Add fallback if TTS fails
- Cache generated audio files

Goal:
- No API cost
- No quota
- Smooth real-time gameplay
```

---

# 🎯 CONSEIL PRO (très important)

👉 sur TikTok :

❌ les gens s’en fichent que la voix soit parfaite
✔ ils veulent :

- énergie
- rythme
- réactions

---

# 🚀 CONCLUSION

Tu as 3 niveaux :

### 🟢 MVP (rapide)

👉 voix Windows

### 🟡 Intermédiaire

👉 Coqui TTS local

### 🔴 Pro

👉 audio pré-généré + mix TTS

---

# 🔥 SI TU VEUX ALLER PLUS LOIN

Je peux te faire :

👉 🎤 voix stylée africaine gratuite (config complète)
👉 ⚡ système ultra fluide sans latence
👉 🤖 robot qui parle comme un vrai streamer

---

Dis-moi :
👉 tu veux que je te fasse un **setup complet Coqui + Unity prêt à brancher** et 
👉 une **solution ultra simple en 5 minutes pour débloquer ton jeu maintenant**

One last thing, I remind you that the voice that speaks is supposed to be the animator, the one who animates the game. That is to say, when the voice speaks, the music is supposed to reduce. so that we can hear the voice more. And she is supposed to read the questions. And when, for example, the player makes mistakes by putting the wrong answers or The voice is supposed to tell him that it was the wrong answer. When the player puts the right answer, the voice is supposed to say it was the right answer. And if the game is over, the type of game is over, we move on to another game, the voice is supposed to announce it and then we move on. to the next game. When the The game starts. For each type of game, the voice explains how the game works. For each type of game, the voice must explain how the game works, the rules, what to do, what to follow, the functioning of the game and where to put the answers. And also, we must see the scores with the number of players and the scores, that is to say the history. The scores of the day, for example, we see the date of the day, today, the scores of the month, the global score or the scores of the week. All that, we must see it. Another point for the chrono game. Again, I repeat, I do not understand how it works. Because when we go to the interface, there is just a chrono that is displayed. In addition, the chrono that is displayed, it hides the texts, the writings that are behind. The interface is badly made. We don't understand the game of time. You have to improve it so that we can understand it, to put a section so that we can insert the answers. And if it's in a live TikTok, it's the users who put the answers in their chat. But the game of Cronut has to have a logic, we have to understand it. So you improve for the time game, but enough time to allow the user to be able to put the right answer. If he makes a mistake, the voice will say that he put the wrong answer. If he put a good answer, the voice will say that he put the right answer. Now, I want you to make some modifications. When the user makes a mistake by putting the wrong answer, remove the voice where there is a person who is laughing because it doesn't look pretty. Remove the voice or the person laughs when there are bad responses. You have to remove it, delete it. And then, for the game of grilles, the game of letters, I repeat, the game where there are letters, grilles, words to form in the grilles. I would like that for each word that is found, There is a list on the recapitulative side like a historical one, a list that will display the words found. De côté, en fouleur. And another thing, There is no precise order, that is to say you give a theme and in this theme we must find words. So you enlarge the grid, add boxes and do not put letters aligned together because it will be easy to guess the word. The goal is not to help the user. So you have to spread the letters well so that the user can find them. the word that needs to be found and some hints, for example, if necessary. But make sure that for each word found, it can appear in the historical right next to it. There is a historical, but also there is no order. That is to say, it's not for example... When we look for a word. Peu importe le mot qu'on cherche si on le trouve. If, for example, the word we are looking for, it must be a word in Lingala and in Kituba that we are looking for, with translations in French. If, for example, the word we are looking for, supposing, is, we will say, maï, which means in French, l'eau. And that I, for example, put... I put for example "motema" which means in French "the heart". As long as my theme is part of the list of words to look for, even if I put "maï" For example: Peu importe l'ordre. There is no order. For example, if we have two words that we need to find: "mai" and "motema", For example, when I launch the session, Moi, je mets mon thème à While the game was expecting me to put "Mai" for example. It doesn't pose any problem because there is no precise order. M-A-Y-I Donc, ça veut dire « eau ». Voilà un peu. What I meant to say, it means "eau" in French. Rearrange all that. Orchestrate all that. Everything must be ordered, structured. with Lia behind who has to organize all that. Lia can make jokes, for example, that is to say, make funny jokes. Se moquer par exemple de l'utilisateur ou des utilisateurs lorsqu'ils se trompent en mettant des blagues drôles. Et lorsque l'utilisateur trouve des bonnes réponses, il peut mettre des blagues drôles pour complimenter l'utilisateur, l'encourager, le motiver. Improve the interface again because at the level of the backgrounds for each type of game, it's always basic backgrounds. Improve the interface Improve the interfaces of each type of game I want a type of game, I want 3D, real, PS5 interfaces for each type of game : 

Parfait, là tu es à une étape critique :
👉 **tu dois transformer ton besoin en un prompt clair, structuré et exploitable par Cursor**
Sinon il va te générer n’importe quoi ou ignorer la moitié.

Je vais te le restructurer en **prompt PRO + clean + compréhensible par une IA** 👇

---

# 🧠 🎯 PROMPT FINAL À DONNER À CURSOR (ULTRA OPTIMISÉ)

Copie-colle **tel quel** :

```text
Refactor and improve my Unity project "CongoGames" with the following requirements.

========================
1. AI HOST (VOICE SYSTEM)
========================

The AI host (Lia) is the main presenter of the game.

Responsibilities:
- Read all questions clearly
- Explain rules at the start of each mini-game
- Announce transitions between games
- React to player answers:
    - Correct answer → positive, encouraging, funny response
    - Wrong answer → light teasing (NO laughing sound, remove any laughing audio)

IMPORTANT:
- When the AI speaks, background music volume must automatically decrease (ducking system)
- After speaking, music returns to normal

Add:
- Clean audio management system (voice priority over music)
- Smooth transitions (fade in/out)

========================
2. GAME FLOW & ANNOUNCEMENTS
========================

- At game start: explain rules clearly
- Between rounds: announce next game
- At game end: summarize and transition

========================
3. SCORE SYSTEM (VISIBLE & STRUCTURED)
========================

Display:
- Current players and scores (live leaderboard)
- Daily scores (with current date)
- Weekly leaderboard
- Monthly leaderboard
- Global leaderboard

UI must:
- Be always visible or easily accessible
- Be clean and animated (not static text)

========================
4. CHRONO GAME (FIX UX PROBLEM)
========================

Current issue:
- Timer overlaps UI
- Game logic unclear

Fix:
- Redesign UI layout so timer does NOT hide text
- Add clear instruction section:
    - what to do
    - how to answer
- Add answer input logic (or chat integration ready)

Gameplay:
- Player answers within time
- If correct → AI reacts positively
- If wrong → AI reacts with humor (NO laugh sound)

Timer:
- Enough time for thinking
- Visual countdown (clear and readable)

========================
5. WORD GRID GAME (MAJOR IMPROVEMENTS)
========================

Requirements:

Grid:
- Increase grid size
- Scatter letters randomly (NOT aligned words)
- Make discovery challenging

Gameplay:
- Theme-based (Lingala / Kituba words with French meaning)
- NO strict order:
    - Any valid word from the list can be found at any time

Example:
If words are ["mai", "motema"], player can find any in any order.

Add:
- Side panel showing:
    - list of found words (history)
    - visual highlight when found

Optional:
- Add hints system

========================
6. AI PERSONALITY (LIA)
========================

Lia must:
- Be energetic and funny
- Encourage players
- Make light jokes when players fail
- Congratulate creatively when correct

NO toxic behavior
NO annoying sounds (remove laughing audio)

========================
7. UI / UX (VERY IMPORTANT)
========================

Current problem:
- Basic UI
- Not engaging

Goal:
- AAA game show style (PS5-level inspiration)

Improve:
- Backgrounds for each game mode (unique themes)
- 3D feeling UI
- Animations (transitions, highlights, feedback)
- Clean layout (no overlap)

Each game mode must have:
- its own visual identity

========================
8. AUDIO SYSTEM IMPROVEMENT
========================

Implement:
- Audio manager with:
    - music channel
    - voice channel
- Auto-ducking system:
    - when voice plays → music volume reduced
    - after → restore volume

========================
9. CODE QUALITY
========================

- Modular architecture
- Clean scripts separation
- Scalable for future features
- Optimize performance (no unnecessary updates)

========================

Output:
- Updated scripts
- UI improvement suggestions
- Refactored systems
- Clear explanations of changes
```

---

# 🔥 POURQUOI CE PROMPT EST PARFAIT

👉 Il transforme ton message (très long) en :

- logique claire
- sections compréhensibles
- priorités nettes

👉 Cursor va :

- mieux comprendre
- mieux structurer
- mieux coder

---

# ⚠️ PETITE CORRECTION IMPORTANTE

Tu avais une idée excellente mais mal exprimée :

👉 le “chrono game” n’est pas clair → maintenant il devient :
✔ lisible
✔ jouable
✔ compréhensible

---

# 💡 CONSEIL PRO (TRÈS IMPORTANT)

Ne donne PLUS jamais :
❌ gros paragraphe mélangé FR/EN

👉 fais toujours :
✔ sections
✔ bullet points
✔ objectifs

---

oui je veux ça aussi

👉“transformer L'ia en vraie personnalité virale (script + punchlines)”

Parfait — là on va transformer **Lia** en **vraie star TikTok**, pas juste une voix robotique.
Le but : qu’elle **retienne les viewers, crée de l’émotion et donne envie de rester**.

---

# 🤖🔥 1. IDENTITÉ DE LIA (PERSONNALITÉ FORTE)

👉 Lia =

- 🎤 animatrice énergique
- 😏 un peu taquine
- 🧠 intelligente (culture Congo 🇨🇬)
- 😂 drôle mais jamais méchante

---

## 🎭 Ton de voix

- rapide
- expressif
- phrases courtes (très important TikTok)

---

# 🎤 2. SCRIPT GLOBAL (FLOW COMPLET)

## 🎬 INTRO LIVE

🔥 “Bienvenue dans CongoGames !!”

🇨🇬 “Ici on teste ta vraie culture du Congo !”

😏 “Attention… ici y’a pas de triche 👀”

🎯 “Réponds dans le chat… et deviens numéro 1 !”

💥 “C’est parti !!”

---

## ❓ LANCEMENT QUESTION

🧠 “Nouvelle question !!”

⚡ “Regarde bien… réfléchis vite…”

⏱️ “T’as 10 secondes !!”

---

## ✅ BONNE RÉPONSE

🔥 “OOOOH BIEN JOUÉ !!!”

💥 “Là tu m’as impressionnée !”

👑 “@username prend la tête !!”

😏 “Ok… toi tu connais vraiment le Congo”

---

## ❌ MAUVAISE RÉPONSE (drôle, pas toxique)

😅 “Aïe… ça pique ça…”

😂 “On va dire que t’étais pas réveillé hein…”

😏 “Même moi j’ai mal pour toi là…”

💡 “C’était pourtant facile !”

---

## ⚔️ BATTLE

⚔️ “OOOOOH BATTLE ACTIVÉE !!!”

🔥 “@player1 VS @player2 !!”

💥 “Que le meilleur gagne !!”

---

## 🎮 CHANGEMENT DE JEU

🎮 “On change de jeu !!”

🧠 “Prépare ton cerveau…”

⚡ “Nouveau défi qui arrive !!”

---

## 🏁 FIN DE PARTIE

🏆 “Fin de la partie !!”

👑 “Le boss aujourd’hui c’est @username !!”

🔥 “Tu peux faire mieux… reviens dans le live !!”

---

# 😂 3. PUNCHLINES VIRALES (TRÈS IMPORTANT)

👉 À injecter aléatoirement :

😏 “Tu joues ou tu regardes ?!”

🔥 “Ici c’est pas pour les faibles !”

😂 “Même ma grand-mère aurait trouvé ça…”

👀 “Je vois tout… fais pas semblant !”

💥 “Ça chauffe là !!”

---

# 🧠 4. VERSION MULTI-LANGUE 🇨🇬

👉 mélange intelligent :

## Lingala

- “Boyei malamu !”
- “Ozui te !” (tu t’es trompé)
- “Malamu !” (bien joué)

## Kituba

- “Mbote !”
- “Yo kele na mayele !”

👉 Lia peut switch selon moments → effet viral

---

# 🤖 5. PROMPT CURSOR (PERSONNALITÉ LIA)

```text id="lia_ai_prompt"
Create a dynamic AI host personality named Lia.

Personality:
- Energetic
- Funny and slightly teasing
- Encouraging players
- TikTok live game show host style

Behavior:
- Short punchy sentences
- React to correct answers with hype
- React to wrong answers with light humor (NO laughing sound)
- Use random punchlines to keep engagement high

Add:
- Multi-language support (French, Lingala, Kituba)
- Random variation in responses (avoid repetition)

Goal:
- Make Lia feel like a real live entertainer, not a robot
```

---

# 🎯 6. LOGIQUE TECH (IMPORTANT)

👉 Lia doit :

- choisir phrase aléatoire
- éviter répétition
- s’adapter au contexte

---

## Exemple simple

```csharp
string[] correctPhrases = {
    "Bien joué !",
    "Incroyable !",
    "Tu gères !"
};

string phrase = correctPhrases[Random.Range(0, correctPhrases.Length)];
AIManager.Speak(phrase);
```

---

# 🔥 7. SECRET POUR DEVENIR VIRAL

👉 Lia doit :

- parler souvent
- réagir à TOUT
- citer les pseudos

---

## 💥 Exemple LIVE

👉 “@Kevin… t’es dernier là 😭”

👉 “@Sarah est en train de DOMINER 🔥”

---

# 🚀 8. RÉSULTAT FINAL

Avec ça, Lia devient :

👉 une animatrice
👉 une personnalité
👉 un “personnage” que les gens veulent revoir

---

# 🎯 ENCORE PLUS FORT

 faire aussi : 

👉 🎤 script complet pour 1h de live
👉 😂 pack 200 punchlines ultra virales
👉 🤖 système IA qui improvise comme un streamer

---

👉 je veux maintenant un **pack complet de 200 punchlines prêt à injecter dans mon jeu** 

Parfait — voilà **un pack massif de 200 punchlines** prêtes à injecter dans ton jeu **CongoGames** 🔥
👉 classées par situation pour que tu puisses les utiliser facilement dans ton code

---

# 🎤 ✅ BONNES RÉPONSES (50)

1. “🔥 Là tu m’as surpris !”
2. “💥 Très très propre !”
3. “👑 On a un champion ici !”
4. “😏 Ok… toi tu connais !”
5. “🎯 Précis comme jamais !”
6. “🔥 Masterclass !”
7. “💪 Niveau expert validé !”
8. “👀 Je t’ai vu faire ça… incroyable”
9. “🎉 Bravo ça passe crème !”
10. “⚡ Trop rapide !”
11. “😮 Ah ouais… respect !”
12. “🔥 Tu joues sérieux toi”
13. “💥 Clean !”
14. “👑 Le boss se révèle”
15. “🎯 Parfait !”
16. “🔥 Là tu cuisines !”
17. “💪 Solide !”
18. “😏 Tu m’impressionnes”
19. “🎉 Validé !”
20. “⚡ Ultra rapide !”
21. “👀 Je note ça”
22. “🔥 Très fort !”
23. “💥 Rien à dire !”
24. “👑 Leader confirmé !”
25. “🎯 C’est carré !”
26. “🔥 Top niveau !”
27. “💪 Tu gères !”
28. “😮 Incroyable !”
29. “🎉 Bien joué !”
30. “⚡ Parfait timing !”
31. “👀 Tu connais le terrain”
32. “🔥 C’est du lourd !”
33. “💥 Validé direct !”
34. “👑 Tu prends la tête !”
35. “🎯 Excellent !”
36. “🔥 Rien à redire”
37. “💪 Fort !”
38. “😏 Ok champion”
39. “🎉 Magnifique !”
40. “⚡ Impressionnant !”
41. “👀 Très propre”
42. “🔥 Tu domines !”
43. “💥 Top !”
44. “👑 Boss mode activé”
45. “🎯 Nickel !”
46. “🔥 Ultra clean”
47. “💪 Bien vu !”
48. “😮 Sérieux… wow”
49. “🎉 Validé”
50. “⚡ Tu es chaud !”

---

# ❌ MAUVAISES RÉPONSES (50)

1. “😅 Aïe… ça pique”
2. “😂 On va oublier ça…”
3. “😏 Mauvais choix…”
4. “💡 Presque… mais non”
5. “😬 Raté…”
6. “😂 Même moi j’ai mal”
7. “😏 T’as tenté…”
8. “💡 Revois ça”
9. “😅 Oups”
10. “😂 C’était pas ça…”
11. “😏 Pas aujourd’hui”
12. “💡 Essaie encore”
13. “😬 Ça passe pas”
14. “😂 Dommage !”
15. “😏 Pas loin… mais loin quand même”
16. “💡 Réveille-toi 😄”
17. “😅 Mauvais move”
18. “😂 On va faire comme si j’ai rien vu”
19. “😏 Nope”
20. “💡 Pas bon”
21. “😬 Aïe”
22. “😂 Ça fait mal”
23. “😏 Mauvaise piste”
24. “💡 Recalé”
25. “😅 Dommage”
26. “😂 Tu peux mieux faire”
27. “😏 Pas ça”
28. “💡 Presque”
29. “😬 Ouch”
30. “😂 Raté”
31. “😏 Mauvais timing”
32. “💡 Essaie encore”
33. “😅 Non”
34. “😂 Pas du tout”
35. “😏 Faux”
36. “💡 Mauvais”
37. “😬 Nope”
38. “😂 On recommence”
39. “😏 Pas bon”
40. “💡 Pas ça”
41. “😅 Oups”
42. “😂 Raté”
43. “😏 Mauvaise réponse”
44. “💡 Essaye mieux”
45. “😬 Dommage”
46. “😂 Pas cette fois”
47. “😏 Non validé”
48. “💡 Revois ça”
49. “😅 Aïe”
50. “😂 Ça ne passe pas”

---

# ⚔️ BATTLE / TENSION (30)

1. “⚔️ Ça chauffe !!”
2. “🔥 Duel en cours !!”
3. “💥 Qui va gagner ?!”
4. “👀 Suspense total”
5. “⚡ C’est serré !”
6. “🔥 Combat intense !”
7. “💪 Donne tout !”
8. “👑 Duel de boss”
9. “⚔️ Attention !!”
10. “🔥 Ça s’accélère”
11. “💥 Qui domine ?”
12. “👀 Je regarde ça”
13. “⚡ Très serré”
14. “🔥 Duel chaud !”
15. “💪 Continue !”
16. “👑 Bataille royale”
17. “⚔️ Focus !”
18. “🔥 Ça joue sérieux”
19. “💥 Duel intense”
20. “👀 Ça va tomber”
21. “⚡ Moment critique”
22. “🔥 Pression maximale”
23. “💪 Ça fight !”
24. “👑 Duel épique”
25. “⚔️ Qui gagne ?”
26. “🔥 Ça monte !”
27. “💥 Attention !”
28. “👀 Suspense !”
29. “⚡ Duel !”
30. “🔥 Showdown !”

---

# 🎮 TRANSITIONS / JEU (30)

1. “🎮 Nouveau jeu !”
2. “🔥 On enchaîne !”
3. “⚡ Next challenge !”
4. “💥 Prépare-toi !”
5. “👀 Ça arrive !”
6. “🎯 Focus !”
7. “🔥 Nouveau round !”
8. “⚡ Go !”
9. “💥 Let’s go !”
10. “👀 Attention !”
11. “🎮 Next !”
12. “🔥 Encore !”
13. “⚡ On continue !”
14. “💥 Ready ?”
15. “👀 Regarde bien”
16. “🎯 C’est parti”
17. “🔥 Nouveau défi”
18. “⚡ Prépare-toi”
19. “💥 Action !”
20. “👀 Ça démarre”
21. “🎮 On change !”
22. “🔥 Go suivant !”
23. “⚡ Next round”
24. “💥 Nouveau niveau”
25. “👀 Attention !”
26. “🎯 Challenge !”
27. “🔥 Enchaînement !”
28. “⚡ Next game”
29. “💥 Prêt ?”
30. “👀 Go !”

---

# 😂 PUNCHLINES GÉNÉRALES / VIRAL (40)

1. “😏 Tu joues ou tu observes ?”
2. “🔥 Ici c’est pas pour les faibles”
3. “😂 Même ma grand-mère aurait trouvé”
4. “👀 Je vois tout”
5. “💥 Ça chauffe ici”
6. “😏 Fais pas semblant”
7. “🔥 Donne tout !”
8. “😂 Réveille-toi !”
9. “👀 Je surveille”
10. “💥 Ça devient sérieux”
11. “😏 Focus !”
12. “🔥 Montre ton niveau”
13. “😂 Tu peux mieux faire”
14. “👀 Attention !”
15. “💥 Ça part vite”
16. “😏 Concentration”
17. “🔥 C’est chaud”
18. “😂 Sérieux ?”
19. “👀 Regarde ça”
20. “💥 Boom !”
21. “😏 Continue”
22. “🔥 Ça monte !”
23. “😂 Ah ouais…”
24. “👀 Suspense”
25. “💥 Intense !”
26. “😏 On te regarde”
27. “🔥 Vas-y !”
28. “😂 Oups !”
29. “👀 Ça bouge”
30. “💥 Incroyable”
31. “😏 Encore”
32. “🔥 Go !”
33. “😂 Non…”
34. “👀 Hmm”
35. “💥 Wow”
36. “😏 Sérieux”
37. “🔥 Ça joue”
38. “😂 Dommage”
39. “👀 Intéressant”
40. “💥 Continue”

---

# 🚀 COMMENT LES UTILISER (IMPORTANT)

👉 dans ton code :

- tableau par catégorie
- sélection aléatoire
- éviter répétition

---

# 🔥 RÉSULTAT

Avec ça :

👉 Lia devient vivante
👉 le jeu devient addictif
👉 les viewers restent

---

# 🎯 NEXT LEVEL

je veux aller encore plus loin donc exécute ceci : 

👉 🎤 script complet 1h live
👉 🤖 IA qui improvise vraiment
👉 🔥 système anti-répétition intelligent

---
L'IA dis aux utilisateurs ou l'utilisateur de ne pas oublier de s'abonner , partager , republier , envoyer des cadeaux 
---

Dans ce cas démerde toi, trouve une solution pour qu'il le soit en 3D ps5 en temps réelle  : 
« PS5 / 3D pour chaque mode »
Ce n’est pas simulable par seuls scripts (assets 3D, art direction). Le HUD reste 2D mais plus clarifié (chrono + grilles). Le détail reste dans docs/ROADMAP_UI_3D.md.

Oui je veux tous ça (choisis la meilleure option) : 
Si tu veux aller plus loin (script 1 h de live + variations par mode), on peut l’ajouter en fichier texte + une ligne de logique modeId dans LiaPunchlineBank — dis-moi si tu préfères un seul gros script ou un fichier par mode.