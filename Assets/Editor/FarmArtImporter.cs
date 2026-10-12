using UnityEditor;
using UnityEngine;

// Préserve l'alpha de l'atlas et permet son découpage au démarrage.
public sealed class FarmArtImporter : AssetPostprocessor
{
    static bool IsFarmArt(string path) => path == "Assets/Resources/Art/village-atlas.png" || path == "Assets/Resources/Art/clearing-ground.png" || path == "Assets/Resources/Art/extra-crops.png";
    void OnPreprocessTexture() { if (IsFarmArt(assetPath)) Configure((TextureImporter)assetImporter); }
    static void Configure(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = !importer.assetPath.EndsWith("clearing-ground.png");
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
    [InitializeOnLoadMethod]
    static void EnsureImported()
    {
        EditorApplication.delayCall += () => {
            foreach (var path in new[] { "Assets/Resources/Art/village-atlas.png", "Assets/Resources/Art/clearing-ground.png", "Assets/Resources/Art/extra-crops.png" }) {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled ||
                    importer.npotScale != TextureImporterNPOTScale.None || !importer.alphaIsTransparency ||
                    (!path.EndsWith("clearing-ground.png") && !importer.isReadable)) {
                    Configure(importer); importer.SaveAndReimport();
                }
            }
        };
    }
}
