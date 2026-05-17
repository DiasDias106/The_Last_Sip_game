using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class OptionsPanelView : MonoBehaviour
{
    private struct SettingsSnapshot
    {
        public float master;
        public float music;
        public float sfx;
        public int audioMode;
        public int subtitles;
        public int language;
    }

    private const string AudioModeKey = "MainMenu.AudioMode";
    private const string SubtitlesKey = "MainMenu.Subtitles";
    private const string LanguageKey = "MainMenu.Language";

    [Header("Core")]
    [SerializeField] private MenuAudioManager menuAudioManager;
    [SerializeField] private TMP_Text sectionTitleText;

    [Header("Tabs")]
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Image[] tabBackgrounds;
    [SerializeField] private TMP_Text[] tabLabels;
    [SerializeField] private GameObject[] selectedMarkers;
    [SerializeField] private GameObject[] sections;
    [SerializeField] private string[] sectionTitles;
    [SerializeField] private Sprite activeTabSprite;
    [SerializeField] private Sprite inactiveTabSprite;

    [Header("Audio Controls")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private TMP_Text sfxValueText;

    [Header("Selectors")]
    [SerializeField] private Button audioModeButton;
    [SerializeField] private TMP_Text audioModeValueText;
    [SerializeField] private Button subtitlesButton;
    [SerializeField] private TMP_Text subtitlesValueText;
    [SerializeField] private Button languageButton;
    [SerializeField] private TMP_Text languageValueText;

    [Header("Actions")]
    [SerializeField] private AnimatedMenuButton applyButton;
    [SerializeField] private AnimatedMenuButton resetDefaultsButton;

    private readonly string[] audioModes = { "ESTEREO", "MONO" };
    private readonly string[] subtitlesModes = { "LIGADO", "DESLIGADO" };
    private readonly string[] languageModes = { "PORTUGUÊS", "ENGLISH" };

    private readonly Color activeTabColor = new(0.3f, 0.08f, 0.08f, 0.95f);
    private readonly Color hoverTabColor = new(0.2f, 0.11f, 0.1f, 0.94f);
    private readonly Color inactiveTabColor = new(0.11f, 0.085f, 0.08f, 0.92f);
    private readonly Color activeLabelColor = new(0.95f, 0.89f, 0.76f, 1f);
    private readonly Color hoverLabelColor = new(0.88f, 0.8f, 0.67f, 1f);
    private readonly Color inactiveLabelColor = new(0.66f, 0.61f, 0.51f, 1f);

    private int audioModeIndex;
    private int subtitlesIndex;
    private int languageIndex;
    private int selectedSectionIndex = 2;
    private int hoveredTabIndex = -1;
    private bool hoverEventsBound;
    private SettingsSnapshot appliedSnapshot;

    private void Awake()
    {
        LoadInitialState();
        SelectSection(selectedSectionIndex);
    }

    private void OnEnable()
    {
        Bind();
        RefreshAllLabels();
        SelectSection(selectedSectionIndex);
    }

    private void OnDisable()
    {
        Unbind();
    }

    public void ApplySettings()
    {
        menuAudioManager?.SaveCurrentVolumes();
        PlayerPrefs.SetInt(AudioModeKey, audioModeIndex);
        PlayerPrefs.SetInt(SubtitlesKey, subtitlesIndex);
        PlayerPrefs.SetInt(LanguageKey, languageIndex);
        PlayerPrefs.Save();
        appliedSnapshot = CaptureCurrentSnapshot();
    }

    public void ResetDefaults()
    {
        if (menuAudioManager != null)
        {
            menuAudioManager.ResetToDefaults(false);
            SetSliderWithoutNotify(masterSlider, menuAudioManager.DefaultMasterVolume);
            SetSliderWithoutNotify(musicSlider, menuAudioManager.DefaultMusicVolume);
            SetSliderWithoutNotify(sfxSlider, menuAudioManager.DefaultSfxVolume);
        }
        else
        {
            SetSliderWithoutNotify(masterSlider, 0.8f);
            SetSliderWithoutNotify(musicSlider, 0.6f);
            SetSliderWithoutNotify(sfxSlider, 0.7f);
        }

        audioModeIndex = 0;
        subtitlesIndex = 0;
        languageIndex = 0;
        RefreshAllLabels();
    }

    public bool HasUnsavedChanges()
    {
        SettingsSnapshot current = CaptureCurrentSnapshot();
        return
            Mathf.Abs(current.master - appliedSnapshot.master) > 0.0001f ||
            Mathf.Abs(current.music - appliedSnapshot.music) > 0.0001f ||
            Mathf.Abs(current.sfx - appliedSnapshot.sfx) > 0.0001f ||
            current.audioMode != appliedSnapshot.audioMode ||
            current.subtitles != appliedSnapshot.subtitles ||
            current.language != appliedSnapshot.language;
    }

    public void RevertToAppliedSettings()
    {
        ApplySnapshot(appliedSnapshot, true);
    }

    private void Bind()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            if (tabButtons[i] != null)
            {
                tabButtons[i].transition = Selectable.Transition.None;
                tabButtons[i].onClick.AddListener(() => SelectSection(index));
                if (!hoverEventsBound)
                {
                    EnsureTabHoverEvents(tabButtons[i], index);
                }
            }
        }
        hoverEventsBound = true;

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(HandleMasterChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        }

        if (audioModeButton != null)
        {
            audioModeButton.onClick.AddListener(CycleAudioMode);
        }

        if (subtitlesButton != null)
        {
            subtitlesButton.onClick.AddListener(CycleSubtitles);
        }

        if (languageButton != null)
        {
            languageButton.onClick.AddListener(CycleLanguage);
        }

        if (applyButton != null)
        {
            applyButton.Clicked += ApplySettings;
        }

        if (resetDefaultsButton != null)
        {
            resetDefaultsButton.Clicked += ResetDefaults;
        }
    }

    private void Unbind()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null)
            {
                tabButtons[i].onClick.RemoveAllListeners();
            }
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        }

        if (audioModeButton != null)
        {
            audioModeButton.onClick.RemoveListener(CycleAudioMode);
        }

        if (subtitlesButton != null)
        {
            subtitlesButton.onClick.RemoveListener(CycleSubtitles);
        }

        if (languageButton != null)
        {
            languageButton.onClick.RemoveListener(CycleLanguage);
        }

        if (applyButton != null)
        {
            applyButton.Clicked -= ApplySettings;
        }

        if (resetDefaultsButton != null)
        {
            resetDefaultsButton.Clicked -= ResetDefaults;
        }
    }

    private void LoadInitialState()
    {
        if (menuAudioManager != null)
        {
            SetSliderWithoutNotify(masterSlider, menuAudioManager.MasterVolume);
            SetSliderWithoutNotify(musicSlider, menuAudioManager.MusicVolume);
            SetSliderWithoutNotify(sfxSlider, menuAudioManager.SfxVolume);
        }

        audioModeIndex = Mathf.Clamp(PlayerPrefs.GetInt(AudioModeKey, 0), 0, audioModes.Length - 1);
        subtitlesIndex = Mathf.Clamp(PlayerPrefs.GetInt(SubtitlesKey, 0), 0, subtitlesModes.Length - 1);
        languageIndex = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, languageModes.Length - 1);
        RefreshAllLabels();
        appliedSnapshot = CaptureCurrentSnapshot();
    }

    private void HandleMasterChanged(float value)
    {
        menuAudioManager?.SetMasterVolume(value);
        RefreshSliderLabels();
    }

    private void HandleMusicChanged(float value)
    {
        menuAudioManager?.SetMusicVolume(value);
        RefreshSliderLabels();
    }

    private void HandleSfxChanged(float value)
    {
        menuAudioManager?.SetSFXVolume(value);
        RefreshSliderLabels();
    }

    private void CycleAudioMode()
    {
        audioModeIndex = (audioModeIndex + 1) % audioModes.Length;
        RefreshSelectorLabels();
    }

    private void CycleSubtitles()
    {
        subtitlesIndex = (subtitlesIndex + 1) % subtitlesModes.Length;
        RefreshSelectorLabels();
    }

    private void CycleLanguage()
    {
        languageIndex = (languageIndex + 1) % languageModes.Length;
        RefreshSelectorLabels();
    }

    private void SelectSection(int index)
    {
        selectedSectionIndex = Mathf.Clamp(index, 0, sections.Length - 1);
        ApplyTabVisuals();

        for (int i = 0; i < sections.Length; i++)
        {
            bool active = i == selectedSectionIndex;

            if (sections[i] != null)
            {
                sections[i].SetActive(active);
            }
        }

        if (sectionTitleText != null && selectedSectionIndex < sectionTitles.Length)
        {
            sectionTitleText.text = sectionTitles[selectedSectionIndex];
        }
    }

    private void ApplyTabVisuals()
    {
        int count = Mathf.Max(
            tabButtons != null ? tabButtons.Length : 0,
            tabBackgrounds != null ? tabBackgrounds.Length : 0,
            tabLabels != null ? tabLabels.Length : 0,
            selectedMarkers != null ? selectedMarkers.Length : 0);

        for (int i = 0; i < count; i++)
        {
            bool active = i == selectedSectionIndex;
            bool hovered = i == hoveredTabIndex && !active;

            if (i < tabBackgrounds.Length && tabBackgrounds[i] != null)
            {
                tabBackgrounds[i].sprite = active ? activeTabSprite : inactiveTabSprite;
                tabBackgrounds[i].color = active ? activeTabColor : hovered ? hoverTabColor : inactiveTabColor;
            }

            if (i < tabLabels.Length && tabLabels[i] != null)
            {
                tabLabels[i].color = active ? activeLabelColor : hovered ? hoverLabelColor : inactiveLabelColor;
            }

            if (i < selectedMarkers.Length && selectedMarkers[i] != null)
            {
                selectedMarkers[i].SetActive(active);
            }
        }
    }

    private void EnsureTabHoverEvents(Button button, int index)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
        }

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            hoveredTabIndex = index;
            ApplyTabVisuals();
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            if (hoveredTabIndex == index)
            {
                hoveredTabIndex = -1;
                ApplyTabVisuals();
            }
        });
        trigger.triggers.Add(exit);
    }

    private void RefreshAllLabels()
    {
        RefreshSliderLabels();
        RefreshSelectorLabels();
    }

    private void RefreshSliderLabels()
    {
        SetPercentLabel(masterValueText, masterSlider != null ? masterSlider.value : 0f);
        SetPercentLabel(musicValueText, musicSlider != null ? musicSlider.value : 0f);
        SetPercentLabel(sfxValueText, sfxSlider != null ? sfxSlider.value : 0f);
    }

    private void RefreshSelectorLabels()
    {
        if (audioModeValueText != null)
        {
            audioModeValueText.text = audioModes[audioModeIndex];
        }

        if (subtitlesValueText != null)
        {
            subtitlesValueText.text = subtitlesModes[subtitlesIndex];
        }

        if (languageValueText != null)
        {
            languageValueText.text = languageModes[languageIndex];
        }
    }

    private static void SetPercentLabel(TMP_Text label, float value)
    {
        if (label != null)
        {
            label.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }
    }

    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }

    private SettingsSnapshot CaptureCurrentSnapshot()
    {
        return new SettingsSnapshot
        {
            master = masterSlider != null ? masterSlider.value : 0f,
            music = musicSlider != null ? musicSlider.value : 0f,
            sfx = sfxSlider != null ? sfxSlider.value : 0f,
            audioMode = audioModeIndex,
            subtitles = subtitlesIndex,
            language = languageIndex
        };
    }

    private void ApplySnapshot(SettingsSnapshot snapshot, bool applyToAudioManager)
    {
        SetSliderWithoutNotify(masterSlider, snapshot.master);
        SetSliderWithoutNotify(musicSlider, snapshot.music);
        SetSliderWithoutNotify(sfxSlider, snapshot.sfx);

        if (applyToAudioManager && menuAudioManager != null)
        {
            menuAudioManager.SetMasterVolume(snapshot.master);
            menuAudioManager.SetMusicVolume(snapshot.music);
            menuAudioManager.SetSFXVolume(snapshot.sfx);
        }

        audioModeIndex = Mathf.Clamp(snapshot.audioMode, 0, audioModes.Length - 1);
        subtitlesIndex = Mathf.Clamp(snapshot.subtitles, 0, subtitlesModes.Length - 1);
        languageIndex = Mathf.Clamp(snapshot.language, 0, languageModes.Length - 1);
        RefreshAllLabels();
    }
}
