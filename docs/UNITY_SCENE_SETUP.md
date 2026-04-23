# Unity Scene Setup (clic par clic)

## 1. Hierarchie minimale

Creer les GameObjects suivants dans la scene:

- `Managers`
  - `LanguageManager` (+ script `LanguageManager`)
  - `GameModeManager` (+ script `GameModeManager`)
  - `ScoreManager` (+ script `ScoreManager`)
  - `QuestionManager` (+ script `QuestionManager`)
  - `BattleManager` (+ script `BattleManager`)
  - `AIHostManager` (+ script `AIHostManager`)
  - `LiveEventClient` (+ script `LiveEventClient`)
- `UI`
  - `LeaderboardText` (UI Text)
  - `QuestionText` (UI Text)
  - `OptionA` (UI Text)
  - `OptionB` (UI Text)
  - `OptionC` (UI Text)
  - `OptionD` (UI Text)
  - `LeaderboardUI` (+ script `LeaderboardUI`)
  - `QuestionUI` (+ script `QuestionUI`)

## 2. Lier les references dans l'inspector

- `LeaderboardUI.leaderboardText` -> `LeaderboardText`
- `QuestionUI.questionText` -> `QuestionText`
- `QuestionUI.optionAText` -> `OptionA`
- `QuestionUI.optionBText` -> `OptionB`
- `QuestionUI.optionCText` -> `OptionC`
- `QuestionUI.optionDText` -> `OptionD`
- `LiveEventClient.questionUI` -> `QuestionUI`

## 3. Ajouter les modes de jeu

Sur un objet `Modes`, ajouter:

- `QuizMode`
- `SemanticMode`
- `WordScrambleMode`
- `CrosswordLiteMode`
- `BlindTestMode`
- `MysteryWordMode`
- `MemoryMode`
- `SpeedChronoMode`
- `ImageGuessMode`

Le `GameModeManager` detecte automatiquement ces modes dans la scene.

## 3 bis. Scène perso sans RuntimeBootstrap : bandeau « manche terminée »

Si vous n’utilisez pas le canvas généré au lancement par `RuntimeBootstrap`, les **transitions entre modes** n’affichent pas le bandeau *RoundVictoryOverlay* tant qu’il n’est pas dans la scène.

- **Menu Unity** : **CongoGames → UI → Ajouter RoundVictoryOverlay (scène perso, sans RuntimeBootstrap)**  
  Cela crée un GameObject `RoundVictoryOverlay` (plein écran, tri 75) avec le script déjà configuré. Pré-requis : un `GameModeManager` actif en Play, comme dans `UNITY_SCENE_SETUP.md` §1–3.

## 4. Demarrage backend + tests rapides

Dans `Backend/`:

```bash
npm run dev
```

Puis tester:

- `POST http://localhost:3000/question/generate` avec body `{ "language": "fr" }`
- `POST http://localhost:3000/events/chat` avec body `{ "user":"test", "message":"A" }`
- `POST http://localhost:3000/events/gift` avec body `{ "user":"test", "giftName":"Rose" }`

## 5. Note port WebSocket

Si `8080` est occupe, le backend prend automatiquement `8081`, `8082`, etc.
Dans ce cas, mettre la meme URL dans `LiveEventClient.wsUrl`.
