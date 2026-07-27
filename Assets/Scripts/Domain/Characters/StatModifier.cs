using System;

namespace WuxiaRoguelite.Domain.Characters
{
    public enum StatType
    {
        MaxHealth,
        Attack,
        Defense,
        AttackSpeed,
        CritChance,
        CritMultiplier,
        LifeSteal,
        DodgeChance,
        MoveSpeed,
        DamageBonus,
        DamageReduction,
        Recovery
    }

    public enum ModifierOperation
    {
        Add,
        MultiplyBase
    }

    public sealed class StatModifier
    {
        public StatModifier(string sourceId, StatType stat, float value, ModifierOperation operation)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("属性修正来源 ID 不能为空。", nameof(sourceId));
            }

            SourceId = sourceId;
            Stat = stat;
            Value = value;
            Operation = operation;
        }

        public string SourceId { get; }
        public StatType Stat { get; }
        public float Value { get; }
        public ModifierOperation Operation { get; }
    }
}
