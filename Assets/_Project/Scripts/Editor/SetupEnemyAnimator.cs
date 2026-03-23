#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Editor tool: populates EnemyAnimator.walkSprites on Enemy_Caminante prefab
/// from the BODY_skeleton.png walkcycle sprite sheet.
/// Menu: Tools > Setup Enemy Animator
/// </summary>
public static class SetupEnemyAnimator
{
    [MenuItem("Tools/Setup Enemy Animator")]
    public static void Run()
    {
        // Load all sub-sprites from the skeleton walkcycle sheet
        const string sheetPath = "Assets/Character/lpc_entry/lpc_entry/png/walkcycle/BODY_skeleton.png";
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        var sprites = allAssets
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("[SetupEnemyAnimator] No sprites found in " + sheetPath);
            return;
        }

        // LPC walkcycle: 4 rows x 9 cols = 36 frames
        // Sprites are named frame_RR_CC (row_col), sorted alphabetically = correct order
        Debug.Log($"[SetupEnemyAnimator] Found {sprites.Length} sprites. Names: {string.Join(", ", sprites.Select(s => s.name))}");

        // Load and modify the prefab
        const string prefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_Caminante.prefab";
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[SetupEnemyAnimator] Could not load prefab: " + prefabPath);
            return;
        }

        // Add EnemyAnimator if not present
        var animator = prefabRoot.GetComponent<EnemyAnimator>();
        if (animator == null)
            animator = prefabRoot.AddComponent<EnemyAnimator>();

        animator.walkSprites = sprites;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log($"[SetupEnemyAnimator] Done! Assigned {sprites.Length} walk sprites to Enemy_Caminante.");
    }
}
#endif
