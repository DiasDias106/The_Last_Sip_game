using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerHUDController : MonoBehaviour
{
    public static PlayerHUDController Instance { get; private set; }

    // ── Palette ──────────────────────────────────────────────────────────────────
    static readonly Color BG        = new Color(0.04f, 0.03f, 0.02f, 0.86f);
    static readonly Color Gold      = new Color(0.82f, 0.66f, 0.22f, 1.00f);
    static readonly Color GoldDim   = new Color(0.40f, 0.30f, 0.08f, 1.00f);
    static readonly Color GoldBdr   = new Color(0.52f, 0.40f, 0.10f, 0.85f);
    static readonly Color GoldText  = new Color(0.96f, 0.86f, 0.55f, 1.00f);
    static readonly Color White     = new Color(0.94f, 0.91f, 0.84f, 1.00f);
    static readonly Color Dim       = new Color(0.60f, 0.56f, 0.48f, 1.00f);
    static readonly Color HpFill    = new Color(0.82f, 0.06f, 0.06f, 1.00f);
    static readonly Color HpBg      = new Color(0.20f, 0.03f, 0.03f, 0.95f);
    static readonly Color StamFill  = new Color(0.12f, 0.68f, 0.22f, 1.00f);
    static readonly Color StamBg    = new Color(0.03f, 0.18f, 0.05f, 0.95f);

    // ── References ───────────────────────────────────────────────────────────────
    private Image _hpFill, _stamFill;
    private Text  _hpText, _stamText, _levelText;
    private Text  _coinsText, _gemsText;
    private Text  _redCount, _blueCount, _greenCount;
    private readonly List<(Text title, Text desc)> _objRows = new List<(Text, Text)>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
            ph.onStaminaChanged.AddListener(OnStamChanged);
            OnHpChanged(ph.CurrentHealth, ph.maxHealth);
            OnStamChanged(ph.CurrentStamina, ph.maxStamina);
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
        if (_hpText) _hpText.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
    }
    void OnStamChanged(float cur, float max)
    {
        if (_stamFill) _stamFill.fillAmount = max > 0 ? cur / max : 0f;
        if (_stamText) _stamText.text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
    }
    void OnPotions(int r, int b, int g)
    {
        if (_redCount)   _redCount.text   = r.ToString();
        if (_blueCount)  _blueCount.text  = b.ToString();
        if (_greenCount) _greenCount.text = g.ToString();
    }

    public void SetLevel(int lvl) { if (_levelText) _levelText.text = lvl.ToString(); }

    public void SetObjective(int i, string title, string desc, bool done = false)
    {
        if (i < 0 || i >= _objRows.Count) return;
        var (t, d) = _objRows[i];
        t.text  = title;
        t.color = done ? new Color(0.4f, 0.88f, 0.4f) : White;
        if (d != null) d.text = desc;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BUILD
    // ═══════════════════════════════════════════════════════════════════════════
    void BuildHUD()
    {
        Font reg  = Resources.Load<Font>("Fonts/Cinzel-Regular")
                    ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        BuildCharacterPanel(root, bold, reg);
        BuildCompass(root, reg);
        BuildResources(root, bold);
        BuildObjectives(root, bold, reg);
        BuildPotions(root, bold, reg);
        BuildActionBar(root, bold);
    }

    // ── Character Panel ── top-left ─────────────────────────────────────────────
    void BuildCharacterPanel(Transform root, Font bold, Font reg)
    {
        var panel = Panel("CharPanel", root,
            new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
            new Vector2(300, 116), new Vector2(14, -14));

        // Outer gold border
        Borders(panel, GoldBdr, 1.5f);

        // Left gold accent strip
        Stripe(panel, true, GoldDim, 4f);

        // Portrait area
        var portrait = R("Portrait", panel);
        portrait.anchorMin = new Vector2(0,0.5f); portrait.anchorMax = new Vector2(0,0.5f);
        portrait.pivot     = new Vector2(0,0.5f);
        portrait.sizeDelta = new Vector2(90, 90); portrait.anchoredPosition = new Vector2(12, 0);

        var portBg = I("PortBg", portrait, new Color(0.08f, 0.06f, 0.04f, 1f));
        Fill(portBg.rectTransform);
        Borders(portrait, Gold, 2f);

        // Lion-crest placeholder text
        var crest = T("Crest", portrait, bold, "L", 38, new Color(0.78f,0.60f,0.16f,1f), TextAnchor.MiddleCenter);
        Fill(crest.rectTransform);

        // Level badge
        var lvlBg = I("LvlBg", portrait, GoldDim);
        var lbRT = lvlBg.rectTransform;
        lbRT.anchorMin = new Vector2(0,0); lbRT.anchorMax = new Vector2(0,0);
        lbRT.pivot = new Vector2(0,0);
        lbRT.sizeDelta = new Vector2(28,22); lbRT.anchoredPosition = Vector2.zero;
        _levelText = T("LvlTxt", lvlBg.transform, bold, "1", 14, Gold, TextAnchor.MiddleCenter);
        Fill(_levelText.rectTransform);

        // Bars
        float bX = 112f, bW = 174f;
        _hpFill   = Bar("HP",   panel, HpFill,   HpBg,   bX, -18f, bW, 24f, bold, "100 / 100", out _hpText);
        _stamFill = Bar("Stam", panel, StamFill, StamBg, bX, -50f, bW, 24f, bold, "100 / 100", out _stamText);

        // Status icon row
        for (int i = 0; i < 3; i++)
        {
            var ic = I($"SI{i}", panel, new Color(0.10f,0.08f,0.06f,0.9f));
            var rt = ic.rectTransform;
            rt.anchorMin = new Vector2(0,0); rt.anchorMax = new Vector2(0,0);
            rt.pivot     = new Vector2(0,0);
            rt.sizeDelta = new Vector2(26,26); rt.anchoredPosition = new Vector2(bX + i * 30f, 10f);
            Borders(ic.transform, GoldBdr, 1f);
        }
    }

    // ── Compass ── top-center ────────────────────────────────────────────────────
    void BuildCompass(Transform root, Font reg)
    {
        var panel = Panel("Compass", root,
            new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0.5f,1),
            new Vector2(440, 38), new Vector2(0, -14));

        string[] labels = { "SO", "S", "SE", "L", "NE", "N", "NO" };
        float[]  offsets= {-192, -128, -64,   0,   64,  128,  192 };
        for (int i = 0; i < labels.Length; i++)
        {
            bool isCentre = i == 3;
            var t = T($"D{i}", panel, reg, labels[i], isCentre ? 16 : 13,
                isCentre ? Gold : Dim, TextAnchor.MiddleCenter);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f,0); rt.anchorMax = new Vector2(0.5f,1);
            rt.pivot = new Vector2(0.5f,0.5f);
            rt.sizeDelta = new Vector2(56,0); rt.anchoredPosition = new Vector2(offsets[i],0);
        }
        // Centre tick mark
        var tick = I("Tick", panel, Gold);
        var tkRT = tick.rectTransform;
        tkRT.anchorMin = new Vector2(0.5f,0); tkRT.anchorMax = new Vector2(0.5f,0);
        tkRT.pivot = new Vector2(0.5f,0);
        tkRT.sizeDelta = new Vector2(2,8); tkRT.anchoredPosition = Vector2.zero;
        // Bottom line
        var ln = I("Line", panel, GoldBdr);
        var lRT = ln.rectTransform;
        lRT.anchorMin = new Vector2(0,0); lRT.anchorMax = new Vector2(1,0);
        lRT.pivot = new Vector2(0.5f,0);
        lRT.sizeDelta = new Vector2(0,1); lRT.anchoredPosition = Vector2.zero;
    }

    // ── Resources ── top-right ────────────────────────────────────────────────────
    void BuildResources(Transform root, Font bold)
    {
        var panel = Panel("Resources", root,
            new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
            new Vector2(286, 46), new Vector2(-14, -14));
        Borders(panel, GoldBdr, 1.5f);

        // Coin
        var coinImg = I("CoinIco", panel, Gold);
        Texture2D ct = Resources.Load<Texture2D>("ShopUI/icon_coin");
        if (ct != null) coinImg.sprite = Sprite.Create(ct, new Rect(0,0,ct.width,ct.height), new Vector2(0.5f,0.5f));
        var ciRT = coinImg.rectTransform;
        ciRT.anchorMin = new Vector2(0,0.5f); ciRT.anchorMax = new Vector2(0,0.5f);
        ciRT.pivot = new Vector2(0,0.5f);
        ciRT.sizeDelta = new Vector2(30,30); ciRT.anchoredPosition = new Vector2(10,0);

        _coinsText = T("Coins", panel, bold, "0", 20, GoldText, TextAnchor.MiddleLeft);
        var ctRT = _coinsText.rectTransform;
        ctRT.anchorMin = new Vector2(0,0); ctRT.anchorMax = new Vector2(0,1);
        ctRT.pivot = new Vector2(0,0.5f);
        ctRT.sizeDelta = new Vector2(100,0); ctRT.anchoredPosition = new Vector2(46,0);

        // Separator
        var sp = I("Sep", panel, GoldBdr);
        var spRT = sp.rectTransform;
        spRT.anchorMin = new Vector2(0,0.15f); spRT.anchorMax = new Vector2(0,0.85f);
        spRT.pivot = new Vector2(0,0.5f);
        spRT.sizeDelta = new Vector2(1,0); spRT.anchoredPosition = new Vector2(154,0);

        // Gem icon (blue circle)
        var gemImg = I("GemIco", panel, new Color(0.3f,0.55f,0.95f,1f));
        var giRT = gemImg.rectTransform;
        giRT.anchorMin = new Vector2(0,0.5f); giRT.anchorMax = new Vector2(0,0.5f);
        giRT.pivot = new Vector2(0,0.5f);
        giRT.sizeDelta = new Vector2(26,26); giRT.anchoredPosition = new Vector2(163,0);

        _gemsText = T("Gems", panel, bold, "0", 20, new Color(0.72f,0.86f,1f,1f), TextAnchor.MiddleLeft);
        var gtRT = _gemsText.rectTransform;
        gtRT.anchorMin = new Vector2(0,0); gtRT.anchorMax = new Vector2(0,1);
        gtRT.pivot = new Vector2(0,0.5f);
        gtRT.sizeDelta = new Vector2(60,0); gtRT.anchoredPosition = new Vector2(196,0);

        // Settings gear icon button
        var gearBg = I("GearBg", panel, new Color(0.10f,0.08f,0.06f,0.9f));
        Borders(gearBg.transform, GoldBdr, 1f);
        var gRT = gearBg.rectTransform;
        gRT.anchorMin = new Vector2(1,0.5f); gRT.anchorMax = new Vector2(1,0.5f);
        gRT.pivot = new Vector2(1,0.5f);
        gRT.sizeDelta = new Vector2(36,36); gRT.anchoredPosition = new Vector2(-5,0);
        var gearTxt = T("GearTxt", gearBg.transform, null, "=", 16, Dim, TextAnchor.MiddleCenter);
        Fill(gearTxt.rectTransform);
    }

    // ── Objectives ── right ───────────────────────────────────────────────────────
    void BuildObjectives(Transform root, Font bold, Font reg)
    {
        var panel = R("Objectives", root);
        panel.SetParent(root, false);
        panel.anchorMin = new Vector2(1,0.5f); panel.anchorMax = new Vector2(1,0.5f);
        panel.pivot     = new Vector2(1,0.5f);
        panel.sizeDelta = new Vector2(310, 270); panel.anchoredPosition = new Vector2(-14, 40);

        // Title
        var titleTxt = T("Title", panel, bold, "OBJETIVOS", 19, Gold, TextAnchor.UpperRight);
        var tRT = titleTxt.rectTransform;
        tRT.anchorMin = new Vector2(0,1); tRT.anchorMax = new Vector2(1,1);
        tRT.pivot = new Vector2(0.5f,1f);
        tRT.sizeDelta = new Vector2(0,32); tRT.anchoredPosition = Vector2.zero;

        // Underline
        var ul = I("UL", panel, GoldBdr);
        var ulRT = ul.rectTransform;
        ulRT.anchorMin = new Vector2(0,1); ulRT.anchorMax = new Vector2(1,1);
        ulRT.pivot = new Vector2(0.5f,1f);
        ulRT.sizeDelta = new Vector2(0,1); ulRT.anchoredPosition = new Vector2(0,-32);

        // 3 objective rows
        string[] titles = { "Explorar a masmorra", "Derrotar os chefes", "Encontrar a saida" };
        string[] descs  = { "Va mais fundo na masmorra.", "0 / 2", "Encontre uma forma de sair." };
        for (int i = 0; i < 3; i++)
        {
            float y = -36f - i * 76f;

            // Diamond bullet (rotated square image)
            var dia = I($"Dia{i}", panel, Gold);
            dia.transform.localRotation = Quaternion.Euler(0,0,45);
            var dRT = dia.rectTransform;
            dRT.anchorMin = new Vector2(1,1); dRT.anchorMax = new Vector2(1,1);
            dRT.pivot = new Vector2(1,1f);
            dRT.sizeDelta = new Vector2(12,12); dRT.anchoredPosition = new Vector2(-8, y - 6);

            // Title
            var objT = T($"OT{i}", panel, bold, titles[i], 16, White, TextAnchor.UpperRight);
            var otRT = objT.rectTransform;
            otRT.anchorMin = new Vector2(0,1); otRT.anchorMax = new Vector2(1,1);
            otRT.pivot = new Vector2(0.5f,1f);
            otRT.sizeDelta = new Vector2(-28, 28); otRT.anchoredPosition = new Vector2(-10, y);

            // Description
            var objD = T($"OD{i}", panel, reg, descs[i], 13, Dim, TextAnchor.UpperRight);
            var odRT = objD.rectTransform;
            odRT.anchorMin = new Vector2(0,1); odRT.anchorMax = new Vector2(1,1);
            odRT.pivot = new Vector2(0.5f,1f);
            odRT.sizeDelta = new Vector2(-28, 44); odRT.anchoredPosition = new Vector2(-10, y - 28);

            _objRows.Add((objT, objD));
        }
    }

    // ── Potions ── bottom-left ────────────────────────────────────────────────────
    void BuildPotions(Transform root, Font bold, Font reg)
    {
        var panel = Panel("Potions", root,
            new Vector2(0,0), new Vector2(0,0), new Vector2(0,0),
            new Vector2(320, 132), new Vector2(14, 14));
        Borders(panel, GoldBdr, 1.5f);

        // "— CURAS —" bottom label
        var lbl = T("CurasLbl", panel, bold, "- CURAS -", 13, GoldBdr, TextAnchor.LowerCenter);
        var lRT = lbl.rectTransform;
        lRT.anchorMin = new Vector2(0,0); lRT.anchorMax = new Vector2(1,0);
        lRT.pivot = new Vector2(0.5f,0f);
        lRT.sizeDelta = new Vector2(0,20); lRT.anchoredPosition = new Vector2(0,3);

        var (rf, bf, gf) = (new Color(0.80f,0.05f,0.05f,1f), new Color(0.10f,0.22f,0.78f,1f), new Color(0.05f,0.52f,0.13f,1f));
        var (rl, bl, gl) = (new Color(1f,0.3f,0.3f,1f),     new Color(0.4f,0.65f,1f,1f),     new Color(0.35f,0.92f,0.4f,1f));

        Flask(panel, bold, reg, rf, rl, "Q", 0, 14f);
        Flask(panel, bold, reg, bf, bl, "E", 1, 112f);
        Flask(panel, bold, reg, gf, gl, "R", 2, 210f);
    }

    void Flask(RectTransform parent, Font bold, Font reg, Color fill, Color light, string key, int idx, float x)
    {
        var slot = R($"Flask_{key}", parent);
        slot.anchorMin = new Vector2(0,0); slot.anchorMax = new Vector2(0,0);
        slot.pivot = new Vector2(0,0);
        slot.sizeDelta = new Vector2(90,106); slot.anchoredPosition = new Vector2(x,18);

        // Key label at top
        var keyT = T("Key", slot, bold, key, 15, Gold, TextAnchor.UpperCenter);
        var kRT = keyT.rectTransform;
        kRT.anchorMin = new Vector2(0,1); kRT.anchorMax = new Vector2(1,1);
        kRT.pivot = new Vector2(0.5f,1f);
        kRT.sizeDelta = new Vector2(0,20); kRT.anchoredPosition = Vector2.zero;

        // Flask body
        var body = I("Body", slot, new Color(fill.r*0.25f, fill.g*0.25f, fill.b*0.25f, 0.96f));
        var bRT = body.rectTransform;
        bRT.anchorMin = new Vector2(0,0); bRT.anchorMax = new Vector2(1,1);
        bRT.offsetMin = new Vector2(8,22); bRT.offsetMax = new Vector2(-8,-24);
        Borders(body.transform, new Color(light.r*0.6f, light.g*0.6f, light.b*0.6f, 0.6f), 1.5f);

        // Fill
        var fillImg = I("Fill", body.transform, fill);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillAmount = 0.65f;
        var fRT = fillImg.rectTransform;
        fRT.anchorMin = new Vector2(0.1f,0.05f); fRT.anchorMax = new Vector2(0.9f,0.95f);
        fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;

        // Shine
        var shine = I("Shine", body.transform, new Color(1f,1f,1f,0.10f));
        var sRT = shine.rectTransform;
        sRT.anchorMin = new Vector2(0.55f,0.35f); sRT.anchorMax = new Vector2(0.82f,0.92f);
        sRT.offsetMin = Vector2.zero; sRT.offsetMax = Vector2.zero;

        // Count at bottom
        var cntBg = I("CntBg", slot, new Color(0f,0f,0f,0.82f));
        var cbRT = cntBg.rectTransform;
        cbRT.anchorMin = new Vector2(0,0); cbRT.anchorMax = new Vector2(1,0);
        cbRT.pivot = new Vector2(0.5f,0f);
        cbRT.sizeDelta = new Vector2(-8,24); cbRT.anchoredPosition = Vector2.zero;

        var cntT = T("Count", cntBg.transform, bold, "0", 17, light, TextAnchor.MiddleCenter);
        Fill(cntT.rectTransform);

        if      (idx == 0) _redCount   = cntT;
        else if (idx == 1) _blueCount  = cntT;
        else               _greenCount = cntT;
    }

    // ── Action Bar ── bottom-right ───────────────────────────────────────────────
    void BuildActionBar(Transform root, Font bold)
    {
        var panel = Panel("ActionBar", root,
            new Vector2(1,0), new Vector2(1,0), new Vector2(1,0),
            new Vector2(340, 94), new Vector2(-14, 14));
        Borders(panel, GoldBdr, 1.5f);

        Color[] slotBg = {
            new Color(0.30f, 0.16f, 0.03f, 0.95f),
            new Color(0.04f, 0.12f, 0.32f, 0.95f),
            new Color(0.22f, 0.04f, 0.04f, 0.95f),
            new Color(0.28f, 0.18f, 0.04f, 0.95f),
        };
        string[] keys = { "1", "2", "3", "4" };

        for (int i = 0; i < 4; i++)
        {
            var slot = I($"Slot{i}", panel, slotBg[i]);
            var sRT = slot.rectTransform;
            sRT.anchorMin = new Vector2(0,0); sRT.anchorMax = new Vector2(0,0);
            sRT.pivot = new Vector2(0,0);
            sRT.sizeDelta = new Vector2(72,72); sRT.anchoredPosition = new Vector2(11 + i * 79f, 11);
            Borders(slot.transform, Gold, 1.5f);

            // Shine on upper half
            var sh = I($"Sh{i}", slot.transform, new Color(1f,1f,1f,0.06f));
            var shRT = sh.rectTransform;
            shRT.anchorMin = new Vector2(0,0.5f); shRT.anchorMax = new Vector2(1,1);
            shRT.offsetMin = Vector2.zero; shRT.offsetMax = Vector2.zero;

            // Key badge bottom-left
            var kb = I($"KB{i}", slot.transform, new Color(0f,0f,0f,0.70f));
            var kbRT = kb.rectTransform;
            kbRT.anchorMin = new Vector2(0,0); kbRT.anchorMax = new Vector2(0,0);
            kbRT.pivot = new Vector2(0,0);
            kbRT.sizeDelta = new Vector2(20,18); kbRT.anchoredPosition = Vector2.zero;
            var kt = T($"KT{i}", kb.transform, bold, keys[i], 13, Gold, TextAnchor.MiddleCenter);
            Fill(kt.rectTransform);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    Image Bar(string id, RectTransform parent, Color fill, Color bg,
        float x, float y, float w, float h, Font font, string def, out Text lbl)
    {
        var track = I($"{id}Track", parent, bg);
        var tRT = track.rectTransform;
        tRT.anchorMin = new Vector2(0,0.5f); tRT.anchorMax = new Vector2(0,0.5f);
        tRT.pivot = new Vector2(0,0.5f);
        tRT.sizeDelta = new Vector2(w,h); tRT.anchoredPosition = new Vector2(x,y);
        Borders(track.transform, GoldBdr, 1f);

        var fillImg = I($"{id}Fill", track.transform, fill);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        Fill(fillImg.rectTransform);

        var shine = I($"{id}Sh", track.transform, new Color(1f,1f,1f,0.07f));
        var sRT = shine.rectTransform;
        sRT.anchorMin = new Vector2(0,0.5f); sRT.anchorMax = new Vector2(1,1);
        sRT.offsetMin = Vector2.zero; sRT.offsetMax = Vector2.zero;

        lbl = T($"{id}Txt", track.transform, font, def, 13, White, TextAnchor.MiddleCenter);
        Fill(lbl.rectTransform);
        return fillImg;
    }

    static RectTransform Panel(string name, Transform root,
        Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 sz, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var bg = go.AddComponent<Image>();
        bg.color = BG; bg.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = piv;
        rt.sizeDelta = sz;   rt.anchoredPosition = pos;
        return rt;
    }

    static RectTransform R(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Image I(string name, RectTransform parent, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = col; img.raycastTarget = false;
        return img;
    }
    static Image I(string name, Transform parent, Color col)
    {
        var rt = parent.GetComponent<RectTransform>() ?? parent.gameObject.AddComponent<RectTransform>();
        return I(name, rt, col);
    }

    static Text T(string name, RectTransform parent, Font font, string text, int size, Color col, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        if (font != null) t.font = font;
        t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        t.raycastTarget = false;
        return t;
    }
    static Text T(string name, Transform parent, Font font, string text, int size, Color col, TextAnchor align)
    {
        var rt = parent.GetComponent<RectTransform>() ?? parent.gameObject.AddComponent<RectTransform>();
        return T(name, rt, font, text, size, col, align);
    }

    static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Borders(Transform parent, Color col, float t)
    {
        void Line(string n, Vector2 aMin, Vector2 aMax, Vector2 piv, bool horiz)
        {
            var ln = I(n, parent, col);
            var rt = ln.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = piv;
            rt.sizeDelta = horiz ? new Vector2(0,t) : new Vector2(t,0);
            rt.anchoredPosition = Vector2.zero;
        }
        Line("BT", new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1), true);
        Line("BB", new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0), true);
        Line("BL", new Vector2(0,0), new Vector2(0,1), new Vector2(0,0.5f), false);
        Line("BR", new Vector2(1,0), new Vector2(1,1), new Vector2(1,0.5f), false);
    }

    static void Stripe(RectTransform parent, bool left, Color col, float w)
    {
        var s = I(left ? "StripL" : "StripR", parent, col);
        var rt = s.rectTransform;
        rt.anchorMin = left ? new Vector2(0,0) : new Vector2(1,0);
        rt.anchorMax = left ? new Vector2(0,1) : new Vector2(1,1);
        rt.pivot = left ? new Vector2(0,0.5f) : new Vector2(1,0.5f);
        rt.sizeDelta = new Vector2(w,0); rt.anchoredPosition = Vector2.zero;
    }
}
