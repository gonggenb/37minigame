using System;
using UnityEngine;
using WuxiaRoguelite.Battle;

namespace WuxiaRoguelite.Visual
{
    public enum HeroAttackForm { Basic, SwordQi, VenomPalm, BloodCleave }

    public static class HeroAttackArt
    {
        public const string ResourceRoot = "Characters/HeroAttacks/";
        private static Sprite[][] clips;
        private static Sprite[] idle;
        private static Sprite[][] basicVariants;
        public static readonly string[] Ids = { "basic", "sword_qi", "venom_palm", "blood_cleave" };
        private static readonly string[] BasicVariantIds = { "jade_crescent", "golden_ember", "ink_afterimage" };
        public static int BasicVariantCount => BasicVariantIds.Length;

        public static void ReleaseCache() { ResetCache(); HeroUltimateArt.ReleaseCache(); HeroExternalVfxArt.ReleaseCache(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { clips = null; idle = null; basicVariants = null; }

        public static Sprite[] BasicFrames(int variant)
        {
            if (basicVariants == null)
            {
                basicVariants = new Sprite[BasicVariantCount][];
                for (int i = 0; i < basicVariants.Length; i++)
                {
                    basicVariants[i] = Resources.LoadAll<Sprite>(ResourceRoot + "spr_hero_attack_basic_" + BasicVariantIds[i] + "_right_8f_v01");
                    Array.Sort(basicVariants[i], (a, b) => string.CompareOrdinal(a.name, b.name));
                    if (basicVariants[i].Length != 8)
                        Debug.LogError("Hero basic attack variant requires eight sprites: " + BasicVariantIds[i]);
                }
            }
            return basicVariants[Mathf.Clamp(variant, 0, BasicVariantCount - 1)];
        }

        // Wide baked-in trails need more transparent canvas. Restore the same on-screen
        // body height around the foot pivot; never resize the shadow or combat layout.
        // Heights match Tools/ArtPipeline/prepare_hero_basic_variants.cjs.
        public static float BasicDisplayScale(int variant) => 136f / (variant == 2 ? 94f : 112f);

        public static Sprite[] Frames(HeroAttackForm form)
        {
            if (clips == null)
            {
                clips = new Sprite[Ids.Length][];
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i] = Resources.LoadAll<Sprite>(ResourceRoot + "spr_hero_attack_" + Ids[i] + "_right_8f_v01");
                    Array.Sort(clips[i], (a, b) => string.CompareOrdinal(a.name, b.name));
                    if (clips[i].Length != 8)
                        Debug.LogError("Hero attack strip requires eight sprites: " + Ids[i]);
                }
                idle = clips[0].Length == 8 ? new[] { clips[0][0] } : Array.Empty<Sprite>();
            }
            return clips[(int)form];
        }

        public static Sprite[] Idle { get { Frames(HeroAttackForm.Basic); return idle; } }

        // Burst beats persistent/on-hit effects in mixed builds. Poison ticks never call this.
        public static HeroAttackForm Select(BattleVfxCue cues)
        {
            if ((cues & (BattleVfxCue.BloodBurst | BattleVfxCue.OpeningStrike)) != 0) return HeroAttackForm.BloodCleave;
            if ((cues & (BattleVfxCue.SwordQi | BattleVfxCue.SwiftCombo)) != 0) return HeroAttackForm.SwordQi;
            if ((cues & (BattleVfxCue.PoisonApplied | BattleVfxCue.ArmorBreak)) != 0) return HeroAttackForm.VenomPalm;
            if ((cues & BattleVfxCue.BloodPower) != 0) return HeroAttackForm.BloodCleave;
            return HeroAttackForm.Basic;
        }

        public static float Duration(HeroAttackForm form)
        {
            switch (form)
            {
                case HeroAttackForm.SwordQi: return 0.40f;
                case HeroAttackForm.VenomPalm: return 0.52f;
                case HeroAttackForm.BloodCleave: return 0.60f;
                default: return 0.48f;
            }
        }
    }
}
