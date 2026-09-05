using UnityEditor;
using UnityEngine;

namespace WuxiaRoguelite.Editor
{
    /// <summary>
    /// Keeps the approved UI theme textures importable as single nine-slice sprites.
    /// Runtime IMGUI reads the same texture and border values through WuxiaUiTheme.
    /// </summary>
    public sealed class WuxiaUiThemeAssetImporter : AssetPostprocessor
    {
        private const string ThemeRoot = "Assets/Resources/UI/Theme/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ThemeRoot))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = assetPath.Contains("timer_dial") ? 256 : 128;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = assetPath.Contains("_v02")
                ? assetPath.Contains("timer_dial") ? Vector4.zero
                    : assetPath.Contains("button_") ? new Vector4(26, 14, 26, 14)
                    : assetPath.Contains("slot_") ? new Vector4(8, 8, 8, 8)
                    : new Vector4(14, 14, 14, 14)
                : assetPath.Contains("button_")
                ? new Vector4(26f, 14f, 26f, 14f)
                : assetPath.Contains("slot_")
                    ? new Vector4(16f, 16f, 16f, 16f)
                    : assetPath.Contains("panel_boss")
                        ? new Vector4(34f, 34f, 34f, 34f)
                        : assetPath.Contains("panel_paper")
                            ? new Vector4(32f, 32f, 32f, 32f)
                            : new Vector4(30f, 30f, 30f, 30f);
        }
    }
}
