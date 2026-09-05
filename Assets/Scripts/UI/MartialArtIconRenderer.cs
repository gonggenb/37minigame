using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.MartialArts;

namespace WuxiaRoguelite.UI
{
    /// <summary>
    /// Shared school palette and GPU-composited icon treatment. The original art stays intact.
    /// Cached, static effects remain legible with reduced motion and never obscure cooldown UI.
    /// </summary>
    public static class MartialArtIconRenderer
    {
        private static readonly Dictionary<(Texture2D, MartialArtSchool, MartialArtSchool), RenderTexture> Cache =
            new Dictionary<(Texture2D, MartialArtSchool, MartialArtSchool), RenderTexture>();
        private static Material material;
        private static bool shaderUnavailable;
        private static readonly int PrimaryColor = Shader.PropertyToID("_PrimaryColor");
        private static readonly int SecondaryColor = Shader.PropertyToID("_SecondaryColor");
        private static readonly int Schools = Shader.PropertyToID("_Schools");

        public static Color SchoolColor(MartialArtSchool school)
        {
            switch (school)
            {
                case MartialArtSchool.SwiftSword: return new Color32(101, 197, 222, 255);
                case MartialArtSchool.VenomPalm: return new Color32(131, 194, 91, 255);
                case MartialArtSchool.IronBody: return new Color32(224, 171, 79, 255);
                case MartialArtSchool.ShadowSteps: return new Color32(171, 143, 215, 255);
                case MartialArtSchool.BloodBlade: return new Color32(219, 100, 88, 255);
                default: return WuxiaUiTheme.Gold;
            }
        }

        public static Color Accent(string id)
        {
            return TryGetSchools(id, out MartialArtSchool first, out _) ? SchoolColor(first) : WuxiaUiTheme.Gold;
        }

        public static Texture Get(Texture2D source, string id)
        {
            if (source == null || !TryGetSchools(id, out MartialArtSchool first, out MartialArtSchool second))
                return source;

            var key = (source, first, second);
            if (Cache.TryGetValue(key, out RenderTexture cached) && cached != null && cached.IsCreated())
                return cached;

            if (shaderUnavailable) return source;
            if (material == null)
            {
                Shader shader = Resources.Load<Shader>("UI/Effects/MartialArtIcon");
                if (shader == null || !shader.isSupported)
                {
                    shaderUnavailable = true;
                    Debug.LogWarning("Martial art icon shader unavailable; using original icons.");
                    return source;
                }
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (cached != null) DestroyObject(cached);
            // One 128 px surface per art, no mipmaps, no per-frame material or texture allocations.
            var result = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB32)
            {
                name = "SchoolIcon_" + source.name,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false
            };
            if (!result.Create())
            {
                DestroyObject(result);
                shaderUnavailable = true;
                return source;
            }

            material.SetColor(PrimaryColor, SchoolColor(first));
            material.SetColor(SecondaryColor, SchoolColor(second));
            material.SetVector(Schools, new Vector4((int)first, (int)second, first == second ? 0f : 1f, 0f));
            RenderTexture previous = RenderTexture.active;
            try { Graphics.Blit(source, result, material); }
            finally { RenderTexture.active = previous; }
            Cache[key] = result;
            return result;
        }

        private static bool TryGetSchools(string id, out MartialArtSchool first, out MartialArtSchool second)
        {
            MartialArtDefinition art = MartialArtCatalog.Get(id);
            MartialArtSecretDefinition secret = MartialArtCatalog.GetSecret(id);
            first = art != null ? art.school : secret != null ? secret.firstSchool : default;
            second = secret != null ? secret.secondSchool : first;
            return art != null || secret != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ClearCache();
            Application.quitting -= ClearCache;
            Application.quitting += ClearCache;
        }

        public static void ClearCache()
        {
            foreach (RenderTexture texture in Cache.Values)
            {
                if (texture == null) continue;
                texture.Release();
                DestroyObject(texture);
            }
            Cache.Clear();
            DestroyObject(material);
            material = null;
            shaderUnavailable = false;
        }

        private static void DestroyObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
