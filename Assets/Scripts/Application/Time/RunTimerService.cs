using System;
using WuxiaRoguelite.Domain.GameFlow;

namespace WuxiaRoguelite.Application.Time
{
    public sealed class RunTimerService
    {
        public RunTimerService(float mainTimeLimit)
        {
            if (mainTimeLimit <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(mainTimeLimit));
            }

            MainTimeLimit = mainTimeLimit;
            MainTimeRemaining = mainTimeLimit;
        }

        public float MainTimeLimit { get; }
        public float MainTimeRemaining { get; private set; }
        public float BossBattleTime { get; private set; }

        public void Reset()
        {
            MainTimeRemaining = MainTimeLimit;
            BossBattleTime = 0f;
        }

        public void Tick(GameState state, float deltaTime, bool explicitlyPaused)
        {
            if (explicitlyPaused || deltaTime <= 0f)
            {
                return;
            }

            if (state == GameState.MainMap || state == GameState.NormalBattle)
            {
                MainTimeRemaining = Math.Max(0f, MainTimeRemaining - deltaTime);
                return;
            }

            if (state == GameState.BossBattle)
            {
                BossBattleTime += deltaTime;
            }
        }
    }
}
