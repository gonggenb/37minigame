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
        MountainBreaker = 4,
        DoubleCleave = 5,
        IronGuard = 6
    }

    /// <summary>
    /// Level-two midpoint power-check tuning. This encounter intentionally avoids
    /// the final boss's phase changes and build-specific counters.
    /// </summary>
    public static class MidBossTuning
    {
        public const float MaxHealth = 290f;
        public const float Attack = 13f;
        public const float TriggerElapsedTime = 30f;
        public const float WarningDuration = 5f;
        public const float OpeningSkillDelay = 4.5f;
        public const float SkillCooldown = 6f;
        public const float SkillAttackRatio = 1.30f;
        public const float SkillImpactDelay = 0.50f;
        public const float SkillVisualDuration = 0.80f;
        public const float DoubleCleaveAttackRatio = 0.65f;
        public const float DoubleCleaveFirstImpact = 0.35f;
        public const float DoubleCleaveSecondImpact = 0.80f;
        public const float DoubleCleaveDuration = 1.20f;
        public const float GuardHealthRatio = 0.50f;
        public const float GuardMaxHealthRatio = 0.08f;
        public const float GuardDuration = 3f;
        public const float GuardActionDuration = 0.80f;
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
        // Real action seconds, paused with gameplay; independent of combat-speed tuning.
        public const float FoxfireFirstImpact = 0.45f;
        public const float FoxfireImpactInterval = 0.20f;
        public const float FoxfireActionDuration = 1.16f;
        public const float FoxfireFlightDuration = 0.18f;
        public const float PhaseActionDuration = 0.80f;
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
