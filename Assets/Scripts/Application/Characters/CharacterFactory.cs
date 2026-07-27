using System;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Application.Characters
{
    public sealed class CharacterFactory
    {
        public CharacterRuntime Create(CharacterConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new CharacterRuntime(
                config.id,
                new CharacterStats(
                    config.maxHealth,
                    config.attack,
                    config.defense,
                    config.attackSpeed,
                    config.critChance,
                    config.critMultiplier,
                    config.lifeSteal,
                    config.dodgeChance,
                    config.moveSpeed));
        }

        public void ApplyMartialArt(
            CharacterRuntime character,
            MartialArtConfig config,
            int rank)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string sourceId = "martial:" + config.id;
            character.RemoveModifiersFrom(sourceId);
            if (rank <= 0 || config.effectType != CombatEffectType.StatModifier)
            {
                return;
            }

            character.AddModifier(new StatModifier(
                sourceId,
                config.primaryStat,
                config.MagnitudeAtRank(rank),
                config.primaryOperation));

            float secondary = config.SecondaryAtRank(rank);
            if (Math.Abs(secondary) > 0.0001f)
            {
                character.AddModifier(new StatModifier(
                    sourceId,
                    config.secondaryStat,
                    secondary,
                    config.secondaryOperation));
            }
        }

        public void ApplyEquipment(CharacterRuntime character, EquipmentConfig config)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string sourceId = "equipment:" + config.id;
            character.RemoveModifiersFrom(sourceId);
            Add(character, sourceId, StatType.Attack, config.attackBonus);
            Add(character, sourceId, StatType.Defense, config.defenseBonus);
            Add(character, sourceId, StatType.MaxHealth, config.maxHealthBonus);
            Add(character, sourceId, StatType.AttackSpeed, config.attackSpeedBonus);
            Add(character, sourceId, StatType.CritChance, config.critChanceBonus);
            Add(character, sourceId, StatType.DodgeChance, config.dodgeChanceBonus);
        }

        public void RemoveEquipment(CharacterRuntime character, string equipmentId)
        {
            if (character == null || string.IsNullOrWhiteSpace(equipmentId))
            {
                return;
            }

            character.RemoveModifiersFrom("equipment:" + equipmentId);
        }

        public CombatEffectDefinition CreateCombatEffect(MartialArtConfig config, int rank)
        {
            if (config == null || rank <= 0 || config.effectType == CombatEffectType.None ||
                config.effectType == CombatEffectType.StatModifier)
            {
                return null;
            }

            return new CombatEffectDefinition(
                "martial:" + config.id,
                config.effectType,
                config.MagnitudeAtRank(rank),
                config.SecondaryAtRank(rank),
                config.TriggerIntervalAtRank(rank),
                config.MaxStacksAtRank(rank));
        }

        public CombatEffectDefinition CreateCombatEffect(EquipmentConfig config)
        {
            if (config == null || config.effectType == CombatEffectType.None ||
                config.effectType == CombatEffectType.StatModifier)
            {
                return null;
            }

            return new CombatEffectDefinition(
                "equipment:" + config.id,
                config.effectType,
                config.magnitude,
                config.secondaryValue,
                config.triggerInterval,
                config.maxStacks);
        }

        private static void Add(
            CharacterRuntime character,
            string sourceId,
            StatType stat,
            float value)
        {
            if (Math.Abs(value) > 0.0001f)
            {
                character.AddModifier(new StatModifier(sourceId, stat, value, ModifierOperation.Add));
            }
        }
    }
}
