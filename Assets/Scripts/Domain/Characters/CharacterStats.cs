using System;
using System.Collections.Generic;

namespace WuxiaRoguelite.Domain.Characters
{
    [Serializable]
    public sealed class CharacterStats
    {
        public CharacterStats(
            float maxHealth,
            float attack,
            float defense,
            float attackSpeed,
            float critChance,
            float critMultiplier,
            float lifeSteal,
            float dodgeChance,
            float moveSpeed,
            float damageBonus = 0f,
            float damageReduction = 0f,
            float recovery = 0f)
        {
            MaxHealth = Math.Max(1f, maxHealth);
            Attack = Math.Max(0f, attack);
            Defense = Math.Max(0f, defense);
            AttackSpeed = Math.Max(0.1f, attackSpeed);
            CritChance = Clamp01(critChance);
            CritMultiplier = Math.Max(1f, critMultiplier);
            LifeSteal = Clamp01(lifeSteal);
            DodgeChance = Clamp01(dodgeChance);
            MoveSpeed = Math.Max(0f, moveSpeed);
            DamageBonus = Math.Max(-1f, damageBonus);
            DamageReduction = Clamp01(damageReduction);
            Recovery = Math.Max(0f, recovery);
        }

        public float MaxHealth { get; }
        public float Attack { get; }
        public float Defense { get; }
        public float AttackSpeed { get; }
        public float CritChance { get; }
        public float CritMultiplier { get; }
        public float LifeSteal { get; }
        public float DodgeChance { get; }
        public float MoveSpeed { get; }
        public float DamageBonus { get; }
        public float DamageReduction { get; }
        public float Recovery { get; }

        internal CharacterStats Apply(IReadOnlyList<StatModifier> modifiers)
        {
            return new CharacterStats(
                Modified(MaxHealth, StatType.MaxHealth, modifiers),
                Modified(Attack, StatType.Attack, modifiers),
                Modified(Defense, StatType.Defense, modifiers),
                Modified(AttackSpeed, StatType.AttackSpeed, modifiers),
                Modified(CritChance, StatType.CritChance, modifiers),
                Modified(CritMultiplier, StatType.CritMultiplier, modifiers),
                Modified(LifeSteal, StatType.LifeSteal, modifiers),
                Modified(DodgeChance, StatType.DodgeChance, modifiers),
                Modified(MoveSpeed, StatType.MoveSpeed, modifiers),
                Modified(DamageBonus, StatType.DamageBonus, modifiers),
                Modified(DamageReduction, StatType.DamageReduction, modifiers),
                Modified(Recovery, StatType.Recovery, modifiers));
        }

        private static float Modified(
            float baseValue,
            StatType stat,
            IReadOnlyList<StatModifier> modifiers)
        {
            float addition = 0f;
            float baseMultiplier = 0f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier modifier = modifiers[i];
                if (modifier.Stat != stat)
                {
                    continue;
                }

                if (modifier.Operation == ModifierOperation.MultiplyBase)
                {
                    baseMultiplier += modifier.Value;
                }
                else
                {
                    addition += modifier.Value;
                }
            }

            return baseValue + baseValue * baseMultiplier + addition;
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}
