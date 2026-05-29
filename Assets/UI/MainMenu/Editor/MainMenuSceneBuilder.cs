#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
    private const string BecoScenePath = "Assets/Scenes/beco.unity";
    private const string GeneratedPath = "Assets/UI/MainMenu/Generated/";
    private const string AudioPath = "Assets/Audio/UI_Medieval-Free/";
    private const string MusicPath = "Assets/Audio/ImportedFromZip/Audio/";
    private const bool CreateDecorationsByDefault = false;

    // O ecrã está finalizado, mas foi reativado para regenerar os créditos
    [MenuItem("Tools/The Last Sip/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        ConfigureSprites();

        Sprite buttonNormal = LoadSprite("button_normal.png");
        Sprite buttonHover = LoadSprite("button_hover.png");
        Sprite buttonPressed = LoadSprite("button_pressed.png");
        Sprite buttonGlow = LoadSprite("button_glow.png");
        Sprite panelWood = LoadSprite("panel_dark_wood.png");
        Sprite vignette = LoadSprite("vignette_overlay.png");
        Sprite background = LoadOptionalGeneratedSprite("imagem_fundo.png") ?? LoadSprite("bg_placeholder_dark_alley.png");
        Sprite title = LoadSprite("title_the_last_sip.png");
        Sprite iconNew = LoadSprite("icon_new_game_wine.png");
        Sprite iconOptions = LoadSprite("icon_options_gear.png");
        Sprite iconCredits = LoadSprite("icon_credits_document.png");
        Sprite iconExit = LoadSprite("icon_exit_door.png");
        Sprite bottle = LoadSprite("decor_wine_bottle.png");
        Sprite glass = LoadSprite("decor_wine_glass.png");
        Sprite newspaper = LoadSprite("decor_newspaper.png");
        AudioClip menuAmbientMusicClip = LoadMusicClip("MenuPrincipal.mp3");
        AudioClip creditsAmbientMusicClip = LoadMusicClip("Creditos.mp3");
        AudioClip hoverClip = LoadAudioClip("UIClick_UIBton ButtonHover _Tonefirst_Medieval UI-Free.wav");
        AudioClip clickClip = LoadAudioClip("UIClick_UIBton ButtonClick _Tonefirst_Medieval UI-Free.wav");

        TMP_FontAsset tmpFont = ResolveTmpFont();
        if (tmpFont == null)
        {
            throw new InvalidOperationException("TextMeshPro font asset could not be resolved.");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenuScene";

        CreateCamera();
        CreateEventSystem();

        GameObject canvasGo = new("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        AudioSource musicSource = canvasGo.AddComponent<AudioSource>();
        AudioSource sfxSource = canvasGo.AddComponent<AudioSource>();
        MenuAudioManager audioManager = canvasGo.AddComponent<MenuAudioManager>();
        SetObjectField(audioManager, "ambientMusicSource", musicSource);
        SetObjectField(audioManager, "uiSfxSource", sfxSource);
        SetObjectField(audioManager, "ambientMusicClip", menuAmbientMusicClip);
        SetObjectField(audioManager, "hoverClip", hoverClip);
        SetObjectField(audioManager, "clickClip", clickClip);

        GameObject mainMenuPanel = CreateUIObject("MainMenuPanel", canvasGo.transform);
        StretchFull(RectOf(mainMenuPanel));
        CanvasGroup mainMenuCanvasGroup = mainMenuPanel.AddComponent<CanvasGroup>();
        BuildMainMenuVisual(mainMenuPanel.transform, tmpFont, background, vignette, title, buttonNormal, buttonHover, buttonPressed, buttonGlow, iconNew, iconOptions, iconCredits, iconExit, bottle, glass, newspaper, audioManager,
            out AnimatedMenuButton newGameButton,
            out AnimatedMenuButton continueButton,
            out AnimatedMenuButton optionsButton,
            out AnimatedMenuButton creditsButton,
            out AnimatedMenuButton exitButton,
            out TMP_Text versionText);

        GameObject loadingPanel = CreateUIObject("LoadingPanel", canvasGo.transform);
        StretchFull(RectOf(loadingPanel));
        CanvasGroup loadingCanvasGroup = loadingPanel.AddComponent<CanvasGroup>();
        BuildLoadingPanel(loadingPanel.transform, tmpFont, background, vignette, title, null, bottle, glass, newspaper, out RectTransform loadingTitleRoot, out RectTransform loadingSpinnerRoot, out TMP_Text loadingQuoteText, out TMP_Text loadingStatusText);
        loadingPanel.SetActive(false);

        GameObject creditsPanel = CreateCreditsPanel(canvasGo.transform, tmpFont, background, vignette, title, panelWood, buttonNormal, buttonHover, buttonPressed, buttonGlow, bottle, glass, newspaper, audioManager);
        creditsPanel.name = "CreditsPanel";
        GameObject optionsPanel = CreateOptionsPanel(canvasGo.transform, tmpFont, background, vignette, title, panelWood, null, null, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        optionsPanel.name = "OptionsPanel";
        GameObject slotSelectionPanel = CreateSlotSelectionPanel(canvasGo.transform, tmpFont, panelWood, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        GameObject confirmOverwritePanel = CreateConfirmOverwritePanel(canvasGo.transform, tmpFont, panelWood, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);

        GameObject fadeGo = CreateUIObject("FadeTransition", canvasGo.transform);
        StretchFull(RectOf(fadeGo));
        Image fadeImage = fadeGo.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        CanvasGroup fadeCanvasGroup = fadeGo.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;
        SceneFadeController fadeController = fadeGo.AddComponent<SceneFadeController>();
        SetObjectField(fadeController, "fadeImage", fadeImage);
        SetObjectField(fadeController, "canvasGroup", fadeCanvasGroup);

        LoadingScreenController loadingController = canvasGo.AddComponent<LoadingScreenController>();
        SetObjectField(loadingController, "menuAudioManager", audioManager);
        SetObjectField(loadingController, "loadingPanel", loadingPanel);
        SetObjectField(loadingController, "mainMenuPanel", mainMenuPanel);
        SetObjectField(loadingController, "creditsPanel", creditsPanel.GetComponent<MenuPanelController>());
        SetObjectField(loadingController, "sceneFadeController", fadeController);
        SetObjectField(loadingController, "loadingCanvasGroup", loadingCanvasGroup);
        SetObjectField(loadingController, "loadingTitleRoot", loadingTitleRoot);
        SetObjectField(loadingController, "loadingIconRoot", loadingSpinnerRoot);
        SetObjectField(loadingController, "quoteText", loadingQuoteText);
        SetObjectField(loadingController, "loadingText", loadingStatusText);

        MainMenuController controller = canvasGo.AddComponent<MainMenuController>();
        SetStringField(controller, "defaultGameSceneName", "Beco");
        SetObjectField(controller, "sceneFadeController", fadeController);
        SetObjectField(controller, "loadingScreenController", loadingController);
        SetObjectField(controller, "menuAudioManager", audioManager);
        SetObjectField(controller, "menuAmbientClip", menuAmbientMusicClip);
        SetObjectField(controller, "creditsAmbientClip", creditsAmbientMusicClip);
        SetObjectField(controller, "mainMenuPanel", mainMenuPanel);
        SetObjectField(controller, "optionsPanel", optionsPanel.GetComponent<MenuPanelController>());
        SetObjectField(controller, "optionsPanelView", optionsPanel.GetComponent<OptionsPanelView>());
        SetObjectField(controller, "creditsPanel", creditsPanel.GetComponent<MenuPanelController>());
        SetObjectField(controller, "fullscreenToggle", FindInChildren<Toggle>(optionsPanel.transform, "FullscreenToggle"));
        SetObjectField(controller, "mainMenuCanvasGroup", mainMenuCanvasGroup);
        SetObjectField(controller, "newGameButton", newGameButton);
        SetObjectField(controller, "continueButton", continueButton);
        SetObjectField(controller, "optionsButton", optionsButton);
        SetObjectField(controller, "creditsButton", creditsButton);
        SetObjectField(controller, "exitButton", exitButton);
        SetObjectField(controller, "slotSelectionController", slotSelectionPanel.GetComponent<SlotSelectionController>());
        SetObjectField(controller, "confirmOverwritePanel", confirmOverwritePanel.GetComponent<ConfirmOverwritePanel>());
        SetObjectField(controller, "versionText", versionText);
        SetBoolField(controller, "useApplicationVersion", false);
        SetStringField(controller, "fallbackVersion", "1.0.0");
        SetStringField(controller, "versionPrefix", "v");

        WireBackButton(optionsPanel.transform, controller, creditsPanel: false);
        WireBackButton(creditsPanel.transform, controller, creditsPanel: true);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        EnsureBuildSettings();
        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("MainMenuScene rebuilt with loading flow.");
    }

    private static void BuildMainMenuVisual(
        Transform parent,
        TMP_FontAsset tmpFont,
        Sprite background,
        Sprite vignette,
        Sprite title,
        Sprite buttonNormal,
        Sprite buttonHover,
        Sprite buttonPressed,
        Sprite buttonGlow,
        Sprite iconNew,
        Sprite iconOptions,
        Sprite iconCredits,
        Sprite iconExit,
        Sprite bottle,
        Sprite glass,
        Sprite newspaper,
        MenuAudioManager audioManager,
        out AnimatedMenuButton newGameButton,
        out AnimatedMenuButton continueButton,
        out AnimatedMenuButton optionsButton,
        out AnimatedMenuButton creditsButton,
        out AnimatedMenuButton exitButton,
        out TMP_Text versionText)
    {
        GameObject backgroundGo = CreateUIObject("Background", parent);
        StretchFull(RectOf(backgroundGo));
        AddImage(backgroundGo, background, Color.white, false).raycastTarget = false;

        bool hasBakedScreen = title == null;
        GameObject overlayGo = CreateUIObject("ReadabilityOverlay", parent);
        StretchFull(RectOf(overlayGo));
        AddImage(overlayGo, null, hasBakedScreen ? new Color(0.02f, 0.015f, 0.02f, 0.18f) : new Color(0.05f, 0.035f, 0.04f, 0.54f), false).raycastTarget = false;

        GameObject vignetteGo = CreateUIObject("VignetteOverlay", parent);
        StretchFull(RectOf(vignetteGo));
        AddImage(vignetteGo, vignette, new Color(1f, 1f, 1f, hasBakedScreen ? 0.42f : 0.96f), false).raycastTarget = false;

        if (title != null)
        {
            GameObject titleGo = CreateUIObject("TitleImage", parent);
            SetRect(RectOf(titleGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(82f, -68f), new Vector2(720f, 250f));
            AddImage(titleGo, title, Color.white, true).raycastTarget = false;
            AddShadow(titleGo, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -16f));
        }

        GameObject buttonContainerGo = CreateUIObject("ButtonContainer", parent);
        SetRect(RectOf(buttonContainerGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), hasBakedScreen ? new Vector2(104f, -334f) : new Vector2(108f, -320f), new Vector2(430f, 420f));
        VerticalLayoutGroup layout = buttonContainerGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        ContentSizeFitter fitter = buttonContainerGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        newGameButton = CreateMenuButton(buttonContainerGo.transform, "NewGameButton", "NOVO JOGO", iconNew, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        continueButton = CreateMenuButton(buttonContainerGo.transform, "ContinueButton", "CONTINUAR", iconNew, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        optionsButton = CreateMenuButton(buttonContainerGo.transform, "OptionsButton", "OPÇÕES", iconOptions, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        creditsButton = CreateMenuButton(buttonContainerGo.transform, "CreditsButton", "CREDITOS", iconCredits, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);
        exitButton = CreateMenuButton(buttonContainerGo.transform, "ExitButton", "SAIR", iconExit, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager);

        if (!hasBakedScreen && CreateDecorationsByDefault)
        {
            GameObject decorationGo = CreateUIObject("DecorationContainer", parent);
            StretchFull(RectOf(decorationGo));

            GameObject bottleGo = CreateUIObject("WineBottle", decorationGo.transform);
            SetRect(RectOf(bottleGo), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-245f, -18f), new Vector2(320f, 670f));
            AddImage(bottleGo, bottle, new Color(1f, 1f, 1f, 0.78f), true).raycastTarget = false;
            RectOf(bottleGo).localRotation = Quaternion.Euler(0f, 0f, -3f);
            DecorationIdleMotion bottleIdle = bottleGo.AddComponent<DecorationIdleMotion>();
            SetVector2Field(bottleIdle, "motionAmplitude", new Vector2(1.5f, 3f));
            SetFloatField(bottleIdle, "rotationAmplitude", 0.35f);
            SetFloatField(bottleIdle, "motionSpeed", 0.22f);
            SetFloatField(bottleIdle, "phaseOffset", 0.65f);

            GameObject glassGo = CreateUIObject("WineGlass", decorationGo.transform);
            SetRect(RectOf(glassGo), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-118f, 112f), new Vector2(232f, 382f));
            AddImage(glassGo, glass, new Color(1f, 1f, 1f, 0.92f), true).raycastTarget = false;
            RectOf(glassGo).localRotation = Quaternion.Euler(0f, 0f, -6f);
            DecorationIdleMotion glassIdle = glassGo.AddComponent<DecorationIdleMotion>();
            SetVector2Field(glassIdle, "motionAmplitude", new Vector2(4f, 8f));
            SetFloatField(glassIdle, "rotationAmplitude", 1.8f);
            SetFloatField(glassIdle, "motionSpeed", 0.58f);
            SetFloatField(glassIdle, "phaseOffset", 1.2f);

            GameObject newspaperGo = CreateUIObject("Newspaper", decorationGo.transform);
            SetRect(RectOf(newspaperGo), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-82f, 18f), new Vector2(356f, 214f));
            AddImage(newspaperGo, newspaper, new Color(1f, 1f, 1f, 0.8f), true).raycastTarget = false;
            RectOf(newspaperGo).localRotation = Quaternion.Euler(0f, 0f, -11f);
        }

        if (!hasBakedScreen)
        {
            GameObject versionGo = CreateUIObject("VersionText", parent);
            SetRect(RectOf(versionGo), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(98f, 38f), new Vector2(220f, 32f));
            versionText = AddText(versionGo, "v1.0.0", tmpFont, 22, FontStyles.SmallCaps, TextAlignmentOptions.BottomLeft, new Color32(196, 178, 143, 255));
            versionText.characterSpacing = 4f;
            versionText.raycastTarget = false;
            AddShadow(versionGo, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -3f));
        }
        else
        {
            versionText = null;
        }
    }

    private static void BuildLoadingPanel(Transform parent, TMP_FontAsset tmpFont, Sprite background, Sprite vignette, Sprite title, Sprite loadingWineRing, Sprite bottle, Sprite glass, Sprite newspaper, out RectTransform titleRoot, out RectTransform spinnerRoot, out TMP_Text quoteText, out TMP_Text loadingText)
    {
        GameObject backgroundGo = CreateUIObject("Background", parent);
        StretchFull(RectOf(backgroundGo));
        AddImage(backgroundGo, background, Color.white, false).raycastTarget = false;
        bool hasBakedScreen = title == null;

        GameObject veilGo = CreateUIObject("ReadabilityOverlay", parent);
        StretchFull(RectOf(veilGo));
        AddImage(veilGo, null, hasBakedScreen ? new Color(0.02f, 0.015f, 0.018f, 0.16f) : new Color(0.02f, 0.015f, 0.018f, 0.88f), false).raycastTarget = false;

        GameObject vignetteGo = CreateUIObject("VignetteOverlay", parent);
        StretchFull(RectOf(vignetteGo));
        AddImage(vignetteGo, vignette, Color.white, false).raycastTarget = false;

        if (CreateDecorationsByDefault)
        {
            GameObject propsGo = CreateUIObject("ForegroundProps", parent);
            StretchFull(RectOf(propsGo));

            GameObject leftBottleGo = CreateUIObject("LeftBottle", propsGo.transform);
            SetRect(RectOf(leftBottleGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 8f), new Vector2(176f, 388f));
            AddImage(leftBottleGo, bottle, new Color(1f, 1f, 1f, 0.26f), true).raycastTarget = false;

            GameObject rightBottleGo = CreateUIObject("RightBottle", propsGo.transform);
            SetRect(RectOf(rightBottleGo), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-112f, 18f), new Vector2(190f, 410f));
            AddImage(rightBottleGo, bottle, new Color(1f, 1f, 1f, 0.5f), true).raycastTarget = false;
            RectOf(rightBottleGo).localRotation = Quaternion.Euler(0f, 0f, -2f);

            GameObject glassGo = CreateUIObject("WineGlass", propsGo.transform);
            SetRect(RectOf(glassGo), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-182f, 134f), new Vector2(148f, 240f));
            AddImage(glassGo, glass, new Color(1f, 1f, 1f, 0.68f), true).raycastTarget = false;
            RectOf(glassGo).localRotation = Quaternion.Euler(0f, 0f, -3f);

            GameObject paperGo = CreateUIObject("Newspaper", propsGo.transform);
            SetRect(RectOf(paperGo), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-150f, 42f), new Vector2(182f, 118f));
            AddImage(paperGo, newspaper, new Color(1f, 1f, 1f, 0.16f), true).raycastTarget = false;
            RectOf(paperGo).localRotation = Quaternion.Euler(0f, 0f, -7f);
        }

        GameObject particlesGo = CreateUIObject("FloatingParticles", parent);
        StretchFull(RectOf(particlesGo));
        BuildLoadingParticles(particlesGo.transform);

        if (title != null)
        {
            GameObject titleGo = CreateUIObject("TitleImage", parent);
            titleRoot = RectOf(titleGo);
            SetRect(titleRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(560f, 220f));
            AddImage(titleGo, title, Color.white, true).raycastTarget = false;
            AddShadow(titleGo, new Color(0f, 0f, 0f, 0.58f), new Vector2(0f, -14f));
        }
        else
        {
            titleRoot = null;
        }

        GameObject quoteGo = CreateUIObject("QuoteText", parent);
        SetRect(RectOf(quoteGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), hasBakedScreen ? new Vector2(0f, 40f) : new Vector2(0f, 118f), new Vector2(760f, 88f));
        quoteText = AddText(quoteGo, "O vinho esconde verdades que a mente nao quer ver.", tmpFont, 34, FontStyles.Italic, TextAlignmentOptions.Center, new Color32(231, 220, 203, 255));
        quoteText.textWrappingMode = TextWrappingModes.Normal;
        AddShadow(quoteGo, new Color(0f, 0f, 0f, 0.45f), new Vector2(0f, -4f));

        GameObject spinnerGo = CreateUIObject("LoadingSpinner", parent);
        SetRect(RectOf(spinnerGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), hasBakedScreen ? new Vector2(0f, -40f) : new Vector2(0f, -2f), new Vector2(210f, 210f));
        spinnerRoot = RectOf(spinnerGo);
        if (loadingWineRing != null)
        {
            GameObject ringGo = CreateUIObject("WineRing", spinnerGo.transform);
            SetRect(RectOf(ringGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180f, 180f));
            AddImage(ringGo, loadingWineRing, Color.white, true).raycastTarget = false;
        }
        else
        {
            BuildSpinnerDots(spinnerGo.transform);
        }

        GameObject loadingGo = CreateUIObject("LoadingStatusText", parent);
        SetRect(RectOf(loadingGo), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), hasBakedScreen ? new Vector2(0f, -188f) : new Vector2(0f, -154f), new Vector2(420f, 40f));
        loadingText = AddText(loadingGo, "A CARREGAR...", tmpFont, 28, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(235, 223, 196, 255));
        loadingText.characterSpacing = 6f;
        AddShadow(loadingGo, new Color(0f, 0f, 0f, 0.4f), new Vector2(0f, -3f));

        GameObject hintGo = CreateUIObject("HintText", parent);
        SetRect(RectOf(hintGo), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), hasBakedScreen ? new Vector2(0f, 140f) : new Vector2(0f, 62f), new Vector2(920f, 36f));
        TMP_Text hintText = AddText(hintGo, "DICA: Ouve os sons à tua volta. Cada detalhe pode esconder uma pista.", tmpFont, 22, FontStyles.SmallCaps, TextAlignmentOptions.Center, new Color32(198, 183, 152, 255));
        hintText.characterSpacing = 2f;
        AddShadow(hintGo, new Color(0f, 0f, 0f, 0.36f), new Vector2(0f, -2f));
    }

    private static GameObject CreateOptionsPanel(Transform parent, TMP_FontAsset tmpFont, Sprite background, Sprite vignette, Sprite title, Sprite panelWood, Sprite screenSettings, Sprite tabSelectedSprite, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, MenuAudioManager audioManager)
    {
        GameObject root = CreateUIObject("OptionsPanel", parent);
        StretchFull(RectOf(root));
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        MenuPanelController panelController = root.AddComponent<MenuPanelController>();
        SetObjectField(panelController, "canvasGroup", canvasGroup);
        SetObjectField(panelController, "panelTransform", RectOf(root));
        SetVector2Field(panelController, "hiddenOffset", new Vector2(0f, -26f));
        SetFloatField(panelController, "transitionDuration", 0.28f);

        GameObject settingsBackground = CreateUIObject("SettingsBackground", root.transform);
        StretchFull(RectOf(settingsBackground));
        CanvasGroup backgroundGroup = settingsBackground.AddComponent<CanvasGroup>();
        bool hasBakedScreen = title == null;

        GameObject bgGo = CreateUIObject("Background", settingsBackground.transform);
        StretchFull(RectOf(bgGo));
        AddImage(bgGo, background, Color.white, false).raycastTarget = false;

        if (screenSettings != null)
        {
            GameObject moodGo = CreateUIObject("ReferenceMood", settingsBackground.transform);
            StretchFull(RectOf(moodGo));
            AddImage(moodGo, screenSettings, new Color(1f, 1f, 1f, 0.08f), false).raycastTarget = false;
        }

        GameObject veilGo = CreateUIObject("ReadabilityOverlay", settingsBackground.transform);
        StretchFull(RectOf(veilGo));
        AddImage(veilGo, null, hasBakedScreen ? new Color(0.02f, 0.015f, 0.02f, 0.06f) : new Color(0.025f, 0.02f, 0.022f, 0.8f), false).raycastTarget = false;

        GameObject vignetteGo = CreateUIObject("VignetteOverlay", settingsBackground.transform);
        StretchFull(RectOf(vignetteGo));
        AddImage(vignetteGo, vignette, Color.white, false).raycastTarget = false;

        if (title != null)
        {
            GameObject logoGo = CreateUIObject("LogoImage", root.transform);
            logoGo.AddComponent<CanvasGroup>();
            SetRect(RectOf(logoGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -24f), new Vector2(220f, 88f));
            AddImage(logoGo, title, Color.white, true).raycastTarget = false;
            AddShadow(logoGo, new Color(0f, 0f, 0f, 0.5f), new Vector2(0f, -8f));
        }

        GameObject frameGo = CreateUIObject("MainSettingsFrame", root.transform);
        RectTransform frameRect = RectOf(frameGo);
        SetRect(frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, -8f), new Vector2(1240f, 740f));
        Image frameImage = AddImage(frameGo, hasBakedScreen ? null : panelWood, hasBakedScreen ? new Color(1f, 1f, 1f, 0f) : new Color(1f, 1f, 1f, 0.96f), false);
        frameImage.type = Image.Type.Sliced;
        if (!hasBakedScreen)
        {
            AddShadow(frameGo, new Color(0f, 0f, 0f, 0.52f), new Vector2(0f, -16f));
        }

        GameObject frameShade = CreateUIObject("FrameShade", frameGo.transform);
        StretchFull(RectOf(frameShade));
        AddImage(frameShade, null, hasBakedScreen ? new Color(0.06f, 0.035f, 0.028f, 0f) : new Color(0.06f, 0.035f, 0.028f, 0.42f), false).raycastTarget = false;
        frameShade.transform.SetAsFirstSibling();

        GameObject innerFrame = CreateUIObject("InnerFrame", frameGo.transform);
        StretchFull(RectOf(innerFrame));
        RectOf(innerFrame).offsetMin = new Vector2(24f, 24f);
        RectOf(innerFrame).offsetMax = new Vector2(-24f, -24f);
        AddImage(innerFrame, null, hasBakedScreen ? new Color(0.08f, 0.055f, 0.045f, 0f) : new Color(0.08f, 0.055f, 0.045f, 0.84f), false).raycastTarget = false;

        GameObject leftTabs = CreateUIObject("LeftTabsArea", frameGo.transform);
        SetRect(RectOf(leftTabs), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -126f), new Vector2(230f, 340f));
        VerticalLayoutGroup tabLayout = leftTabs.AddComponent<VerticalLayoutGroup>();
        tabLayout.spacing = 12f;
        tabLayout.childAlignment = TextAnchor.UpperLeft;
        tabLayout.childControlHeight = false;
        tabLayout.childControlWidth = false;
        tabLayout.childForceExpandHeight = false;
        tabLayout.childForceExpandWidth = false;

        GameObject gameTab = CreateSettingsTab(leftTabs.transform, "GameTab", "JOGO", false, tmpFont, tabSelectedSprite);
        GameObject graphicsTab = CreateSettingsTab(leftTabs.transform, "GraphicsTab", "GRÁFICOS", false, tmpFont, tabSelectedSprite);
        GameObject audioTab = CreateSettingsTab(leftTabs.transform, "AudioTab", "ÁUDIO", true, tmpFont, tabSelectedSprite);
        GameObject controlsTab = CreateSettingsTab(leftTabs.transform, "ControlsTab", "CONTROLOS", false, tmpFont, tabSelectedSprite);
        GameObject languageTab = CreateSettingsTab(leftTabs.transform, "LanguageTab", "IDIOMA", false, tmpFont, tabSelectedSprite);

        GameObject rightContent = CreateUIObject("RightContentArea", frameGo.transform);
        SetRect(RectOf(rightContent), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, -112f), new Vector2(860f, 430f));

        GameObject sectionTitleGo = CreateUIObject("SectionTitle", rightContent.transform);
        SetRect(RectOf(sectionTitleGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(860f, 44f));
        TMP_Text sectionTitle = AddText(sectionTitleGo, "ÁUDIO", tmpFont, 34, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color32(241, 226, 194, 255));
        sectionTitle.characterSpacing = 6f;
        ConfigureSingleLineText(sectionTitle, 24f, 34f, TextOverflowModes.Ellipsis);

        GameObject dividerGo = CreateUIObject("SectionDivider", rightContent.transform);
        SetRect(RectOf(dividerGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -52f), new Vector2(860f, 2f));
        AddImage(dividerGo, null, new Color(0.66f, 0.54f, 0.4f, 0.34f), false).raycastTarget = false;

        GameObject sectionsRoot = CreateUIObject("SectionsRoot", rightContent.transform);
        SetRect(RectOf(sectionsRoot), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -74f), new Vector2(860f, 364f));

        GameObject gameSection = CreateUIObject("GameSection", sectionsRoot.transform);
        StretchFull(RectOf(gameSection));
        GameObject gamePlaceholder = CreateUIObject("PlaceholderText", gameSection.transform);
        SetRect(RectOf(gamePlaceholder), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(780f, 70f));
        TMP_Text gameText = AddText(gamePlaceholder, "Nenhuma opção disponível por agora.", tmpFont, 24, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color32(174, 160, 140, 255));
        ConfigureSingleLineText(gameText, 18f, 24f, TextOverflowModes.Ellipsis);

        GameObject graphicsSection = CreateUIObject("GraphicsSection", sectionsRoot.transform);
        StretchFull(RectOf(graphicsSection));
        GameObject graphicsPlaceholder = CreateUIObject("PlaceholderText", graphicsSection.transform);
        SetRect(RectOf(graphicsPlaceholder), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(780f, 70f));
        TMP_Text graphicsText = AddText(graphicsPlaceholder, "Nenhuma opção disponível por agora.", tmpFont, 24, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color32(174, 160, 140, 255));
        ConfigureSingleLineText(graphicsText, 18f, 24f, TextOverflowModes.Ellipsis);

        GameObject audioSection = CreateUIObject("AudioSection", sectionsRoot.transform);
        StretchFull(RectOf(audioSection));
        CreateAudioSliderRow(audioSection.transform, "MasterRow", "VOLUME GERAL", 0, tmpFont, out Slider masterSlider, out TMP_Text masterValueText);
        CreateAudioSliderRow(audioSection.transform, "MusicRow", "VOLUME DA MÚSICA", 1, tmpFont, out Slider musicSlider, out TMP_Text musicValueText);
        CreateAudioSliderRow(audioSection.transform, "SfxRow", "VOLUME DOS EFEITOS", 2, tmpFont, out Slider sfxSlider, out TMP_Text sfxValueText);
        CreateSelectorRow(audioSection.transform, "AudioModeRow", "MODO DE ÁUDIO", "ESTÉREO", 3, tmpFont, out Button audioModeButton, out TMP_Text audioModeValueText);
        CreateSelectorRow(audioSection.transform, "SubtitlesRow", "LEGENDAS", "LIGADO", 4, tmpFont, out Button subtitlesButton, out TMP_Text subtitlesValueText);

        GameObject controlsSection = CreateUIObject("ControlsSection", sectionsRoot.transform);
        StretchFull(RectOf(controlsSection));
        BuildControlsSection(controlsSection.transform, tmpFont);


        GameObject languageSection = CreateUIObject("LanguageSection", sectionsRoot.transform);
        StretchFull(RectOf(languageSection));
        CreateSelectorRow(languageSection.transform, "LanguageRow", "IDIOMA", "PORTUGUÊS", 0, tmpFont, out Button languageButton, out TMP_Text languageValueText);

        GameObject bottomButtons = CreateUIObject("BottomButtonsArea", frameGo.transform);
        SetRect(RectOf(bottomButtons), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(54f, 34f), new Vector2(-54f, 96f));

        AnimatedMenuButton resetButton = CreateMenuButton(bottomButtons.transform, "ResetDefaultsButton", "R  REDEFINIR PADROES", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 300f, 52f);
        SetRect(RectOf(resetButton.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 4f), new Vector2(300f, 52f));
        ConfigureMenuButtonLabel(resetButton, 10f, 18f);

        AnimatedMenuButton applyButton = CreateMenuButton(bottomButtons.transform, "ApplyButton", "APLICAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 180f, 52f);
        SetRect(RectOf(applyButton.gameObject), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(180f, 52f));
        ConfigureMenuButtonLabel(applyButton, 18f, 22f);

        AnimatedMenuButton backButton = CreateMenuButton(bottomButtons.transform, "BackButton", "ESC  VOLTAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 190f, 52f);
        SetRect(RectOf(backButton.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 4f), new Vector2(190f, 52f));
        ConfigureMenuButtonLabel(backButton, 12f, 20f);

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
        SetObjectField(view, "activeTabSprite", tabSelectedSprite);
        SetObjectField(view, "inactiveTabSprite", null);
        SetObjectArrayField(view, "tabButtons", new UnityEngine.Object[] { gameTab.GetComponent<Button>(), graphicsTab.GetComponent<Button>(), audioTab.GetComponent<Button>(), controlsTab.GetComponent<Button>(), languageTab.GetComponent<Button>() });
        SetObjectArrayField(view, "tabBackgrounds", new UnityEngine.Object[] { gameTab.GetComponent<Image>(), graphicsTab.GetComponent<Image>(), audioTab.GetComponent<Image>(), controlsTab.GetComponent<Image>(), languageTab.GetComponent<Image>() });
        SetObjectArrayField(view, "tabLabels", new UnityEngine.Object[] { FindInChildren<TMP_Text>(gameTab.transform, "Label"), FindInChildren<TMP_Text>(graphicsTab.transform, "Label"), FindInChildren<TMP_Text>(audioTab.transform, "Label"), FindInChildren<TMP_Text>(controlsTab.transform, "Label"), FindInChildren<TMP_Text>(languageTab.transform, "Label") });
        SetObjectArrayField(view, "selectedMarkers", new UnityEngine.Object[] { FindInChildren<Transform>(gameTab.transform, "SelectedMarker")?.gameObject, FindInChildren<Transform>(graphicsTab.transform, "SelectedMarker")?.gameObject, FindInChildren<Transform>(audioTab.transform, "SelectedMarker")?.gameObject, FindInChildren<Transform>(controlsTab.transform, "SelectedMarker")?.gameObject, FindInChildren<Transform>(languageTab.transform, "SelectedMarker")?.gameObject });
        SetObjectArrayField(view, "sections", new UnityEngine.Object[] { gameSection, graphicsSection, audioSection, controlsSection, languageSection });
        SetStringArrayField(view, "sectionTitles", new[] { "JOGO", "GRÁFICOS", "ÁUDIO", "CONTROLOS", "IDIOMA" });

        panelController.HideImmediate();
        return root;
    }

    private static GameObject CreateCreditsPanel(Transform parent, TMP_FontAsset tmpFont, Sprite background, Sprite vignette, Sprite title, Sprite panelWood, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, Sprite bottle, Sprite glass, Sprite newspaper, MenuAudioManager audioManager)
    {
        GameObject panelRoot = CreatePanelState("CreditsPanel", parent, tmpFont, background, vignette, title, panelWood, out GameObject bodyRoot, out MenuPanelController controller);
        BuildCreditsBackdrop(panelRoot.transform, bottle, glass, newspaper);
        BuildCreditsPaper(bodyRoot, tmpFont);

        AnimatedMenuButton backButton = CreateMenuButton(panelRoot.transform, "BackButton", "VOLTAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 250f, 66f);
        SetRect(RectOf(backButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(250f, 66f));
        controller.HideImmediate();
        return panelRoot;
    }

    private static GameObject CreateSlotSelectionPanel(Transform parent, TMP_FontAsset tmpFont, Sprite panelWood, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, MenuAudioManager audioManager)
    {
        GameObject root = CreateUIObject("SlotSelectionPanel", parent);
        StretchFull(RectOf(root));
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        MenuPanelController panel = root.AddComponent<MenuPanelController>();
        SetObjectField(panel, "canvasGroup", canvasGroup);
        SetObjectField(panel, "panelTransform", RectOf(root));
        SetVector2Field(panel, "hiddenOffset", new Vector2(0f, -20f));

        GameObject frame = CreateUIObject("PanelFrame", root.transform);
        SetRect(RectOf(frame), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, -8f), new Vector2(1240f, 740f));
        Image frameImage = AddImage(frame, panelWood, new Color(1f, 1f, 1f, 0.96f), false);
        frameImage.type = Image.Type.Sliced;
        AddShadow(frame, new Color(0f, 0f, 0f, 0.52f), new Vector2(0f, -16f));

        GameObject titleGo = CreateUIObject("Title", frame.transform);
        SetRect(RectOf(titleGo), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(-140f, 40f));
        TMP_Text titleText = AddText(titleGo, "ESCOLHER SLOT", tmpFont, 30, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(241, 226, 194, 255));
        ConfigureSingleLineText(titleText, 18f, 30f, TextOverflowModes.Ellipsis);

        GameObject slotArea = CreateUIObject("SlotArea", frame.transform);
        SetRect(RectOf(slotArea), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(-56f, 500f));
        HorizontalLayoutGroup slotsLayout = slotArea.AddComponent<HorizontalLayoutGroup>();
        slotsLayout.spacing = 20f;
        slotsLayout.childAlignment = TextAnchor.UpperCenter;
        slotsLayout.childControlWidth = false;
        slotsLayout.childControlHeight = false;
        slotsLayout.childForceExpandWidth = false;
        slotsLayout.childForceExpandHeight = false;

        SaveSlotButton slot1 = CreateSlotCard(slotArea.transform, tmpFont, buttonNormal, 1);
        SaveSlotButton slot2 = CreateSlotCard(slotArea.transform, tmpFont, buttonNormal, 2);
        SaveSlotButton slot3 = CreateSlotCard(slotArea.transform, tmpFont, buttonNormal, 3);

        AnimatedMenuButton backButton = CreateMenuButton(frame.transform, "BackButton", "VOLTAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 220f, 58f);
        SetRect(RectOf(backButton.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(220f, 58f));
        ConfigureMenuButtonLabel(backButton, 14f, 22f);

        SlotSelectionController controller = root.AddComponent<SlotSelectionController>();
        SetObjectField(controller, "panel", panel);
        SetObjectField(controller, "titleText", titleText);
        SetObjectArrayField(controller, "slotButtons", new UnityEngine.Object[] { slot1, slot2, slot3 });
        SetObjectField(controller, "backButton", backButton);

        panel.HideImmediate();
        return root;
    }

    private static SaveSlotButton CreateSlotCard(Transform parent, TMP_FontAsset tmpFont, Sprite cardSprite, int slotIndex)
    {
        GameObject card = CreateUIObject($"Slot{slotIndex}", parent);
        RectTransform cardRect = RectOf(card);
        cardRect.sizeDelta = new Vector2(350f, 420f);
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.preferredWidth = 350f;
        layout.preferredHeight = 420f;

        Image bg = AddImage(card, cardSprite, new Color(0.2f, 0.1f, 0.1f, 0.9f), false);
        bg.type = Image.Type.Sliced;
        Button button = card.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        GameObject title = CreateUIObject("Title", card.transform);
        SetRect(RectOf(title), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-22f, 40f));
        TMP_Text titleText = AddText(title, $"SLOT {slotIndex}", tmpFont, 28, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(238, 223, 194, 255));
        ConfigureSingleLineText(titleText, 18f, 28f, TextOverflowModes.Ellipsis);

        GameObject status = CreateUIObject("Status", card.transform);
        SetRect(RectOf(status), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(-22f, 32f));
        TMP_Text statusText = AddText(status, "Slot vazio", tmpFont, 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color32(220, 205, 178, 255));
        ConfigureSingleLineText(statusText, 14f, 22f, TextOverflowModes.Ellipsis);

        GameObject details = CreateUIObject("Details", card.transform);
        SetRect(RectOf(details), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(-26f, 64f));
        TMP_Text detailsText = AddText(details, "Novo Jogo", tmpFont, 20, FontStyles.Normal, TextAlignmentOptions.Top, new Color32(204, 188, 161, 255));
        detailsText.alignment = TextAlignmentOptions.TopLeft;
        detailsText.textWrappingMode = TextWrappingModes.Normal;
        detailsText.margin = new Vector4(8f, 0f, 8f, 0f);

        GameObject infoDivider = CreateUIObject("InfoDivider", card.transform);
        SetRect(RectOf(infoDivider), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -228f), new Vector2(-34f, 2f));
        AddImage(infoDivider, null, new Color(0.72f, 0.57f, 0.43f, 0.30f), false).raycastTarget = false;

        GameObject preview = CreateUIObject("Preview", card.transform);
        SetRect(RectOf(preview), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(-26f, 138f));
        Image previewImage = AddImage(preview, null, new Color(1f, 1f, 1f, 0.08f), false);
        previewImage.raycastTarget = false;
        details.transform.SetAsLastSibling();

        GameObject footer = CreateUIObject("Footer", card.transform);
        SetRect(RectOf(footer), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-22f, 30f));
        TMP_Text footerText = AddText(footer, "Novo Jogo", tmpFont, 20, FontStyles.SmallCaps, TextAlignmentOptions.Center, new Color32(233, 217, 190, 255));
        ConfigureSingleLineText(footerText, 14f, 20f, TextOverflowModes.Ellipsis);

        GameObject disabledOverlay = CreateUIObject("DisabledOverlay", card.transform);
        StretchFull(RectOf(disabledOverlay));
        AddImage(disabledOverlay, null, new Color(0f, 0f, 0f, 0.5f), false).raycastTarget = false;
        disabledOverlay.SetActive(false);

        SaveSlotButton slot = card.AddComponent<SaveSlotButton>();
        SetIntegerField(slot, "slotIndex", slotIndex);
        SetObjectField(slot, "button", button);
        SetObjectField(slot, "backgroundImage", bg);
        SetObjectField(slot, "titleText", titleText);
        SetObjectField(slot, "statusText", statusText);
        SetObjectField(slot, "detailsText", detailsText);
        SetObjectField(slot, "footerText", footerText);
        SetObjectField(slot, "previewImage", previewImage);
        SetObjectField(slot, "disabledOverlay", disabledOverlay);
        return slot;
    }

    private static GameObject CreateConfirmOverwritePanel(Transform parent, TMP_FontAsset tmpFont, Sprite panelWood, Sprite buttonNormal, Sprite buttonHover, Sprite buttonPressed, Sprite buttonGlow, MenuAudioManager audioManager)
    {
        GameObject root = CreateUIObject("ConfirmOverwritePanel", parent);
        StretchFull(RectOf(root));

        GameObject shade = CreateUIObject("Shade", root.transform);
        StretchFull(RectOf(shade));
        AddImage(shade, null, new Color(0f, 0f, 0f, 0.6f), false).raycastTarget = true;

        GameObject panelRoot = CreateUIObject("Panel", root.transform);
        SetRect(RectOf(panelRoot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, -8f), new Vector2(860f, 340f));
        Image panelImage = AddImage(panelRoot, panelWood, new Color(1f, 1f, 1f, 0.97f), false);
        panelImage.type = Image.Type.Sliced;

        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        MenuPanelController menuPanel = root.AddComponent<MenuPanelController>();
        SetObjectField(menuPanel, "canvasGroup", canvasGroup);
        SetObjectField(menuPanel, "panelTransform", RectOf(root));
        SetVector2Field(menuPanel, "hiddenOffset", new Vector2(0f, -14f));

        GameObject msg = CreateUIObject("Message", panelRoot.transform);
        SetRect(RectOf(msg), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(-70f, 172f));
        TMP_Text msgText = AddText(msg, "Este slot ja tem um jogo guardado. Queres apagar este progresso e comecar um novo jogo?", tmpFont, 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color32(233, 218, 191, 255));
        msgText.enableAutoSizing = true;
        msgText.fontSizeMin = 16f;
        msgText.fontSizeMax = 24f;
        msgText.textWrappingMode = TextWrappingModes.Normal;

        AnimatedMenuButton confirm = CreateMenuButton(panelRoot.transform, "ConfirmButton", "SIM, APAGAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 260f, 58f);
        SetRect(RectOf(confirm.gameObject), new Vector2(0.34f, 0f), new Vector2(0.34f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(260f, 58f));
        ConfigureMenuButtonLabel(confirm, 12f, 20f);

        AnimatedMenuButton cancel = CreateMenuButton(panelRoot.transform, "CancelButton", "CANCELAR", null, tmpFont, buttonNormal, buttonHover, buttonPressed, buttonGlow, audioManager, 220f, 58f);
        SetRect(RectOf(cancel.gameObject), new Vector2(0.66f, 0f), new Vector2(0.66f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(220f, 58f));
        ConfigureMenuButtonLabel(cancel, 12f, 20f);

        ConfirmOverwritePanel controller = root.AddComponent<ConfirmOverwritePanel>();
        SetObjectField(controller, "panel", menuPanel);
        SetObjectField(controller, "messageText", msgText);
        SetObjectField(controller, "confirmButton", confirm);
        SetObjectField(controller, "cancelButton", cancel);

        menuPanel.HideImmediate();
        return root;
    }

    private static GameObject CreatePanelState(string name, Transform parent, TMP_FontAsset tmpFont, Sprite background, Sprite vignette, Sprite title, Sprite panelWood, out GameObject bodyRoot, out MenuPanelController panelController)
    {
        GameObject root = CreateUIObject(name, parent);
        StretchFull(RectOf(root));
        CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
        panelController = root.AddComponent<MenuPanelController>();
        SetObjectField(panelController, "canvasGroup", canvasGroup);
        SetObjectField(panelController, "panelTransform", RectOf(root));
        SetVector2Field(panelController, "hiddenOffset", new Vector2(0f, -24f));
        SetFloatField(panelController, "transitionDuration", 0.24f);

        GameObject bgGo = CreateUIObject("Background", root.transform);
        StretchFull(RectOf(bgGo));
        AddImage(bgGo, background, Color.white, false).raycastTarget = false;
        bool hasBakedScreen = title == null;

        GameObject veilGo = CreateUIObject("ReadabilityOverlay", root.transform);
        StretchFull(RectOf(veilGo));
        AddImage(veilGo, null, hasBakedScreen ? new Color(0.04f, 0.025f, 0.03f, 0.12f) : new Color(0.04f, 0.025f, 0.03f, 0.72f), false).raycastTarget = false;

        GameObject vignetteGo = CreateUIObject("VignetteOverlay", root.transform);
        StretchFull(RectOf(vignetteGo));
        AddImage(vignetteGo, vignette, Color.white, false).raycastTarget = false;

        if (title != null)
        {
            GameObject titleGo = CreateUIObject("TitleImage", root.transform);
            SetRect(RectOf(titleGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(78f, -58f), new Vector2(360f, 148f));
            AddImage(titleGo, title, Color.white, true).raycastTarget = false;
        }

        bodyRoot = CreateUIObject("PanelBody", root.transform);
        SetRect(RectOf(bodyRoot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), hasBakedScreen ? new Vector2(-86f, -2f) : new Vector2(180f, -2f), hasBakedScreen ? new Vector2(720f, 760f) : new Vector2(760f, 470f));
        Image bodyImage = AddImage(bodyRoot, hasBakedScreen ? null : panelWood, hasBakedScreen ? new Color(1f, 1f, 1f, 0f) : new Color(1f, 1f, 1f, 0.97f), false);
        bodyImage.type = Image.Type.Sliced;
        if (!hasBakedScreen)
        {
            AddShadow(bodyRoot, new Color(0f, 0f, 0f, 0.48f), new Vector2(0f, -14f));
        }

        GameObject shadeGo = CreateUIObject("PanelShade", bodyRoot.transform);
        StretchFull(RectOf(shadeGo));
        AddImage(shadeGo, null, hasBakedScreen ? new Color(0.06f, 0.03f, 0.02f, 0f) : new Color(0.06f, 0.03f, 0.02f, 0.32f), false).raycastTarget = false;
        shadeGo.transform.SetAsFirstSibling();

        return root;
    }

    private static void BuildCreditsBackdrop(Transform root, Sprite bottle, Sprite glass, Sprite newspaper)
    {
        if (!CreateDecorationsByDefault)
        {
            return;
        }

        GameObject decorationRoot = CreateUIObject("CreditsDecorations", root);
        StretchFull(RectOf(decorationRoot));
        decorationRoot.transform.SetSiblingIndex(3);

        GameObject bottleGo = CreateUIObject("WineBottle", decorationRoot.transform);
        SetRect(RectOf(bottleGo), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-124f, 38f), new Vector2(266f, 598f));
        AddImage(bottleGo, bottle, new Color(1f, 1f, 1f, 0.72f), true).raycastTarget = false;
        RectOf(bottleGo).localRotation = Quaternion.Euler(0f, 0f, -2f);

        GameObject glassGo = CreateUIObject("WineGlass", decorationRoot.transform);
        SetRect(RectOf(glassGo), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-82f, 122f), new Vector2(188f, 296f));
        AddImage(glassGo, glass, new Color(1f, 1f, 1f, 0.88f), true).raycastTarget = false;
        RectOf(glassGo).localRotation = Quaternion.Euler(0f, 0f, -4f);

        GameObject newspaperGo = CreateUIObject("Newspaper", decorationRoot.transform);
        SetRect(RectOf(newspaperGo), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-202f, 54f), new Vector2(242f, 164f));
        AddImage(newspaperGo, newspaper, new Color(1f, 1f, 1f, 0.26f), true).raycastTarget = false;
        RectOf(newspaperGo).localRotation = Quaternion.Euler(0f, 0f, -12f);
    }

    private static void BuildCreditsPaper(GameObject bodyRoot, TMP_FontAsset tmpFont)
    {
        RectTransform bodyRect = RectOf(bodyRoot);
        // Centralizar bem e manter um tamanho adequado para o texto rolar no meio do ecrã
        SetRect(bodyRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(692f, 782f));
        // Sem caixa não faz sentido ter o texto inclinado, vamos colocar a direito (0 graus)
        bodyRect.localRotation = Quaternion.identity;

        Image bodyImage = bodyRoot.GetComponent<Image>();
        if (bodyImage != null)
        {
            // Remover a caixa visual
            bodyImage.enabled = false;
        }

        Shadow bodyShadow = bodyRoot.GetComponent<Shadow>();
        if (bodyShadow != null)
        {
            // Remover a sombra da caixa
            bodyShadow.enabled = false;
        }

        Transform shade = bodyRoot.transform.Find("PanelShade");
        if (shade != null && shade.TryGetComponent(out Image shadeImage))
        {
            // Remover o escurecimento do fundo da caixa
            shadeImage.enabled = false;
        }

        // --- ECRÃ DE CRÉDITOS ROLANTE (SCROLL) ---
        
        // 1. Criar o Viewport (Área de visão visível com máscara)
        GameObject viewport = CreateUIObject("Viewport", bodyRoot.transform);
        StretchFull(RectOf(viewport));
        // Margens simétricas e equilibradas para o texto não ficar colado às bordas do papel
        RectOf(viewport).offsetMin = new Vector2(40f, 60f); 
        RectOf(viewport).offsetMax = new Vector2(-40f, -40f);
        
        // Usar RectMask2D em vez de Mask+Image para não criar quadrados de fundo feios
        viewport.AddComponent<UnityEngine.UI.RectMask2D>();
        
        // Adicionar um Image transparente apenas para apanhar os eventos do rato (scroll)
        UnityEngine.UI.Image viewportImage = AddImage(viewport, null, new Color(0, 0, 0, 0), false);
        viewportImage.raycastTarget = true;

        // 2. Criar o Content (O recipiente elástico onde ficam os nomes)
        GameObject contentGo = CreateUIObject("CreditsContent", viewport.transform);
        // Ancorar ao topo para crescer para baixo
        SetRect(RectOf(contentGo), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        
        ContentSizeFitter sizeFitter = contentGo.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        VerticalLayoutGroup layoutGroup = contentGo.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        // Padding inferior mais pequeno para garantir que o scroll pára quando a última linha atinge o fundo
        layoutGroup.padding = new RectOffset(20, 20, 80, 100); 
        layoutGroup.spacing = 16f;

        // 3. Adicionar o ScrollRect
        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = RectOf(contentGo);
        scrollRect.viewport = RectOf(viewport);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 35f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // 4. Adicionar o novo Script de Animação (AutoScroll)
        AutoScrollCredits autoScroll = viewport.AddComponent<AutoScrollCredits>();
        
        // --- CONTEÚDO TIPOGRÁFICO ---
        
        // Título Principal (Agora em tons dourados/claros para ler na madeira escura)
        GameObject titleGo = CreateUIObject("CreditsTitle", contentGo.transform);
        TMP_Text titleText = AddText(titleGo, "CRÉDITOS", tmpFont, 44, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(241, 226, 194, 255));
        titleText.characterSpacing = 8f;
        titleGo.AddComponent<LayoutElement>().minHeight = 60f;

        // Divisor Elegante (Cor dourada clara)
        GameObject dividerGo = CreateUIObject("HeaderDivider", contentGo.transform);
        LayoutElement dividerLayout = dividerGo.AddComponent<LayoutElement>();
        dividerLayout.minHeight = 2f;
        dividerLayout.preferredHeight = 2f;
        GameObject dividerLine = CreateUIObject("Line", dividerGo.transform);
        SetRect(RectOf(dividerLine), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 2f));
        AddImage(dividerLine, null, new Color(0.86f, 0.74f, 0.54f, 0.38f), false).raycastTarget = false;
        
        // Espaço de Respiração
        GameObject spacer1 = CreateUIObject("Spacer", contentGo.transform);
        spacer1.AddComponent<LayoutElement>().minHeight = 60f;

        // Texto dos Créditos (Cores claras ajustadas para madeira escura)
        string richText = 
            "<size=20><color=#a89678><b>DESENVOLVIDO POR</b></color></size>\n" +
            "<size=32><color=#f3e4c2>Rafa Team</color></size>\n\n\n" +
            
            "<size=20><color=#a89678><b>DESIGN</b></color></size>\n" +
            "<size=32><color=#f3e4c2>Rafa Silva</color></size>\n\n\n" +
            
            "<size=20><color=#a89678><b>PROGRAMAÇÃO</b></color></size>\n" +
            "<size=32><color=#f3e4c2>Rafa Silva</color></size>\n\n\n" +
            
            "<size=20><color=#a89678><b>ARTE</b></color></size>\n" +
            "<size=32><color=#f3e4c2>Rafa Silva</color></size>\n\n\n" +
            
            "<size=20><color=#a89678><b>MÚSICA</b></color></size>\n" +
            "<size=32><color=#f3e4c2>Biblioteca de Áudio do Unity</color></size>\n\n\n" +
            
            "<size=20><color=#a89678><b>AGRADECIMENTOS</b></color></size>\n" +
            "<size=32><color=#f3e4c2>A todos que apoiaram este projeto.</color></size>\n\n\n\n\n" +
            
            "<size=38><color=#f1e2c2><i>Obrigado por jogar!</i></color></size>\n\n";

        GameObject textGo = CreateUIObject("CreditsBody", contentGo.transform);
        TMP_Text contentText = AddText(textGo, richText, tmpFont, 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color32(241, 226, 194, 255));
        contentText.lineSpacing = 16f;
    }

    private static void CreatePaperBlotch(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color, float rotation)
    {
        GameObject blotchGo = CreateUIObject(name, parent);
        SetRect(RectOf(blotchGo), anchor, anchor, anchor, anchoredPosition, size);
        AddImage(blotchGo, null, color, false).raycastTarget = false;
        RectOf(blotchGo).localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static GameObject CreateSettingsTab(Transform parent, string name, string label, bool selected, TMP_FontAsset tmpFont, Sprite selectedSprite)
    {
        GameObject tabGo = CreateUIObject(name, parent);
        RectTransform rect = RectOf(tabGo);
        rect.sizeDelta = new Vector2(190f, 55f);
        LayoutElement layout = tabGo.AddComponent<LayoutElement>();
        layout.preferredWidth = 190f;
        layout.preferredHeight = 55f;

        bool useSelectedSprite = selected && selectedSprite != null;
        Image background = AddImage(tabGo, useSelectedSprite ? selectedSprite : null, selected ? new Color(0.3f, 0.08f, 0.08f, 0.95f) : new Color(0.11f, 0.085f, 0.08f, 0.92f), false);
        background.raycastTarget = true;
        AddShadow(tabGo, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -5f));

        Button button = tabGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = selected ? Color.white : background.color;
        colors.highlightedColor = selected ? new Color(1f, 0.96f, 0.96f, 1f) : new Color(0.18f, 0.12f, 0.1f, 1f);
        colors.pressedColor = selected ? new Color(0.94f, 0.88f, 0.88f, 1f) : new Color(0.42f, 0.16f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        if (selected && selectedSprite == null)
        {
            GameObject marker = CreateUIObject("SelectedMarker", tabGo.transform);
            SetRect(RectOf(marker), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-8f, 0f), new Vector2(12f, 20f));
            AddImage(marker, null, new Color(0.62f, 0.15f, 0.16f, 1f), false).raycastTarget = false;
        }

        GameObject labelGo = CreateUIObject("Label", tabGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-20f, 0f));
        TMP_Text text = AddText(labelGo, label, tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, selected ? new Color32(243, 228, 194, 255) : new Color32(168, 154, 129, 255));
        text.characterSpacing = 2f;
        ConfigureSingleLineText(text, 16f, 22f, TextOverflowModes.Ellipsis);

        return tabGo;
    }

    private static void CreateAudioSliderRow(Transform parent, string name, string label, int rowIndex, TMP_FontAsset tmpFont, out Slider slider, out TMP_Text valueText)
    {
        float rowY = -rowIndex * 70f;
        GameObject rowGo = CreateUIObject(name, parent);
        SetRect(RectOf(rowGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(780f, 70f));

        GameObject labelGo = CreateUIObject("Label", rowGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(330f, 70f));
        TMP_Text labelText = AddText(labelGo, label, tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color32(218, 205, 181, 255));
        labelText.characterSpacing = 2f;
        ConfigureSingleLineText(labelText, 16f, 22f, TextOverflowModes.Ellipsis);

        GameObject sliderGo = CreateSlider(name + "Slider", rowGo.transform, Vector2.zero);
        RectTransform sliderRect = RectOf(sliderGo);
        SetRect(sliderRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(360f, 0f), new Vector2(280f, 26f));
        slider = sliderGo.GetComponent<Slider>();

        GameObject valueGo = CreateUIObject("ValueText", rowGo.transform);
        SetRect(RectOf(valueGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(670f, 0f), new Vector2(80f, 70f));
        valueText = AddText(valueGo, "80%", tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color32(217, 205, 181, 255));
        ConfigureSingleLineText(valueText, 16f, 22f, TextOverflowModes.Ellipsis);
    }

    private static void CreateSelectorRow(Transform parent, string name, string label, string initialValue, int rowIndex, TMP_FontAsset tmpFont, out Button button, out TMP_Text valueText)
    {
        float rowY = -rowIndex * 70f;
        GameObject rowGo = CreateUIObject(name, parent);
        SetRect(RectOf(rowGo), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, rowY), new Vector2(780f, 70f));

        GameObject labelGo = CreateUIObject("Label", rowGo.transform);
        SetRect(RectOf(labelGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(330f, 70f));
        TMP_Text labelText = AddText(labelGo, label, tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color32(218, 205, 181, 255));
        labelText.characterSpacing = 2f;
        ConfigureSingleLineText(labelText, 16f, 22f, TextOverflowModes.Ellipsis);

        GameObject selectorGo = CreateUIObject("SelectorButton", rowGo.transform);
        SetRect(RectOf(selectorGo), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(360f, 0f), new Vector2(260f, 42f));
        Image selectorBg = AddImage(selectorGo, null, new Color(0.09f, 0.075f, 0.07f, 0.94f), false);
        AddOutline(selectorGo, new Color(0.49f, 0.39f, 0.28f, 0.6f));
        button = selectorGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = selectorBg.color;
        colors.highlightedColor = new Color(0.14f, 0.11f, 0.1f, 1f);
        colors.pressedColor = new Color(0.18f, 0.14f, 0.13f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;

        GameObject valueGo = CreateUIObject("ValueText", selectorGo.transform);
        SetRect(RectOf(valueGo), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 0f), new Vector2(-30f, 0f));
        valueText = AddText(valueGo, initialValue, tmpFont, 22, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.Center, new Color32(225, 212, 188, 255));
        ConfigureSingleLineText(valueText, 16f, 22f, TextOverflowModes.Ellipsis);

        GameObject arrowGo = CreateUIObject("Arrow", selectorGo.transform);
        SetRect(RectOf(arrowGo), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, -1f), new Vector2(18f, 18f));
        TMP_Text arrow = AddText(arrowGo, ">", tmpFont, 18, FontStyles.Bold, TextAlignmentOptions.Center, new Color32(185, 171, 148, 255));
        ConfigureSingleLineText(arrow, 14f, 18f, TextOverflowModes.Truncate);
        RectOf(arrowGo).localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    private static void BuildControlsSection(Transform parent, TMP_FontAsset tmpFont)
    {
        GameObject viewportGo = CreateUIObject("ControlsViewport", parent);
        StretchFull(RectOf(viewportGo));
        Image viewportImage = AddImage(viewportGo, null, new Color(0f, 0f, 0f, 0.001f), false);
        viewportImage.raycastTarget = true;
        Mask mask = viewportGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentGo = CreateUIObject("ControlsContent", viewportGo.transform);
        RectTransform contentRect = RectOf(contentGo);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(780f, 770f);
        contentRect.localScale = Vector3.one;

        CreateSelectorRow(contentGo.transform, "ForwardRow", "ANDAR PARA A FRENTE", "W", 0, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "LeftRow", "ANDAR PARA A ESQUERDA", "A", 1, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "BackRow", "ANDAR PARA TRÃS", "S", 2, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "RightRow", "ANDAR PARA A DIREITA", "D", 3, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "RunRow", "CORRER", "SHIFT", 4, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "InteractRow", "INTERACAO", "F", 5, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "AttackRow", "ATAQUE", "J", 6, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "HeavyAttackRow", "ATAQUE PESADO", "K", 7, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "DefendRow", "DEFENDER", "L", 8, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "JumpRow", "SALTAR", "SPACE", 9, tmpFont, out _, out _);
        CreateSelectorRow(contentGo.transform, "RollRow", "REBOLAR", "C", 10, tmpFont, out _, out _);

        ScrollRect scrollRect = parent.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = RectOf(viewportGo);
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 36f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private static void BuildSpinnerDots(Transform parent)
    {
        const int dotCount = 12;
        const float radius = 70f;
        for (int i = 0; i < dotCount; i++)
        {
            float t = i / (float)dotCount;
            float angle = t * Mathf.PI * 2f;
            GameObject dot = CreateUIObject($"Dot{i + 1}", parent);
            SetRect(RectOf(dot), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius), new Vector2(16f, 16f));
            Color color = Color.Lerp(new Color(0.34f, 0.06f, 0.06f, 0.22f), new Color(0.8f, 0.22f, 0.22f, 0.98f), t);
            AddImage(dot, null, color, false).raycastTarget = false;
        }
    }

    private static void BuildLoadingParticles(Transform parent)
    {
        Vector2[] positions =
        {
            new(-420f, 150f), new(-280f, 30f), new(-180f, -120f), new(-40f, 210f),
            new(120f, 90f), new(240f, -40f), new(360f, 160f), new(420f, -110f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject mote = CreateUIObject($"Mote{i + 1}", parent);
            SetRect(RectOf(mote), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), positions[i], new Vector2(10f + (i % 3) * 6f, 10f + (i % 3) * 6f));
            AddImage(mote, null, new Color(0.72f, 0.11f, 0.11f, 0.28f), false).raycastTarget = false;
            LoadingParticleDrift drift = mote.AddComponent<LoadingParticleDrift>();
            SetFloatField(drift, "horizontalAmplitude", 10f + i * 1.4f);
            SetFloatField(drift, "verticalAmplitude", 16f + i * 2.4f);
            SetFloatField(drift, "speed", 0.18f + i * 0.035f);
            SetFloatField(drift, "alphaMin", 0.06f);
            SetFloatField(drift, "alphaMax", 0.24f + (i % 3) * 0.06f);
            SetFloatField(drift, "phaseOffset", i * 0.7f);
        }
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
        SetRect(RectOf(sliderGo), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(-140f, 26f));

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

    private static GameObject CreateToggle(string name, Transform parent, TMP_FontAsset tmpFont, string label, Vector2 anchoredPosition)
    {
        GameObject toggleGo = CreateUIObject(name, parent);
        SetRect(RectOf(toggleGo), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(-140f, 34f));
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
        GameObject labelGo = CreateUIObject($"{textValue}Label", parent);
        SetRect(RectOf(labelGo), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(-140f, 28f));
        TMP_Text label = AddText(labelGo, textValue, tmpFont, 24, FontStyles.SmallCaps | FontStyles.Bold, TextAlignmentOptions.BottomLeft, new Color32(238, 224, 191, 255));
        label.margin = new Vector4(42f, 0f, 0f, 0f);
    }

    private static void WireBackButton(Transform panelRoot, MainMenuController controller, bool creditsPanel)
    {
        Button backButton = FindInChildren<Button>(panelRoot, "BackButton");
        if (backButton == null)
        {
            return;
        }

        if (creditsPanel)
        {
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.ReturnFromCredits);
        }
        else
        {
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.RequestCloseOptionsPanel);
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
            "icon_exit_door.png",
            "decor_wine_bottle.png",
            "decor_wine_glass.png",
            "decor_newspaper.png"
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

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

    }

    private static Sprite LoadSprite(string fileName)
    {
        string path = GeneratedPath + fileName;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new FileNotFoundException("Missing sprite at " + path);
        }

        return sprite;
    }

    private static Sprite LoadOptionalGeneratedSprite(string fileName)
    {
        string path = GeneratedPath + fileName;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AudioClip LoadAudioClip(string fileName)
    {
        string path = AudioPath + fileName;
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            throw new FileNotFoundException("Missing audio clip at " + path);
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

    private static void CreateCamera()
    {
        GameObject cameraGo = new("Main Camera", typeof(Camera), typeof(AudioListener));
        Camera camera = cameraGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.025f, 0.03f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraGo.tag = "MainCamera";
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemGo = new("EventSystem", typeof(EventSystem));
        Type inputModuleType =
            Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem") ??
            Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI");

        if (inputModuleType != null)
        {
            eventSystemGo.AddComponent(inputModuleType);
        }
        else
        {
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }
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

    private static void ConfigureSingleLineText(TMP_Text text, float minSize, float maxSize, TextOverflowModes overflow)
    {
        if (text == null)
        {
            return;
        }

        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.overflowMode = overflow;
    }

    private static void ConfigureMenuButtonLabel(AnimatedMenuButton button, float minSize, float maxSize)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text label = FindInChildren<TMP_Text>(button.transform, "Label");
        if (label != null)
        {
            ConfigureSingleLineText(label, minSize, maxSize, TextOverflowModes.Ellipsis);
            label.alignment = TextAlignmentOptions.Center;
            label.characterSpacing = 1.5f;
            label.margin = Vector4.zero;
        }
    }

    private static void AddShadow(GameObject go, Color color, Vector2 distance)
    {
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void AddOutline(GameObject go, Color color)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private static TMP_FontAsset ResolveTmpFont()
    {
        string[] preferredPaths =
        {
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
            "Packages/com.unity.textmeshpro/Runtime/Resources/Fonts & Materials/LiberationSans SDF.asset"
        };

        foreach (string path in preferredPaths)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                return font;
            }
        }

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (font != null)
            {
                return font;
            }
        }

        EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        AssetDatabase.Refresh();

        guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (font != null)
            {
                return font;
            }
        }

        return null;
    }

    private static void SetObjectField(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetObjectArrayField(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetStringField(UnityEngine.Object target, string propertyName, string value)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetStringArrayField(UnityEngine.Object target, string propertyName, string[] values)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetBoolField(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetIntegerField(UnityEngine.Object target, string propertyName, int value)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetFloatField(UnityEngine.Object target, string propertyName, float value)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetVector2Field(UnityEngine.Object target, string propertyName, Vector2 value)
    {
        SerializedObject so = new(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            throw new MissingFieldException(target.GetType().Name, propertyName);
        }

        property.vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static T FindInChildren<T>(Transform root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child.GetComponent<T>();
            }
        }

        return null;
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new(EditorBuildSettings.scenes);
        scenes.RemoveAll(scene =>
            string.Equals(scene.path, MainMenuScenePath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scene.path, BecoScenePath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scene.path, "Assets/Scenes/Beco.unity", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scene.path, "Assets/Scenes/GameScene.unity", StringComparison.OrdinalIgnoreCase));

        InsertOrMoveScene(scenes, MainMenuScenePath, 0);
        InsertOrMoveScene(scenes, BecoScenePath, 1);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void InsertOrMoveScene(List<EditorBuildSettingsScene> scenes, string path, int targetIndex)
    {
        scenes.RemoveAll(scene => scene.path == path);
        targetIndex = Mathf.Clamp(targetIndex, 0, scenes.Count);
        scenes.Insert(targetIndex, new EditorBuildSettingsScene(path, true));
    }
}
#endif
