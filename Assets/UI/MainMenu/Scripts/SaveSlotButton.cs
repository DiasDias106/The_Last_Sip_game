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
    [SerializeField] private Color occupiedColor = new Color(0.24f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color emptyColor = new Color(0.16f, 0.11f, 0.11f, 0.9f);

    public int SlotIndex => slotIndex;
    public event Action<int> Clicked;
    private Texture2D loadedPreviewTexture;
    private Sprite loadedPreviewSprite;

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
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
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
        bool hasSave = data != null && data.hasSave;
        bool interactable = !continueMode || hasSave;

        if (titleText != null)
        {
            titleText.text = $"SLOT {slotIndex}";
        }

        if (statusText != null)
        {
            statusText.text = hasSave ? "Jogo guardado" : "Slot vazio";
        }

        if (detailsText != null)
        {
            if (hasSave)
            {
                string sceneName = string.IsNullOrWhiteSpace(data.currentScene) ? "Beco" : data.currentScene;
                string date = string.IsNullOrWhiteSpace(data.lastPlayedDate) ? "-" : data.lastPlayedDate;
                float playTime = SaveManager.GetLivePlayTimeForSlot(slotIndex);
                detailsText.text = $"Data: {date}\nScene: {sceneName}\nTempo: {FormatPlayTime(playTime)}";
            }
            else
            {
                detailsText.text = continueMode ? "Vazio" : "Novo Jogo";
            }
        }

        if (footerText != null)
        {
            if (hasSave)
            {
                footerText.text = continueMode ? "Continuar" : "Jogo guardado";
            }
            else
            {
                footerText.text = continueMode ? "Vazio" : "Novo Jogo";
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

        UpdatePreview(data, hasSave);
    }

    private void HandleClick()
    {
        Clicked?.Invoke(slotIndex);
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
