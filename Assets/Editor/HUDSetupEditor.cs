using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility that rebuilds Canvas_HUD from scratch.
/// Menu: Block&Blood / Setup HUD
/// Run this once to create a fully wired HUD canvas.
/// </summary>
public static class HUDSetupEditor
{
    [MenuItem("Block&Blood/Setup HUD")]
    public static void SetupHUD()
    {
        // ── Destroy old canvas if it exists ──────────────────────────────────
        var old = GameObject.Find("Canvas_HUD");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
            Debug.Log("[HUDSetup] Removed old Canvas_HUD.");
        }

        // ── Ensure EventSystem ────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        }

        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas_HUD");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas_HUD");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight   = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Load TowerData SOs ────────────────────────────────────────────────
        var meleeSO = AssetDatabase.LoadAssetAtPath<TowerData>(
            "Assets/_Project/ScriptableObjects/Towers/TowerMelee_Lv1.asset");
        var rangeSO = AssetDatabase.LoadAssetAtPath<TowerData>(
            "Assets/_Project/ScriptableObjects/Towers/TowerRange_Lv1.asset");

        // ── Gold Panel (top-left) ─────────────────────────────────────────────
        var goldPanel = MakePanel(canvasGO.transform, "GoldPanel",
            anchor: new Vector2(0, 1), pivot: new Vector2(0, 1),
            pos: new Vector2(10, -10), size: new Vector2(180, 52));

        var goldText = MakeText(goldPanel.transform, "GoldText", "50g", 30,
            TextAlignmentOptions.Center, Color.yellow);
        FillParent(goldText.GetComponent<RectTransform>());

        // ── Tower Buttons Panel (bottom-center) ───────────────────────────────
        var btnPanel = MakePanel(canvasGO.transform, "TowerButtonsPanel",
            anchor: new Vector2(0.5f, 0), pivot: new Vector2(0.5f, 0),
            pos: new Vector2(0, 10), size: new Vector2(340, 90));

        var btnMelee = MakeButton(btnPanel.transform, "Btn_Melee",
            meleeSO != null ? $"{meleeSO.TowerName}\n{meleeSO.Cost}g" : "Melee\n12g",
            new Color(0.2f, 0.3f, 0.5f));
        SetRect(btnMelee, new Vector2(0, 0), new Vector2(0.5f, 1),
                new Vector2(0.5f, 0.5f), new Vector2(-4, -8), Vector2.zero);

        var btnRange = MakeButton(btnPanel.transform, "Btn_Range",
            rangeSO != null ? $"{rangeSO.TowerName}\n{rangeSO.Cost}g" : "Rango\n10g",
            new Color(0.2f, 0.4f, 0.3f));
        SetRect(btnRange, new Vector2(0.5f, 0), new Vector2(1, 1),
                new Vector2(0.5f, 0.5f), new Vector2(4, -8), Vector2.zero);

        // ── Tower Info Panel (bottom-right, starts hidden) ────────────────────
        var infoPanel = MakePanel(canvasGO.transform, "TowerInfoPanel",
            anchor: new Vector2(1, 0), pivot: new Vector2(1, 0),
            pos: new Vector2(-10, 10), size: new Vector2(240, 280));

        var nameText    = MakeText(infoPanel.transform, "TowerNameText", "Torre Melee", 22,
                              TextAlignmentOptions.Center, Color.white, bold: true);
        SetLayoutElement(nameText.gameObject, preferredHeight: 32);

        var upg0Btn     = MakeButton(infoPanel.transform, "Btn_Upgrade0", "Mejorar", new Color(0.2f, 0.5f, 0.2f));
        SetLayoutElement(upg0Btn, preferredHeight: 44);
        var upg0Text    = upg0Btn.GetComponentInChildren<TextMeshProUGUI>();

        var upg1Btn     = MakeButton(infoPanel.transform, "Btn_Upgrade1", "Mejorar", new Color(0.2f, 0.5f, 0.2f));
        SetLayoutElement(upg1Btn, preferredHeight: 44);
        var upg1Text    = upg1Btn.GetComponentInChildren<TextMeshProUGUI>();

        var sellBtn     = MakeButton(infoPanel.transform, "Btn_Sell", "Vender (0g)", new Color(0.5f, 0.15f, 0.15f));
        SetLayoutElement(sellBtn, preferredHeight: 44);
        var sellText    = sellBtn.GetComponentInChildren<TextMeshProUGUI>();

        var closeBtn    = MakeButton(infoPanel.transform, "Btn_Close", "Cerrar", new Color(0.3f, 0.3f, 0.3f));
        SetLayoutElement(closeBtn, preferredHeight: 34);

        // Add VerticalLayoutGroup to infoPanel
        var vlg = infoPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = 6;
        vlg.padding              = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        infoPanel.SetActive(false);

        // ── Wire HUDController ────────────────────────────────────────────────
        var hud = canvasGO.AddComponent<HUDController>();

        var so = new SerializedObject(hud);
        so.FindProperty("_goldText").objectReferenceValue           = goldText;
        so.FindProperty("_meleeTowerData").objectReferenceValue     = meleeSO;
        so.FindProperty("_rangeTowerData").objectReferenceValue     = rangeSO;
        so.FindProperty("_meleeButton").objectReferenceValue        = btnMelee.GetComponent<Button>();
        so.FindProperty("_rangeButton").objectReferenceValue        = btnRange.GetComponent<Button>();
        so.FindProperty("_meleeButtonText").objectReferenceValue    = btnMelee.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("_rangeButtonText").objectReferenceValue    = btnRange.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("_towerPanel").objectReferenceValue         = infoPanel;
        so.FindProperty("_towerNameText").objectReferenceValue      = nameText;
        so.FindProperty("_upgradePath0Button").objectReferenceValue = upg0Btn.GetComponent<Button>();
        so.FindProperty("_upgradePath1Button").objectReferenceValue = upg1Btn.GetComponent<Button>();
        so.FindProperty("_upgrade0Text").objectReferenceValue       = upg0Text;
        so.FindProperty("_upgrade1Text").objectReferenceValue       = upg1Text;
        so.FindProperty("_sellButton").objectReferenceValue         = sellBtn.GetComponent<Button>();
        so.FindProperty("_sellText").objectReferenceValue           = sellText;
        so.FindProperty("_closeButton").objectReferenceValue        = closeBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(canvasGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[HUDSetup] Canvas_HUD created and wired successfully.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, string name,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        go.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float fontSize, TextAlignmentOptions align, Color color, bool bold = false)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font           = FindTMPFont();
        tmp.text           = text;
        tmp.fontSize       = fontSize;
        tmp.alignment      = align;
        tmp.color          = color;
        tmp.fontStyle      = bold ? FontStyles.Bold : FontStyles.Normal;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string name, string label, Color bgColor)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = bgColor;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        // Label child
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        FillParent(labelGO.GetComponent<RectTransform>());
        var tmp         = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.font        = FindTMPFont();
        tmp.text        = label;
        tmp.fontSize    = 18;
        tmp.alignment   = TextAlignmentOptions.Center;
        tmp.color       = Color.white;

        return go;
    }

    static void FillParent(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    static void SetRect(GameObject go,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt          = go.GetComponent<RectTransform>();
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.pivot        = pivot;
        rt.offsetMin    = offsetMin;
        rt.offsetMax    = offsetMax;
    }

    static void SetLayoutElement(GameObject go, float preferredHeight)
    {
        var le              = go.AddComponent<LayoutElement>();
        le.preferredHeight  = preferredHeight;
    }

    static TMP_FontAsset _cachedFont;
    static TMP_FontAsset FindTMPFont()
    {
        if (_cachedFont != null) return _cachedFont;

        // Search all TMP_FontAsset in project + packages
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) { _cachedFont = font; return font; }
        }

        // Last resort: try loading from Resources (TMP default path)
        _cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (_cachedFont != null) return _cachedFont;

        Debug.LogWarning("[HUDSetup] No TMP font asset found. Please import TMP Essential Resources via Window > TextMeshPro > Import TMP Essential Resources.");
        return null;
    }
}
