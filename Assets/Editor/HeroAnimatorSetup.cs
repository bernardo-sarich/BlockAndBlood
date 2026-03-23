using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot utility: slices the LPC walk and slash sheets, adds HeroAnimator
/// to the Hero, and assigns sprites in the correct LPC order.
/// Run via: Tools > Setup Hero Animator
/// </summary>
public class HeroAnimatorSetup
{
    private const string WalkSheetPath   = "Assets/Character/lpc_entry/lpc_entry/png/walkcycle/BODY_male.png";
    private const string AttackSheetPath = "Assets/Character/lpc_entry/lpc_entry/png/slash/BODY_human.png";
    private const int    FrameSize       = 64;
    private const int    Rows            = 4;   // LPC standard: 4 directions
    private const int    WalkCols        = 9;   // 1 idle + 8 walk frames
    private const int    AttackCols      = 6;   // 6 slash frames per direction

    [MenuItem("Tools/Setup Hero Animator")]
    public static void Run()
    {
        // 1 — Slice the sprite sheets
        SliceSheet(WalkSheetPath,   WalkCols);
        SliceSheet(AttackSheetPath, AttackCols);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2 — Find the Hero
        var hero = Object.FindFirstObjectByType<HeroBehaviour>();
        if (hero == null)
        {
            Debug.LogError("[HeroAnimatorSetup] No HeroBehaviour found in the active scene.");
            return;
        }

        // 3 — Add HeroAnimator component if missing
        var anim = hero.GetComponent<HeroAnimator>();
        if (anim == null)
            anim = hero.gameObject.AddComponent<HeroAnimator>();

        // 4 — Load sprites sorted in LPC order (top row = highest Y = Up direction)
        Sprite[] walkSprites   = LoadSorted(WalkSheetPath);
        Sprite[] attackSprites = LoadSorted(AttackSheetPath);

        // 5 — Assign via SerializedObject
        var so = new SerializedObject(anim);
        SetSpriteArray(so, "_walkSprites",   walkSprites);
        SetSpriteArray(so, "_attackSprites", attackSprites);
        so.ApplyModifiedProperties();

        // 6 — Adjust scale: LPC sprites at 64 PPU = 1 unit local → scale 0.72 = 0.72 world units
        hero.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        EditorUtility.SetDirty(hero.gameObject);
        Debug.Log($"[HeroAnimatorSetup] Done. Walk: {walkSprites.Length} sprites, Attack: {attackSprites.Length} sprites.");
    }

    static void SliceSheet(string path, int cols)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[HeroAnimatorSetup] Importer not found: " + path);
            return;
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning("[HeroAnimatorSetup] Texture not loaded: " + path);
            return;
        }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.filterMode          = FilterMode.Point;
        importer.mipmapEnabled       = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 64f;

        // Standard LPC sheet: Rows * FrameSize tall (256 for 4-row sheets)
        int sheetH = Rows * FrameSize;

        var list = new List<SpriteMetaData>();
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Unity Rect Y is bottom-left; LPC row 0 is at top of image
                int texY = sheetH - (row + 1) * FrameSize;
                list.Add(new SpriteMetaData
                {
                    name      = $"frame_{row:00}_{col:00}",
                    rect      = new Rect(col * FrameSize, texY, FrameSize, FrameSize),
                    pivot     = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                });
            }
        }

        importer.spritesheet = list.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        Debug.Log($"[HeroAnimatorSetup] Sliced {path}: {cols} cols × {Rows} rows = {list.Count} sprites");
    }

    static Sprite[] LoadSorted(string path)
    {
        // Sort: highest Y (top row = Up) first, then left to right within each row
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();
    }

    static void SetSpriteArray(SerializedObject so, string propName, Sprite[] sprites)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogError($"[HeroAnimatorSetup] Property not found: {propName}"); return; }
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }
}
