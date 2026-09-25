using System;
using UnityEngine;

namespace WuxiaRoguelite.Visual
{
    /// <summary>Approved A1/B1/C1 six-frame external effects, cached outside combat rendering.</summary>
    public static class HeroExternalVfxArt
    {
        public const string ResourceRoot = "Effects/HeroExternal/";
        public const int FrameCount = 6;
        private static Sprite[][] clips;
        private static readonly string[] Ids = { "sword_qi", "palm_force", "heavy_cleave" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ReleaseCache() => clips = null;

        public static void Preload()
        {
            if (clips != null) return;
            clips = new Sprite[Ids.Length][];
            for (int index = 0; index < Ids.Length; index++)
            {
                clips[index] = Resources.LoadAll<Sprite>(ResourceRoot + "spr_vfx_hero_" + Ids[index] + "_6f_v01");
                Array.Sort(clips[index], (a, b) => string.CompareOrdinal(a.name, b.name));
                if (clips[index].Length != FrameCount)
                    Debug.LogError("Hero external VFX requires six sprites: " + Ids[index]);
            }
        }

        public static Sprite[] Frames(HeroAttackForm form)
        {
            if (form == HeroAttackForm.Basic) return Array.Empty<Sprite>();
            Preload();
            int index = form == HeroAttackForm.SwordQi ? 0 : form == HeroAttackForm.VenomPalm ? 1 : 2;
            return clips[index];
        }

        public static Sprite Frame(HeroAttackForm form, float progress)
        {
            Sprite[] frames = Frames(form);
            if (frames.Length != FrameCount || progress < 0f || progress >= 1f) return null;
            return frames[Mathf.Min(FrameCount - 1, Mathf.FloorToInt(progress * FrameCount))];
        }
    }
}
