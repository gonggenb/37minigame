using NUnit.Framework;
using WuxiaRoguelite.Application.Rewards;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class RewardServiceTests
    {
        [Test]
        public void RewardReturnsCurrenciesAndHealsConfiguredRatio()
        {
            var character = new CharacterRuntime(
                "player_wuxia",
                new CharacterStats(100f, 12f, 3f, 1f, 0.05f, 1.5f, 0f, 0.03f, 5f));
            character.TakeDamage(50f);
            var reward = new RewardConfig
            {
                id = "reward_treasure",
                cultivation = 18,
                copper = 10,
                healRatio = 0.25f,
                equipmentId = "equipment_qinggang_sword",
                martialArtId = "skill_sword_qi"
            };

            RewardResult result = new RewardService().Apply(reward, character);

            Assert.That(result.Cultivation, Is.EqualTo(18));
            Assert.That(result.Copper, Is.EqualTo(10));
            Assert.That(result.EquipmentId, Is.EqualTo("equipment_qinggang_sword"));
            Assert.That(result.MartialArtId, Is.EqualTo("skill_sword_qi"));
            Assert.That(character.CurrentHealth, Is.EqualTo(75f).Within(0.001f));
        }
    }
}
