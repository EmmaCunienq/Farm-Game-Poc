using UnityEditor;
using UnityEngine;

// Préserve l'alpha de l'atlas et permet son découpage au démarrage.
public sealed class FarmArtImporter : AssetPostprocessor
{
    static readonly string[] ArtPaths={"Assets/Resources/Art/village-atlas.png","Assets/Resources/Art/clearing-ground.png","Assets/Resources/Art/extra-crops.png","Assets/Resources/Art/home-paper-atlas.png","Assets/Resources/Art/home-paper-sides.png","Assets/Resources/Art/home-painted-surfaces.png","Assets/Resources/Art/farmer-paper-parts.png"};
    static bool IsFarmArt(string path) => System.Array.IndexOf(ArtPaths,path)>=0;
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
            foreach (var path in ArtPaths) {
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
