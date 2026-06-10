using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerDeathSceneLoader : MonoBehaviour
{
    private const string DeathRetryKey    = "__DR__";
    private const string DeathRetrySlotKey = "__DR_SLOT__";
    private const string BecoNoiteSceneName = "beco_noite";
    private const int RetryCoins = 0;
    private const int RetryGems = 0;
    private const int RetryRedPotions = 1;
    private const int RetryBluePotions = 1;
    private const int RetryGreenPotions = 1;

    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private string spawnSceneName    = "masmorra_spawn";
    [SerializeField] private float  deathScreenDelay  = 2.2f;

    private PlayerHealth _playerHealth;
    private bool         _triggered;
    private GameObject   _deathCanvas;

    void Awake() => _playerHealth = GetComponent<PlayerHealth>();

    void OnEnable()
    {
        if (_playerHealth != null)
            _playerHealth.onDeath.AddListener(OnPlayerDeath);
    }

    void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.onDeath.RemoveListener(OnPlayerDeath);
    }

    void OnPlayerDeath()
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(DeathSequence());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  DEATH SEQUENCE  –  wait, then fade the panel in (no ScreenFader wipe)
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator DeathSequence()
    {
        float t = deathScreenDelay;
        while (t > 0f) { t -= Time.unscaledDeltaTime; yield return null; }

        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        BuildDeathUI();

        // Fade the death canvas in (0 → 1 alpha) without using ScreenFader
        var cg = _deathCanvas.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            float dur = 0.5f, el = 0f;
            while (el < dur)
            {
                el += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(el / dur);
                yield return null;
            }
            cg.alpha = 1f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BUILD DEATH UI
    // ─────────────────────────────────────────────────────────────────────────
    void BuildDeathUI()
    {
        EnsureEventSystem();

        Font reg  = Resources.Load<Font>("Fonts/Cinzel-Regular")
                    ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Font bold = Resources.Load<Font>("Fonts/Cinzel-Bold") ?? reg;

        _deathCanvas = new GameObject("PlayerDeathCanvas");
        var canvas = _deathCanvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = _deathCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _deathCanvas.AddComponent<GraphicRaycaster>();

        // CanvasGroup starts invisible – DeathSequence fades it in
        var cg = _deathCanvas.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        var root = _deathCanvas.transform;

        // ── Full-screen dark overlay ──────────────────────────────────────────
        var ov = new GameObject("Overlay");
        ov.transform.SetParent(root, false);
        ov.AddComponent<Image>().color = new Color(0f, 0f, 0f, 1f);
        FillRect(ov.GetComponent<RectTransform>());

        // ── Center panel ─────────────────────────────────────────────────────
        var panel = new GameObject("Panel");
        panel.transform.SetParent(root, false);
        panel.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.97f);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin = pRT.anchorMax = pRT.pivot = new Vector2(0.5f, 0.5f);
        pRT.sizeDelta = new Vector2(500f, 330f);
        pRT.anchoredPosition = Vector2.zero;
        AddBorder(panel.transform, new Color(0.52f, 0.40f, 0.10f, 0.85f), 2f);

        // ── "VOCÊ MORREU" title (TMP) ─────────────────────────────────────────
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var tt = titleGo.AddComponent<TextMeshProUGUI>();
        tt.text      = "VOCÊ MORREU";
        tt.fontSize  = 52;
        tt.fontStyle = FontStyles.Bold;
        tt.alignment = TextAlignmentOptions.Center;
        tt.color     = new Color(0.85f, 0.14f, 0.10f, 1f);
        var tRT = titleGo.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot     = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(0f, 80f);
        tRT.anchoredPosition = new Vector2(0f, -22f);

        // ── Divider ───────────────────────────────────────────────────────────
        var divGo = new GameObject("Divider");
        divGo.transform.SetParent(panel.transform, false);
        divGo.AddComponent<Image>().color = new Color(0.52f, 0.40f, 0.10f, 0.55f);
        var dRT = divGo.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.08f, 1f); dRT.anchorMax = new Vector2(0.92f, 1f);
        dRT.pivot     = new Vector2(0.5f, 1f);
        dRT.sizeDelta = new Vector2(0f, 1.5f);
        dRT.anchoredPosition = new Vector2(0f, -108f);

        // ── Buttons ───────────────────────────────────────────────────────────
        var retry = MakeButton(panel.transform, bold, "Tentar Novamente", new Vector2(0f, 8f));
        retry.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(RetryLevel()));

        var menuBtn = MakeButton(panel.transform, bold, "Menu Principal", new Vector2(0f, -58f));
        menuBtn.GetComponent<Button>().onClick.AddListener(() => StartCoroutine(ExitToMenu()));
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();

        System.Type inputModuleType =
            System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem") ??
            System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI");

        if (inputModuleType != null)
        {
            eventSystemGo.AddComponent(inputModuleType);
        }
        else
        {
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }
    }

    GameObject MakeButton(Transform parent, Font font, string label, Vector2 pos)
    {
        var go = new GameObject(label.Replace(" ", ""));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.04f, 1f);

        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None; // visuals handled via EventTrigger

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(390f, 56f);
        rt.anchoredPosition = pos;

        AddBorder(go.transform, new Color(0.52f, 0.40f, 0.10f, 0.85f), 1.5f);

        // Glow overlay — hidden by default, appears on hover
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(go.transform, false);
        var glowImg = glowGo.AddComponent<Image>();
        glowImg.color = new Color(0.52f, 0.40f, 0.10f, 0f);
        glowImg.raycastTarget = false;
        var glowRT = glowGo.GetComponent<RectTransform>();
        glowRT.anchorMin = Vector2.zero; glowRT.anchorMax = Vector2.one;
        glowRT.sizeDelta = Vector2.zero; glowRT.anchoredPosition = Vector2.zero;

        var lGo = new GameObject("Label");
        lGo.transform.SetParent(go.transform, false);
        var lt = lGo.AddComponent<TextMeshProUGUI>();
        lt.text      = label;
        lt.fontSize  = 22;
        lt.fontStyle = FontStyles.Bold;
        lt.alignment = TextAlignmentOptions.Center;
        lt.color     = new Color(0.89f, 0.84f, 0.72f, 1f); // warm beige (main menu normal)
        var lRT = lGo.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.sizeDelta = Vector2.zero; lRT.anchoredPosition = Vector2.zero;

        // Hover/press animation matching main menu AnimatedMenuButton behaviour
        Color colNormal  = new Color(0.89f, 0.84f, 0.72f, 1f);
        Color colHover   = new Color(1.00f, 0.94f, 0.78f, 1f);
        Color colPressed = new Color(0.84f, 0.75f, 0.56f, 1f);
        Color glowOff    = new Color(0.52f, 0.40f, 0.10f, 0f);
        Color glowOn     = new Color(0.52f, 0.40f, 0.10f, 0.45f);

        var trig = go.AddComponent<EventTrigger>();
        AddEvt(trig, EventTriggerType.PointerEnter, _ => { lt.color = colHover;   glowImg.color = glowOn;  rt.localScale = Vector3.one * 1.04f; });
        AddEvt(trig, EventTriggerType.PointerExit,  _ => { lt.color = colNormal;  glowImg.color = glowOff; rt.localScale = Vector3.one; });
        AddEvt(trig, EventTriggerType.PointerDown,  _ => { lt.color = colPressed; rt.localScale = Vector3.one * 0.97f; });
        AddEvt(trig, EventTriggerType.PointerUp,    _ => { lt.color = colHover;   rt.localScale = Vector3.one * 1.04f; });

        return go;
    }

    static void AddEvt(EventTrigger t, EventTriggerType type, System.Action<BaseEventData> cb)
    {
        var e = new EventTrigger.Entry { eventID = type };
        e.callback.AddListener(d => cb(d));
        t.triggers.Add(e);
    }

    void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
    }

    void AddBorder(Transform parent, Color col, float t)
    {
        void Ln(string n, Vector2 aMin, Vector2 aMax, Vector2 piv, bool h)
        {
            var ln = new GameObject(n); ln.transform.SetParent(parent, false);
            var img = ln.AddComponent<Image>(); img.color = col; img.raycastTarget = false;
            var rt = ln.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = piv;
            rt.sizeDelta = h ? new Vector2(0, t) : new Vector2(t, 0);
            rt.anchoredPosition = Vector2.zero;
        }
        Ln("BT", new Vector2(0,1), new Vector2(1,1), new Vector2(.5f,1), true);
        Ln("BB", new Vector2(0,0), new Vector2(1,0), new Vector2(.5f,0), true);
        Ln("BL", new Vector2(0,0), new Vector2(0,1), new Vector2(0,.5f), false);
        Ln("BR", new Vector2(1,0), new Vector2(1,1), new Vector2(1,.5f), false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RETRY  –  fade to black → set flag → load main menu
    //            PostDeathFader hands off to the main menu loading screen
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator RetryLevel()
    {
        foreach (var b in _deathCanvas.GetComponentsInChildren<Button>()) b.interactable = false;

        var fader = ScreenFader.GetOrCreate();
        yield return fader.FadeOut(0.45f);

        if (_deathCanvas != null) Destroy(_deathCanvas);

        string retrySceneName = ResolveRetrySceneName();
        PersistRetrySlotAndInventory();
        SaveManager.SaveActiveSlotFromCurrentScene(retrySceneName);

        PlayerPrefs.SetInt(DeathRetryKey, 1);
        PlayerPrefs.SetInt("JustRespawned", 1);
        PlayerPrefs.Save();

        var loaderGo = new GameObject("__PostDeathFader__");
        DontDestroyOnLoad(loaderGo);
        var pf = loaderGo.AddComponent<PostDeathFader>();
        pf.IsRetry   = true;
        pf.SpawnScene = retrySceneName;

        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EXIT TO MAIN MENU  –  clean fade, no retry logic
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator ExitToMenu()
    {
        foreach (var b in _deathCanvas.GetComponentsInChildren<Button>()) b.interactable = false;

        var fader = ScreenFader.GetOrCreate();
        yield return fader.FadeOut(0.45f);

        if (_deathCanvas != null) Destroy(_deathCanvas);

        string resumeSceneName = ResolveRetrySceneName();
        PersistRetrySlotAndInventory();
        SaveManager.SaveActiveSlotFromCurrentScene(resumeSceneName);

        var loaderGo = new GameObject("__PostDeathFader__");
        DontDestroyOnLoad(loaderGo);
        loaderGo.AddComponent<PostDeathFader>();   // IsRetry = false by default

        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }

    private string ResolveRetrySceneName()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == BecoNoiteSceneName)
        {
            return BecoNoiteSceneName;
        }

        return string.IsNullOrWhiteSpace(spawnSceneName) ? "masmorra_spawn" : spawnSceneName;
    }

    private void PersistRetrySlotAndInventory()
    {
        int activeSlot = SaveManager.GetActiveSlot();
        PlayerPrefs.SetInt(DeathRetrySlotKey, activeSlot);

        if (SceneManager.GetActiveScene().name == BecoNoiteSceneName)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.SaveToPrefs();
            }

            return;
        }

        LevelCompletionState.ClearForSlot(activeSlot);
        PlayerInventory.ResetPrefsForSlot(
            activeSlot,
            RetryCoins,
            RetryGems,
            RetryRedPotions,
            RetryBluePotions,
            RetryGreenPotions);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Survives scene loads (DontDestroyOnLoad).
