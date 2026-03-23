using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ProjectileArrowPatch
{
    [MenuItem("Block&Blood/Patch Projectile Arrow")]
    static void Run()
    {
        const string arrowSheet = "Assets/Character/lpc_entry/lpc_entry/png/bow/WEAPON_arrow.png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(arrowSheet)
            .OfType<Sprite>()
            .OrderByDescending(s => s.rect.y)
            .ThenBy(s => s.rect.x)
            .ToArray();

        // row3 col0 = right-facing arrow
        Sprite arrow = sprites.Length > 39 ? sprites[39] : sprites[0];

        const string prefabPath = "Assets/_Project/Prefabs/Towers/Projectile_Default.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);

        root.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

        var sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite       = arrow;
            sr.sortingOrder = 8;
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log($"[Setup] Projectile_Default updated with arrow sprite={arrow.name}");
    }
}
