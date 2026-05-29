using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MemoryPickup : MonoBehaviour, IInteractable
{
    private const string PendingSceneKey = "__PENDING_LOAD_SCENE__";

    [HideInInspector] public int videoIndex = 1;

    [Header("Float Animation")]
    public float floatSpeed  = 1.5f;
    public float floatHeight = 0.25f;
    public float rotateSpeed = 50f;

    [Header("Interaction")]
    public bool loadSceneOnInteract = true;
    public bool playVideoBeforeSceneLoad = true;
    public string videoFileName = "1_memoria_v2";
    public string mainMenuSceneName = "MainMenuScene";
    public string targetSceneName = "masmorra_spawn";
    public string completedLevelSceneName = "level_1";
    public float blackFadeDuration = 0.28f;
    public string interactionPromptText = "Pressiona {0} para interagir";

    private Vector3 basePosition;
    private bool isTransitioning;
    private Image fadeImage;
    private Canvas fadeCanvas;
    private bool showingOwnPrompt;

    public bool CanInteract => !isTransitioning;

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatHeight);
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (CanInteract)
        {
            PlayerHUDController.Instance?.ShowInteractionPrompt(GetInteractionPromptText());
            showingOwnPrompt = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (showingOwnPrompt)
        {
            PlayerHUDController.Instance?.HideInteractionPrompt();
            showingOwnPrompt = false;
        }
    }

    public void Interact()
    {
        if (!CanInteract) return;

        if (showingOwnPrompt)
        {
            PlayerHUDController.Instance?.HideInteractionPrompt();
            showingOwnPrompt = false;
        }

        StartCoroutine(InteractionSequence());
    }

    private System.Collections.IEnumerator InteractionSequence()
    {
        isTransitioning = true;
        
        // 1. Melhorar a entrada: Fade to black antes do vídeo
        yield return FadeToBlack();

        // Esconder o modelo 3D da memória
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        // 2. Parar a música de fundo
        var musicMgr = FindObjectOfType<GameMusicManager>();
        if (musicMgr != null)
        {
            musicMgr.StopAll();
        }

        // 3. Tocar vídeo e carregar cena em background
        if (playVideoBeforeSceneLoad && !string.IsNullOrEmpty(videoFileName))
        {
            if (MemoryVideoPlayer.Instance == null)
            {
                var prefab = Resources.Load<GameObject>("MemoryVideoPlayer");
                if (prefab != null) Instantiate(prefab);
                else new GameObject("MemoryVideoPlayer").AddComponent<MemoryVideoPlayer>();
            }

            bool videoFinished = false;
            MemoryVideoPlayer.Instance?.PlayVideoByName(videoFileName, () => 
            {
                videoFinished = true;
            });

            if (loadSceneOnInteract)
            {
                // Esperar que o vídeo acabe
                while (!videoFinished)
                {
                    yield return null;
                }
                
                // Quando o vídeo acabar, carrega através do ecrã de loading normal
                yield return StartCoroutine(LoadTargetSceneThroughMainMenu());
            }
            else
            {
                while (!videoFinished) yield return null;
                Destroy(gameObject);
            }
        }
        else
        {
            if (loadSceneOnInteract)
            {
                StartCoroutine(LoadTargetSceneThroughMainMenu());
            }
            else
            {
                if (MemoryVideoPlayer.Instance == null)
                {
                    var prefab = Resources.Load<GameObject>("MemoryVideoPlayer");
                    if (prefab != null) Instantiate(prefab);
                    else new GameObject("MemoryVideoPlayer").AddComponent<MemoryVideoPlayer>();
                }
                MemoryVideoPlayer.Instance?.PlayVideo(videoIndex);
                Destroy(gameObject);
            }
        }
    }

    private System.Collections.IEnumerator LoadTargetSceneThroughMainMenu()
    {
        isTransitioning = true;
        yield return FadeToBlack();

        if (!string.IsNullOrWhiteSpace(completedLevelSceneName))
        {
            LevelCompletionState.SetCompleted(completedLevelSceneName, true);
        }

        GlobalSceneTransitionRequest.PendingSceneName = targetSceneName;
        PlayerPrefs.SetString(PendingSceneKey, targetSceneName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private System.Collections.IEnumerator FadeToBlack()
    {
        EnsureFadeOverlay();
        if (fadeImage.color.a >= 1f) yield break;

        float elapsed = 0f;
        float startAlpha = fadeImage.color.a;

        while (elapsed < blackFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color color = fadeImage.color;
            color.a = Mathf.Lerp(startAlpha, 1f, elapsed / blackFadeDuration);
            fadeImage.color = color;
            yield return null;
        }
    }

    private void EnsureFadeOverlay()
    {
        if (fadeCanvas != null && fadeImage != null)
        {
            return;
        }

        var canvasObject = new GameObject("MemoryTransitionFadeCanvas");
        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 90;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        var imageObject = new GameObject("Fade");
        imageObject.transform.SetParent(canvasObject.transform, false);
        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private string GetInteractionPromptText()
    {
        string keyName = MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact);
        string prompt = interactionPromptText ?? "Pressiona {0} para interagir";
        if (prompt.Contains("{0}"))
        {
            return string.Format(prompt, keyName);
        }

        prompt = prompt.Replace("Pressiona F", "Pressiona " + keyName).Replace("Pressiona E", "Pressiona " + keyName);
        return prompt;
    }
}