//  • IsRetry = true  → fades ScreenFader out while the main menu loading screen
//                       takes over, then triggers ShowLoadingThenLoadScene.
//                       Destroys itself once the spawn scene finishes loading.
//  • IsRetry = false → fades ScreenFader back in, then destroys itself.
// ─────────────────────────────────────────────────────────────────────────────
public class PostDeathFader : MonoBehaviour
{
    public bool   IsRetry;
    public string SpawnScene = "masmorra_spawn";

    // PlayLoadingIntro takes ~1.42s (FadeToBlack 0.22 + FadeCanvasGroup 0.4 + AnimateIntro 0.8)
    // before FadeFromBlack reveals the loading panel. Start revealing ScreenFader at 1.4s
    // so both faders uncover the loading screen at the same time.
    private const float ScreenFaderRevealDelay    = 1.4f;
    private const float ScreenFaderRevealDuration = 0.22f;

    private bool _usedFallback;

    void OnEnable()
    {
        if (IsRetry) SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenuScene")
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_usedFallback)
                StartCoroutine(FallbackFadeAndDestroy());
            else
                Destroy(gameObject); // ScreenFader already transparent from delayed reveal
        }
    }

    IEnumerator Start()
    {
        while (SceneManager.GetActiveScene().name != "MainMenuScene")
        {
            yield return null;
        }

        yield return null; // one frame – let the new scene initialise

        if (IsRetry)
        {
            var loader = Object.FindObjectOfType<LoadingScreenController>();
            if (loader != null)
            {
                // Delay ScreenFader reveal until LoadingPanel is visible behind it
                StartCoroutine(DelayedScreenFaderReveal());
                loader.ShowLoadingThenLoadScene(SpawnScene);
            }
            else
            {
                // Fallback: load directly; OnSceneLoaded fades ScreenFader in the spawn scene
                _usedFallback = true;
                float t = 0.35f;
                while (t > 0f) { t -= Time.unscaledDeltaTime; yield return null; }
                SceneManager.LoadSceneAsync(SpawnScene);
            }
        }
        else
        {
            var fader = ScreenFader.Instance;
            if (fader != null) yield return fader.FadeIn(0.55f);
            Destroy(gameObject);
        }
    }

    IEnumerator DelayedScreenFaderReveal()
    {
        float t = ScreenFaderRevealDelay;
        while (t > 0f) { t -= Time.unscaledDeltaTime; yield return null; }
        var fader = ScreenFader.Instance;
        if (fader != null) yield return fader.FadeIn(ScreenFaderRevealDuration);
    }

    IEnumerator FallbackFadeAndDestroy()
    {
        var fader = ScreenFader.Instance;
        if (fader != null) yield return fader.FadeIn(0.5f);
        Destroy(gameObject);
    }
}
