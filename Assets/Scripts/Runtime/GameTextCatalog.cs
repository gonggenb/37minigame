namespace WuxiaRoguelite.Runtime
{
    /// <summary>
    /// Canonical player-facing names that are shared across scenes and UI surfaces.
    /// Keep unique proper nouns here so serialized scenes and repeated copy cannot drift.
    /// </summary>
    public static class GameTextCatalog
    {
        public const string FinalBossName = "九尾妖狐";
        public const string FinalBossVisualId = "fox_demon_boss";
        public const string FinalBossPhaseOneName = "妖狐试锋";
        public const string FinalBossPhaseTwoName = "妖甲护体";
        public const string FinalBossPhaseThreeName = "残血狂暴";
        public const string FinalBossFoxfireSkillName = "狐火连击";
    }
}
