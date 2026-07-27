using System;
using System.Collections.Generic;

namespace WuxiaRoguelite.Domain.Characters
{
    public sealed class CharacterRuntime
    {
        private readonly List<StatModifier> modifiers = new List<StatModifier>();

        public CharacterRuntime(string configId, CharacterStats baseStats)
        {
            if (string.IsNullOrWhiteSpace(configId))
            {
                throw new ArgumentException("角色配置 ID 不能为空。", nameof(configId));
            }

            ConfigId = configId;
            BaseStats = baseStats ?? throw new ArgumentNullException(nameof(baseStats));
            Stats = BaseStats;
            CurrentHealth = Stats.MaxHealth;
        }

        public string ConfigId { get; }
        public CharacterStats BaseStats { get; }
        public CharacterStats Stats { get; private set; }
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }

            modifiers.RemoveAll(item => item.SourceId == modifier.SourceId && item.Stat == modifier.Stat);
            modifiers.Add(modifier);
            RecalculateStats();
        }

        public void RemoveModifiersFrom(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            if (modifiers.RemoveAll(item => item.SourceId == sourceId) > 0)
            {
                RecalculateStats();
            }
        }

        public void TakeDamage(float amount)
        {
            float reduced = Math.Max(0f, amount) * (1f - Stats.DamageReduction);
            CurrentHealth = Math.Max(0f, CurrentHealth - reduced);
        }

        public void Heal(float amount)
        {
            CurrentHealth = Math.Min(Stats.MaxHealth, CurrentHealth + Math.Max(0f, amount));
        }

        public void ResetHealth()
        {
            CurrentHealth = Stats.MaxHealth;
        }

        private void RecalculateStats()
        {
            float healthRatio = Stats.MaxHealth <= 0f ? 0f : CurrentHealth / Stats.MaxHealth;
            Stats = BaseStats.Apply(modifiers);
            CurrentHealth = Math.Max(0f, Math.Min(Stats.MaxHealth, Stats.MaxHealth * healthRatio));
        }
    }
}
