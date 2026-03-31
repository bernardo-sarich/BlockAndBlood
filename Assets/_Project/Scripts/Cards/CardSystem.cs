using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages card acquisition at each XP level-up.
/// Listens to XPManager.OnLevelUp, presents 3 random cards based on current level rarity,
/// adds the chosen card to PlayerInventory, then fires OnCardChosen to resume Playing state.
/// </summary>
public class CardSystem : MonoBehaviour
{
    public static CardSystem Instance { get; private set; }

    /// <summary>True while the card picker is visible. Other systems check this to block input.</summary>
    public static bool IsPickerActive { get; private set; }

    /// <summary>Fired when the player picks a card. Listened by GameManager to resume Playing.</summary>
    public static event Action OnCardChosen;

    // ── Layout constants ────────────────────────────────────────────────────
    private const int   OffersCount   = 3;
    private const float PanelW        = 580f;
    private const float PanelH        = 300f;
    private const float CardW         = 160f;
    private const float CardH         = 180f;
    private const float CardSpacing   = 12f;
    private const float IconAnimFps   = 8f;

    // ── Named colors (no magic hex in logic) ────────────────────────────────
    private static readonly Color ColOverlay  = new Color(0.00f, 0.00f, 0.00f, 0.78f);
    private static readonly Color ColPanel    = new Color(0.07f, 0.10f, 0.07f, 0.97f);
    private static readonly Color ColTitle    = new Color(0.91f, 0.96f, 0.91f, 1.00f);
    private static readonly Color ColCardBg   = new Color(0.11f, 0.16f, 0.11f, 0.97f);
    private static readonly Color ColCardBdr  = new Color(0.20f, 0.35f, 0.20f, 1.00f);
    private static readonly Color ColCardHov  = new Color(0.18f, 0.28f, 0.18f, 0.97f);
    private static readonly Color ColCardName = new Color(0.91f, 0.96f, 0.91f, 1.00f);
    private static readonly Color ColCommon   = new Color(0.90f, 0.90f, 0.90f, 1.00f); // blanco
    private static readonly Color ColRare     = new Color(1.00f, 0.85f, 0.20f, 1.00f); // amarillo
    private static readonly Color ColEpic     = new Color(0.75f, 0.40f, 1.00f, 1.00f); // violeta

    // ── Runtime ─────────────────────────────────────────────────────────────
    private GameObject _pickerCanvas;
    private readonly CardData[] _offers = new CardData[OffersCount];

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => XPManager.OnLevelUp += HandleLevelUp;
    private void OnDisable() => XPManager.OnLevelUp -= HandleLevelUp;

    // ── Event handler ────────────────────────────────────────────────────────

    private void HandleLevelUp()
    {
        if (_pickerCanvas != null) return; // already showing (shouldn't happen, but guard)

        int level = XPManager.Instance != null ? XPManager.Instance.CurrentLevel : 0;
        GenerateOffers();
        ShowPicker(level);
    }

    // ── Card generation ──────────────────────────────────────────────────────

    private void GenerateOffers()
    {
        CardData[] pool = Resources.LoadAll<CardData>("Cards");
        var used = new HashSet<int>();

        for (int i = 0; i < OffersCount; i++)
        {
            CardRarity rarity = XPManager.GetRarityForCurrentLevel();
            _offers[i] = PickCard(pool, rarity, used);
        }
    }

    /// <summary>
    /// Picks a card from the pool matching the requested rarity.
    /// Avoids repeating cards within the same offer set when possible.
    /// Falls back to placeholder if Resources/Cards is empty.
    /// </summary>
    private static CardData PickCard(CardData[] pool, CardRarity rarity, HashSet<int> used)
    {
        if (pool == null || pool.Length == 0)
            return MakePlaceholderCard(rarity, used.Count + 1);

        CardData.Rarity target = MapRarity(rarity);

        // Prefer matching rarity, not yet used in this offer set
        var candidates = new List<int>();
        for (int i = 0; i < pool.Length; i++)
            if (pool[i].CardRarity == target && !used.Contains(i))
                candidates.Add(i);

        // Fallback: any card not yet used
        if (candidates.Count == 0)
            for (int i = 0; i < pool.Length; i++)
                if (!used.Contains(i)) candidates.Add(i);

        // Fallback: allow repeats
        if (candidates.Count == 0)
            for (int i = 0; i < pool.Length; i++)
                candidates.Add(i);

        int chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        used.Add(chosen);
        return pool[chosen];
    }

    private static CardData MakePlaceholderCard(CardRarity rarity, int index)
    {
        var card         = ScriptableObject.CreateInstance<CardData>();
        card.CardName    = $"Carta {rarity} {index}";
        card.Description = $"Efecto de rareza {rarity} (placeholder).";
        card.CardRarity  = MapRarity(rarity);
        card.MaxCharges  = 1;
        return card;
    }

    private static CardData.Rarity MapRarity(CardRarity r) => r switch
    {
        CardRarity.Rare  => CardData.Rarity.Rare,
        CardRarity.Epic  => CardData.Rarity.Epic,
        _                => CardData.Rarity.Common,
    };

    // ── Card picker UI ───────────────────────────────────────────────────────

