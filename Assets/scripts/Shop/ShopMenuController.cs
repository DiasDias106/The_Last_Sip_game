using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ShopMenuController : MonoBehaviour
{
    private enum ShopItemKind
    {
        None,
        RedPotion,
        BluePotion
    }

    public static ShopMenuController Instance { get; private set; }

    public bool IsVisible => _shopPanel != null && _shopPanel.activeSelf;

    private GameObject _shopPanel;
    private Text _statusText;
    private bool _wasCursorVisible;
    private CursorLockMode _previousLockMode;

    [Header("Dialogos da Loja")]
    public DialogueData dialogoInventarioCheio;
    public DialogueData dialogoComprar;
    public DialogueData dialogoSairSemComprar;

    private bool _comprouAlgo;

    [System.Serializable]
    private struct DrinkData
    {
        public string Name;
        public string Description;
        public int Price;
        public string IconPath;
        public bool Available;
        public ShopItemKind ItemKind;
    }

    private sealed class ShopRowRefs
    {
        public DrinkData Drink;
        public Text PriceText;
        public Image CoinImage;
    }

    private readonly List<DrinkData> _drinks = new List<DrinkData>
    {
        new DrinkData { Name = "Vinho da Vida", Description = "Vinho doce guardado em garrafa lacrada a cera. Restaura um pouco de Vida.", Price = 15, IconPath = "ShopUI/icon_vinho_vida", Available = true, ItemKind = ShopItemKind.RedPotion },
        new DrinkData { Name = "Bencao de Baco", Description = "A lendaria bebida divina de tom rubi. Restaura a Saude completamente.", Price = 20, IconPath = "ShopUI/icon_bencao_baco", Available = true, ItemKind = ShopItemKind.BluePotion },
        new DrinkData { Name = "Pao Rustico", Description = "Pao fresco e simples, feito com farinha de trigo. Restaura uma pequena quantidade de Vida.", Price = 2, IconPath = "ShopUI/icon_bread", Available = false, ItemKind = ShopItemKind.None },
        new DrinkData { Name = "Ensopado de Carne", Description = "Ensopado quente e nutritivo, preparado com carne e legumes da estacao. Restaura uma boa quantidade de Vida.", Price = 8, IconPath = "ShopUI/icon_stew", Available = false, ItemKind = ShopItemKind.None },
        new DrinkData { Name = "Perna de Porco Assada", Description = "Carne suculenta e bem temperada, assada lentamente. Restaura uma grande quantidade de Vida.", Price = 12, IconPath = "ShopUI/icon_meat", Available = false, ItemKind = ShopItemKind.None },
        new DrinkData { Name = "Queijo Curado", Description = "Queijo curado, forte e saboroso. Restaura um pouco de Vida.", Price = 4, IconPath = "ShopUI/icon_cheese", Available = false, ItemKind = ShopItemKind.None }
    };
    private readonly List<ShopRowRefs> _rows = new List<ShopRowRefs>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void BuildUI()
    {
        _rows.Clear();

        Font fontRegular = Resources.Load<Font>("Fonts/Cinzel-Regular");
        Font fontBold = Resources.Load<Font>("Fonts/Cinzel-Bold");
        if (fontRegular == null) fontRegular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fontBold == null) fontBold = fontRegular;

        GameObject canvasGo = new GameObject("ShopCanvas");
        canvasGo.transform.SetParent(transform);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject overlayGo = new GameObject("OverlayPanel");
        overlayGo.transform.SetParent(canvasGo.transform, false);
        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.02f, 0.02f, 0.85f);
        RectTransform overlayRect = overlayGo.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        _shopPanel = overlayGo;

        GameObject bgGo = new GameObject("DarkBgPanel");
        bgGo.transform.SetParent(_shopPanel.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        Texture2D bgTex = Resources.Load<Texture2D>("ShopUI/panel_dark_frame");
        if (bgTex != null)
        {
            bgImg.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            bgImg.color = Color.white;
            bgImg.type = Image.Type.Simple;
        }
        else
        {
            bgImg.color = new Color(0.1f, 0.08f, 0.06f, 1f);
        }

        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(1400, 950);

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(bgGo.transform, false);
        Text titleText = titleGo.AddComponent<Text>();
        titleText.text = UiText.LocalizeKnownText("Taverna do Baco");
        titleText.font = fontBold;
        titleText.fontSize = 58;
        titleText.fontStyle = FontStyle.Normal;
        titleText.color = new Color(0.8f, 0.7f, 0.5f);
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.offsetMin = new Vector2(0, -90);
        titleRect.offsetMax = new Vector2(0, -20);

        GameObject subGo = new GameObject("Subtitle");
        subGo.transform.SetParent(bgGo.transform, false);
        Text subText = subGo.AddComponent<Text>();
        subText.text = UiText.LocalizeKnownText("Comida, Bebida e Boas Historias");
        subText.font = fontRegular;
        subText.fontSize = 24;
        subText.fontStyle = FontStyle.Italic;
        subText.color = new Color(0.6f, 0.55f, 0.45f);
        subText.alignment = TextAnchor.MiddleCenter;
        RectTransform subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0, 1);
        subRect.anchorMax = new Vector2(1, 1);
        subRect.pivot = new Vector2(0.5f, 1);
        subRect.offsetMin = new Vector2(0, -130);
        subRect.offsetMax = new Vector2(0, -90);

        GameObject headerGo = new GameObject("HeaderRow");
        headerGo.transform.SetParent(bgGo.transform, false);
        RectTransform headerRect = headerGo.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.offsetMin = new Vector2(60, -180);
        headerRect.offsetMax = new Vector2(-60, -140);

        CreateText(headerGo.transform, "HeaderItem", UiText.LocalizeKnownText("Item"), fontRegular, 28, new Color(0.7f, 0.65f, 0.5f), TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0.2f, 1));
        CreateText(headerGo.transform, "HeaderDesc", UiText.LocalizeKnownText("Descricao"), fontRegular, 28, new Color(0.7f, 0.65f, 0.5f), TextAnchor.MiddleCenter, new Vector2(0.2f, 0), new Vector2(0.8f, 1));
        CreateText(headerGo.transform, "HeaderPrice", UiText.LocalizeKnownText("Preco"), fontRegular, 28, new Color(0.7f, 0.65f, 0.5f), TextAnchor.MiddleRight, new Vector2(0.8f, 0), new Vector2(1, 1), new Vector2(-60, 0));

        CreateLine(bgGo.transform, new Vector2(60, -190), new Vector2(-60, -192));

        GameObject listGo = new GameObject("List");
        listGo.transform.SetParent(bgGo.transform, false);
        RectTransform listRect = listGo.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0, 0);
        listRect.anchorMax = new Vector2(1, 1);
        listRect.offsetMin = new Vector2(60, 100);
        listRect.offsetMax = new Vector2(-60, -195);

        VerticalLayoutGroup vLayout = listGo.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 0;
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childForceExpandWidth = true;

        foreach (DrinkData drink in _drinks)
        {
            CreateDrinkItem(drink, listGo.transform, fontRegular);
        }

        GameObject closeBtnGo = new GameObject("CloseButton");
        closeBtnGo.transform.SetParent(bgGo.transform, false);
        Image closeImg = closeBtnGo.AddComponent<Image>();
        closeImg.color = new Color(0f, 0f, 0f, 0f);
        Button closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(Hide);
        RectTransform closeRect = closeBtnGo.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.sizeDelta = new Vector2(300, 50);
        closeRect.anchoredPosition = new Vector2(0, 30);

        GameObject closeTextGo = new GameObject("Text");
        closeTextGo.transform.SetParent(closeBtnGo.transform, false);
        Text closeText = closeTextGo.AddComponent<Text>();
        closeText.text = UiText.LocalizeKnownText("[ESC] Voltar");
        closeText.font = fontRegular;
        closeText.fontSize = 24;
        closeText.fontStyle = FontStyle.Normal;
        closeText.color = new Color(0.6f, 0.55f, 0.45f);
        closeText.alignment = TextAnchor.MiddleCenter;
        RectTransform closeTextRect = closeTextGo.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        GameObject statusGo = new GameObject("StatusText");
        statusGo.transform.SetParent(bgGo.transform, false);
        _statusText = statusGo.AddComponent<Text>();
        _statusText.font = fontRegular;
        _statusText.fontSize = 20;
        _statusText.fontStyle = FontStyle.Italic;
        _statusText.color = new Color(0.9f, 0.8f, 0.6f);
        _statusText.alignment = TextAnchor.MiddleCenter;
        _statusText.text = string.Empty;
        RectTransform statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.sizeDelta = new Vector2(800, 40);
        statusRect.anchoredPosition = new Vector2(0, 82);

        _shopPanel.SetActive(false);
    }

    private void CreateText(Transform parent, string goName, string text, Font font, int size, Color color, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Vector2? offsetMax = null)
    {
        GameObject txtGo = new GameObject(goName);
        txtGo.transform.SetParent(parent, false);
        Text textComponent = txtGo.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = font;
        textComponent.fontSize = size;
        textComponent.color = color;
        textComponent.alignment = align;
        RectTransform rt = txtGo.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = offsetMax ?? Vector2.zero;
    }

    private void CreateLine(Transform parent, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject lineGo = new GameObject("Line");
        lineGo.transform.SetParent(parent, false);
        Image lineImg = lineGo.AddComponent<Image>();
        lineImg.color = new Color(0.2f, 0.15f, 0.1f, 0.8f);
        RectTransform lineRect = lineGo.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 1);
        lineRect.anchorMax = new Vector2(1, 1);
        lineRect.offsetMin = offsetMin;
        lineRect.offsetMax = offsetMax;
    }

    private void CreateDrinkItem(DrinkData drink, Transform parent, Font fontRegular)
    {
        GameObject itemGo = new GameObject("Row_" + drink.Name);
        itemGo.transform.SetParent(parent, false);
        RectTransform itemRect = itemGo.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0, 110);

        Image bg = itemGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        GameObject lineGo = new GameObject("Line");
        lineGo.transform.SetParent(itemGo.transform, false);
        Image lineImg = lineGo.AddComponent<Image>();
        lineImg.color = new Color(0.15f, 0.1f, 0.05f, 0.8f);
        RectTransform lineRect = lineGo.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 0);
        lineRect.anchorMax = new Vector2(1, 0);
        lineRect.offsetMin = new Vector2(0, 0);
        lineRect.offsetMax = new Vector2(0, 2);

        Color activeTitleCol = new Color(0.9f, 0.85f, 0.7f);
        Color inactiveTitleCol = new Color(0.5f, 0.45f, 0.4f);
        Color activeDescCol = new Color(0.6f, 0.55f, 0.45f);
        Color inactiveDescCol = new Color(0.4f, 0.35f, 0.3f);
        Color activePriceCol = new Color(0.9f, 0.8f, 0.6f);
        Color inactivePriceCol = new Color(0.5f, 0.45f, 0.4f);

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(itemGo.transform, false);
        Image iconImg = iconGo.AddComponent<Image>();
        Texture2D iconTex = Resources.Load<Texture2D>(drink.IconPath);
        if (iconTex != null)
        {
            iconImg.sprite = Sprite.Create(iconTex, new Rect(0, 0, iconTex.width, iconTex.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            iconImg.color = new Color(0.2f, 0.2f, 0.2f);
        }

        if (!drink.Available)
        {
            iconImg.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
        }

        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.sizeDelta = new Vector2(80, 80);
        iconRect.anchoredPosition = new Vector2(20, 0);

        CreateText(itemGo.transform, "Name", UiText.LocalizeKnownText(drink.Name), fontRegular, 26, drink.Available ? activeTitleCol : inactiveTitleCol, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(0.35f, 1), new Vector2(0, 0));
        RectTransform nameRt = itemGo.transform.Find("Name").GetComponent<RectTransform>();
        nameRt.offsetMin = new Vector2(120, 0);

        CreateText(itemGo.transform, "Desc", UiText.LocalizeKnownText(drink.Description), fontRegular, 18, drink.Available ? activeDescCol : inactiveDescCol, TextAnchor.MiddleLeft, new Vector2(0.35f, 0), new Vector2(0.85f, 1), new Vector2(0, 0));
        CreateText(itemGo.transform, "Price", drink.Price.ToString(), fontRegular, 28, drink.Available ? activePriceCol : inactivePriceCol, TextAnchor.MiddleRight, new Vector2(0.85f, 0), new Vector2(1, 1), new Vector2(-60, 0));
        Text priceText = itemGo.transform.Find("Price").GetComponent<Text>();

        GameObject coinGo = new GameObject("Coin");
        coinGo.transform.SetParent(itemGo.transform, false);
        Image coinImg = coinGo.AddComponent<Image>();
        Texture2D coinTex = Resources.Load<Texture2D>("ShopUI/icon_coin");
        if (coinTex != null)
        {
            coinImg.sprite = Sprite.Create(coinTex, new Rect(0, 0, coinTex.width, coinTex.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            coinImg.color = Color.yellow;
        }

        if (!drink.Available)
        {
            coinImg.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }

        RectTransform coinRect = coinGo.GetComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(1, 0.5f);
        coinRect.anchorMax = new Vector2(1, 0.5f);
        coinRect.pivot = new Vector2(1, 0.5f);
        coinRect.sizeDelta = new Vector2(32, 32);
        coinRect.anchoredPosition = new Vector2(-20, 0);

        _rows.Add(new ShopRowRefs
        {
            Drink = drink,
            PriceText = priceText,
            CoinImage = coinImg
        });

        if (!drink.Available)
        {
            return;
        }

        Button btn = itemGo.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0f, 0f, 0f, 0f);
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
        cb.pressedColor = new Color(0f, 0f, 0f, 0.2f);
        cb.selectedColor = new Color(0f, 0f, 0f, 0f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;
        btn.onClick.AddListener(() => TryPurchaseDrink(drink));
    }

    private void TryPurchaseDrink(DrinkData drink)
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            SetStatus(UiText.LocalizeKnownText("Inventario nao encontrado."));
            return;
        }

        bool hasSpace = drink.ItemKind switch
        {
            ShopItemKind.RedPotion => inventory.CanAddRedPotion(),
            ShopItemKind.BluePotion => inventory.CanAddBluePotion(),
            _ => false
        };

        if (!hasSpace)
        {
            SetStatus(UiText.LocalizeKnownText("Ja tens o maximo de 5 dessa garrafa."));
            if (dialogoInventarioCheio != null) 
            {
                DialogueUIManager.Instance.StartDialogue(dialogoInventarioCheio);
            }
            return;
        }

        if (!inventory.SpendCoins(drink.Price))
        {
            SetStatus(UiText.LocalizeKnownText("Nao tens moedas suficientes."));
            return;
        }

        switch (drink.ItemKind)
        {
            case ShopItemKind.RedPotion:
                inventory.AddRedPotion();
                break;
            case ShopItemKind.BluePotion:
                inventory.AddBluePotion();
                break;
        }

        SetStatus(UiText.Choose("Compraste: ", "Purchased: ") + UiText.LocalizeKnownText(drink.Name));
        RefreshShopRows();
        
        if (dialogoComprar != null) 
        {
            DialogueUIManager.Instance.StartDialogue(dialogoComprar);
        }
        _comprouAlgo = true;
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }
    }

    private void RefreshShopRows()
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        foreach (ShopRowRefs row in _rows)
        {
            bool isFull = inventory != null && IsItemFull(inventory, row.Drink.ItemKind);
            if (row.PriceText != null)
            {
                row.PriceText.text = isFull ? UiText.LocalizeKnownText("CHEIO") : row.Drink.Price.ToString();
            }

            if (row.CoinImage != null)
            {
                row.CoinImage.enabled = !isFull;
            }
        }
    }

    private static bool IsItemFull(PlayerInventory inventory, ShopItemKind itemKind)
    {
        return itemKind switch
        {
            ShopItemKind.RedPotion => inventory.RedPotions >= PlayerInventory.MaxPotionCount,
            ShopItemKind.BluePotion => inventory.BluePotions >= PlayerInventory.MaxPotionCount,
            _ => false
        };
    }

    public void Show()
    {
        if (IsVisible) return;

        _shopPanel.SetActive(true);
        RefreshShopRows();
        SetStatus(UiText.LocalizeKnownText("Cada garrafa pode ir ate 5 no inventario."));
        Time.timeScale = 0f;
        _comprouAlgo = false;

        _wasCursorVisible = Cursor.visible;
        _previousLockMode = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Hide()
    {
        if (!IsVisible) return;

        _shopPanel.SetActive(false);
        SetStatus(string.Empty);
        Time.timeScale = 1f;

        Cursor.visible = _wasCursorVisible;
        Cursor.lockState = _previousLockMode;

        if (!_comprouAlgo && dialogoSairSemComprar != null)
        {
            DialogueUIManager.Instance.StartDialogue(dialogoSairSemComprar);
        }
    }

    private void Update()
    {
        if (IsVisible && IsEscapePressed())
        {
            Hide();
        }
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
