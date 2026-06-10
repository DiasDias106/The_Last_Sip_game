using UnityEngine;
using System.Collections;

public sealed class GameMusicManager : MonoBehaviour
{
    public static GameMusicManager Instance { get; private set; }

    [SerializeField] private float fadeInDuration  = 2f;
    [SerializeField] private float fadeOutDuration = 1.5f;

    private AudioSource _source;
    private AudioClip   _ambientClip;
    private AudioClip   _combatClip;
    private AudioClip   _bossClip;
    private AudioClip   _targetClip;
    private int  _combatCount;
    private bool _bossActive;
    private Coroutine _fadeRoutine;
    private float _sceneMultiplier = 1f;

    private const string MasterKey = "MainMenu.MasterVolume";
    private const string MusicKey  = "MainMenu.MusicVolume";

    public static GameMusicManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameMusicManager");
        DontDestroyOnLoad(go);
        return go.AddComponent<GameMusicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _source = gameObject.AddComponent<AudioSource>();
        _source.loop = true;
        _source.playOnAwake = false;
        _source.volume = 0f;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void SetupForScene(AudioClip ambient, AudioClip combat, AudioClip boss, float sceneMultiplier = 1f)
    {
        _sceneMultiplier = sceneMultiplier <= 0f ? 1f : sceneMultiplier;
        _ambientClip = ambient;
        _combatClip  = combat;
        _bossClip    = boss;
        _combatCount = 0;
        _bossActive  = false;

        // Pré-carregar todos os clips para evitar stutter quando mudam
        PreloadClip(ambient);
        PreloadClip(combat);
        PreloadClip(boss);

        StartMusicFadeIn(ambient);
    }

    private static void PreloadClip(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
    }

    /// <summary>True quando inimigos estão a perseguir/atacar o player.</summary>
    public bool IsInCombat => _combatCount > 0 || _bossActive;

    public void StopAll(bool instant = false)
    {
        _ambientClip = _combatClip = _bossClip = _targetClip = null;
        _combatCount = 0; _bossActive = false;
        
        if (instant)
        {
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _source.Stop();
            _source.volume = 0f;
            _fadeRoutine = null;
        }
        else
        {
            FadeOutAndStop();
        }
    }

    public void EnterCombat()
    {
        _combatCount++;
        if (_combatCount == 1 && !_bossActive && _combatClip != null)
            CrossFadeTo(_combatClip);
    }

    public void ExitCombat()
    {
        _combatCount = Mathf.Max(0, _combatCount - 1);
        if (_combatCount == 0 && !_bossActive)
            CrossFadeTo(_ambientClip);
    }

    public void EnterBoss()
    {
        _bossActive = true;
        CrossFadeTo(_bossClip != null ? _bossClip : _ambientClip);
    }

    public void ExitBoss()
    {
        _bossActive = false;
        CrossFadeTo(_combatCount > 0 && _combatClip != null ? _combatClip : _ambientClip);
    }

    // ── Privado ───────────────────────────────────────────────────────────────
    private void StartMusicFadeIn(AudioClip clip)
    {
        if (clip == null) return;
        _targetClip = clip;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _source.Stop();
        _source.clip  = clip;
        _source.volume = 0f;
        _source.Play();
        _fadeRoutine = StartCoroutine(FadeVolume(0f, TargetVolume(), fadeInDuration));
    }

    private void CrossFadeTo(AudioClip clip)
    {
        if (clip == null) return;
        if (_targetClip == clip) return;
        
        _targetClip = clip;
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        
        if (_source.clip == clip)
        {
            if (!_source.isPlaying) _source.Play();
            _fadeRoutine = StartCoroutine(FadeVolume(_source.volume, TargetVolume(), fadeInDuration));
        }
        else
        {
            _fadeRoutine = StartCoroutine(CrossFadeRoutine(clip));
        }
    }

    private IEnumerator CrossFadeRoutine(AudioClip newClip)
    {
        // Fade out
        float startVol = _source.volume;
        float elapsed  = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        _source.volume = 0f;
        _source.clip = newClip;
        _source.Play();
        // Fade in
        elapsed = 0f;
        float target = TargetVolume();
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(0f, target, elapsed / fadeInDuration);
            yield return null;
        }
        _source.volume = target;
        _fadeRoutine = null;
    }

    private void FadeOutAndStop()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutStopRoutine());
    }

    private IEnumerator FadeOutStopRoutine()
    {
        float startVol = _source.volume;
        float elapsed  = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        _source.Stop();
        _source.volume = 0f;
        _fadeRoutine = null;
    }

    private IEnumerator FadeVolume(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _source.volume = to;
        _fadeRoutine = null;
    }

    private float TargetVolume()
    {
        float m = PlayerPrefs.GetFloat(MasterKey, 0.8f);
        float t = PlayerPrefs.GetFloat(MusicKey, 0.6f);
        return m * t * _sceneMultiplier;
    }
}
