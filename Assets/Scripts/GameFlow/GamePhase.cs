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
        NormalBattleRunning,
        CaveRunning,
        LevelUpPaused,
        BossBattle,
        Result
    }
}
