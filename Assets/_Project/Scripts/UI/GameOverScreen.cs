using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds and shows a fullscreen end-of-run overlay when the game reaches
/// Defeat or Victory. Hides on any other state.
/// The canvas is built procedurally — no scene setup required.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    // ── Named colors ─────────────────────────────────────────────────────────
    private static readonly Color ColOverlay  = new Color(0.00f, 0.00f, 0.00f, 0.82f);
    private static readonly Color ColPanel    = new Color(0.06f, 0.08f, 0.06f, 0.97f);
    private static readonly Color ColTitle    = new Color(0.95f, 0.95f, 0.95f, 1.00f);
    private static readonly Color ColSubtitle = new Color(0.70f, 0.70f, 0.70f, 1.00f);
    private static readonly Color ColBtnBg    = new Color(0.14f, 0.22f, 0.14f, 0.97f);
    private static readonly Color ColBtnBdr   = new Color(0.25f, 0.45f, 0.25f, 1.00f);
    private static readonly Color ColBtnTxt   = new Color(0.55f, 0.90f, 0.55f, 1.00f);
    private static readonly Color ColBtnHov   = new Color(0.20f, 0.32f, 0.20f, 0.97f);

    private const float PanelW = 480f;
    private const float PanelH = 290f;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private GameObject _canvas;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void OnEnable()  => GameManager.OnGameStateChanged += HandleStateChanged;
    private void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleStateChanged;
        if (_canvas != null) Destroy(_canvas);
    }

    // ── State handler ────────────────────────────────────────────────────────

    private void HandleStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Defeat:
                Show("DERROTA", "Llegaron demasiados enemigos a la base");
                break;

            case GameManager.GameState.Victory:
                Show("VICTORIA", "¡Derrotaste al Troll Anciano!");
                break;

            default:
                if (_canvas != null) _canvas.SetActive(false);
                break;
        }
    }

    // ── UI builder ───────────────────────────────────────────────────────────

    private void Show(string titleText, string subtitleText)
    {
        if (_canvas != null) Destroy(_canvas);

        _canvas = new GameObject("GameOverCanvas");
        var canvas = _canvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        _canvas.AddComponent<CanvasScaler>();
        _canvas.AddComponent<GraphicRaycaster>();

        // Full-screen overlay
        var overlay = MakeGO("Overlay", _canvas.transform);
        Stretch(overlay);
        overlay.AddComponent<Image>().color = ColOverlay;

        // Center panel
        var panel   = MakeGO("Panel", _canvas.transform);
        var panelRT = Center(panel, PanelW, PanelH);
        panel.AddComponent<Image>().color = ColPanel;

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 14;
        vlg.padding               = new RectOffset(40, 40, 36, 36);
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment        = TextAnchor.UpperCenter;

        // Large title
        var titleGO  = MakeGO("Title", panel.transform);
        titleGO.AddComponent<LayoutElement>().preferredHeight = 72;
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = titleText;
        titleTMP.fontSize  = 56f;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = ColTitle;
        titleTMP.fontStyle = FontStyles.Bold;

        // Subtitle
        var subGO  = MakeGO("Subtitle", panel.transform);
        subGO.AddComponent<LayoutElement>().preferredHeight = 30;
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text      = subtitleText;
        subTMP.fontSize  = 15f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color     = ColSubtitle;

        // Retry button
        var btnGO = MakeGO("RetryButton", panel.transform);
        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 52;
        btnLE.preferredWidth  = 200;

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = ColBtnBg;

        var btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor    = ColBtnBdr;
        btnOutline.effectDistance = new Vector2(1f, -1f);

        var btn    = btnGO.AddComponent<Button>();
        var cols   = btn.colors;
        cols.normalColor      = ColBtnBg;
        cols.highlightedColor = ColBtnHov;
        cols.pressedColor     = ColBtnHov;
        btn.colors = cols;
        btn.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

        var lblGO  = MakeGO("Label", btnGO.transform);
        var lblRT  = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = "Reintentar";
        lblTMP.fontSize  = 20f;
        lblTMP.alignment = TextAlignmentOptions.Center;
        lblTMP.color     = ColBtnTxt;
        lblTMP.fontStyle = FontStyles.Bold;

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRT);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt       = go.GetComponent<RectTransform>();
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
}
