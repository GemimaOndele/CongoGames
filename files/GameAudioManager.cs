// ============================================================
//  GameAudioManager.cs
//  CongoGames — UnityProject/Assets/Scripts/Audio/
//
//  RÔLE :
//    • Joue et change les BGM de chaque mini-jeu avec cross-fade.
//    • Duck automatique quand le robot IA parle (TTS).
//    • Joue les SFX (bonne réponse, cadeau TikTok, victoire…).
//
//  SETUP UNITY :
//    1. Attacher ce script sur un GameObject "CongoGames_Audio"
//       (ou sur CongoGames_Services s'il existe déjà).
//    2. Glisser les AudioClip depuis Assets/Audio/ dans l'Inspecteur.
//    3. Les autres scripts appellent : GameAudioManager.Instance.OnQuizStart() etc.
// ============================================================
using System.Collections;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameAudioManager Instance { get; private set; }

    // ── BGM — un clip par mini-jeu (Assets/Audio/BGM/) ──────────────────────
    [Header("BGM Mini-jeux — glisser depuis Assets/Audio/BGM/")]
    public AudioClip lobbyTheme;           // Afrobeat djembé — accueil TikTok
    public AudioClip quizTheme;            // Suspense loop
    public AudioClip battleTheme;          // Battle Theme A — combat épique
    public AudioClip speedChronoTheme;     // EDM Fast — urgence
    public AudioClip memoryTheme;          // Calm Relaxing — zen
    public AudioClip wordScrambleTheme;    // Puzzle Loop — légèreté
    public AudioClip crosswordTheme;       // Chiptune Calm — intellect
    public AudioClip mysteryWordTheme;     // Infiltration — mystère
    public AudioClip semanticTheme;        // Soft Ambient — réflexion
    public AudioClip imageToWordTheme;     // Happy Ukelele — joyeux

    // ── SFX — événements TikTok Live (Assets/Audio/SFX/) ────────────────────
    [Header("SFX Événements Live — glisser depuis Assets/Audio/SFX/")]
    public AudioClip correctAnswer;
    public AudioClip wrongAnswer;
    public AudioClip giftReceived;
    public AudioClip newViewer;
    public AudioClip battleStartSFX;
    public AudioClip roundWin;
    public AudioClip timerTick;
    public AudioClip timerUrgent;
    public AudioClip crowdCheer;

    // ── Volumes ──────────────────────────────────────────────────────────────
    [Header("Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    [Range(0f, 1f)] public float duckVolume = 0.12f; // volume pendant la voix robot

    // ── Durées de fade par mini-jeu (secondes) ───────────────────────────────
    private const float FADE_LOBBY    = 3.0f;
    private const float FADE_QUIZ     = 2.0f;
    private const float FADE_BATTLE   = 0.8f;  // court = tension
    private const float FADE_SPEED    = 0.5f;  // quasi-immédiat
    private const float FADE_MEMORY   = 2.5f;  // très doux
    private const float FADE_SCRAMBLE = 1.5f;
    private const float FADE_CROSS    = 2.0f;
    private const float FADE_MYSTERY  = 1.8f;
    private const float FADE_SEMANTIC = 2.0f;
    private const float FADE_IMAGE    = 1.5f;

    // ── Sources audio internes (cross-fade A/B) ───────────────────────────────
    private AudioSource _bgmA;
    private AudioSource _bgmB;
    private AudioSource _sfxSrc;
    private bool        _usingA = true;
    private Coroutine   _currentFade;

    // =========================================================================
    //  LIFECYCLE
    // =========================================================================
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _bgmA   = CreateSource(bgmVolume, loop: true);
        _bgmB   = CreateSource(0f,        loop: true);
        _sfxSrc = CreateSource(sfxVolume, loop: false);
    }

    void Start()
    {
        // Lance le lobby dès le démarrage
        OnLobby();
    }

    private AudioSource CreateSource(float vol, bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.volume = vol;
        s.loop   = loop;
        s.playOnAwake = false;
        return s;
    }

    // =========================================================================
    //  API PUBLIQUE — MINI-JEUX
    // =========================================================================
    public void OnLobby()            => PlayBGM(lobbyTheme,        FADE_LOBBY);
    public void OnQuizStart()        => PlayBGM(quizTheme,         FADE_QUIZ);
    public void OnBattleStart()      { PlayBGM(battleTheme, FADE_BATTLE); PlaySFX(battleStartSFX); }
    public void OnSpeedChronoStart() => PlayBGM(speedChronoTheme,  FADE_SPEED);
    public void OnMemoryStart()      => PlayBGM(memoryTheme,        FADE_MEMORY);
    public void OnWordScrambleStart()=> PlayBGM(wordScrambleTheme, FADE_SCRAMBLE);
    public void OnCrosswordStart()   => PlayBGM(crosswordTheme,    FADE_CROSS);
    public void OnMysteryWordStart() => PlayBGM(mysteryWordTheme,   FADE_MYSTERY);
    public void OnSemanticStart()    => PlayBGM(semanticTheme,      FADE_SEMANTIC);
    public void OnImageToWordStart() => PlayBGM(imageToWordTheme,   FADE_IMAGE);

    // ── Mini-jeux gérés par playlist/ → pas de BGM ici (Blind Test, Guess Image)
    // public void OnBlindTestStart()  → géré par BlindTestManager avec playlist/
    // public void OnGuessImageStart() → géré par GuessImageManager avec playlist/

    // =========================================================================
    //  API PUBLIQUE — ÉVÉNEMENTS TIKTOK LIVE
    // =========================================================================
    public void OnCorrectAnswer() { PlaySFX(correctAnswer); PlaySFX(crowdCheer); }
    public void OnWrongAnswer()   => PlaySFX(wrongAnswer);
    public void OnGiftReceived()  => PlaySFX(giftReceived);
    public void OnNewViewer()     => PlaySFX(newViewer);
    public void OnRoundWin()      { PlaySFX(roundWin); StopBGM(1.5f); }
    public void OnTimerTick()     => PlaySFX(timerTick);
    public void OnTimerUrgent()   => PlaySFX(timerUrgent);

    // =========================================================================
    //  API PUBLIQUE — DUCK (pour la voix robot IA)
    // =========================================================================
    /// <summary>Baisser la BGM pendant que le robot IA parle (TTS)</summary>
    public void DuckForRobot(float fadeDuration = 0.3f)
        => FadeBGMTo(duckVolume, fadeDuration);

    /// <summary>Restaurer la BGM après la voix robot</summary>
    public void RestoreFromRobot(float fadeDuration = 0.6f)
        => FadeBGMTo(bgmVolume, fadeDuration);

    // =========================================================================
    //  CORE — CROSS-FADE
    // =========================================================================
    public void PlayBGM(AudioClip clip, float fadeDuration = 1.5f)
    {
        if (clip == null) return;
        AudioSource current = _usingA ? _bgmA : _bgmB;
        if (current.clip == clip && current.isPlaying) return;

        if (_currentFade != null) StopCoroutine(_currentFade);
        _currentFade = StartCoroutine(CrossFade(clip, fadeDuration));
    }

    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (_currentFade != null) StopCoroutine(_currentFade);
        _currentFade = StartCoroutine(FadeOutActive(fadeDuration));
    }

    private void FadeBGMTo(float targetVol, float duration)
    {
        if (_currentFade != null) StopCoroutine(_currentFade);
        _currentFade = StartCoroutine(FadeVolumeTo(targetVol, duration));
    }

    // ── Coroutines internes ──────────────────────────────────────────────────
    private IEnumerator CrossFade(AudioClip nextClip, float duration)
    {
        AudioSource outSrc = _usingA ? _bgmA : _bgmB;
        AudioSource inSrc  = _usingA ? _bgmB : _bgmA;

        inSrc.clip   = nextClip;
        inSrc.volume = 0f;
        inSrc.Play();

        float startVol = outSrc.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float ratio  = Mathf.SmoothStep(0f, 1f, t / duration); // courbe douce
            outSrc.volume = Mathf.Lerp(startVol, 0f, ratio);
            inSrc.volume  = Mathf.Lerp(0f, bgmVolume, ratio);
            yield return null;
        }

        outSrc.Stop();
        outSrc.clip   = null;
        outSrc.volume = 0f;
        inSrc.volume  = bgmVolume;
        _usingA       = !_usingA;
        _currentFade  = null;
    }

    private IEnumerator FadeOutActive(float duration)
    {
        AudioSource active = _usingA ? _bgmA : _bgmB;
        float start = active.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            active.volume = Mathf.Lerp(start, 0f, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        active.Stop();
        _currentFade = null;
    }

    private IEnumerator FadeVolumeTo(float target, float duration)
    {
        AudioSource active = _usingA ? _bgmA : _bgmB;
        float start = active.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            active.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        active.volume = target;
        _currentFade  = null;
    }

    // ── SFX ─────────────────────────────────────────────────────────────────
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) _sfxSrc.PlayOneShot(clip, sfxVolume);
    }
}
