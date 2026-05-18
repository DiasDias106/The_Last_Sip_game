using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class SceneFadeController : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.6f;

    private Coroutine runningFade;

    private void Reset()
    {
        fadeImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (fadeImage == null)
        {
            fadeImage = GetComponent<Image>();
        }

        SetVisibleImmediate(false);
    }

    public void SetVisibleImmediate(bool visible)
    {
        if (runningFade != null)
        {
            StopCoroutine(runningFade);
            runningFade = null;
        }

        float alpha = visible ? 1f : 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }

        if (fadeImage != null)
        {
            var color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }

    public void FadeOutAndThen(Action onComplete)
    {
        if (runningFade != null)
        {
            StopCoroutine(runningFade);
        }

        runningFade = StartCoroutine(FadeRoutine(0f, 1f, onComplete));
    }

    public void FadeInFromBlack(Action onComplete = null)
    {
        if (runningFade != null)
        {
            StopCoroutine(runningFade);
        }

        SetVisibleImmediate(true);
        runningFade = StartCoroutine(FadeRoutine(1f, 0f, onComplete));
    }

    public IEnumerator FadeToAlpha(float targetAlpha, float duration)
    {
        if (runningFade != null)
        {
            StopCoroutine(runningFade);
            runningFade = null;
        }

        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        float elapsed = 0f;

        if (canvasGroup != null)
        {
            bool blocksInput = targetAlpha > startAlpha;
            canvasGroup.blocksRaycasts = blocksInput;
            canvasGroup.interactable = blocksInput;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            ApplyAlpha(alpha);
            yield return null;
        }

        ApplyAlpha(targetAlpha);

        if (canvasGroup != null)
        {
            bool blocksInput = targetAlpha > 0.001f;
            canvasGroup.blocksRaycasts = blocksInput;
            canvasGroup.interactable = blocksInput;
        }
    }

    private IEnumerator FadeRoutine(float from, float to, Action onComplete)
    {
        float elapsed = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            ApplyAlpha(alpha);
            yield return null;
        }

        ApplyAlpha(to);

        if (canvasGroup != null)
        {
            bool blocksInput = to > 0.001f;
            canvasGroup.blocksRaycasts = blocksInput;
            canvasGroup.interactable = blocksInput;
        }

        onComplete?.Invoke();
        runningFade = null;
    }

    private void ApplyAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (fadeImage != null)
        {
            var color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
}
