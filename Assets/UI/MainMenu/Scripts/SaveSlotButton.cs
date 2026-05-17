using System;
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
    [SerializeField] private GameObject disabledOverlay;
    [SerializeField] private Color occupiedColor = new Color(0.24f, 0.08f, 0.08f, 0.92f);
    [SerializeField] private Color emptyColor = new Color(0.11f, 0.09f, 0.09f, 0.82f);

    public int SlotIndex => slotIndex;
    public event Action<int> Clicked;

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
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
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
                detailsText.text = $"Data: {date}\nScene: {sceneName}\nTempo: {FormatPlayTime(data.playTime)}";
            }
            else
            {
                detailsText.text = continueMode ? "Vazio" : "Novo Jogo";
            }
        }

        if (footerText != null)
        {
            footerText.text = hasSave ? (data.displayName ?? "Jogo Guardado") : "Novo Jogo";
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
    }

    private void HandleClick()
    {
        Clicked?.Invoke(slotIndex);
    }

    private static string FormatPlayTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        return $"{time.Minutes:00}:{time.Seconds:00}";
    }
}

