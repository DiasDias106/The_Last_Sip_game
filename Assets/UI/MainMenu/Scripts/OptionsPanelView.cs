using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
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
#if ENABLE_INPUT_SYSTEM
        public Dictionary<MovementKeyBindings.GameAction, Key> controlKeys;
#endif
    }
    private struct ControlBindingDefinition
    {
        public ControlBindingDefinition(MovementKeyBindings.GameAction action, string rowName, string label)
        {
            Action = action;
            RowName = rowName;
            Label = label;
        }
        public MovementKeyBindings.GameAction Action { get; }
        public string RowName { get; }
        public string Label { get; }
    }
    private sealed class ControlBindingWidgets
    {
        public RectTransform rowRect;
        public TMP_Text labelText;
        public Button button;
        public TMP_Text valueText;
    }
    private const string AudioModeKey = "MainMenu.AudioMode";
    private const string SubtitlesKey = "MainMenu.Subtitles";
    private const string LanguageKey = "MainMenu.Language";
    private const float ControlsRowHeight = 60f;
    private const float ControlsContentWidth = 780f;
    private static readonly (string RowName, string Label, string Value)[] SupplementalControlRows =
    {
        ("SmallPotionRow", "POÇÃO DE CURA PEQUENA", "Q"),
        ("BigPotionRow", "POÇÃO DE CURA GRANDE", "E")
    };
    private static readonly ControlBindingDefinition[] BindingDefinitions =
    {
        new(MovementKeyBindings.GameAction.Forward, "ForwardRow", "ANDAR PARA A FRENTE"),
        new(MovementKeyBindings.GameAction.Left, "LeftRow", "ANDAR PARA A ESQUERDA"),
        new(MovementKeyBindings.GameAction.Backward, "BackRow", "ANDAR PARA TRAS"),
        new(MovementKeyBindings.GameAction.Right, "RightRow", "ANDAR PARA A DIREITA"),
        new(MovementKeyBindings.GameAction.Run, "RunRow", "CORRER"),
        new(MovementKeyBindings.GameAction.Interact, "InteractRow", "INTERAGIR"),
        new(MovementKeyBindings.GameAction.Attack, "AttackRow", "ATAQUE"),
        new(MovementKeyBindings.GameAction.HeavyAttack, "HeavyAttackRow", "ATAQUE PESADO"),
        new(MovementKeyBindings.GameAction.Defend, "DefendRow", "DEFENDER"),
        new(MovementKeyBindings.GameAction.Jump, "JumpRow", "SALTAR"),
        new(MovementKeyBindings.GameAction.Roll, "RollRow", "REBOLAR")
    };
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
    private readonly Color activeTabColor = new(0.3f, 0.08f, 0.08f, 0.95f);
    private readonly Color hoverTabColor = new(0.2f, 0.11f, 0.1f, 0.94f);
    private readonly Color inactiveTabColor = new(0.11f, 0.085f, 0.08f, 0.92f);
    private readonly Color activeLabelColor = new(0.95f, 0.89f, 0.76f, 1f);
    private readonly Color hoverLabelColor = new(0.88f, 0.8f, 0.67f, 1f);
    private readonly Color inactiveLabelColor = new(0.66f, 0.61f, 0.51f, 1f);
    private readonly Dictionary<MovementKeyBindings.GameAction, ControlBindingWidgets> bindingWidgets = new();
#if ENABLE_INPUT_SYSTEM
    private readonly Dictionary<MovementKeyBindings.GameAction, Key> pendingBindings = new();
    private readonly Dictionary<MovementKeyBindings.GameAction, UnityAction> bindingClickHandlers = new();
    private MovementKeyBindings.GameAction? listeningAction;
