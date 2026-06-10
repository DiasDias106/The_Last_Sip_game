using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MenuAudioManager : MonoBehaviour
{
    private const string MasterVolumeKey = "MainMenu.MasterVolume";
    private const string MusicVolumeKey = "MainMenu.MusicVolume";
    private const string SfxVolumeKey = "MainMenu.SfxVolume";

    [Header("Sources")]
    [SerializeField] private AudioSource ambientMusicSource;
    [SerializeField] private AudioSource uiSfxSource;

    [Header("Optional Clips")]
    [SerializeField] private AudioClip ambientMusicClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    [Header("UI")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Defaults")]
    [SerializeField] [Range(0f, 1f)] private float defaultMasterVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float defaultMusicVolume = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float defaultSfxVolume = 0.7f;

    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private float runtimeMusicMultiplier = 1f;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public float DefaultMasterVolume => defaultMasterVolume;
    public float DefaultMusicVolume => defaultMusicVolume;
    public float DefaultSfxVolume => defaultSfxVolume;

    private void Reset()
    {
        EnsureAudioSources();
    }

    private void Awake()
    {
        EnsureAudioSources();
        LoadSavedVolumes();
        ConfigureSources();
        ApplyVolumes();
        BindSliders();
    }

    private void OnDestroy()
    {
        UnbindSliders();
    }

    public void PlayHover()
    {
        PlaySfxClip(hoverClip);
    }

    public void PlayClick()
    {
        PlaySfxClip(clickClip);
    }

    public void SetAmbientMusicClip(AudioClip clip, bool restartIfPlaying = true)
    {
        ambientMusicClip = clip;
        if (ambientMusicSource == null)
        {
            return;
        }

        if (ambientMusicSource.clip == clip && ambientMusicSource.isPlaying)
        {
            if (restartIfPlaying)
            {
                ambientMusicSource.time = 0f;
            }
            return;
        }

        ambientMusicSource.clip = clip;
        if (clip == null)
        {
            ambientMusicSource.Stop();
            return;
        }

        if (restartIfPlaying || !ambientMusicSource.isPlaying)
        {
            ambientMusicSource.Play();
        }
    }

    public void RestartAmbientMusic()
    {
        if (ambientMusicSource == null)
        {
            return;
        }

        if (ambientMusicSource.clip == null)
        {
            ambientMusicSource.clip = ambientMusicClip;
        }

        if (ambientMusicSource.clip == null)
        {
            return;
        }

        ambientMusicSource.time = 0f;
        if (!ambientMusicSource.isPlaying)
        {
            ambientMusicSource.Play();
        }
    }

    public void EnsureAmbientMusicIsPlaying()
    {
        if (ambientMusicSource == null)
        {
            return;
        }

        if (ambientMusicSource.clip == null)
        {
            ambientMusicSource.clip = ambientMusicClip;
        }

        if (ambientMusicSource.clip != null && !ambientMusicSource.isPlaying)
        {
            ambientMusicSource.Play();
        }
    }

    public void SetMasterVolume(float value)
    {
        SetMasterVolume(value, false);
    }

    public void SetMasterVolume(float value, bool save)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();

        if (save)
        {
            SaveCurrentVolumes();
        }
    }

    public void SetMusicVolume(float value)
    {
        SetMusicVolume(value, false);
    }

    public void SetMusicVolume(float value, bool save)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplyVolumes();

        if (save)
        {
            SaveCurrentVolumes();
        }
    }

    public void SetSFXVolume(float value)
    {
        SetSFXVolume(value, false);
    }

    public void SetSFXVolume(float value, bool save)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();

        if (save)
        {
            SaveCurrentVolumes();
        }
    }

    public void SetRuntimeMusicMultiplier(float multiplier)
    {
        runtimeMusicMultiplier = Mathf.Clamp(multiplier, 0f, 1.5f);
        ApplyVolumes();
    }

    public void SaveCurrentVolumes()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    public void ResetToDefaults(bool save)
    {
        masterVolume = defaultMasterVolume;
        musicVolume = defaultMusicVolume;
        sfxVolume = defaultSfxVolume;
        ApplyVolumes();

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(masterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
        }

        if (save)
        {
            SaveCurrentVolumes();
        }
    }

    private void EnsureAudioSources()
    {
        AudioSource[] audioSources = GetComponents<AudioSource>();

        if (ambientMusicSource == null && audioSources.Length > 0)
        {
            ambientMusicSource = audioSources[0];
        }

        if (uiSfxSource == null && audioSources.Length > 1)
        {
            uiSfxSource = audioSources[1];
        }

        if (ambientMusicSource == null)
        {
            ambientMusicSource = gameObject.AddComponent<AudioSource>();
        }

        if (uiSfxSource == null)
        {
            uiSfxSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void LoadSavedVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
    }

    private void ConfigureSources()
    {
        if (ambientMusicSource != null)
        {
            ambientMusicSource.playOnAwake = false;
            ambientMusicSource.loop = true;
            ambientMusicSource.clip = ambientMusicClip;
            ambientMusicSource.ignoreListenerPause = true;
        }

        if (uiSfxSource != null)
        {
            uiSfxSource.playOnAwake = false;
            uiSfxSource.loop = false;
            uiSfxSource.clip = null;
            uiSfxSource.ignoreListenerPause = true;
        }
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = masterVolume;

        if (ambientMusicSource != null)
        {
            ambientMusicSource.volume = musicVolume * runtimeMusicMultiplier;
        }

        if (uiSfxSource != null)
        {
            uiSfxSource.volume = sfxVolume;
        }
    }

    private void BindSliders()
    {
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(masterVolume);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    private void UnbindSliders()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
        }
    }

    private void PlaySfxClip(AudioClip clip)
    {
        if (uiSfxSource == null || clip == null)
        {
            return;
        }

        uiSfxSource.PlayOneShot(clip, 1f);
    }
}
