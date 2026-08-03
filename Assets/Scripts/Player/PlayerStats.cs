using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Player
{
    public class PlayerStats : MonoBehaviour
    {
        public readonly struct TimedBuffSnapshot
        {
            public readonly string id;
            public readonly string displayName;
            public readonly string effectSummary;
            public readonly string iconId;
            public readonly float remainingDuration;
            public readonly float totalDuration;
            public readonly int stackCount;

            public TimedBuffSnapshot(
                string id,
                string displayName,
                string effectSummary,
                string iconId,
                float remainingDuration,
                float totalDuration,
                int stackCount)
            {
                this.id = id;
                this.displayName = displayName;
                this.effectSummary = effectSummary;
                this.iconId = iconId;
                this.remainingDuration = remainingDuration;
                this.totalDuration = totalDuration;
                this.stackCount = stackCount;
            }

            public float RemainingRatio => totalDuration <= 0f
                ? 0f
                : Mathf.Clamp01(remainingDuration / totalDuration);
        }

        private sealed class TimedMoveSpeedBuff
        {
            public float ratio;
            public float remainingDuration;
            public float totalDuration;
        }

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
        private readonly List<TimedMoveSpeedBuff> temporaryMoveSpeedBuffs =
            new List<TimedMoveSpeedBuff>();

        public float CurrentMoveSpeed
        {
            get
            {
                if (runtimeStats == null)
                {
                    return 0f;
                }

                float totalBonusRatio = 0f;
                foreach (TimedMoveSpeedBuff buff in temporaryMoveSpeedBuffs)
                {
                    totalBonusRatio += buff.ratio;
                }

                return runtimeStats.moveSpeed * (1f + totalBonusRatio);
            }
        }

        public int ActiveTemporaryMoveSpeedBuffCount => temporaryMoveSpeedBuffs.Count;

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
            ClearTemporaryMoveSpeedBuffs();
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

        public void ApplyAttackBuff(float ratio)
        {
            runtimeStats.attack *= 1f + Mathf.Max(0f, ratio);
        }

        public void ApplyDefenseBuff(float amount)
        {
            runtimeStats.defense += Mathf.Max(0f, amount);
        }

        public void ApplyMoveSpeedBuff(float ratio)
        {
            runtimeStats.moveSpeed *= 1f + Mathf.Max(0f, ratio);
        }

        public bool ApplyTemporaryMoveSpeedBuff(float ratio, float duration)
        {
            float safeRatio = Mathf.Clamp(ratio, 0f, 1f);
            float safeDuration = Mathf.Clamp(duration, 0f, 3f);
            if (safeRatio <= 0f || safeDuration <= 0f)
            {
                return false;
            }

            temporaryMoveSpeedBuffs.Add(new TimedMoveSpeedBuff
            {
                ratio = safeRatio,
                remainingDuration = safeDuration,
                totalDuration = safeDuration
            });
            return true;
        }

        /// <summary>
        /// Copies current timed effects into a caller-owned buffer so the HUD can
        /// render them without allocating every IMGUI pass. Effects of the same
        /// type are grouped and the timer shows the next stack that will expire.
        /// Future timed buffs should add another snapshot here while keeping their
        /// gameplay ownership in PlayerStats (or the system that applies them).
        /// </summary>
        public void GetTimedBuffSnapshots(List<TimedBuffSnapshot> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();
            if (temporaryMoveSpeedBuffs.Count == 0)
            {
                return;
            }

            float totalRatio = 0f;
            float nextExpiry = float.MaxValue;
            float nextExpiryDuration = 0f;
            foreach (TimedMoveSpeedBuff buff in temporaryMoveSpeedBuffs)
            {
                totalRatio += buff.ratio;
                if (buff.remainingDuration < nextExpiry)
                {
                    nextExpiry = buff.remainingDuration;
                    nextExpiryDuration = buff.totalDuration;
                }
            }

            buffer.Add(new TimedBuffSnapshot(
                "normal_victory_move_speed",
                "乘胜轻身",
                $"移速 +{Mathf.RoundToInt(totalRatio * 100f)}%",
                "疾剑式",
                Mathf.Max(0f, nextExpiry),
                Mathf.Max(0.01f, nextExpiryDuration),
                temporaryMoveSpeedBuffs.Count));
        }

        public void AdvanceTemporaryMoveSpeedBuffs(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (safeDeltaTime <= 0f)
            {
                return;
            }

            for (int i = temporaryMoveSpeedBuffs.Count - 1; i >= 0; i--)
            {
                TimedMoveSpeedBuff buff = temporaryMoveSpeedBuffs[i];
                buff.remainingDuration -= safeDeltaTime;
                if (buff.remainingDuration <= 0f)
                {
                    temporaryMoveSpeedBuffs.RemoveAt(i);
                }
            }
        }

        public void ClearTemporaryMoveSpeedBuffs()
        {
            temporaryMoveSpeedBuffs.Clear();
        }

        public void ApplyMysteryPoison(float healthLossRatio)
        {
            float damage = runtimeStats.maxHealth * Mathf.Clamp01(healthLossRatio);
            runtimeStats.currentHealth = Mathf.Max(1f, runtimeStats.currentHealth - damage);
        }

        public void ApplyMysteryWeakness(float ratio)
        {
            float multiplier = 1f - Mathf.Clamp(ratio, 0f, 0.8f);
            runtimeStats.attack = Mathf.Max(1f, runtimeStats.attack * multiplier);
            runtimeStats.attackSpeed = Mathf.Max(0.25f, runtimeStats.attackSpeed * multiplier);
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