#endif
    private Transform controlsSectionRoot;
    private RectTransform controlsContentRoot;
    private int audioModeIndex;
    private int subtitlesIndex;
    private int languageIndex;
    private int selectedSectionIndex = 2;
    private int hoveredTabIndex = -1;
    private bool hoverEventsBound;
    private SettingsSnapshot appliedSnapshot;
    private void Awake()
    {
        ResolveControlBindingReferences();
        LoadInitialState();
        ApplyLocalizedStaticLabels();
        SelectSection(selectedSectionIndex);
    }
    private void OnEnable()
    {
        Bind();
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }
        RefreshAllLabels();
        SelectSection(selectedSectionIndex);
    }
    private void OnDisable()
    {
        Unbind();
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
#if ENABLE_INPUT_SYSTEM
        listeningAction = null;
#endif
    }
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (!listeningAction.HasValue)
        {
            return;
        }
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            listeningAction = null;
            RefreshBindingLabels();
            return;
        }
        foreach (KeyControl keyControl in keyboard.allKeys)
        {
            if (!keyControl.wasPressedThisFrame)
            {
                continue;
            }
            ApplyPendingBinding(listeningAction.Value, keyControl.keyCode);
            listeningAction = null;
            RefreshBindingLabels();
            break;
        }
#endif
    }
    public void ApplySettings()
    {
        menuAudioManager?.SaveCurrentVolumes();
        PlayerPrefs.SetInt(AudioModeKey, audioModeIndex);
        PlayerPrefs.SetInt(SubtitlesKey, subtitlesIndex);
        PlayerPrefs.SetInt(LanguageKey, languageIndex);
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(UiText.LanguageFromIndex(languageIndex));
        }
#if ENABLE_INPUT_SYSTEM
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            MovementKeyBindings.SetKey(definition.Action, GetPendingKey(definition.Action));
        }
        MovementKeyBindings.Save();
        SyncPendingBindingsFromRuntime();
        RefreshBindingLabels();
