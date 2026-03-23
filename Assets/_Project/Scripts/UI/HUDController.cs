using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the in-game HUD:
///   • Top bar: Gold + Lives display (Inspector-wired, unchanged)
///   • Bottom panel (148px): Portrait | Stats | Cards | Actions
///
/// The bottom panel is built procedurally in Start().
/// After updating this script, delete the old build-buttons container and
/// tower-info panel GameObjects from the scene — they are no longer referenced.
/// Wire _fireTowerData and _waterTowerData in the Inspector.
/// </summary>
public class HUDController : MonoBehaviour
{
    // ── Top HUD (Inspector-wired, unchanged) ─────────────────────────────────
    [Header("Gold")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private Image           _goldIcon;

    [Header("Lives")]
    [SerializeField] private TextMeshProUGUI _livesText;
    [SerializeField] private Image           _heartIcon;

    // ── Tower Data (wire in Inspector) ───────────────────────────────────────
    [Header("Tower Data — Build Buttons")]
    [SerializeField] private TowerData _meleeTowerData;
    [SerializeField] private TowerData _rangeTowerData;
    [SerializeField] private TowerData _fireTowerData;
    [SerializeField] private TowerData _waterTowerData;

    // ── Stat-bar max values (public constants for CardEffect to reference) ───
    public const float MaxDamage      = 50f;
    public const float MaxRange       = 4f;
    public const float MaxAttackSpeed = 5f;
    public const float MaxMoveSpeed   = 8f;

    // ── Layout constants ─────────────────────────────────────────────────────
    private const float PanelH    = 148f;
    private const float PortraitW = 100f;
    private const float CardsW    = 210f;
    private const float ActionsW  = 160f;
    private const int   Slots     = 6;

    // ── Palette ──────────────────────────────────────────────────────────────
    private static readonly Color ColBg       = new Color(0.067f, 0.090f, 0.067f, 0.95f);
    private static readonly Color ColDamage   = new Color(0.886f, 0.294f, 0.290f, 1f);
    private static readonly Color ColRange    = new Color(0.498f, 0.467f, 0.867f, 1f);
    private static readonly Color ColSpeed    = new Color(0.941f, 0.753f, 0.251f, 1f);
    private static readonly Color ColEffect   = new Color(0.114f, 0.620f, 0.459f, 1f);
    private static readonly Color ColBarBg    = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color ColLabel    = new Color(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color ColBorder   = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color ColSlotBg   = new Color(0.10f, 0.10f, 0.10f, 0.6f);
    private static readonly Color ColSlotBdr  = new Color(1f, 1f, 1f, 0.20f);
    private static readonly Color ColBtn      = new Color(0.18f, 0.18f, 0.18f, 0.90f);
    private static readonly Color ColSellBtn  = new Color(0.70f, 0.18f, 0.18f, 0.90f);

    // ── Runtime panel references ─────────────────────────────────────────────
    // Portrait
    private Image           _portImg;
    private TextMeshProUGUI _portName;
    private TextMeshProUGUI _portSub;

    // Stats (4 rows)
    private readonly Image[]           _barFills  = new Image[4];
    private readonly TextMeshProUGUI[] _barLabels = new TextMeshProUGUI[4];
    private readonly TextMeshProUGUI[] _barValues = new TextMeshProUGUI[4];
    private readonly Image[]           _barBgs    = new Image[4];

    // Cards — hero mode
    private GameObject         _heroCardsGO;
    private readonly Image[]   _hInvIco = new Image[Slots];
    private readonly Button[]  _buildBtn  = new Button[4];
    private readonly Image[]   _buildIco  = new Image[4];
    private readonly TextMeshProUGUI[] _buildCst = new TextMeshProUGUI[4];

    // Cards — tower mode
    private GameObject                   _towerCardsGO;
    private readonly Image[]             _tEffIco   = new Image[Slots];
    private readonly TextMeshProUGUI[]   _tEffPlus  = new TextMeshProUGUI[Slots];
    private readonly Image[]             _tEffDot   = new Image[Slots];
    private readonly Button[]            _tInvBtn   = new Button[Slots];
    private readonly Image[]             _tInvIco   = new Image[Slots];

    // Actions — hero
    private GameObject      _heroActGO;

    // Actions — tower
    private GameObject       _towerActGO;
    private Button           _upBtn0;
    private TextMeshProUGUI  _upTxt0;
    private Button           _upBtn1;
    private TextMeshProUGUI  _upTxt1;
    private Button           _sellBtn;
    private TextMeshProUGUI  _sellTxt;
    private TextMeshProUGUI  _sellWarn;

    // State
    private TowerBehaviour _selectedTower;
    private TowerData[]    _bDatas;
    private int[]          _bCosts;

    // ══════════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ══════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        _bDatas = new[] { _meleeTowerData, _rangeTowerData, _fireTowerData, _waterTowerData };
        _bCosts = new int[4];
        for (int i = 0; i < 4; i++) _bCosts[i] = FullCost(i);

        BuildPanel();

        RefreshGold(EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0);
        RefreshLives(LivesManager.Instance != null ? LivesManager.Instance.Lives : 0);
        RefreshAll();
    }

    private void OnEnable()
    {
        EconomyManager.OnGoldChanged        += RefreshGold;
        EconomyManager.OnGoldChanged        += RefreshGoldDependents;
        LivesManager.OnLivesChanged         += RefreshLives;
        SelectionManager.OnSelectionChanged += OnSelection;
        TowerBehaviour.OnTowerSold          += OnSold;
        TowerBehaviour.OnTowerUpgraded      += OnUpgraded;
        TowerBehaviour.OnEffectApplied      += OnApplied;
        PlayerInventory.OnInventoryChanged  += RefreshCards;
    }

    private void OnDisable()
    {
        EconomyManager.OnGoldChanged        -= RefreshGold;
        EconomyManager.OnGoldChanged        -= RefreshGoldDependents;
        LivesManager.OnLivesChanged         -= RefreshLives;
        SelectionManager.OnSelectionChanged -= OnSelection;
        TowerBehaviour.OnTowerSold          -= OnSold;
        TowerBehaviour.OnTowerUpgraded      -= OnUpgraded;
        TowerBehaviour.OnEffectApplied      -= OnApplied;
        PlayerInventory.OnInventoryChanged  -= RefreshCards;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Panel construction
    // ══════════════════════════════════════════════════════════════════════════

    private void BuildPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("[HUD] No parent Canvas."); return; }

        // ── Root ─────────────────────────────────────────────────────────────
        var root = UI("BottomPanel", canvas.transform);
        var rt   = root.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = new Vector2(1, 0);
        rt.pivot            = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0, PanelH);

