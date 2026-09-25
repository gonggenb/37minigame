using UnityEngine;

namespace WuxiaRoguelite.Runtime
{
    // Stable order is part of the v1 local save schema; append new skills, never reorder.
    public enum EndlessSkill { Vitality, Power, Guard, Haste, Precision, Evasion }
    public enum EndlessReward { Normal, Elite, Cave, MidBoss, Round }

    public static class EndlessProgressionCatalog
    {
        public const string Title = "无尽修炼";
        public const int CoinsPerPoint = 25;
        public const int MaxRank = 10;
        public const int SkillCount = 6;
        public const int BalanceLimit = 1000000000;
        public static readonly EndlessSkill[] Skills = {
            EndlessSkill.Vitality, EndlessSkill.Power, EndlessSkill.Guard,
            EndlessSkill.Haste, EndlessSkill.Precision, EndlessSkill.Evasion };
        public static bool IsValid(EndlessSkill skill) => (int)skill >= 0 && (int)skill < SkillCount;
        public static int UpgradeCost(int rank) => rank >= MaxRank ? 0 : 1 + Mathf.Max(0, rank) / 3;
        public static int RewardCoins(EndlessReward reward) => reward switch
        {
            EndlessReward.Normal => 2, EndlessReward.Elite => 5, EndlessReward.Cave => 4,
            EndlessReward.MidBoss => 12, EndlessReward.Round => 30, _ => 0
        };
        public static string Name(EndlessSkill skill) => skill switch
        {
            EndlessSkill.Vitality => "养元功", EndlessSkill.Power => "破锋诀", EndlessSkill.Guard => "护体功",
            EndlessSkill.Haste => "疾风诀", EndlessSkill.Precision => "凝神诀", EndlessSkill.Evasion => "灵燕步", _ => ""
        };
        // Reuse the shipped icon family, keeping stable icon ids separate from display names.
        public static string IconId(EndlessSkill skill) => skill switch
        {
            EndlessSkill.Vitality => "art_iron_shirt", EndlessSkill.Power => "art_sword_qi",
            EndlessSkill.Guard => "art_golden_bell", EndlessSkill.Haste => "art_swift_sword",
            EndlessSkill.Precision => "art_swan_strike", EndlessSkill.Evasion => "art_snowless_step", _ => ""
        };
        public static string Effect(EndlessSkill skill, int rank) => skill switch
        {
            EndlessSkill.Vitality => $"气血上限 +{rank * 5}%", EndlessSkill.Power => $"攻击 +{rank * 3}%",
            EndlessSkill.Guard => $"防御 +{rank * .5f:0.#}", EndlessSkill.Haste => $"攻速 +{rank * 2}%",
            EndlessSkill.Precision => $"暴击率 +{rank}%", EndlessSkill.Evasion => $"闪避率 +{rank}%", _ => ""
        };
        public static void Apply(CombatantStats stats, EndlessProgressionData data)
        {
            if (stats == null || data == null) return;
            stats.maxHealth *= 1 + data.Rank(EndlessSkill.Vitality) * .05f;
            stats.attack *= 1 + data.Rank(EndlessSkill.Power) * .03f;
            stats.defense += data.Rank(EndlessSkill.Guard) * .5f;
            stats.attackSpeed *= 1 + data.Rank(EndlessSkill.Haste) * .02f;
            stats.critChance = Mathf.Clamp01(stats.critChance + data.Rank(EndlessSkill.Precision) * .01f);
            stats.dodgeChance = Mathf.Clamp01(stats.dodgeChance + data.Rank(EndlessSkill.Evasion) * .01f);
            stats.ResetHealth();
        }
    }
}
