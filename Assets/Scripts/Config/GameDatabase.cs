using System;
using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Application.Configuration;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Config
{
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "一炷江湖/Game Database")]
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterConfig> characters = new List<CharacterConfig>();
        [SerializeField] private List<MartialArtConfig> martialArts = new List<MartialArtConfig>();
        [SerializeField] private List<EquipmentConfig> equipment = new List<EquipmentConfig>();
        [SerializeField] private List<RewardConfig> rewards = new List<RewardConfig>();
        [SerializeField] private List<SpawnConfig> spawns = new List<SpawnConfig>();

        [NonSerialized] private GameDatabaseIndex index;

        public IReadOnlyList<CharacterConfig> Characters => characters;
        public IReadOnlyList<MartialArtConfig> MartialArts => martialArts;
        public IReadOnlyList<EquipmentConfig> Equipment => equipment;
        public IReadOnlyList<RewardConfig> Rewards => rewards;
        public IReadOnlyList<SpawnConfig> Spawns => spawns;

        public void ReplaceAll(GameConfigSet data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            characters = new List<CharacterConfig>(data.characters);
            martialArts = new List<MartialArtConfig>(data.martialArts);
            equipment = new List<EquipmentConfig>(data.equipment);
            rewards = new List<RewardConfig>(data.rewards);
            spawns = new List<SpawnConfig>(data.spawns);
            RebuildIndex();
        }

        public CharacterConfig GetCharacter(string id)
        {
            EnsureIndex();
            return index.GetCharacter(id);
        }

        public MartialArtConfig GetMartialArt(string id)
        {
            EnsureIndex();
            return index.GetMartialArt(id);
        }

        public EquipmentConfig GetEquipment(string id)
        {
            EnsureIndex();
            return index.GetEquipment(id);
        }

        public RewardConfig GetReward(string id)
        {
            EnsureIndex();
            return index.GetReward(id);
        }

        public SpawnConfig GetSpawn(string id)
        {
            EnsureIndex();
            return index.GetSpawn(id);
        }

        public GameConfigSet CreateConfigSetCopy()
        {
            return new GameConfigSet
            {
                characters = new List<CharacterConfig>(characters),
                martialArts = new List<MartialArtConfig>(martialArts),
                equipment = new List<EquipmentConfig>(equipment),
                rewards = new List<RewardConfig>(rewards),
                spawns = new List<SpawnConfig>(spawns)
            };
        }

        private void OnEnable()
        {
            RebuildIndex();
        }

        private void OnValidate()
        {
            index = null;
        }

        private void EnsureIndex()
        {
            if (index == null)
            {
                RebuildIndex();
            }
        }

        private void RebuildIndex()
        {
            index = new GameDatabaseIndex(CreateConfigSetCopy());
        }
    }
}
