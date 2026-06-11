using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingScreenController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private MenuAudioManager menuAudioManager;

    [Header("Panel References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private MenuPanelController creditsPanel;
    [SerializeField] private SceneFadeController sceneFadeController;
    [SerializeField] private CanvasGroup loadingCanvasGroup;

    [Header("Loading Visuals")]
    [SerializeField] private RectTransform loadingTitleRoot;
    [SerializeField] private RectTransform loadingIconRoot;
    [SerializeField] private TMP_Text quoteText;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private float loadingDuration = 4.5f;
    [SerializeField] private float returnToMenuLoadingDuration = 6f;
    [SerializeField] private float shortFadeDuration = 0.22f;
    [SerializeField] private float loadingFadeDuration = 0.4f;
    [SerializeField] private float iconRotationSpeed = 160f;
    [SerializeField] private float textPulseSpeed = 1.9f;
    [SerializeField] private float quotePulseSpeed = 1.25f;
    [SerializeField] private float titleRevealDuration = 0.55f;
    [SerializeField] private float quoteRevealDuration = 0.8f;

    [Header("Loading Tips")]
    [SerializeField] private string[] loadingTips =
    {
        "Guarda o jogo regularmente — um progresso perdido é uma aventura perdida.",
        "Explora cada canto da masmorra. Os melhores tesouros escondem-se nos lugares mais sombrios.",
        "Os inimigos ficam mais perigosos à medida que avanças. Prepara-te antes de mergulhares fundo.",
        "A tua saúde não se regenera sozinha. Usa as poções com sabedoria.",
        "Observa os padrões de ataque dos inimigos antes de te lançares ao combate.",
        "O mercador pode ter exactamente o que precisas para sobreviver à próxima sala.",
        "Nem todos os confrontos precisam de ser ganhos pela força bruta.",
        "Um golpe bem cronometrado vale mais do que cem ataques precipitados.",
        "A escuridão da masmorra esconde tanto perigo como recompensa.",
        "Cada morte é uma lição. Os mais sábios aprendem com ela.",
        "Mantém o inventário organizado — nunca sabes quando vais precisar de um item urgente.",
        "Os chefes têm padrões previsíveis. Estuda os seus movimentos antes de atacar.",
        "A paciência é uma virtude no combate. Espera sempre pelo momento certo.",
        "Dinheiro bem gasto no mercador pode ser a diferença entre a vida e a morte.",
        "Atenção aos detalhes do ambiente — podem indicar armadilhas à frente.",
        "Recolhe tudo o que encontrares. Nunca sabes o que pode ser útil mais tarde.",
        "A beco guarda segredos que apenas os mais atentos conseguem descobrir.",
        "O teu personagem tem mais força do que imaginas — aprende a dominá-lo.",
        "Alguns inimigos são melhor evitados do que confrontados directamente.",
        "A masmorra tem memória — os teus actos têm consequências.",
    };

    public event Action SequenceFinished;

    private Quaternion iconInitialRotation;
    private Vector3 titleInitialScale;
    private Color quoteBaseColor;
    private Color loadingBaseColor;
    private Coroutine activeSequence;
    private bool loadingAudioMuted;

    private void Awake()
    {
        if (menuAudioManager == null)
        {
            menuAudioManager = GetComponent<MenuAudioManager>();
            if (menuAudioManager == null)
            {
                menuAudioManager = FindFirstObjectByType<MenuAudioManager>(FindObjectsInactive.Include);
            }
        }

        if (loadingIconRoot != null)
        {
            iconInitialRotation = loadingIconRoot.localRotation;
        }

        if (loadingTitleRoot != null)
        {
            titleInitialScale = loadingTitleRoot.localScale;
        }

        if (quoteText != null)
        {
            quoteBaseColor = quoteText.color;
        }

        if (loadingText != null)
        {
            loadingBaseColor = loadingText.color;
        }

        HideLoading();
    }

    private void Update()
    {
        if (loadingPanel == null || !loadingPanel.activeSelf)
        {
            return;
        }

        float delta = Time.unscaledDeltaTime;

        if (loadingIconRoot != null)
        {
            loadingIconRoot.Rotate(0f, 0f, iconRotationSpeed * delta);
        }

        if (loadingText != null)
        {
            float pulse = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * textPulseSpeed * Mathf.PI * 2f));
            Color color = loadingBaseColor;
            color.a = pulse;
            loadingText.color = color;
        }

        if (quoteText != null)
        {
            float pulse = 0.65f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * quotePulseSpeed * Mathf.PI * 2f));
            Color color = quoteBaseColor;
            color.a = pulse;
            quoteText.color = color;
        }
    }

    public void ShowLoadingThenOpenCredits()
    {
        menuAudioManager?.EnsureAmbientMusicIsPlaying();
        StartSequence(ShowLoadingThenOpenCreditsRoutine());
    }

    public void ShowLoadingThenLoadScene(string sceneName)
    {
        StartSequence(ShowLoadingThenLoadSceneRoutine(sceneName));
    }

    public void HideLoading()
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.interactable = false;
            loadingCanvasGroup.blocksRaycasts = false;
        }

        if (loadingIconRoot != null)
        {
            loadingIconRoot.localRotation = iconInitialRotation;
        }

        if (loadingTitleRoot != null)
        {
            loadingTitleRoot.localScale = titleInitialScale;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        RestoreAudioAfterLoading();
    }

    private void StartSequence(IEnumerator routine)
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
        }

        activeSequence = StartCoroutine(routine);
    }

    private IEnumerator ShowLoadingThenOpenCreditsRoutine()
    {
        yield return PlayLoadingIntro();
        yield return WaitForUnscaledSeconds(loadingDuration);
        yield return FadeToBlack();

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (creditsPanel != null)
        {
            creditsPanel.Show();
        }

        RestoreAudioAfterLoading();
        yield return FadeFromBlack();
        FinishSequence();
    }

    private IEnumerator ShowLoadingThenLoadSceneRoutine(string sceneName)
    {
        // Garantir que o overlay preto está escondido antes de mostrar o loading
        RuntimeSceneTransitionOverlay.HideOverlay();
        yield return PlayLoadingIntro();

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' is not available in Build Settings.");
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            RestoreAudioAfterLoading();
            yield return FadeFromBlack();
            FinishSequence();
            yield break;
        }

        // Carregar a cena em background enquanto o loading está visível
        var asyncOp = SceneManager.LoadSceneAsync(sceneName);
        asyncOp.allowSceneActivation = false;

        // Esperar o tempo mínimo de loading E que a cena esteja pronta
        float elapsed = 0f;
        while (elapsed < loadingDuration || asyncOp.progress < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        asyncOp.allowSceneActivation = true;
    }

    public void ShowReturnToMenuLoading(System.Action onMenuReady)
    {
        StartSequence(ShowReturnToMenuLoadingRoutine(onMenuReady));
    }

    private IEnumerator ShowReturnToMenuLoadingRoutine(System.Action onMenuReady)
    {
        SetRandomTip();
        MuteAudioForLoading();
        RuntimeSceneTransitionOverlay.HideOverlay();

        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.interactable = true;
            loadingCanvasGroup.blocksRaycasts = true;
        }
        if (loadingIconRoot != null) loadingIconRoot.localRotation = iconInitialRotation;
        if (loadingTitleRoot != null) loadingTitleRoot.localScale = titleInitialScale * 0.95f;
        if (quoteText != null) { Color c = quoteBaseColor; c.a = 0f; quoteText.color = c; }
        if (loadingText != null) { Color c = loadingBaseColor; c.a = 0f; loadingText.color = c; }

        yield return FadeCanvasGroup(loadingCanvasGroup, 0f, 1f, loadingFadeDuration);
        yield return StartCoroutine(AnimateTitleAndQuoteIntro());
        yield return FadeFromBlack();

        yield return WaitForUnscaledSeconds(returnToMenuLoadingDuration);

        yield return FadeToBlack();
        if (loadingPanel != null) loadingPanel.SetActive(false);

        onMenuReady?.Invoke();

        yield return FadeFromBlack();
        RestoreAudioAfterLoading();
        FinishSequence();
    }

    private void SetRandomTip()
    {
        if (quoteText == null || loadingTips == null || loadingTips.Length == 0) return;
        string tip = loadingTips[UnityEngine.Random.Range(0, loadingTips.Length)];
        quoteText.text = UiText.LocalizeKnownText(tip);
    }

    private IEnumerator PlayLoadingIntro()
    {
        SetRandomTip();
        MuteAudioForLoading();

        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(1f, shortFadeDuration);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.interactable = true;
            loadingCanvasGroup.blocksRaycasts = true;
        }

        if (loadingIconRoot != null)
        {
            loadingIconRoot.localRotation = iconInitialRotation;
        }

        if (loadingTitleRoot != null)
        {
            loadingTitleRoot.localScale = titleInitialScale * 0.95f;
        }

        if (quoteText != null)
        {
            Color quoteColor = quoteBaseColor;
            quoteColor.a = 0f;
            quoteText.color = quoteColor;
        }

        if (loadingText != null)
        {
            Color loadingColor = loadingBaseColor;
            loadingColor.a = 0f;
            loadingText.color = loadingColor;
        }

        yield return FadeCanvasGroup(loadingCanvasGroup, 0f, 1f, loadingFadeDuration);
        yield return StartCoroutine(AnimateTitleAndQuoteIntro());
        yield return FadeFromBlack();
    }

    private IEnumerator AnimateTitleAndQuoteIntro()
    {
        float elapsed = 0f;
        while (elapsed < Mathf.Max(titleRevealDuration, quoteRevealDuration))
        {
            elapsed += Time.unscaledDeltaTime;

            if (loadingTitleRoot != null)
            {
                float t = Mathf.Clamp01(elapsed / titleRevealDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                loadingTitleRoot.localScale = Vector3.Lerp(titleInitialScale * 0.95f, titleInitialScale, eased);
            }

            if (quoteText != null)
            {
                float t = Mathf.Clamp01(elapsed / quoteRevealDuration);
                Color quoteColor = quoteBaseColor;
                quoteColor.a = Mathf.Lerp(0f, quoteBaseColor.a, t);
                quoteText.color = quoteColor;
            }

            if (loadingText != null)
            {
                float t = Mathf.Clamp01(elapsed / 0.45f);
                Color loadingColor = loadingBaseColor;
                loadingColor.a = Mathf.Lerp(0f, loadingBaseColor.a, t);
                loadingText.color = loadingColor;
            }

            yield return null;
        }

        if (loadingTitleRoot != null)
        {
            loadingTitleRoot.localScale = titleInitialScale;
        }

        if (quoteText != null)
        {
            quoteText.color = quoteBaseColor;
        }

        if (loadingText != null)
        {
            loadingText.color = loadingBaseColor;
        }
    }

    private IEnumerator FadeToBlack()
    {
        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(1f, shortFadeDuration);
        }
    }

    private IEnumerator FadeFromBlack()
    {
        if (sceneFadeController != null)
        {
            yield return sceneFadeController.FadeToAlpha(0f, shortFadeDuration);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup target, float from, float to, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        float elapsed = 0f;
        target.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            target.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        target.alpha = to;
    }

    private IEnumerator WaitForUnscaledSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void FinishSequence()
    {
        RestoreAudioAfterLoading();
        activeSequence = null;
        SequenceFinished?.Invoke();
    }

    private void MuteAudioForLoading()
    {
        if (menuAudioManager == null)
        {
            return;
        }

        menuAudioManager.SetRuntimeMusicMultiplier(0f);
        loadingAudioMuted = true;
    }

    private void RestoreAudioAfterLoading()
    {
        if (!loadingAudioMuted || menuAudioManager == null)
        {
            return;
        }

        menuAudioManager.SetRuntimeMusicMultiplier(1f);
        loadingAudioMuted = false;
    }
}
