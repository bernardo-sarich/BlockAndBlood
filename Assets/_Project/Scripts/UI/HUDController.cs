using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Right-side panel HUD (200 px wide, full height).
/// Camera.main.rect is adjusted in Awake so the game area excludes the panel strip.
/// The top HUD (gold, lives) is repositioned to top-left of the game area.
/// </summary>
public class HUDController : MonoBehaviour
{
    // ── Inspector-wired (top-HUD elements already in scene) ──────────────
    [Header("Top HUD — Gold")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private Image           _goldIcon;

    [Header("Top HUD — Lives")]
    [SerializeField] private TextMeshProUGUI _livesText;
    [SerializeField] private Image           _heartIcon;

    [Header("Tower Data")]
    [SerializeField] private TowerData _meleeTowerData;
    [SerializeField] private TowerData _rangeTowerData;

    // ── Public constants (CardEffect may reference) ───────────────────────
    public const float MaxDamage      = 50f;
    public const float MaxRange       = 8f;  // cells
    public const float MaxAttackSpeed = 5f;
    public const float MaxMoveSpeed   = 12f; // cells/sec

    // ── Layout ───────────────────────────────────────────────────────────
    private const float PanelW      = 200f;
    private const float PortraitH   = 70f;
    private const int   Slots       = 6;
    private const int   InvColumns  = 3;

    // ── Palette ──────────────────────────────────────────────────────────
    private static readonly Color ColBg      = new Color(0.086f, 0.110f, 0.086f, 0.97f); // #161C16
    private static readonly Color ColBgDark  = new Color(0.067f, 0.090f, 0.067f, 1f);    // #111711
    private static readonly Color ColDamage  = new Color(0.886f, 0.294f, 0.290f, 1f);
    private static readonly Color ColRange   = new Color(0.498f, 0.467f, 0.867f, 1f);
    private static readonly Color ColSpeed   = new Color(0.941f, 0.753f, 0.251f, 1f);
    private static readonly Color ColEffect  = new Color(0.114f, 0.620f, 0.459f, 1f);
    private static readonly Color ColBarBg   = new Color(0.15f,  0.15f,  0.15f,  1f);
    private static readonly Color ColLabel   = new Color(0.478f, 0.604f, 0.478f, 1f);    // #7A9A7A
    private static readonly Color ColXpText  = new Color(0.478f, 0.722f, 0.478f, 1f);    // #7AB87A — XP / Level display
    private static readonly Color ColBorder  = new Color(1f,     1f,     1f,     0.10f);
    private static readonly Color ColSlotBg  = new Color(0.10f,  0.10f,  0.10f,  0.70f);
    private static readonly Color ColSlotBdr = new Color(0.165f, 0.290f, 0.165f, 1f);    // #2A4A2A
    private static readonly Color ColBtn     = new Color(0.18f,  0.18f,  0.18f,  0.90f);
    private static readonly Color ColSellBtn = new Color(0.65f,  0.15f,  0.15f,  0.90f);
    private static readonly Color ColTitle   = new Color(0.416f, 0.667f, 0.416f, 1f);    // #6AAA6A
    private static readonly Color ColDivider = new Color(0.118f, 0.180f, 0.118f, 1f);    // #1E2E1E

    // Sub-palette: portrait
    private static readonly Color ColPortName = new Color(0.910f, 0.961f, 0.910f, 1f);   // #E8F5E8
    private static readonly Color ColPortSub  = new Color(0.114f, 0.620f, 0.459f, 1f);   // #1D9E75

    // Sub-palette: stat values
    private static readonly Color ColStatVal  = new Color(0.784f, 0.902f, 0.788f, 1f);   // #C8E6C9
    private static readonly Color ColNoEffect = new Color(0.290f, 0.416f, 0.290f, 1f);   // #4A6A4A
    private static readonly Color ColActiveEff= new Color(0.886f, 0.565f, 0.353f, 1f);   // #E2905A

    // Sub-palette: upgrade buttons
    private static readonly Color ColUpBg     = new Color(0.102f, 0.180f, 0.125f, 0.90f);// #1A2E20
    private static readonly Color ColUpBdr    = new Color(0.165f, 0.353f, 0.220f, 1f);    // #2A5A38
    private static readonly Color ColUpTxt    = new Color(0.416f, 0.800f, 0.541f, 1f);    // #6ACC8A
    private static readonly Color ColUpCost   = new Color(0.941f, 0.753f, 0.251f, 1f);    // #F0C040

    // Sub-palette: sell button
    private static readonly Color ColSellBg   = new Color(0.118f, 0.118f, 0.118f, 0.90f);// #1E1E1E
    private static readonly Color ColSellBdr  = new Color(0.227f, 0.227f, 0.227f, 1f);   // #3A3A3A
    private static readonly Color ColSellTxt  = new Color(0.533f, 0.533f, 0.533f, 1f);   // #888888
    private static readonly Color ColSellRef  = new Color(0.416f, 0.541f, 0.416f, 1f);   // #6A8A6A
    private static readonly Color ColWarnTxt  = new Color(0.353f, 0.290f, 0.227f, 1f);   // #5A4A3A

    // ── Runtime refs ─────────────────────────────────────────────────────
    private Image           _portImg;
    private TextMeshProUGUI _portName;
    private TextMeshProUGUI _portSub;

    private readonly Image[]           _barFills  = new Image[4];
    private readonly Image[]           _barBgs    = new Image[4];
    private readonly TextMeshProUGUI[] _barLabels = new TextMeshProUGUI[4];
    private readonly TextMeshProUGUI[] _barValues = new TextMeshProUGUI[4];

    private GameObject         _heroCardsGO;
    private TextMeshProUGUI    _hInvTitle;
    private readonly Image[]           _hInvIco     = new Image[Slots];
    private readonly TextMeshProUGUI[] _hInvPlus    = new TextMeshProUGUI[Slots];
    private readonly TextMeshProUGUI[] _hInvCharges = new TextMeshProUGUI[Slots];

    private GameObject                 _towerCardsGO;
    private TextMeshProUGUI            _tCardsTitle;
    private readonly Image[]           _tEffIco     = new Image[Slots];
    private readonly TextMeshProUGUI[] _tEffPlus    = new TextMeshProUGUI[Slots];
    private readonly Image[]           _tEffDot     = new Image[Slots];
    private readonly Button[]          _tInvBtn     = new Button[Slots];
    private readonly Image[]           _tInvIco     = new Image[Slots];
    private readonly TextMeshProUGUI[] _tInvCharges = new TextMeshProUGUI[Slots];

    private GameObject       _buildSec;
    private readonly Button[]          _buildBtn = new Button[4];
    private readonly Image[]           _buildIco = new Image[4];
    private readonly TextMeshProUGUI[] _buildCst = new TextMeshProUGUI[4];

    private GameObject       _actionsSec;
    private Button           _upBtn0;
    private TextMeshProUGUI  _upTxt0;
    private Button           _upBtn1;
    private TextMeshProUGUI  _upTxt1;
    private Button           _sellBtn;
    private TextMeshProUGUI  _sellTxt;
    private TextMeshProUGUI  _sellWarn;

    private RectTransform  _panelRT;
    private TowerBehaviour _selectedTower;
    private TowerData[]    _bDatas;
    private int            _lastScreenW;

    // ── XP bar (bottom of screen) ─────────────────────────────────────────
    private Image           _xpBarFill;
    private TextMeshProUGUI _xpBarLabel;

    // ── Game timer (bottom-right) ─────────────────────────────────────────
    private TextMeshProUGUI _timerLabel;
    private float           _playingStartTime  = -1f;
    private float           _totalPausedTime   = 0f;
    private float           _pauseStartTime    = -1f;
    private bool            _timerRunning      = false;

    // ══════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _bDatas = new[] { _meleeTowerData, _rangeTowerData };
        RepositionTopHUD();
        BuildXpHUD();
        BuildTimerHUD();
        BuildRightPanel();
    }

