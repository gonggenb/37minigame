using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.GameFlow
{
    public partial class GameFlowController
    {
        private string endlessRunId;
        private readonly int[] endlessRewardCounts = new int[5];
        public int EndlessCoinsEarned { get; private set; }
        public bool CanManageEndlessProgression => !UI.LevelLoadingScreen.IsLoading &&
            (CurrentPhase == GamePhase.Ready || CurrentPhase == GamePhase.Result);
        public int EndlessRewardCount(EndlessReward kind) => (int)kind >= 0 && (int)kind < endlessRewardCounts.Length
            ? endlessRewardCounts[(int)kind] : 0;
        private void ResetEndlessProgressionRun()
        {
            System.Array.Clear(endlessRewardCounts, 0, endlessRewardCounts.Length);
            EndlessCoinsEarned = 0;
            endlessRunId = IsEndlessMode ? EndlessProgression.BeginRun() : null;
        }
        private void ApplyEndlessProgression()
        {
            if (IsEndlessMode && playerStats != null)
                EndlessProgressionCatalog.Apply(playerStats.runtimeStats, EndlessProgression.Read());
        }
        private void RecordEndlessReward(EndlessReward kind)
        {
            if (!IsEndlessMode || string.IsNullOrEmpty(endlessRunId) || CurrentPhase == GamePhase.Result) return;
            int index = (int)kind;
            if (index < 0 || index >= endlessRewardCounts.Length) return;
            endlessRewardCounts[index] = Mathf.Min(EndlessProgressionCatalog.BalanceLimit, endlessRewardCounts[index] + 1);
            EndlessCoinsEarned = Mathf.Min(EndlessProgressionCatalog.BalanceLimit,
                EndlessCoinsEarned + EndlessProgressionCatalog.RewardCoins(kind));
            SaveEndlessRewards();
        }
        private void SaveEndlessRewards()
        {
            if (IsEndlessMode) EndlessProgression.CreditRun(endlessRunId, EndlessCoinsEarned,
                EndlessRewardCount(EndlessReward.Round));
        }
        public bool ExchangeEndlessPoint() => CanManageEndlessProgression && EndlessProgression.TryExchange();
        public bool UpgradeEndlessSkill(EndlessSkill skill) => CanManageEndlessProgression && EndlessProgression.TryUpgrade(skill);
    }
}
