using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace WuxiaRoguelite.Editor
{
    public sealed class HeroExternalVfxImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/Resources/Effects/HeroExternal";

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(Folder + "/")) Configure((TextureImporter)assetImporter);
        }

        private static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 256f;
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

        [MenuItem("37 MiniGame/Art/Reimport Hero External VFX")]
        public static void ImportAll()
        {
            if (!Directory.Exists(Folder)) throw new DirectoryNotFoundException(Folder);
            string[] paths = Directory.GetFiles(Folder, "*.png");
            foreach (string path in paths)
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
                for (int index = 0; index < 6; index++)
                {
                    string name = basename + "_" + index.ToString("D2");
                    GUID id;
                    if (!ids.TryGetValue(name, out id)) id = GUID.Generate();
                    rects.Add(new SpriteRect { name = name, spriteID = id,
                        rect = new Rect(index * 256, 0, 256, 256),
                        alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f) });
                    pairs.Add(new SpriteNameFileIdPair(name, id));
                }
                provider.SetSpriteRects(rects.ToArray());
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                provider.Apply();
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Hero external VFX imported: " + paths.Length + " strips; six 256px frames, center pivot, Point.");
        }
    }
}
