using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class LimoDayInteractable : MonoBehaviour
{
    private const string SkipSplashKey = "__SKIP_SPLASH__";

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2.5f;
    [SerializeField] private Transform doorAnchor;
    [SerializeField] private Vector3 doorPromptLocalOffset = new Vector3(0f, 0.9f, 1.5f);

    [Header("Sequence")]
    [SerializeField] private float entryHoldTime = 2.5f;
    [SerializeField] private float driveSpeed = 9f;
    [SerializeField] private float cameraPitch = 8f;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Credits — Logo")]
    [SerializeField] private Sprite logoSprite;
    [SerializeField] private float logoFadeIn = 1.2f;
    [SerializeField] private float logoHoldTime = 2.5f;
    [SerializeField] private float logoFadeOut = 1f;

    [Header("Credits — Music")]
    [SerializeField] private AudioClip creditsMusic;
    [SerializeField] private float creditsMusicVolume = 0.75f;

    [Header("Credits — Text")]
    [SerializeField] private float creditsScrollSpeed = 55f;
    [SerializeField, TextArea(6, 20)] private string creditsText =
        "THE LAST SIP\n\n\n" +
        "Desenvolvido por\n" +
        "Francisco Dias\n\n\n" +
        "Orientação\n" +
        "Professor\n\n\n" +
        "Agradecimentos\n" +
        "...\n\n\n\n\n\n";

    private bool _sequenceStarted;
    private GameObject _player;
    private Canvas _promptCanvas;
    private RectTransform _promptRT;

    void Start()
    {
        BuildPrompt();
    }

    void Update()
    {
        if (_sequenceStarted)
        {
            if (_promptCanvas != null) _promptCanvas.gameObject.SetActive(false);
            return;
        }

        bool inRange = IsPlayerInRange();

        if (_promptCanvas != null)
        {
            _promptCanvas.gameObject.SetActive(inRange);
            if (inRange && Camera.main != null)
            {
                Vector3 doorWorld = doorAnchor != null
                    ? doorAnchor.position
                    : transform.TransformPoint(doorPromptLocalOffset);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(doorWorld);
                if (screenPos.z > 0f)
                    _promptRT.position = screenPos;
            }
        }

        if (inRange && IsFPressed())
        {
            UserImportedPlayerController ctrl = GetController();
            if (ctrl != null)
            {
                _sequenceStarted = true;
                StartCoroutine(PlaySequence(ctrl));
            }
        }
    }

    private static bool IsFPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current?.fKey.wasPressedThisFrame ?? false;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    private UserImportedPlayerController GetController()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player");
        return _player != null ? _player.GetComponent<UserImportedPlayerController>() : null;
    }

    private IEnumerator PlaySequence(UserImportedPlayerController controller)
    {
        controller.SetExternalControlLocked(true);
        CharacterController cc = controller.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        controller.SetVisualVisible(false);
        controller.PlayExternalAnimation("idle");

        yield return new WaitForSeconds(entryHoldTime);

        GameObject anchor = new GameObject("LimoDayCameraAnchor");
        anchor.transform.position = transform.position + new Vector3(-8f, 3f, 0f);
        anchor.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        controller.SetCameraFollowOverride(anchor.transform, true, cameraPitch);

        // Fullscreen black canvas — stays visible through credits
        Canvas screenCanvas = new GameObject("LimoDayScreen").AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 999;
        screenCanvas.gameObject.AddComponent<CanvasScaler>();
        DontDestroyOnLoad(screenCanvas.gameObject);

        Image bg = new GameObject("Bg").AddComponent<Image>();
        bg.transform.SetParent(screenCanvas.transform, false);
        FillRect(bg.rectTransform);
        bg.color = Color.clear;
        bg.raycastTarget = false;

        // Drive limo + fade to black
        float destX = transform.position.x + 50f;
        float driveTimer = 0f;
        float fadeAlpha = 0f;

        while (fadeAlpha < 1f)
        {
            if (Mathf.Abs(transform.position.x - destX) > 0.05f)
                transform.position += Vector3.right * driveSpeed * Time.deltaTime;

            driveTimer += Time.deltaTime;
            if (driveTimer >= 2.5f)
            {
                fadeAlpha = Mathf.Clamp01(fadeAlpha + Time.deltaTime / 2f);
                bg.color = new Color(0f, 0f, 0f, fadeAlpha);
            }

            yield return null;
        }

        bg.color = Color.black;

        controller.ClearCameraFollowOverride();
        Destroy(anchor);

        RuntimeSceneTransitionOverlay.ShowBlackForSceneLoad();
        Destroy(screenCanvas.gameObject);
        PlayerPrefs.SetInt(SkipSplashKey, 1);
        PlayerPrefs.Save();
        GlobalSceneTransitionRequest.PendingOpenCredits = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator ShowCredits(Canvas canvas)
    {
        // Stop any game audio
        AudioListener.pause = false;

        // Start credits music
        AudioSource audioSrc = null;
        if (creditsMusic != null)
        {
            audioSrc = canvas.gameObject.AddComponent<AudioSource>();
            audioSrc.clip = creditsMusic;
            audioSrc.loop = false;
            audioSrc.volume = 0f;
            audioSrc.Play();
            // Fade music in
            float mv = 0f;
            while (mv < creditsMusicVolume)
            {
                mv = Mathf.MoveTowards(mv, creditsMusicVolume, Time.deltaTime * 0.5f);
                audioSrc.volume = mv;
                yield return null;
            }
        }

        // Logo
        if (logoSprite != null)
        {
            GameObject logoGO = new GameObject("Logo");
            logoGO.transform.SetParent(canvas.transform, false);
            Image logoImg = logoGO.AddComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            RectTransform lrt = logoImg.rectTransform;
            lrt.anchorMin = new Vector2(0.25f, 0.25f);
            lrt.anchorMax = new Vector2(0.75f, 0.75f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            logoImg.color = new Color(1f, 1f, 1f, 0f);

            yield return FadeImage(logoImg, 0f, 1f, logoFadeIn);
            yield return new WaitForSeconds(logoHoldTime);
            yield return FadeImage(logoImg, 1f, 0f, logoFadeOut);

            Destroy(logoGO);
        }

        // Scrolling credits text
        GameObject textGO = new GameObject("CreditsText");
        textGO.transform.SetParent(canvas.transform, false);

        Text label = textGO.AddComponent<Text>();
        label.font = Font.CreateDynamicFontFromOSFont("Arial", 30);
        label.fontSize = 30;
        label.alignment = TextAnchor.UpperCenter;
        label.color = Color.white;
        label.text = creditsText;
        label.supportRichText = true;
        label.lineSpacing = 1.4f;

        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0.15f, 0f);
        rt.anchorMax = new Vector2(0.85f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, 4000f);

        Canvas.ForceUpdateCanvases();
        float textH = LayoutUtility.GetPreferredHeight(rt);
        rt.sizeDelta = new Vector2(0f, textH);

        float screenH = Screen.height;
        rt.anchoredPosition = new Vector2(0f, -screenH);

        float elapsed = 0f;
        float duration = (textH + screenH) / creditsScrollSpeed;
        bool skipped = false;

        while (elapsed < duration && !skipped)
        {
            rt.anchoredPosition = new Vector2(0f, -screenH + creditsScrollSpeed * elapsed);
            elapsed += Time.deltaTime;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current?.anyKey.wasPressedThisFrame ?? false) skipped = true;
#else
            if (Input.anyKeyDown) skipped = true;
#endif
            yield return null;
        }

        // Fade out text + music
        float t = 0f;
        float startVol = audioSrc != null ? audioSrc.volume : 0f;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.deltaTime * 1.5f, 1f);
            label.color = Color.Lerp(Color.white, Color.clear, t);
            if (audioSrc != null) audioSrc.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        if (audioSrc != null) audioSrc.Stop();
        Destroy(textGO);
    }

    private static IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        float t = 0f;
        Color c = img.color;
        while (t < 1f)
        {
            t = Mathf.Min(t + Time.deltaTime / duration, 1f);
            c.a = Mathf.Lerp(from, to, t);
            img.color = c;
            yield return null;
        }
    }

    private void BuildPrompt()
    {
        _promptCanvas = new GameObject("LimoDoorPrompt").AddComponent<Canvas>();
        _promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _promptCanvas.sortingOrder = 200;
        _promptCanvas.gameObject.AddComponent<CanvasScaler>();

        GameObject box = new GameObject("Box");
        box.transform.SetParent(_promptCanvas.transform, false);
        _promptRT = box.AddComponent<RectTransform>();
        _promptRT.sizeDelta = new Vector2(72f, 72f);
        _promptRT.pivot = new Vector2(0.5f, 0f);

        Image boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject fGO = new GameObject("F");
        fGO.transform.SetParent(box.transform, false);
        Text fText = fGO.AddComponent<Text>();
        fText.font = Font.CreateDynamicFontFromOSFont("Arial", 38);
        fText.fontSize = 38;
        fText.fontStyle = FontStyle.Bold;
        fText.alignment = TextAnchor.UpperCenter;
        fText.color = Color.white;
        fText.text = "F";
        RectTransform fRT = fText.rectTransform;
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.offsetMin = new Vector2(2f, 18f);
        fRT.offsetMax = new Vector2(-2f, -4f);

        GameObject hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(box.transform, false);
        Text hint = hintGO.AddComponent<Text>();
        hint.font = Font.CreateDynamicFontFromOSFont("Arial", 13);
        hint.fontSize = 13;
        hint.alignment = TextAnchor.LowerCenter;
        hint.color = new Color(1f, 1f, 1f, 0.8f);
        hint.text = "Entrar";
        RectTransform hintRT = hint.rectTransform;
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0.32f);
        hintRT.offsetMin = Vector2.zero;
        hintRT.offsetMax = Vector2.zero;

        _promptCanvas.gameObject.SetActive(false);
    }

    private static void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private bool IsPlayerInRange()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player");
        if (_player == null) return false;
        Vector3 p = _player.transform.position;
        Vector3 t = transform.position;
        p.y = 0f;
        t.y = 0f;
        return Vector3.Distance(p, t) <= interactionDistance;
    }
}
