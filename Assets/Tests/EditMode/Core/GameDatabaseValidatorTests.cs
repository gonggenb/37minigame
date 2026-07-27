using System.Linq;
using NUnit.Framework;
using WuxiaRoguelite.Application.Configuration;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class GameDatabaseValidatorTests
    {
        [Test]
        public void DuplicateIdsAndMissingSpawnReferencesAreErrors()
        {
            var data = new GameConfigSet();
            data.characters.Add(Character("enemy_bandit"));
            data.characters.Add(Character("enemy_bandit"));
            data.spawns.Add(new SpawnConfig
            {
                id = "spawn_missing_enemy",
                regionId = "east_forest",
                entityType = SpawnEntityType.Enemy,
                configId = "enemy_missing",
                prefabId = "prefab_enemy",
                minCount = 1,
                maxCount = 2,
                weight = 1f
            });

            ValidationReport report = new GameDatabaseValidator().Validate(data);

            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Errors.Any(issue => issue.message.Contains("重复 ID")), Is.True);
            Assert.That(report.Errors.Any(issue => issue.message.Contains("enemy_missing")), Is.True);
        }

        [Test]
        public void MinimumLinkedConfigurationPassesValidation()
        {
            var data = new GameConfigSet();
            data.characters.Add(Character("player_wuxia", CharacterKind.Player));
            data.characters.Add(Character("enemy_bandit"));
            data.rewards.Add(new RewardConfig { id = "reward_enemy", cultivation = 10, copper = 2 });
            data.spawns.Add(new SpawnConfig
            {
                id = "spawn_bandit",
                regionId = "east_forest",
                entityType = SpawnEntityType.Enemy,
                configId = "enemy_bandit",
                prefabId = "prefab_enemy",
                rewardId = "reward_enemy",
                minCount = 1,
                maxCount = 3,
                weight = 1f
            });

            ValidationReport report = new GameDatabaseValidator().Validate(data);

            Assert.That(report.IsValid, Is.True, string.Join("\n", report.Errors.Select(issue => issue.message)));
        }

        private static CharacterConfig Character(string id, CharacterKind kind = CharacterKind.Enemy)
        {
            return new CharacterConfig
            {
                id = id,
                displayName = id,
                kind = kind,
                maxHealth = 100f,
                attack = 10f,
                defense = 2f,
                attackSpeed = 1f,
                critChance = 0.05f,
                critMultiplier = 1.5f,
                moveSpeed = 5f
            };
        }
    }
}
