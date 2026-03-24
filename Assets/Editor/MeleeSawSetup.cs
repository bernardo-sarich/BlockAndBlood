using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot editor utility: sets serra_redonda.png as Sprite,
/// assigns it to Melee Lv1/Lv2 prefabs, and adds SpinForever.
/// Menu: Block&Blood / Setup Melee Saw
/// </summary>
public static class MeleeSawSetup
{
    [MenuItem("Block&Blood/Setup Melee Saw")]
    static void Run()
    {
        // 1. Fix texture import → Sprite
        const string texPath = "Assets/serra_redonda.png";
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[MeleeSaw] TextureImporter not found at " + texPath);
            return;
        }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType  = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        Sprite sawSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (sawSprite == null)
        {
            Debug.LogError("[MeleeSaw] Failed to load Sprite from " + texPath);
            return;
        }

        // 2. Fix Tower_Building sprite import
        const string buildingTexPath = "Assets/Resources/Grid/Tower_Building.png";
        var buildingImporter = AssetImporter.GetAtPath(buildingTexPath) as TextureImporter;
        if (buildingImporter != null && buildingImporter.textureType != TextureImporterType.Sprite)
        {
            buildingImporter.textureType      = TextureImporterType.Sprite;
            buildingImporter.spriteImportMode  = SpriteImportMode.Single;
            buildingImporter.spritePixelsPerUnit = 267; // 256px / 0.96 cells ≈ 267 PPU
            buildingImporter.SaveAndReimport();
        }

        // 3. Patch melee prefabs (sprite + spin)
        PatchPrefab("Assets/_Project/Prefabs/Towers/Tower_Melee_Lv1.prefab", sawSprite, 540f);
        PatchPrefab("Assets/_Project/Prefabs/Towers/Tower_Melee_Lv2.prefab", sawSprite, 720f);

        // 3. Fix sortingOrder on ALL tower prefabs so they render above grid tiles
        FixSortingOrder("Assets/_Project/Prefabs/Towers/Tower_Range_Lv1.prefab");

        Debug.Log("[MeleeSaw] Done — all tower prefabs updated.");
    }

    static void PatchPrefab(string prefabPath, Sprite sprite, float degreesPerSec)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[MeleeSaw] Prefab not found: " + prefabPath);
            return;
        }

        // Open prefab for editing
        var root = PrefabUtility.LoadPrefabContents(prefabPath);

        // Sprite is 349x335px @ 100PPU = 3.49 units. Cell is 0.96.
        // Scale 0.96/3.49 ≈ 0.275 to fit one cell
        root.transform.localScale = new Vector3(0.275f, 0.275f, 1f);

        // Sprite + sorting
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sprite;
            sr.sortingOrder = 10; // above grid tiles (which use -100 to -108)
        }

        // SpinForever — add only if missing
        if (root.GetComponent<SpinForever>() == null)
        {
            var spin = root.AddComponent<SpinForever>();
            var prefabSO = new SerializedObject(spin);
            prefabSO.FindProperty("_degreesPerSecond").floatValue = degreesPerSec;
            prefabSO.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void FixSortingOrder(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = 10;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
