using System;
using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Application.Characters;
using WuxiaRoguelite.Application.Progression;
using WuxiaRoguelite.Application.Rewards;
using WuxiaRoguelite.Config;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Combat;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Architecture.Characters
{
    public sealed class CharacterRewardGrant
    {
        public CharacterRewardGrant(RewardResult reward, int levelsGained)
        {
            Reward = reward;
            LevelsGained = levelsGained;
        }

        public RewardResult Reward { get; }
        public int LevelsGained { get; }
    }

    [DisallowMultipleComponent]
    public sealed class CharacterManager : MonoBehaviour
    {
        [SerializeField] private GameDatabaseProvider databaseProvider;
        [SerializeField] private string playerCharacterId = "player_wuxia";
        [SerializeField] private int[] levelRequirements = { 20, 35, 55, 80, 120 };
        [SerializeField] private string[] startingEquipmentIds =
        {
            "equipment_qinggang_sword",
            "equipment_light_scale",
            "equipment_practice_bracer"
        };

        private readonly CharacterFactory characterFactory = new CharacterFactory();
        private readonly RewardService rewardService = new RewardService();
        private readonly Dictionary<string, int> martialArtRanks = new Dictionary<string, int>();
        private readonly Dictionary<string, string> equippedBySlot = new Dictionary<string, string>();
        private ProgressionService progressionService;

        public event Action Changed;

        public CharacterRuntime Player { get; private set; }
        public int Level { get; private set; } = 1;
        public int Cultivation { get; private set; }
        public int Copper { get; private set; }
        public int KillCount { get; private set; }
        public int CaveEntries { get; private set; }
        public IReadOnlyDictionary<string, int> MartialArtRanks => martialArtRanks;
        public IReadOnlyDictionary<string, string> EquippedBySlot => equippedBySlot;
        public GameDatabase Database => databaseProvider.Database;

        private void Awake()
        {
            progressionService = new ProgressionService(levelRequirements);
        }

        public void StartNewRun()
        {
            EnsureConfigured();
            progressionService = new ProgressionService(levelRequirements);
            Player = characterFactory.Create(Database.GetCharacter(playerCharacterId));
            Level = 1;
            Cultivation = 0;
            Copper = 0;
            KillCount = 0;
            CaveEntries = 0;
            martialArtRanks.Clear();
            equippedBySlot.Clear();

            for (int i = 0; i < startingEquipmentIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(startingEquipmentIds[i]))
                {
                    Equip(startingEquipmentIds[i], false);
                }
            }

            NotifyChanged();
        }

        public CharacterRewardGrant GrantReward(string rewardId)
        {
            EnsurePlayer();
            RewardResult reward = rewardService.Apply(Database.GetReward(rewardId), Player);
            Copper += reward.Copper;
            ProgressionResult progression = progressionService.AddCultivation(
                Level,
                Cultivation,
                reward.Cultivation);
            Level = progression.Level;
            Cultivation = progression.Cultivation;

            if (!string.IsNullOrWhiteSpace(reward.EquipmentId))
            {
                Equip(reward.EquipmentId, false);
            }

            if (!string.IsNullOrWhiteSpace(reward.MartialArtId))
            {
                LearnMartialArt(reward.MartialArtId, false);
            }

            NotifyChanged();
            return new CharacterRewardGrant(reward, progression.LevelsGained);
        }

        public int LearnMartialArt(string martialArtId)
        {
            return LearnMartialArt(martialArtId, true);
        }

        public bool Equip(string equipmentId)
        {
            return Equip(equipmentId, true);
        }

        public IReadOnlyList<CombatEffectDefinition> BuildCombatEffects()
        {
            EnsurePlayer();
            var effects = new List<CombatEffectDefinition>();
            foreach (KeyValuePair<string, int> entry in martialArtRanks)
            {
                CombatEffectDefinition effect = characterFactory.CreateCombatEffect(
                    Database.GetMartialArt(entry.Key),
                    entry.Value);
                if (effect != null)
                {
                    effects.Add(effect);
                }
            }

            foreach (KeyValuePair<string, string> entry in equippedBySlot)
            {
                CombatEffectDefinition effect = characterFactory.CreateCombatEffect(
                    Database.GetEquipment(entry.Value));
                if (effect != null)
                {
                    effects.Add(effect);
                }
            }

            return effects;
        }

        public void HealRatio(float ratio)
        {
            EnsurePlayer();
            Player.Heal(Player.Stats.MaxHealth * Mathf.Clamp01(ratio));
            NotifyChanged();
        }

        public void ResetHealth()
        {
            EnsurePlayer();
            Player.ResetHealth();
            NotifyChanged();
        }

        public void RecordKill()
        {
            KillCount += 1;
            NotifyChanged();
        }

        public void RecordCaveEntry()
        {
            CaveEntries += 1;
            NotifyChanged();
        }

        public int GetMartialArtRank(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && martialArtRanks.TryGetValue(id, out int rank)
                ? rank
                : 0;
        }

        private int LearnMartialArt(string martialArtId, bool notify)
        {
            EnsurePlayer();
            MartialArtConfig config = Database.GetMartialArt(martialArtId);
            int current = GetMartialArtRank(martialArtId);
            if (current >= config.maxRank)
            {
                return current;
            }

            int rank = current + 1;
            martialArtRanks[martialArtId] = rank;
            characterFactory.ApplyMartialArt(Player, config, rank);
            if (notify)
            {
                NotifyChanged();
            }

            return rank;
        }

        private bool Equip(string equipmentId, bool notify)
        {
            EnsurePlayer();
            EquipmentConfig config = Database.GetEquipment(equipmentId);
            string slot = config.slot ?? string.Empty;
            if (equippedBySlot.TryGetValue(slot, out string previousId))
            {
                if (previousId == equipmentId)
                {
                    return false;
                }

                characterFactory.RemoveEquipment(Player, previousId);
            }

            equippedBySlot[slot] = equipmentId;
            characterFactory.ApplyEquipment(Player, config);
            if (notify)
            {
                NotifyChanged();
            }

            return true;
        }

        private void EnsureConfigured()
        {
            if (databaseProvider == null || !databaseProvider.IsConfigured)
            {
                throw new InvalidOperationException("CharacterManager 需要绑定已配置的 GameDatabaseProvider。");
            }
        }

        private void EnsurePlayer()
        {
            EnsureConfigured();
            if (Player == null)
            {
                throw new InvalidOperationException("角色尚未初始化，请先调用 StartNewRun。");
            }
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
