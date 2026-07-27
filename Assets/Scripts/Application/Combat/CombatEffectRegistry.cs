using System;
using System.Collections.Generic;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;

namespace WuxiaRoguelite.Application.Combat
{
    public sealed class CombatEffectRegistry
    {
        public float CalculateOpeningShield(
            IReadOnlyList<CombatEffectDefinition> effects,
            CharacterRuntime player)
        {
            float shield = 0f;
            ForEach(effects, CombatEffectType.OpeningShield, effect =>
            {
                shield += Math.Max(0f, effect.magnitude) +
                          player.Stats.Defense * Math.Max(0f, effect.secondaryValue);
            });
            return shield;
        }

        public int PoisonStacksPerHit(IReadOnlyList<CombatEffectDefinition> effects)
        {
            int stacks = 0;
            ForEach(effects, CombatEffectType.PoisonOnHit,
                effect => stacks += Math.Max(0, (int)Math.Round(effect.magnitude)));
            return stacks;
        }

        public int PoisonStackLimit(IReadOnlyList<CombatEffectDefinition> effects)
        {
            int limit = 0;
            ForEach(effects, CombatEffectType.PoisonOnHit,
                effect => limit = Math.Max(limit, effect.maxStacks));
            return limit <= 0 ? 8 : limit;
        }

        public float PoisonDamagePerStack(IReadOnlyList<CombatEffectDefinition> effects)
        {
            float amount = 0.55f;
            ForEach(effects, CombatEffectType.PoisonOnHit, effect =>
            {
                if (effect.secondaryValue > 0f)
                {
                    amount = Math.Max(amount, effect.secondaryValue);
                }
            });
            return amount;
        }

        public float ArmorBreakPerHit(IReadOnlyList<CombatEffectDefinition> effects)
        {
            float amount = 0f;
            ForEach(effects, CombatEffectType.ArmorBreakOnHit,
                effect => amount += Math.Max(0f, effect.magnitude));
            return amount;
        }

        public float SwordQiDamage(
            IReadOnlyList<CombatEffectDefinition> effects,
            int successfulHits,
            float attack)
        {
            float damage = 0f;
            ForEach(effects, CombatEffectType.SwordQi, effect =>
            {
                int interval = Math.Max(1, effect.triggerInterval);
                if (successfulHits % interval == 0)
                {
                    damage += Math.Max(0f, effect.magnitude) * attack;
                }
            });
            return damage;
        }

        public float RetaliationDamage(
            IReadOnlyList<CombatEffectDefinition> effects,
            float defense)
        {
            float damage = 0f;
            ForEach(effects, CombatEffectType.Retaliation,
                effect => damage += Math.Max(0f, effect.magnitude) * defense);
            return damage;
        }

        public float AdditionalLifeSteal(IReadOnlyList<CombatEffectDefinition> effects)
        {
            float ratio = 0f;
            ForEach(effects, CombatEffectType.LifeSteal,
                effect => ratio += Math.Max(0f, effect.magnitude));
            return Math.Min(1f, ratio);
        }

        private static void ForEach(
            IReadOnlyList<CombatEffectDefinition> effects,
            CombatEffectType type,
            Action<CombatEffectDefinition> action)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                CombatEffectDefinition effect = effects[i];
                if (effect != null && effect.effectType == type)
                {
                    action(effect);
                }
            }
        }
    }
}
