using System;
using System.Collections.Generic;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;

namespace WuxiaRoguelite.Domain.Configuration
{
    public enum CharacterKind
    {
        Player,
        Enemy,
        EliteEnemy,
        CaveEnemy,
        Boss
    }

    public enum MartialArtSchool
    {
        SwiftSword,
        VenomPalm,
        IronBody
    }

    public enum SpawnEntityType
    {
        Enemy,
        Treasure,
        Herb,
        Cave
    }

    [Serializable]
    public sealed class CharacterConfig
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public CharacterKind kind;
        public string visualId = string.Empty;
        public float maxHealth = 100f;
        public float attack = 10f;
        public float defense;
        public float attackSpeed = 1f;
        public float critChance = 0.05f;
        public float critMultiplier = 1.5f;
        public float lifeSteal;
        public float dodgeChance;
        public float moveSpeed = 5f;
    }

    [Serializable]
    public sealed class MartialArtConfig
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public MartialArtSchool school;
        public bool isStarter;
        public int maxRank = 3;
        public CombatEffectType effectType;
        public StatType primaryStat;
        public ModifierOperation primaryOperation;
        public StatType secondaryStat;
        public ModifierOperation secondaryOperation;
        public float[] magnitudes = Array.Empty<float>();
        public float[] secondaryValues = Array.Empty<float>();
        public int[] triggerIntervals = Array.Empty<int>();
        public int[] maxStacks = Array.Empty<int>();
        public string description = string.Empty;

        public float MagnitudeAtRank(int rank)
        {
            return ValueAtRank(magnitudes, rank);
        }

        public float SecondaryAtRank(int rank)
        {
            return ValueAtRank(secondaryValues, rank);
        }

        public int TriggerIntervalAtRank(int rank)
        {
            return ValueAtRank(triggerIntervals, rank);
        }

        public int MaxStacksAtRank(int rank)
        {
            return ValueAtRank(maxStacks, rank);
        }

        private static float ValueAtRank(float[] values, int rank)
        {
            if (values == null || values.Length == 0)
            {
                return 0f;
            }

            int index = Math.Max(0, Math.Min(values.Length - 1, rank - 1));
            return values[index];
        }

        private static int ValueAtRank(int[] values, int rank)
        {
            if (values == null || values.Length == 0)
            {
                return 0;
            }

            int index = Math.Max(0, Math.Min(values.Length - 1, rank - 1));
            return values[index];
        }
    }

    [Serializable]
    public sealed class EquipmentConfig
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string slot = string.Empty;
        public string rarity = string.Empty;
        public float attackBonus;
        public float defenseBonus;
        public float maxHealthBonus;
        public float attackSpeedBonus;
        public float critChanceBonus;
        public float dodgeChanceBonus;
        public CombatEffectType effectType;
        public float magnitude;
        public float secondaryValue;
        public int triggerInterval;
        public int maxStacks;
        public string description = string.Empty;
    }

    [Serializable]
    public sealed class RewardConfig
    {
        public string id = string.Empty;
        public int cultivation;
        public int copper;
        public float healRatio;
        public string equipmentId = string.Empty;
        public string martialArtId = string.Empty;
    }

    [Serializable]
    public sealed class SpawnConfig
    {
        public string id = string.Empty;
        public string regionId = string.Empty;
        public SpawnEntityType entityType;
        public string configId = string.Empty;
        public string prefabId = string.Empty;
        public string rewardId = string.Empty;
        public int minCount;
        public int maxCount;
        public float weight = 1f;
    }

    [Serializable]
    public sealed class GameConfigSet
    {
        public List<CharacterConfig> characters = new List<CharacterConfig>();
        public List<MartialArtConfig> martialArts = new List<MartialArtConfig>();
        public List<EquipmentConfig> equipment = new List<EquipmentConfig>();
        public List<RewardConfig> rewards = new List<RewardConfig>();
        public List<SpawnConfig> spawns = new List<SpawnConfig>();
    }
}
