using UnityEngine;
using WuxiaRoguelite.Architecture.GameFlow;

namespace WuxiaRoguelite.Architecture.Interaction
{
    public sealed class EnemyEncounter : WorldInteractableBehaviour
    {
        [SerializeField] private RunManager runManager;
        [SerializeField] private string characterId = "enemy_bandit";
        [SerializeField] private string rewardId = "reward_normal_enemy";
        [SerializeField] private bool caveBattle;

        public void Configure(RunManager manager, string configuredCharacterId, string configuredRewardId, bool isCaveBattle)
        {
            runManager = manager;
            characterId = configuredCharacterId;
            rewardId = configuredRewardId;
            caveBattle = isCaveBattle;
            ResetInteraction();
        }

        protected override bool TryInteract(GameObject actor)
        {
            if (runManager == null)
            {
                return false;
            }

            return caveBattle
                ? runManager.TryBeginCaveBattle(characterId, rewardId)
                : runManager.TryBeginNormalBattle(characterId, rewardId);
        }
    }
}
