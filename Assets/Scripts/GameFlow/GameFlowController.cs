using System;
using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.GameFlow
{
    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController Instance { get; private set; }

        [Header("Core References")]
        public PlayerStats playerStats;
        public PlayerController playerController;
        public BattleManager battleManager;
        public CaveRoomController caveRoom;

        [Header("Timers")]
        public float mainTimeLimit = 60f;
        public float mainTimeRemaining;
        public float bossBattleTime;

        [Header("Boss")]
        public CombatantStats bossStats = new CombatantStats
        {
            displayName = "黑风寨主",
            maxHealth = 550f,
            currentHealth = 550f,
            attack = 18f,
            defense = 5f,
            attackSpeed = 0.8f,
            critChance = 0.08f,
            critMultiplier = 1.6f
        };

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Ready;
        public bool IsCharacterMenuPaused { get; private set; }
        public string statusMessage = "按开始进入江湖";
        public bool bossDefeated;
        public int pendingCultivationReward;
        public int pendingCopperReward;
        public readonly string[] allMartialArts = { "剑气诀", "疾剑式", "铁布衫", "吸星诀", "毒砂掌", "破甲掌" };
        public readonly List<string> currentChoices = new List<string>();

        private GamePhase phaseBeforeLevelUp = GamePhase.MainMapRunning;
        private float timeScaleBeforeCharacterMenu = 1f;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (battleManager != null)
            {
                battleManager.playerStats = playerStats;
            }

            StartRun();
        }

        private void Update()
        {
            if (CurrentPhase == GamePhase.MainMapRunning || CurrentPhase == GamePhase.NormalBattleRunning)
            {
                mainTimeRemaining -= Time.deltaTime;
                if (mainTimeRemaining <= 0f)
                {
                    mainTimeRemaining = 0f;
                    BeginBossBattle();
                }
            }

            if (CurrentPhase == GamePhase.BossBattle)
            {
                bossBattleTime += Time.deltaTime;
            }
        }

        public void StartRun()
        {
            if (battleManager != null)
            {
                battleManager.CancelBattle();
            }

            caveRoom?.ResetRoom();

            if (playerStats != null)
            {
                playerStats.ResetRun();
            }

            playerController?.ResetToSpawn();

            foreach (EncounterTrigger encounter in FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include))
            {
                encounter.ResetEncounter();
            }

            mainTimeRemaining = mainTimeLimit;
            bossBattleTime = 0f;
            bossDefeated = false;
            pendingCultivationReward = 0;
            pendingCopperReward = 0;
            currentChoices.Clear();
            phaseBeforeLevelUp = GamePhase.MainMapRunning;
            SetPhase(GamePhase.MainMapRunning);
            statusMessage = "主地图探索开始：碰怪会自动战斗，主时间继续流逝。";
        }

        public void SetCharacterMenuPaused(bool paused)
        {
            if (paused && CurrentPhase != GamePhase.MainMapRunning)
            {
                return;
            }

            if (IsCharacterMenuPaused == paused)
            {
                return;
            }

            IsCharacterMenuPaused = paused;
            if (paused)
            {
                timeScaleBeforeCharacterMenu = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = timeScaleBeforeCharacterMenu;
            }

            if (playerController != null)
            {
                playerController.SetMovementEnabled(!paused && CurrentPhase == GamePhase.MainMapRunning);
            }
        }

        public void HandleEncounter(EncounterTrigger encounter)
        {
            if (encounter == null || CurrentPhase != GamePhase.MainMapRunning)
            {
                return;
            }

            switch (encounter.encounterType)
            {
                case EncounterType.NormalEnemy:
                case EncounterType.EliteEnemy:
                    encounter.Consume();
                    BeginNormalBattle(encounter.CreateEnemyStats(), encounter.cultivationReward, encounter.copperReward);
                    break;
                case EncounterType.HiddenCave:
                    encounter.Consume();
                    BeginHiddenCave(encounter);
                    break;
                case EncounterType.Treasure:
                    encounter.Consume();
                    GiveRewards(encounter.cultivationReward, encounter.copperReward);
                    string equipmentName = playerStats.GrantTreasureEquipment();
                    statusMessage = string.IsNullOrEmpty(equipmentName)
                        ? $"打开宝箱：修为 +{encounter.cultivationReward}，铜钱 +{encounter.copperReward}"
                        : $"打开宝箱：获得 {equipmentName}，修为 +{encounter.cultivationReward}";
                    break;
                case EncounterType.Herb:
                    encounter.Consume();
                    playerStats.HealPercent(encounter.healRatio);
                    statusMessage = "采到药草：恢复部分气血。";
                    break;
            }
        }

        public void ChooseMartialArt(int index)
        {
            if (CurrentPhase != GamePhase.LevelUpPaused || index < 0 || index >= currentChoices.Count)
            {
                return;
            }

            string art = currentChoices[index];
            playerStats.ApplyMartialArt(art);
            currentChoices.Clear();
            statusMessage = $"习得 {art}，继续探索。";

            if (mainTimeRemaining <= 0f && phaseBeforeLevelUp == GamePhase.MainMapRunning)
            {
                BeginBossBattle();
                return;
            }

            SetPhase(phaseBeforeLevelUp);
        }

        public void ForceEnterBoss()
        {
            if (CurrentPhase == GamePhase.Result)
            {
                return;
            }

            mainTimeRemaining = 0f;
            BeginBossBattle();
        }

        public void AddDebugCultivation()
        {
            GiveRewards(25, 0);
            statusMessage = "调试：获得 25 修为。";
        }

        public void AddDebugPower()
        {
            playerStats.runtimeStats.attack += 10f;
            playerStats.runtimeStats.Heal(50f);
            statusMessage = "调试：攻击提升并恢复气血。";
        }

        public void DebugEnterCave(CaveContentType content)
        {
            if (CurrentPhase != GamePhase.MainMapRunning || caveRoom == null)
            {
                return;
            }

            playerStats.caveEntries += 1;
            SetPhase(GamePhase.CaveRunning);
            statusMessage = "调试进入隐藏洞穴：主地图倒数暂停。";
            caveRoom.EnterCave(null, content);
        }

        private void BeginNormalBattle(CombatantStats enemy, int cultivationReward, int copperReward)
        {
            pendingCultivationReward = cultivationReward;
            pendingCopperReward = copperReward;
            SetPhase(GamePhase.NormalBattleRunning);
            statusMessage = $"普通战斗：{enemy.displayName}。主地图时间继续流逝。";
            battleManager.BeginBattle(enemy, OnNormalBattleFinished);
        }

        private void OnNormalBattleFinished(bool playerWon)
        {
            if (CurrentPhase == GamePhase.BossBattle || CurrentPhase == GamePhase.Result)
            {
                return;
            }

            if (!playerWon)
            {
                EndRun(false, "普通战斗失败");
                return;
            }

            playerStats.killCount += 1;
            GiveRewards(pendingCultivationReward, pendingCopperReward);
            pendingCultivationReward = 0;
            pendingCopperReward = 0;

            if (CurrentPhase != GamePhase.LevelUpPaused)
            {
                SetPhase(GamePhase.MainMapRunning);
                statusMessage = "战斗胜利，返回主地图。";
            }
        }

        private void BeginHiddenCave(EncounterTrigger encounter)
        {
            if (caveRoom == null)
            {
                statusMessage = "洞穴房间未连接，无法进入。";
                return;
            }

            playerStats.caveEntries += 1;
            SetPhase(GamePhase.CaveRunning);
            statusMessage = "进入隐藏洞穴：主地图 60 秒倒计时暂停。";
            caveRoom.EnterCave(encounter, encounter.caveContent);
        }

        public void BeginCaveBattle(CombatantStats enemy, int cultivationReward, int copperReward, Action<bool> onComplete)
        {
            if (CurrentPhase != GamePhase.CaveRunning || enemy == null)
            {
                return;
            }

            enemy.displayName = string.IsNullOrEmpty(enemy.displayName) ? "守洞武人" : enemy.displayName;
            statusMessage = $"洞穴战斗：{enemy.displayName}。主地图倒数保持暂停。";
            battleManager.BeginBattle(enemy, playerWon =>
            {
                if (!playerWon)
                {
                    EndRun(false, "洞穴挑战失败");
                    onComplete?.Invoke(false);
                    return;
                }

                playerStats.killCount += 1;
                GiveRewards(Mathf.Max(25, cultivationReward), Mathf.Max(8, copperReward));
                statusMessage = "洞穴战斗胜利，主地图倒数仍暂停。";
                onComplete?.Invoke(true);
            });
        }

        public string GrantCaveTreasure()
        {
            if (CurrentPhase != GamePhase.CaveRunning)
            {
                return "古匣没有反应";
            }

            GiveRewards(18, 10);
            string equipmentName = playerStats.GrantTreasureEquipment();
            string art = GrantRandomMartialArt();
            string equipmentText = string.IsNullOrEmpty(equipmentName) ? "一批精炼物资" : equipmentName;
            statusMessage = $"洞穴秘藏：{equipmentText}、{art}、修为与铜钱。";
            return $"{equipmentText}、功法《{art}》、修为 +18、铜钱 +10";
        }

        public string GrantRandomMartialArt()
        {
            string art = allMartialArts[UnityEngine.Random.Range(0, allMartialArts.Length)];
            playerStats.ApplyMartialArt(art);
            return art;
        }

        public void ExitHiddenCave(bool completed)
        {
            if (CurrentPhase != GamePhase.CaveRunning || (battleManager != null && battleManager.IsBattleActive))
            {
                return;
            }

            SetPhase(GamePhase.MainMapRunning);
            statusMessage = completed
                ? "离开隐藏洞穴，主地图时间恢复流逝。"
                : "暂时撤离洞穴，入口仍可再次进入。主地图时间恢复流逝。";
        }

        private void BeginBossBattle()
        {
            if (CurrentPhase == GamePhase.BossBattle || CurrentPhase == GamePhase.Result)
            {
                return;
            }

            caveRoom?.ResetRoom();
            battleManager.CancelBattle();
            bossBattleTime = 0f;
            CombatantStats boss = bossStats.Clone();
            boss.ResetHealth();
            SetPhase(GamePhase.BossBattle);
            statusMessage = "最终 Boss 战开始：不再消耗主地图 60 秒时间。";
            battleManager.BeginBattle(boss, OnBossBattleFinished);
        }

        private void OnBossBattleFinished(bool playerWon)
        {
            bossDefeated = playerWon;
            EndRun(playerWon, playerWon ? "击败黑风寨主" : "Boss 战失败");
        }

        private void GiveRewards(int cultivationReward, int copperReward)
        {
            playerStats.GainCopper(copperReward);
            bool leveledUp = playerStats.GainCultivation(cultivationReward);
            if (leveledUp)
            {
                EnterLevelUp();
            }
        }

        private void EnterLevelUp()
        {
            phaseBeforeLevelUp = CurrentPhase == GamePhase.NormalBattleRunning
                ? GamePhase.MainMapRunning
                : CurrentPhase;
            currentChoices.Clear();
            int start = UnityEngine.Random.Range(0, allMartialArts.Length);
            for (int i = 0; i < 3; i++)
            {
                currentChoices.Add(allMartialArts[(start + i) % allMartialArts.Length]);
            }

            SetPhase(GamePhase.LevelUpPaused);
            statusMessage = "修为突破：选择一门武学。所有时间暂停。";
        }

        private void EndRun(bool victory, string reason)
        {
            if (battleManager != null)
            {
                battleManager.CancelBattle();
            }

            caveRoom?.ResetRoom();
            bossDefeated = victory;
            SetPhase(GamePhase.Result);
            statusMessage = reason;
        }

        private void SetPhase(GamePhase phase)
        {
            if (IsCharacterMenuPaused)
            {
                SetCharacterMenuPaused(false);
            }

            CurrentPhase = phase;

            bool canMove = phase == GamePhase.MainMapRunning;
            if (playerController != null)
            {
                playerController.SetMovementEnabled(canMove);
            }
        }

        private void OnDisable()
        {
            if (IsCharacterMenuPaused)
            {
                SetCharacterMenuPaused(false);
            }
        }
    }
}
