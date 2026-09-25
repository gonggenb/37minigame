using UnityEngine;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.GameFlow
{
    public partial class GameFlowController
    {
        public bool IsEndlessMode { get; private set; }
        public int EndlessRound { get; private set; }
        public int EndlessRoundsCleared => IsEndlessMode ? Mathf.Max(0, EndlessRound - 1) : 0;
        public string EndlessRoundLabel => $"无尽 · 第{EndlessRound}轮";
        public string EndlessResultSummary => $"已通过 {EndlessRoundsCleared} 轮 · 止步第 {EndlessRound} 轮";

        public void SelectEndlessMode()
        {
            if (!IsLevelTwoUnlocked || LevelLoadingScreen.IsLoading) return;
            IsLevelSelectionOpen = false;
            LevelSequence.LoadEndlessMode();
        }

        private void ResetEndlessRun()
        {
            EndlessRound = IsEndlessMode ? 1 : 0;
            ResetEndlessProgressionRun();
            if (IsEndlessMode) mainTimeLimit = EndlessModeTuning.RoundSeconds;
        }

        public void ApplyEndlessEnemy(CombatantStats clone)
        {
            if (IsEndlessMode) EndlessModeTuning.Apply(clone, EndlessRound);
        }

        private void BeginNextEndlessRound()
        {
            EndlessRound++;
            battleManager?.CancelBattle();
            caveRoom?.ResetRoom();
            // Do not reset PlayerStats: ranks, secrets, equipment, resources and remaining HP persist.
            playerStats?.ClearTemporaryMoveSpeedBuffs();
            playerController?.ResetToSpawn();
            cameraFollow?.ResetVision();
            ResetRouteProgress(resetRunReview: false);
            foreach (var encounter in FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include))
                if (encounter.gameObject.scene == gameObject.scene)
                    encounter.ResetEncounter(rerollCaveContent: true);
            // Preserve the revealed enemy traits so every round increases the same enemy's stats.
            if (ChallengeRun != null)
                foreach (var bounty in ChallengeRun.bounties)
                {
                    bounty.completed = false;
                    bounty.rewardReceived = string.Empty;
                }
            pendingBountyEncounter = null;
            ChallengeResult = string.Empty;
            IsChallengeBriefingActive = false;
            IsDebugInfiniteTime = false;
            mainTimeLimit = mainTimeRemaining = EndlessModeTuning.RoundSeconds;
            midBossBattleTime = bossBattleTime = 0f;
            bossDefeated = midBossDefeated = false;
            IsBossTransitionPending = IsMidBossTransitionPending = IsBossIntroActive = false;
            BossIntroTimeRemaining = 0f;
            pendingBoss = pendingMidBoss = null;
            pendingCultivationReward = pendingCopperReward = 0;
            pendingEnemyName = string.Empty;
            pendingEnemyLevel = 0;
            pendingEnemyType = EncounterType.NormalEnemy;
            isOpeningMartialArtChoice = false;
            currentChoices.Clear();
            martialArtRerollsRemaining = 1;
            phaseBeforeLevelUp = GamePhase.MainMapRunning;
            SetPhase(GamePhase.MainMapRunning);
            statusMessage = $"{EndlessRoundLabel}：武学与成长保留，敌人全面增强，六十息重新开始。";
        }
    }
}
