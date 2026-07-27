using System;
using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Application.Time;
using WuxiaRoguelite.Architecture.Battle;
using WuxiaRoguelite.Architecture.Characters;
using WuxiaRoguelite.Architecture.Spawning;
using WuxiaRoguelite.Domain.Configuration;
using WuxiaRoguelite.Domain.GameFlow;
using WuxiaRoguelite.Player;

namespace WuxiaRoguelite.Architecture.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class RunManager : MonoBehaviour
    {
        [SerializeField] private CharacterManager characterManager;
        [SerializeField] private BattleRunner battleRunner;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private CaveSpawner caveSpawner;
        [SerializeField, Min(1f)] private float mainTimeLimit = 60f;
        [SerializeField] private string bossCharacterId = "boss_fox_demon";

        private readonly List<string> martialArtChoices = new List<string>();
        private RunTimerService timer;
        private GameState stateBeforeLevelUp = GameState.MainMap;
        private string pendingRewardId = string.Empty;
        private int pendingLevelUps;
        private bool bossTransitionPending;
        private bool explicitlyPaused;
        private int rerollsRemaining;

        public event Action StateChanged;
        public event Action Updated;

        public GameState CurrentState { get; private set; } = GameState.Ready;
        public RunTimerService Timer => timer;
        public CharacterManager Characters => characterManager;
        public BattleRunner Battle => battleRunner;
        public IReadOnlyList<string> MartialArtChoices => martialArtChoices;
        public string StatusMessage { get; private set; } = "准备踏入江湖";
        public bool Victory { get; private set; }
        public bool BossTransitionPending => bossTransitionPending;
        public int RerollsRemaining => rerollsRemaining;

        private void Awake()
        {
            timer = new RunTimerService(mainTimeLimit);
        }

        private void Update()
        {
            timer.Tick(CurrentState, Time.deltaTime, explicitlyPaused);
            if (timer.MainTimeRemaining <= 0f)
            {
                if (CurrentState == GameState.MainMap)
                {
                    BeginBossBattle();
                }
                else if (CurrentState == GameState.NormalBattle)
                {
                    bossTransitionPending = true;
                    StatusMessage = "主地图时间已尽，完成当前战斗和突破后进入 Boss 战。";
                }
            }

            Updated?.Invoke();
        }

        public void StartRun()
        {
            EnsureConfigured();
            battleRunner.CancelBattle();
            characterManager.StartNewRun();
            timer = new RunTimerService(mainTimeLimit);
            pendingRewardId = string.Empty;
            pendingLevelUps = 0;
            bossTransitionPending = false;
            explicitlyPaused = false;
            rerollsRemaining = 1;
            martialArtChoices.Clear();
            Victory = false;
            enemySpawner?.SpawnForRun();
            itemSpawner?.SpawnForRun();
            caveSpawner?.SpawnForRun();
            StatusMessage = "主地图探索开始，普通战斗期间倒计时继续。";
            SetState(GameState.MainMap);
        }

        public bool TryBeginNormalBattle(string enemyCharacterId, string rewardId)
        {
            if (CurrentState != GameState.MainMap || string.IsNullOrWhiteSpace(enemyCharacterId))
            {
                return false;
            }

            pendingRewardId = rewardId ?? string.Empty;
            StatusMessage = "普通战斗开始，主地图倒计时继续。";
            SetState(GameState.NormalBattle);
            battleRunner.BeginBattle(enemyCharacterId, OnNormalBattleFinished);
            return true;
        }

        public bool TryEnterCave()
        {
            if (CurrentState != GameState.MainMap)
            {
                return false;
            }

            characterManager.RecordCaveEntry();
            StatusMessage = "进入隐藏洞穴，主地图倒计时暂停。";
            SetState(GameState.Cave);
            return true;
        }

        public bool TryBeginCaveBattle(string enemyCharacterId, string rewardId)
        {
            if (CurrentState != GameState.Cave || battleRunner.IsActive)
            {
                return false;
            }

            pendingRewardId = rewardId ?? string.Empty;
            StatusMessage = "洞穴战斗开始，主地图倒计时保持暂停。";
            battleRunner.BeginBattle(enemyCharacterId, OnCaveBattleFinished);
            return true;
        }

        public bool TryCollectReward(string rewardId)
        {
            if (CurrentState != GameState.MainMap && CurrentState != GameState.Cave)
            {
                return false;
            }

            GameState resumeState = CurrentState;
            CharacterRewardGrant grant = characterManager.GrantReward(rewardId);
            StatusMessage = $"获得修为 {grant.Reward.Cultivation}、铜钱 {grant.Reward.Copper}。";
            if (grant.LevelsGained > 0)
            {
                EnterLevelUp(resumeState, grant.LevelsGained);
            }

            return true;
        }

        public bool ExitCave()
        {
            if (CurrentState != GameState.Cave || battleRunner.IsActive)
            {
                return false;
            }

            if (timer.MainTimeRemaining <= 0f)
            {
                BeginBossBattle();
            }
            else
            {
                StatusMessage = "离开洞穴，主地图倒计时恢复。";
                SetState(GameState.MainMap);
            }

            return true;
        }

        public bool ChooseMartialArt(int choiceIndex)
        {
            if (CurrentState != GameState.LevelUp || choiceIndex < 0 || choiceIndex >= martialArtChoices.Count)
            {
                return false;
            }

            string id = martialArtChoices[choiceIndex];
            int rank = characterManager.LearnMartialArt(id);
            pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
            StatusMessage = $"{characterManager.Database.GetMartialArt(id).displayName} 提升至第 {rank} 重。";
            if (pendingLevelUps > 0)
            {
                GenerateMartialArtChoices();
                StateChanged?.Invoke();
                return true;
            }

            martialArtChoices.Clear();
            if (timer.MainTimeRemaining <= 0f && stateBeforeLevelUp == GameState.MainMap)
            {
                BeginBossBattle();
            }
            else
            {
                SetState(stateBeforeLevelUp);
            }

            return true;
        }

        public bool RerollMartialArtChoices()
        {
            if (CurrentState != GameState.LevelUp || rerollsRemaining <= 0)
            {
                return false;
            }

            rerollsRemaining -= 1;
            GenerateMartialArtChoices();
            StateChanged?.Invoke();
            return true;
        }

        public void SetExplicitPause(bool paused)
        {
            explicitlyPaused = paused;
            RefreshPlayerMovement();
        }

        public void ForceBossBattle()
        {
            if (CurrentState != GameState.Result)
            {
                BeginBossBattle();
            }
        }

        private void OnNormalBattleFinished(bool playerWon)
        {
            if (!playerWon)
            {
                EndRun(false, "普通战斗失败");
                return;
            }

            characterManager.RecordKill();
            CharacterRewardGrant grant = GrantPendingReward();
            if (grant != null && grant.LevelsGained > 0)
            {
                EnterLevelUp(GameState.MainMap, grant.LevelsGained);
                return;
            }

            if (timer.MainTimeRemaining <= 0f || bossTransitionPending)
            {
                BeginBossBattle();
                return;
            }

            StatusMessage = "战斗胜利，返回主地图。";
            SetState(GameState.MainMap);
        }

        private void OnCaveBattleFinished(bool playerWon)
        {
            if (!playerWon)
            {
                EndRun(false, "洞穴挑战失败");
                return;
            }

            characterManager.RecordKill();
            CharacterRewardGrant grant = GrantPendingReward();
            if (grant != null && grant.LevelsGained > 0)
            {
                EnterLevelUp(GameState.Cave, grant.LevelsGained);
                return;
            }

            StatusMessage = "洞穴战斗胜利，主地图倒计时仍暂停。";
            SetState(GameState.Cave);
        }

        private CharacterRewardGrant GrantPendingReward()
        {
            if (string.IsNullOrWhiteSpace(pendingRewardId))
            {
                return null;
            }

            string rewardId = pendingRewardId;
            pendingRewardId = string.Empty;
            return characterManager.GrantReward(rewardId);
        }

        private void EnterLevelUp(GameState resumeState, int levelsGained)
        {
            stateBeforeLevelUp = resumeState;
            pendingLevelUps = Mathf.Max(1, levelsGained);
            GenerateMartialArtChoices();
            StatusMessage = "修为突破，所有时间暂停。";
            SetState(GameState.LevelUp);
        }

        private void GenerateMartialArtChoices()
        {
            martialArtChoices.Clear();
            var candidates = new List<string>();
            IReadOnlyList<MartialArtConfig> configs = characterManager.Database.MartialArts;
            bool hasLearned = characterManager.MartialArtRanks.Count > 0;
            for (int i = 0; i < configs.Count; i++)
            {
                MartialArtConfig config = configs[i];
                if (characterManager.GetMartialArtRank(config.id) >= config.maxRank)
                {
                    continue;
                }

                if (!hasLearned && !config.isStarter)
                {
                    continue;
                }

                candidates.Add(config.id);
            }

            while (candidates.Count > 0 && martialArtChoices.Count < 3)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                martialArtChoices.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
        }

        private void BeginBossBattle()
        {
            if (CurrentState == GameState.BossBattle || CurrentState == GameState.Result)
            {
                return;
            }

            battleRunner.CancelBattle();
            characterManager.ResetHealth();
            bossTransitionPending = false;
            martialArtChoices.Clear();
            StatusMessage = "最终 Boss 战开始，主地图时间不再流逝。";
            SetState(GameState.BossBattle);
            battleRunner.BeginBattle(bossCharacterId, OnBossBattleFinished);
        }

        private void OnBossBattleFinished(bool playerWon)
        {
            EndRun(playerWon, playerWon ? "击败最终 Boss" : "Boss 战失败");
        }

        private void EndRun(bool victory, string message)
        {
            Victory = victory;
            StatusMessage = message;
            bossTransitionPending = false;
            SetState(GameState.Result);
        }

        private void SetState(GameState state)
        {
            CurrentState = state;
            RefreshPlayerMovement();
            StateChanged?.Invoke();
            Updated?.Invoke();
        }

        private void RefreshPlayerMovement()
        {
            playerController?.SetMovementEnabled(CurrentState == GameState.MainMap && !explicitlyPaused);
        }

        private void EnsureConfigured()
        {
            if (characterManager == null || battleRunner == null || playerController == null)
            {
                throw new InvalidOperationException("RunManager 需要绑定 CharacterManager、BattleRunner 和 PlayerController。");
            }
        }
    }
}
