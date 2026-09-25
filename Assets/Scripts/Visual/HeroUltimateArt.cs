using System;
using UnityEngine;
using WuxiaRoguelite.MartialArts;

namespace WuxiaRoguelite.Visual
{
    public static class HeroUltimateArt
    {
        private static Texture2D[] textures;
        private static Sprite[][] poses;
        // Shared normalization scale per strip, from prepare_hero_ultimate_poses.cjs.
        private static readonly float[] BodyHeights = { 95f, 101f, 136f, 103f, 124f };
        private static readonly float[] PoseEnds = { .055f, .16f, .26f, .38f, .54f, .72f, .89f, 1f };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ReleaseCache() { textures = null; poses = null; }

        public static Sprite[] Frames(int index)
        {
            if (index < 0 || index >= MartialUltimateCatalog.ArtIds.Length) return Array.Empty<Sprite>();
            if (poses == null)
            {
                poses = new Sprite[MartialUltimateCatalog.ArtIds.Length][];
                for (int i = 0; i < poses.Length; i++)
                {
                    poses[i] = Resources.LoadAll<Sprite>(HeroAttackArt.ResourceRoot + "spr_hero_ultimate_" +
                        MartialUltimateCatalog.TextureIds[i] + "_right_8f_v01");
                    Array.Sort(poses[i], (a,b) => string.CompareOrdinal(a.name,b.name));
                    if (poses[i].Length != 8) Debug.LogError("Ultimate pose requires eight frames: " + MartialUltimateCatalog.ArtIds[i]);
                }
            }
            return poses[index];
        }

        public static float DisplayScale(int index) => index >= 0 && index < BodyHeights.Length ? 136f/BodyHeights[index] : 1f;
        public static int FrameIndex(float progress)
        {
            for (int i = 0; i < PoseEnds.Length; i++) if (progress < PoseEnds[i]) return i;
            return 7;
        }
        // Existing DrawFighter selects from uniform slots; map the authored timing
        // onto slot centers without changing its general enemy/ordinary playback.
        public static float SampleProgress(float progress) => (FrameIndex(progress)+.5f)/8f;
        public static Texture2D Texture(int index)
        {
            if (textures == null)
            {
                textures = new Texture2D[MartialUltimateCatalog.ArtIds.Length];
                for (int i=0;i<textures.Length;i++)
                {
                    textures[i] = Resources.Load<Texture2D>("HeroUltimates/vfx_ultimate_" + MartialUltimateCatalog.TextureIds[i] + "_v01");
                    if (textures[i] == null) Debug.LogError("Missing ultimate VFX: " + MartialUltimateCatalog.TextureIds[i]);
                }
            }
            return index >= 0 && index < textures.Length ? textures[index] : null;
        }
    }
}
