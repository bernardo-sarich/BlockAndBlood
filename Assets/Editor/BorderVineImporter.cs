using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Applies the vine border art and edge shadows.
/// Borders are parented to the Main Camera so they appear as perfectly flat
/// vertical strips regardless of the perspective camera tilt.
/// Shadows are world-space sprites using an unlit material (no Global Light glow).
/// </summary>
public class BorderVineImporter : AssetPostprocessor
{
    const string VinePath     = "Assets/_Project/Art/Sprites/Background/Border_Vine.jpg";
    const string UnlitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    // Grid constants (CLAUDE.md)
    const float CellSize    = 0.96f;
    const int   GridCols    = 7;
    const int   GridRows    = 9;
    const float GridOriginX = -0.96f;
    const float GridOriginY =  0f;

    // Camera constants (measured from scene)
    const float CamX    =  2.34f;
    const float CamZ    = -10f;
    const float VFovDeg =  38f;
    const float Aspect  = 16f / 9f;

    // Camera-local Z where we park the border sprites.
    // Far enough to minimise any residual distortion; past near-clip (0.3).
    const float BorderLocalZ = 9f;

    // ── AssetPostprocessor ───────────────────────────────────────────────────
    void OnPreprocessTexture()
    {
        if (assetPath != VinePath) return;
        var imp = (TextureImporter)assetImporter;
        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100;
        imp.filterMode          = FilterMode.Bilinear;
        imp.wrapMode            = TextureWrapMode.Clamp;
        imp.mipmapEnabled       = false;
    }

