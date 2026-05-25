using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerHUDController : MonoBehaviour
{
    public static PlayerHUDController Instance { get; private set; }

    // Colors for Minimalist HUD
    static readonly Color HpFill   = new Color(0.7f, 0.1f, 0.1f, 0.9f);
    static readonly Color HpBg     = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    static readonly Color TextDim  = new Color(0.8f, 0.8f, 0.8f, 0.9f);
    static readonly Color GoldText = new Color(0.9f, 0.8f, 0.5f, 1f);

    private Image _hpFill;
    private Text _redCount, _blueCount, _greenCount;
    private Text _coinsText, _gemsText;
    private Text _interactionPrompt;
    private readonly List<(Text title, Text desc)> _objRows = new List<(Text, Text)>();

    void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene")
            Destroy(gameObject);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (PauseMenuController.Instance == null)
        {
            var pausePrefab = Resources.Load<GameObject>("PauseMenuCanvas");
            if (pausePrefab != null)
            {
                var inst = Instantiate(pausePrefab);
                inst.name = "PauseMenuCanvas";
            }
        }

        BuildHUD();
    }

    void Start() => StartCoroutine(LateSubscribe());

    IEnumerator LateSubscribe()
    {
        yield return null;
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph != null)
        {
            ph.onHealthChanged.AddListener(OnHpChanged);
            OnHpChanged(ph.CurrentHealth, ph.maxHealth);
        }
        var inv = PlayerInventory.Instance;
        if (inv != null)
        {
            inv.onCoinsChanged.AddListener(c => { if (_coinsText) _coinsText.text = c.ToString("N0"); });
            inv.onGemsChanged.AddListener(g  => { if (_gemsText)  _gemsText.text  = g.ToString(); });
            inv.onPotionsChanged.AddListener(OnPotions);
            if (_coinsText) _coinsText.text = inv.Coins.ToString("N0");
            if (_gemsText)  _gemsText.text  = inv.Gems.ToString();
            OnPotions(inv.RedPotions, inv.BluePotions, inv.GreenPotions);
        }
    }

    void OnHpChanged(float cur, float max)
    {
        if (_hpFill) _hpFill.fillAmount = max > 0 ? cur / max : 0f;
    }

    void OnPotions(int r, int b, int g)
    {
        if (_redCount)   _redCount.text   = r.ToString();
        if (_blueCount)  _blueCount.text  = b.ToString();
        if (_greenCount) _greenCount.text = g.ToString();
    }

    public void SetObjective(int i, string title, string desc, bool done = false)
    {
        if (i < 0 || i >= _objRows.Count) return;
        var (t, d) = _objRows[i];
        t.text  = title;
        t.color = done ? new Color(0.5f, 0.8f, 0.5f, 0.9f) : GoldText;
        if (d != null) d.text = desc;
    }

    void BuildHUD()
    {
        Font reg  = Resources.Load<Font>("Fonts/Cinzel-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Font bold = Resources.Load<Font>("Fonts/Cinzel-Bold") ?? reg;

        var cGo = new GameObject("HUDCanvas");
        cGo.transform.SetParent(transform);
        var cv = cGo.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var sc = cGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cGo.AddComponent<GraphicRaycaster>();

        var root = cGo.transform;
        
        BuildHealthBar(root);
        BuildPotions(root, bold);
        BuildResources(root, bold);
        BuildInteractionPrompt(root, bold);
    }

    void BuildInteractionPrompt(Transform root, Font font)
    {
        var promptGo = new GameObject("InteractionPrompt");
        promptGo.transform.SetParent(root, false);

        _interactionPrompt = promptGo.AddComponent<Text>();
        _interactionPrompt.font = font;
        _interactionPrompt.fontSize = 22;
        _interactionPrompt.alignment = TextAnchor.MiddleCenter;
        _interactionPrompt.color = new Color(0.95f, 0.90f, 0.75f, 1f);
        _interactionPrompt.text = "";
        _interactionPrompt.enabled = false;

        var outline = promptGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        var rt = promptGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.18f);
        rt.anchorMax = new Vector2(0.5f, 0.18f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 42f);
        rt.anchoredPosition = Vector2.zero;
    }

    public void ShowInteractionPrompt(string message)
    {
        if (_interactionPrompt == null) return;
        _interactionPrompt.text = message;
        _interactionPrompt.enabled = !string.IsNullOrWhiteSpace(message);
    }

    public void HideInteractionPrompt()
    {
        if (_interactionPrompt == null) return;
        _interactionPrompt.text = "";
        _interactionPrompt.enabled = false;
    }

    void BuildHealthBar(Transform root)
    {
        var track = new GameObject("HpTrack");
        track.transform.SetParent(root, false);
        var bg = track.AddComponent<Image>();
        bg.color = HpBg;
        var tRT = track.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(0, 1);
        tRT.pivot = new Vector2(0, 1);
        tRT.sizeDelta = new Vector2(400, 12);
        tRT.anchoredPosition = new Vector2(40, -40);
        
        var outline = new GameObject("HpOutline");
        outline.transform.SetParent(track.transform, false);
        var outImg = outline.AddComponent<Image>();
        outImg.color = new Color(0,0,0,0.8f);
        var oRT = outline.GetComponent<RectTransform>();
        oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
        oRT.sizeDelta = new Vector2(4, 4);
        outline.transform.SetAsFirstSibling();

        var fillGo = new GameObject("HpFill");
        fillGo.transform.SetParent(track.transform, false);
        _hpFill = fillGo.AddComponent<Image>();
        _hpFill.color = HpFill;
        _hpFill.type = Image.Type.Filled;
        _hpFill.fillMethod = Image.FillMethod.Horizontal;
        _hpFill.fillAmount = 1f;
        var fRT = fillGo.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = Vector2.zero;
        fRT.anchoredPosition = Vector2.zero;
    }

    void BuildPotions(Transform root, Font bold)
    {
        var container = new GameObject("PotionsContainer");
        container.transform.SetParent(root, false);
        var cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(0, 0);
        cRT.pivot = new Vector2(0, 0);
        cRT.anchoredPosition = new Vector2(40, 40);

        var texVinho = Resources.Load<Texture2D>("ShopUI/icon_vinho_vida");
        var texBencao = Resources.Load<Texture2D>("ShopUI/icon_bencao_baco");

        _redCount = CreateFlask(container.transform, bold, "Q", texVinho, 0);
        _blueCount = CreateFlask(container.transform, bold, "E", texBencao, 110);
    }

    Text CreateFlask(Transform parent, Font font, string key, Texture2D tex, float x)
    {
        var slot = new GameObject($"Flask_{key}");
        slot.transform.SetParent(parent, false);
        var sRT = slot.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(0, 0);
        sRT.pivot = new Vector2(0, 0);
        sRT.sizeDelta = new Vector2(80, 80);
        sRT.anchoredPosition = new Vector2(x, 0);

        var fill = new GameObject("Icon");
        fill.transform.SetParent(slot.transform, false);
        var fImg = fill.AddComponent<Image>();
        if (tex != null) fImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        var fRT = fill.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = Vector2.zero;
        fRT.anchoredPosition = Vector2.zero;

        Borders(slot.transform, new Color(0.52f, 0.40f, 0.10f, 0.85f), 1.5f);

        var badgeGo = new GameObject("KeyBadge");
        badgeGo.transform.SetParent(slot.transform, false);
        var bImg = badgeGo.AddComponent<Image>();
        bImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        var bRT = badgeGo.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 1f); bRT.anchorMax = new Vector2(0.5f, 1f);
        bRT.pivot = new Vector2(0.5f, 0.5f);
        bRT.sizeDelta = new Vector2(28, 28);
        bRT.anchoredPosition = new Vector2(0, 0);
        
        Borders(badgeGo.transform, new Color(0.52f, 0.40f, 0.10f, 0.85f), 1.5f);

        var txt = new GameObject("KeyText");
        txt.transform.SetParent(badgeGo.transform, false);
        var t = txt.AddComponent<Text>();
        t.font = font; t.text = key; t.fontSize = 16;
        t.color = new Color(0.9f, 0.8f, 0.5f, 1f);
        t.alignment = TextAnchor.MiddleCenter;
        var tRT = txt.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.sizeDelta = Vector2.zero;
        tRT.anchoredPosition = new Vector2(0, 1);

        var diamond = new GameObject("Diamond");
        diamond.transform.SetParent(slot.transform, false);
        var dImg = diamond.AddComponent<Image>();
        dImg.color = new Color(0.52f, 0.40f, 0.10f, 1f);
        var dRT = diamond.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0f); dRT.anchorMax = new Vector2(0.5f, 0f);
        dRT.pivot = new Vector2(0.5f, 0.5f);
        dRT.sizeDelta = new Vector2(10, 10);
        dRT.anchoredPosition = new Vector2(0, 0);
        diamond.transform.localRotation = Quaternion.Euler(0, 0, 45);

        var dIn = new GameObject("DiamondInner");
        dIn.transform.SetParent(diamond.transform, false);
        var diImg = dIn.AddComponent<Image>();
        diImg.color = new Color(0.9f, 0.8f, 0.5f, 1f);
        var diRT = dIn.GetComponent<RectTransform>();
        diRT.anchorMin = new Vector2(0.5f, 0.5f); diRT.anchorMax = new Vector2(0.5f, 0.5f);
        diRT.pivot = new Vector2(0.5f, 0.5f);
        diRT.sizeDelta = new Vector2(4, 4);
        diRT.anchoredPosition = Vector2.zero;

        var count = new GameObject("Count");
        count.transform.SetParent(slot.transform, false);
        var ct = count.AddComponent<Text>();
        ct.font = font; ct.text = "0"; ct.fontSize = 24;
        ct.color = Color.white;
        ct.alignment = TextAnchor.MiddleRight;
        ct.horizontalOverflow = HorizontalWrapMode.Overflow;
        ct.verticalOverflow = VerticalWrapMode.Overflow;
        
        var outC = count.AddComponent<Outline>();
        outC.effectColor = Color.black;
        outC.effectDistance = new Vector2(1, -1);

        var ctRT = count.GetComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(1f, 0f); ctRT.anchorMax = new Vector2(1f, 0f);
        ctRT.pivot = new Vector2(1f, 0f);
        ctRT.sizeDelta = new Vector2(40, 30);
        ctRT.anchoredPosition = new Vector2(10, -10);

        return ct;
    }

    void BuildObjectives(Transform root, Font bold, Font reg)
    {
        var container = new GameObject("ObjectivesContainer");
        container.transform.SetParent(root, false);
        var cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1, 0.6f); cRT.anchorMax = new Vector2(1, 0.6f);
        cRT.pivot = new Vector2(1, 0.5f);
        cRT.anchoredPosition = new Vector2(-40, 0);

        string[] titles = { "Explorar a masmorra", "Derrotar os chefes", "Encontrar a saida" };
        string[] descs  = { "Va mais fundo na masmorra.", "0 / 2", "Encontre uma forma de sair." };

        for (int i = 0; i < 3; i++)
        {
            var titleGo = new GameObject($"ObjTitle{i}");
            titleGo.transform.SetParent(container.transform, false);
            var tt = titleGo.AddComponent<Text>();
            tt.font = bold; tt.text = titles[i]; tt.fontSize = 18;
            tt.color = GoldText; tt.alignment = TextAnchor.MiddleRight;
            
            var outline = titleGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            var ttRT = titleGo.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(1, 1); ttRT.anchorMax = new Vector2(1, 1);
            ttRT.pivot = new Vector2(1, 1);
            ttRT.sizeDelta = new Vector2(300, 30);
            ttRT.anchoredPosition = new Vector2(0, -i * 60f);

            var descGo = new GameObject($"ObjDesc{i}");
            descGo.transform.SetParent(container.transform, false);
            var dt = descGo.AddComponent<Text>();
            dt.font = reg; dt.text = descs[i]; dt.fontSize = 14;
            dt.color = TextDim; dt.alignment = TextAnchor.MiddleRight;
            
            var outlineD = descGo.AddComponent<Outline>();
            outlineD.effectColor = Color.black;
            outlineD.effectDistance = new Vector2(1, -1);

            var dtRT = descGo.GetComponent<RectTransform>();
            dtRT.anchorMin = new Vector2(1, 1); dtRT.anchorMax = new Vector2(1, 1);
            dtRT.pivot = new Vector2(1, 1);
            dtRT.sizeDelta = new Vector2(300, 30);
            dtRT.anchoredPosition = new Vector2(0, -20f - i * 60f);

            _objRows.Add((tt, dt));
        }
    }

    void BuildResources(Transform root, Font bold)
    {
        var container = new GameObject("ResourcesContainer");
        container.transform.SetParent(root, false);
        var cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1, 1); cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(1, 1);
        cRT.sizeDelta = new Vector2(160, 48);
        cRT.anchoredPosition = new Vector2(-40, -40);

        var bg = container.AddComponent<Image>();
        bg.color = new Color(12f/255f, 15f/255f, 20f/255f, 1f);

        Borders(container.transform, new Color(0.52f, 0.40f, 0.10f, 0.85f), 1.5f);

        var coinTex = Resources.Load<Texture2D>("ShopUI/icon_coin");
        
        var coinImg = new GameObject("CoinIcon");
        coinImg.transform.SetParent(container.transform, false);
        var ci = coinImg.AddComponent<Image>();
        if (coinTex != null) ci.sprite = Sprite.Create(coinTex, new Rect(0,0,coinTex.width,coinTex.height), new Vector2(0.5f,0.5f));
        var ciRT = coinImg.GetComponent<RectTransform>();
        ciRT.anchorMin = new Vector2(0, 0.5f); ciRT.anchorMax = new Vector2(0, 0.5f);
        ciRT.pivot = new Vector2(0, 0.5f);
        ciRT.sizeDelta = new Vector2(36, 36);
        ciRT.anchoredPosition = new Vector2(24, 0);

        var coinTextGo = new GameObject("CoinText");
        coinTextGo.transform.SetParent(container.transform, false);
        _coinsText = coinTextGo.AddComponent<Text>();
        _coinsText.font = bold; _coinsText.text = "0"; _coinsText.fontSize = 24;
        _coinsText.color = GoldText; _coinsText.alignment = TextAnchor.MiddleRight;
        _coinsText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _coinsText.verticalOverflow = VerticalWrapMode.Overflow;
        
        var outC = coinTextGo.AddComponent<Outline>();
        outC.effectColor = Color.black;
        outC.effectDistance = new Vector2(1, -1);

        var ctRT = coinTextGo.GetComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(1, 0.5f); ctRT.anchorMax = new Vector2(1, 0.5f);
        ctRT.pivot = new Vector2(1, 0.5f);
        ctRT.sizeDelta = new Vector2(100, 30);
        ctRT.anchoredPosition = new Vector2(-16, 0);
    }

    void Borders(Transform parent, Color col, float t)
    {
        void Line(string n, Vector2 aMin, Vector2 aMax, Vector2 piv, bool horiz)
        {
            var ln = new GameObject(n);
            ln.transform.SetParent(parent, false);
            var img = ln.AddComponent<Image>();
            img.color = col; img.raycastTarget = false;
            var rt = ln.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = piv;
            rt.sizeDelta = horiz ? new Vector2(0,t) : new Vector2(t,0);
            rt.anchoredPosition = Vector2.zero;
        }
        Line("BT", new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), true);
        Line("BB", new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), true);
        Line("BL", new Vector2(0,0), new Vector2(0,1), new Vector2(0,0.5f), false);
        Line("BR", new Vector2(1,0), new Vector2(1,1), new Vector2(1,0.5f), false);
    }
}
