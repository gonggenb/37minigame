using NUnit.Framework;
using WuxiaRoguelite.Application.Progression;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class ProgressionServiceTests
    {
        [Test]
        public void LargeCultivationRewardCanGrantMultipleLevels()
        {
            var progression = new ProgressionService(new[] { 20, 35, 55, 80, 120 });

            ProgressionResult result = progression.AddCultivation(1, 0, 120);

            Assert.That(result.Level, Is.EqualTo(4));
            Assert.That(result.Cultivation, Is.EqualTo(10));
            Assert.That(result.LevelsGained, Is.EqualTo(3));
        }
    }
}
