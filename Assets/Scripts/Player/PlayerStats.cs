using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Player
{
    public class PlayerStats : MonoBehaviour
    {
        public CombatantStats baseStats = new CombatantStats
        {
            displayName = "无名少侠",
            maxHealth = 100f,
            currentHealth = 100f,
            attack = 12f,
            defense = 3f,
            attackSpeed = 1f,
            critChance = 0.05f,
            critMultiplier = 1.5f,
            lifeSteal = 0f,
            dodgeChance = 0.03f,
            moveSpeed = 5f
        };

        public CombatantStats runtimeStats;
        public PlayerEquipment equipment;
        public int level = 1;
        public int cultivation;
        public int copper;
        public int killCount;
        public int caveEntries;
        public readonly List<string> learnedMartialArts = new List<string>();

        private readonly int[] levelRequirements = { 20, 35, 55, 80, 120 };

        public int NextLevelRequirement
        {
            get
            {
                int index = Mathf.Clamp(level - 1, 0, levelRequirements.Length - 1);
                return levelRequirements[index];
            }
        }

        public void ResetRun()
        {
            runtimeStats = baseStats.Clone();
            runtimeStats.ResetHealth();
            equipment = equipment == null ? GetComponent<PlayerEquipment>() : equipment;
            level = 1;
            cultivation = 0;
            copper = 0;
            killCount = 0;
            caveEntries = 0;
            learnedMartialArts.Clear();
            equipment?.ResetRun(this);
        }

        private void Awake()
        {
            if (runtimeStats == null)
            {
                ResetRun();
            }
        }

        public bool GainCultivation(int amount)
        {
            cultivation += Mathf.Max(0, amount);
            if (cultivation < NextLevelRequirement)
            {
                return false;
            }

            cultivation -= NextLevelRequirement;
            level += 1;
            return true;
        }

        public void GainCopper(int amount)
        {
            copper += Mathf.Max(0, amount);
        }

        public bool TrySpendCopper(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (copper < amount)
            {
                return false;
            }

            copper -= amount;
            return true;
        }

        public void HealPercent(float ratio)
        {
            runtimeStats.Heal(runtimeStats.maxHealth * Mathf.Clamp01(ratio));
        }

        public string GrantTreasureEquipment()
        {
            return equipment == null ? string.Empty : equipment.AddTreasureItem();
        }

        public void ApplyMartialArt(string artId)
        {
            if (string.IsNullOrEmpty(artId))
            {
                return;
            }

            learnedMartialArts.Add(artId);

            switch (artId)
            {
                case "剑气诀":
                    runtimeStats.attack += 5f;
                    break;
                case "疾剑式":
                    runtimeStats.attackSpeed += 0.18f;
                    break;
                case "铁布衫":
                    float healthGain = runtimeStats.maxHealth * 0.25f;
                    runtimeStats.maxHealth += healthGain;
                    runtimeStats.Heal(healthGain);
                    runtimeStats.defense += 2f;
                    break;
                case "吸星诀":
                    runtimeStats.lifeSteal += 0.08f;
                    break;
                case "毒砂掌":
                    runtimeStats.attack += 3f;
                    runtimeStats.critChance += 0.03f;
                    break;
                case "破甲掌":
                    runtimeStats.attack += 7f;
                    break;
            }
        }
    }
}