#endif
        PlayerPrefs.Save();
        appliedSnapshot = CaptureCurrentSnapshot();
        RuntimeTextLocalizer.RequestRefresh();
    }
    public void ResetDefaults()
    {
        if (selectedSectionIndex == 2)
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
        }
        else if (selectedSectionIndex == 4)
        {
            languageIndex = 0;
        }
        else if (selectedSectionIndex == 3)
        {
#if ENABLE_INPUT_SYSTEM
            ResetPendingBindingsToDefaults();
#endif
        }
        ApplyLocalizedStaticLabels();
        RefreshAllLabels();
    }
    public bool HasUnsavedChanges()
    {
        SettingsSnapshot current = CaptureCurrentSnapshot();
        bool hasBaseChanges =
            Mathf.Abs(current.master - appliedSnapshot.master) > 0.0001f ||
            Mathf.Abs(current.music - appliedSnapshot.music) > 0.0001f ||
            Mathf.Abs(current.sfx - appliedSnapshot.sfx) > 0.0001f ||
            current.audioMode != appliedSnapshot.audioMode ||
            current.subtitles != appliedSnapshot.subtitles ||
            current.language != appliedSnapshot.language;
#if ENABLE_INPUT_SYSTEM
        return hasBaseChanges || !BindingDictionariesMatch(current.controlKeys, appliedSnapshot.controlKeys);
#else
        return hasBaseChanges;
#endif
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
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(HandleMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(HandleMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(HandleSfxChanged);
        if (audioModeButton != null) audioModeButton.onClick.AddListener(CycleAudioMode);
        if (subtitlesButton != null) subtitlesButton.onClick.AddListener(CycleSubtitles);
        if (languageButton != null) languageButton.onClick.AddListener(CycleLanguage);
#if ENABLE_INPUT_SYSTEM
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            if (!bindingWidgets.TryGetValue(definition.Action, out ControlBindingWidgets widgets) || widgets.button == null)
            {
                continue;
            }
            UnityAction action = () => BeginListeningForBinding(definition.Action);
            bindingClickHandlers[definition.Action] = action;
            widgets.button.onClick.AddListener(action);
        }
#endif
        if (applyButton != null) applyButton.Clicked += ApplySettings;
        if (resetDefaultsButton != null) resetDefaultsButton.Clicked += ResetDefaults;
    }
    private void Unbind()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] != null) tabButtons[i].onClick.RemoveAllListeners();
        }
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(HandleMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(HandleMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(HandleSfxChanged);
        if (audioModeButton != null) audioModeButton.onClick.RemoveListener(CycleAudioMode);
        if (subtitlesButton != null) subtitlesButton.onClick.RemoveListener(CycleSubtitles);
        if (languageButton != null) languageButton.onClick.RemoveListener(CycleLanguage);
#if ENABLE_INPUT_SYSTEM
        foreach (KeyValuePair<MovementKeyBindings.GameAction, UnityAction> pair in bindingClickHandlers)
        {
            if (bindingWidgets.TryGetValue(pair.Key, out ControlBindingWidgets widgets) && widgets.button != null)
            {
                widgets.button.onClick.RemoveListener(pair.Value);
            }
        }
        bindingClickHandlers.Clear();
#endif
        if (applyButton != null) applyButton.Clicked -= ApplySettings;
        if (resetDefaultsButton != null) resetDefaultsButton.Clicked -= ResetDefaults;
    }
    private void LoadInitialState()
    {
        if (menuAudioManager != null)
        {
            SetSliderWithoutNotify(masterSlider, menuAudioManager.MasterVolume);
            SetSliderWithoutNotify(musicSlider, menuAudioManager.MusicVolume);
            SetSliderWithoutNotify(sfxSlider, menuAudioManager.SfxVolume);
        }
        audioModeIndex = Mathf.Clamp(PlayerPrefs.GetInt(AudioModeKey, 0), 0, 1);
        subtitlesIndex = Mathf.Clamp(PlayerPrefs.GetInt(SubtitlesKey, 0), 0, 1);
        languageIndex = UiText.IndexFromLanguage(UiText.CurrentLanguage);
#if ENABLE_INPUT_SYSTEM
        SyncPendingBindingsFromRuntime();
#endif
        ApplyLocalizedStaticLabels();
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
        audioModeIndex = (audioModeIndex + 1) % 2;
        RefreshSelectorLabels();
    }
    private void CycleSubtitles()
    {
        subtitlesIndex = (subtitlesIndex + 1) % 2;
        RefreshSelectorLabels();
    }
    private void CycleLanguage()
    {
        languageIndex = (languageIndex + 1) % 2;
        RefreshSelectorLabels();
    }
    private void SelectSection(int index)
    {
        selectedSectionIndex = Mathf.Clamp(index, 0, sections.Length - 1);
        ApplyTabVisuals();
        for (int i = 0; i < sections.Length; i++)
        {
            bool active = i == selectedSectionIndex;
            if (sections[i] != null) sections[i].SetActive(active);
        }
        if (sectionTitleText != null && selectedSectionIndex < sectionTitles.Length)
        {
            sectionTitleText.text = UiText.GetOptionsSectionTitle(selectedSectionIndex);
        }
    }
    private void HandleLanguageChanged()
    {
        languageIndex = UiText.IndexFromLanguage(UiText.CurrentLanguage);
        RefreshAllLabels();
        SelectSection(selectedSectionIndex);
    }
    private void ApplyLocalizedStaticLabels()
    {
        for (int i = 0; i < tabLabels.Length; i++)
        {
            if (tabLabels[i] != null)
            {
                tabLabels[i].text = UiText.GetOptionsSectionTitle(i);
            }
        }
        applyButton?.SetLabel(UiText.GetMainMenuButtonLabel("apply"));
        resetDefaultsButton?.SetLabel(UiText.GetMainMenuButtonLabel("reset"));
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
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            hoveredTabIndex = index;
            ApplyTabVisuals();
        });
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
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
        ApplyLocalizedStaticLabels();
        RefreshSliderLabels();
        RefreshSelectorLabels();
        RefreshBindingLabels();
    }
    private void RefreshSliderLabels()
    {
        SetPercentLabel(masterValueText, masterSlider != null ? masterSlider.value : 0f);
        SetPercentLabel(musicValueText, musicSlider != null ? musicSlider.value : 0f);
        SetPercentLabel(sfxValueText, sfxSlider != null ? sfxSlider.value : 0f);
    }
    private void RefreshSelectorLabels()
    {
        if (audioModeValueText != null) audioModeValueText.text = UiText.GetSelectorLabel("audio", audioModeIndex);
        if (subtitlesValueText != null) subtitlesValueText.text = UiText.GetSelectorLabel("subtitles", subtitlesIndex);
        if (languageValueText != null) languageValueText.text = UiText.GetSelectorLabel("language", languageIndex);
    }
    private void ResolveControlBindingReferences()
    {
        controlsSectionRoot = null;
        if (sections != null && sections.Length > 3 && sections[3] != null)
        {
            controlsSectionRoot = sections[3].transform;
        }
        if (controlsSectionRoot == null)
        {
            controlsSectionRoot = FindChildByEnding("SectionsRoot/ControlsSection");
        }
        if (controlsSectionRoot == null)
        {
            return;
        }
        EnsureControlsContainer();
        RebuildAllBindingRows();
        ResolveExistingBindingWidgets();
    }
    private void EnsureControlsContainer()
    {
        Transform viewportExisting = controlsSectionRoot.Find("ControlsViewport");
        Transform contentRoot = controlsSectionRoot.Find("ControlsViewport/ControlsContent");
        RectTransform viewportRect;
        if (viewportExisting != null)
        {
            viewportRect = viewportExisting as RectTransform;
            Image existingImage = viewportExisting.GetComponent<Image>();
            if (existingImage == null) existingImage = viewportExisting.gameObject.AddComponent<Image>();
            existingImage.color = new Color(1f, 1f, 1f, 1f);
            existingImage.raycastTarget = true;
            Mask existingMask = viewportExisting.GetComponent<Mask>();
            if (existingMask == null) existingMask = viewportExisting.gameObject.AddComponent<Mask>();
            existingMask.showMaskGraphic = false;
        }
        else
        {
            GameObject viewportGo = new("ControlsViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(controlsSectionRoot, false);
            viewportRect = viewportGo.GetComponent<RectTransform>();
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 1f);
            viewportImage.raycastTarget = true;
            Mask viewportMask = viewportGo.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;
        }
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(0f, 92f);
        viewportRect.offsetMax = new Vector2(-30f, -14f);
        if (contentRoot == null)
        {
            GameObject contentGo = new("ControlsContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewportRect, false);
            controlsContentRoot = contentGo.GetComponent<RectTransform>();
        }
        else
        {
            controlsContentRoot = contentRoot as RectTransform;
        }
        controlsContentRoot.anchorMin = new Vector2(0f, 1f);
        controlsContentRoot.anchorMax = new Vector2(0f, 1f);
        controlsContentRoot.pivot = new Vector2(0f, 1f);
        controlsContentRoot.anchoredPosition = Vector2.zero;
        float totalRowCount = BindingDefinitions.Length + SupplementalControlRows.Length;
        controlsContentRoot.sizeDelta = new Vector2(ControlsContentWidth, ControlsRowHeight * totalRowCount);
        ScrollRect scrollRect = controlsSectionRoot.GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            scrollRect = controlsSectionRoot.gameObject.AddComponent<ScrollRect>();
        }
        Scrollbar verticalScrollbar = EnsureVerticalScrollbar();
        scrollRect.viewport = viewportRect;
        scrollRect.content = controlsContentRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 36f;
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.verticalScrollbar = verticalScrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = 6f;
    }
    private void RebuildAllBindingRows()
    {
        if (controlsContentRoot == null)
        {
            return;
        }
        for (int i = 0; i < BindingDefinitions.Length; i++)
        {
            Transform legacyRow = controlsSectionRoot.Find(BindingDefinitions[i].RowName);
            if (legacyRow != null && legacyRow.parent == controlsSectionRoot)
            {
                legacyRow.gameObject.SetActive(false);
            }
        }
        for (int i = controlsContentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = controlsContentRoot.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        for (int i = 0; i < BindingDefinitions.Length; i++)
        {
            ControlBindingDefinition definition = BindingDefinitions[i];
            Transform row = CreateFallbackRow(definition, controlsContentRoot);
            RectTransform rowRect = row as RectTransform;
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(0f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -i * ControlsRowHeight);
                rowRect.sizeDelta = new Vector2(ControlsContentWidth, ControlsRowHeight);
                rowRect.localScale = Vector3.one;
            }
        }
        for (int i = 0; i < SupplementalControlRows.Length; i++)
        {
            var rowData = SupplementalControlRows[i];
            Transform row = CreateStaticInfoRow(rowData.RowName, UiText.LocalizeKnownText(rowData.Label), rowData.Value, controlsContentRoot);
            RectTransform rowRect = row as RectTransform;
            if (rowRect != null)
            {
                int rowIndex = BindingDefinitions.Length + i;
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(0f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -rowIndex * ControlsRowHeight);
                rowRect.sizeDelta = new Vector2(ControlsContentWidth, ControlsRowHeight);
                rowRect.localScale = Vector3.one;
            }
        }
        float totalRowCount = BindingDefinitions.Length + SupplementalControlRows.Length;
        controlsContentRoot.sizeDelta = new Vector2(ControlsContentWidth, ControlsRowHeight * totalRowCount);
    }
    private Transform FindTemplateRow()
    {
        if (controlsSectionRoot == null)
        {
            return null;
        }
        Transform[] candidates =
        {
            controlsSectionRoot.Find("ForwardRow"),
            controlsSectionRoot.Find("LeftRow"),
            controlsSectionRoot.Find("BackRow"),
            controlsSectionRoot.Find("RightRow"),
            controlsSectionRoot.Find("RunRow"),
            controlsSectionRoot.Find("ControlsViewport/ControlsContent/ForwardRow")
        };
        foreach (Transform candidate in candidates)
        {
            if (candidate != null)
            {
                return candidate;
            }
        }
        return null;
    }
    private Transform CreateFallbackRow(ControlBindingDefinition definition, Transform parent)
    {
        TMP_FontAsset font = sectionTitleText != null ? sectionTitleText.font : TMP_Settings.defaultFontAsset;
        GameObject rowGo = new(definition.RowName, typeof(RectTransform));
        rowGo.transform.SetParent(parent, false);
        GameObject labelGo = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(rowGo.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(330f, ControlsRowHeight);
        TMP_Text labelText = labelGo.GetComponent<TMP_Text>();
        labelText.font = font;
        labelText.fontSize = 20f;
        labelText.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
        labelText.color = new Color32(218, 205, 181, 255);
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.text = UiText.GetBindingActionLabel(definition.Action);
        GameObject selectorGo = new("SelectorButton", typeof(RectTransform), typeof(Image), typeof(Button));
        selectorGo.transform.SetParent(rowGo.transform, false);
        RectTransform selectorRect = selectorGo.GetComponent<RectTransform>();
        selectorRect.anchorMin = new Vector2(0f, 0.5f);
        selectorRect.anchorMax = new Vector2(0f, 0.5f);
        selectorRect.pivot = new Vector2(0f, 0.5f);
        selectorRect.anchoredPosition = new Vector2(360f, 0f);
        selectorRect.sizeDelta = new Vector2(260f, 42f);
        Image selectorBg = selectorGo.GetComponent<Image>();
        selectorBg.color = new Color(0.09f, 0.075f, 0.07f, 0.94f);
        GameObject valueGo = new("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueGo.transform.SetParent(selectorGo.transform, false);
        RectTransform valueRect = valueGo.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(8f, 0f);
        valueRect.offsetMax = new Vector2(-28f, 0f);
        TMP_Text valueText = valueGo.GetComponent<TMP_Text>();
        valueText.font = font;
        valueText.fontSize = 20f;
        valueText.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
        valueText.color = new Color32(225, 212, 188, 255);
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.text = GetBindingLabelFallback(definition.Action);
        GameObject arrowGo = new("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGo.transform.SetParent(selectorGo.transform, false);
        RectTransform arrowRect = arrowGo.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-10f, 0f);
        arrowRect.sizeDelta = new Vector2(16f, 20f);
        TMP_Text arrowText = arrowGo.GetComponent<TMP_Text>();
        arrowText.font = font;
        arrowText.fontSize = 16f;
        arrowText.fontStyle = FontStyles.Bold;
        arrowText.color = new Color32(185, 171, 148, 255);
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.text = "^";
        return rowGo.transform;
    }
    private Transform CreateStaticInfoRow(string rowName, string label, string value, Transform parent)
    {
        TMP_FontAsset font = sectionTitleText != null ? sectionTitleText.font : TMP_Settings.defaultFontAsset;
        GameObject rowGo = new(rowName, typeof(RectTransform));
        rowGo.transform.SetParent(parent, false);
        GameObject labelGo = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(rowGo.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(330f, ControlsRowHeight);
        TMP_Text labelText = labelGo.GetComponent<TMP_Text>();
        labelText.font = font;
        labelText.fontSize = 20f;
        labelText.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
        labelText.color = new Color32(218, 205, 181, 255);
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.text = label;
        ConfigureLabelText(labelText, true);
        GameObject valuePanel = new("ValuePanel", typeof(RectTransform), typeof(Image));
        valuePanel.transform.SetParent(rowGo.transform, false);
        RectTransform panelRect = valuePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(360f, 0f);
        panelRect.sizeDelta = new Vector2(260f, 42f);
        Image panelImage = valuePanel.GetComponent<Image>();
        panelImage.color = new Color(0.09f, 0.075f, 0.07f, 0.94f);
        GameObject valueGo = new("ValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueGo.transform.SetParent(valuePanel.transform, false);
        RectTransform valueRect = valueGo.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(8f, 0f);
        valueRect.offsetMax = new Vector2(-28f, 0f);
        TMP_Text valueText = valueGo.GetComponent<TMP_Text>();
        valueText.font = font;
        valueText.fontSize = 20f;
        valueText.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
        valueText.color = new Color32(225, 212, 188, 255);
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.text = value;
        ConfigureLabelText(valueText, false);
        GameObject arrowGo = new("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGo.transform.SetParent(valuePanel.transform, false);
        RectTransform arrowRect = arrowGo.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-10f, 0f);
        arrowRect.sizeDelta = new Vector2(16f, 20f);
        TMP_Text arrowText = arrowGo.GetComponent<TMP_Text>();
        arrowText.font = font;
        arrowText.fontSize = 16f;
        arrowText.fontStyle = FontStyles.Bold;
        arrowText.color = new Color32(185, 171, 148, 255);
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.text = "^";
        return rowGo.transform;
    }
    private Scrollbar EnsureVerticalScrollbar()
    {
        Transform existing = controlsSectionRoot.Find("ControlsScrollbar");
        if (existing != null)
        {
            Scrollbar existingScrollbar = existing.GetComponent<Scrollbar>();
            if (existingScrollbar != null)
            {
                return existingScrollbar;
            }
        }
        GameObject scrollbarGo = new("ControlsScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGo.transform.SetParent(controlsSectionRoot, false);
        RectTransform scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(20f, 0f);
        scrollbarRect.offsetMin = new Vector2(-20f, 92f);
        scrollbarRect.offsetMax = new Vector2(-4f, -14f);
        Image scrollbarBg = scrollbarGo.GetComponent<Image>();
        scrollbarBg.color = new Color(0.12f, 0.08f, 0.07f, 0.95f);
        scrollbarBg.raycastTarget = true;
        GameObject slidingAreaGo = new("Sliding Area", typeof(RectTransform));
        slidingAreaGo.transform.SetParent(scrollbarGo.transform, false);
        RectTransform slidingAreaRect = slidingAreaGo.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(2f, 2f);
        slidingAreaRect.offsetMax = new Vector2(-2f, -2f);
        GameObject handleGo = new("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(slidingAreaGo.transform, false);
        RectTransform handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 1f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(0f, 80f);
        Image handleImage = handleGo.GetComponent<Image>();
        handleImage.color = new Color(0.72f, 0.56f, 0.4f, 0.98f);
        handleImage.raycastTarget = true;
        Scrollbar scrollbar = scrollbarGo.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        scrollbar.size = 0.25f;
        scrollbar.value = 1f;
        scrollbar.numberOfSteps = 0;
        return scrollbar;
    }
    private void ResolveExistingBindingWidgets()
    {
        bindingWidgets.Clear();
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            Transform row = FindControlRow(definition.RowName);
            if (row == null)
            {
                continue;
            }
            ControlBindingWidgets widgets = GetOrCreateWidgets(row);
            if (widgets.labelText != null)
            {
                widgets.labelText.text = UiText.GetBindingActionLabel(definition.Action);
                ConfigureLabelText(widgets.labelText, true);
            }
            if (widgets.valueText != null)
            {
                ConfigureLabelText(widgets.valueText, false);
            }
            bindingWidgets[definition.Action] = widgets;
        }
    }
    private static ControlBindingWidgets GetOrCreateWidgets(Transform row)
    {
        ControlBindingWidgets widgets = new()
        {
            rowRect = row as RectTransform,
            labelText = row.Find("Label")?.GetComponent<TMP_Text>(),
            button = row.Find("SelectorButton")?.GetComponent<Button>(),
            valueText = row.Find("SelectorButton/ValueText")?.GetComponent<TMP_Text>()
        };
        if (widgets.button != null) widgets.button.transition = Selectable.Transition.ColorTint;
        return widgets;
    }
    private static void ConfigureLabelText(TMP_Text text, bool leftAligned)
    {
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = leftAligned ? 20f : 18f;
        text.characterSpacing = leftAligned ? 1.4f : 0.8f;
        text.alignment = leftAligned ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }
    private static void SetPercentLabel(TMP_Text label, float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }
    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider != null) slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }
    private SettingsSnapshot CaptureCurrentSnapshot()
    {
        SettingsSnapshot snapshot = new()
        {
            master = masterSlider != null ? masterSlider.value : 0f,
            music = musicSlider != null ? musicSlider.value : 0f,
            sfx = sfxSlider != null ? sfxSlider.value : 0f,
            audioMode = audioModeIndex,
            subtitles = subtitlesIndex,
            language = languageIndex
        };
#if ENABLE_INPUT_SYSTEM
        snapshot.controlKeys = ClonePendingBindings();
#endif
        return snapshot;
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
        audioModeIndex = Mathf.Clamp(snapshot.audioMode, 0, 1);
        subtitlesIndex = Mathf.Clamp(snapshot.subtitles, 0, 1);
        languageIndex = Mathf.Clamp(snapshot.language, 0, 1);
#if ENABLE_INPUT_SYSTEM
        pendingBindings.Clear();
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            Key key = snapshot.controlKeys != null && snapshot.controlKeys.TryGetValue(definition.Action, out Key storedKey)
                ? storedKey
                : MovementKeyBindings.GetDefaultKey(definition.Action);
            pendingBindings[definition.Action] = key;
        }
#endif
        RefreshAllLabels();
    }
    private Transform FindChildByEnding(string relativePath)
    {
        string expected = relativePath.Replace('\\', '/');
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            string childPath = child.name;
            Transform current = child.parent;
            while (current != null && current != transform)
            {
                childPath = current.name + "/" + childPath;
                current = current.parent;
            }
            if (childPath.EndsWith(expected, System.StringComparison.Ordinal))
            {
                return child;
            }
        }
        return null;
    }
    private Transform FindControlRow(string rowName)
    {
        if (controlsContentRoot != null)
        {
            Transform inContent = controlsContentRoot.Find(rowName);
            if (inContent != null)
            {
                return inContent;
            }
        }
        Transform direct = controlsSectionRoot.Find(rowName);
        if (direct != null)
        {
            return direct;
        }
        return FindChildByEnding("SectionsRoot/ControlsSection/ControlsViewport/ControlsContent/" + rowName);
    }
    private void RefreshBindingLabels()
    {
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            if (!bindingWidgets.TryGetValue(definition.Action, out ControlBindingWidgets widgets) || widgets.valueText == null)
            {
                continue;
            }
#if ENABLE_INPUT_SYSTEM
            if (widgets.button != null)
            {
                widgets.button.interactable = true;
            }
#endif
            widgets.valueText.text = GetBindingLabelForDisplay(definition.Action);
        }
    }
    private string GetBindingLabelForDisplay(MovementKeyBindings.GameAction action)
    {
#if ENABLE_INPUT_SYSTEM
        if (listeningAction.HasValue && listeningAction.Value == action)
        {
            return UiText.LocalizeKnownText("PRIME UMA TECLA");
        }
        string keyName = MovementKeyBindings.GetDisplayName(GetPendingKey(action));
        return action switch
        {
            MovementKeyBindings.GameAction.Attack => "MOUSE LEFT",
            MovementKeyBindings.GameAction.HeavyAttack => "MOUSE RIGHT",
            _ => keyName
        };
#else
        return action switch
        {
            MovementKeyBindings.GameAction.Forward => "W",
            MovementKeyBindings.GameAction.Left => "A",
            MovementKeyBindings.GameAction.Backward => "S",
            MovementKeyBindings.GameAction.Right => "D",
            MovementKeyBindings.GameAction.Run => "SHIFT",
            MovementKeyBindings.GameAction.Interact => "F",
            MovementKeyBindings.GameAction.Attack => "MOUSE LEFT",
            MovementKeyBindings.GameAction.HeavyAttack => "MOUSE RIGHT",
            MovementKeyBindings.GameAction.Defend => "R",
            MovementKeyBindings.GameAction.Jump => "SPACE",
            MovementKeyBindings.GameAction.Roll => "C",
            _ => "-"
        };
#endif
    }
    private static string GetBindingLabelFallback(MovementKeyBindings.GameAction action)
    {
        return action switch
        {
            MovementKeyBindings.GameAction.Forward => "W",
            MovementKeyBindings.GameAction.Left => "A",
            MovementKeyBindings.GameAction.Backward => "S",
            MovementKeyBindings.GameAction.Right => "D",
            MovementKeyBindings.GameAction.Run => "SHIFT",
            MovementKeyBindings.GameAction.Interact => "F",
            MovementKeyBindings.GameAction.Attack => "MOUSE LEFT",
            MovementKeyBindings.GameAction.HeavyAttack => "MOUSE RIGHT",
            MovementKeyBindings.GameAction.Defend => "R",
            MovementKeyBindings.GameAction.Jump => "SPACE",
            MovementKeyBindings.GameAction.Roll => "C",
            _ => "-"
        };
    }
