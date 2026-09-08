using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace WuxiaRoguelite.Editor
{
    public sealed class HeroAttackArtImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Resources/Characters/HeroAttacks";

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(Folder + "/")) Configure((TextureImporter)assetImporter);
        }

        private static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 160f;
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("37 MiniGame/Art/Reimport Hero Attack Pack")]
        public static void ImportAll()
        {
            foreach (string path in Directory.GetFiles(Folder, "*.png"))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Configure(importer);
                importer.SaveAndReimport();
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
                provider.InitSpriteEditorDataProvider();
                var ids = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
                var rects = new List<SpriteRect>();
                var pairs = new List<SpriteNameFileIdPair>();
                string basename = Path.GetFileNameWithoutExtension(path);
                int count = 8;
                for (int i = 0; i < count; i++)
                {
                    string name = basename + "_" + i.ToString("D2");
                    GUID id;
                    if (!ids.TryGetValue(name, out id)) id = GUID.Generate();
                    rects.Add(new SpriteRect { name = name, spriteID = id,
                        rect = new Rect(i * 256, 0, 256, 256), alignment = SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0.125f) });
                    pairs.Add(new SpriteNameFileIdPair(name, id));
                }
                provider.SetSpriteRects(rects.ToArray());
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply();
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Hero attack pack imported: 4 strips, 32 frames; PPU 160, foot pivot, Point, FullRect.");
        }
    }
}
