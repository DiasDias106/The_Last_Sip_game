using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RuntimeSceneTransitionOverlay : MonoBehaviour
{
    private const string OverlayObjectName = "RuntimeSceneTransitionOverlay";
    private const string LoadingSceneName = "MainMenuScene";
    private const float LoadingControllerTimeout = 5f;

    private Canvas _canvas;
    private Image _background;
    private Coroutine _activeRoutine;

    public static void BeginTransition(string targetSceneName, float blackScreenDuration)
    {
        RuntimeSceneTransitionOverlay overlay = FindOrCreateOverlay();
        overlay.StartTransitionInternal(targetSceneName, blackScreenDuration);
    }

    private static RuntimeSceneTransitionOverlay FindOrCreateOverlay()
    {
        RuntimeSceneTransitionOverlay existing = FindAnyObjectByType<RuntimeSceneTransitionOverlay>();
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject(OverlayObjectName);
        DontDestroyOnLoad(root);
        return root.AddComponent<RuntimeSceneTransitionOverlay>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildRuntimeCanvas();
        HideImmediate();
    }

    private void StartTransitionInternal(string targetSceneName, float blackScreenDuration)
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
        }

        _activeRoutine = StartCoroutine(TransitionRoutine(targetSceneName, blackScreenDuration));
    }

    private IEnumerator TransitionRoutine(string targetSceneName, float blackScreenDuration)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"Scene '{targetSceneName}' is not available in Build Settings.");
            HideImmediate();
            _activeRoutine = null;
            yield break;
        }

        ShowBlackOnly();
        yield return WaitForRealSeconds(blackScreenDuration);
        yield return StartCoroutine(ShowMainMenuLoadingThenLoad(targetSceneName));
        HideImmediate();
        _activeRoutine = null;
    }

    private IEnumerator ShowMainMenuLoadingThenLoad(string targetSceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName))
        {
            Debug.LogWarning($"Loading scene '{LoadingSceneName}' is not available in Build Settings.");
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }

        Scene loadingScene = SceneManager.GetSceneByName(LoadingSceneName);
        if (!loadingScene.isLoaded)
        {
            AsyncOperation loadMenuOp = SceneManager.LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
            while (loadMenuOp != null && !loadMenuOp.isDone)
            {
                yield return null;
            }
        }

        float elapsed = 0f;
        LoadingScreenController controller = null;
        while (controller == null && elapsed < LoadingControllerTimeout)
        {
            controller = FindAnyObjectByType<LoadingScreenController>(FindObjectsInactive.Include);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (controller == null)
        {
            Debug.LogWarning("Could not find LoadingScreenController in MainMenuScene. Falling back to direct scene load.");
            SceneManager.LoadScene(targetSceneName);
            yield break;
        }

        controller.ShowLoadingThenLoadScene(targetSceneName);
    }

    private void BuildRuntimeCanvas()
    {
        _canvas = gameObject.GetComponentInChildren<Canvas>(true);
        if (_canvas != null)
        {
            return;
        }

        GameObject canvasGo = new GameObject("OverlayCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        _background = bgGo.AddComponent<Image>();
        _background.color = Color.black;

        RectTransform bgRect = _background.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

    }

    private void ShowBlackOnly()
    {
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(true);
        }

        if (_background != null)
        {
            _background.color = new Color(0f, 0f, 0f, 1f);
        }

    }

    private IEnumerator WaitForRealSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void HideImmediate()
    {
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(false);
        }
    }
}
