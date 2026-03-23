using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot utility that sets up the Hero with layered LPC chain-armor-bandit sprites.
/// Run via: Tools > Setup Hero Chain Armor
///
/// Creates one child GameObject per body layer, each with its own SpriteRenderer.
/// HeroAnimator advances all layers in sync so they always show the same frame.
/// </summary>
public class HeroChainArmorSetup
{
    private const string WalkRoot   = "Assets/Character/lpc_entry/lpc_entry/png/walkcycle/";
    private const string AttackRoot = "Assets/Character/lpc_entry/lpc_entry/png/slash/";
    private const int    FrameSize  = 64;
    private const int    Rows       = 4;
    private const int    WalkCols   = 9;
    private const int    AttackCols = 6;
    private const int    PPU        = 64;

    // LPC stacking order (bottom = 0, rendered behind; top = last, rendered in front)
    // label, walkFile, attackFile, sortingOffset
    private static readonly (string label, string walk, string attack, int order)[] LayerDefs =
    {
        ("Body",   "BODY_male.png",                       "BODY_human.png",                       0),
        ("Feet",   "FEET_shoes_brown.png",                "FEET_shoes_brown.png",                 1),
        ("Legs",   "LEGS_pants_greenish.png",             "LEGS_pants_greenish.png",              2),
        ("Torso",  "TORSO_chain_armor_torso.png",         "TORSO_chain_armor_torso.png",          3),
        ("Jacket", "TORSO_chain_armor_jacket_purple.png", "TORSO_chain_armor_jacket_purple.png",  4),
        ("Belt",   "BELT_leather.png",                    "BELT_leather.png",                     5),
        ("Head",   "HEAD_chain_armor_helmet.png",         "HEAD_chain_armor_helmet.png",          6),
    };

