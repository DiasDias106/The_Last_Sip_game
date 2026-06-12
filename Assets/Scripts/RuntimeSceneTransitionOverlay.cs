using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class RuntimeSceneTransitionOverlay : MonoBehaviour
{
    private const string OverlayObjectName = "RuntimeSceneTransitionOverlay";
    private const string LoadingSceneName = "MainMenuScene";
    private const string PendingSceneKey = "__PENDING_LOAD_SCENE__";
    private Canvas _canvas;
    private Image _background;
    private Coroutine _activeRoutine;
    private bool _autoFadeOnLoad;
    public static void BeginTransition(string targetSceneName, float blackScreenDuration)
    {
        FindOrCreateOverlay().StartTransitionInternal(targetSceneName, blackScreenDuration);
    }
    public static void ShowBlackForSceneLoad()
    {
        var overlay = FindOrCreateOverlay();
        overlay.ShowBlackOnly();
        overlay._autoFadeOnLoad = true;
    }
    public static void TransitionInstantlyToLoading(string targetSceneName)
    {
        var overlay = FindOrCreateOverlay();
        overlay.ShowBlackOnly();
        if (overlay._activeRoutine != null) overlay.StopCoroutine(overlay._activeRoutine);
        overlay._activeRoutine = overlay.StartCoroutine(overlay.ShowMainMenuLoadingThenLoad(targetSceneName));
    }
    public static void HideOverlay()
    {
        var existing = FindAnyObjectByType<RuntimeSceneTransitionOverlay>();
        if (existing != null)
        {
            existing._autoFadeOnLoad = false;
            if (existing._activeRoutine != null) existing.StopCoroutine(existing._activeRoutine);
            existing.HideImmediate();
        }
    }
    public static bool IsOverlayVisible()
    {
        var existing = FindAnyObjectByType<RuntimeSceneTransitionOverlay>();
        if (existing == null || existing._canvas == null || existing._background == null)
        {
            return false;
        }
        return existing._canvas.gameObject.activeSelf && existing._background.color.a > 0.01f;
    }
    public static void WaitForPlayerAndHidePanel(UnityEngine.GameObject panel, float spinSpeed, UnityEngine.RectTransform icon)
    {
        FindOrCreateOverlay().StartCoroutine(FindOrCreateOverlay().WaitAndHidePanelRoutine(panel, spinSpeed, icon));
    }
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildRuntimeCanvas();
        HideImmediate();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_autoFadeOnLoad)
        {
            _autoFadeOnLoad = false;
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(WaitThenFadeFromBlack());
        }
    }
    private IEnumerator WaitThenFadeFromBlack()
    {
        yield return StartCoroutine(FadeFromBlack(1f));
        _activeRoutine = null;
    }
    private IEnumerator WaitAndHidePanelRoutine(UnityEngine.GameObject panel, float spinSpeed, UnityEngine.RectTransform icon)
    {
        float timeout = 10f;
        float elapsed = 0f;
        while (elapsed < timeout && panel != null)
        {
            elapsed += Time.unscaledDeltaTime;
            if (icon != null) icon.Rotate(0f, 0f, spinSpeed * Time.unscaledDeltaTime);
            if (UnityEngine.GameObject.FindGameObjectWithTag("Player") != null) break;
            yield return null;
        }
        float extra = 0f;
        while (extra < 1f && panel != null)
        {
            extra += Time.unscaledDeltaTime;
            if (icon != null) icon.Rotate(0f, 0f, spinSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        if (panel != null) UnityEngine.Object.Destroy(panel);
    }
    private static RuntimeSceneTransitionOverlay FindOrCreateOverlay()
    {
        var existing = FindAnyObjectByType<RuntimeSceneTransitionOverlay>();
        if (existing != null) return existing;
        var root = new GameObject(OverlayObjectName);
        DontDestroyOnLoad(root);
        return root.AddComponent<RuntimeSceneTransitionOverlay>();
    }
    private void StartTransitionInternal(string targetSceneName, float blackScreenDuration)
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(TransitionRoutine(targetSceneName, blackScreenDuration));
    }
    private IEnumerator TransitionRoutine(string targetSceneName, float blackScreenDuration)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName)) yield break;
        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"Scene '{targetSceneName}' is not in Build Settings.");
            HideImmediate();
            _activeRoutine = null;
            yield break;
        }
        yield return StartCoroutine(FadeToBlack(1f));
        yield return WaitForRealSeconds(blackScreenDuration);
        yield return StartCoroutine(ShowMainMenuLoadingThenLoad(targetSceneName));
        HideImmediate();
        _activeRoutine = null;
    }
    private IEnumerator FadeToBlack(float duration)
    {
        if (_canvas == null || _background == null) yield break;
        _canvas.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _background.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _background.color = Color.black;
    }
    private IEnumerator FadeFromBlack(float duration)
    {
        if (_canvas == null || _background == null) yield break;
        _canvas.gameObject.SetActive(true);
        _background.color = Color.black;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _background.color = new Color(0f, 0f, 0f, Mathf.Clamp01(1f - elapsed / duration));
            yield return null;
        }
        HideImmediate();
        _activeRoutine = null;
    }
    private void ShowBlackOnly()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(true);
        if (_background != null) _background.color = Color.black;
    }
    private IEnumerator WaitForRealSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds) { elapsed += Time.unscaledDeltaTime; yield return null; }
    }
    private IEnumerator ShowMainMenuLoadingThenLoad(string targetSceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            _autoFadeOnLoad = true;
            yield return SceneManager.LoadSceneAsync(targetSceneName);
            yield break;
        }
        GlobalSceneTransitionRequest.PendingSceneName = targetSceneName;
        PlayerPrefs.SetString(PendingSceneKey, targetSceneName);
        PlayerPrefs.Save();
        yield return SceneManager.LoadSceneAsync(LoadingSceneName);
    }
    private void HideImmediate()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }
    private void BuildRuntimeCanvas()
    {
        _canvas = gameObject.GetComponentInChildren<Canvas>(true);
        if (_canvas != null) return;
        var canvasGo = new GameObject("OverlayCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        _background = bgGo.AddComponent<Image>();
        _background.color = Color.black;
        var bgRect = _background.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
    }
}
