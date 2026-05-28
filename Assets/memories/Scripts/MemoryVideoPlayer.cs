using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MemoryVideoPlayer : MonoBehaviour
{
    public static MemoryVideoPlayer Instance { get; private set; }

    [Header("Video Clips")]
    [Tooltip("Slot 0 = video for boss 1, Slot 1 = boss 2, Slot 2 = boss 3")]
    public VideoClip[] videoClips = new VideoClip[3];

    // UI references — assigned at runtime when the prefab is built
    private GameObject     videoPanel;
    private RawImage       videoDisplay;
    private Text           skipText;
    private VideoPlayer    videoPlayer;
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
        // Canvas
        var canvasGO = new GameObject("MemoryCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Dark panel
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

        videoPanel.SetActive(false);
    }

    void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake   = false;
        videoPlayer.renderMode    = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.timeReference = VideoTimeReference.Freerun; // Ignora o timeScale = 0

        var audioSource = gameObject.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audioSource);

        renderTexture = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = renderTexture;
        if (videoDisplay != null) videoDisplay.texture = renderTexture;

        videoPlayer.loopPointReached += _ => CloseVideo();
    }

    public void PlayVideo(int index)
    {
        int slot = index - 1;
        if (slot < 0 || slot >= videoClips.Length || videoClips[slot] == null)
        {
            Debug.LogWarning($"[MemoryVideoPlayer] No video clip assigned for boss {index}. Add clips in the Inspector.");
            return;
        }

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClips[slot];
        videoDisplay.texture = renderTexture;
        videoPanel.SetActive(true);
        isPlaying = true;
        Time.timeScale = 0f;
        videoPlayer.Play();
    }

    public void PlayVideoByName(string videoFileName, System.Action onComplete = null)
    {
        onVideoFinishedCallback = onComplete;
        
        string pathStr = System.IO.Path.Combine(Application.dataPath, "videos", videoFileName + ".mp4");
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = pathStr;
        
        videoDisplay.texture = renderTexture;
        videoPanel.SetActive(true);
        isPlaying = true;
        Time.timeScale = 0f;
        videoPlayer.Play();
    }

    void Update()
    {
        if (isPlaying && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
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
}
