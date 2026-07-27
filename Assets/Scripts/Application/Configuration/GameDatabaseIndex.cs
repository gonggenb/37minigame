using System;
using System.Collections.Generic;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Application.Configuration
{
    public sealed class GameDatabaseIndex
    {
        private readonly Dictionary<string, CharacterConfig> characters;
        private readonly Dictionary<string, MartialArtConfig> martialArts;
        private readonly Dictionary<string, EquipmentConfig> equipment;
        private readonly Dictionary<string, RewardConfig> rewards;
        private readonly Dictionary<string, SpawnConfig> spawns;

        public GameDatabaseIndex(GameConfigSet data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            characters = Build(data.characters, item => item.id, "characters");
            martialArts = Build(data.martialArts, item => item.id, "martial_arts");
            equipment = Build(data.equipment, item => item.id, "equipment");
            rewards = Build(data.rewards, item => item.id, "rewards");
            spawns = Build(data.spawns, item => item.id, "spawns");
        }

        public CharacterConfig GetCharacter(string id)
        {
            return Get(characters, id, "角色");
        }

        public MartialArtConfig GetMartialArt(string id)
        {
            return Get(martialArts, id, "武学");
        }

        public EquipmentConfig GetEquipment(string id)
        {
            return Get(equipment, id, "装备");
        }

        public RewardConfig GetReward(string id)
        {
            return Get(rewards, id, "奖励");
        }

        public SpawnConfig GetSpawn(string id)
        {
            return Get(spawns, id, "生成规则");
        }

        public bool TryGetCharacter(string id, out CharacterConfig value)
        {
            return characters.TryGetValue(id ?? string.Empty, out value);
        }

        public bool TryGetReward(string id, out RewardConfig value)
        {
            return rewards.TryGetValue(id ?? string.Empty, out value);
        }

        private static Dictionary<string, T> Build<T>(
            IReadOnlyList<T> source,
            Func<T, string> idSelector,
            string table)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                string id = idSelector(source[i]);
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException($"{table} 存在空 ID。");
                }

                if (!result.TryAdd(id, source[i]))
                {
                    throw new InvalidOperationException($"{table} 存在重复 ID：{id}");
                }
            }

            return result;
        }

        private static T Get<T>(Dictionary<string, T> index, string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || !index.TryGetValue(id, out T value))
            {
                throw new KeyNotFoundException($"找不到{label}配置：{id}");
            }

            return value;
        }
    }
}
