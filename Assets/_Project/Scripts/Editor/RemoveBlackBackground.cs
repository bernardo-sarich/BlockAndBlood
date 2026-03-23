using UnityEditor;
using UnityEngine;
using System.IO;

public static class RemoveBlackBackground
{
    private const string SourcePath = "Assets/2f557103f59448839095bd648cd46971.jpg";
    private const string OutputPath = "Assets/_Project/Art/Sprites/2f557103f59448839095bd648cd46971_transparent.png";
    private const float Threshold = 0.12f; // píxeles con R,G,B todos < threshold → transparentes

    [MenuItem("Tools/Remove Black Background from 2f557103")]
    public static void Run()
    {
        // 1. Asegurar que la textura sea Read/Write
        var importer = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"No se encontró el importer para {SourcePath}");
            return;
        }

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        // 2. Cargar la textura
        var src = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
        if (src == null)
        {
            Debug.LogError($"No se pudo cargar {SourcePath}");
            return;
        }

        // 3. Copiar a textura RGBA con alfa
        var dst = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        Color[] pixels = src.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            if (c.r < Threshold && c.g < Threshold && c.b < Threshold)
                pixels[i] = Color.clear;
        }

        dst.SetPixels(pixels);
        dst.Apply();

        // 4. Restaurar estado original del importer
        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        // 5. Guardar como PNG
        string absOut = Path.Combine(Application.dataPath, "../", OutputPath);
        absOut = Path.GetFullPath(absOut);
        File.WriteAllBytes(absOut, dst.EncodeToPNG());
        Object.DestroyImmediate(dst);

        AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"Sprite generado en {OutputPath}");
        EditorUtility.RevealInFinder(OutputPath);
    }
}
