using System;
using WuxiaRoguelite.Domain.Characters;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Application.Rewards
{
    public sealed class RewardResult
    {
        public RewardResult(
            int cultivation,
            int copper,
            string equipmentId,
            string martialArtId)
        {
            Cultivation = cultivation;
            Copper = copper;
            EquipmentId = equipmentId ?? string.Empty;
            MartialArtId = martialArtId ?? string.Empty;
        }

        public int Cultivation { get; }
        public int Copper { get; }
        public string EquipmentId { get; }
        public string MartialArtId { get; }
    }

    public sealed class RewardService
    {
        public RewardResult Apply(RewardConfig reward, CharacterRuntime character)
        {
            if (reward == null)
            {
                throw new ArgumentNullException(nameof(reward));
            }

            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (reward.healRatio > 0f)
            {
                character.Heal(character.Stats.MaxHealth * Math.Min(1f, reward.healRatio));
            }

            return new RewardResult(
                Math.Max(0, reward.cultivation),
                Math.Max(0, reward.copper),
                reward.equipmentId,
                reward.martialArtId);
        }
    }
}
