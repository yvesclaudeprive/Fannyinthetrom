using UnityEditor;

namespace FannyIn2Trom.EditorTools
{
    /// <summary>
    /// Règle automatiquement l'import des images déposées dans Assets/Resources/Art
    /// (et de l'icône) : pas de redimensionnement en puissance de 2, transparence conservée,
    /// bonne qualité sur iPhone.
    /// </summary>
    public class ArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Resources/Art/") && !path.StartsWith("Assets/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            if (path.StartsWith("Assets/Art/Icone/"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }
    }
}
