using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets up: skeleton sprite for enemies, tower.png for range tower, arrow for projectile.
/// Menu: Block&Blood / Setup Enemies & Range Tower
/// </summary>
public static class EnemyAndRangeTowerSetup
{
    private const int FrameSize = 64;
    private const int PPU       = 64;

    [MenuItem("Block&Blood/Setup Enemies and Range Tower")]
    static void Run()
    {
        SetupSkeleton();
        SetupRangeTower();
        SetupArrowProjectile();
        Debug.Log("[Setup] Done — skeleton enemy, range tower, and arrow projectile configured.");
    }

    // ── Skeleton Enemy ──────────────────────────────────────────────────────

    static void SetupSkeleton()
    {
        const string sheetPath = "Assets/Character/lpc_entry/lpc_entry/png/walkcycle/BODY_skeleton.png";

        // Slice spritesheet (9 cols x 4 rows)
        SliceLPC(sheetPath, 9);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Load front-facing idle frame (row 2 = "down", col 0 = idle)
        // LPC layout: row0=up, row1=left, row2=down, row3=right
        // In sprite coords: row2 from top = texY for row2
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

        // row2, col0 = index 2*9 + 0 = 18
        Sprite frontIdle = sprites.Length > 18 ? sprites[18] : sprites[0];

        // Patch enemy prefab
        const string prefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Caminante.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);

        root.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite       = frontIdle;
            sr.sortingOrder = 5;
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        // Fix AIPath for 2D: disable rotation so the sprite doesn't deform
        var aiPath = root.GetComponent<Pathfinding.AIPath>();
        if (aiPath != null)
        {
            aiPath.enableRotation = false;
            aiPath.orientation    = Pathfinding.OrientationMode.ZAxisForward;
        }

        Debug.Log($"[Setup] Skeleton sprite assigned to Enemy_Caminante, scale=1.6, sprite={frontIdle.name}");
    }

    // ── Range Tower ─────────────────────────────────────────────────────────

    static void SetupRangeTower()
    {
        const string texPath = "Assets/tower.png";

        // Import as sprite
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer == null) { Debug.LogError("[Setup] tower.png not found"); return; }

        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode       = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Sprite towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (towerSprite == null) { Debug.LogError("[Setup] Failed to load tower sprite"); return; }

        // tower.png is 239x486. To fit cell width (0.96 units) at 100 PPU:
        // spriteWidth = 239/100 = 2.39 units at scale 1
        // scale = 0.96 / 2.39 ≈ 0.40
        float scaleVal = 0.40f;

        string[] rangePrefabs = {
            "Assets/_Project/Prefabs/Towers/Tower_Range_Lv1.prefab",
            "Assets/_Project/Prefabs/Towers/Tower_Fire_Lv2.prefab",
            "Assets/_Project/Prefabs/Towers/Tower_Water_Lv2.prefab",
        };

        foreach (string path in rangePrefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            root.transform.localScale = new Vector3(scaleVal, scaleVal, 1f);

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite       = towerSprite;
                sr.sortingOrder = 10;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log("[Setup] Range tower sprite assigned to Range/Fire/Water prefabs");
    }

    // ── Arrow Projectile ────────────────────────────────────────────────────

    static void SetupArrowProjectile()
    {
        const string sheetPath = "Assets/Character/lpc_entry/lpc_entry/png/bow/WEAPON_arrow.png";

        // 832x256 = 13 cols x 4 rows
        SliceLPC(sheetPath, 13);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Pick a right-pointing arrow: row 3 (right direction), col 0
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

        // row3, col0 = index 3*13 + 0 = 39
        Sprite arrowSprite = sprites.Length > 39 ? sprites[39] : sprites[0];

        // Check if projectile prefab exists
        const string projPath = "Assets/_Project/Prefabs/Towers/Projectile_Arrow.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);

        if (existing == null)
        {
            // Create projectile prefab
            var go = new GameObject("Projectile_Arrow");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = arrowSprite;
            sr.sortingOrder = 8;

            go.AddComponent<ProjectileBehaviour>();

            var col = go.AddComponent<CircleCollider2D>();
            col.radius    = 0.1f;
            col.isTrigger = true;

            go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            PrefabUtility.SaveAsPrefabAsset(go, projPath);
            Object.DestroyImmediate(go);
        }
        else
        {
            // Update existing prefab
            var root = PrefabUtility.LoadPrefabContents(projPath);
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = arrowSprite;
            PrefabUtility.SaveAsPrefabAsset(root, projPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[Setup] Arrow projectile created/updated with sprite={arrowSprite.name}");
    }

    // ── Shared LPC slicer ───────────────────────────────────────────────────

    static void SliceLPC(string path, int cols)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogWarning("[Setup] Not found: " + path); return; }

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.filterMode          = FilterMode.Point;
        importer.mipmapEnabled       = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = PPU;

        int rows   = 4;
        int sheetH = rows * FrameSize;

        var list = new List<SpriteMetaData>();
        for (int row = 0; row < rows; row++)
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
}