    [MenuItem("Tools/Setup Hero Chain Armor")]
    public static void Run()
    {
        // 1 — Slice all required sprite sheets
        var pathsToSlice = new HashSet<string>();
        foreach (var (_, walk, attack, _) in LayerDefs)
        {
            pathsToSlice.Add(WalkRoot   + walk);
            pathsToSlice.Add(AttackRoot + attack);
        }
        foreach (string path in pathsToSlice)
            SliceSheet(path, path.StartsWith(WalkRoot) ? WalkCols : AttackCols);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2 — Find the Hero
        var hero = UnityEngine.Object.FindFirstObjectByType<HeroBehaviour>();
        if (hero == null)
        {
            Debug.LogError("[HeroChainArmorSetup] No HeroBehaviour found in the active scene.");
            return;
        }

        // 3 — Remove old layer children (tagged "HeroLayer")
        var toDelete = new List<GameObject>();
        foreach (Transform child in hero.transform)
            if (child.CompareTag("HeroLayer"))
                toDelete.Add(child.gameObject);
        foreach (var go in toDelete)
            UnityEngine.Object.DestroyImmediate(go);

        // 4 — Add HeroAnimator if missing; reset config
        var anim = hero.GetComponent<HeroAnimator>();
        if (anim == null) anim = hero.gameObject.AddComponent<HeroAnimator>();

        // Disable the root SpriteRenderer — layers handle all rendering now
        var rootSr = hero.GetComponent<SpriteRenderer>();
        if (rootSr != null) rootSr.enabled = false;

        // 5 — Create one child per layer
        // Base sorting order of the hero (keep existing or use 10)
        int baseSortOrder = rootSr != null ? rootSr.sortingOrder : 10;
        string sortingLayer = rootSr != null ? rootSr.sortingLayerName : "Default";

        var animLayers = new HeroAnimator.AnimLayer[LayerDefs.Length];

        for (int i = 0; i < LayerDefs.Length; i++)
        {
            var (label, walkFile, attackFile, sortOffset) = LayerDefs[i];

            // Create child GameObject
            var child = new GameObject($"Layer_{label}");
            child.transform.SetParent(hero.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.tag = "HeroLayer";

            // Add SpriteRenderer
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder     = baseSortOrder + sortOffset;

            // Load sorted sprites
            Sprite[] walkSprites   = LoadSorted(WalkRoot   + walkFile,   WalkCols);
            Sprite[] attackSprites = LoadSorted(AttackRoot + attackFile, AttackCols);

            animLayers[i] = new HeroAnimator.AnimLayer
            {
                label         = label,
                renderer      = sr,
                walkSprites   = walkSprites,
                attackSprites = attackSprites,
            };

            Debug.Log($"[HeroChainArmorSetup] Layer '{label}': walk={walkSprites.Length} attack={attackSprites.Length}");
        }

        // 6 — Assign layers + frame config to HeroAnimator
        var so = new SerializedObject(anim);

        var layersProp = so.FindProperty("layers");
        layersProp.arraySize = animLayers.Length;
        for (int i = 0; i < animLayers.Length; i++)
        {
            var elem = layersProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("label").stringValue                             = animLayers[i].label;
            elem.FindPropertyRelative("renderer").objectReferenceValue                 = animLayers[i].renderer;
            SetSpriteArray(elem.FindPropertyRelative("walkSprites"),   animLayers[i].walkSprites);
            SetSpriteArray(elem.FindPropertyRelative("attackSprites"), animLayers[i].attackSprites);
        }

        so.FindProperty("walkCols").intValue   = WalkCols;
        so.FindProperty("attackCols").intValue = AttackCols;
        so.ApplyModifiedProperties();

        // 7 — Adjust scale: LPC 64px at 64 PPU = 1 unit; hero should be 0.72 world units
        hero.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        // 8 — Create the shadow child directly (avoids Start() timing issues)
        // Delete existing shadow if present
        var existingShadow = hero.transform.Find("Shadow");
        if (existingShadow != null)
            UnityEngine.Object.DestroyImmediate(existingShadow.gameObject);

        // Get a sprite from the Body layer's walk array (idle frame, Down direction)
        // Down = row 2, frame 0  →  index = 2 * WalkCols + 0 = 18
        Sprite shadowSprite = null;
        if (animLayers.Length > 0 && animLayers[0].walkSprites.Length > 18)
            shadowSprite = animLayers[0].walkSprites[18];
        else if (animLayers.Length > 0 && animLayers[0].walkSprites.Length > 0)
            shadowSprite = animLayers[0].walkSprites[0];

        if (shadowSprite != null)
        {
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(hero.transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            shadowGo.transform.localScale    = new Vector3(0.7f, 0.35f, 1f);

            var shadowSr             = shadowGo.AddComponent<SpriteRenderer>();
            shadowSr.sprite          = shadowSprite;
            shadowSr.color           = new Color(0f, 0f, 0f, 0.35f);
            shadowSr.sortingLayerName = sortingLayer;
            shadowSr.sortingOrder    = baseSortOrder - 1; // behind everything

            // Disable EntityShadow so it doesn't try to create a duplicate at runtime
            var entityShadow = hero.GetComponent<EntityShadow>();
            if (entityShadow != null) entityShadow.enabled = false;

            Debug.Log("[HeroChainArmorSetup] Shadow child created.");
        }
        else
        {
            Debug.LogWarning("[HeroChainArmorSetup] Could not find a sprite for shadow.");
        }

        EditorUtility.SetDirty(hero.gameObject);
        Debug.Log($"[HeroChainArmorSetup] Done! {animLayers.Length} layers configured.");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static void SliceSheet(string path, int cols)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[HeroChainArmorSetup] Not found: " + path);
            return;
        }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.filterMode          = FilterMode.Point;
        importer.mipmapEnabled       = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = PPU;

        int sheetH = Rows * FrameSize; // 256

        var list = new List<SpriteMetaData>();
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int texY = sheetH - (row + 1) * FrameSize;
                list.Add(new SpriteMetaData
                {
                    name      = $"frame_{row:00}_{col:00}",
                    rect      = new Rect(col * FrameSize, texY, FrameSize, FrameSize),
                    pivot     = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center,
                });
            }
        }

        importer.spritesheet = list.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    static Sprite[] LoadSorted(string path, int expectedCols)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

        if (sprites.Length == 0)
            Debug.LogWarning($"[HeroChainArmorSetup] No sprites found at: {path}");

        return sprites;
    }

    static void SetSpriteArray(SerializedProperty prop, Sprite[] sprites)
    {
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
