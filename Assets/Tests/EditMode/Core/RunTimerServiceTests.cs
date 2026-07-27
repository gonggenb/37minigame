using NUnit.Framework;
using WuxiaRoguelite.Application.Time;
using WuxiaRoguelite.Domain.GameFlow;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class RunTimerServiceTests
    {
        [Test]
        public void MainTimerTicksOnMapAndNormalBattleOnly()
        {
            var timer = new RunTimerService(60f);

            timer.Tick(GameState.MainMap, 5f, false);
            timer.Tick(GameState.NormalBattle, 4f, false);
            timer.Tick(GameState.Cave, 8f, false);
            timer.Tick(GameState.LevelUp, 8f, false);

            Assert.That(timer.MainTimeRemaining, Is.EqualTo(51f).Within(0.001f));
        }

        [Test]
        public void BossTimerIsIndependentFromMainTimer()
        {
            var timer = new RunTimerService(60f);
            timer.Tick(GameState.MainMap, 60f, false);

            timer.Tick(GameState.BossBattle, 7.5f, false);

            Assert.That(timer.MainTimeRemaining, Is.Zero);
            Assert.That(timer.BossBattleTime, Is.EqualTo(7.5f).Within(0.001f));
        }

        [Test]
        public void ExplicitPauseStopsEveryTimer()
        {
            var timer = new RunTimerService(60f);

            timer.Tick(GameState.MainMap, 10f, true);
            timer.Tick(GameState.BossBattle, 10f, true);

            Assert.That(timer.MainTimeRemaining, Is.EqualTo(60f));
            Assert.That(timer.BossBattleTime, Is.Zero);
        }
    }
}
