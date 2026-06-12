using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class NewGameControlsSceneController : MonoBehaviour
{
    private const string LoadingSceneName = "MainMenuScene";
    private const string PendingSceneKey = "__PENDING_LOAD_SCENE__";
    private const string PreviewCanvasName = "ControlsIntroCanvas";

    [SerializeField] private Sprite menuBackgroundSprite;
    [SerializeField] private Sprite vignetteSprite;
    [SerializeField] private string gameplayStartSceneName = "Beco";
    [SerializeField] private float continueInputDelay = 0.35f;
    [SerializeField] private float promptPulseSpeed = 1.2f;
    [SerializeField] private float autosaveSpinSpeed = 160f;

    private RectTransform autosaveRing;
    private CanvasGroup promptCanvasGroup;
    private float canContinueAt;
    private bool transitionStarted;

    private void Awake()
    {
        RebuildSceneUi();
        if (Application.isPlaying)
        {
            canContinueAt = Time.unscaledTime + continueInputDelay;
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            RebuildSceneUi();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EditorApplication.delayCall -= RebuildEditorPreview;
        EditorApplication.delayCall += RebuildEditorPreview;
    }

    private void RebuildEditorPreview()
    {
        if (this == null || Application.isPlaying)
        {
            return;
        }

        RebuildSceneUi();
    }
#endif

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (autosaveRing != null)
        {
            autosaveRing.Rotate(0f, 0f, autosaveSpinSpeed * Time.unscaledDeltaTime);
        }

        if (promptCanvasGroup != null)
        {
            float pulse = 0.6f + 0.4f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * promptPulseSpeed * Mathf.PI * 2f));
            promptCanvasGroup.alpha = pulse;
        }

        if (transitionStarted || Time.unscaledTime < canContinueAt)
        {
            return;
        }

        if (WasContinuePressedThisFrame())
        {
            transitionStarted = true;
            BeginGameplayLoading();
        }
    }

    private void BeginGameplayLoading()
    {
        string targetScene = string.IsNullOrWhiteSpace(gameplayStartSceneName) ? "Beco" : gameplayStartSceneName;
        GlobalSceneTransitionRequest.PendingSceneName = targetScene;
        PlayerPrefs.SetString(PendingSceneKey, targetScene);
        PlayerPrefs.Save();
        RuntimeSceneTransitionOverlay.HideOverlay();
        SceneManager.LoadSceneAsync(LoadingSceneName);
    }

    private void RebuildSceneUi()
    {
        CleanupExistingPreview();
        BuildSceneUi();
    }

    private void CleanupExistingPreview()
    {
        Transform existing = transform.Find(PreviewCanvasName);
        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existing.gameObject);
        }
        else
        {
            DestroyImmediate(existing.gameObject);
        }
    }

    private void BuildSceneUi()
    {
        EnsureCamera();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite autosaveSprite = Resources.Load<Sprite>("loading_wine_ring");

        GameObject canvasGo = new GameObject(PreviewCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject background = CreateUiObject("Background", canvasGo.transform);
        StretchFull(background);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = menuBackgroundSprite;
        backgroundImage.color = menuBackgroundSprite != null ? Color.white : new Color(0.05f, 0.03f, 0.03f, 1f);
        backgroundImage.preserveAspect = false;

        GameObject vignette = CreateUiObject("Vignette", background.transform);
        StretchFull(vignette);
        Image vignetteImage = vignette.AddComponent<Image>();
        vignetteImage.sprite = vignetteSprite;
        vignetteImage.color = vignetteSprite != null ? new Color(1f, 1f, 1f, 0.92f) : new Color(0.16f, 0.03f, 0.01f, 0.45f);
        vignetteImage.preserveAspect = false;

        GameObject frame = CreateUiObject("Frame", canvasGo.transform);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(1420f, 900f);
        Image frameImage = frame.AddComponent<Image>();
        frameImage.color = new Color(0.15f, 0.07f, 0.05f, 0.94f);
        Outline frameOutline = frame.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.60f, 0.38f, 0.14f, 0.9f);
        frameOutline.effectDistance = new Vector2(2f, -2f);

        CreateLabel(frame.transform, "Eyebrow", UiText.LocalizeKnownText("ANTES DE COMECAR"), font, 24, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Color(0.88f, 0.73f, 0.50f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(520f, 30f));
        CreateLabel(frame.transform, "Title", UiText.LocalizeKnownText("CONTROLOS"), font, 50, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Color(0.96f, 0.90f, 0.78f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(760f, 58f));
        CreateLabel(frame.transform, "Subtitle", UiText.LocalizeKnownText("Aprende as teclas antes de entrar no jogo."), font, 18, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Color(0.84f, 0.78f, 0.68f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -144f), new Vector2(720f, 24f));

        GameObject leftColumn = CreateColumn(frame.transform, "LeftColumn", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -200f), new Vector2(560f, 430f));
        GameObject rightColumn = CreateColumn(frame.transform, "RightColumn", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-86f, -200f), new Vector2(560f, 430f));

        CreateSectionTitle(leftColumn.transform, font, UiText.LocalizeKnownText("MOVIMENTO"));
        CreateMovementCluster(leftColumn.transform, font);
        CreateControlRow(leftColumn.transform, font, UiText.LocalizeKnownText("CORRER"), MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Run));
        CreateControlRow(leftColumn.transform, font, UiText.LocalizeKnownText("SALTAR"), MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Jump));
        CreateControlRow(leftColumn.transform, font, UiText.LocalizeKnownText("ROLAR"), MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Roll));

        CreateSectionTitle(rightColumn.transform, font, UiText.LocalizeKnownText("AÇÕES"));
        CreateControlRow(rightColumn.transform, font, UiText.LocalizeKnownText("INTERAGIR"), MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact));
        CreateControlRow(rightColumn.transform, font, UiText.LocalizeKnownText("ATAQUE"), "MOUSE LEFT");
        CreateControlRow(rightColumn.transform, font, UiText.LocalizeKnownText("ATAQUE PESADO"), "MOUSE RIGHT");
        CreateControlRow(rightColumn.transform, font, UiText.LocalizeKnownText("DEFENDER"), MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Defend));
        CreateControlRow(rightColumn.transform, font, UiText.LocalizeKnownText("POÇÃO PEQUENA"), "Q");
        CreateControlRow(rightColumn.transform, font, UiText.LocalizeKnownText("POÇÃO GRANDE"), "E");
        CreateControlRow(rightColumn.transform, font, UiText.LocalizeKnownText("PAUSA"), "ESC");

        GameObject autosavePanel = CreateUiObject("AutosavePanel", frame.transform);
        RectTransform autosavePanelRect = autosavePanel.GetComponent<RectTransform>();
        autosavePanelRect.anchorMin = new Vector2(0.5f, 0f);
        autosavePanelRect.anchorMax = new Vector2(0.5f, 0f);
        autosavePanelRect.pivot = new Vector2(0.5f, 0f);
        autosavePanelRect.anchoredPosition = new Vector2(0f, 98f);
        autosavePanelRect.sizeDelta = new Vector2(1140f, 76f);
        Image autosavePanelImage = autosavePanel.AddComponent<Image>();
        autosavePanelImage.color = new Color(0.18f, 0.08f, 0.06f, 0.96f);
        Outline autosaveOutline = autosavePanel.AddComponent<Outline>();
        autosaveOutline.effectColor = new Color(0.58f, 0.36f, 0.13f, 0.8f);
        autosaveOutline.effectDistance = new Vector2(2f, -2f);

        GameObject autosaveIconRoot = CreateUiObject("AutosaveIconRoot", autosavePanel.transform);
        RectTransform autosaveIconRootRect = autosaveIconRoot.GetComponent<RectTransform>();
        autosaveIconRootRect.anchorMin = new Vector2(0f, 0.5f);
        autosaveIconRootRect.anchorMax = new Vector2(0f, 0.5f);
        autosaveIconRootRect.pivot = new Vector2(0f, 0.5f);
        autosaveIconRootRect.anchoredPosition = new Vector2(20f, 0f);
        autosaveIconRootRect.sizeDelta = new Vector2(52f, 52f);

        GameObject autosaveRingGo = CreateUiObject("AutosaveRing", autosaveIconRoot.transform);
        autosaveRing = autosaveRingGo.GetComponent<RectTransform>();
        autosaveRing.anchorMin = new Vector2(0.5f, 0.5f);
        autosaveRing.anchorMax = new Vector2(0.5f, 0.5f);
        autosaveRing.pivot = new Vector2(0.5f, 0.5f);
        autosaveRing.anchoredPosition = Vector2.zero;
        autosaveRing.sizeDelta = new Vector2(42f, 42f);
        Image autosaveImage = autosaveRingGo.AddComponent<Image>();
        autosaveImage.sprite = autosaveSprite;
        autosaveImage.preserveAspect = true;
        autosaveImage.color = new Color(1f, 1f, 1f, 0.96f);

        CreateLabel(autosavePanel.transform, "AutosaveTitle", UiText.LocalizeKnownText("AUTOSAVE"), font, 20, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Color(0.96f, 0.90f, 0.78f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(150f, 24f));
        CreateLabel(autosavePanel.transform, "AutosaveBody", UiText.LocalizeKnownText("Quando vires este circulo a rodar, o jogo está a guardar automaticamente"), font, 14, FontStyle.Normal, TextAnchor.MiddleLeft,
            new Color(0.84f, 0.78f, 0.68f, 1f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(218f, 0f), new Vector2(-242f, 24f));

        GameObject prompt = CreateLabel(frame.transform, "Prompt", UiText.LocalizeKnownText("PRESS SPACE PARA CONTINUAR"), font, 24, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Color(0.98f, 0.91f, 0.76f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(760f, 34f));
        promptCanvasGroup = prompt.AddComponent<CanvasGroup>();
    }

    private void EnsureCamera()
    {
        Camera existingCamera = FindAnyObjectByType<Camera>();
        if (existingCamera != null)
        {
            existingCamera.clearFlags = CameraClearFlags.SolidColor;
            existingCamera.backgroundColor = new Color(0.02f, 0.01f, 0.01f, 1f);
            existingCamera.transform.position = new Vector3(0f, 0f, -10f);
            existingCamera.transform.rotation = Quaternion.identity;
            return;
        }

        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(transform, false);
        Camera cameraComponent = cameraGo.AddComponent<Camera>();
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.backgroundColor = new Color(0.02f, 0.01f, 0.01f, 1f);
        cameraComponent.transform.position = new Vector3(0f, 0f, -10f);
        cameraComponent.transform.rotation = Quaternion.identity;
        cameraGo.AddComponent<AudioListener>();
    }

    private static GameObject CreateColumn(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject column = CreateUiObject(name, parent);
        RectTransform rect = column.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x == 0f ? 0f : 1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 6f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        return column;
    }

    private static void CreateSectionTitle(Transform parent, Font font, string text)
    {
        GameObject title = CreateUiObject(text + "Title", parent);
        LayoutElement layout = title.AddComponent<LayoutElement>();
        layout.preferredHeight = 24f;

        Text label = title.AddComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = new Color(0.92f, 0.80f, 0.58f, 1f);
    }

    private static void CreateMovementCluster(Transform parent, Font font)
    {
        GameObject cluster = CreateUiObject("MovementCluster", parent);
        LayoutElement layout = cluster.AddComponent<LayoutElement>();
        layout.preferredHeight = 128f;

        Image bg = cluster.AddComponent<Image>();
        ApplyCardFrame(cluster, bg, new Color(0.15f, 0.075f, 0.055f, 0.92f));

        CreateLabel(cluster.transform, "MovementLabel", UiText.LocalizeKnownText("ANDAR"), font, 22, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Color(0.90f, 0.86f, 0.80f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(180f, 28f));

        GameObject keysRoot = CreateUiObject("MovementKeys", cluster.transform);
        RectTransform keysRootRect = keysRoot.GetComponent<RectTransform>();
        keysRootRect.anchorMin = new Vector2(1f, 0.5f);
        keysRootRect.anchorMax = new Vector2(1f, 0.5f);
        keysRootRect.pivot = new Vector2(1f, 0.5f);
        keysRootRect.anchoredPosition = new Vector2(-26f, 0f);
        keysRootRect.sizeDelta = new Vector2(200f, 96f);

        CreateKeyBox(keysRoot.transform, font, "W", new Vector2(0f, 28f), new Vector2(52f, 24f));
        CreateKeyBox(keysRoot.transform, font, "A", new Vector2(-56f, -8f), new Vector2(52f, 24f));
        CreateKeyBox(keysRoot.transform, font, "S", new Vector2(0f, -8f), new Vector2(52f, 24f));
        CreateKeyBox(keysRoot.transform, font, "D", new Vector2(56f, -8f), new Vector2(52f, 24f));
    }

    private static void CreateControlRow(Transform parent, Font font, string actionLabel, string keyLabel)
    {
        GameObject row = CreateUiObject(actionLabel + "Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 40f;

        Image rowBg = row.AddComponent<Image>();
        ApplyCardFrame(row, rowBg, new Color(0.15f, 0.075f, 0.055f, 0.92f));

        CreateLabel(row.transform, "Action", actionLabel, font, 22, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Color(0.90f, 0.86f, 0.80f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(236f, 24f));

        GameObject key = CreateUiObject("KeyPill", row.transform);
        RectTransform keyRect = key.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(1f, 0.5f);
        keyRect.anchorMax = new Vector2(1f, 0.5f);
        keyRect.pivot = new Vector2(1f, 0.5f);
        keyRect.anchoredPosition = new Vector2(-36f, 0f);
        keyRect.sizeDelta = new Vector2(144f, 24f);
        BuildKeyVisual(key, font, keyLabel, new Vector2(128f, 18f), 12);
    }

    private static void CreateKeyBox(Transform parent, Font font, string keyLabel, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject key = CreateUiObject(keyLabel + "Key", parent);
        RectTransform keyRect = key.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0.5f, 0.5f);
        keyRect.anchorMax = new Vector2(0.5f, 0.5f);
        keyRect.pivot = new Vector2(0.5f, 0.5f);
        keyRect.anchoredPosition = anchoredPosition;
        keyRect.sizeDelta = size;

        BuildKeyVisual(key, font, keyLabel, new Vector2(size.x - 8f, size.y - 4f), 15);
    }

    private static void BuildKeyVisual(GameObject key, Font font, string keyLabel, Vector2 labelSize, int fontSize)
    {
        Image keyBorder = key.AddComponent<Image>();
        keyBorder.color = new Color(0.72f, 0.50f, 0.18f, 0.95f);

        GameObject keyFill = CreateUiObject("Fill", key.transform);
        RectTransform keyFillRect = keyFill.GetComponent<RectTransform>();
        keyFillRect.anchorMin = Vector2.zero;
        keyFillRect.anchorMax = Vector2.one;
        keyFillRect.offsetMin = new Vector2(2f, 2f);
        keyFillRect.offsetMax = new Vector2(-2f, -2f);
        Image keyFillImage = keyFill.AddComponent<Image>();
        keyFillImage.color = new Color(0.46f, 0.12f, 0.08f, 1f);

        CreateLabel(keyFill.transform, "KeyLabel", keyLabel, font, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Color(0.98f, 0.92f, 0.80f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, labelSize);
    }

    private static GameObject CreateLabel(Transform parent, string name, string text, Font font, int fontSize, FontStyle fontStyle,
        TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject labelGo = CreateUiObject(name, parent);
        RectTransform rect = labelGo.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(
            Mathf.Approximately(anchorMin.x, anchorMax.x) ? anchorMin.x : 0.5f,
            Mathf.Approximately(anchorMin.y, anchorMax.y) ? anchorMin.y : 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text label = labelGo.AddComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return labelGo;
    }

    private static void ApplyCardFrame(GameObject target, Image background, Color fillColor)
    {
        background.color = fillColor;

        Outline outerOutline = target.AddComponent<Outline>();
        outerOutline.effectColor = new Color(0.58f, 0.36f, 0.13f, 0.85f);
        outerOutline.effectDistance = new Vector2(1.4f, -1.4f);

        Shadow innerGlow = target.AddComponent<Shadow>();
        innerGlow.effectColor = new Color(0.92f, 0.70f, 0.32f, 0.12f);
        innerGlow.effectDistance = Vector2.zero;
        innerGlow.useGraphicAlpha = true;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void StretchFull(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static bool WasContinuePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        return false;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
