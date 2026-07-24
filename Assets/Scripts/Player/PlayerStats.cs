using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.MartialArts;
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
        public readonly Dictionary<string, int> martialArtRanks = new Dictionary<string, int>();

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
            martialArtRanks.Clear();
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

        public int GetMartialArtRank(string artId)
        {
            return !string.IsNullOrEmpty(artId) && martialArtRanks.TryGetValue(artId, out int rank)
                ? rank
                : 0;
        }

        public bool HasMartialArtSchool(MartialArtSchool school)
        {
            foreach (KeyValuePair<string, int> entry in martialArtRanks)
            {
                MartialArtDefinition definition = MartialArtCatalog.Get(entry.Key);
                if (entry.Value > 0 && definition != null && definition.school == school)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetMartialArtSchoolRank(MartialArtSchool school)
        {
            int total = 0;
            foreach (KeyValuePair<string, int> entry in martialArtRanks)
            {
                MartialArtDefinition definition = MartialArtCatalog.Get(entry.Key);
                if (definition != null && definition.school == school)
                {
                    total += entry.Value;
                }
            }

            return total;
        }

        public int ApplyMartialArt(string artId)
        {
            MartialArtDefinition definition = MartialArtCatalog.Get(artId);
            if (definition == null)
            {
                return 0;
            }

            int currentRank = GetMartialArtRank(artId);
            if (currentRank >= definition.maxRank)
            {
                return currentRank;
            }

            int newRank = currentRank + 1;
            martialArtRanks[artId] = newRank;
            if (currentRank == 0)
            {
                learnedMartialArts.Add(artId);
            }

            switch (artId)
            {
                case "疾剑式":
                    runtimeStats.attackSpeed += 0.12f;
                    break;
                case "铁布衫":
                    float healthGain = runtimeStats.maxHealth * 0.15f;
                    runtimeStats.maxHealth += healthGain;
                    runtimeStats.Heal(healthGain);
                    runtimeStats.defense += 1f;
                    break;
                case "吸星诀":
                    runtimeStats.lifeSteal = Mathf.Clamp01(runtimeStats.lifeSteal + 0.04f);
                    break;
            }

            return newRank;
        }
    }
}
