using UnityEngine;

namespace WuxiaRoguelite.Battle
{
    public enum BossTalent { None, SwordGuard, VenomWard, ArmorPiercer, BloodSeal, KeenSight }

    /// <summary>One announced, bounded counter per boss. Never selected from the player's build.</summary>
    public static class BossTalentCatalog
    {
        public const int Count = 5;
        public const float PlayerDefenseEfficiency = 1.2f;
        public const float IronBodyDefensePerRank = 1.5f;
        public static BossTalent Roll() => (BossTalent)Random.Range(1, Count + 1);
        public static string Name(BossTalent talent) => talent switch
        {
            BossTalent.SwordGuard => "截剑势",
            BossTalent.VenomWard => "辟毒体",
            BossTalent.ArmorPiercer => "透甲劲",
            BossTalent.BloodSeal => "封血印",
            BossTalent.KeenSight => "照影眼",
            _ => "无天赋"
        };
        public static string Target(BossTalent talent) => talent switch
        {
            BossTalent.SwordGuard => "快剑", BossTalent.VenomWard => "毒掌",
            BossTalent.ArmorPiercer => "铁壁", BossTalent.BloodSeal => "血刀",
            BossTalent.KeenSight => "轻身", _ => "无"
        };
        public static string Description(BossTalent talent) => talent switch
        {
            BossTalent.SwordGuard => "剑气与连环剑伤害降低25%；可补毒伤或反震。",
            BossTalent.VenomWard => "毒伤降低30%，仍可叠毒破甲；可补剑气。",
            BossTalent.ArmorPiercer => "无视20%防御，反震伤害降低20%；可补气血与护盾。",
            BossTalent.BloodSeal => "战斗回血降低30%，暴击额外伤害降低25%；可补护盾。",
            BossTalent.KeenSight => "随机闪避率降低30%，残影必闪保留；可补防御。",
            _ => string.Empty
        };
        public static string Summary(BossTalent talent) => Name(talent) + " · 克制" + Target(talent);
        public static float DamageMultiplier(BossTalent talent, DamageSource source) =>
            talent == BossTalent.SwordGuard && (source == DamageSource.SwordQi || source == DamageSource.Combo) ? .75f :
            talent == BossTalent.VenomWard && source == DamageSource.Poison ? .70f :
            talent == BossTalent.ArmorPiercer && source == DamageSource.Retaliation ? .80f : 1f;
        public static float DefenseMultiplier(BossTalent talent) => talent == BossTalent.ArmorPiercer ? .8f : 1f;
        public static float DodgeMultiplier(BossTalent talent) => talent == BossTalent.KeenSight ? .7f : 1f;
        public static float HealingMultiplier(BossTalent talent) => talent == BossTalent.BloodSeal ? .7f : 1f;
        public static float CriticalMultiplier(BossTalent talent, float multiplier) =>
            1f + Mathf.Max(0f, multiplier - 1f) * (talent == BossTalent.BloodSeal ? .75f : 1f);
        public static float RetaliationRatio(int rank) => .70f + rank * .20f;
    }
}
