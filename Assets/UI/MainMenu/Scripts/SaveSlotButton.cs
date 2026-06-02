using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SaveSlotButton : MonoBehaviour
{
    [SerializeField] private int slotIndex = 1;
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private TMP_Text footerText;
    [SerializeField] private Image previewImage;
    [SerializeField] private GameObject disabledOverlay;
    [SerializeField] private Image disabledOverlayImage;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Text deleteButtonText;
    [SerializeField] private Color occupiedColor = new Color(0.24f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color emptyColor = new Color(0.16f, 0.11f, 0.11f, 0.9f);

    public int SlotIndex => slotIndex;
    public event Action<int> Clicked;
    public event Action<int> DeleteClicked;
    private Texture2D loadedPreviewTexture;
    private Sprite loadedPreviewSprite;
    private bool showDeleteButton;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        if (disabledOverlayImage == null && disabledOverlay != null)
        {
            disabledOverlayImage = disabledOverlay.GetComponent<Image>();
        }

        EnsureDeleteButton();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(HandleDeleteClick);
        }

        ReleasePreviewAssets();
    }

    public void SetSlotIndex(int value)
    {
        slotIndex = Mathf.Clamp(value, 1, 3);
        if (titleText != null)
        {
            titleText.text = $"SLOT {slotIndex}";
        }
    }

    public void Configure(SaveSlotData data, bool continueMode)
    {
        EnsureDeleteButton();

        bool hasSave = data != null && data.hasSave;
        bool interactable = !continueMode || hasSave;
        showDeleteButton = !continueMode && hasSave;

        if (titleText != null)
        {
            titleText.text = $"SLOT {slotIndex}";
        }

        if (statusText != null)
        {
            statusText.text = hasSave ? UiText.LocalizeKnownText("Jogo guardado") : UiText.LocalizeKnownText("Slot vazio");
        }

        if (detailsText != null)
        {
            if (hasSave)
            {
                string sceneName = string.IsNullOrWhiteSpace(data.currentScene) ? "Beco" : data.currentScene;
                string date = string.IsNullOrWhiteSpace(data.lastPlayedDate) ? "-" : data.lastPlayedDate;
                float playTime = SaveManager.GetLivePlayTimeForSlot(slotIndex);
                detailsText.text = $"{UiText.LocalizeKnownText("Data:")} {date}\n{UiText.LocalizeKnownText("Scene:")} {sceneName}\n{UiText.LocalizeKnownText("Tempo:")} {FormatPlayTime(playTime)}";
            }
            else
            {
                detailsText.text = continueMode ? UiText.LocalizeKnownText("Vazio") : UiText.LocalizeKnownText("Novo Jogo");
            }
        }

        if (footerText != null)
        {
            bool showFooter = continueMode || !hasSave;
            footerText.gameObject.SetActive(showFooter);

            if (showFooter)
            {
                if (hasSave)
                {
                    footerText.text = UiText.LocalizeKnownText("CONTINUAR");
                }
                else
                {
                    footerText.text = continueMode ? UiText.LocalizeKnownText("Vazio") : UiText.LocalizeKnownText("Novo Jogo");
                }
            }
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = hasSave ? occupiedColor : emptyColor;
        }

        if (button != null)
        {
            button.interactable = interactable;
        }

        if (disabledOverlay != null)
        {
            disabledOverlay.SetActive(!interactable);
        }

        if (disabledOverlayImage != null)
        {
            disabledOverlayImage.color = interactable
                ? new Color(0f, 0f, 0f, 0f)
                : new Color(0f, 0f, 0f, 0.22f);
        }

        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(showDeleteButton);
        }

        UpdatePreview(data, hasSave);
    }

    private void HandleClick()
    {
        Clicked?.Invoke(slotIndex);
    }

    private void HandleDeleteClick()
    {
        DeleteClicked?.Invoke(slotIndex);
    }

    private void EnsureDeleteButton()
    {
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(HandleDeleteClick);
            deleteButton.onClick.AddListener(HandleDeleteClick);
            RectTransform existingRect = deleteButton.transform as RectTransform;
            if (existingRect != null)
            {
                existingRect.anchorMin = new Vector2(0.5f, 0f);
                existingRect.anchorMax = new Vector2(0.5f, 0f);
                existingRect.pivot = new Vector2(0.5f, 0f);
                existingRect.anchoredPosition = new Vector2(0f, -56f);
                existingRect.sizeDelta = new Vector2(180f, 52f);
                existingRect.localScale = Vector3.one;
            }
            return;
        }

        Button templateButton = FindTemplateBackButton();
        if (templateButton != null)
        {
            GameObject deleteRoot = Instantiate(templateButton.gameObject, transform, false);
            deleteRoot.name = "DeleteButton";

            RectTransform deleteRect = deleteRoot.transform as RectTransform;
            if (deleteRect != null)
            {
                deleteRect.anchorMin = new Vector2(0.5f, 0f);
                deleteRect.anchorMax = new Vector2(0.5f, 0f);
                deleteRect.pivot = new Vector2(0.5f, 0f);
                deleteRect.anchoredPosition = new Vector2(0f, -56f);
                deleteRect.sizeDelta = new Vector2(180f, 52f);
                deleteRect.localScale = Vector3.one;
            }

            deleteButton = deleteRoot.GetComponent<Button>();
            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(HandleDeleteClick);
            }

            deleteButtonText = deleteRoot.GetComponentInChildren<TMP_Text>(true);
            if (deleteButtonText != null)
            {
                deleteButtonText.text = UiText.GetMainMenuButtonLabel("delete");
            }

            deleteRoot.transform.SetAsLastSibling();
            deleteRoot.SetActive(false);
            return;
        }

        CreateFallbackDeleteButton();
    }

    private Button FindTemplateBackButton()
    {
        Transform current = transform;
        while (current != null)
        {
            Transform backButton = current.Find("BackButton");
            if (backButton != null)
            {
                Button buttonComponent = backButton.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    return buttonComponent;
                }
            }

            current = current.parent;
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == "BackButton")
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void CreateFallbackDeleteButton()
    {
        GameObject deleteRoot = new GameObject("DeleteButton");
        deleteRoot.transform.SetParent(transform, false);
        RectTransform deleteRect = deleteRoot.AddComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(0.5f, 0f);
        deleteRect.anchorMax = new Vector2(0.5f, 0f);
        deleteRect.pivot = new Vector2(0.5f, 0f);
        deleteRect.anchoredPosition = new Vector2(0f, -56f);
        deleteRect.sizeDelta = new Vector2(180f, 52f);

        Image deleteBg = deleteRoot.AddComponent<Image>();
        deleteBg.color = new Color(0.34f, 0.12f, 0.08f, 0.98f);

        Outline outline = deleteRoot.AddComponent<Outline>();
        outline.effectColor = new Color(0.52f, 0.40f, 0.10f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);

        deleteButton = deleteRoot.AddComponent<Button>();
        deleteButton.onClick.AddListener(HandleDeleteClick);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(deleteRoot.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        deleteButtonText = labelGo.AddComponent<TextMeshProUGUI>();
        deleteButtonText.text = UiText.GetMainMenuButtonLabel("delete");
        deleteButtonText.fontSize = 18f;
        deleteButtonText.alignment = TextAlignmentOptions.Center;
        deleteButtonText.color = new Color32(233, 217, 190, 255);
        deleteButtonText.enableAutoSizing = true;
        deleteButtonText.fontSizeMin = 12f;
        deleteButtonText.fontSizeMax = 18f;

        deleteRoot.transform.SetAsLastSibling();
        deleteRoot.SetActive(false);
    }

    private static string FormatPlayTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}";
    }

    private void UpdatePreview(SaveSlotData data, bool hasSave)
    {
        if (previewImage == null)
        {
            return;
        }

        if (!hasSave || data == null || string.IsNullOrWhiteSpace(data.screenshotPath) || !File.Exists(data.screenshotPath))
        {
            ReleasePreviewAssets();
            previewImage.enabled = false;
            previewImage.sprite = null;
            previewImage.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(data.screenshotPath);
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            ReleasePreviewAssets();
            loadedPreviewTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            loadedPreviewTexture.LoadImage(bytes, false);
            loadedPreviewTexture.wrapMode = TextureWrapMode.Clamp;
            loadedPreviewSprite = Sprite.Create(
                loadedPreviewTexture,
                new Rect(0f, 0f, loadedPreviewTexture.width, loadedPreviewTexture.height),
                new Vector2(0.5f, 0.5f));
            previewImage.enabled = true;
            previewImage.sprite = loadedPreviewSprite;
            previewImage.preserveAspect = false;
            previewImage.color = Color.white;
        }
        catch
        {
            ReleasePreviewAssets();
            previewImage.enabled = false;
            previewImage.sprite = null;
            previewImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void ReleasePreviewAssets()
    {
        if (loadedPreviewSprite != null)
        {
            Destroy(loadedPreviewSprite);
            loadedPreviewSprite = null;
        }

        if (loadedPreviewTexture != null)
        {
            Destroy(loadedPreviewTexture);
            loadedPreviewTexture = null;
        }
    }
}