    private void ShowPicker(int level)
    {
        IsPickerActive  = true;
        Cursor.visible  = true;
        Cursor.lockState = CursorLockMode.Confined;

        _pickerCanvas = new GameObject("CardPickerCanvas");
        var canvas    = _pickerCanvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        _pickerCanvas.AddComponent<CanvasScaler>();
        _pickerCanvas.AddComponent<GraphicRaycaster>();

        // Full-screen dark overlay that absorbs stray clicks
        var overlay = MakeGO("Overlay", _pickerCanvas.transform);
        Stretch(overlay);
        overlay.AddComponent<Image>().color = ColOverlay;
        overlay.AddComponent<Button>(); // blocks clicks to the game underneath

        // Centered panel
        var panel    = MakeGO("Panel", _pickerCanvas.transform);
        var panelRT  = Center(panel, PanelW, PanelH);
        panel.AddComponent<Image>().color = ColPanel;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 16;
        vlg.padding               = new RectOffset(20, 20, 20, 20);
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment        = TextAnchor.UpperCenter;

        // Title row
        var titleGO = MakeGO("Title", panel.transform);
        titleGO.AddComponent<LayoutElement>().preferredHeight = 36;
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text             = $"¡Subiste al nivel {level}!  Elegí una carta";
        titleTMP.fontSize         = 20f;
        titleTMP.alignment        = TextAlignmentOptions.Center;
        titleTMP.color            = ColTitle;
        titleTMP.fontStyle        = FontStyles.Bold;

        // Cards row
        var row   = MakeGO("CardRow", panel.transform);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = CardH;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = CardSpacing;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment        = TextAnchor.MiddleCenter;

        for (int i = 0; i < OffersCount; i++)
            BuildCardButton(row.transform, i);

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRT);
    }

    private void BuildCardButton(Transform parent, int idx)
    {
        CardData card = _offers[idx];

        var go = MakeGO($"Card{idx}", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = CardW;
        le.preferredHeight = CardH;

        go.AddComponent<Image>().color = ColCardBg;

        var outline = go.AddComponent<Outline>();
        outline.effectColor    = ColCardBdr;
        outline.effectDistance = new Vector2(1f, -1f);

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = ColCardBg;
        colors.highlightedColor = ColCardHov;
        colors.pressedColor     = ColCardHov;
        btn.colors = colors;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 6;
        vlg.padding               = new RectOffset(10, 10, 14, 10);
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment        = TextAnchor.UpperCenter;

        // Icon
        Sprite firstFrame = (card.IconFrames != null && card.IconFrames.Length > 0)
            ? card.IconFrames[0] : card.Icon;
        if (firstFrame != null)
        {
            var iconGO = MakeGO("Icon", go.transform);
            iconGO.AddComponent<LayoutElement>().preferredHeight = 64;
            var img           = iconGO.AddComponent<Image>();
            img.sprite        = firstFrame;
            img.preserveAspect = true;

            if (card.IconFrames != null && card.IconFrames.Length > 1)
                StartCoroutine(AnimateIcon(img, card.IconFrames));
        }

        // Card name
        var nameGO  = MakeGO("Name", go.transform);
        nameGO.AddComponent<LayoutElement>().preferredHeight = 32;
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text             = card.CardName;
        nameTMP.fontSize         = 13f;
        nameTMP.alignment        = TextAlignmentOptions.Center;
        nameTMP.color            = ColCardName;
        nameTMP.fontStyle        = FontStyles.Bold;
        nameTMP.textWrappingMode = TextWrappingModes.Normal;

        // Rarity label
        var rarGO  = MakeGO("Rarity", go.transform);
        rarGO.AddComponent<LayoutElement>().preferredHeight = 16;
        var rarTMP = rarGO.AddComponent<TextMeshProUGUI>();
        rarTMP.text      = card.CardRarity.ToString().ToUpper();
        rarTMP.fontSize  = 10f;
        rarTMP.alignment = TextAlignmentOptions.Center;
        rarTMP.color     = RarityColor(card.CardRarity);
        rarTMP.fontStyle = FontStyles.Bold;

        // Charges label — only shown when the card has more than 1 use
        if (card.MaxCharges > 1)
        {
            var chGO  = MakeGO("Charges", go.transform);
            chGO.AddComponent<LayoutElement>().preferredHeight = 14;
            var chTMP = chGO.AddComponent<TextMeshProUGUI>();
            chTMP.text      = $"x{card.MaxCharges} usos";
            chTMP.fontSize  = 10f;
            chTMP.alignment = TextAlignmentOptions.Center;
            chTMP.color     = new Color(0.94f, 0.75f, 0.25f, 1f); // dorado
            chTMP.fontStyle = FontStyles.Bold;
        }

        int captured = idx;
        btn.onClick.AddListener(() => ChooseCard(captured));
    }

    // ── Selection ────────────────────────────────────────────────────────────

    private void ChooseCard(int idx)
    {
        CardData chosen = _offers[idx];
        PlayerInventory.Instance?.AddCard(chosen);

        IsPickerActive   = false;
        Cursor.lockState = CursorLockMode.None;

        if (_pickerCanvas != null) { Destroy(_pickerCanvas); _pickerCanvas = null; }

        OnCardChosen?.Invoke();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Color RarityColor(CardData.Rarity r) => r switch
    {
        CardData.Rarity.Rare => ColRare,
        CardData.Rarity.Epic => ColEpic,
        _                    => ColCommon,
    };

    private static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt      = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static RectTransform Center(GameObject go, float w, float h)
    {
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    // ── Animation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Cycles through IconFrames on a UI Image at IconAnimFps.
    /// Uses WaitForSecondsRealtime so it works while Time.timeScale == 0 (Paused state).
    /// Stops automatically when the Image is destroyed.
    /// </summary>
    private static IEnumerator AnimateIcon(Image img, Sprite[] frames)
    {
        var wait = new WaitForSecondsRealtime(1f / IconAnimFps);
        int f    = 0;
        while (img != null)
        {
            img.sprite = frames[f];
            f          = (f + 1) % frames.Length;
            yield return wait;
        }
    }

    /// <summary>For testing — skips card selection and resumes immediately.</summary>
    public static void SimulateCardChosen() => OnCardChosen?.Invoke();
}
