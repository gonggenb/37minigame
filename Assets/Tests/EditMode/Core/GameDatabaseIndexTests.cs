using System.Collections.Generic;
using NUnit.Framework;
using WuxiaRoguelite.Application.Configuration;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class GameDatabaseIndexTests
    {
        [Test]
        public void StableIdLookupReturnsConfiguredRows()
        {
            var data = new GameConfigSet();
            data.characters.Add(new CharacterConfig
            {
                id = "player_wuxia",
                displayName = "无名少侠",
                maxHealth = 100f,
                attack = 12f,
                attackSpeed = 1f,
                critMultiplier = 1.5f
            });
            data.rewards.Add(new RewardConfig { id = "reward_enemy", cultivation = 10 });

            var index = new GameDatabaseIndex(data);

            Assert.That(index.GetCharacter("player_wuxia").displayName, Is.EqualTo("无名少侠"));
            Assert.That(index.GetReward("reward_enemy").cultivation, Is.EqualTo(10));
            Assert.Throws<KeyNotFoundException>(() => index.GetCharacter("enemy_missing"));
        }
    }
}