    private void Start()
    {
        // ApplyCameraViewport runs in Start so GridManager.Awake has already
        // positioned the camera and set perspective FOV/distance.
        ApplyCameraViewport();
        RefreshGold(EconomyManager.Instance  != null ? EconomyManager.Instance.Gold   : 0);
        RefreshLives(LivesManager.Instance   != null ? LivesManager.Instance.Lives    : 0);
        RefreshXp(XPManager.Instance         != null ? XPManager.Instance.CurrentXp   : 0);
        RefreshAll();
    }

    private void OnEnable()
    {
        EconomyManager.OnGoldChanged        += RefreshGold;
        EconomyManager.OnGoldChanged        += RefreshGoldDependents;
        LivesManager.OnLivesChanged         += RefreshLives;
        SelectionManager.OnSelectionChanged += OnSelectionChanged;
        TowerBehaviour.OnTowerSold          += OnTowerSold;
        TowerBehaviour.OnTowerUpgraded      += OnTowerUpgraded;
        TowerBehaviour.OnEffectApplied      += OnEffectApplied;
        PlayerInventory.OnInventoryChanged  += RefreshCardSections;
        XPManager.OnXpChanged               += RefreshXp;
        XPManager.OnLevelUp                 += RefreshLevel;
        EnemyBehaviour.OnEnemyDeath         += OnEnemyDied;
        GameManager.OnGameStateChanged      += OnGameStateChangedTimer;
    }

    private void OnDisable()
    {
        EconomyManager.OnGoldChanged        -= RefreshGold;
        EconomyManager.OnGoldChanged        -= RefreshGoldDependents;
        LivesManager.OnLivesChanged         -= RefreshLives;
        SelectionManager.OnSelectionChanged -= OnSelectionChanged;
        TowerBehaviour.OnTowerSold          -= OnTowerSold;
        TowerBehaviour.OnTowerUpgraded      -= OnTowerUpgraded;
        TowerBehaviour.OnEffectApplied      -= OnEffectApplied;
        PlayerInventory.OnInventoryChanged  -= RefreshCardSections;
        XPManager.OnXpChanged               -= RefreshXp;
        XPManager.OnLevelUp                 -= RefreshLevel;
        EnemyBehaviour.OnEnemyDeath         -= OnEnemyDied;
        GameManager.OnGameStateChanged      -= OnGameStateChangedTimer;
    }

    private void Update()
    {
        if (Screen.width != _lastScreenW) ApplyCameraViewport();
        if (_timerRunning && _timerLabel != null)
        {
            float elapsed = Time.time - _playingStartTime - _totalPausedTime;
            int   mins    = (int)(elapsed / 60f);
            int   secs    = (int)(elapsed % 60f);
            _timerLabel.text = $"{mins}:{secs:D2}";
        }
    }

    // ── Camera & top HUD ─────────────────────────────────────────────────

    private void ApplyCameraViewport()
    {
        _lastScreenW = Screen.width;
        if (Camera.main == null) return;
        Camera.main.rect = new Rect(0f, 0f, 1f, 1f);

        if (GridManager.Instance != null)
            GridManager.Instance.CenterCamera();
    }

    private void RepositionTopHUD()
    {
        GameObject goldPanel  = _goldText  != null ? _goldText.transform.parent.gameObject  : null;
        GameObject livesPanel = _livesText != null ? _livesText.transform.parent.gameObject : null;
        SetTopItem(goldPanel,  6f,  6f, 0f, 24f);
        SetTopItem(livesPanel, 90f, 6f, 0f, 24f);

        SetupIconTextRow(goldPanel,  _goldIcon,  _goldText);
        SetupIconTextRow(livesPanel, _heartIcon, _livesText);
    }

    /// <summary>
    /// Re-parents icon as first child of panel and adds a HorizontalLayoutGroup
    /// so icon → text render left-to-right without overlap.
    /// </summary>
    private static void SetupIconTextRow(GameObject panel, Image icon, TextMeshProUGUI txt)
    {
        if (panel == null || icon == null || txt == null) return;

        // Ensure order: icon first, text second
        icon.transform.SetParent(panel.transform, false);
        icon.transform.SetSiblingIndex(0);
        txt.transform.SetSiblingIndex(1);

        // Text must never wrap regardless of digit count
        txt.enableWordWrapping = false;
        txt.overflowMode       = TextOverflowModes.Overflow;

        // Fixed-size LayoutElement on the icon
        float size             = txt.fontSize;
        var iconLE             = icon.GetComponent<LayoutElement>() ?? icon.gameObject.AddComponent<LayoutElement>();
        iconLE.minWidth        = size;
        iconLE.preferredWidth  = size;
        iconLE.minHeight       = size;
        iconLE.preferredHeight = size;
        iconLE.flexibleWidth   = 0;

        // Panel must not have a fixed preferred width
        var panelLE = panel.GetComponent<LayoutElement>();
        if (panelLE != null) panelLE.preferredWidth = -1;

        // HorizontalLayoutGroup on the panel
        var hlg                    = panel.GetComponent<HorizontalLayoutGroup>() ?? panel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 4f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.padding                = new RectOffset(2, 2, 0, 0);

        // Let the panel auto-size horizontally to fit icon + text
        var csf = panel.GetComponent<ContentSizeFitter>() ?? panel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;
    }

