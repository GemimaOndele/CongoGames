// ============================================================
//  LiveEventClient_AudioPatch.cs
//  CongoGames — UnityProject/Assets/Scripts/
//
//  INSTRUCTIONS D'INTÉGRATION :
//  Ce fichier montre exactement QUOI et OÙ ajouter dans ton
//  LiveEventClient.cs existant.
//  NE PAS copier ce fichier tel quel — suis les sections marquées
//  [AJOUTER ICI] dans ton fichier original.
//
//  Pour chaque type d'événement TikTok reçu via WebSocket,
//  on appelle la méthode correspondante du GameAudioManager.
// ============================================================

// ─────────────────────────────────────────────────────────────
//  SECTION 1 — Dans la méthode qui reçoit les messages WebSocket
//  (cherche "OnMessage", "HandleMessage", "ParseEvent" ou similaire)
// ─────────────────────────────────────────────────────────────

/*

// [AJOUTER ICI] après avoir parsé le type d'événement :

switch (eventType)          // ou if/else selon ton code actuel
{
    // ── Événements de jeu ──────────────────────────────────
    case "quiz":
    case "quiz_start":
        GameAudioManager.Instance?.OnQuizStart();
        break;

    case "battle":
    case "battle_start":
    case "battle_trigger":
        GameAudioManager.Instance?.OnBattleStart();
        break;

    case "speed_chrono":
    case "speed":
        GameAudioManager.Instance?.OnSpeedChronoStart();
        break;

    case "memory":
    case "memory_start":
        GameAudioManager.Instance?.OnMemoryStart();
        break;

    case "word_scramble":
        GameAudioManager.Instance?.OnWordScrambleStart();
        break;

    case "crossword":
        GameAudioManager.Instance?.OnCrosswordStart();
        break;

    case "mystery_word":
        GameAudioManager.Instance?.OnMysteryWordStart();
        break;

    case "semantic":
    case "semantic_challenge":
        GameAudioManager.Instance?.OnSemanticStart();
        break;

    case "image_guess":
    case "image_to_word":
        GameAudioManager.Instance?.OnImageToWordStart();
        break;

    // ── Événements TikTok Live ─────────────────────────────
    case "correct":
    case "correct_answer":
        GameAudioManager.Instance?.OnCorrectAnswer();
        break;

    case "wrong":
    case "wrong_answer":
        GameAudioManager.Instance?.OnWrongAnswer();
        break;

    case "gift":
    case "gift_received":
        GameAudioManager.Instance?.OnGiftReceived();
        break;

    case "viewer":
    case "new_viewer":
    case "join":
        GameAudioManager.Instance?.OnNewViewer();
        break;

    case "round_win":
    case "winner":
        GameAudioManager.Instance?.OnRoundWin();
        break;

    case "timer_tick":
        GameAudioManager.Instance?.OnTimerTick();
        break;

    case "timer_urgent":
    case "timer_warning":
        GameAudioManager.Instance?.OnTimerUrgent();
        break;

    case "lobby":
    case "round_reset":
    case "game_end":
        GameAudioManager.Instance?.OnLobby();
        break;
}

*/

// ─────────────────────────────────────────────────────────────
//  SECTION 2 — Si tu utilises des events/delegates Unity
//  (OnRoundReset, OnGameModeChanged, etc.) plutôt qu'un switch
// ─────────────────────────────────────────────────────────────

/*

// [AJOUTER ICI] dans OnEnable() ou là où tu abonnes tes handlers :

// Exemple si tu as des UnityEvents ou C# events :
RoundManager.OnRoundReset    += () => GameAudioManager.Instance?.OnLobby();
RoundManager.OnRoundWin      += () => GameAudioManager.Instance?.OnRoundWin();
GameModeManager.OnModeChanged += (mode) =>
{
    switch (mode)
    {
        case GameMode.Quiz:          GameAudioManager.Instance?.OnQuizStart();         break;
        case GameMode.Battle:        GameAudioManager.Instance?.OnBattleStart();       break;
        case GameMode.SpeedChrono:   GameAudioManager.Instance?.OnSpeedChronoStart();  break;
        case GameMode.Memory:        GameAudioManager.Instance?.OnMemoryStart();        break;
        case GameMode.WordScramble:  GameAudioManager.Instance?.OnWordScrambleStart(); break;
        case GameMode.Crossword:     GameAudioManager.Instance?.OnCrosswordStart();    break;
        case GameMode.MysteryWord:   GameAudioManager.Instance?.OnMysteryWordStart();  break;
        case GameMode.Semantic:      GameAudioManager.Instance?.OnSemanticStart();      break;
        case GameMode.ImageToWord:   GameAudioManager.Instance?.OnImageToWordStart();  break;
        case GameMode.Lobby:         GameAudioManager.Instance?.OnLobby();             break;
    }
};

// [AJOUTER ICI] dans OnDisable() pour se désabonner :
RoundManager.OnRoundReset    -= ...
RoundManager.OnRoundWin      -= ...
GameModeManager.OnModeChanged -= ...

*/
