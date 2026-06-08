using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class MainMenuController : MonoBehaviour
{
    private const string PendingSceneKey = "__PENDING_LOAD_SCENE__";

    [Header("Audio")]
    [SerializeField] private MenuAudioManager menuAudioManager;
    [SerializeField] private AudioClip menuAmbientClip;
    [SerializeField] private AudioClip creditsAmbientClip;

    [Header("Scene Flow")]
    [SerializeField] private string defaultGameSceneName = "Beco";
    [SerializeField] private string newGameIntroSceneName = "new_game_controls";
    [SerializeField] private SceneFadeController sceneFadeController;
    [SerializeField] private LoadingScreenController loadingScreenController;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private MenuPanelController optionsPanel;
    [SerializeField] private OptionsPanelView optionsPanelView;
    [SerializeField] private MenuPanelController creditsPanel;
    [SerializeField] private SlotSelectionController slotSelectionController;
    [SerializeField] private ConfirmOverwritePanel confirmOverwritePanel;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private AnimatedMenuButton newGameButton;
    [SerializeField] private AnimatedMenuButton continueButton;
    [SerializeField] private AnimatedMenuButton optionsButton;
    [SerializeField] private AnimatedMenuButton creditsButton;
    [SerializeField] private AnimatedMenuButton exitButton;

    [Header("Version")]
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private bool useApplicationVersion = false;
    [SerializeField] private string versionPrefix = "v";
    [SerializeField] private string fallbackVersion = "1.0.0";

    private const string FullscreenPrefsKey = "MainMenu.FullScreen";
    private bool isTransitioning;
    private bool isMainMenuInputBlocked;
    private MenuPanelController activePanel;
    private string pendingSceneFromTransition;
    private bool pendingSceneLaunchQueued;
    private bool pendingCreditsLaunchQueued;

    private void Awake()
    {
        if (menuAudioManager == null)
        {
            menuAudioManager = GetComponent<MenuAudioManager>();
        }

        if (loadingScreenController == null)
        {
            loadingScreenController = FindFirstObjectByType<LoadingScreenController>(FindObjectsInactive.Include);
        }

        if (optionsPanelView == null && optionsPanel != null)
        {
            optionsPanelView = optionsPanel.GetComponent<OptionsPanelView>();
        }

        GameMusicManager.Instance?.StopAll();
        if (menuAudioManager != null && menuAmbientClip != null)
        {
            menuAudioManager.SetAmbientMusicClip(menuAmbientClip);
        }

        if (versionText != null)
        {
            string resolvedVersion = useApplicationVersion && !string.IsNullOrWhiteSpace(Application.version)
                ? Application.version
                : fallbackVersion;
            versionText.text = versionPrefix + resolvedVersion;
        }

        // When arriving from a death-retry, ScreenFader is already black (sortingOrder=999).
        // Don't touch any panels — LoadingScreenController.PlayLoadingIntro will hide
        // mainMenuPanel once it's safe to do so (under its own SceneFadeController black).
        bool deathRetry = PlayerPrefs.GetInt("__DR__", 0) == 1;
        if (deathRetry)
        {
            PlayerPrefs.DeleteKey("__DR__");
            PlayerPrefs.Save();
        }

        pendingSceneFromTransition = !string.IsNullOrWhiteSpace(GlobalSceneTransitionRequest.PendingSceneName)
            ? GlobalSceneTransitionRequest.PendingSceneName
            : PlayerPrefs.GetString(PendingSceneKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(pendingSceneFromTransition))
        {
            GlobalSceneTransitionRequest.PendingSceneName = null;
            PlayerPrefs.DeleteKey(PendingSceneKey);
            PlayerPrefs.Save();
            pendingSceneLaunchQueued = true;
        }

        if (GlobalSceneTransitionRequest.PendingOpenCredits)
        {
            GlobalSceneTransitionRequest.PendingOpenCredits = false;
            pendingCreditsLaunchQueued = true;
        }

        optionsPanel?.HideImmediate();
        creditsPanel?.HideImmediate();
        slotSelectionController?.HideImmediate();
        confirmOverwritePanel?.HideImmediate();
        loadingScreenController?.HideLoading();
        if (pendingSceneLaunchQueued || pendingCreditsLaunchQueued)
        {
            mainMenuPanel?.SetActive(false);
            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = 0f;
                mainMenuCanvasGroup.interactable = false;
                mainMenuCanvasGroup.blocksRaycasts = false;
            }
            sceneFadeController?.SetVisibleImmediate(true);
        }
        else
        {
            sceneFadeController?.SetVisibleImmediate(false);
        }

        bool fullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, 1) == 1;
        Screen.fullScreen = fullscreen;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }

        RefreshContinueVisibility();
        ApplyLocalizedLabels();
        UpdateMainMenuPresentation();

    }

    private void OnEnable()
    {
        BindButton(newGameButton, OpenNewGameSlots);
        BindButton(continueButton, OpenContinueSlots);
        BindButton(optionsButton, OpenOptions);
        BindButton(creditsButton, OnCreditsClicked);
        BindButton(exitButton, QuitGame);

        BindPanel(optionsPanel);
        BindPanel(creditsPanel);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (slotSelectionController != null)
        {
            slotSelectionController.SlotChosen += HandleSlotChosen;
            slotSelectionController.SlotDeleteRequested += HandleSlotDeleteRequested;
            slotSelectionController.BackRequested += ReturnFromSlotSelection;
        }

        if (loadingScreenController != null)
        {
            loadingScreenController.SequenceFinished += HandleLoadingSequenceFinished;
        }

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.OnLanguageChanged += HandleLanguageChanged;
        }

    }

    private void Start()
    {
        if (pendingSceneLaunchQueued)
        {
            StartCoroutine(BeginPendingSceneTransition());
            return;
        }

        if (pendingCreditsLaunchQueued)
        {
            pendingCreditsLaunchQueued = false;
            StartCoroutine(OpenCreditsFromGameTransition());
        }
    }

    // Opens credits directly from a game-to-credits transition (no loading screen).
    private System.Collections.IEnumerator OpenCreditsFromGameTransition()
    {
        isTransitioning = true;
        SetMainMenuInputBlocked(true);
        mainMenuPanel?.SetActive(false);

        if (menuAudioManager != null && creditsAmbientClip != null)
            menuAudioManager.SetAmbientMusicClip(creditsAmbientClip);

        optionsPanel?.HideImmediate();
        slotSelectionController?.HideImmediate();
        confirmOverwritePanel?.HideImmediate();

        creditsPanel?.Show();
        activePanel = creditsPanel;

        // Fade in from the black screen left by the limo sequence
        if (sceneFadeController != null)
            yield return sceneFadeController.FadeToAlpha(0f, 0.5f);

        SetMainMenuInputBlocked(false);
        isTransitioning = false;
        UpdateMainMenuPresentation();
    }

    private void OnDisable()
    {
        UnbindButton(newGameButton, OpenNewGameSlots);
        UnbindButton(continueButton, OpenContinueSlots);
        UnbindButton(optionsButton, OpenOptions);
        UnbindButton(creditsButton, OnCreditsClicked);
        UnbindButton(exitButton, QuitGame);

        UnbindPanel(optionsPanel);
        UnbindPanel(creditsPanel);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        }

        if (slotSelectionController != null)
        {
            slotSelectionController.SlotChosen -= HandleSlotChosen;
            slotSelectionController.SlotDeleteRequested -= HandleSlotDeleteRequested;
            slotSelectionController.BackRequested -= ReturnFromSlotSelection;
        }

        if (loadingScreenController != null)
        {
            loadingScreenController.SequenceFinished -= HandleLoadingSequenceFinished;
        }

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
        }
    }

    private void Update()
    {
        if (!IsEscapePressedThisFrame() || isTransitioning)
        {
            return;
        }

        if (confirmOverwritePanel != null && confirmOverwritePanel.IsVisible)
        {
            confirmOverwritePanel.HideImmediate();
            return;
        }

        if (slotSelectionController != null && slotSelectionController.IsVisible)
        {
            ReturnFromSlotSelection();
            return;
        }

        if (activePanel == creditsPanel)
        {
            ReturnFromCredits();
            return;
        }

        if (activePanel == optionsPanel)
        {
            RequestCloseOptionsPanel();
            return;
        }

        if (activePanel != null)
        {
            activePanel.Hide();
        }
    }

    public void OpenNewGameSlots()
    {
        if (isTransitioning || slotSelectionController == null)
        {
            return;
        }

        activePanel = null;
        optionsPanel?.HideImmediate();
        creditsPanel?.HideImmediate();
        confirmOverwritePanel?.HideImmediate();
        slotSelectionController.Open(SlotSelectionController.SlotSelectionMode.NewGame);
        UpdateMainMenuPresentation();
    }

    public void OpenContinueSlots()
    {
        if (isTransitioning || slotSelectionController == null || !SaveManager.HasAnySave())
        {
            return;
        }

        activePanel = null;
        optionsPanel?.HideImmediate();
        creditsPanel?.HideImmediate();
        confirmOverwritePanel?.HideImmediate();
        slotSelectionController.Open(SlotSelectionController.SlotSelectionMode.Continue);
        UpdateMainMenuPresentation();
    }

    public void OpenOptions()
    {
        if (isTransitioning || (slotSelectionController != null && slotSelectionController.IsVisible))
        {
            return;
        }

        if (creditsPanel != null && creditsPanel.IsVisible)
        {
            return;
        }

        mainMenuPanel?.SetActive(true);
        activePanel = optionsPanel;
        optionsPanel?.Show();
        UpdateMainMenuPresentation();
    }

    public void OnCreditsClicked()
    {
        if (isTransitioning || loadingScreenController == null)
        {
            return;
        }

        StartCoroutine(OpenCreditsWithLoading());
    }

    public void ReturnFromCredits()
    {
        if (isTransitioning || creditsPanel == null || !creditsPanel.IsVisible)
        {
            return;
        }

        StartCoroutine(ReturnFromCreditsRoutine());
    }

    public void ReturnFromSlotSelection()
    {
        confirmOverwritePanel?.HideImmediate();
        slotSelectionController?.HideImmediate();
        mainMenuPanel?.SetActive(true);
        activePanel = null;
        UpdateMainMenuPresentation();
    }

    public void RequestCloseOptionsPanel()
    {
        if (isTransitioning || optionsPanel == null || !optionsPanel.IsVisible)
        {
            return;
        }

        if (optionsPanelView != null && optionsPanelView.HasUnsavedChanges())
        {
            confirmOverwritePanel?.Show(
                "Existem alterações por guardar. Deseja sair sem guardar as alterações?",
                () =>
                {
                    optionsPanelView.RevertToAppliedSettings();
                    optionsPanel.Hide();
                },
                () => { });
            return;
        }

        optionsPanel.Hide();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPrefsKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        if (isTransitioning)
        {
            return;
        }

        #if UNITY_EDITOR
        Debug.Log("Quit Game");
        #endif
        Application.Quit();
    }

    private System.Collections.IEnumerator ReturnFromCreditsRoutine()
    {
        isTransitioning = true;
        SetMainMenuInputBlocked(true);

        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(1f, 0.22f);
        }

        creditsPanel.HideImmediate();
        activePanel = null;
        mainMenuPanel?.SetActive(true);
        if (menuAudioManager != null && menuAmbientClip != null)
        {
            menuAudioManager.SetAmbientMusicClip(menuAmbientClip);
        }
        UpdateMainMenuPresentation();

        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(0f, 0.22f);
        }

        SetMainMenuInputBlocked(false);
        isTransitioning = false;
    }

    private System.Collections.IEnumerator OpenCreditsWithLoading()
    {
        isTransitioning = true;
        SetMainMenuInputBlocked(true);

        if (menuAudioManager != null && creditsAmbientClip != null)
        {
            menuAudioManager.SetAmbientMusicClip(creditsAmbientClip);
        }

        optionsPanel?.HideImmediate();
        slotSelectionController?.HideImmediate();
        confirmOverwritePanel?.HideImmediate();

        bool sequenceCompleted = false;
        void HandleFinished() { sequenceCompleted = true; }

        loadingScreenController.SequenceFinished += HandleFinished;
        loadingScreenController.ShowLoadingThenOpenCredits();

        while (!sequenceCompleted)
        {
            yield return null;
        }

        loadingScreenController.SequenceFinished -= HandleFinished;
        activePanel = creditsPanel;
        SetMainMenuInputBlocked(false);
        isTransitioning = false;
        UpdateMainMenuPresentation();
    }

    private void HandleSlotChosen(int slotIndex, SlotSelectionController.SlotSelectionMode mode, bool hasSave)
    {
        if (isTransitioning)
        {
            return;
        }

        if (mode == SlotSelectionController.SlotSelectionMode.NewGame)
        {
            if (!hasSave)
            {
                StartNewGameInSlot(slotIndex);
                return;
            }

            confirmOverwritePanel?.Show(
                UiText.LocalizeKnownText("Este slot já tem um jogo guardado. Queres apagar este progresso e começar um novo jogo?"),
                () => StartNewGameInSlot(slotIndex),
                () => slotSelectionController?.Refresh());
            return;
        }

        if (!hasSave)
        {
            return;
        }

        ContinueFromSlot(slotIndex);
    }

    private void StartNewGameInSlot(int slotIndex)
    {
        SaveManager.DeleteSlot(slotIndex);
        SaveManager.CreateNewGame(slotIndex);
        SaveManager.SetActiveSlot(slotIndex);
        RefreshContinueVisibility();
        string introScene = string.IsNullOrWhiteSpace(newGameIntroSceneName) ? defaultGameSceneName : newGameIntroSceneName;
        StartLoadingToScene(introScene);
    }

    private void ContinueFromSlot(int slotIndex)
    {
        SaveManager.SetActiveSlot(slotIndex);
        string scene = SaveManager.GetNextSceneForSlot(slotIndex);
        StartLoadingToScene(string.IsNullOrWhiteSpace(scene) ? defaultGameSceneName : scene);
    }

    private void HandleSlotDeleteRequested(int slotIndex, SlotSelectionController.SlotSelectionMode mode, bool hasSave)
    {
        if (isTransitioning || mode != SlotSelectionController.SlotSelectionMode.NewGame || !hasSave)
        {
            return;
        }

        confirmOverwritePanel?.Show(
            UiText.LocalizeKnownText("Queres apagar este jogo guardado deste slot?"),
            () =>
            {
                SaveManager.DeleteSlot(slotIndex);
                slotSelectionController?.Refresh();
                RefreshContinueVisibility();
            },
            () => slotSelectionController?.Refresh());
    }

    private void StartLoadingToScene(string sceneName)
    {
        if (loadingScreenController == null)
        {
            loadingScreenController = FindFirstObjectByType<LoadingScreenController>(FindObjectsInactive.Include);
        }

        if (loadingScreenController == null)
        {
            Debug.LogWarning("LoadingScreenController not found. Falling back to direct scene load.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(ResolveSceneName(sceneName));
            return;
        }

        isTransitioning = true;
        SetMainMenuInputBlocked(true);
        slotSelectionController?.HideImmediate();
        confirmOverwritePanel?.HideImmediate();
        loadingScreenController.ShowLoadingThenLoadScene(ResolveSceneName(sceneName));
    }

    private System.Collections.IEnumerator BeginPendingSceneTransition()
    {
        yield return null;

        if (string.IsNullOrWhiteSpace(pendingSceneFromTransition))
        {
            pendingSceneLaunchQueued = false;
            yield break;
        }

        mainMenuPanel?.SetActive(false);
        StartLoadingToScene(pendingSceneFromTransition);
        pendingSceneFromTransition = null;
        pendingSceneLaunchQueued = false;
    }

    private string ResolveSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            sceneName = defaultGameSceneName;
        }

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return sceneName;
        }

        string lower = sceneName.ToLowerInvariant();
        if (Application.CanStreamedLevelBeLoaded(lower))
        {
            return lower;
        }

        string upper = sceneName.ToUpperInvariant();
        if (Application.CanStreamedLevelBeLoaded(upper))
        {
            return upper;
        }

        // Scene aliases after beco split/rename.
        if (lower == "beco")
        {
            if (Application.CanStreamedLevelBeLoaded("beco_noite"))
            {
                return "beco_noite";
            }

            if (Application.CanStreamedLevelBeLoaded("beco_dia"))
            {
                return "beco_dia";
            }
        }

        return sceneName;
    }

    private void HandlePanelVisibilityChanged(bool _visible)
    {
        activePanel = optionsPanel != null && optionsPanel.IsVisible
            ? optionsPanel
            : creditsPanel != null && creditsPanel.IsVisible
                ? creditsPanel
                : null;

        UpdateMainMenuPresentation();
    }

    private void HandleLoadingSequenceFinished()
    {
        isTransitioning = false;
        SetMainMenuInputBlocked(false);
        activePanel = creditsPanel != null && creditsPanel.IsVisible ? creditsPanel : null;
        RefreshContinueVisibility();
        UpdateMainMenuPresentation();
    }

    private void UpdateMainMenuPresentation()
    {
        if (mainMenuCanvasGroup == null)
        {
            return;
        }

        bool showSlots = slotSelectionController != null && slotSelectionController.IsVisible;
        bool dimMainMenu = activePanel == optionsPanel || showSlots;
        mainMenuCanvasGroup.alpha = dimMainMenu ? 0f : 1f;
        mainMenuCanvasGroup.interactable = !dimMainMenu && !isMainMenuInputBlocked;
        mainMenuCanvasGroup.blocksRaycasts = !dimMainMenu && !isMainMenuInputBlocked;
    }

    private void RefreshContinueVisibility()
    {
        ApplyLocalizedLabels();
        if (continueButton == null)
        {
            return;
        }

        bool hasAnySave = SaveManager.HasAnySave();
        continueButton.gameObject.SetActive(hasAnySave);
        Button button = continueButton.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = hasAnySave;
        }
    }

    private void SetMainMenuInputBlocked(bool blocked)
    {
        isMainMenuInputBlocked = blocked;
        UpdateMainMenuPresentation();
    }

    private void HandleLanguageChanged()
    {
        ApplyLocalizedLabels();
        slotSelectionController?.Refresh();
        RuntimeTextLocalizer.RequestRefresh();
    }

    private void ApplyLocalizedLabels()
    {
        newGameButton?.SetLabel(UiText.GetMainMenuButtonLabel("new"));
        continueButton?.SetLabel(UiText.GetMainMenuButtonLabel("continue"));
        optionsButton?.SetLabel(UiText.GetMainMenuButtonLabel("options"));
        creditsButton?.SetLabel(UiText.GetMainMenuButtonLabel("credits"));
        exitButton?.SetLabel(UiText.GetMainMenuButtonLabel("exit"));
    }

    private static void BindButton(AnimatedMenuButton button, System.Action action)
    {
        if (button != null)
        {
            button.Clicked += action;
        }
    }

    private static void UnbindButton(AnimatedMenuButton button, System.Action action)
    {
        if (button != null)
        {
            button.Clicked -= action;
        }
    }

    private void BindPanel(MenuPanelController panel)
    {
        if (panel != null)
        {
            panel.VisibilityChanged += HandlePanelVisibilityChanged;
        }
    }

    private void UnbindPanel(MenuPanelController panel)
    {
        if (panel != null)
        {
            panel.VisibilityChanged -= HandlePanelVisibilityChanged;
        }
    }

    private static bool IsEscapePressedThisFrame()
    {
        #if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        #else
        return Input.GetKeyDown(KeyCode.Escape);
        #endif
    }
}