    private static void SetTopItem(GameObject go, float x, float y, float w, float h)
    {
        if (go == null) return;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(0, 1);
        rt.pivot            = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Panel construction
    // ══════════════════════════════════════════════════════════════════════

    private void BuildRightPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("[HUD] No Canvas parent."); return; }

        // Destroy any old BottomPanel if it survived
        var old = canvas.transform.Find("BottomPanel");
        if (old != null) Destroy(old.gameObject);

        // Root
        var root = UI("RightPanel", canvas.transform);
        var rt   = root.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(PanelW, 0f);
        _panelRT = rt;

        root.AddComponent<Image>().color = ColBg;

        var csf = root.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Left-edge separator line (positioned absolutely, excluded from VLG)
        var sep = UI("LeftBorder", root.transform);
        var sepLE = sep.AddComponent<LayoutElement>();
        sepLE.ignoreLayout = true;
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0, 0);
        sepRT.anchorMax = new Vector2(0, 1);
        sepRT.pivot     = new Vector2(0, 0.5f);
        sepRT.offsetMin = sepRT.offsetMax = Vector2.zero;
        sepRT.sizeDelta = new Vector2(1, 0);
        sep.AddComponent<Image>().color = ColDivider;

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 0;
        vlg.padding                = new RectOffset(0, 0, 0, 0);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperLeft;

        BuildPortrait(root.transform);
        AddHDivider(root.transform);
        BuildStats(root.transform);
        AddHDivider(root.transform);
        BuildHeroCards(root.transform);
        BuildTowerCards(root.transform);
        AddHDivider(root.transform);
        BuildBuildSec(root.transform);
        BuildActionsSec(root.transform);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRT);
    }

    // ── Portrait ─────────────────────────────────────────────────────────

    private void BuildPortrait(Transform p)
    {
        var sec = UI("Portrait", p);
        FH(sec, PortraitH);
        sec.AddComponent<Image>().color = ColBgDark;

        var hlg = sec.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 8;
        hlg.padding                = new RectOffset(8, 8, 10, 10);
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Icon 44×44
        var icoGO = UI("Icon", sec.transform);
        var icoLE = icoGO.AddComponent<LayoutElement>();
        icoLE.minWidth = icoLE.preferredWidth = 44;
        _portImg = icoGO.AddComponent<Image>();
        _portImg.preserveAspect = true;
        _portImg.raycastTarget  = false;

        // Text column
        var col = UI("Info", sec.transform);
        col.AddComponent<LayoutElement>().flexibleWidth = 1;
        var cv = col.AddComponent<VerticalLayoutGroup>();
        cv.spacing = 2;
        cv.childControlWidth      = true;
        cv.childControlHeight     = true;
        cv.childForceExpandWidth  = true;
        cv.childForceExpandHeight = false;

        _portName = TMP("Name", col.transform, 14, TextAlignmentOptions.Left, ColPortName);
        _portName.fontStyle = FontStyles.Bold;
        _portName.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

        _portSub = TMP("Sub", col.transform, 9, TextAlignmentOptions.Left, ColPortSub);
        _portSub.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        _portSub.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;
    }

    // ── Stats ─────────────────────────────────────────────────────────────

    private void BuildStats(Transform p)
    {
        var sec = UI("Stats", p);
        // title 14 + 4 rows × 22 + spacing 3×4 + pad 5+5 = 14+88+12+10 = 124
        FH(sec, 124);

        var vlg = sec.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 3;
        vlg.padding                = new RectOffset(8, 8, 5, 5);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        MakeSectionTitle(sec.transform, "STATS");

        BuildStatRow(sec.transform, 0, "Dano",       ColDamage);
        BuildStatRow(sec.transform, 1, "Rango",      ColRange);
        BuildStatRow(sec.transform, 2, "Vel.atq",    ColSpeed);
        BuildStatRow(sec.transform, 3, "Velocidad",  ColSpeed);
    }

    private void BuildStatRow(Transform p, int i, string label, Color barCol)
    {
        var row = UI($"SR{i}", p);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 22;
        rowLE.flexibleHeight  = 0;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 3;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // label 52px
        var lbl = UI("L", row.transform);
        FW(lbl, 52);
        _barLabels[i] = lbl.AddComponent<TextMeshProUGUI>();
        _barLabels[i].text               = label;
        _barLabels[i].fontSize           = 10;
        _barLabels[i].color              = ColLabel;
        _barLabels[i].fontStyle          = FontStyles.Normal;
        _barLabels[i].alignment          = TextAlignmentOptions.MidlineLeft;
        _barLabels[i].raycastTarget      = false;
        _barLabels[i].enableWordWrapping = false;

        // bar container 56px fixed
        var barCtr = UI("Bar", row.transform);
        FW(barCtr, 56);

        var bgGO = UI("Bg", barCtr.transform);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.2f);
        bgRT.anchorMax = new Vector2(1, 0.8f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        _barBgs[i] = bgGO.AddComponent<Image>();
        _barBgs[i].color = ColBarBg;
        _barBgs[i].raycastTarget = false;

        var fillGO = UI("Fill", bgGO.transform);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        _barFills[i] = fillGO.AddComponent<Image>();
        _barFills[i].color = barCol;
        _barFills[i].raycastTarget = false;

        // value — remaining width
        var val = UI("V", row.transform);
        val.AddComponent<LayoutElement>().flexibleWidth = 1;
        _barValues[i] = val.AddComponent<TextMeshProUGUI>();
        _barValues[i].fontSize           = 11;
        _barValues[i].fontStyle          = FontStyles.Bold;
        _barValues[i].color              = ColStatVal;
        _barValues[i].alignment          = TextAlignmentOptions.MidlineRight;
        _barValues[i].raycastTarget      = false;
        _barValues[i].enableWordWrapping = false;
    }

    // ── Hero inventory (direct VLG child, hero only) ──────────────────────

    private void BuildHeroCards(Transform p)
    {
        _heroCardsGO = UI("HeroCards", p);
        // title 14 + 2 rows of 56 + spacing 6 + padding 6+4 = ~142
        FH(_heroCardsGO, 148);

        var vlg = _heroCardsGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4;
        vlg.padding                = new RectOffset(8, 8, 6, 4);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var hTitleRow = MakeSectionTitle(_heroCardsGO.transform, "INVENTARIO (0/6)");
        _hInvTitle = hTitleRow;

        var grid = UI("Grid", _heroCardsGO.transform);
        var gg   = grid.AddComponent<GridLayoutGroup>();
        gg.cellSize        = new Vector2(56, 56);
        gg.spacing         = new Vector2(4, 4);
        gg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gg.constraintCount = InvColumns;
        gg.childAlignment  = TextAnchor.UpperLeft;
        grid.AddComponent<LayoutElement>().preferredHeight = 118; // 2 rows × 56 + spacing 6

        for (int i = 0; i < Slots; i++)
        {
            var s = UI($"S{i}", grid.transform);
            s.AddComponent<Image>().color = ColSlotBg;
            AddOutline(s, ColSlotBdr);

            var ico = UI("I", s.transform);
            Stretch(ico, 2);
            _hInvIco[i] = ico.AddComponent<Image>();
            _hInvIco[i].preserveAspect = true;
            _hInvIco[i].raycastTarget  = false;
            _hInvIco[i].color          = Color.clear;

            _hInvPlus[i] = TMP("+", s.transform, 10, TextAlignmentOptions.Center, ColSlotBdr);
            Stretch(_hInvPlus[i].gameObject);
            _hInvPlus[i].text = "+";

            // Charge count badge — bottom-right corner
            var badge = UI("Ch", s.transform);
            AnchorFill(badge, 0.45f, 0f, 1f, 0.38f);
            _hInvCharges[i] = badge.AddComponent<TextMeshProUGUI>();
            _hInvCharges[i].fontSize   = 9f;
            _hInvCharges[i].fontStyle  = FontStyles.Bold;
            _hInvCharges[i].alignment  = TextAlignmentOptions.BottomRight;
            _hInvCharges[i].color      = ColSpeed; // amarillo dorado
            _hInvCharges[i].raycastTarget = false;
            _hInvCharges[i].text       = "";
        }
    }

    // ── Tower cards (direct VLG child, tower only) ────────────────────────

    private void BuildTowerCards(Transform p)
    {
        _towerCardsGO = UI("TowerCards", p);
        // title 14 + eff-grid 32 + apply-label 14 + inv-grid 118 + spacing+padding ≈ 200
        FH(_towerCardsGO, 200);

        var vlg = _towerCardsGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4;
        vlg.padding                = new RectOffset(8, 8, 6, 4);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Applied effects title
        _tCardsTitle = MakeSectionTitle(_towerCardsGO.transform, "EFECTOS APLICADOS (0/6)");

        // Applied effects grid
        var eGrid = UI("EffGrid", _towerCardsGO.transform);
        var eg    = eGrid.AddComponent<GridLayoutGroup>();
        eg.cellSize        = new Vector2(26, 32);
        eg.spacing         = new Vector2(3, 0);
        eg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        eg.constraintCount = Slots;
        eg.childAlignment  = TextAnchor.UpperLeft;
        eGrid.AddComponent<LayoutElement>().preferredHeight = 32;

        for (int i = 0; i < Slots; i++)
        {
            var s = UI($"E{i}", eGrid.transform);
            s.AddComponent<Image>().color = ColSlotBg;
            AddOutline(s, ColSlotBdr);

            var ico = UI("I", s.transform);
            Stretch(ico, 2);
            _tEffIco[i] = ico.AddComponent<Image>();
            _tEffIco[i].preserveAspect = true;
            _tEffIco[i].raycastTarget  = false;
            _tEffIco[i].color          = Color.clear;

            var dot = UI("D", s.transform);
            AnchorFill(dot, 0.55f, 0f, 1f, 0.35f);
            _tEffDot[i] = dot.AddComponent<Image>();
            _tEffDot[i].raycastTarget = false;
            _tEffDot[i].color = Color.clear;

            _tEffPlus[i] = TMP("+", s.transform, 10, TextAlignmentOptions.Center, ColSlotBdr);
            Stretch(_tEffPlus[i].gameObject);
            _tEffPlus[i].text = "+";
        }

        // "Apply card" subtitle
        MakeSectionTitle(_towerCardsGO.transform, "APLICAR CARTA");

        // Inventory (clickable) — 3 columns with charge badges
        var tGrid = UI("InvGrid", _towerCardsGO.transform);
        var tg    = tGrid.AddComponent<GridLayoutGroup>();
        tg.cellSize        = new Vector2(56, 56);
        tg.spacing         = new Vector2(4, 4);
        tg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        tg.constraintCount = InvColumns;
        tg.childAlignment  = TextAnchor.UpperLeft;
        tGrid.AddComponent<LayoutElement>().preferredHeight = 118; // 2 rows × 56 + spacing 6

        for (int i = 0; i < Slots; i++)
        {
            int idx = i;
            var s = UI($"TI{i}", tGrid.transform);
            s.AddComponent<Image>().color = ColSlotBg;
            AddOutline(s, ColSlotBdr);

            _tInvBtn[i] = s.AddComponent<Button>();
            var bc = _tInvBtn[i].colors;
            bc.normalColor   = Color.white;
            bc.disabledColor = new Color(1, 1, 1, 0.3f);
            _tInvBtn[i].colors = bc;
            _tInvBtn[i].onClick.AddListener(() => ClickTowerInv(idx));

            var ico = UI("I", s.transform);
            Stretch(ico, 2);
            _tInvIco[i] = ico.AddComponent<Image>();
            _tInvIco[i].preserveAspect = true;
            _tInvIco[i].raycastTarget  = false;
            _tInvIco[i].color          = Color.clear;

            // Charge count badge — bottom-right corner
            var badge = UI("Ch", s.transform);
            AnchorFill(badge, 0.45f, 0f, 1f, 0.38f);
            _tInvCharges[i] = badge.AddComponent<TextMeshProUGUI>();
            _tInvCharges[i].fontSize      = 9f;
            _tInvCharges[i].fontStyle     = FontStyles.Bold;
            _tInvCharges[i].alignment     = TextAlignmentOptions.BottomRight;
            _tInvCharges[i].color         = ColSpeed; // amarillo dorado
            _tInvCharges[i].raycastTarget = false;
            _tInvCharges[i].text          = "";
        }

        _towerCardsGO.SetActive(false);
    }

    // ── Build section (hero only) ─────────────────────────────────────────

    private void BuildBuildSec(Transform p)
    {
        _buildSec = UI("Build", p);

        var vlg = _buildSec.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4;
        vlg.padding                = new RectOffset(8, 8, 6, 6);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        MakeSectionTitle(_buildSec.transform, "CONSTRUIR TORRE");

        // Count valid tower data to size grid dynamically
        int validCount = 0;
        for (int i = 0; i < _bDatas.Length; i++) if (_bDatas[i] != null) validCount++;
        int rows = Mathf.CeilToInt(validCount / 2f);
        int gridH = rows > 0 ? rows * 48 + (rows - 1) * 4 : 0;

        var grid = UI("Grid", _buildSec.transform);
        var gg   = grid.AddComponent<GridLayoutGroup>();
        gg.cellSize        = new Vector2(84, 48);
        gg.spacing         = new Vector2(4, 4);
        gg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gg.constraintCount = 2;
        gg.childAlignment  = TextAnchor.UpperLeft;
        grid.AddComponent<LayoutElement>().preferredHeight = gridH;

        // Section height: title 14 + spacing 4 + grid + padding 12
        FH(_buildSec, 14 + 4 + gridH + 12);

        for (int i = 0; i < _bDatas.Length; i++)
        {
            if (_bDatas[i] == null) continue; // skip unassigned tower data

            int idx = i;
            var btn = UI($"B{i}", grid.transform);
            btn.AddComponent<Image>().color = ColBtn;

            _buildBtn[i] = btn.AddComponent<Button>();
            var bc = _buildBtn[i].colors;
            bc.normalColor      = Color.white;
            bc.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            bc.pressedColor     = new Color(0.8f, 0.8f, 0.8f);
            bc.disabledColor    = new Color(1f, 1f, 1f, 0.35f);
            _buildBtn[i].colors = bc;

            var bvl = btn.AddComponent<VerticalLayoutGroup>();
            bvl.spacing                = 1;
            bvl.padding                = new RectOffset(2, 2, 2, 2);
            bvl.childAlignment         = TextAnchor.UpperCenter;
            bvl.childControlWidth      = true;
            bvl.childControlHeight     = true;
            bvl.childForceExpandWidth  = true;
            bvl.childForceExpandHeight = false;

            var icoGO = UI("I", btn.transform);
            var icoLE = icoGO.AddComponent<LayoutElement>();
            icoLE.preferredHeight = 24;
            icoLE.preferredWidth  = 24;
            _buildIco[i] = icoGO.AddComponent<Image>();
            _buildIco[i].preserveAspect = true;
            _buildIco[i].raycastTarget  = false;
            _buildIco[i].color          = Color.clear;

            var nameGO = UI("N", btn.transform);
            nameGO.AddComponent<LayoutElement>().preferredHeight = 10;
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize           = 8;
            nameTmp.color              = Color.white;
            nameTmp.alignment          = TextAlignmentOptions.Center;
            nameTmp.raycastTarget      = false;
            nameTmp.enableWordWrapping = false;

            var costGO = UI("C", btn.transform);
            costGO.AddComponent<LayoutElement>().preferredHeight = 10;
            _buildCst[i] = costGO.AddComponent<TextMeshProUGUI>();
            _buildCst[i].fontSize           = 8;
            _buildCst[i].color              = ColUpCost;
            _buildCst[i].alignment          = TextAlignmentOptions.Center;
            _buildCst[i].raycastTarget      = false;
            _buildCst[i].enableWordWrapping = false;

            if (_bDatas[i] != null)
            {
                _buildIco[i].sprite = TowerIcon(_bDatas[i]);
                _buildIco[i].color  = _buildIco[i].sprite != null ? Color.white : Color.clear;
                nameTmp.text        = _bDatas[i].TowerName;
                _buildCst[i].text   = $"{BuildCost(i)}g";
                _buildBtn[i].onClick.AddListener(() => ClickBuild(idx));
            }
        }
    }

    // ── Actions section (tower only) ──────────────────────────────────────

    private void BuildActionsSec(Transform p)
    {
        _actionsSec = UI("Actions", p);
        // title 14 + 2 upgrade btns × 28 + divider 1 + sell btn 28 + warn 24 + spacing + padding ≈ 160
        FH(_actionsSec, 160);

        var vlg = _actionsSec.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = 4;
        vlg.padding                = new RectOffset(8, 8, 6, 6);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // "MEJORAR" section
        MakeSectionTitle(_actionsSec.transform, "MEJORAR");

        _upBtn0 = MakeStyledBtn("Up0", _actionsSec.transform, ColUpBg, ColUpBdr, 28);
        _upTxt0 = _upBtn0.GetComponentInChildren<TextMeshProUGUI>();
        _upTxt0.color = ColUpTxt;
        _upBtn0.onClick.AddListener(() => _selectedTower?.TryUpgrade(0));

        _upBtn1 = MakeStyledBtn("Up1", _actionsSec.transform, ColUpBg, ColUpBdr, 28);
        _upTxt1 = _upBtn1.GetComponentInChildren<TextMeshProUGUI>();
        _upTxt1.color = ColUpTxt;
        _upBtn1.onClick.AddListener(() => _selectedTower?.TryUpgrade(1));

        // "VENDER" section
        MakeSectionTitle(_actionsSec.transform, "VENDER");

        _sellBtn = MakeStyledBtn("Sell", _actionsSec.transform, ColSellBg, ColSellBdr, 28);
        _sellTxt = _sellBtn.GetComponentInChildren<TextMeshProUGUI>();
        _sellTxt.color = ColSellTxt;
        _sellBtn.onClick.AddListener(() => _selectedTower?.Sell());

        var wGO = UI("Warn", _actionsSec.transform);
        wGO.AddComponent<LayoutElement>().preferredHeight = 24;
        _sellWarn = wGO.AddComponent<TextMeshProUGUI>();
        _sellWarn.text          = "Las cartas aplicadas se\npierden al vender.";
        _sellWarn.fontSize      = 8;
        _sellWarn.fontStyle     = FontStyles.Italic;
        _sellWarn.color         = ColWarnTxt;
        _sellWarn.alignment     = TextAlignmentOptions.TopLeft;
        _sellWarn.raycastTarget = false;

        _actionsSec.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Refresh
    // ══════════════════════════════════════════════════════════════════════

    private void RefreshAll()
    {
        bool tower = _selectedTower != null;

        RefreshPortrait(tower);
        RefreshStats(tower);
        RefreshCardSections();
        RefreshActions(tower);

        if (_buildSec   != null) _buildSec.SetActive(!tower);
        if (_actionsSec != null) _actionsSec.SetActive(tower);
    }

    private void RefreshPortrait(bool tower)
    {
        if (_portImg == null) return; // panel not built yet

        if (tower && _selectedTower != null)
        {
            Sprite ico = TowerIcon(_selectedTower.Data);
            _portImg.sprite = ico;
            _portImg.color  = ico != null ? Color.white : Color.clear;
            _portName.text  = _selectedTower.Data.TowerName;
            _portSub.text   = FormatTowerSubtype(_selectedTower.Data);
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
            _portName.text  = "Heroe";
            _portSub.text   = "MAGO";
        }
    }

    private void RefreshStats(bool tower)
    {
        if (tower && _selectedTower != null)
        {
            TowerData d = _selectedTower.Data;

            FillBar(0, d.DamageBase, MaxDamage, $"{d.DamageBase:0.#}");
            FillBar(1, d.Range, MaxRange, $"{d.Range:0.#}c");

            if (d.IsAreaAttack)
                FillBar(2, d.DamageBase * 0.5f, MaxAttackSpeed, "AOE");
            else
                FillBar(2, d.AttackSpeed, MaxAttackSpeed, $"{d.AttackSpeed:0.#}/s");

            string eff = FmtEffects(d);
            bool hasEff = d.OnHitEffects != null && d.OnHitEffects.Length > 0;
            _barBgs[3].color    = Color.clear;
            _barFills[3].color  = Color.clear;
            _barLabels[3].text  = "Efecto";
            _barLabels[3].color = ColLabel;
            if (hasEff)
            {
                _barValues[3].text      = eff;
                _barValues[3].color     = ColActiveEff;
                _barValues[3].fontStyle = FontStyles.Bold;
            }
            else
            {
                _barValues[3].text      = "ninguno";
                _barValues[3].color     = ColNoEffect;
                _barValues[3].fontStyle = FontStyles.Italic;
            }
        }
        else if (HeroBehaviour.Instance != null)
        {
            var h = HeroBehaviour.Instance;

            FillBar(0, h.Damage,              MaxDamage,      $"{h.Damage:0.#}");
            FillBar(1, h.AttackRange / GridManager.CellSize, MaxRange, $"{h.AttackRange / GridManager.CellSize:0.#}c");
            FillBar(2, 1f / h.AttackInterval, MaxAttackSpeed, $"{1f / h.AttackInterval:0.#}/s");
            FillBar(3, h.MoveSpeed,           MaxMoveSpeed,   $"{h.MoveSpeed:0.#}");

            _barLabels[3].text      = "Velocidad";
            _barLabels[3].color     = ColLabel;
            _barValues[3].color     = ColStatVal;
            _barValues[3].fontStyle = FontStyles.Bold;
            _barBgs[3].color        = ColBarBg;
            _barFills[3].color      = ColSpeed;
        }
    }

    private void RefreshCardSections()
    {
        bool tower = _selectedTower != null;
        if (_heroCardsGO  != null) _heroCardsGO.SetActive(!tower);
        if (_towerCardsGO != null) _towerCardsGO.SetActive(tower);

        IReadOnlyList<CardSlot> inv = PlayerInventory.Instance?.Slots;
        int invCount = inv?.Count ?? 0;

        if (!tower)
        {
            if (_hInvTitle != null)
                _hInvTitle.text = $"INVENTARIO ({invCount}/6)";

            for (int i = 0; i < Slots; i++)
            {
                bool   has = i < invCount && inv[i].IsValid;
                Sprite ico = has ? inv[i].Card.DisplayIcon : null;
                _hInvIco[i].sprite = ico;
                _hInvIco[i].color  = ico != null ? Color.white : Color.clear;
                _hInvPlus[i].gameObject.SetActive(!has);
                if (_hInvCharges[i] != null)
                    _hInvCharges[i].text = has ? $"x{inv[i].Charges}" : "";
            }

            int gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;
            for (int i = 0; i < _bDatas.Length; i++)
                if (_buildBtn[i] != null)
                    _buildBtn[i].interactable = _bDatas[i] != null && gold >= BuildCost(i);
        }
        else
        {
            var applied = _selectedTower.AppliedEffects;
            int appCnt  = applied?.Count ?? 0;

            if (_tCardsTitle != null)
                _tCardsTitle.text = $"EFECTOS APLICADOS ({appCnt}/6)";

            for (int i = 0; i < Slots; i++)
            {
                bool has = i < appCnt && applied[i] != null;
                Sprite effIco = has ? applied[i].DisplayIcon : null;
                _tEffIco[i].sprite = effIco;
                _tEffIco[i].color  = effIco != null ? Color.white : Color.clear;
                _tEffPlus[i].gameObject.SetActive(!has);
                _tEffDot[i].color = has ? RarityCol(applied[i].CardRarity) : Color.clear;
            }

            TowerType tt   = _selectedTower.Data.Type;
            bool      full = appCnt >= TowerBehaviour.MaxAppliedCards;

            for (int i = 0; i < Slots; i++)
            {
                bool   has    = i < invCount && inv != null && inv[i].IsValid;
                Sprite invIco = has ? inv[i].Card.DisplayIcon : null;
                _tInvIco[i].sprite = invIco;
                _tInvIco[i].color  = invIco != null ? Color.white : Color.clear;
                _tInvBtn[i].interactable = has && !full && inv[i].Card.IsCompatibleWith(tt);
                if (_tInvCharges[i] != null)
                    _tInvCharges[i].text = has ? $"x{inv[i].Charges}" : "";
            }
        }
    }

    private void RefreshActions(bool tower)
    {
        if (_actionsSec == null) return;
        if (!tower || _selectedTower == null) { _actionsSec.SetActive(false); return; }

        _actionsSec.SetActive(true);

        TowerData   d     = _selectedTower.Data;
        TowerData[] paths = d.UpgradePaths;
        bool has0 = paths != null && paths.Length > 0 && paths[0] != null;
        bool has1 = paths != null && paths.Length > 1 && paths[1] != null;
        int  gold = EconomyManager.Instance != null ? EconomyManager.Instance.Gold : 0;

        _upBtn0.gameObject.SetActive(has0);
        _upBtn1.gameObject.SetActive(has1);

        if (has0)
        {
            _upTxt0.text = $"{paths[0].TowerName} <color=#{ColorUtility.ToHtmlStringRGB(ColUpCost)}>+{paths[0].Cost}g</color>";
            _upBtn0.interactable = gold >= paths[0].Cost;
        }
        if (has1)
        {
            _upTxt1.text = $"{paths[1].TowerName} <color=#{ColorUtility.ToHtmlStringRGB(ColUpCost)}>+{paths[1].Cost}g</color>";
            _upBtn1.interactable = gold >= paths[1].Cost;
        }

        _sellTxt.text = $"Vender <color=#{ColorUtility.ToHtmlStringRGB(ColSellRef)}>({_selectedTower.SellValue}g)</color>";

        bool hasCards = _selectedTower.AppliedEffects != null && _selectedTower.AppliedEffects.Count > 0;
        _sellWarn.gameObject.SetActive(hasCards);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Event handlers
    // ══════════════════════════════════════════════════════════════════════

    private void OnSelectionChanged(ISelectable prev, ISelectable curr)
    {
        _selectedTower = curr as TowerBehaviour;
        RefreshAll();
        if (_panelRT != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRT);
    }

    private void OnTowerSold(TowerBehaviour t, int refund)
    {
        if (_selectedTower == t) { _selectedTower = null; RefreshAll(); }
    }

    private void OnTowerUpgraded(TowerBehaviour t)
    {
        if (_selectedTower == t) RefreshAll();
    }

    private void OnEffectApplied(TowerBehaviour t)
    {
        if (_selectedTower == t) RefreshCardSections();
    }

    private void RefreshGold(int gold)
    {
        if (_goldText != null) _goldText.text = gold.ToString();
    }

    private void RefreshLives(int lives)
    {
        if (_livesText != null) _livesText.text = lives.ToString();
    }

    private void BuildXpHUD()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Destroy stale bar if rebuilding
        var oldBar = canvas.transform.Find("XPBar");
        if (oldBar != null) Destroy(oldBar.gameObject);

        // ── Background ────────────────────────────────────────────────────────
        var barGO              = new GameObject("XPBar");
        barGO.transform.SetParent(canvas.transform, false);
        var barRT              = barGO.AddComponent<RectTransform>();
        barRT.anchorMin        = new Vector2(0.05f, 0f);
        barRT.anchorMax        = new Vector2(0.95f, 0f);
        barRT.pivot            = new Vector2(0.5f,  0f);
        barRT.anchoredPosition = new Vector2(0f, 20f);
        barRT.sizeDelta        = new Vector2(0f, 10f);
        barGO.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 120f / 255f);

        // ── Fill (anchor-based width: anchorMax.x = fillAmount) ───────────────
        var fillGO              = new GameObject("XPFill");
        fillGO.transform.SetParent(barGO.transform, false);
        var fillRT              = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin        = new Vector2(0f, 0f);
        fillRT.anchorMax        = new Vector2(0f, 1f); // starts empty; x updated by RefreshXp
        fillRT.sizeDelta        = Vector2.zero;
        fillRT.anchoredPosition = Vector2.zero;
        _xpBarFill              = fillGO.AddComponent<Image>();
        _xpBarFill.color        = new Color(0.784f, 0.659f, 0.251f, 1f); // #C8A840
        Debug.Log($"[XPBar Init] fillImage={_xpBarFill.name}, " +
                  $"type={_xpBarFill.type}, " +
                  $"parent={_xpBarFill.transform.parent.name}");

        // ── Label ─────────────────────────────────────────────────────────────
        var lblGO          = new GameObject("XpLabel");
        lblGO.transform.SetParent(barGO.transform, false);
        var lblRT          = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin    = Vector2.zero;
        lblRT.anchorMax    = Vector2.one;
        lblRT.offsetMin    = Vector2.zero;
        lblRT.offsetMax    = Vector2.zero;
        _xpBarLabel              = lblGO.AddComponent<TextMeshProUGUI>();
        _xpBarLabel.fontSize     = 11f;
        _xpBarLabel.color        = Color.white;
        _xpBarLabel.alignment    = TextAlignmentOptions.Center;
        _xpBarLabel.raycastTarget = false;
        _xpBarLabel.text         = "0 / 100";
    }

    private void OnEnemyDied(EnemyBehaviour e, int gold, int xp)
    {
        RefreshXp(XPManager.Instance?.CurrentXp ?? 0);
    }

    private void RefreshXp(int xp)
    {
        if (_xpBarFill == null || _xpBarLabel == null) return;
        int lvl = XPManager.Instance?.CurrentLevel ?? 0;
        if (lvl >= XPManager.MaxLevel)
        {
            _xpBarFill.rectTransform.anchorMax = new Vector2(1f, 1f);
            _xpBarLabel.text                   = "MAX";
            Debug.Log($"[XPBar] fill=1 (MAX), xp={xp}");
            return;
        }
        float xpRelativa   = Mathf.Max(0f, xp - lvl * XPManager.XpPerLevel);
        float fillAmount   = Mathf.Clamp01(xpRelativa / XPManager.XpPerLevel);
        _xpBarFill.rectTransform.anchorMax = new Vector2(fillAmount, 1f);
        _xpBarLabel.text   = $"{(int)xpRelativa} / {XPManager.XpPerLevel}";
        Debug.Log($"[XPBar] fill={fillAmount:F2}, xp={xp}, lvl={lvl}, xpRel={xpRelativa}");
    }

    private void RefreshLevel()
    {
        RefreshXp(XPManager.Instance?.CurrentXp ?? 0);
    }

    private void BuildTimerHUD()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var go          = new GameObject("GameTimer");
        go.transform.SetParent(canvas.transform, false);
        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = new Vector2(1f, 0f);
        rt.anchorMax    = new Vector2(1f, 0f);
        rt.pivot        = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-10f, 35f);
        rt.sizeDelta    = new Vector2(80f, 20f);

        _timerLabel                  = go.AddComponent<TextMeshProUGUI>();
        _timerLabel.fontSize         = 13f;
        _timerLabel.color            = new Color(0.8f, 0.8f, 0.8f, 0.85f);
        _timerLabel.alignment        = TextAlignmentOptions.BottomRight;
        _timerLabel.raycastTarget    = false;
        _timerLabel.enableWordWrapping = false;
        _timerLabel.text             = "0:00";
    }

    private void OnGameStateChangedTimer(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Playing:
                if (_playingStartTime < 0f)
                    _playingStartTime = Time.time;
                else if (_pauseStartTime >= 0f)
                {
                    _totalPausedTime += Time.time - _pauseStartTime;
                    _pauseStartTime   = -1f;
                }
                _timerRunning = true;
                break;

            case GameManager.GameState.Paused:
                _pauseStartTime = Time.time;
                _timerRunning   = false;
                break;

            case GameManager.GameState.Defeat:
            case GameManager.GameState.Victory:
                _timerRunning = false;
                break;
        }
    }

    private void RefreshGoldDependents(int gold)
    {
        for (int i = 0; i < _bDatas.Length; i++)
            if (_buildBtn[i] != null)
                _buildBtn[i].interactable = _bDatas[i] != null && gold >= BuildCost(i);

        if (_selectedTower != null)
        {
            var paths = _selectedTower.Data.UpgradePaths;
            if (paths != null && paths.Length > 0 && paths[0] != null)
                _upBtn0.interactable = gold >= paths[0].Cost;
            if (paths != null && paths.Length > 1 && paths[1] != null)
                _upBtn1.interactable = gold >= paths[1].Cost;
        }
    }

    // ── Button handlers ───────────────────────────────────────────────────

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
        inv.SpendCard(slot, _selectedTower);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════════════

    private void FillBar(int i, float val, float max, string text)
    {
        var rt = _barFills[i].GetComponent<RectTransform>();
        rt.anchorMax       = new Vector2(Mathf.Clamp01(val / max), 1);
        _barValues[i].text = text;
    }

    private int BuildCost(int idx)
    {
        if (_bDatas == null || idx < 0 || idx >= _bDatas.Length || _bDatas[idx] == null) return 0;
        return _bDatas[idx].Cost;
    }

    private static string FmtEffects(TowerData d)
    {
        if (d.OnHitEffects == null || d.OnHitEffects.Length == 0) return "\u2014";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < d.OnHitEffects.Length; i++)
        {
            if (i > 0) sb.Append('+');
            var e = d.OnHitEffects[i];
            switch (e.Type)
            {
                case EffectType.Burn:           sb.Append($"Burn {e.Value:0.#}dps");        break;
                case EffectType.Slow:
                case EffectType.SlowArea:       sb.Append($"Slow -{e.Value * 100:0}%");     break;
                case EffectType.ArmorReduction: sb.Append($"Arm -{e.Value * 100:0}%");      break;
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

    // ── UI helpers ────────────────────────────────────────────────────────

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

    /// <summary>Sets a fixed height on a GO that is a direct child of a VLG with childControlHeight=false.</summary>
    private static void FH(GameObject go, float h)
    {
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, h);
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight       = h;
        le.flexibleHeight  = 0;
    }

    private static void FW(GameObject go, float w)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.minWidth       = w;
    }

    private static void Stretch(GameObject go, float inset = 0)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void AnchorFill(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void AddOutline(GameObject go, Color color)
    {
        var ol = go.AddComponent<Outline>();
        ol.effectColor    = color;
        ol.effectDistance = new Vector2(1, 1);
    }

    /// <summary>Builds an HLG row: bold title text + flexible 1px horizontal line.</summary>
    private static TextMeshProUGUI MakeSectionTitle(Transform parent, string text)
    {
        var row = UI("SecTitle", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 14;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 6;
        hlg.padding                = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        var tmp = TMP("Lbl", row.transform, 9, TextAlignmentOptions.MidlineLeft, ColTitle);
        tmp.text      = text;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;

        var lineGO = UI("Line", row.transform);
        lineGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        var lineImg = lineGO.AddComponent<Image>();
        lineImg.color = new Color(0.165f, 0.227f, 0.165f, 1f); // #2A3A2A
        lineImg.raycastTarget = false;
        // Constrain to 1px via RectTransform anchorMin/anchorMax centered vertically
        var lineRT = lineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0f, 0.4f);
        lineRT.anchorMax = new Vector2(1f, 0.6f);
        lineRT.offsetMin = lineRT.offsetMax = Vector2.zero;

        return tmp;
    }

    /// <summary>Creates a button with custom bg color and outline border.</summary>
    private static Button MakeStyledBtn(string name, Transform parent, Color bgCol, Color borderCol, float h)
    {
        var go = UI(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = h;
        go.AddComponent<Image>().color = bgCol;
        AddOutline(go, borderCol);

        var btn = go.AddComponent<Button>();
        var c   = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        c.pressedColor     = new Color(0.8f, 0.8f, 0.8f);
        c.disabledColor    = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        btn.colors = c;

        var txt = TMP("T", go.transform, 10, TextAlignmentOptions.Center, Color.white);
        Stretch(txt.gameObject, 3);
        txt.enableWordWrapping = false;
        txt.richText = true;

        return btn;
    }

    private static string FormatTowerSubtype(TowerData d)
    {
        if (d == null) return "";
        switch (d.Type)
        {
            case TowerType.Melee: return "MELEE \u00b7 LV " + (d.UpgradePaths != null && d.UpgradePaths.Length > 0 ? "1" : "2");
            case TowerType.Range: return "RANGO \u00b7 LV 1";
            default:              return d.Type.ToString().ToUpper();
        }
    }

    private static void AddHDivider(Transform p)
    {
        var go = UI("Div", p);
        FH(go, 1);
        go.AddComponent<Image>().color = ColDivider;
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

        var txt = TMP("T", go.transform, 9, TextAlignmentOptions.Center, Color.white);
        Stretch(txt.gameObject, 3);
        txt.enableWordWrapping = false;

        return btn;
    }
}
