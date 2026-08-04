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
        public readonly List<string> unlockedSecrets = new List<string>();
        public readonly Dictionary<string, int> secretRanks = new Dictionary<string, int>();
        public readonly List<string> relics = new List<string>();

        private readonly int[] levelRequirements = { 18, 27, 35, 45, 60, 80 };
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
            unlockedSecrets.Clear();
            secretRanks.Clear();
            relics.Clear();
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

        public int GetSecretRank(string secretId)
        {
            return !string.IsNullOrEmpty(secretId) && secretRanks.TryGetValue(secretId, out int rank)
                ? rank
                : 0;
        }

        public bool HasRelic(string relicId)
        {
            return !string.IsNullOrEmpty(relicId) && relics.Contains(relicId);
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
                case "踏雪无痕":
                    runtimeStats.dodgeChance = Mathf.Clamp01(runtimeStats.dodgeChance + 0.04f);
                    break;
                case "流云步":
                    runtimeStats.moveSpeed *= 1.08f;
                    runtimeStats.attackSpeed += 0.06f;
                    break;
                case "饮血刀法":
                    runtimeStats.lifeSteal = Mathf.Clamp01(runtimeStats.lifeSteal + 0.03f);
                    break;
                case "沸血诀":
                    runtimeStats.attack *= 1.08f;
                    runtimeStats.attackSpeed += 0.05f;
                    break;
                case "修罗血域":
                    runtimeStats.lifeSteal = Mathf.Clamp01(runtimeStats.lifeSteal + 0.02f + newRank * 0.01f);
                    break;
            }

            RefreshMartialArtSecrets();

            return newRank;
        }

        public string UpgradeRandomMartialArt()
        {
            List<string> candidates = new List<string>();
            foreach (string artId in learnedMartialArts)
            {
                MartialArtDefinition definition = MartialArtCatalog.Get(artId);
                if (definition != null && GetMartialArtRank(artId) < definition.maxRank)
                {
                    candidates.Add(artId);
                }
            }

            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            string selected = candidates[Random.Range(0, candidates.Count)];
            int rank = ApplyMartialArt(selected);
            return $"{selected}·{rank}重";
        }

        public bool GrantRelic(string relicId)
        {
            RunRelicDefinition relic = RunContentCatalog.GetRelic(relicId);
            if (relic == null || HasRelic(relicId) || relics.Count >= 2)
            {
                return false;
            }

            relics.Add(relicId);
            switch (relicId)
            {
                case "compass":
                    runtimeStats.moveSpeed *= 1.08f;
                    break;
                case "abacus":
                    copper += 4;
                    break;
                case "meditation_mat":
                    cultivation += 12;
                    break;
                case "broken_sword_tassel":
                    runtimeStats.attackSpeed += 0.08f;
                    break;
                case "toad_jade":
                    runtimeStats.lifeSteal = Mathf.Clamp01(runtimeStats.lifeSteal + 0.03f);
                    break;
                case "mountain_bell":
                    runtimeStats.defense += 1.5f;
                    break;
                case "shadow_jade":
                    runtimeStats.dodgeChance = Mathf.Clamp01(runtimeStats.dodgeChance + 0.04f);
                    break;
                case "blood_marrow_pearl":
                    float healthGain = runtimeStats.maxHealth * 0.12f;
                    runtimeStats.maxHealth += healthGain;
                    runtimeStats.Heal(healthGain);
                    break;
            }

            return true;
        }

        public bool ApplyConsumable(string consumableId)
        {
            RunConsumableDefinition consumable = RunContentCatalog.GetConsumable(consumableId);
            if (consumable == null)
            {
                return false;
            }

            switch (consumableId)
            {
                case "healing_salve":
                    HealPercent(0.45f);
                    break;
                case "tiger_bone_pill":
                    ApplyDefenseBuff(1.5f);
                    break;
                case "lightness_powder":
                    ApplyMoveSpeedBuff(0.12f);
                    break;
                case "red_sun_pill":
                    ApplyAttackBuff(0.12f);
                    break;
                case "foundation_pill":
                    float healthGain = runtimeStats.maxHealth * 0.10f;
                    runtimeStats.maxHealth += healthGain;
                    runtimeStats.Heal(healthGain);
                    break;
                case "insight_incense":
                    cultivation += 18;
                    break;
            }

            return true;
        }

        private void RefreshMartialArtSecrets()
        {
            foreach (string secretId in MartialArtCatalog.AllSecretIds)
            {
                MartialArtSecretDefinition secret = MartialArtCatalog.GetSecret(secretId);
                if (secret == null)
                {
                    continue;
                }

                int pairedDepth = Mathf.Min(
                    GetMartialArtSchoolRank(secret.firstSchool),
                    GetMartialArtSchoolRank(secret.secondSchool));
                int targetRank = pairedDepth >= 4 ? 2 : pairedDepth >= 2 ? 1 : 0;
                int currentRank = GetSecretRank(secretId);
                if (targetRank <= currentRank)
                {
                    continue;
                }

                secretRanks[secretId] = targetRank;
                if (currentRank == 0)
                {
                    unlockedSecrets.Add(secretId);
                }
            }
        }
    }
}
