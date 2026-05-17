using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingScreenController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private MenuAudioManager menuAudioManager;

    [Header("Panel References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private MenuPanelController creditsPanel;
    [SerializeField] private SceneFadeController sceneFadeController;
    [SerializeField] private CanvasGroup loadingCanvasGroup;

    [Header("Loading Visuals")]
    [SerializeField] private RectTransform loadingTitleRoot;
    [SerializeField] private RectTransform loadingIconRoot;
    [SerializeField] private TMP_Text quoteText;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private float loadingDuration = 2.5f;
    [SerializeField] private float shortFadeDuration = 0.22f;
    [SerializeField] private float loadingFadeDuration = 0.4f;
    [SerializeField] private float iconRotationSpeed = 160f;
    [SerializeField] private float textPulseSpeed = 1.9f;
    [SerializeField] private float quotePulseSpeed = 1.25f;
    [SerializeField] private float titleRevealDuration = 0.55f;
    [SerializeField] private float quoteRevealDuration = 0.8f;

    public event Action SequenceFinished;

    private Quaternion iconInitialRotation;
    private Vector3 titleInitialScale;
    private Color quoteBaseColor;
    private Color loadingBaseColor;
    private Coroutine activeSequence;

    private void Awake()
    {
        if (loadingIconRoot != null)
        {
            iconInitialRotation = loadingIconRoot.localRotation;
        }

        if (loadingTitleRoot != null)
        {
            titleInitialScale = loadingTitleRoot.localScale;
        }

        if (quoteText != null)
        {
            quoteBaseColor = quoteText.color;
        }

        if (loadingText != null)
        {
            loadingBaseColor = loadingText.color;
        }

        HideLoading();
    }

    private void Update()
    {
        if (loadingPanel == null || !loadingPanel.activeSelf)
        {
            return;
        }

        float delta = Time.unscaledDeltaTime;

        if (loadingIconRoot != null)
        {
            loadingIconRoot.Rotate(0f, 0f, -iconRotationSpeed * delta);
        }

        if (loadingText != null)
        {
            float pulse = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * textPulseSpeed * Mathf.PI * 2f));
            Color color = loadingBaseColor;
            color.a = pulse;
            loadingText.color = color;
        }

        if (quoteText != null)
        {
            float pulse = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * quotePulseSpeed * Mathf.PI * 2f));
            Color color = quoteBaseColor;
            color.a = pulse;
            quoteText.color = color;
        }
    }

    public void ShowLoadingThenOpenCredits()
    {
        StartSequence(ShowLoadingThenOpenCreditsRoutine());
    }

    public void ShowLoadingThenLoadScene(string sceneName)
    {
        StartSequence(ShowLoadingThenLoadSceneRoutine(sceneName));
    }

    public void HideLoading()
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.interactable = false;
            loadingCanvasGroup.blocksRaycasts = false;
        }

        if (loadingIconRoot != null)
        {
            loadingIconRoot.localRotation = iconInitialRotation;
        }

        if (loadingTitleRoot != null)
        {
            loadingTitleRoot.localScale = titleInitialScale;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void StartSequence(IEnumerator routine)
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
        }

        activeSequence = StartCoroutine(routine);
    }

    private IEnumerator ShowLoadingThenOpenCreditsRoutine()
    {
        yield return PlayLoadingIntro();
        yield return WaitForUnscaledSeconds(loadingDuration);
        yield return FadeToBlack();

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (creditsPanel != null)
        {
            creditsPanel.Show();
        }

        yield return FadeFromBlack();
        FinishSequence();
    }

    private IEnumerator ShowLoadingThenLoadSceneRoutine(string sceneName)
    {
        yield return PlayLoadingIntro();
        yield return WaitForUnscaledSeconds(loadingDuration);
        yield return FadeToBlack();

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        Debug.LogWarning($"Scene '{sceneName}' is not available in Build Settings.");

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        yield return FadeFromBlack();
        FinishSequence();
    }

    private IEnumerator PlayLoadingIntro()
    {
        menuAudioManager?.EnsureAmbientMusicIsPlaying();

        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(1f, shortFadeDuration);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.interactable = true;
            loadingCanvasGroup.blocksRaycasts = true;
        }

        if (loadingIconRoot != null)
        {
            loadingIconRoot.localRotation = iconInitialRotation;
        }

        if (loadingTitleRoot != null)
        {
            loadingTitleRoot.localScale = titleInitialScale * 0.95f;
        }

        if (quoteText != null)
        {
            Color quoteColor = quoteBaseColor;
            quoteColor.a = 0f;
            quoteText.color = quoteColor;
        }

        if (loadingText != null)
        {
            Color loadingColor = loadingBaseColor;
            loadingColor.a = 0f;
            loadingText.color = loadingColor;
        }

        yield return FadeCanvasGroup(loadingCanvasGroup, 0f, 1f, loadingFadeDuration);
        yield return StartCoroutine(AnimateTitleAndQuoteIntro());
        yield return FadeFromBlack();
    }

    private IEnumerator AnimateTitleAndQuoteIntro()
    {
        float elapsed = 0f;
        while (elapsed < Mathf.Max(titleRevealDuration, quoteRevealDuration))
        {
            elapsed += Time.unscaledDeltaTime;

            if (loadingTitleRoot != null)
            {
                float t = Mathf.Clamp01(elapsed / titleRevealDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                loadingTitleRoot.localScale = Vector3.Lerp(titleInitialScale * 0.95f, titleInitialScale, eased);
            }

            if (quoteText != null)
            {
                float t = Mathf.Clamp01(elapsed / quoteRevealDuration);
                Color quoteColor = quoteBaseColor;
                quoteColor.a = Mathf.Lerp(0f, quoteBaseColor.a, t);
                quoteText.color = quoteColor;
            }

            if (loadingText != null)
            {
                float t = Mathf.Clamp01(elapsed / 0.45f);
                Color loadingColor = loadingBaseColor;
                loadingColor.a = Mathf.Lerp(0f, loadingBaseColor.a, t);
                loadingText.color = loadingColor;
            }

            yield return null;
        }

        if (loadingTitleRoot != null)
        {
            loadingTitleRoot.localScale = titleInitialScale;
        }

        if (quoteText != null)
        {
            quoteText.color = quoteBaseColor;
        }

        if (loadingText != null)
        {
            loadingText.color = loadingBaseColor;
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(1f, shortFadeDuration);
        }
    }

    private IEnumerator FadeFromBlack()
    {
        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(0f, shortFadeDuration);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup target, float from, float to, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        float elapsed = 0f;
        target.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            target.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        target.alpha = to;
    }

    private IEnumerator WaitForUnscaledSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void FinishSequence()
    {
        activeSequence = null;
        SequenceFinished?.Invoke();
    }
}
