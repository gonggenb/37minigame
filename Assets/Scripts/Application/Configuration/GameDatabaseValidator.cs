using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Application.Configuration
{
    public sealed class ValidationIssue
    {
        public ValidationIssue(string table, string rowId, string message)
        {
            this.table = table;
            this.rowId = rowId;
            this.message = message;
        }

        public string table { get; }
        public string rowId { get; }
        public string message { get; }
    }

    public sealed class ValidationReport
    {
        private readonly List<ValidationIssue> errors = new List<ValidationIssue>();

        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<ValidationIssue> Errors => errors;

        internal void Add(string table, string rowId, string message)
        {
            errors.Add(new ValidationIssue(table, rowId, message));
        }
    }

    public sealed class GameDatabaseValidator
    {
        private static readonly Regex StableId = new Regex("^[a-z0-9][a-z0-9_-]*$", RegexOptions.Compiled);

        public ValidationReport Validate(GameConfigSet data)
        {
            var report = new ValidationReport();
            if (data == null)
            {
                report.Add("database", string.Empty, "配置集合不能为空。");
                return report;
            }

            ValidateIds(data.characters, item => item.id, "characters", report);
            ValidateIds(data.martialArts, item => item.id, "martial_arts", report);
            ValidateIds(data.equipment, item => item.id, "equipment", report);
            ValidateIds(data.rewards, item => item.id, "rewards", report);
            ValidateIds(data.spawns, item => item.id, "spawns", report);

            var characterIds = new HashSet<string>(data.characters.Select(item => item.id));
            var martialArtIds = new HashSet<string>(data.martialArts.Select(item => item.id));
            var equipmentIds = new HashSet<string>(data.equipment.Select(item => item.id));
            var rewardIds = new HashSet<string>(data.rewards.Select(item => item.id));

            for (int i = 0; i < data.characters.Count; i++)
            {
                CharacterConfig item = data.characters[i];
                if (item.maxHealth <= 0f || item.attack < 0f || item.defense < 0f || item.attackSpeed <= 0f)
                {
                    report.Add("characters", item.id, "角色气血、攻击、防御或攻速超出允许范围。");
                }

                if (!InUnitRange(item.critChance) || !InUnitRange(item.lifeSteal) ||
                    !InUnitRange(item.dodgeChance) || item.critMultiplier < 1f)
                {
                    report.Add("characters", item.id, "角色概率属性或暴击倍率超出允许范围。");
                }
            }

            for (int i = 0; i < data.martialArts.Count; i++)
            {
                MartialArtConfig item = data.martialArts[i];
                if (item.maxRank <= 0 || item.effectType == Domain.Combat.CombatEffectType.None)
                {
                    report.Add("martial_arts", item.id, "武学必须有正数等级上限和有效效果类型。");
                }

                if (!HasRankValues(item.magnitudes, item.maxRank) ||
                    !HasRankValues(item.secondaryValues, item.maxRank) ||
                    !HasRankValues(item.triggerIntervals, item.maxRank) ||
                    !HasRankValues(item.maxStacks, item.maxRank))
                {
                    report.Add("martial_arts", item.id, "武学各等级效果数组长度必须覆盖 maxRank。");
                }
            }

            for (int i = 0; i < data.rewards.Count; i++)
            {
                RewardConfig item = data.rewards[i];
                if (item.cultivation < 0 || item.copper < 0 || !InUnitRange(item.healRatio))
                {
                    report.Add("rewards", item.id, "奖励数值不能为负，恢复比例必须在 0 到 1 之间。");
                }

                CheckOptionalReference("rewards", item.id, "equipmentId", item.equipmentId, equipmentIds, report);
                CheckOptionalReference("rewards", item.id, "martialArtId", item.martialArtId, martialArtIds, report);
            }

            for (int i = 0; i < data.spawns.Count; i++)
            {
                SpawnConfig item = data.spawns[i];
                if (string.IsNullOrWhiteSpace(item.regionId) || string.IsNullOrWhiteSpace(item.prefabId))
                {
                    report.Add("spawns", item.id, "生成规则必须填写 regionId 和 prefabId。");
                }

                if (item.minCount < 0 || item.maxCount < item.minCount || item.weight <= 0f)
                {
                    report.Add("spawns", item.id, "生成数量或权重超出允许范围。");
                }

                if (item.entityType == SpawnEntityType.Enemy)
                {
                    CheckRequiredReference("spawns", item.id, item.configId, characterIds, report);
                }
                else if (item.entityType == SpawnEntityType.Treasure || item.entityType == SpawnEntityType.Herb)
                {
                    CheckRequiredReference("spawns", item.id, item.configId, rewardIds, report);
                }

                CheckOptionalReference("spawns", item.id, "rewardId", item.rewardId, rewardIds, report);
            }

            return report;
        }

        private static void ValidateIds<T>(
            IReadOnlyList<T> items,
            Func<T, string> idSelector,
            string table,
            ValidationReport report)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < items.Count; i++)
            {
                string id = idSelector(items[i]) ?? string.Empty;
                if (!StableId.IsMatch(id))
                {
                    report.Add(table, id, "ID 必须使用稳定的小写 ASCII 字母、数字、下划线或短横线。");
                }

                if (!seen.Add(id))
                {
                    report.Add(table, id, $"发现重复 ID：{id}");
                }
            }
        }

        private static void CheckRequiredReference(
            string table,
            string rowId,
            string reference,
            HashSet<string> knownIds,
            ValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(reference) || !knownIds.Contains(reference))
            {
                report.Add(table, rowId, $"引用的配置 ID 不存在：{reference}");
            }
        }

        private static void CheckOptionalReference(
            string table,
            string rowId,
            string field,
            string reference,
            HashSet<string> knownIds,
            ValidationReport report)
        {
            if (!string.IsNullOrWhiteSpace(reference) && !knownIds.Contains(reference))
            {
                report.Add(table, rowId, $"{field} 引用的配置 ID 不存在：{reference}");
            }
        }

        private static bool InUnitRange(float value)
        {
            return value >= 0f && value <= 1f;
        }

        private static bool HasRankValues<T>(T[] values, int maxRank)
        {
            return values != null && values.Length >= maxRank;
        }
    }
}
