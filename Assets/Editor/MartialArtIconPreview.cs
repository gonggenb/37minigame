using System.IO;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.Editor
{
    public sealed class MartialArtIconPreview : EditorWindow
    {
        private Vector2 scroll;
        private int sizeIndex;
        private static readonly int[] Sizes = { 64, 48, 32 };
        private static readonly string[] SizeLabels = { "64 px", "48 px", "32 px" };

        [InitializeOnLoadMethod]
        private static void RegisterCleanup()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= MartialArtIconRenderer.ClearCache;
            AssemblyReloadEvents.beforeAssemblyReload += MartialArtIconRenderer.ClearCache;
        }

        [MenuItem("37 MiniGame/Preview School Icons")]
        public static void Open()
        {
            var window = GetWindow<MartialArtIconPreview>("School Icons");
            window.minSize = new Vector2(780, 610);
            window.Show();
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();
            var label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            RuntimeChineseFont.Apply(label);
            GUIStyle button = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button));
            GUILayout.Label("Original (left) / School effect (right). Static effects; no continuous flashing.", label);
            sizeIndex = GUILayout.Toolbar(sizeIndex, SizeLabels, button);
            if (GUILayout.Button("Export 25 icons / validate GPU output", button)) Export();
            scroll = GUILayout.BeginScrollView(scroll);
            for (int row = 0; row < 6; row++)
            {
                Rect strip = GUILayoutUtility.GetRect(760, 88);
                int count = row == 5 ? 5 : 4;
                for (int column = 0; column < count; column++)
                {
                    string id = row == 5 ? MartialArtCatalog.AllSecretIds[column] : MartialArtCatalog.AllIds[row * 4 + column];
                    float x = strip.x + column * 150;
                    Texture2D original = Load(id);
                    int size = Sizes[sizeIndex];
                    Rect before = new Rect(x, strip.y, 64, 64);
                    Rect after = new Rect(x + 70, strip.y, 64, 64);
                    WuxiaUiTheme.DrawSlot(before, WuxiaUiTheme.BackgroundInk, WuxiaUiTheme.Brass);
                    WuxiaUiTheme.DrawSlot(after, WuxiaUiTheme.BackgroundInk, MartialArtIconRenderer.Accent(id));
                    if (original != null)
                    {
                        GUI.DrawTexture(new Rect(before.center.x - size / 2f, before.center.y - size / 2f, size, size), original);
                        GUI.DrawTexture(new Rect(after.center.x - size / 2f, after.center.y - size / 2f, size, size), MartialArtIconRenderer.Get(original, id));
                    }
                    GUI.Label(new Rect(x, strip.y + 64, 136, 22), id, label);
                }
            }
            GUILayout.EndScrollView();
        }

        private static Texture2D Load(string id) => Resources.Load<Texture2D>("Icons/" + ContentIconCatalog.MartialArt(id));

        private static void Export()
        {
            string directory = Path.GetFullPath("ArtSource/Previews/UI/SchoolIcons_20260905");
            Directory.CreateDirectory(directory);
            int count = 0;
            foreach (string id in System.Linq.Enumerable.Concat(MartialArtCatalog.AllIds, MartialArtCatalog.AllSecretIds))
            {
                Texture2D source = Load(id);
                var rendered = MartialArtIconRenderer.Get(source, id) as RenderTexture;
                if (source == null || rendered == null || !rendered.IsCreated())
                    throw new System.InvalidOperationException("School icon GPU output missing: " + id);
                RenderTexture previous = RenderTexture.active;
                var copy = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = rendered;
                    copy.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
                    copy.Apply();
                    File.WriteAllBytes(Path.Combine(directory, ContentIconCatalog.MartialArt(id) + ".png"), copy.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous;
                    DestroyImmediate(copy);
                }
                count++;
            }
            Debug.Log("School icon GPU validation passed: " + count + " icons. Preview exports: " + directory);
        }
    }
}
