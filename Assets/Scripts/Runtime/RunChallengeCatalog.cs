using UnityEngine;
using WuxiaRoguelite.Battle;

namespace WuxiaRoguelite.Runtime
{
    public enum BossApproach { None, Armored, Rapid, Fierce }

    public static class RunChallengeCatalog
    {
        public const int TierCount = 3;
        public static string TierName(int tier) => tier >= 2 ? GameTextCatalog.ChallengeDesperate :
            tier == 1 ? GameTextCatalog.ChallengeDangerous : GameTextCatalog.ChallengeTraining;
        public static string TierDescription(int tier) => tier >= 2
            ? "敌势更强 · 精英首击重撞 · 妖甲与狂暴后接狐火"
            : tier == 1 ? "敌势增强 · 精英开场护甲 · 妖甲后接狐火"
            : "六十息历练 · 中期强敌检验构筑";
        public static float EnemyHealth(int tier) => tier >= 2 ? 1.12f : tier == 1 ? 1.06f : 1f;
        public static float EnemyAttack(int tier) => tier >= 2 ? 1.08f : tier == 1 ? 1.04f : 1f;
        public static float BossHealth(int tier) => tier >= 2 ? 1.40f : tier == 1 ? 1.23f : 1.18f;
        public static float BossAttack(int tier) => tier >= 2 ? 1.20f : tier == 1 ? 1.12f : 1.08f;
        public static float MidBossHealth(int tier) => tier >= 2 ? 1.20f : tier == 1 ? 1.14f : 1.10f;
        public static float MidBossAttack(int tier) => tier >= 2 ? 1.12f : tier == 1 ? 1.07f : 1.04f;
        public static EnemyTrait EliteTrait(int tier) => tier >= 2 ? EnemyTrait.HeavyOpening :
            tier == 1 ? EnemyTrait.OpeningArmor : EnemyTrait.None;
        public static string ApproachName(BossApproach approach) => approach switch
        {
            BossApproach.Armored => GameTextCatalog.IntelArmored,
            BossApproach.Rapid => GameTextCatalog.IntelRapid,
            BossApproach.Fierce => GameTextCatalog.IntelFierce,
            _ => string.Empty
        };
        public static string ApproachDescription(BossApproach approach) => approach switch
        {
            BossApproach.Armored => "开战自带妖甲，七成气血时护甲更厚；攻击较弱。",
            BossApproach.Rapid => "普攻更快、狐火间隔缩短；单次攻击较弱。",
            BossApproach.Fierce => "普攻与狐火更痛，但出手较慢、首次狐火较晚。",
            _ => string.Empty
        };
        public static string ApproachAdvice(BossApproach approach) => approach switch
        {
            BossApproach.Armored => "可用快攻或持续毒伤破盾，也可补足续航。所有伤害均可削盾。",
            BossApproach.Rapid => "可用防御与反震承接连击，也可用闪避与恢复稳住气血。",
            BossApproach.Fierce => "可用气血与开场护盾承受爆发，也可抢先输出或提高闪避。",
            _ => string.Empty
        };
        public static string ApproachIcon(BossApproach approach) => approach switch
        {
            BossApproach.Armored => "intel_armor",
            BossApproach.Rapid => "intel_flurry",
            BossApproach.Fierce => "intel_burst",
            _ => "challenge_badge"
        };
        public static void ApplyBoss(CombatantStats stats, int tier, BossApproach approach, bool finalBoss)
        {
            stats.maxHealth *= finalBoss ? BossHealth(tier) : MidBossHealth(tier);
            stats.attack *= finalBoss ? BossAttack(tier) : MidBossAttack(tier);
            stats.challengeTier = tier;
            stats.bossApproach = finalBoss ? approach : BossApproach.None;
            if (finalBoss)
            {
                if (approach == BossApproach.Armored) stats.attack *= .90f;
                if (approach == BossApproach.Rapid) { stats.attack *= .85f; stats.attackSpeed *= 1.25f; }
                if (approach == BossApproach.Fierce) { stats.attack *= 1.15f; stats.attackSpeed *= .85f; }
            }
            stats.ResetHealth();
        }
    }
}
