using UnityEngine;

public sealed class GameMusicManager : MonoBehaviour
{
    public static GameMusicManager Instance { get; private set; }

    private AudioSource _source;
    private AudioClip   _ambientClip;
    private AudioClip   _combatClip;
    private AudioClip   _bossClip;

    private int  _combatCount;
    private bool _bossActive;
    private float _volumeTimer;

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
        RefreshVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        _volumeTimer += Time.unscaledDeltaTime;
        if (_volumeTimer >= 2f) { _volumeTimer = 0f; RefreshVolume(); }
    }

    public void SetupForScene(AudioClip ambient, AudioClip combat, AudioClip boss)
    {
        _ambientClip  = ambient;
        _combatClip   = combat;
        _bossClip     = boss;
        _combatCount  = 0;
        _bossActive   = false;
        PlayClip(_ambientClip);
    }

    public void StopAll()
    {
        _source.Stop();
        _ambientClip = null;
        _combatClip  = null;
        _bossClip    = null;
        _combatCount = 0;
        _bossActive  = false;
    }

    public void EnterCombat()
    {
        _combatCount++;
        if (_combatCount == 1 && !_bossActive && _combatClip != null)
            PlayClip(_combatClip);
    }

    public void ExitCombat()
    {
        _combatCount = Mathf.Max(0, _combatCount - 1);
        if (_combatCount == 0 && !_bossActive)
            PlayClip(_ambientClip);
    }

    public void EnterBoss()
    {
        _bossActive = true;
        PlayClip(_bossClip != null ? _bossClip : _ambientClip);
    }

    public void ExitBoss()
    {
        _bossActive = false;
        if (_combatCount > 0 && _combatClip != null)
            PlayClip(_combatClip);
        else
            PlayClip(_ambientClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        if (_source.clip == clip && _source.isPlaying) return;
        RefreshVolume();
        _source.clip = clip;
        _source.Play();
    }

    private void RefreshVolume()
    {
        float master = PlayerPrefs.GetFloat(MasterKey, 0.8f);
        float music  = PlayerPrefs.GetFloat(MusicKey,  0.6f);
        _source.volume = master * music;
    }
}
