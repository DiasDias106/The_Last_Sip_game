#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class PauseMenuBuilder
{
    private const string GeneratedPath = "Assets/UI/MainMenu/Generated/";
    private const string AudioPath = "Assets/Audio/UI_Medieval-Free/";
    private const string MusicPath = "Assets/Audio/ImportedFromZip/Audio/";
    private const string PauseMenuPrefabPath = "Assets/UI/PauseMenu/Prefabs/PauseMenuCanvas.prefab";

    [MenuItem("Tools/The Last Sip/Build Pause Menu")]
    public static void BuildPauseMenu()
    {
        if (!EditorSceneManager.GetActiveScene().isLoaded)
        {
            Debug.LogWarning("Nenhuma scene ativa. Abre a scene alvo e volta a correr o Build Pause Menu.");
            return;
        }

        ConfigureSprites();

        Sprite buttonNormal = LoadSprite("button_normal.png");
        Sprite buttonHover = LoadSprite("button_hover.png");
        Sprite buttonPressed = LoadSprite("button_pressed.png");
        Sprite buttonGlow = LoadSprite("button_glow.png");
        Sprite panelWood = LoadSprite("panel_dark_wood.png");
        Sprite vignette = LoadSprite("vignette_overlay.png");
        Sprite background = LoadOptionalGeneratedSprite("imagem_fundo.png") ?? LoadSprite("bg_placeholder_dark_alley.png");
        Sprite title = LoadSprite("title_the_last_sip.png");
        Sprite iconContinue = LoadSprite("icon_new_game_wine.png");
        Sprite iconSettings = LoadSprite("icon_options_gear.png");
        Sprite iconSave = LoadSprite("icon_credits_document.png");
        Sprite iconExit = LoadSprite("icon_exit_door.png");
        AudioClip ambientMusicClip = LoadMusicClip("becoManha.mp3");
        AudioClip hoverClip = LoadAudioClip("UIClick_UIBton ButtonHover _Tonefirst_Medieval UI-Free.wav");
        AudioClip clickClip = LoadAudioClip("UIClick_UIBton ButtonClick _Tonefirst_Medieval UI-Free.wav");
        TMP_FontAsset tmpFont = ResolveTmpFont();

        EnsureEventSystem();

        GameObject existingCanvas = GameObject.Find("PauseMenuCanvas");
        if (existingCanvas != null)
        {
            Object.DestroyImmediate(existingCanvas);
        }

        GameObject canvasGo = new("PauseMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = canvasGo.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        AudioSource musicSource = canvasGo.AddComponent<AudioSource>();
        AudioSource sfxSource = canvasGo.AddComponent<AudioSource>();
        MenuAudioManager audioManager = canvasGo.AddComponent<MenuAudioManager>();
        SetObjectField(audioManager, "ambientMusicSource", musicSource);
        SetObjectField(audioManager, "uiSfxSource", sfxSource);
        SetObjectField(audioManager, "ambientMusicClip", ambientMusicClip);
        SetObjectField(audioManager, "hoverClip", hoverClip);
        SetObjectField(audioManager, "clickClip", clickClip);

        GameObject overlayGo = CreateUIObject("PauseOverlay", canvasGo.transform);
        StretchFull(RectOf(overlayGo));
        overlayGo.SetActive(false);

        GameObject bgGo = CreateUIObject("Background", overlayGo.transform);
        StretchFull(RectOf(bgGo));
        AddImage(bgGo, background, new Color(1f, 1f, 1f, 0.66f), false).raycastTarget = false;

        GameObject shadeGo = CreateUIObject("Shade", overlayGo.transform);
        StretchFull(RectOf(shadeGo));
        AddImage(shadeGo, null, new Color(0.035f, 0.02f, 0.025f, 0.86f), false).raycastTarget = false;

        GameObject vignetteGo = CreateUIObject("VignetteOverlay", overlayGo.transform);
        StretchFull(RectOf(vignetteGo));
        AddImage(vignetteGo, vignette, Color.white, false).raycastTarget = false;

        GameObject titleGo = CreateUIObject("TitleImage", overlayGo.transform);
        SetRect(RectOf(titleGo), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(280f, 114f));
        AddImage(titleGo, title, Color.white, true).raycastTarget = false;
        AddShadow(titleGo, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -10f));

        GameObject pausePanel = CreatePanel("PausePanel", overlayGo.transform, panelWood, tmpFont, "PAUSA", out GameObject pauseBody);
        SetRect(RectOf(pausePanel), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f, -8f), new Vector2(520f, 530f));

        VerticalLayoutGroup pauseLayout = pauseBody.AddComponent<VerticalLayoutGroup>();
        pauseLayout.spacing = 18f;
        pauseLayout.childAlignment = TextAnchor.UpperCenter;
        pauseLayout.childControlHeight = false;
        pauseLayout.childControlWidth = false;
        pauseLayout.childForceExpandHeight = false;
        pauseLayout.childForceExpandWidth = false;
        ContentSizeFitter pauseFitter = pauseBody.AddComponent<ContentSizeFitter>();
        pauseFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AnimatedMenuButton continueButton = CreateMenuButton(pauseBody.transform, "ContinueButton", "CONTINUAR", iconContinue, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        AnimatedMenuButton saveButton = CreateMenuButton(pauseBody.transform, "SaveButton", "GUARDAR", iconSave, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        AnimatedMenuButton settingsButton = CreateMenuButton(pauseBody.transform, "SettingsButton", "DEFINICOES", iconSettings, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        AnimatedMenuButton exitButton = CreateMenuButton(pauseBody.transform, "ExitButton", "SAIR", iconExit, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);

        GameObject saveFeedbackGo = CreateUIObject("SaveFeedbackText", pausePanel.transform);
        SetRect(RectOf(saveFeedbackGo), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(360f, 28f));
        TMP_Text saveFeedback = AddText(saveFeedbackGo, string.Empty, tmpFont, 18, FontStyles.SmallCaps, TextAlignmentOptions.Center, new Color32(200, 183, 151, 255));
        saveFeedback.characterSpacing = 2f;

        GameObject settingsPanel = CreateFullSettingsPanel("SettingsPanel", overlayGo.transform, panelWood, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, out Toggle fullscreenToggle, out AnimatedMenuButton backButton);
        SetRect(RectOf(settingsPanel), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(1240f, 740f));
        ConfirmOverwritePanel confirmLeaveSettingsPanel = CreateConfirmLeaveSettingsPanel(overlayGo.transform, tmpFont, panelWood, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        SaveSuccessPanel saveSuccessPanel = CreateSaveSuccessPanel(overlayGo.transform, tmpFont, panelWood, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);

        PauseMenuController controller = canvasGo.AddComponent<PauseMenuController>();
        SetObjectField(controller, "pauseOverlayRoot", overlayGo);
        SetObjectField(controller, "overlayCanvasGroup", canvasGroup);
        SetObjectField(controller, "pausePanel", pausePanel.GetComponent<MenuPanelController>());
        SetObjectField(controller, "settingsPanel", settingsPanel.GetComponent<MenuPanelController>());
        SetObjectField(controller, "optionsPanelView", settingsPanel.GetComponent<OptionsPanelView>());
        SetObjectField(controller, "confirmLeaveSettingsPanel", confirmLeaveSettingsPanel);
        SetObjectField(controller, "saveSuccessPanel", saveSuccessPanel);
        SetObjectField(controller, "continueButton", continueButton);
        SetObjectField(controller, "saveButton", saveButton);
        SetObjectField(controller, "settingsButton", settingsButton);
        SetObjectField(controller, "exitButton", exitButton);
        SetObjectField(controller, "settingsBackButton", backButton);
        SetObjectField(controller, "fullscreenToggle", fullscreenToggle);
        SetObjectField(controller, "saveFeedbackText", saveFeedback);
        SetStringField(controller, "mainMenuSceneName", "MainMenuScene");

        SaveOrUpdatePauseMenuPrefab(canvasGo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Pause menu built in scene: {EditorSceneManager.GetActiveScene().name}");
    }

    [MenuItem("Tools/The Last Sip/Update Pause Menu Prefab (Current Scene)")]
    public static void UpdatePauseMenuPrefabCurrentScene()
    {
        GameObject canvasGo = GameObject.Find("PauseMenuCanvas");
        if (canvasGo == null)
        {
            Debug.LogWarning("PauseMenuCanvas nao encontrado na cena ativa.");
            return;
        }

        SaveOrUpdatePauseMenuPrefab(canvasGo);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Pause menu prefab atualizado.");
    }

    [MenuItem("Tools/The Last Sip/Apply Pause Menu Prefab To All Scenes")]
    public static void ApplyPauseMenuPrefabToAllScenes()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PauseMenuPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("Prefab do pause menu nao encontrado. Corre primeiro: Build Pause Menu.");
            return;
        }

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            if (string.IsNullOrEmpty(scenePath))
            {
                continue;
            }

            if (scenePath.EndsWith("/MainMenuScene.unity"))
            {
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject existing = GameObject.Find("PauseMenuCanvas");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                continue;
            }

            instance.name = "PauseMenuCanvas";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Pause menu prefab aplicado a todas as scenes (exceto MainMenuScene).");
    }

    private static void SaveOrUpdatePauseMenuPrefab(GameObject pauseMenuCanvas)
    {
        EnsureFolder("Assets/UI/PauseMenu");
        EnsureFolder("Assets/UI/PauseMenu/Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(pauseMenuCanvas, PauseMenuPrefabPath, InteractionMode.AutomatedAction);
    }

    private static GameObject CreatePanel(string name, Transform parent, Sprite panelWood, TMP_FontAsset tmpFont, string heading, out GameObject bodyGo)
    {
        GameObject panelGo = CreateUIObject(name, parent);
        Image image = AddImage(panelGo, panelWood, new Color(1f, 1f, 1f, 0.96f), false);
        image.type = Image.Type.Sliced;
        AddShadow(panelGo, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -14f));

        CanvasGroup canvasGroup = panelGo.AddComponent<CanvasGroup>();
        MenuPanelController menuPanel = panelGo.AddComponent<MenuPanelController>();
        SetObjectField(menuPanel, "canvasGroup", canvasGroup);
        SetObjectField(menuPanel, "panelTransform", RectOf(panelGo));
        SetVector2Field(menuPanel, "hiddenOffset", new Vector2(0f, -18f));

        GameObject shadeGo = CreateUIObject("PanelShade", panelGo.transform);
        StretchFull(RectOf(shadeGo));
        AddImage(shadeGo, null, new Color(0.08f, 0.04f, 0.03f, 0.34f), false).raycastTarget = false;
        shadeGo.transform.SetAsFirstSibling();

        GameObject headingGo = CreateUIObject("PanelTitle", panelGo.transform);
        SetRect(RectOf(headingGo), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(320f, 44f));
        TMP_Text headingText = AddText(headingGo, heading, tmpFont, 30, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(243, 226, 193, 255));
        headingText.characterSpacing = 8f;

        GameObject dividerGo = CreateUIObject("Divider", panelGo.transform);
        SetRect(RectOf(dividerGo), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(360f, 2f));
        AddImage(dividerGo, null, new Color(0.72f, 0.57f, 0.43f, 0.32f), false).raycastTarget = false;

        bodyGo = CreateUIObject("Body", panelGo.transform);
        SetRect(RectOf(bodyGo), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(420f, 300f));
        return panelGo;
    }

    private static ConfirmOverwritePanel CreateConfirmLeaveSettingsPanel(Transform parent, TMP_FontAsset tmpFont, Sprite panelWood, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, MenuAudioManager audioManager)
    {
        GameObject root = CreateUIObject("ConfirmLeaveSettingsPanel", parent);
        StretchFull(RectOf(root));

        GameObject blocker = CreateUIObject("Blocker", root.transform);
        StretchFull(RectOf(blocker));
        AddImage(blocker, null, new Color(0f, 0f, 0f, 0.66f), false).raycastTarget = true;

        GameObject frame = CreateUIObject("Frame", root.transform);
        SetRect(RectOf(frame), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(920f, 360f));
        Image frameImage = AddImage(frame, panelWood, new Color(1f, 1f, 1f, 0.98f), false);
        frameImage.type = Image.Type.Sliced;
        AddShadow(frame, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -16f));

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        MenuPanelController panel = root.AddComponent<MenuPanelController>();
        SetObjectField(panel, "canvasGroup", canvasGroup);
        SetObjectField(panel, "panelTransform", RectOf(frame));
        SetVector2Field(panel, "hiddenOffset", new Vector2(0f, -20f));

        GameObject messageGo = CreateUIObject("MessageText", frame.transform);
        SetRect(RectOf(messageGo), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(820f, 128f));
        TMP_Text message = AddText(messageGo, "Existem alteracoes por guardar. Deseja sair sem guardar as alteracoes?", tmpFont, 26, FontStyles.SmallCaps, TextAlignmentOptions.Center, new Color32(235, 220, 194, 255));
        message.textWrappingMode = TextWrappingModes.Normal;
        message.enableAutoSizing = true;
        message.fontSizeMin = 16f;
        message.fontSizeMax = 26f;

        AnimatedMenuButton confirmButton = CreateMenuButton(frame.transform, "ConfirmButton", "SAIR SEM GUARDAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 300f, 56f);
        SetRect(RectOf(confirmButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 30f), new Vector2(300f, 56f));
        ConfigureMenuButtonLabel(confirmButton, 11f, 19f);

        AnimatedMenuButton cancelButton = CreateMenuButton(frame.transform, "CancelButton", "CANCELAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 210f, 56f);
        SetRect(RectOf(cancelButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(20f, 30f), new Vector2(210f, 56f));
        ConfigureMenuButtonLabel(cancelButton, 11f, 19f);

        ConfirmOverwritePanel controller = root.AddComponent<ConfirmOverwritePanel>();
        SetObjectField(controller, "panel", panel);
        SetObjectField(controller, "messageText", message);
        SetObjectField(controller, "confirmButton", confirmButton);
        SetObjectField(controller, "cancelButton", cancelButton);
        controller.HideImmediate();
        return controller;
    }

    private static SaveSuccessPanel CreateSaveSuccessPanel(Transform parent, TMP_FontAsset tmpFont, Sprite panelWood, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, MenuAudioManager audioManager)
    {
        GameObject root = CreateUIObject("SaveSuccessPanel", parent);
        StretchFull(RectOf(root));

        GameObject blocker = CreateUIObject("Blocker", root.transform);
        StretchFull(RectOf(blocker));
        AddImage(blocker, null, new Color(0f, 0f, 0f, 0.6f), false).raycastTarget = true;

        GameObject frame = CreateUIObject("Frame", root.transform);
        SetRect(RectOf(frame), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 260f));
        Image frameImage = AddImage(frame, panelWood, new Color(1f, 1f, 1f, 0.98f), false);
        frameImage.type = Image.Type.Sliced;
        AddShadow(frame, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -14f));

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        MenuPanelController panel = root.AddComponent<MenuPanelController>();
        SetObjectField(panel, "canvasGroup", canvasGroup);
        SetObjectField(panel, "panelTransform", RectOf(frame));
        SetVector2Field(panel, "hiddenOffset", new Vector2(0f, -16f));

        GameObject messageGo = CreateUIObject("MessageText", frame.transform);
        SetRect(RectOf(messageGo), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(560f, 76f));
        TMP_Text message = AddText(messageGo, "JOGO GUARDADO COM SUCESSO.", tmpFont, 28, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(236, 220, 191, 255));
        ConfigureSingleLineText(message, 18f, 28f);

        AnimatedMenuButton backButton = CreateMenuButton(frame.transform, "BackButton", "VOLTAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 190f, 56f);
        SetRect(RectOf(backButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(190f, 56f));
        ConfigureMenuButtonLabel(backButton, 12f, 20f);

        SaveSuccessPanel controller = root.AddComponent<SaveSuccessPanel>();
        SetObjectField(controller, "panel", panel);
        SetObjectField(controller, "messageText", message);
        SetObjectField(controller, "backButton", backButton);
        controller.HideImmediate();
        return controller;
    }

    private static GameObject CreateFullSettingsPanel(string name, Transform parent, Sprite panelWood, TMP_FontAsset tmpFont, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, MenuAudioManager audioManager, out Toggle fullscreenToggle, out AnimatedMenuButton backButton)
    {
        GameObject root = CreateUIObject(name, parent);
        Image frameImage = AddImage(root, panelWood, new Color(1f, 1f, 1f, 0.96f), false);
        frameImage.type = Image.Type.Sliced;
        AddShadow(root, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -14f));

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        MenuPanelController panelController = root.AddComponent<MenuPanelController>();
        SetObjectField(panelController, "canvasGroup", canvasGroup);
        SetObjectField(panelController, "panelTransform", RectOf(root));
        SetVector2Field(panelController, "hiddenOffset", new Vector2(0f, -18f));

        GameObject shadeGo = CreateUIObject("PanelShade", root.transform);
        StretchFull(RectOf(shadeGo));
        AddImage(shadeGo, null, new Color(0.08f, 0.04f, 0.03f, 0.34f), false).raycastTarget = false;
        shadeGo.transform.SetAsFirstSibling();

        GameObject leftTabs = CreateUIObject("LeftTabsArea", root.transform);
        SetRect(RectOf(leftTabs), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -130f), new Vector2(230f, 330f));
        VerticalLayoutGroup tabLayout = leftTabs.AddComponent<VerticalLayoutGroup>();
        tabLayout.spacing = 12f;
        tabLayout.childAlignment = TextAnchor.UpperLeft;
        tabLayout.childControlHeight = false;
        tabLayout.childControlWidth = false;
        tabLayout.childForceExpandHeight = false;
        tabLayout.childForceExpandWidth = false;

        GameObject audioTab = CreateSettingsTab(leftTabs.transform, "AudioTab", "AUDIO", true, tmpFont);
        GameObject controlsTab = CreateSettingsTab(leftTabs.transform, "ControlsTab", "CONTROLOS", false, tmpFont);
        GameObject languageTab = CreateSettingsTab(leftTabs.transform, "LanguageTab", "IDIOMA", false, tmpFont);

        GameObject rightContent = CreateUIObject("RightContentArea", root.transform);
        SetRect(RectOf(rightContent), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(320f, -110f), new Vector2(780f, 430f));

        GameObject sectionTitleGo = CreateUIObject("SectionTitle", rightContent.transform);
        SetRect(RectOf(sectionTitleGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(780f, 44f));
        TMP_Text sectionTitle = AddText(sectionTitleGo, "AUDIO", tmpFont, 34, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color32(241, 226, 194, 255));
        sectionTitle.characterSpacing = 6f;
        ConfigureSingleLineText(sectionTitle, 24f, 34f);

        GameObject dividerGo = CreateUIObject("SectionDivider", rightContent.transform);
        SetRect(RectOf(dividerGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -52f), new Vector2(780f, 2f));
        AddImage(dividerGo, null, new Color(0.66f, 0.54f, 0.4f, 0.34f), false).raycastTarget = false;

        GameObject sectionsRoot = CreateUIObject("SectionsRoot", rightContent.transform);
        SetRect(RectOf(sectionsRoot), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -74f), new Vector2(780f, 364f));

        GameObject audioSection = CreateUIObject("AudioSection", sectionsRoot.transform);
        StretchFull(RectOf(audioSection));
        CreateAudioSliderRow(audioSection.transform, "MasterRow", "VOLUME GERAL", 0, tmpFont, out Slider masterSlider, out TMP_Text masterValueText);
        CreateAudioSliderRow(audioSection.transform, "MusicRow", "VOLUME DA MUSICA", 1, tmpFont, out Slider musicSlider, out TMP_Text musicValueText);
        CreateAudioSliderRow(audioSection.transform, "SfxRow", "VOLUME DOS EFEITOS", 2, tmpFont, out Slider sfxSlider, out TMP_Text sfxValueText);
        CreateSelectorRow(audioSection.transform, "AudioModeRow", "MODO DE AUDIO", "ESTEREO", 3, tmpFont, out Button audioModeButton, out TMP_Text audioModeValueText);
        CreateSelectorRow(audioSection.transform, "SubtitlesRow", "LEGENDAS", "LIGADO", 4, tmpFont, out Button subtitlesButton, out TMP_Text subtitlesValueText);

        GameObject controlsSection = CreateUIObject("ControlsSection", sectionsRoot.transform);
        StretchFull(RectOf(controlsSection));
        CreateSelectorRow(controlsSection.transform, "ForwardRow", "ANDAR PARA A FRENTE", "W", 0, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "LeftRow", "ANDAR PARA A ESQUERDA", "A", 1, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "BackRow", "ANDAR PARA TRAS", "S", 2, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "RightRow", "ANDAR PARA A DIREITA", "D", 3, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "RunRow", "CORRER", "SHIFT", 4, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "InteractRow", "INTERAGIR", "F", 5, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "AttackRow", "ATAQUE", "MOUSE LEFT", 6, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "HeavyAttackRow", "ATAQUE PESADO", "MOUSE RIGHT", 7, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "DefendRow", "DEFENDER", "R", 8, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "JumpRow", "SALTAR", "SPACE", 9, tmpFont, out _, out _);
        CreateSelectorRow(controlsSection.transform, "RollRow", "REBOLAR", "C", 10, tmpFont, out _, out _);

        GameObject languageSection = CreateUIObject("LanguageSection", sectionsRoot.transform);
        StretchFull(RectOf(languageSection));
        CreateSelectorRow(languageSection.transform, "LanguageRow", "IDIOMA", "PORTUGUES", 0, tmpFont, out Button languageButton, out TMP_Text languageValueText);

        GameObject bottomButtons = CreateUIObject("BottomButtonsArea", root.transform);
        SetRect(RectOf(bottomButtons), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(54f, 34f), new Vector2(-54f, 96f));

        AnimatedMenuButton resetButton = CreateMenuButton(bottomButtons.transform, "ResetDefaultsButton", "REPOR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 190f, 52f);
        SetRect(RectOf(resetButton.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, 4f), new Vector2(190f, 52f));
        ConfigureMenuButtonLabel(resetButton, 18f, 22f);

        AnimatedMenuButton applyButton = CreateMenuButton(bottomButtons.transform, "ApplyButton", "APLICAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 190f, 52f);
        SetRect(RectOf(applyButton.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(190f, 52f));
        ConfigureMenuButtonLabel(applyButton, 18f, 22f);

        backButton = CreateMenuButton(bottomButtons.transform, "BackButton", "VOLTAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 190f, 52f);
        SetRect(RectOf(backButton.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 4f), new Vector2(190f, 52f));
        ConfigureMenuButtonLabel(backButton, 18f, 22f);

        GameObject fullscreenToggleGo = CreateToggle("FullscreenToggle", root.transform, tmpFont, "Ecra Inteiro", new Vector2(40f, 26f));
        fullscreenToggle = fullscreenToggleGo.GetComponent<Toggle>();
        fullscreenToggleGo.SetActive(false);

        OptionsPanelView view = root.AddComponent<OptionsPanelView>();
        SetObjectField(view, "menuAudioManager", audioManager);
        SetObjectField(view, "sectionTitleText", sectionTitle);
        SetObjectField(view, "masterSlider", masterSlider);
        SetObjectField(view, "musicSlider", musicSlider);
        SetObjectField(view, "sfxSlider", sfxSlider);
        SetObjectField(view, "masterValueText", masterValueText);
        SetObjectField(view, "musicValueText", musicValueText);
        SetObjectField(view, "sfxValueText", sfxValueText);
        SetObjectField(view, "audioModeButton", audioModeButton);
        SetObjectField(view, "audioModeValueText", audioModeValueText);
        SetObjectField(view, "subtitlesButton", subtitlesButton);
        SetObjectField(view, "subtitlesValueText", subtitlesValueText);
        SetObjectField(view, "languageButton", languageButton);
        SetObjectField(view, "languageValueText", languageValueText);
        SetObjectField(view, "applyButton", applyButton);
        SetObjectField(view, "resetDefaultsButton", resetButton);
        SetObjectField(view, "activeTabSprite", null);
        SetObjectField(view, "inactiveTabSprite", null);
        SetButtonArrayField(view, "tabButtons", new[] { audioTab.GetComponent<Button>(), controlsTab.GetComponent<Button>(), languageTab.GetComponent<Button>() });
        SetImageArrayField(view, "tabBackgrounds", new[] { audioTab.GetComponent<Image>(), controlsTab.GetComponent<Image>(), languageTab.GetComponent<Image>() });
        SetTextArrayField(view, "tabLabels", new[] { FindChildText(audioTab.transform, "Label"), FindChildText(controlsTab.transform, "Label"), FindChildText(languageTab.transform, "Label") });
        SetGameObjectArrayField(view, "selectedMarkers", new[] { FindChild(audioTab.transform, "SelectedMarker"), FindChild(controlsTab.transform, "SelectedMarker"), FindChild(languageTab.transform, "SelectedMarker") });
        SetGameObjectArrayField(view, "sections", new[] { audioSection, controlsSection, languageSection });
        SetStringArrayField(view, "sectionTitles", new[] { "audio", "controls", "language" });

        return root;
    }

    private static AnimatedMenuButton CreateMenuButton(Transform parent, string name, string label, Sprite icon, TMP_FontAsset tmpFont, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, MenuAudioManager audioManager, float width = 395f, float height = 80f)
    {
        GameObject buttonGo = CreateUIObject(name, parent);
        RectTransform rect = RectOf(buttonGo);
        rect.sizeDelta = new Vector2(width, height);
        LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        Image background = AddImage(buttonGo, buttonNormal, Color.white, false);
        background.type = Image.Type.Sliced;
        AddShadow(buttonGo, new Color(0f, 0f, 0f, 0.38f), new Vector2(0f, -10f));

        Button button = buttonGo.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        GameObject glowGo = CreateUIObject("Glow", buttonGo.transform);
        StretchFull(RectOf(glowGo));
        RectOf(glowGo).offsetMin = new Vector2(-16f, -12f);
        RectOf(glowGo).offsetMax = new Vector2(18f, 14f);
        Image glow = AddImage(glowGo, buttonGlow, new Color(1f, 0.84f, 0.55f, 0f), false);
        glow.enabled = false;
        glow.raycastTarget = false;
        glowGo.transform.SetAsFirstSibling();

        Image iconImage = null;
        if (icon != null)
        {
            GameObject iconGo = CreateUIObject("Icon", buttonGo.transform);
            SetRect(RectOf(iconGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(42f, 42f));
            iconImage = AddImage(iconGo, icon, new Color32(237, 223, 191, 255), true);
            iconImage.raycastTarget = false;
        }

        GameObject labelGo = CreateUIObject("Label", buttonGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), icon != null ? new Vector2(16f, 0f) : Vector2.zero, new Vector2(-20f, 0f));
        TMP_Text labelText = AddText(labelGo, label, tmpFont, 28, FontStyles.SmallCaps | FontStyles.Bold, icon != null ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center, new Color32(227, 214, 184, 255));
        labelText.margin = icon != null ? new Vector4(78f, 0f, 16f, 0f) : new Vector4(16f, 0f, 16f, 0f);
        labelText.characterSpacing = 7f;
        AddShadow(labelGo, new Color(0f, 0f, 0f, 0.42f), new Vector2(0f, -2f));

        AnimatedMenuButton animated = buttonGo.AddComponent<AnimatedMenuButton>();
        SetObjectField(animated, "backgroundImage", background);
        SetObjectField(animated, "glowImage", glow);
        SetObjectField(animated, "iconImage", iconImage);
        SetObjectField(animated, "labelText", labelText);
        SetObjectField(animated, "contentRoot", rect);
        SetObjectField(animated, "menuAudioManager", audioManager);
        SetObjectField(animated, "normalSprite", buttonNormal);
        SetObjectField(animated, "hoverSprite", buttonHover);
        SetObjectField(animated, "pressedSprite", buttonPressed);
        return animated;
    }

    private static GameObject CreateSlider(string name, Transform parent, Vector2 anchoredPosition)
    {
        GameObject sliderGo = CreateUIObject(name, parent);
        SetRect(RectOf(sliderGo), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(0f, 26f));
        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject backgroundGo = CreateUIObject("Background", sliderGo.transform);
        StretchFull(RectOf(backgroundGo));
        AddImage(backgroundGo, null, new Color32(54, 37, 29, 255), false);

        GameObject fillAreaGo = CreateUIObject("Fill Area", sliderGo.transform);
        StretchFull(RectOf(fillAreaGo));
        RectOf(fillAreaGo).offsetMin = new Vector2(12f, 7f);
        RectOf(fillAreaGo).offsetMax = new Vector2(-12f, -7f);

        GameObject fillGo = CreateUIObject("Fill", fillAreaGo.transform);
        StretchFull(RectOf(fillGo));
        AddImage(fillGo, null, new Color32(142, 38, 38, 255), false);

        GameObject handleAreaGo = CreateUIObject("Handle Slide Area", sliderGo.transform);
        StretchFull(RectOf(handleAreaGo));
        RectOf(handleAreaGo).offsetMin = new Vector2(12f, 0f);
        RectOf(handleAreaGo).offsetMax = new Vector2(-12f, 0f);

        GameObject handleGo = CreateUIObject("Handle", handleAreaGo.transform);
        SetRect(RectOf(handleGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
        Image handle = AddImage(handleGo, null, new Color32(228, 209, 173, 255), false);

        slider.targetGraphic = handle;
        slider.fillRect = RectOf(fillGo);
        slider.handleRect = RectOf(handleGo);
        return sliderGo;
    }

    private static GameObject CreatePlaceholderSection(Transform parent, string name, string textValue, TMP_FontAsset tmpFont)
    {
        GameObject section = CreateUIObject(name, parent);
        StretchFull(RectOf(section));
        GameObject placeholder = CreateUIObject("PlaceholderText", section.transform);
        SetRect(RectOf(placeholder), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(780f, 70f));
        TMP_Text text = AddText(placeholder, textValue, tmpFont, 24, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color32(174, 160, 140, 255));
        ConfigureSingleLineText(text, 18f, 24f);
        return section;
    }

    private static GameObject CreateSettingsTab(Transform parent, string name, string label, bool selected, TMP_FontAsset tmpFont)
    {
        GameObject tabGo = CreateUIObject(name, parent);
        RectTransform rect = RectOf(tabGo);
        rect.sizeDelta = new Vector2(190f, 55f);
        LayoutElement layout = tabGo.AddComponent<LayoutElement>();
        layout.preferredWidth = 190f;
        layout.preferredHeight = 55f;

        Image background = AddImage(tabGo, null, selected ? new Color(0.3f, 0.08f, 0.08f, 0.95f) : new Color(0.11f, 0.085f, 0.08f, 0.92f), false);
        background.raycastTarget = true;
        AddShadow(tabGo, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -5f));

        Button button = tabGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = selected ? new Color(1f, 0.96f, 0.96f, 1f) : new Color(0.18f, 0.12f, 0.1f, 1f);
        colors.pressedColor = selected ? new Color(0.94f, 0.88f, 0.88f, 1f) : new Color(0.42f, 0.16f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        GameObject marker = CreateUIObject("SelectedMarker", tabGo.transform);
        SetRect(RectOf(marker), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-8f, 0f), new Vector2(12f, 20f));
        AddImage(marker, null, new Color(0.62f, 0.15f, 0.16f, 1f), false).raycastTarget = false;
        marker.SetActive(selected);

        GameObject labelGo = CreateUIObject("Label", tabGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-20f, 0f));
        TMP_Text text = AddText(labelGo, label, tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, selected ? new Color32(243, 228, 194, 255) : new Color32(168, 154, 129, 255));
        text.characterSpacing = 2f;
        ConfigureSingleLineText(text, 16f, 22f);
        return tabGo;
    }

    private static void CreateAudioSliderRow(Transform parent, string name, string label, int rowIndex, TMP_FontAsset tmpFont, out Slider slider, out TMP_Text valueText)
    {
        float rowY = -rowIndex * 70f;
        GameObject rowGo = CreateUIObject(name, parent);
        SetRect(RectOf(rowGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(780f, 70f));

        GameObject labelGo = CreateUIObject("Label", rowGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(330f, 70f));
        TMP_Text labelText = AddText(labelGo, label, tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color32(218, 205, 181, 255));
        labelText.characterSpacing = 2f;
        ConfigureSingleLineText(labelText, 16f, 22f);

        GameObject sliderGo = CreateSlider(name + "Slider", rowGo.transform, Vector2.zero);
        SetRect(RectOf(sliderGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(360f, 0f), new Vector2(280f, 26f));
        slider = sliderGo.GetComponent<Slider>();

        GameObject valueGo = CreateUIObject("ValueText", rowGo.transform);
        SetRect(RectOf(valueGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(670f, 0f), new Vector2(80f, 70f));
        valueText = AddText(valueGo, "80%", tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color32(217, 205, 181, 255));
        ConfigureSingleLineText(valueText, 16f, 22f);
    }

    private static void CreateSelectorRow(Transform parent, string name, string label, string initialValue, int rowIndex, TMP_FontAsset tmpFont, out Button button, out TMP_Text valueText)
    {
        float rowY = -rowIndex * 70f;
        GameObject rowGo = CreateUIObject(name, parent);
        SetRect(RectOf(rowGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(780f, 70f));

        GameObject labelGo = CreateUIObject("Label", rowGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(330f, 70f));
        TMP_Text labelText = AddText(labelGo, label, tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color32(218, 205, 181, 255));
        labelText.characterSpacing = 2f;
        ConfigureSingleLineText(labelText, 16f, 22f);

        GameObject buttonGo = CreateUIObject("SelectorButton", rowGo.transform);
        SetRect(RectOf(buttonGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(360f, 0f), new Vector2(260f, 42f));
        Image image = AddImage(buttonGo, null, new Color32(18, 14, 13, 235), false);
        button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.18f, 0.14f, 0.12f, 1f);
        colors.pressedColor = new Color(0.3f, 0.16f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        GameObject valueGo = CreateUIObject("ValueText", buttonGo.transform);
        SetRect(RectOf(valueGo), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 0f), new Vector2(-34f, 0f));
        valueText = AddText(valueGo, initialValue, tmpFont, 20, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(228, 216, 191, 255));
        ConfigureSingleLineText(valueText, 14f, 20f);

        GameObject arrowGo = CreateUIObject("Arrow", buttonGo.transform);
        SetRect(RectOf(arrowGo), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, -1f), new Vector2(20f, 20f));
        TMP_Text arrowText = AddText(arrowGo, "^", tmpFont, 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color32(192, 174, 146, 255));
        ConfigureSingleLineText(arrowText, 14f, 18f);
    }

    private static void ConfigureMenuButtonLabel(AnimatedMenuButton button, float minSize, float maxSize)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
        {
            return;
        }

        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = minSize;
        label.fontSizeMax = maxSize;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void ConfigureSingleLineText(TMP_Text text, float minSize, float maxSize)
    {
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static GameObject CreateToggle(string name, Transform parent, TMP_FontAsset tmpFont, string label, Vector2 anchoredPosition)
    {
        GameObject toggleGo = CreateUIObject(name, parent);
        SetRect(RectOf(toggleGo), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(0f, 34f));
        Toggle toggle = toggleGo.AddComponent<Toggle>();

        GameObject backgroundGo = CreateUIObject("Background", toggleGo.transform);
        SetRect(RectOf(backgroundGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(28f, 28f));
        Image background = AddImage(backgroundGo, null, new Color32(60, 42, 32, 255), false);

        GameObject checkmarkGo = CreateUIObject("Checkmark", backgroundGo.transform);
        SetRect(RectOf(checkmarkGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
        Image checkmark = AddImage(checkmarkGo, null, new Color32(140, 38, 38, 255), false);

        GameObject labelGo = CreateUIObject("Label", toggleGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-120f, 0f));
        TMP_Text labelText = AddText(labelGo, label, tmpFont, 24, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color32(238, 224, 191, 255));
        labelText.margin = new Vector4(88f, 0f, 0f, 0f);

        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        return toggleGo;
    }

    private static void CreateLabel(Transform parent, TMP_FontAsset tmpFont, string textValue, Vector2 anchoredPosition)
    {
        GameObject labelGo = CreateUIObject(textValue + "Label", parent);
        SetRect(RectOf(labelGo), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(0f, 28f));
        TMP_Text label = AddText(labelGo, textValue, tmpFont, 24, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.BottomLeft, new Color32(238, 224, 191, 255));
        label.margin = new Vector4(42f, 0f, 0f, 0f);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemGo = new("EventSystem", typeof(EventSystem));
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

    private static void ConfigureSprites()
    {
        string[] paths =
        {
            "button_normal.png",
            "button_hover.png",
            "button_pressed.png",
            "button_glow.png",
            "panel_dark_wood.png",
            "vignette_overlay.png",
            "bg_placeholder_dark_alley.png",
            "imagem_fundo.png",
            "title_the_last_sip.png",
            "icon_new_game_wine.png",
            "icon_options_gear.png",
            "icon_credits_document.png",
            "icon_exit_door.png"
        };

        foreach (string fileName in paths)
        {
            string path = GeneratedPath + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException("Missing texture importer for " + path);
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static TMP_FontAsset ResolveTmpFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }

        if (font == null)
        {
            throw new FileNotFoundException("Unable to resolve a TMP font asset.");
        }

        return font;
    }

    private static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedPath + fileName);
        if (sprite == null)
        {
            throw new FileNotFoundException("Missing sprite at " + GeneratedPath + fileName);
        }

        return sprite;
    }

    private static Sprite LoadOptionalGeneratedSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedPath + fileName);
    }

    private static AudioClip LoadAudioClip(string fileName)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + fileName);
        if (clip == null)
        {
            throw new FileNotFoundException("Missing audio clip at " + AudioPath + fileName);
        }

        return clip;
    }

    private static AudioClip LoadMusicClip(string fileName)
    {
        string path = MusicPath + fileName;
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            throw new FileNotFoundException("Missing music clip at " + path);
        }

        return clip;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform RectOf(GameObject go)
    {
        return go.GetComponent<RectTransform>();
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static Image AddImage(GameObject go, Sprite sprite, Color color, bool preserveAspect)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = preserveAspect;
        return image;
    }

    private static TMP_Text AddText(GameObject go, string textValue, TMP_FontAsset font, float fontSize, FontStyles styles, TextAlignmentOptions alignment, Color color)
    {
        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = textValue;
        text.fontSize = fontSize;
        text.fontStyle = styles;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static void AddShadow(GameObject go, Color color, Vector2 distance)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private static void SetObjectField(Object target, string fieldName, Object value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }

    private static void SetButtonArrayField(Object target, string fieldName, Button[] values)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, values);
        EditorUtility.SetDirty(target);
    }

    private static void SetImageArrayField(Object target, string fieldName, Image[] values)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, values);
        EditorUtility.SetDirty(target);
    }

    private static void SetTextArrayField(Object target, string fieldName, TMP_Text[] values)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, values);
        EditorUtility.SetDirty(target);
    }

    private static void SetGameObjectArrayField(Object target, string fieldName, GameObject[] values)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, values);
        EditorUtility.SetDirty(target);
    }

    private static void SetStringField(Object target, string fieldName, string value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        int slash = assetPath.LastIndexOf('/');
        if (slash <= 0)
        {
            return;
        }

        string parent = assetPath.Substring(0, slash);
        string name = assetPath.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetStringArrayField(Object target, string fieldName, string[] values)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, values);
        EditorUtility.SetDirty(target);
    }

    private static void SetVector2Field(Object target, string fieldName, Vector2 value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }

    private static GameObject FindChild(Transform root, string name)
    {
        Transform child = root.Find(name);
        return child != null ? child.gameObject : null;
    }

    private static TMP_Text FindChildText(Transform root, string name)
    {
        Transform child = root.Find(name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }
}
#endif
