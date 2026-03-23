
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class GrassSpriteSheetSlicer
{
    [MenuItem("Tools/Slice GRASS+ Sprite Sheet")]
    public static void SliceGrassSheet()
    {
        SlicePath("Assets/_Project/Art/Sprites/GRASS+.png");
        SlicePath("Assets/Resources/Decorations/GRASS+.png");
    }

    private static void SlicePath(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[Slicer] TextureImporter not found at {path}");
            return;
        }

        // Pixel art settings
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 16;
        importer.mipmapEnabled = false;

        // Read texture dimensions
        object[] args = new object[] { 0, 0 };
        var method = typeof(TextureImporter).GetMethod("GetWidthAndHeight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(importer, args);
        int texWidth = (int)args[0];
        int texHeight = (int)args[1];

        Debug.Log($"[Slicer] Texture size: {texWidth}x{texHeight}");

        int cellSize = 16;
        int cols = texWidth / cellSize;
        int rows = texHeight / cellSize;

        var spriteMetaData = new List<SpriteMetaData>();
        int index = 0;

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                var smd = new SpriteMetaData
                {
                    name = $"GRASS+_{index}",
                    rect = new Rect(col * cellSize, row * cellSize, cellSize, cellSize),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
                spriteMetaData.Add(smd);
                index++;
            }
        }

        importer.spritesheet = spriteMetaData.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[Slicer] Done! Sliced into {index} sprites ({cols}x{rows} grid of {cellSize}x{cellSize}px)");
    }
}
