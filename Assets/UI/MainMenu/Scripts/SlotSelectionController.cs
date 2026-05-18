using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlotSelectionController : MonoBehaviour
{
    public enum SlotSelectionMode
    {
        NewGame,
        Continue
    }

    [SerializeField] private MenuPanelController panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private SaveSlotButton[] slotButtons;
    [SerializeField] private AnimatedMenuButton backButton;

    public event Action<int, SlotSelectionMode, bool> SlotChosen;
    public event Action BackRequested;

    public bool IsVisible => panel != null && panel.IsVisible;
    public SlotSelectionMode CurrentMode { get; private set; } = SlotSelectionMode.NewGame;

    private void OnEnable()
    {
        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] != null)
                {
                    slotButtons[i].Clicked += HandleSlotClicked;
                }
            }
        }

        if (backButton != null)
        {
            backButton.Clicked += HandleBackClicked;
        }
    }

    private void OnDisable()
    {
        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] != null)
                {
                    slotButtons[i].Clicked -= HandleSlotClicked;
                }
            }
        }

        if (backButton != null)
        {
            backButton.Clicked -= HandleBackClicked;
        }
    }

    public void Open(SlotSelectionMode mode)
    {
        CurrentMode = mode;
        if (titleText != null)
        {
            titleText.text = mode == SlotSelectionMode.NewGame ? "NOVO JOGO" : "CONTINUAR JOGO";
        }

        Refresh();
        panel?.Show();
    }

    public void HideImmediate()
    {
        panel?.HideImmediate();
    }

    public void Refresh()
    {
        if (slotButtons == null)
        {
            return;
        }

        bool continueMode = CurrentMode == SlotSelectionMode.Continue;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            SaveSlotButton slotButton = slotButtons[i];
            if (slotButton == null)
            {
                continue;
            }

            int slotIndex = i + 1;
            slotButton.SetSlotIndex(slotIndex);
            SaveSlotData data = SaveManager.LoadSlot(slotIndex);
            slotButton.Configure(data, continueMode);
        }
    }

    private void HandleSlotClicked(int slotIndex)
    {
        SaveSlotData data = SaveManager.LoadSlot(slotIndex);
        SlotChosen?.Invoke(slotIndex, CurrentMode, data.hasSave);
    }

    private void HandleBackClicked()
    {
        BackRequested?.Invoke();
    }
}
