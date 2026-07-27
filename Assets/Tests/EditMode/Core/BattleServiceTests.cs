using NUnit.Framework;
using WuxiaRoguelite.Application.Combat;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class BattleServiceTests
    {
        [Test]
        public void PlayerAttackUsesDefenseAndAppliesTypedPoisonEffect()
        {
            var player = Character("player", 100f, 20f, 2f, 1f);
            var enemy = Character("enemy", 100f, 8f, 5f, 0.1f);
            var poison = new CombatEffectDefinition(
                "skill_venom_palm",
                CombatEffectType.PoisonOnHit,
                2f,
                maxStacks: 8);
            var battle = new BattleService(player, enemy, new FixedRandomSource(0.99f), new[] { poison });

            BattleAttackResult result = battle.ResolveAttack(BattleSide.Player);

            Assert.That(result.DamageToHealth, Is.EqualTo(15f).Within(0.001f));
            Assert.That(enemy.CurrentHealth, Is.EqualTo(85f).Within(0.001f));
            Assert.That(battle.EnemyPoisonStacks, Is.EqualTo(2));
        }

        [Test]
        public void OpeningShieldAbsorbsDamageBeforePlayerHealth()
        {
            var player = Character("player", 100f, 12f, 5f, 1f);
            var enemy = Character("enemy", 100f, 30f, 0f, 1f);
            var shield = new CombatEffectDefinition(
                "skill_golden_bell",
                CombatEffectType.OpeningShield,
                10f,
                secondaryValue: 1f);
            var battle = new BattleService(player, enemy, new FixedRandomSource(0.99f), new[] { shield });

            BattleAttackResult result = battle.ResolveAttack(BattleSide.Enemy);

            Assert.That(result.ShieldAbsorbed, Is.EqualTo(15f).Within(0.001f));
            Assert.That(result.DamageToHealth, Is.EqualTo(10f).Within(0.001f));
            Assert.That(player.CurrentHealth, Is.EqualTo(90f).Within(0.001f));
            Assert.That(battle.PlayerShield, Is.Zero);
        }

        [Test]
        public void TickUsesAttackSpeedWithoutUnityTime()
        {
            var player = Character("player", 100f, 20f, 0f, 1f);
            var enemy = Character("enemy", 100f, 1f, 0f, 0.1f);
            var battle = new BattleService(player, enemy, new FixedRandomSource(0.99f));

            battle.Tick(1.01f);

            Assert.That(enemy.CurrentHealth, Is.EqualTo(80f).Within(0.001f));
            Assert.That(player.CurrentHealth, Is.EqualTo(100f).Within(0.001f));
        }

        private static CharacterRuntime Character(
            string id,
            float health,
            float attack,
            float defense,
            float attackSpeed)
        {
            return new CharacterRuntime(
                id,
                new CharacterStats(health, attack, defense, attackSpeed, 0f, 1.5f, 0f, 0f, 5f));
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly float value;

            public FixedRandomSource(float value)
            {
                this.value = value;
            }

            public float Next01()
            {
                return value;
            }
        }
    }
}
