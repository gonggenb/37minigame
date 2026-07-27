using NUnit.Framework;
using WuxiaRoguelite.Domain.Characters;

namespace WuxiaRoguelite.Tests.Core
{
    public sealed class CharacterRuntimeTests
    {
        [Test]
        public void StatModifiersRecalculateWithoutDestroyingCurrentHealthRatio()
        {
            var character = new CharacterRuntime(
                "player_wuxia",
                new CharacterStats(100f, 12f, 3f, 1f, 0.05f, 1.5f, 0f, 0.03f, 5f));
            character.TakeDamage(50f);

            character.AddModifier(new StatModifier("skill_iron_body", StatType.MaxHealth, 0.15f,
                ModifierOperation.MultiplyBase));

            Assert.That(character.Stats.MaxHealth, Is.EqualTo(115f).Within(0.001f));
            Assert.That(character.CurrentHealth, Is.EqualTo(57.5f).Within(0.001f));
        }

        [Test]
        public void ResetHealthRestoresConfiguredMaximum()
        {
            var character = new CharacterRuntime(
                "enemy_bandit",
                new CharacterStats(35f, 5f, 1f, 0.9f, 0.03f, 1.5f, 0f, 0f, 0f));
            character.TakeDamage(20f);

            character.ResetHealth();

            Assert.That(character.CurrentHealth, Is.EqualTo(35f));
        }

        [Test]
        public void MultiplyBaseAndAddModifiersAreOrderIndependent()
        {
            var first = new CharacterRuntime(
                "player_first",
                new CharacterStats(100f, 12f, 3f, 1f, 0.05f, 1.5f, 0f, 0.03f, 5f));
            first.AddModifier(new StatModifier("equipment_armor", StatType.MaxHealth, 20f, ModifierOperation.Add));
            first.AddModifier(new StatModifier("skill_iron_body", StatType.MaxHealth, 0.15f,
                ModifierOperation.MultiplyBase));

            var second = new CharacterRuntime(
                "player_second",
                new CharacterStats(100f, 12f, 3f, 1f, 0.05f, 1.5f, 0f, 0.03f, 5f));
            second.AddModifier(new StatModifier("skill_iron_body", StatType.MaxHealth, 0.15f,
                ModifierOperation.MultiplyBase));
            second.AddModifier(new StatModifier("equipment_armor", StatType.MaxHealth, 20f, ModifierOperation.Add));

            Assert.That(first.Stats.MaxHealth, Is.EqualTo(135f).Within(0.001f));
            Assert.That(second.Stats.MaxHealth, Is.EqualTo(135f).Within(0.001f));
        }
    }
}
