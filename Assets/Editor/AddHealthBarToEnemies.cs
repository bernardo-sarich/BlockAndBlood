#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot Editor utility. Run via Tools > Add Health Bar To All Enemies.
/// Adds a "HealthBar" child GO (SpriteRenderer + EnemyHealthBar) to every enemy prefab
/// and populates the _frames array with the 29 lifeRemaining sprites.
/// Safe to run multiple times — skips prefabs that already have EnemyHealthBar.
/// </summary>
public static class AddHealthBarToEnemies
{
    private static readonly string[] PrefabPaths = new[]
    {
        "Assets/_Project/Prefabs/Enemies/Enemy_Caminante.prefab",
        "Assets/_Project/Prefabs/Enemies/Enemy_Rapido.prefab",
        "Assets/_Project/Prefabs/Enemies/Enemy_Blindado.prefab",
        "Assets/_Project/Prefabs/Enemies/Enemy_Sacerdote.prefab",
        "Assets/_Project/Prefabs/Enemies/Enemy_Bruto.prefab",
    };

    private const string SpriteSheetPath = "Assets/_Project/Art/Effects/lifeRemaining.png";
    private const int    FrameCount      = 29;

    [MenuItem("Tools/Add Health Bar To All Enemies")]
    public static void Run()
    {
        // Load all 29 sprites from the sheet (they are sub-assets of the PNG)
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath);
        var frames    = new Sprite[FrameCount];
        int found     = 0;

        foreach (var asset in allAssets)
        {
            if (asset is not Sprite sp) continue;
            // Names are lifeRemaining_0 … lifeRemaining_28
            for (int i = 0; i < FrameCount; i++)
            {
                if (sp.name == $"lifeRemaining_{i}")
                {
                    frames[i] = sp;
                    found++;
                    break;
                }
            }
        }

        if (found != FrameCount)
        {
            Debug.LogError($"[AddHealthBarToEnemies] Expected {FrameCount} sprites in '{SpriteSheetPath}', found {found}. " +
                           "Make sure the sprite sheet is sliced correctly before running this tool.");
            return;
        }

        foreach (string path in PrefabPaths)
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var root = scope.prefabContentsRoot;

            // Skip if already set up
            if (root.GetComponentInChildren<EnemyHealthBar>() != null)
            {
                Debug.Log($"[AddHealthBarToEnemies] Skipping '{path}' — EnemyHealthBar already present.");
                continue;
            }

            // Create child GO
            var barGO = new GameObject("HealthBar");
            barGO.transform.SetParent(root.transform, worldPositionStays: false);
            barGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            // SpriteRenderer — sorting layer Effects, order 10
            var sr          = barGO.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Effects";
            sr.sortingOrder     = 10;
            sr.enabled          = false; // hidden until first hit

            // EnemyHealthBar
            var hb = barGO.AddComponent<EnemyHealthBar>();

            // Assign sprites via SerializedObject so Unity serializes them properly
            var so    = new SerializedObject(hb);
            var frProp = so.FindProperty("_frames");
            frProp.arraySize = FrameCount;
            for (int i = 0; i < FrameCount; i++)
                frProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[AddHealthBarToEnemies] Added HealthBar to '{path}'.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AddHealthBarToEnemies] Done.");
    }
}
#endif
