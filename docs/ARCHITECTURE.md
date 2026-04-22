# Architecture CongoGames

## Vue globale

- Unity gere le rendu 3D, UI et gameplay live
- Node.js gere les evenements TikTok, gifts, questions IA
- WebSocket pousse les evenements temps reel vers Unity

## Modules Unity

- `Core/GameModeManager` : enchainement des modes
- `Core/ScoreManager` : points, streak, top joueurs
- `Core/LanguageManager` : FR/Lingala/Kituba
- `Core/BattleManager` : duels top 2 ou gift-triggered
- `AI/AIHostManager` : voix, reactions, scripts host
- `Perf/*` : pooling, cache audio

## Modules Backend

- `services/questionGenerator` : generation IA
- `services/giftEngine` : economie gifts + anti-abus
- `protocol/messages` : schema messages WS
- `server` : API HTTP + WebSocket hub
