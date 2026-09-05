namespace WuxiaRoguelite.Battle
{
    public enum BossBattlePhase
    {
        None = 0,
        Foxfire = 1,
        DemonArmor = 2,
        BloodFrenzy = 3
    }

    public enum BossSkillId
    {
        None = 0,
        FoxfireBarrage = 1,
        DemonArmor = 2,
        BloodFrenzy = 3,
        MountainBreaker = 4
    }

    /// <summary>
    /// Level-two midpoint power-check tuning. This encounter intentionally avoids
    /// the final boss's phase changes and build-specific counters.
    /// </summary>
    public static class MidBossTuning
    {
        public const float TriggerElapsedTime = 30f;
        public const float WarningDuration = 5f;
        public const float OpeningSkillDelay = 4.5f;
        public const float SkillCooldown = 6f;
        public const float SkillAttackRatio = 1.30f;
        public const float SkillImpactDelay = 0.50f;
        public const float SkillVisualDuration = 0.80f;
    }

    /// <summary>
    /// Central Boss V2 tuning. Keeping these values out of the combat loop makes
    /// phase, UI and adaptive-music thresholds share one source of truth.
    /// </summary>
    public static class BossV2Tuning
    {
        public const float PhaseTwoHealthRatio = 0.70f;
        public const float PhaseThreeHealthRatio = 0.35f;

        public const int FoxfireHitCount = 3;
        public const float FoxfireAttackRatioPerHit = 0.32f;
        public const float FoxfireDefenseRatioPerHit = 0.42f;
        public const float FoxfireLightnessDodgeBonusPerRank = 0.03f;
        public const float OpeningFoxfireDelay = 3.2f;
        public const float PhaseOneFoxfireCooldown = 5.8f;
        public const float PhaseTwoFoxfireCooldown = 4.8f;
        public const float PhaseThreeFoxfireCooldown = 3.8f;

        public const float DemonArmorMaxHealthRatio = 0.12f;
        public const float BloodFrenzyAttackMultiplier = 1.15f;
        public const float BloodFrenzyAttackSpeedMultiplier = 1.25f;
    }
}
