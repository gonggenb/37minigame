namespace WuxiaRoguelite.GameFlow
{
    public enum BossApproachStage
    {
        None,
        Omen,
        Imminent,
        FinalCountdown,
        Arrived
    }

    public enum GamePhase
    {
        Ready,
        MainMapRunning,
        OpeningIntro,
        NormalBattleRunning,
        CaveRunning,
        LevelUpPaused,
        MidBossBattle,
        BossBattle,
        Result,
        TutorialLearning
    }
}
