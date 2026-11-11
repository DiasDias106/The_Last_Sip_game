using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SaveSuccessPanel : MonoBehaviour
{
    [SerializeField] private MenuPanelController panel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private AnimatedMenuButton backButton;

    public bool IsVisible => panel != null && panel.IsVisible;

    private void OnEnable()
    {
        backButton?.SetLabel(UiText.GetMainMenuButtonLabel("back"));
        if (backButton != null)
        {
            backButton.Clicked += Hide;
        }
    }

    private void OnDisable()
    {
        if (backButton != null)
        {
            backButton.Clicked -= Hide;
        }
    }

    public void Show(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        panel?.Show();
    }

    public void Hide()
    {
        panel?.Hide();
    }

    public void HideImmediate()
    {
        panel?.HideImmediate();
    }
}

