using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class MenuPanelController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panelTransform;
    [SerializeField] private float transitionDuration = 0.24f;
    [SerializeField] private Vector3 shownScale = Vector3.one;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.92f, 0.92f, 1f);
    [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -22f);

    private Coroutine transitionRoutine;
    private Vector2 shownAnchoredPosition;

    public event System.Action<bool> VisibilityChanged;
    public bool IsVisible { get; private set; }

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        panelTransform = transform as RectTransform;
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (panelTransform == null)
        {
            panelTransform = transform as RectTransform;
        }

        if (panelTransform != null)
        {
            shownAnchoredPosition = panelTransform.anchoredPosition;
        }
    }

    public void Show()
    {
        PlayTransition(true);
    }

    public void Hide()
    {
        PlayTransition(false);
    }

    private void PlayTransition(bool visible)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        gameObject.SetActive(true);
        transitionRoutine = StartCoroutine(TransitionRoutine(visible));
    }

    private IEnumerator TransitionRoutine(bool visible)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        Vector3 startScale = panelTransform != null ? panelTransform.localScale : Vector3.one;
        Vector2 startPosition = panelTransform != null ? panelTransform.anchoredPosition : Vector2.zero;
        float targetAlpha = visible ? 1f : 0f;
        Vector3 targetScale = visible ? shownScale : hiddenScale;
        Vector2 targetPosition = visible ? shownAnchoredPosition : shownAnchoredPosition + hiddenOffset;
        float elapsed = 0f;

        SetInteractable(false);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                panelTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            }

            yield return null;
        }

        SetState(visible, targetAlpha, targetPosition);
        transitionRoutine = null;
    }

    private void SetState(bool visible, float alpha, Vector2 anchoredPosition)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (panelTransform != null)
        {
            panelTransform.localScale = visible ? shownScale : hiddenScale;
            panelTransform.anchoredPosition = anchoredPosition;
        }

        IsVisible = visible;
        SetInteractable(visible);
        gameObject.SetActive(visible);
        VisibilityChanged?.Invoke(visible);
    }

    private void SetInteractable(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    public void SetHiddenOffset(Vector2 value)
    {
        hiddenOffset = value;
    }

    public void SetShownPosition(Vector2 value)
    {
        shownAnchoredPosition = value;
    }

    private void OnValidate()
    {
        if (panelTransform == null)
        {
            panelTransform = transform as RectTransform;
        }

        if (panelTransform != null)
        {
            shownAnchoredPosition = panelTransform.anchoredPosition;
        }
    }

    public void HideImmediate()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        Vector2 targetPosition = shownAnchoredPosition + hiddenOffset;
        SetState(false, 0f, targetPosition);
    }
}
