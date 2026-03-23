using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// One-shot utility: slices roguelikeChar_magenta.png into 16x16 sprites
/// and applies Point filter mode on both roguelikeChar spritesheets.
/// Run via: Tools > Setup Roguelike Character Sprites
/// </summary>
public class RoguelikeCharacterSpritesSetup
{
    private const string TransparentPath = "Assets/kenney_roguelike-characters/Spritesheet/roguelikeChar_transparent.png";
    private const string MagentaPath     = "Assets/kenney_roguelike-characters/Spritesheet/roguelikeChar_magenta.png";

    private const int TileSize = 16;
    private const int Spacing  = 1;
    private const int Step     = TileSize + Spacing; // 17

    [MenuItem("Tools/Setup Roguelike Character Sprites")]
    public static void Setup()
    {
        ApplyPointFilter(TransparentPath);
        SliceMagenta();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RoguelikeCharacterSpritesSetup] Done — both spritesheets configured.");
    }

    static void ApplyPointFilter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[Setup] Importer not found: " + assetPath);
            return;
        }
        if (importer.filterMode == FilterMode.Point)
            return;

        importer.filterMode = FilterMode.Point;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        Debug.Log("[Setup] FilterMode → Point : " + assetPath);
    }

    static void SliceMagenta()
    {
        var importer = AssetImporter.GetAtPath(MagentaPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[Setup] Importer not found: " + MagentaPath);
            return;
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(MagentaPath);
        if (tex == null)
        {
            Debug.LogWarning("[Setup] Texture not loaded: " + MagentaPath);
            return;
        }

        int texW = tex.width;   // 918
        int texH = tex.height;  // 203

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.filterMode          = FilterMode.Point;
        importer.mipmapEnabled       = false;
        importer.alphaIsTransparency = false; // magenta bg, not alpha channel

        // 54 cols x 12 rows on a 17-px step grid (16 tile + 1 gap)
        int cols = (texW + Spacing) / Step; // 54
        int rows = (texH + Spacing) / Step; // 12

        var list = new List<SpriteMetaData>();
        int idx = 0;

        // Iterate top row first (image-space) so index 0 = top-left character
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                int x = col * Step;
                int y = row * Step;

                if (x + TileSize > texW || y + TileSize > texH)
                    continue;

                list.Add(new SpriteMetaData
                {
                    name      = "roguelikeChar_magenta_" + idx,
                    rect      = new Rect(x, y, TileSize, TileSize),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot     = new Vector2(0.5f, 0f) // base pivot for 3/4 view
                });
                idx++;
            }
        }

        importer.spritesheet = list.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[Setup] Magenta sliced: {cols}×{rows} = {list.Count} sprites");
    }
}
