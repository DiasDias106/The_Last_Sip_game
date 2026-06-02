using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AutoSceneSaveController : MonoBehaviour
{
    private const string ControllerObjectName = "AutoSceneSaveController";
    private const string MainMenuSceneName = "MainMenuScene";

    private static AutoSceneSaveController instance;

    private Coroutine activeSaveRoutine;
    private Canvas indicatorCanvas;
    private RectTransform indicatorRoot;
    private RectTransform indicatorRingRoot;
    private Image indicatorImage;
    private string lastLoadedSceneName = string.Empty;
    private Sprite indicatorSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        AutoSceneSaveController existing = FindFirstObjectByType<AutoSceneSaveController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject root = new GameObject(ControllerObjectName);
        DontDestroyOnLoad(root);
        instance = root.AddComponent<AutoSceneSaveController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildIndicator();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (indicatorRingRoot != null && indicatorImage != null)
        {
            indicatorRingRoot.Rotate(0f, 0f, 160f * Time.unscaledDeltaTime);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsGameplayScene(scene.name))
        {
            HideIndicator();
            lastLoadedSceneName = scene.name;
            return;
        }

        bool sceneChanged = !string.Equals(lastLoadedSceneName, scene.name, System.StringComparison.OrdinalIgnoreCase);
        lastLoadedSceneName = scene.name;

        if (!sceneChanged && mode != LoadSceneMode.Single)
        {
            return;
        }

        if (activeSaveRoutine != null)
        {
            StopCoroutine(activeSaveRoutine);
        }

        activeSaveRoutine = StartCoroutine(AutoSaveRoutine(scene.name));
    }

    private IEnumerator AutoSaveRoutine(string sceneName)
    {
        const float playerWaitTimeout = 10f;
        const float settleDuration = 1.35f;

        float elapsed = 0f;
        while (elapsed < playerWaitTimeout)
        {
            if (GameObject.FindGameObjectWithTag("Player") != null)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        float settle = 0f;
        while (settle < settleDuration)
        {
            if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            {
                HideIndicator();
                activeSaveRoutine = null;
                yield break;
            }

            settle += Time.unscaledDeltaTime;
            yield return null;
        }

        while (RuntimeSceneTransitionOverlay.IsOverlayVisible())
        {
            if (!IsGameplayScene(SceneManager.GetActiveScene().name))
            {
                HideIndicator();
                activeSaveRoutine = null;
                yield break;
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.35f);
        yield return SaveManager.SaveActiveSlotFromCurrentSceneRoutine(sceneName);
        ShowIndicator();
        yield return new WaitForSecondsRealtime(2f);
        HideIndicator();
        activeSaveRoutine = null;
    }

    private void BuildIndicator()
    {
        if (indicatorCanvas == null)
        {
            GameObject canvasGo = new GameObject("AutosaveIndicatorCanvas");
            canvasGo.transform.SetParent(transform, false);
            indicatorCanvas = canvasGo.AddComponent<Canvas>();
            indicatorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            indicatorCanvas.sortingOrder = 250;
            DontDestroyOnLoad(canvasGo);

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (indicatorSprite == null)
        {
            indicatorSprite = Resources.Load<Sprite>("loading_wine_ring");
        }
    }

    private void CreateIndicatorIfNeeded()
    {
        if (indicatorRoot != null && indicatorImage != null)
        {
            return;
        }

        Transform parent = indicatorCanvas != null ? indicatorCanvas.transform : transform;

        GameObject iconGo = new GameObject("AutosaveIndicator");
        iconGo.transform.SetParent(parent, false);
        indicatorRoot = iconGo.AddComponent<RectTransform>();
        indicatorRoot.sizeDelta = new Vector2(112f, 112f);

        GameObject ringGo = new GameObject("WineRing");
        ringGo.transform.SetParent(iconGo.transform, false);
        indicatorImage = ringGo.AddComponent<Image>();
        indicatorImage.raycastTarget = false;
        indicatorImage.sprite = indicatorSprite;
        indicatorImage.preserveAspect = true;
        indicatorImage.color = new Color(1f, 1f, 1f, 0.95f);

        indicatorRingRoot = ringGo.GetComponent<RectTransform>();
        indicatorRingRoot.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRingRoot.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRingRoot.pivot = new Vector2(0.5f, 0.5f);
        indicatorRingRoot.anchoredPosition = Vector2.zero;
        indicatorRingRoot.sizeDelta = new Vector2(72f, 72f);

        ApplyFallbackPlacement();
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) && !IsMainMenuScene(sceneName);
    }

    private static bool IsMainMenuScene(string sceneName)
    {
        return string.Equals(sceneName, MainMenuSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void ShowIndicator()
    {
        CreateIndicatorIfNeeded();

        if (indicatorImage != null && indicatorRoot != null)
        {
            ApplyFallbackPlacement();
            indicatorRoot.localRotation = Quaternion.identity;
            if (indicatorRingRoot != null)
            {
                indicatorRingRoot.localRotation = Quaternion.identity;
            }
        }
    }

    private void HideIndicator()
    {
        if (indicatorRoot != null)
        {
            Destroy(indicatorRoot.gameObject);
        }

        indicatorRoot = null;
        indicatorRingRoot = null;
        indicatorImage = null;
    }

    private void ApplyFallbackPlacement()
    {
        if (indicatorRoot == null)
        {
            return;
        }

        indicatorRoot.anchorMin = new Vector2(1f, 1f);
        indicatorRoot.anchorMax = new Vector2(1f, 1f);
        indicatorRoot.pivot = new Vector2(1f, 1f);
        indicatorRoot.anchoredPosition = new Vector2(-96f, -112f);
        indicatorRoot.sizeDelta = new Vector2(112f, 112f);
        indicatorRoot.localScale = Vector3.one;
    }
}
