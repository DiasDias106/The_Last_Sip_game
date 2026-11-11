using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public sealed class AnimatedMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Visual References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private MenuAudioManager menuAudioManager;
    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;
    [Header("Animation")]
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float pressedScale = 0.98f;
    [SerializeField] private float lerpSpeed = 14f;
    [SerializeField] private Color normalTextColor = new Color32(227, 214, 184, 255);
    [SerializeField] private Color hoverTextColor = new Color32(255, 239, 198, 255);
    [SerializeField] private Color pressedTextColor = new Color32(214, 191, 143, 255);
    [SerializeField] private Color iconTint = new Color32(236, 222, 191, 255);
    [SerializeField] private Color glowVisibleColor = new Color32(255, 214, 140, 145);
    [SerializeField] private Color glowHiddenColor = new Color32(255, 214, 140, 0);
    public event System.Action Clicked;
    private Button button;
    private Vector3 initialScale;
    private Vector3 targetScale;
    private bool isHovered;
    private void Reset()
    {
        backgroundImage = GetComponent<Image>();
        button = GetComponent<Button>();
        contentRoot = transform as RectTransform;
    }
    private void Awake()
    {
        button = GetComponent<Button>();
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        if (contentRoot == null)
        {
            contentRoot = transform as RectTransform;
        }
        if (menuAudioManager == null)
        {
            menuAudioManager = FindAnyObjectByType<MenuAudioManager>();
        }
        initialScale = contentRoot != null ? contentRoot.localScale : Vector3.one;
        targetScale = initialScale;
        if (iconImage != null)
        {
            iconImage.color = iconTint;
        }
        button.onClick.AddListener(HandleClicked);
        ApplyNormalState(true);
    }
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }
    private void Update()
    {
        if (contentRoot == null)
        {
            return;
        }
        contentRoot.localScale = Vector3.Lerp(contentRoot.localScale, targetScale, Time.unscaledDeltaTime * lerpSpeed);
    }
    public void SetLabel(string value)
    {
        if (labelText != null)
        {
            labelText.text = value;
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        menuAudioManager?.PlayHover();
        ApplyHoverState();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyNormalState(false);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPressedState();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHovered)
        {
            ApplyHoverState();
        }
        else
        {
            ApplyNormalState(false);
        }
    }
    private void HandleClicked()
    {
        menuAudioManager?.PlayClick();
        Clicked?.Invoke();
    }
    private void ApplyNormalState(bool instant)
    {
        SetVisualState(normalSprite, normalTextColor, glowHiddenColor, 1f, instant);
    }
    private void ApplyHoverState()
    {
        SetVisualState(hoverSprite != null ? hoverSprite : normalSprite, hoverTextColor, glowVisibleColor, hoverScale, false);
    }
    private void ApplyPressedState()
    {
        SetVisualState(pressedSprite != null ? pressedSprite : normalSprite, pressedTextColor, glowVisibleColor, pressedScale, false);
    }
    private void SetVisualState(Sprite sprite, Color textColor, Color glowColor, float scaleMultiplier, bool instant)
    {
        if (backgroundImage != null && sprite != null)
        {
            backgroundImage.sprite = sprite;
        }
        if (labelText != null)
        {
            labelText.color = textColor;
        }
        if (glowImage != null)
        {
            glowImage.color = glowColor;
            glowImage.enabled = glowColor.a > 0f;
        }
        if (contentRoot == null)
        {
            return;
        }
        targetScale = initialScale * scaleMultiplier;
        if (instant)
        {
            contentRoot.localScale = targetScale;
        }
    }
}
