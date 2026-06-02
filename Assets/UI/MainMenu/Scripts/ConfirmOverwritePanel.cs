using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConfirmOverwritePanel : MonoBehaviour
{
    [SerializeField] private MenuPanelController panel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private AnimatedMenuButton confirmButton;
    [SerializeField] private AnimatedMenuButton cancelButton;

    private Action confirmAction;
    private Action cancelAction;

    public bool IsVisible => panel != null && panel.IsVisible;

    private void OnEnable()
    {
        confirmButton?.SetLabel(UiText.GetMainMenuButtonLabel("confirm"));
        cancelButton?.SetLabel(UiText.GetMainMenuButtonLabel("cancel"));

        if (confirmButton != null)
        {
            confirmButton.Clicked += HandleConfirm;
        }

        if (cancelButton != null)
        {
            cancelButton.Clicked += HandleCancel;
        }
    }

    private void OnDisable()
    {
        if (confirmButton != null)
        {
            confirmButton.Clicked -= HandleConfirm;
        }

        if (cancelButton != null)
        {
            cancelButton.Clicked -= HandleCancel;
        }
    }

    public void Show(string message, Action onConfirm, Action onCancel)
    {
        confirmAction = onConfirm;
        cancelAction = onCancel;

        if (messageText != null)
        {
            messageText.text = message;
        }

        panel?.Show();
    }

    public void HideImmediate()
    {
        confirmAction = null;
        cancelAction = null;
        panel?.HideImmediate();
    }

    private void HandleConfirm()
    {
        Action action = confirmAction;
        HideImmediate();
        action?.Invoke();
    }

    private void HandleCancel()
    {
        Action action = cancelAction;
        HideImmediate();
        action?.Invoke();
    }
}

