using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Root")]
    [SerializeField] private GameObject pauseOverlayRoot;
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Header("Panels")]
    [SerializeField] private MenuPanelController pausePanel;
    [SerializeField] private MenuPanelController settingsPanel;
    [SerializeField] private OptionsPanelView optionsPanelView;
    [SerializeField] private ConfirmOverwritePanel confirmLeaveSettingsPanel;
    [SerializeField] private SaveSuccessPanel saveSuccessPanel;

    [Header("Buttons")]
    [SerializeField] private AnimatedMenuButton continueButton;
    [SerializeField] private AnimatedMenuButton saveButton;
    [SerializeField] private AnimatedMenuButton settingsButton;
    [SerializeField] private AnimatedMenuButton exitButton;
    [SerializeField] private AnimatedMenuButton settingsBackButton;

    [Header("Settings")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private MenuAudioManager menuAudioManager;
    [SerializeField] [Range(0.5f, 1f)] private float pauseMusicMultiplier = 0.5f;

    [Header("Feedback")]
    [SerializeField] private TMP_Text saveFeedbackText;

    private const string FullscreenPrefsKey = "MainMenu.FullScreen";
    private Coroutine feedbackRoutine;
    private bool isPaused;
    private bool isTransitioning;
    private bool isSaving;

    public static PauseMenuController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (menuAudioManager == null)
        {
            menuAudioManager = GetComponent<MenuAudioManager>();
            if (menuAudioManager == null)
            {
                menuAudioManager = FindFirstObjectByType<MenuAudioManager>(FindObjectsInactive.Include);
            }
        }

        Time.timeScale = 1f;
        SetOverlayVisible(false);

        if (pausePanel != null)
        {
            pausePanel.HideImmediate();
        }

        if (settingsPanel != null)
        {
            settingsPanel.HideImmediate();
        }

        if (optionsPanelView == null && settingsPanel != null)
        {
            optionsPanelView = settingsPanel.GetComponent<OptionsPanelView>();
        }

        confirmLeaveSettingsPanel?.HideImmediate();
        saveSuccessPanel?.HideImmediate();

        if (saveFeedbackText != null)
        {
            saveFeedbackText.alpha = 0f;
            saveFeedbackText.text = string.Empty;
        }

        bool fullscreen = PlayerPrefs.GetInt(FullscreenPrefsKey, 1) == 1;
        Screen.fullScreen = fullscreen;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }
    }

    private void OnEnable()
    {
        BindButton(continueButton, ResumeGame);
        BindButton(saveButton, SaveGame);
        BindButton(settingsButton, OpenSettings);
        BindButton(exitButton, ExitToMainMenu);
        BindButton(settingsBackButton, BackToPauseMenu);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void OnDisable()
    {
        UnbindButton(continueButton, ResumeGame);
        UnbindButton(saveButton, SaveGame);
        UnbindButton(settingsButton, OpenSettings);
        UnbindButton(exitButton, ExitToMainMenu);
        UnbindButton(settingsBackButton, BackToPauseMenu);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        }
    }

    private void Update()
    {
        if (!IsEscapePressedThisFrame() || isTransitioning)
        {
            return;
        }

        if (!isPaused)
        {
            PauseGame();
            return;
        }

        if (settingsPanel != null && settingsPanel.IsVisible)
        {
            BackToPauseMenu();
            return;
        }

        ResumeGame();
    }

    public void PauseGame()
    {
        if (isPaused || isTransitioning)
        {
            return;
        }

        isPaused = true;
        isTransitioning = true;
        Time.timeScale = 0f;
        menuAudioManager?.SetRuntimeMusicMultiplier(pauseMusicMultiplier);
        SetOverlayVisible(true);
        pausePanel?.Show();
        settingsPanel?.HideImmediate();
        SetCursorState(true);
        StartCoroutine(FinishTransitionNextFrame());
    }

    public void ResumeGame()
    {
        if (!isPaused || isTransitioning)
        {
            return;
        }

        StartCoroutine(ResumeRoutine());
    }

    public void OpenSettings()
    {
        if (!isPaused || isTransitioning)
        {
            return;
        }

        pausePanel?.HideImmediate();
        settingsPanel?.Show();
    }

    public void BackToPauseMenu()
    {
        if (!isPaused || isTransitioning)
        {
            return;
        }

        if (confirmLeaveSettingsPanel != null && confirmLeaveSettingsPanel.IsVisible)
        {
            return;
        }

        if (optionsPanelView != null && optionsPanelView.HasUnsavedChanges())
        {
            confirmLeaveSettingsPanel?.Show(
                "Existem alteracoes por guardar. Deseja sair sem guardar as alteracoes?",
                () =>
                {
                    optionsPanelView.RevertToAppliedSettings();
                    settingsPanel?.HideImmediate();
                    pausePanel?.Show();
                },
                () => { });
            return;
        }

        settingsPanel?.HideImmediate();
        pausePanel?.Show();
    }

    public void SaveGame()
    {
        if (!isPaused || isSaving)
        {
            return;
        }

        StartCoroutine(SaveGameRoutine());
    }

    public void ExitToMainMenu()
    {
        if (isTransitioning)
        {
            return;
        }

        menuAudioManager?.SetRuntimeMusicMultiplier(1f);
        Time.timeScale = 1f;

        if (PlayerHUDController.Instance != null) Destroy(PlayerHUDController.Instance.gameObject);
        if (PlayerInventory.Instance != null) Destroy(PlayerInventory.Instance.gameObject);
        
        Destroy(gameObject);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPrefsKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private IEnumerator ResumeRoutine()
    {
        isTransitioning = true;
        confirmLeaveSettingsPanel?.HideImmediate();
        pausePanel?.Hide();
        settingsPanel?.HideImmediate();

        float elapsed = 0f;
        const float waitDuration = 0.24f;
        while (elapsed < waitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetOverlayVisible(false);
        SetCursorState(false);
        menuAudioManager?.SetRuntimeMusicMultiplier(1f);
        Time.timeScale = 1f;
        isPaused = false;
        isTransitioning = false;
    }

    private IEnumerator FinishTransitionNextFrame()
    {
        yield return null;
        isTransitioning = false;
    }

    private void SetOverlayVisible(bool visible)
    {
        if (pauseOverlayRoot != null)
        {
            pauseOverlayRoot.SetActive(visible);
        }

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = visible ? 1f : 0f;
            overlayCanvasGroup.interactable = visible;
            overlayCanvasGroup.blocksRaycasts = visible;
        }
    }

    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void ShowSaveFeedback(string message)
    {
        if (saveFeedbackText == null)
        {
            return;
        }

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(SaveFeedbackRoutine(message));
    }

    private IEnumerator SaveFeedbackRoutine(string message)
    {
        saveFeedbackText.text = message;
        saveFeedbackText.alpha = 1f;

        float hold = 1.2f;
        while (hold > 0f)
        {
            hold -= Time.unscaledDeltaTime;
            yield return null;
        }

        float fade = 0.35f;
        while (fade > 0f)
        {
            fade -= Time.unscaledDeltaTime;
            saveFeedbackText.alpha = Mathf.Clamp01(fade / 0.35f);
            yield return null;
        }

        saveFeedbackText.alpha = 0f;
        feedbackRoutine = null;
    }

    private IEnumerator SaveGameRoutine()
    {
        isSaving = true;

        bool settingsWasVisible = settingsPanel != null && settingsPanel.IsVisible;
        bool pauseWasVisible = pausePanel != null && pausePanel.IsVisible;

        confirmLeaveSettingsPanel?.HideImmediate();
        settingsPanel?.HideImmediate();
        pausePanel?.HideImmediate();
        SetOverlayVisible(false);
        yield return null;
        yield return new WaitForEndOfFrame();

        int activeSlot = SaveManager.GetActiveSlot();
        string screenshotPath = SaveManager.GetSlotScreenshotPath(activeSlot);

        if (!TryCaptureScreenshot(screenshotPath))
        {
            screenshotPath = string.Empty;
        }

        SaveManager.SaveActiveSlotFromCurrentScene(SceneManager.GetActiveScene().name, screenshotPath);

        SetOverlayVisible(true);
        if (settingsWasVisible)
        {
            settingsPanel?.Show();
        }
        else if (pauseWasVisible)
        {
            pausePanel?.Show();
        }

        if (saveSuccessPanel != null)
        {
            saveSuccessPanel.Show("JOGO GUARDADO COM SUCESSO.");
        }
        else
        {
            ShowSaveFeedback("JOGO GUARDADO");
        }
        isSaving = false;
    }

    private bool TryCaptureScreenshot(string screenshotPath)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath))
        {
            return false;
        }

        string directory = Path.GetDirectoryName(screenshotPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Texture2D shot = null;
        try
        {
            shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (shot == null)
            {
                return false;
            }

            byte[] png = shot.EncodeToPNG();
            if (png == null || png.Length == 0)
            {
                return false;
            }

            File.WriteAllBytes(screenshotPath, png);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (shot != null)
            {
                Destroy(shot);
            }
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
}