        root.AddComponent<Image>().color = ColBg;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 0;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        SecPortrait(root.transform);
        SecStats(root.transform);
        SecCards(root.transform);
        SecActions(root.transform);
    }

    // ── Portrait (100px) ─────────────────────────────────────────────────────

    private void SecPortrait(Transform p)
    {
        var root = UI("Portrait", p);
        FW(root, PortraitW);
        root.AddComponent<Image>().color = ColBg;

        // right border line
        var bd = UI("Bdr", root.transform);
        AnchorRT(bd, new Vector2(1, 0.05f), new Vector2(1, 0.95f), new Vector2(1, 0.5f));
        bd.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 0);
        bd.AddComponent<Image>().color = ColBorder;

        // sprite
        var spr = UI("Spr", root.transform);
        AnchorFill(spr, 0.1f, 0.38f, 0.9f, 0.95f);
        _portImg = spr.AddComponent<Image>();
        _portImg.preserveAspect = true;
        _portImg.raycastTarget  = false;

        // name
        _portName = TMP("Name", root.transform, 13, TextAlignmentOptions.Center, Color.white);
        AnchorFill(_portName.gameObject, 0.05f, 0.16f, 0.95f, 0.38f);

        // subtype
        _portSub = TMP("Sub", root.transform, 10, TextAlignmentOptions.Center, ColLabel);
        AnchorFill(_portSub.gameObject, 0.05f, 0.02f, 0.95f, 0.18f);
    }

    // ── Stats (flex) ─────────────────────────────────────────────────────────

    private void SecStats(Transform p)
    {
        var root = UI("Stats", p);
        var le   = root.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.minWidth      = 140;

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 2;
        vlg.padding                = new RectOffset(10, 10, 8, 8);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = true;

        MakeRow(root.transform, 0, "Daño",        ColDamage);
        MakeRow(root.transform, 1, "Rango",       ColRange);
        MakeRow(root.transform, 2, "Vel. ataque", ColSpeed);
        MakeRow(root.transform, 3, "Velocidad",   ColSpeed);
    }

    private void MakeRow(Transform p, int i, string label, Color barCol)
    {
        var row = UI($"R{i}", p);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 4;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // label (70px)
        var lbl = UI("L", row.transform);
        FW(lbl, 70);
        _barLabels[i] = lbl.AddComponent<TextMeshProUGUI>();
        _barLabels[i].text          = label;
        _barLabels[i].fontSize      = 11;
        _barLabels[i].color         = ColLabel;
        _barLabels[i].alignment     = TextAlignmentOptions.MidlineLeft;
        _barLabels[i].raycastTarget = false;

        // bar container (flex) — holds fixed-height bar centered vertically
        var ctr = UI("C", row.transform);
        ctr.AddComponent<LayoutElement>().flexibleWidth = 1;

        var bg = UI("Bg", ctr.transform);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0, 0.25f);
        bgrt.anchorMax = new Vector2(1, 0.75f);
        bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
        _barBgs[i] = bg.AddComponent<Image>();
        _barBgs[i].color         = ColBarBg;
        _barBgs[i].raycastTarget = false;

        var fill = UI("F", bg.transform);
        var frt  = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = new Vector2(0, 1);
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        _barFills[i] = fill.AddComponent<Image>();
        _barFills[i].color         = barCol;
        _barFills[i].raycastTarget = false;

        // value (56px)
        var val = UI("V", row.transform);
        FW(val, 56);
        _barValues[i] = val.AddComponent<TextMeshProUGUI>();
        _barValues[i].fontSize      = 11;
        _barValues[i].color         = Color.white;
        _barValues[i].alignment     = TextAlignmentOptions.MidlineRight;
        _barValues[i].raycastTarget = false;
    }

    // ── Cards (210px) ────────────────────────────────────────────────────────

    private void SecCards(Transform p)
    {
        var root = UI("Cards", p);
        FW(root, CardsW);

        // left border
        var bd = UI("Bdr", root.transform);
        AnchorRT(bd, new Vector2(0, 0.05f), new Vector2(0, 0.95f), new Vector2(0, 0.5f));
        bd.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 0);
        bd.AddComponent<Image>().color = ColBorder;

        // ── Hero mode ────────────────────────────────────────────────────────
        _heroCardsGO = UI("HCards", root.transform);
        Stretch(_heroCardsGO);

        var hv = _heroCardsGO.AddComponent<VerticalLayoutGroup>();
        hv.spacing = 4;  hv.padding = new RectOffset(8, 8, 6, 6);
        hv.childControlWidth = hv.childControlHeight = true;
        hv.childForceExpandWidth = hv.childForceExpandHeight = true;

        // top: inventory
        var hInv = UI("Inv", _heroCardsGO.transform);
        var hig  = hInv.AddComponent<GridLayoutGroup>();
        hig.cellSize        = new Vector2(30, 42);
        hig.spacing         = new Vector2(2, 0);
        hig.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        hig.constraintCount = Slots;
        hig.childAlignment  = TextAnchor.MiddleCenter;

        for (int i = 0; i < Slots; i++)
        {
            var s = UI($"S{i}", hInv.transform);
            s.AddComponent<Image>().color = ColSlotBg;
            var ol = s.AddComponent<Outline>();
            ol.effectColor    = ColSlotBdr;
            ol.effectDistance  = new Vector2(1, 1);

            var ico = UI("I", s.transform);
            Stretch(ico, 2);
            _hInvIco[i] = ico.AddComponent<Image>();
            _hInvIco[i].preserveAspect = true;
            _hInvIco[i].raycastTarget  = false;
            _hInvIco[i].color          = Color.clear;
        }

        // bottom: 2×2 build buttons
        var bRow = UI("Build", _heroCardsGO.transform);
        var bg   = bRow.AddComponent<GridLayoutGroup>();
        bg.cellSize        = new Vector2(92, 26);
        bg.spacing         = new Vector2(4, 3);
        bg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        bg.constraintCount = 2;
        bg.childAlignment  = TextAnchor.MiddleCenter;

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var btn = UI($"B{i}", bRow.transform);
            btn.AddComponent<Image>().color = ColBtn;

            var b = btn.AddComponent<Button>();
            var bc = b.colors;
            bc.normalColor      = Color.white;
            bc.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            bc.pressedColor     = new Color(0.8f, 0.8f, 0.8f);
            bc.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            b.colors = bc;
            _buildBtn[i] = b;

            var bhl = btn.AddComponent<HorizontalLayoutGroup>();
            bhl.spacing            = 3;
            bhl.padding            = new RectOffset(3, 4, 2, 2);
            bhl.childAlignment     = TextAnchor.MiddleCenter;
            bhl.childControlWidth  = false;
            bhl.childControlHeight = true;
            bhl.childForceExpandWidth  = false;
            bhl.childForceExpandHeight = true;

            // icon
            var icoGO = UI("I", btn.transform);
            FW(icoGO, 18);
            _buildIco[i] = icoGO.AddComponent<Image>();
            _buildIco[i].preserveAspect = true;
            _buildIco[i].raycastTarget  = false;

            // cost label
            var cGO = UI("C", btn.transform);
            cGO.AddComponent<LayoutElement>().flexibleWidth = 1;
            _buildCst[i] = cGO.AddComponent<TextMeshProUGUI>();
            _buildCst[i].fontSize      = 10;
            _buildCst[i].color         = Color.white;
            _buildCst[i].alignment     = TextAlignmentOptions.MidlineLeft;
            _buildCst[i].raycastTarget = false;

            if (_bDatas[i] != null)
            {
                _buildIco[i].sprite = TowerIcon(_bDatas[i]);
                _buildCst[i].text   = $"{_bDatas[i].TowerName} {_bCosts[i]}g";
                b.onClick.AddListener(() => ClickBuild(idx));
            }
        }

        // ── Tower mode ───────────────────────────────────────────────────────
        _towerCardsGO = UI("TCards", root.transform);
        Stretch(_towerCardsGO);

        var tv = _towerCardsGO.AddComponent<VerticalLayoutGroup>();
        tv.spacing = 4;  tv.padding = new RectOffset(8, 8, 6, 6);
        tv.childControlWidth = tv.childControlHeight = true;
        tv.childForceExpandWidth = tv.childForceExpandHeight = true;

        // top: applied effects
        var eRow = UI("Eff", _towerCardsGO.transform);
        var eg   = eRow.AddComponent<GridLayoutGroup>();
        eg.cellSize        = new Vector2(30, 42);
        eg.spacing         = new Vector2(2, 0);
        eg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        eg.constraintCount = Slots;
        eg.childAlignment  = TextAnchor.MiddleCenter;

        for (int i = 0; i < Slots; i++)
        {
            var s = UI($"E{i}", eRow.transform);
            s.AddComponent<Image>().color = ColSlotBg;
            var ol = s.AddComponent<Outline>();
            ol.effectColor   = ColSlotBdr;
            ol.effectDistance = new Vector2(1, 1);

            var ico = UI("I", s.transform);
            Stretch(ico, 2);
            _tEffIco[i] = ico.AddComponent<Image>();
            _tEffIco[i].preserveAspect = true;
            _tEffIco[i].raycastTarget  = false;
            _tEffIco[i].color          = Color.clear;

            // rarity dot (top-right corner)
            var dot = UI("D", s.transform);
            AnchorFill(dot, 0.70f, 0.75f, 0.95f, 0.95f);
            _tEffDot[i] = dot.AddComponent<Image>();
            _tEffDot[i].raycastTarget = false;
            _tEffDot[i].color         = Color.clear;

            // "+" placeholder
            _tEffPlus[i] = TMP("+", s.transform, 16, TextAlignmentOptions.Center, ColSlotBdr);
            Stretch(_tEffPlus[i].gameObject);
            _tEffPlus[i].text = "+";
        }

        // bottom: player inventory (clickable)
        var tInv = UI("TInv", _towerCardsGO.transform);
        var tig  = tInv.AddComponent<GridLayoutGroup>();
        tig.cellSize        = new Vector2(30, 42);
        tig.spacing         = new Vector2(2, 0);
        tig.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        tig.constraintCount = Slots;
        tig.childAlignment  = TextAnchor.MiddleCenter;

        for (int i = 0; i < Slots; i++)
        {
            int idx = i;
            var s = UI($"TI{i}", tInv.transform);
            s.AddComponent<Image>().color = ColSlotBg;
            var ol = s.AddComponent<Outline>();
            ol.effectColor   = ColSlotBdr;
            ol.effectDistance = new Vector2(1, 1);

            _tInvBtn[i] = s.AddComponent<Button>();
            var bc = _tInvBtn[i].colors;
            bc.normalColor   = Color.white;
            bc.disabledColor = new Color(1, 1, 1, 0.35f);
            _tInvBtn[i].colors = bc;
            _tInvBtn[i].onClick.AddListener(() => ClickTowerInv(idx));

            var ico = UI("I", s.transform);
            Stretch(ico, 2);
            _tInvIco[i] = ico.AddComponent<Image>();
            _tInvIco[i].preserveAspect = true;
            _tInvIco[i].raycastTarget  = false;
            _tInvIco[i].color          = Color.clear;
        }

        _towerCardsGO.SetActive(false);
    }

    // ── Actions (160px) ──────────────────────────────────────────────────────

    private void SecActions(Transform p)
    {
        var root = UI("Actions", p);
        FW(root, ActionsW);

        // left border
        var bd = UI("Bdr", root.transform);
        AnchorRT(bd, new Vector2(0, 0.05f), new Vector2(0, 0.95f), new Vector2(0, 0.5f));
        bd.GetComponent<RectTransform>().sizeDelta = new Vector2(1, 0);
        bd.AddComponent<Image>().color = ColBorder;

        // ── Hero ─────────────────────────────────────────────────────────────
        _heroActGO = UI("HAct", root.transform);
        Stretch(_heroActGO);
        var msg = TMP("Msg", _heroActGO.transform, 11, TextAlignmentOptions.Center, ColLabel);
        Stretch(msg.gameObject, 12);
        msg.text = "Seleccioná una torre\npara ver sus opciones";

        // ── Tower ────────────────────────────────────────────────────────────
        _towerActGO = UI("TAct", root.transform);
        Stretch(_towerActGO);

        var vlg = _towerActGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(10, 10, 10, 6);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // upgrade button 0
        _upBtn0 = MakeBtn("Up0", _towerActGO.transform, ColBtn, 30);
        _upTxt0 = _upBtn0.GetComponentInChildren<TextMeshProUGUI>();
        _upBtn0.onClick.AddListener(() => _selectedTower?.TryUpgrade(0));

        // upgrade button 1
        _upBtn1 = MakeBtn("Up1", _towerActGO.transform, ColBtn, 30);
        _upTxt1 = _upBtn1.GetComponentInChildren<TextMeshProUGUI>();
        _upBtn1.onClick.AddListener(() => _selectedTower?.TryUpgrade(1));

        // sell button
        _sellBtn = MakeBtn("Sell", _towerActGO.transform, ColSellBtn, 30);
        _sellTxt = _sellBtn.GetComponentInChildren<TextMeshProUGUI>();
        _sellBtn.onClick.AddListener(() => _selectedTower?.Sell());

        // warning
        var wGO = UI("W", _towerActGO.transform);
        wGO.AddComponent<LayoutElement>().preferredHeight = 24;
        _sellWarn = wGO.AddComponent<TextMeshProUGUI>();
        _sellWarn.text          = "Cartas aplicadas se\npierden al vender.";
        _sellWarn.fontSize      = 9;
        _sellWarn.color         = new Color(0.85f, 0.55f, 0.30f, 0.75f);
        _sellWarn.alignment     = TextAlignmentOptions.Top;
        _sellWarn.raycastTarget = false;

        _towerActGO.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Refresh
    // ══════════════════════════════════════════════════════════════════════════

    private void RefreshAll()
    {
        bool tower = _selectedTower != null;
        RefreshPortrait(tower);
        RefreshStats(tower);
        RefreshCards();
        RefreshActions(tower);

        if (_heroCardsGO  != null) _heroCardsGO.SetActive(!tower);
        if (_towerCardsGO != null) _towerCardsGO.SetActive(tower);
        if (_heroActGO    != null) _heroActGO.SetActive(!tower);
        if (_towerActGO   != null) _towerActGO.SetActive(tower);
    }

    private void RefreshPortrait(bool tower)
    {
        if (tower && _selectedTower != null)
        {
            Sprite ico = TowerIcon(_selectedTower.Data);
            _portImg.sprite = ico;
            _portImg.color  = ico != null ? Color.white : Color.clear;
            _portName.text  = _selectedTower.Data.TowerName;
            _portSub.text   = _selectedTower.Data.Type.ToString();
        }
        else
        {
            Sprite heroSpr = null;
            if (HeroBehaviour.Instance != null)
            {
                var sr = HeroBehaviour.Instance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) heroSpr = sr.sprite;
            }
            _portImg.sprite = heroSpr;
            _portImg.color  = heroSpr != null ? Color.white : Color.clear;
            _portName.text  = "Héroe";
            _portSub.text   = "Mago";
        }
    }

    private void RefreshStats(bool tower)
    {
        if (tower && _selectedTower != null)
        {
            TowerData d = _selectedTower.Data;

            FillBar(0, d.DamageBase, MaxDamage, $"{d.DamageBase:0.#}");
            FillBar(1, d.Range, MaxRange, $"{d.Range / GridManager.CellSize:0.#} celdas");

            if (d.IsAreaAttack)
                FillBar(2, d.DamageBase * 0.5f, MaxAttackSpeed, "Continuo");
            else
                FillBar(2, d.AttackSpeed, MaxAttackSpeed, $"{d.AttackSpeed:0.#}/s");

            // 4th row: effect text, no bar
            string eff = FmtEffects(d);
            _barBgs[3].color    = Color.clear;
            _barFills[3].color  = Color.clear;
            _barLabels[3].text  = "Efecto";
            _barLabels[3].color = ColEffect;
            _barValues[3].text  = eff;
            _barValues[3].color = ColEffect;
        }
        else if (HeroBehaviour.Instance != null)
        {
            var h = HeroBehaviour.Instance;

            FillBar(0, h.Damage,             MaxDamage,      $"{h.Damage:0.#}");
            FillBar(1, h.AttackRange,        MaxRange,       $"{h.AttackRange / GridManager.CellSize:0.#} celdas");
            FillBar(2, 1f / h.AttackInterval, MaxAttackSpeed, $"{1f / h.AttackInterval:0.#}/s");
            FillBar(3, h.MoveSpeed,          MaxMoveSpeed,   $"{h.MoveSpeed:0.#}");

            // restore 4th row appearance
            _barLabels[3].text  = "Velocidad";
            _barLabels[3].color = ColLabel;
            _barValues[3].color = Color.white;
            _barBgs[3].color    = ColBarBg;
            _barFills[3].color  = ColSpeed;
        }
    }

    private void RefreshCards()
    {
        bool tower = _selectedTower != null;
        if (_heroCardsGO  != null) _heroCardsGO.SetActive(!tower);
        if (_towerCardsGO != null) _towerCardsGO.SetActive(tower);

        IReadOnlyList<CardData> inv = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.Cards : null;
        int invCount = inv?.Count ?? 0;

        if (!tower)
        {
            // hero mode — show inventory, refresh build buttons
            for (int i = 0; i < Slots; i++)
            {
                bool has = i < invCount && inv[i] != null;
                _hInvIco[i].sprite = has ? inv[i].Icon : null;
                _hInvIco[i].color  = has && inv[i].Icon != null ? Color.white : Color.clear;
            }

            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;
            for (int i = 0; i < 4; i++)
                if (_buildBtn[i] != null)
                    _buildBtn[i].interactable = _bDatas[i] != null && gold >= _bCosts[i];
        }
        else
        {
            // tower mode — applied effects + clickable inventory
            var applied = _selectedTower.AppliedEffects;
            int appCnt  = applied?.Count ?? 0;

            for (int i = 0; i < Slots; i++)
            {
                bool has = i < appCnt && applied[i] != null;
                _tEffIco[i].sprite = has ? applied[i].Icon : null;
                _tEffIco[i].color  = has && applied[i].Icon != null ? Color.white : Color.clear;
                _tEffPlus[i].gameObject.SetActive(!has);
                _tEffDot[i].color = has ? RarityCol(applied[i].CardRarity) : Color.clear;
            }

            TowerType tt   = _selectedTower.Data.Type;
            bool      full = appCnt >= TowerBehaviour.MaxAppliedCards;

            for (int i = 0; i < Slots; i++)
            {
                bool has = i < invCount && inv[i] != null;
                _tInvIco[i].sprite = has ? inv[i].Icon : null;
                _tInvIco[i].color  = has && inv[i].Icon != null ? Color.white : Color.clear;
                _tInvBtn[i].interactable = has && !full && inv[i].IsCompatibleWith(tt);
            }
        }
    }

    private void RefreshActions(bool tower)
    {
        if (_heroActGO  != null) _heroActGO.SetActive(!tower);
        if (_towerActGO != null) _towerActGO.SetActive(tower);

        if (!tower || _selectedTower == null) return;

        TowerData d = _selectedTower.Data;
        TowerData[] paths = d.UpgradePaths;
        bool has0 = paths != null && paths.Length > 0 && paths[0] != null;
        bool has1 = paths != null && paths.Length > 1 && paths[1] != null;
        int  gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;

        _upBtn0.gameObject.SetActive(has0);
        _upBtn1.gameObject.SetActive(has1);

        if (has0)
        {
            _upTxt0.text          = $"{paths[0].TowerName} (+{paths[0].Cost}g)";
            _upBtn0.interactable  = gold >= paths[0].Cost;
        }
        if (has1)
        {
            _upTxt1.text          = $"{paths[1].TowerName} (+{paths[1].Cost}g)";
            _upBtn1.interactable  = gold >= paths[1].Cost;
        }

        _sellTxt.text = $"Vender ({_selectedTower.SellValue}g)";

        // Show sell warning only if tower has applied cards
        bool hasCards = _selectedTower.AppliedEffects != null && _selectedTower.AppliedEffects.Count > 0;
        _sellWarn.gameObject.SetActive(hasCards);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Event handlers
    // ══════════════════════════════════════════════════════════════════════════

    private void OnSelection(ISelectable prev, ISelectable curr)
    {
        _selectedTower = curr as TowerBehaviour;
        RefreshAll();
    }

    private void OnSold(TowerBehaviour t, int refund)
    {
        if (_selectedTower == t) { _selectedTower = null; RefreshAll(); }
    }

    private void OnUpgraded(TowerBehaviour t)
    {
        if (_selectedTower == t) RefreshAll();
    }

    private void OnApplied(TowerBehaviour t)
    {
        if (_selectedTower == t) RefreshCards();
    }

    private void RefreshGold(int gold)
    {
        if (_goldText != null) _goldText.text = gold.ToString();
    }

    private void RefreshLives(int lives)
    {
        if (_livesText != null) _livesText.text = lives.ToString();
    }

    private void RefreshGoldDependents(int gold)
    {
        // build buttons
        for (int i = 0; i < 4; i++)
            if (_buildBtn[i] != null)
                _buildBtn[i].interactable = _bDatas[i] != null && gold >= _bCosts[i];

        // upgrade buttons
        if (_selectedTower != null)
        {
            var paths = _selectedTower.Data.UpgradePaths;
            if (paths != null && paths.Length > 0 && paths[0] != null)
                _upBtn0.interactable = gold >= paths[0].Cost;
            if (paths != null && paths.Length > 1 && paths[1] != null)
                _upBtn1.interactable = gold >= paths[1].Cost;
        }
    }

    // ── Button clicks ────────────────────────────────────────────────────────

    private void ClickBuild(int idx)
    {
        if (idx < 0 || idx >= _bDatas.Length || _bDatas[idx] == null) return;
        TowerPlacementManager.Instance?.SelectTower(_bDatas[idx]);
    }

    private void ClickTowerInv(int slot)
    {
        if (_selectedTower == null) return;
        var inv = PlayerInventory.Instance;
        if (inv == null) return;
        if (slot < 0 || slot >= inv.Cards.Count) return;
        inv.SpendCard(inv.Cards[slot], _selectedTower);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════════════════

    private void FillBar(int i, float val, float max, string text)
    {
        var rt = _barFills[i].GetComponent<RectTransform>();
        rt.anchorMax       = new Vector2(Mathf.Clamp01(val / max), 1);
        _barValues[i].text = text;
    }

    private int FullCost(int idx)
    {
        if (_bDatas[idx] == null) return 0;
        // Fuego/Agua: Rango base + upgrade delta
        if ((idx == 2 || idx == 3) && _rangeTowerData != null)
            return _rangeTowerData.Cost + _bDatas[idx].Cost;
        return _bDatas[idx].Cost;
    }

    private static string FmtEffects(TowerData d)
    {
        if (d.OnHitEffects == null || d.OnHitEffects.Length == 0) return "\u2014";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < d.OnHitEffects.Length; i++)
        {
            if (i > 0) sb.Append(" + ");
            var e = d.OnHitEffects[i];
            switch (e.Type)
            {
                case EffectType.Burn:
                    sb.Append($"Burn {e.Value:0.#} dps"); break;
                case EffectType.Slow:
                case EffectType.SlowArea:
                    sb.Append($"Slow -{e.Value * 100:0}%"); break;
                case EffectType.ArmorReduction:
                    sb.Append($"Arm -{e.Value * 100:0}%"); break;
            }
        }
        return sb.ToString();
    }

    private static Color RarityCol(CardData.Rarity r)
    {
        switch (r)
        {
            case CardData.Rarity.Common: return new Color(0.70f, 0.70f, 0.70f);
            case CardData.Rarity.Rare:   return new Color(0.33f, 0.60f, 1.00f);
            case CardData.Rarity.Epic:   return new Color(0.80f, 0.40f, 1.00f);
            default: return Color.white;
        }
    }

    private static Sprite TowerIcon(TowerData d)
    {
        if (d == null) return null;
        if (d.Icon != null) return d.Icon;
        if (d.TowerPrefab != null)
        {
            var sr = d.TowerPrefab.GetComponent<SpriteRenderer>();
            if (sr != null) return sr.sprite;
        }
        return null;
    }

    // ── UI creation shortcuts ────────────────────────────────────────────────

    private static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI TMP(string name, Transform parent,
        float size, TextAlignmentOptions align, Color color)
    {
        var go  = UI(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize      = size;
        tmp.alignment     = align;
        tmp.color         = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void FW(GameObject go, float w)
    {
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.minWidth       = w;
    }

    private static void AnchorRT(GameObject go, Vector2 min, Vector2 max, Vector2 pivot)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot     = pivot;
    }

    private static void AnchorFill(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Stretch(GameObject go, float inset = 0)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static Button MakeBtn(string name, Transform parent, Color bgCol, float h)
    {
        var go = UI(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = h;
        go.AddComponent<Image>().color = bgCol;

        var btn = go.AddComponent<Button>();
        var c   = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        c.pressedColor     = new Color(0.8f, 0.8f, 0.8f);
        c.disabledColor    = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        btn.colors = c;

        var txt = TMP("T", go.transform, 11, TextAlignmentOptions.Center, Color.white);
        Stretch(txt.gameObject, 4);

        return btn;
    }
}
