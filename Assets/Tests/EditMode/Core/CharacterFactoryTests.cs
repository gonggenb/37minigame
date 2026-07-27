using NUnit.Framework;
using WuxiaRoguelite.Application.Characters;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class CharacterFactoryTests
    {
        [Test]
        public void CharacterConfigCreatesRuntimeWithoutUnityTypes()
        {
            var config = new CharacterConfig
            {
                id = "player_wuxia",
                displayName = "无名少侠",
                maxHealth = 100f,
                attack = 12f,
                defense = 3f,
                attackSpeed = 1f,
                critChance = 0.05f,
                critMultiplier = 1.5f,
                dodgeChance = 0.03f,
                moveSpeed = 5f
            };

            CharacterRuntime character = new CharacterFactory().Create(config);

            Assert.That(character.ConfigId, Is.EqualTo("player_wuxia"));
            Assert.That(character.Stats.Attack, Is.EqualTo(12f));
            Assert.That(character.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void ReapplyingMartialArtRankReplacesPreviousTotalModifier()
        {
            var character = new CharacterRuntime(
                "player_wuxia",
                new CharacterStats(100f, 12f, 3f, 1f, 0.05f, 1.5f, 0f, 0.03f, 5f));
            var martialArt = new MartialArtConfig
            {
                id = "skill_iron_body",
                maxRank = 3,
                effectType = CombatEffectType.StatModifier,
                primaryStat = StatType.MaxHealth,
                primaryOperation = ModifierOperation.MultiplyBase,
                secondaryStat = StatType.Defense,
                secondaryOperation = ModifierOperation.Add,
                magnitudes = new[] { 0.15f, 0.30f, 0.45f },
                secondaryValues = new[] { 1f, 2f, 3f },
                triggerIntervals = new[] { 0, 0, 0 },
                maxStacks = new[] { 0, 0, 0 }
            };
            var factory = new CharacterFactory();

            factory.ApplyMartialArt(character, martialArt, 1);
            factory.ApplyMartialArt(character, martialArt, 2);

            Assert.That(character.Stats.MaxHealth, Is.EqualTo(130f).Within(0.001f));
            Assert.That(character.Stats.Defense, Is.EqualTo(5f).Within(0.001f));
        }
    }
}