#if ENABLE_INPUT_SYSTEM
    private void BeginListeningForBinding(MovementKeyBindings.GameAction action)
    {
        if (action == MovementKeyBindings.GameAction.Attack || action == MovementKeyBindings.GameAction.HeavyAttack)
        {
            return;
        }
        listeningAction = action;
        RefreshBindingLabels();
    }
    private void ApplyPendingBinding(MovementKeyBindings.GameAction action, Key key)
    {
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            if (definition.Action == action) continue;
            if (GetPendingKey(definition.Action) == key)
            {
                pendingBindings[definition.Action] = GetPendingKey(action);
                break;
            }
        }
        pendingBindings[action] = key;
    }
    private void SyncPendingBindingsFromRuntime()
    {
        MovementKeyBindings.EnsureLoaded();
        pendingBindings.Clear();
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            pendingBindings[definition.Action] = MovementKeyBindings.GetKey(definition.Action);
        }
    }
    private void ResetPendingBindingsToDefaults()
    {
        pendingBindings.Clear();
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            pendingBindings[definition.Action] = MovementKeyBindings.GetDefaultKey(definition.Action);
        }
    }
    private Key GetPendingKey(MovementKeyBindings.GameAction action)
    {
        return pendingBindings.TryGetValue(action, out Key key)
            ? key
            : MovementKeyBindings.GetDefaultKey(action);
    }
    private Dictionary<MovementKeyBindings.GameAction, Key> ClonePendingBindings()
    {
        Dictionary<MovementKeyBindings.GameAction, Key> copy = new();
        foreach (ControlBindingDefinition definition in BindingDefinitions)
        {
            copy[definition.Action] = GetPendingKey(definition.Action);
        }
        return copy;
    }
    private static bool BindingDictionariesMatch(
        Dictionary<MovementKeyBindings.GameAction, Key> left,
        Dictionary<MovementKeyBindings.GameAction, Key> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        foreach (KeyValuePair<MovementKeyBindings.GameAction, Key> pair in left)
        {
            if (!right.TryGetValue(pair.Key, out Key rightValue) || rightValue != pair.Value)
            {
                return false;
            }
        }
        return true;
    }
#endif
}
