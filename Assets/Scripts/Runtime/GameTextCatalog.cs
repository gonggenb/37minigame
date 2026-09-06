namespace WuxiaRoguelite.Runtime
{
    /// <summary>
    /// Canonical player-facing names that are shared across scenes and UI surfaces.
    /// Keep unique proper nouns here so serialized scenes and repeated copy cannot drift.
    /// </summary>
    public static class GameTextCatalog
    {
        public const string GameTitle = "一炷江湖";
        public const string TutorialLevelName = "初入江湖";
        public const string MainLevelName = "驿路风云";
        public const string TutorialBossName = "山道恶霸";
        public const string TutorialBossVisualId = "orc_warlord";
        public const string MidBossName = "玄甲镇关使";
        public const string MidBossVisualId = "xuanjia_gate_warden";
        public const string MidBossSkillName = "镇关·震岳斩";
        public const string MidBossDoubleCleaveName = "横刀连破";
        public const string MidBossIronGuardName = "玄甲固守";
        public const string MidBossWardName = "玄甲";
        public const string FinalBossName = "九尾妖狐";
        public const string FinalBossVisualId = "fox_demon_boss";
        public const string FinalBossPhaseOneName = "妖狐试锋";
        public const string FinalBossPhaseTwoName = "妖甲护体";
        public const string FinalBossPhaseThreeName = "残血狂暴";
        public const string FinalBossFoxfireSkillName = "狐火连击";
    }
}
