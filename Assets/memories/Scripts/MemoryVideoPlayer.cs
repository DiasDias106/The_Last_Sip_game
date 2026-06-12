using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class MemoryVideoPlayer : MonoBehaviour
{
    public static MemoryVideoPlayer Instance { get; private set; }

    [Header("Video Clips")]
    [Tooltip("Slot 0 = video for boss 1, Slot 1 = boss 2, Slot 2 = boss 3")]
    public VideoClip[] videoClips = new VideoClip[3];

    [Header("Fade")]
    [SerializeField] private float fadeInDuration  = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    private GameObject     videoPanel;
    private RawImage       videoDisplay;
    private Image          fadeOverlay;   // overlay preto para fade in/out
    private Text           skipText;
    private VideoPlayer    videoPlayer;
    private AudioSource    videoAudioSource;
    private RenderTexture  renderTexture;
    private bool           isPlaying;
    private System.Action  onVideoFinishedCallback;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildUI();
        SetupVideoPlayer();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("MemoryCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Dark panel background
        videoPanel = new GameObject("VideoPanel");
        videoPanel.transform.SetParent(canvasGO.transform, false);
        var panelImg = videoPanel.AddComponent<Image>();
        panelImg.color = Color.black;
        var panelRect = videoPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Video display
        var displayGO = new GameObject("VideoDisplay");
        displayGO.transform.SetParent(videoPanel.transform, false);
        videoDisplay = displayGO.AddComponent<RawImage>();
        var displayRect = displayGO.GetComponent<RectTransform>();
        displayRect.anchorMin = Vector2.zero;
        displayRect.anchorMax = Vector2.one;
        displayRect.offsetMin = new Vector2(0, 40);
        displayRect.offsetMax = Vector2.zero;

        // Skip hint
        var skipGO = new GameObject("SkipText");
        skipGO.transform.SetParent(videoPanel.transform, false);
        skipText = skipGO.AddComponent<Text>();
        skipText.text = "Pressiona ESPAÇO para saltar";
        skipText.alignment = TextAnchor.MiddleCenter;
        skipText.color = new Color(1f, 1f, 1f, 0.5f);
        skipText.fontSize = 14;
        skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var skipRect = skipGO.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0, 0);
        skipRect.anchorMax = new Vector2(1, 0);
        skipRect.offsetMin = new Vector2(0, 0);
        skipRect.offsetMax = new Vector2(0, 30);

        // Fade overlay — fica no topo, começa preto e faz fade
        var fadeGO = new GameObject("FadeOverlay");
        fadeGO.transform.SetParent(videoPanel.transform, false);
        fadeOverlay = fadeGO.AddComponent<Image>();
        fadeOverlay.color = Color.black;
        var fadeRect = fadeGO.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;

        videoPanel.SetActive(false);
    }

    void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake   = false;
        videoPlayer.renderMode    = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.timeReference = VideoTimeReference.Freerun;

        videoAudioSource = gameObject.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        renderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = renderTexture;
        if (videoDisplay != null) videoDisplay.texture = renderTexture;

        videoPlayer.loopPointReached += _ => StartCoroutine(FadeOutAndClose());
    }

    void ClearRenderTexture()
    {
        if (renderTexture != null)
        {
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }
    }

    public void PlayVideo(int index)
    {
        int slot = index - 1;
        if (slot < 0 || slot >= videoClips.Length || videoClips[slot] == null)
        {
            Debug.LogWarning($"[MemoryVideoPlayer] No video clip assigned for boss {index}.");
            return;
        }

        ClearRenderTexture();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClips[slot];
        videoDisplay.texture = renderTexture;
        StartCoroutine(PlayWithFade());
    }

    public void PlayVideoByName(string videoFileName, System.Action onComplete = null)
    {
        onVideoFinishedCallback = onComplete;
        ClearRenderTexture();

        string pathStr = System.IO.Path.Combine(Application.streamingAssetsPath, "videos", videoFileName + ".mp4");
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = pathStr;
        videoDisplay.texture = renderTexture;
        StartCoroutine(PlayWithFade());
    }

    private IEnumerator PlayWithFade()
    {
        // Mostrar painel com overlay preto
        SetOverlayAlpha(1f);
        videoPanel.SetActive(true);
        isPlaying = true;
        Time.timeScale = 0f;
        videoPlayer.Play();

        // Fade in — overlay vai de preto para transparente
        yield return StartCoroutine(FadeOverlay(1f, 0f, fadeInDuration));
    }

    void Update()
    {
        if (isPlaying && UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            StartCoroutine(FadeOutAndClose());
    }

    private IEnumerator FadeOutAndClose()
    {
        if (!isPlaying) yield break;

        // Fade out — overlay vai de transparente para preto
        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeOutDuration));

        CloseVideo();
    }

    void CloseVideo()
    {
        if (!isPlaying) return;
        isPlaying = false;
        videoPlayer.Stop();
        videoPanel.SetActive(false);
        Time.timeScale = 1f;

        var callback = onVideoFinishedCallback;
        onVideoFinishedCallback = null;
        callback?.Invoke();
    }

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float elapsed = 0f;
        SetOverlayAlpha(from);
        
        float startVol = 1f - from;
        float endVol = 1f - to;
        if (videoAudioSource != null) videoAudioSource.volume = startVol;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetOverlayAlpha(Mathf.Lerp(from, to, t));
            
            if (videoAudioSource != null)
                videoAudioSource.volume = Mathf.Lerp(startVol, endVol, t);
                
            yield return null;
        }
        SetOverlayAlpha(to);
        if (videoAudioSource != null) videoAudioSource.volume = endVol;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (fadeOverlay != null)
            fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
    }
}
