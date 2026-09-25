using System;
using UnityEngine;
using WuxiaRoguelite.Battle;

namespace WuxiaRoguelite.Visual
{
    public static class MartialProcVfxArt
    {
        public const int FrameCount = 6;
        public const string ResourceRoot = "Effects/MartialProcs/";
        private static readonly string[] Ids = { "armor_break", "swift_combo", "retaliation", "life_drain" };
        private static Sprite[][] clips;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ReleaseCache() => clips = null;

        public static void Preload()
        {
            if (clips != null) return;
            clips = new Sprite[Ids.Length][];
            for (int i = 0; i < Ids.Length; i++)
            {
                clips[i] = Resources.LoadAll<Sprite>(ResourceRoot + "spr_vfx_proc_" + Ids[i] + "_6f_v01");
                Array.Sort(clips[i], (a, b) => string.CompareOrdinal(a.name, b.name));
                if (clips[i].Length != FrameCount) Debug.LogError("Martial proc requires six sprites: " + Ids[i]);
            }
        }

        public static Sprite[] Frames(MartialProcKind kind) { Preload(); return clips[(int)kind]; }
        public static Sprite Frame(MartialProcKind kind, float progress)
        {
            var frames = Frames(kind);
            if (frames.Length != FrameCount || progress < 0 || progress >= 1) return null;
            return frames[Mathf.Min(FrameCount - 1, Mathf.FloorToInt(progress * FrameCount))];
        }
    }
}
