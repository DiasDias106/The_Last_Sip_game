using System.Collections;
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

    [Header("Buttons")]
    [SerializeField] private AnimatedMenuButton continueButton;
    [SerializeField] private AnimatedMenuButton saveButton;
    [SerializeField] private AnimatedMenuButton settingsButton;
    [SerializeField] private AnimatedMenuButton exitButton;
    [SerializeField] private AnimatedMenuButton settingsBackButton;

    [Header("Settings")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Feedback")]
    [SerializeField] private TMP_Text saveFeedbackText;

    private const string FullscreenPrefsKey = "MainMenu.FullScreen";
    private Coroutine feedbackRoutine;
    private bool isPaused;
    private bool isTransitioning;

    private void Awake()
    {
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

        settingsPanel?.HideImmediate();
        pausePanel?.Show();
    }

    public void SaveGame()
    {
        if (!isPaused)
        {
            return;
        }

        Debug.Log("Save Game requested. Save system not connected yet.");
        ShowSaveFeedback("JOGO GUARDADO EM BREVE");
    }

    public void ExitToMainMenu()
    {
        if (isTransitioning)
        {
            return;
        }

        Time.timeScale = 1f;
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