    // ── Main entry ───────────────────────────────────────────────────────────
    [MenuItem("Tools/Apply Border Vine Sprites")]
    static void ApplyBorderVineSprites()
    {
        AssetDatabase.ImportAsset(VinePath, ImportAssetOptions.ForceUpdate);
        var vine = AssetDatabase.LoadAssetAtPath<Sprite>(VinePath);
        if (vine == null) { Debug.LogError("[BorderVine] Sprite not found: " + VinePath); return; }

        float nativeW = vine.rect.width  / vine.pixelsPerUnit;   // 5.92
        // float nativeH = vine.rect.height / vine.pixelsPerUnit; // 17.92 (unused – uniform scale)

        // Derived world measurements
        float gridLeft    = GridOriginX;
        float gridRight   = GridOriginX + GridCols * CellSize;   // 5.76
        float gridHeight  = GridRows  * CellSize;                // 8.64
        float gridCenterY = GridOriginY + gridHeight / 2f;       // 4.32

        float hFov   = 2f * Mathf.Atan(Mathf.Tan(VFovDeg * Mathf.Deg2Rad / 2f) * Aspect);
        float halfW  = Mathf.Abs(CamZ) * Mathf.Tan(hFov / 2f);  // ≈ 6.12
        float screenLeft  = CamX - halfW;   // ≈ -3.78
        float screenRight = CamX + halfW;   // ≈  8.46

        float leftPanelW  = gridLeft  - screenLeft;  // ≈ 2.82
        float rightPanelW = screenRight - gridRight;  // ≈ 2.70

        // Uniform scale = sprite fills panel width exactly; height overshoots (camera clips it)
        float scaleLeft  = leftPanelW  / nativeW;   // ≈ 0.476
        float scaleRight = rightPanelW / nativeW;   // ≈ 0.456

        // Camera-local X of each panel centre.
        // Camera.right == world X (no Y/Z rotation), so: world_x = CamX + local_x
        float leftCenterWorldX  = screenLeft  + leftPanelW  / 2f;   // ≈ -2.37
        float rightCenterWorldX = screenRight - rightPanelW / 2f;   // ≈  7.11
        float leftLocalX  = leftCenterWorldX  - CamX;   // ≈ -4.71
        float rightLocalX = rightCenterWorldX - CamX;   // ≈  4.77

        Debug.Log($"[BorderVine] leftPanel={leftPanelW:F2}u scale={scaleLeft:F3} | rightPanel={rightPanelW:F2}u scale={scaleRight:F3}");

        // Find the main camera
        var camGO = GameObject.FindWithTag("MainCamera");
        if (camGO == null) { Debug.LogError("[BorderVine] MainCamera not found."); return; }

        // Remove any leftover BorderCanvas from a previous attempt
        var oldCanvas = GameObject.Find("BorderCanvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        // ── Camera-child border sprites ───────────────────────────────────────
        // flipX=true  → vine at left screen edge, black toward grid
        SetupBorder("Border_Left",  camGO, vine, flipX: true,
                    localX: leftLocalX,  scaleXY: scaleLeft);
        // flipX=false → vine at right screen edge, black toward grid
        SetupBorder("Border_Right", camGO, vine, flipX: false,
                    localX: rightLocalX, scaleXY: scaleRight);

        // ── World-space edge shadows (unlit – no Global Light glow) ───────────
        var unlitMat = AssetDatabase.LoadAssetAtPath<Material>(UnlitMatPath);
        if (unlitMat == null)
            Debug.LogWarning("[BorderVine] Unlit material not found; shadows may glow.");

        float shadowW = CellSize * 1.5f;   // 1.44 u
        CreateEdgeShadow("Shadow_GridLeft",  gridLeft,  gridCenterY, gridHeight, shadowW, fromRight: false, unlitMat);
        CreateEdgeShadow("Shadow_GridRight", gridRight, gridCenterY, gridHeight, shadowW, fromRight: true,  unlitMat);

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[BorderVine] Done. Scene saved.");
    }

    // ── Camera-child border setup ─────────────────────────────────────────────
    static void SetupBorder(string goName, GameObject cameraGO, Sprite vine,
                             bool flipX, float localX, float scaleXY)
    {
        // Find or create the border GO; ensure it is a child of the camera
        var existing = cameraGO.transform.Find(goName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            // Might exist as a root object from a previous run
            var root = GameObject.Find(goName);
            if (root != null)
            {
                go = root;
                go.transform.SetParent(cameraGO.transform, worldPositionStays: false);
            }
            else
            {
                go = new GameObject(goName);
                go.transform.SetParent(cameraGO.transform, worldPositionStays: false);
            }
        }

        // Position in camera-local space:
        //   X = horizontal centre of this panel relative to camera centre
        //   Y = 0 (vertically centred on camera axis)
        //   Z = BorderLocalZ (in front of camera, past near-clip plane)
        go.transform.localPosition = new Vector3(localX, 0f, BorderLocalZ);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = new Vector3(scaleXY, scaleXY, 1f);

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        sr.enabled          = true;
        sr.sprite           = vine;
        sr.flipX            = flipX;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = -500;   // render behind all game objects

        EditorUtility.SetDirty(go);
        Debug.Log($"[BorderVine] {goName}: localX={localX:F2} scale={scaleXY:F3} flipX={flipX}");
    }

    // ── World-space gradient shadow on a grid edge ────────────────────────────
    // fromRight=false → shadow grows from LEFT edge inward (left border shadow)
    // fromRight=true  → shadow grows from RIGHT edge inward (right border shadow)
    static void CreateEdgeShadow(string goName, float edgeX, float centerY,
                                  float height, float width, bool fromRight,
                                  Material unlitMat)
    {
        var existing = GameObject.Find(goName);
        if (existing != null) Object.DestroyImmediate(existing);

        // 64×2 gradient: cubic ease for a soft inner fade, no harsh cutoff
        int tw = 64, th = 2;
        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < tw; x++)
        {
            float t = fromRight ? (float)x / (tw - 1) : 1f - (float)x / (tw - 1);
            t = t * t * t;   // cubic ease
            for (int y = 0; y < th; y++)
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, t * 0.95f));
        }
        tex.Apply();

        string texPath = $"Assets/_Project/Art/Sprites/Background/{goName}_grad.png";
        System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

        var texImp = (TextureImporter)AssetImporter.GetAtPath(texPath);
        texImp.textureType         = TextureImporterType.Sprite;
        texImp.spriteImportMode    = SpriteImportMode.Single;
        texImp.spritePixelsPerUnit = 100;
        texImp.filterMode          = FilterMode.Bilinear;
        texImp.wrapMode            = TextureWrapMode.Clamp;
        texImp.mipmapEnabled       = false;
        texImp.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (sprite == null) { Debug.LogError("[BorderVine] Shadow sprite not found: " + texPath); return; }

        var go = new GameObject(goName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;


        // Unlit material → Global Light 2D has no effect → no "brillo"
        if (unlitMat != null) sr.material = unlitMat;

        // Sort above grid tiles (order 0) but below characters
        bool hasGround = SortingLayer.layers.Any(l => l.name == "Ground");
        sr.sortingLayerName = hasGround ? "Ground" : "Default";
        sr.sortingOrder     = 10;

        float nW = sprite.rect.width  / sprite.pixelsPerUnit;
        float nH = sprite.rect.height / sprite.pixelsPerUnit;
        go.transform.localScale = new Vector3(width / nW, height / nH, 1f);

        // Centre the shadow half-width inside the grid from the edge
        float cx = fromRight ? edgeX - width / 2f : edgeX + width / 2f;
        go.transform.position = new Vector3(cx, centerY, 0f);

        EditorUtility.SetDirty(go);
        Debug.Log($"[BorderVine] Shadow '{goName}' → {(unlitMat != null ? "unlit" : "lit-fallback")} at x={cx:F2}");
    }
}
