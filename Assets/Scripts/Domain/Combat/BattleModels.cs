using System;

namespace WuxiaRoguelite.Domain.Combat
{
    public enum BattleSide
    {
        Player,
        Enemy
    }

    public enum CombatEffectType
    {
        None,
        SwordQi,
        PoisonOnHit,
        ArmorBreakOnHit,
        OpeningShield,
        Retaliation,
        LifeSteal,
        DodgeHeal,
        CriticalHaste,
        StatModifier
    }

    public interface IRandomSource
    {
        float Next01();
    }

    [Serializable]
    public sealed class CombatEffectDefinition
    {
        public CombatEffectDefinition()
        {
        }

        public CombatEffectDefinition(
            string sourceId,
            CombatEffectType effectType,
            float magnitude,
            float secondaryValue = 0f,
            int triggerInterval = 0,
            int maxStacks = 0)
        {
            this.sourceId = sourceId;
            this.effectType = effectType;
            this.magnitude = magnitude;
            this.secondaryValue = secondaryValue;
            this.triggerInterval = triggerInterval;
            this.maxStacks = maxStacks;
        }

        public string sourceId = string.Empty;
        public CombatEffectType effectType;
        public float magnitude;
        public float secondaryValue;
        public int triggerInterval;
        public int maxStacks;
    }

    public sealed class BattleAttackResult
    {
        public BattleAttackResult(
            BattleSide attacker,
            bool dodged,
            bool critical,
            float damageToHealth,
            float shieldAbsorbed,
            float bonusDamage)
        {
            Attacker = attacker;
            Dodged = dodged;
            Critical = critical;
            DamageToHealth = damageToHealth;
            ShieldAbsorbed = shieldAbsorbed;
            BonusDamage = bonusDamage;
        }

        public BattleSide Attacker { get; }
        public bool Dodged { get; }
        public bool Critical { get; }
        public float DamageToHealth { get; }
        public float ShieldAbsorbed { get; }
        public float BonusDamage { get; }
    }
}
