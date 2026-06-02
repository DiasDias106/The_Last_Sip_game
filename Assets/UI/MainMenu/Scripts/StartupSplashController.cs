using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public sealed class StartupSplashController : MonoBehaviour
{
    private const string PendingSceneKey = "__PENDING_LOAD_SCENE__";
    private static bool hasShownSplashThisSession;

    [Header("References")]
    [SerializeField] private GameObject splashPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private CanvasGroup splashCanvasGroup;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TMP_Text promptText;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 0.15f;
    [SerializeField] private float fadeInDuration = 1.1f;
    [SerializeField] private float promptFadeDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float promptPulseSpeed = 1.15f;
    [SerializeField] private string promptMessage = "CLIQUE EM QUALQUER TELA PARA COMECAR";

    private bool waitingForInput;
    private bool skipSplashForSceneTransition;

    private void Awake()
    {
        skipSplashForSceneTransition =
            hasShownSplashThisSession ||
            !string.IsNullOrWhiteSpace(GlobalSceneTransitionRequest.PendingSceneName) ||
            !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(PendingSceneKey, string.Empty)) ||
            PlayerPrefs.GetInt("__DR__", 0) == 1;

        if (promptText != null)
        {
            promptText.text = promptMessage;
        }

        if (skipSplashForSceneTransition)
        {
            if (splashPanel != null)
            {
                splashPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 0f;
                splashCanvasGroup.interactable = false;
                splashCanvasGroup.blocksRaycasts = false;
            }

            if (promptCanvasGroup != null)
            {
                promptCanvasGroup.alpha = 0f;
                promptCanvasGroup.interactable = false;
                promptCanvasGroup.blocksRaycasts = false;
            }

            return;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (splashPanel != null)
        {
            splashPanel.SetActive(true);
        }

        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 0f;
            splashCanvasGroup.interactable = true;
            splashCanvasGroup.blocksRaycasts = true;
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (skipSplashForSceneTransition)
        {
            return;
        }

        StartCoroutine(PlaySequence());
    }

    private void Update()
    {
        if (!waitingForInput || promptCanvasGroup == null)
        {
            return;
        }

        float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * promptPulseSpeed * Mathf.PI * 2f));
        promptCanvasGroup.alpha = pulse;
    }

    private IEnumerator PlaySequence()
    {
        hasShownSplashThisSession = true;

        if (initialDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(initialDelay);
        }

        yield return FadeCanvasGroup(splashCanvasGroup, 0f, 1f, fadeInDuration);
        yield return FadeCanvasGroup(promptCanvasGroup, 0f, 1f, promptFadeDuration);

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.interactable = true;
            promptCanvasGroup.blocksRaycasts = false;
        }

        waitingForInput = true;
        while (!WasContinuePressedThisFrame())
        {
            yield return null;
        }

        waitingForInput = false;
        yield return FadeCanvasGroup(splashCanvasGroup, splashCanvasGroup != null ? splashCanvasGroup.alpha : 1f, 0f, fadeOutDuration);

        if (splashPanel != null)
        {
            splashPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private static bool WasContinuePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame))
        {
            return true;
        }

        if (Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame ||
             Gamepad.current.startButton.wasPressedThisFrame))
        {
            return true;
        }

        if (Touchscreen.current != null)
        {
            foreach (TouchControl touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        return false;
#else
        return Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
#endif
    }
}
