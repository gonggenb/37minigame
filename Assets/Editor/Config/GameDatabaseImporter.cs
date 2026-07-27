using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.Application.Configuration;
using WuxiaRoguelite.Config;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Editor.Config
{
    public static class GameDatabaseImporter
    {
        public const string TableDirectory = "Assets/GameData/Tables";
        public const string DatabaseAssetPath = "Assets/GameData/Generated/GameDatabase.asset";

        [MenuItem("Tools/一炷江湖/导入 CSV 配置")]
        public static void ImportAll()
        {
            try
            {
                GameConfigSet data = ReadTables();
                ValidationReport report = new GameDatabaseValidator().Validate(data);
                if (!report.IsValid)
                {
                    throw new InvalidDataException(BuildValidationMessage(report));
                }

                EnsureAssetFolder("Assets/GameData/Generated");
                GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabaseAssetPath);
                if (database == null)
                {
                    database = ScriptableObject.CreateInstance<GameDatabase>();
                    AssetDatabase.CreateAsset(database, DatabaseAssetPath);
                }

                database.ReplaceAll(data);
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
                Debug.Log($"CSV 配置导入完成：{DatabaseAssetPath}", database);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CSV 配置导入失败：\n{exception.Message}");
            }
        }

        public static GameConfigSet ReadTables()
        {
            var data = new GameConfigSet();
            Read("characters.csv", row => data.characters.Add(ToCharacter(row)));
            Read("martial_arts.csv", row => data.martialArts.Add(ToMartialArt(row)));
            Read("equipment.csv", row => data.equipment.Add(ToEquipment(row)));
            Read("rewards.csv", row => data.rewards.Add(ToReward(row)));
            Read("spawns.csv", row => data.spawns.Add(ToSpawn(row)));
            return data;
        }

        private static CharacterConfig ToCharacter(CsvRow row)
        {
            return new CharacterConfig
            {
                id = Required(row, "id"),
                displayName = Required(row, "display_name"),
                kind = EnumValue<CharacterKind>(row, "kind"),
                visualId = Required(row, "visual_id"),
                maxHealth = Float(row, "max_health"),
                attack = Float(row, "attack"),
                defense = Float(row, "defense"),
                attackSpeed = Float(row, "attack_speed"),
                critChance = Float(row, "crit_chance"),
                critMultiplier = Float(row, "crit_multiplier"),
                lifeSteal = Float(row, "life_steal"),
                dodgeChance = Float(row, "dodge_chance"),
                moveSpeed = Float(row, "move_speed")
            };
        }

        private static MartialArtConfig ToMartialArt(CsvRow row)
        {
            return new MartialArtConfig
            {
                id = Required(row, "id"),
                displayName = Required(row, "display_name"),
                school = EnumValue<MartialArtSchool>(row, "school"),
                isStarter = Bool(row, "is_starter"),
                maxRank = Int(row, "max_rank"),
                effectType = EnumValue<CombatEffectType>(row, "effect_type"),
                primaryStat = EnumValue<StatType>(row, "primary_stat"),
                primaryOperation = EnumValue<ModifierOperation>(row, "primary_operation"),
                secondaryStat = EnumValue<StatType>(row, "secondary_stat"),
                secondaryOperation = EnumValue<ModifierOperation>(row, "secondary_operation"),
                magnitudes = FloatArray(row, "magnitudes"),
                secondaryValues = FloatArray(row, "secondary_values"),
                triggerIntervals = IntArray(row, "trigger_intervals"),
                maxStacks = IntArray(row, "max_stacks"),
                description = Required(row, "description")
            };
        }

        private static EquipmentConfig ToEquipment(CsvRow row)
        {
            return new EquipmentConfig
            {
                id = Required(row, "id"),
                displayName = Required(row, "display_name"),
                slot = Required(row, "slot"),
                rarity = Required(row, "rarity"),
                attackBonus = Float(row, "attack_bonus"),
                defenseBonus = Float(row, "defense_bonus"),
                maxHealthBonus = Float(row, "max_health_bonus"),
                attackSpeedBonus = Float(row, "attack_speed_bonus"),
                critChanceBonus = Float(row, "crit_chance_bonus"),
                dodgeChanceBonus = Float(row, "dodge_chance_bonus"),
                effectType = EnumValue<CombatEffectType>(row, "effect_type"),
                magnitude = Float(row, "magnitude"),
                secondaryValue = Float(row, "secondary_value"),
                triggerInterval = Int(row, "trigger_interval"),
                maxStacks = Int(row, "max_stacks"),
                description = Required(row, "description")
            };
        }

        private static RewardConfig ToReward(CsvRow row)
        {
            return new RewardConfig
            {
                id = Required(row, "id"),
                cultivation = Int(row, "cultivation"),
                copper = Int(row, "copper"),
                healRatio = Float(row, "heal_ratio"),
                equipmentId = Optional(row, "equipment_id"),
                martialArtId = Optional(row, "martial_art_id")
            };
        }

        private static SpawnConfig ToSpawn(CsvRow row)
        {
            return new SpawnConfig
            {
                id = Required(row, "id"),
                regionId = Required(row, "region_id"),
                entityType = EnumValue<SpawnEntityType>(row, "entity_type"),
                configId = Optional(row, "config_id"),
                prefabId = Required(row, "prefab_id"),
                rewardId = Optional(row, "reward_id"),
                minCount = Int(row, "min_count"),
                maxCount = Int(row, "max_count"),
                weight = Float(row, "weight")
            };
        }

        private static void Read(string fileName, Action<CsvRow> visitor)
        {
            string path = Path.Combine(TableDirectory, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"缺少 CSV 表：{path}", path);
            }

            string text = File.ReadAllText(path, new UTF8Encoding(false, true));
            CsvTable table = new CsvTableParser().Parse(text);
            for (int i = 0; i < table.Rows.Count; i++)
            {
                try
                {
                    visitor(table.Rows[i]);
                }
                catch (Exception exception)
                {
                    throw new InvalidDataException($"{fileName} 第 {i + 2} 行：{exception.Message}", exception);
                }
            }
        }

        private static string Required(CsvRow row, string column)
        {
            string value = Optional(row, column);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException($"{column} 不能为空。");
            }

            return value;
        }

        private static string Optional(CsvRow row, string column)
        {
            return row.TryGet(column, out string value) ? value : string.Empty;
        }

        private static int Int(CsvRow row, string column)
        {
            string value = Required(row, column);
            if (!System.Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                throw new InvalidDataException($"{column} 不是有效整数：{value}");
            }

            return result;
        }

        private static float Float(CsvRow row, string column)
        {
            string value = Required(row, column);
            if (!System.Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                throw new InvalidDataException($"{column} 不是有效数字：{value}");
            }

            return result;
        }

        private static bool Bool(CsvRow row, string column)
        {
            string value = Required(row, column);
            if (!System.Boolean.TryParse(value, out bool result))
            {
                throw new InvalidDataException($"{column} 不是 true/false：{value}");
            }

            return result;
        }

        private static T EnumValue<T>(CsvRow row, string column) where T : struct
        {
            string value = Required(row, column);
            if (!System.Enum.TryParse(value, true, out T result))
            {
                throw new InvalidDataException($"{column} 不是有效 {typeof(T).Name}：{value}");
            }

            return result;
        }

        private static float[] FloatArray(CsvRow row, string column)
        {
            string[] values = Required(row, column).Split('|');
            var result = new float[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (!System.Single.TryParse(values[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                {
                    throw new InvalidDataException($"{column} 第 {i + 1} 项不是有效数字：{values[i]}");
                }
            }

            return result;
        }

        private static int[] IntArray(CsvRow row, string column)
        {
            string[] values = Required(row, column).Split('|');
            var result = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (!System.Int32.TryParse(values[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]))
                {
                    throw new InvalidDataException($"{column} 第 {i + 1} 项不是有效整数：{values[i]}");
                }
            }

            return result;
        }

        private static string BuildValidationMessage(ValidationReport report)
        {
            var builder = new StringBuilder("配置校验失败：");
            for (int i = 0; i < report.Errors.Count; i++)
            {
                ValidationIssue issue = report.Errors[i];
                builder.AppendLine();
                builder.Append($"- [{issue.table}/{issue.rowId}] {issue.message}");
            }

            return builder.ToString();
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }

    public sealed class GameDatabaseCsvPostprocessor : AssetPostprocessor
    {
        private static bool scheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (scheduled || (!ContainsTable(importedAssets) && !ContainsTable(movedAssets)))
            {
                return;
            }

            scheduled = true;
            EditorApplication.delayCall += () =>
            {
                scheduled = false;
                GameDatabaseImporter.ImportAll();
            };
        }

        private static bool ContainsTable(IReadOnlyList<string> paths)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].StartsWith(GameDatabaseImporter.TableDirectory, StringComparison.Ordinal) &&
                    paths[i].EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
