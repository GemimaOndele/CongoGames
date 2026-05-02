// ============================================================
//  AIHostManager_AudioPatch.cs
//  CongoGames — UnityProject/Assets/Scripts/
//
//  INSTRUCTIONS D'INTÉGRATION :
//  Montre exactement OÙ ajouter les appels DuckForRobot() et
//  RestoreFromRobot() dans ton AIHostManager.cs existant.
//
//  PRINCIPE :
//    Quand le robot parle → BGM baisse à 12% (inaudible sans couper)
//    Quand le robot finit → BGM remonte en douceur (0.6s)
// ============================================================

// ─────────────────────────────────────────────────────────────
//  SECTION 1 — Route TTS HTTP (/tts)
//  Cherche la méthode qui lance la requête vers http://127.0.0.1:3000/tts
// ─────────────────────────────────────────────────────────────

/*

private IEnumerator PlayTTS(string text)
{
    // [AJOUTER ICI] juste AVANT de jouer l'audio TTS :
    GameAudioManager.Instance?.DuckForRobot(fadeDuration: 0.3f);

    // ... ton code TTS existant qui envoie la requête et joue l'audio ...
    // Exemple : UnityWebRequest www = UnityWebRequest.PostWwwForm(ttsUrl, form);
    //           yield return www.SendWebRequest();
    //           ... jouer le clip audio reçu ...

    // [AJOUTER ICI] juste APRÈS que l'audio TTS a fini de jouer :
    GameAudioManager.Instance?.RestoreFromRobot(fadeDuration: 0.6f);
}

*/

// ─────────────────────────────────────────────────────────────
//  SECTION 2 — Si tu utilises une AudioSource dédiée pour le TTS
//  et que tu attends avec WaitUntil ou WaitForSeconds
// ─────────────────────────────────────────────────────────────

/*

private IEnumerator SpeakAndWait(AudioClip ttsClip)
{
    // [AJOUTER ICI] — duck avant de parler
    GameAudioManager.Instance?.DuckForRobot(0.3f);

    _ttsAudioSource.clip = ttsClip;
    _ttsAudioSource.Play();

    // Attendre que l'audio TTS soit terminé
    yield return new WaitWhile(() => _ttsAudioSource.isPlaying);

    // [AJOUTER ICI] — restaurer après avoir parlé
    GameAudioManager.Instance?.RestoreFromRobot(0.6f);
}

*/

// ─────────────────────────────────────────────────────────────
//  SECTION 3 — Si tu utilises ElevenLabs ou OpenAI TTS (fallback)
//  Le principe est identique : duck avant, restore après
// ─────────────────────────────────────────────────────────────

/*

// Avant l'appel API TTS :
GameAudioManager.Instance?.DuckForRobot(0.25f);

// ... appel ElevenLabs / OpenAI / Edge TTS ...

// Après avoir joué le son reçu :
GameAudioManager.Instance?.RestoreFromRobot(0.8f);

*/

// ─────────────────────────────────────────────────────────────
//  SECTION 4 — Cas spécial : Phase de suspense pendant une question
//  Tu peux aussi baisser légèrement la BGM pendant qu'une question
//  s'affiche, puis la remonter à la révélation de la réponse.
// ─────────────────────────────────────────────────────────────

/*

void OnQuestionDisplayed()
{
    // Baisser la BGM à 35% pendant la phase de lecture de la question
    GameAudioManager.Instance?.DuckForRobot(0.4f);
    // (réutilise DuckForRobot car l'effet est le même)
}

void OnAnswerRevealed(bool isCorrect)
{
    // Restaurer la BGM
    GameAudioManager.Instance?.RestoreFromRobot(0.5f);

    // Puis jouer le SFX approprié
    if (isCorrect)
        GameAudioManager.Instance?.OnCorrectAnswer();
    else
        GameAudioManager.Instance?.OnWrongAnswer();
}

*/
