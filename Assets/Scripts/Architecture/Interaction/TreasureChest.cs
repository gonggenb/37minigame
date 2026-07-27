using UnityEngine;
using WuxiaRoguelite.Architecture.GameFlow;

namespace WuxiaRoguelite.Architecture.Interaction
{
    public sealed class TreasureChest : WorldInteractableBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private string rewardId = "reward_treasure";

        public void Configure(RunManager manager, string configuredRewardId)
        {
            runManager = manager;
            rewardId = configuredRewardId;
            ResetInteraction();
        }

        protected override bool TryInteract(GameObject actor)
        {
            return runManager != null && runManager.TryCollectReward(rewardId);
        }
    }
}
