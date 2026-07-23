using System;
using UnityEngine;

namespace WuxiaRoguelite.Runtime
{
    [Serializable]
    public class CombatantStats
    {
        public string displayName = "少侠";
        public string visualId = string.Empty;
        [Min(0)] public int level;
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float attack = 12f;
        public float defense = 3f;
        public float attackSpeed = 1f;
        [Range(0f, 1f)] public float critChance = 0.05f;
        public float critMultiplier = 1.5f;
        [Range(0f, 1f)] public float lifeSteal = 0f;
        [Range(0f, 1f)] public float dodgeChance = 0f;
        public float moveSpeed = 5f;

        public bool IsDead => currentHealth <= 0f;
        public float HealthRatio => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        public int DisplayLevel
        {
            get
            {
                if (level > 0)
                {
                    return level;
                }

                float durability = maxHealth / 35f + defense / 3f;
                float damagePerSecond = attack * Mathf.Max(0.1f, attackSpeed) / 6f;
                return Mathf.Clamp(Mathf.RoundToInt((durability + damagePerSecond) * 0.85f), 1, 99);
            }
        }

        public CombatantStats Clone()
        {
            return new CombatantStats
            {
                displayName = displayName,
                visualId = visualId,
                level = level,
                maxHealth = maxHealth,
                currentHealth = currentHealth,
                attack = attack,
                defense = defense,
                attackSpeed = attackSpeed,
                critChance = critChance,
                critMultiplier = critMultiplier,
                lifeSteal = lifeSteal,
                dodgeChance = dodgeChance,
                moveSpeed = moveSpeed
            };
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount));
        }

        public void TakeDamage(float amount)
        {
            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
        }
    }
}
