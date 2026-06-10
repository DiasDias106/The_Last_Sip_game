using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BossHUDController : MonoBehaviour
{
    public static BossHUDController Instance { get; private set; }

    private CanvasGroup _group;
    private RectTransform _fillRect;
    private RectTransform _armorFillRect;
    private TextMeshProUGUI _nameLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("[BossHUD]");
            go.AddComponent<BossHUDController>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildHUD();
        Hide();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene")
        {
            Hide();
        }
    }

    public void Show()
    {
        if (_group == null) return;
        _group.alpha = 1f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }

    public void Hide()
    {
        if (_group == null) return;
        _group.alpha = 0f;
        _group.interactable = false;
        _group.blocksRaycasts = false;
    }

    public void Refresh(float current, float max)
    {
        if (_fillRect == null) return;
        float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        _fillRect.anchorMax = new Vector2(pct, 1f);
        if (_armorFillRect != null)
        {
            _armorFillRect.anchorMax = new Vector2(0f, 1f);
        }
    }

    public void RefreshWithArmor(float currentHealth, float maxHealth, float currentArmor, float maxArmor)
    {
        if (_fillRect == null) return;

        float hpPct = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        _fillRect.anchorMax = new Vector2(hpPct, 1f);

        if (_armorFillRect == null) return;
        float armorPct = maxArmor > 0f ? Mathf.Clamp01(currentArmor / maxArmor) : 0f;
        _armorFillRect.anchorMax = new Vector2(armorPct, 1f);
    }

    private void BuildHUD()
    {
        var canvasGo = new GameObject("BossHUDCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        _group = canvasGo.AddComponent<CanvasGroup>();
        _group.alpha = 0f;

        // Nome do boss por cima da barra
        var nameGO = new GameObject("BossNameLabel");
        nameGO.transform.SetParent(canvasGo.transform, false);
        _nameLabel = nameGO.AddComponent<TextMeshProUGUI>();
        _nameLabel.text = "";
        _nameLabel.fontSize = 18f;
        _nameLabel.fontStyle = TMPro.FontStyles.Bold;
        _nameLabel.alignment = TMPro.TextAlignmentOptions.Center;
        _nameLabel.color = new Color(1f, 0.92f, 0.72f, 1f);
        var nameLabelRect = nameGO.GetComponent<RectTransform>();
        nameLabelRect.anchorMin = new Vector2(0.5f, 1f);
        nameLabelRect.anchorMax = new Vector2(0.5f, 1f);
        nameLabelRect.pivot = new Vector2(0.5f, 1f);
        nameLabelRect.sizeDelta = new Vector2(700f, 24f);
        nameLabelRect.anchoredPosition = new Vector2(0f, -8f);

        // Barra de vida
        var track = new GameObject("BossHpTrack");
        track.transform.SetParent(canvasGo.transform, false);
        var trackImage = track.AddComponent<Image>();
        trackImage.color = new Color(0.08f, 0.02f, 0.02f, 0.9f);

        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 1f);
        trackRect.anchorMax = new Vector2(0.5f, 1f);
        trackRect.pivot = new Vector2(0.5f, 1f);
        trackRect.sizeDelta = new Vector2(700f, 30f);
        trackRect.anchoredPosition = new Vector2(0f, -36f);

        var outline = new GameObject("BossHpOutline");
        outline.transform.SetParent(track.transform, false);
        var outlineImg = outline.AddComponent<Image>();
        outlineImg.color = new Color(0f, 0f, 0f, 0.9f);
        var outlineRect = outline.GetComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.sizeDelta = new Vector2(6f, 6f);
        outline.transform.SetAsFirstSibling();

        var fill = new GameObject("BossHpFill");
        fill.transform.SetParent(track.transform, false);
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.88f, 0.08f, 0.08f, 1f);
        _fillRect = fill.GetComponent<RectTransform>();
        _fillRect.anchorMin = new Vector2(0f, 0f);
        _fillRect.anchorMax = new Vector2(1f, 1f);
        _fillRect.offsetMin = new Vector2(4f, 4f);
        _fillRect.offsetMax = new Vector2(-4f, -4f);

        var armorFill = new GameObject("BossArmorFill");
        armorFill.transform.SetParent(track.transform, false);
        var armorFillImage = armorFill.AddComponent<Image>();
        armorFillImage.color = new Color(0.95f, 0.52f, 0.12f, 0.95f);
        _armorFillRect = armorFill.GetComponent<RectTransform>();
        _armorFillRect.anchorMin = new Vector2(0f, 0f);
        _armorFillRect.anchorMax = new Vector2(1f, 1f);
        _armorFillRect.offsetMin = new Vector2(4f, 4f);
        _armorFillRect.offsetMax = new Vector2(-4f, -4f);
        armorFill.transform.SetAsLastSibling();
    }

    public void SetName(string bossName)
    {
        if (_nameLabel != null) _nameLabel.text = UiText.LocalizeKnownText(bossName);
    }
}
