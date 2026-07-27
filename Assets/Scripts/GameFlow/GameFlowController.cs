using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.MartialArts;
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
            displayName = "九尾妖姬",
            visualId = "fox_demon_boss",
            maxHealth = 550f,
            currentHealth = 550f,
            attack = 18f,
            defense = 5f,
            attackSpeed = 0.8f,
            critChance = 0.08f,
            critMultiplier = 1.6f
        };
        [Min(0f)] public float bossIntroDuration = 4f;

        public GamePhase CurrentPhase { get; private set; } = GamePhase.Ready;
        public bool IsCharacterMenuPaused { get; private set; }
        public bool IsBossTransitionPending { get; private set; }
        public bool IsBossIntroActive { get; private set; }
        public bool IsOpeningIntroActive => CurrentPhase == GamePhase.OpeningIntro;
        public int OpeningDialogueIndex { get; private set; }
        public int OpeningDialogueCount => 5;
        public string OpeningPlayerName =>
            playerStats != null && playerStats.runtimeStats != null
                ? playerStats.runtimeStats.displayName
                : "无名少侠";
        public string CurrentOpeningSpeaker
        {
            get
            {
                return OpeningDialogueIndex switch
                {
                    0 => "旁白",
                    1 => OpeningPlayerName,
                    2 => bossStats.displayName,
                    3 => OpeningPlayerName,
                    _ => "出发提示"
                };
            }
        }
        public string CurrentOpeningDialogue
        {
            get
            {
                return OpeningDialogueIndex switch
                {
                    0 => "暮色压下青崖，村道尽头狐火明灭。山中妖物受九尾妖姬驱使，正截断你的去路。",
                    1 => "妖气一路延至此处……九尾妖姬，现身。",
                    2 => "想见我？先从这些傀儡手里活下来。六十息后，我在血月古刹等你。",
                    3 => "六十息足够。斩妖、寻洞、练功——我会亲自到你面前。",
                    _ => "主香点燃后，只有六十息准备。碰怪会自动交锋且主香不停；进入隐藏洞穴时主香暂停。寻找武学，赶往血月古刹。"
                };
            }
        }
        public float BossIntroTimeRemaining { get; private set; }
        public BossApproachStage CurrentBossApproachStage
        {
            get
            {
                if (CurrentPhase == GamePhase.Ready ||
                    CurrentPhase == GamePhase.BossBattle ||
                    CurrentPhase == GamePhase.Result)
                {
                    return BossApproachStage.None;
                }

                if (IsBossTransitionPending)
                {
                    return BossApproachStage.Arrived;
                }

                if (mainTimeRemaining <= 5f)
                {
                    return BossApproachStage.FinalCountdown;
                }

                if (mainTimeRemaining <= 10f)
                {
                    return BossApproachStage.Imminent;
                }

                if (mainTimeRemaining <= 15f)
                {
                    return BossApproachStage.Omen;
                }

                return BossApproachStage.None;
            }
        }
        public string statusMessage = "按开始进入江湖";
        public bool bossDefeated;
        public int pendingCultivationReward;
        public int pendingCopperReward;
        public readonly string[] allMartialArts = MartialArtCatalog.AllIds;
        public readonly List<string> currentChoices = new List<string>();
        public int martialArtRerollsRemaining = 1;

        private GamePhase phaseBeforeLevelUp = GamePhase.MainMapRunning;
        private float timeScaleBeforeCharacterMenu = 1f;
        private CombatantStats pendingBoss;

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

            mainTimeRemaining = mainTimeLimit;
            bossBattleTime = 0f;
            bossDefeated = false;
            IsBossIntroActive = false;
            BossIntroTimeRemaining = 0f;
            pendingBoss = null;
            ClearOpeningIntro();
            SetPhase(GamePhase.Ready);
            statusMessage = "按开始进入江湖";
        }

        private void Update()
        {
            if (CurrentPhase == GamePhase.MainMapRunning || CurrentPhase == GamePhase.NormalBattleRunning)
            {
                mainTimeRemaining -= Time.deltaTime;
                if (mainTimeRemaining <= 0f)
                {
                    mainTimeRemaining = 0f;
                    if (CurrentPhase == GamePhase.NormalBattleRunning)
                    {
                        MarkBossTransitionPending();
                    }
                    else
                    {
                        BeginBossBattle();
                    }
                }
            }

            if (CurrentPhase == GamePhase.BossBattle)
            {
                if (IsBossIntroActive)
                {
                    BossIntroTimeRemaining = Mathf.Max(0f, BossIntroTimeRemaining - Time.deltaTime);
                    if (BossIntroTimeRemaining <= 0f)
                    {
                        BeginBossCombat();
                    }
                }
                else if (battleManager != null && battleManager.IsBattleActive)
                {
                    bossBattleTime += Time.deltaTime;
                }
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
                encounter.ResetEncounter(rerollCaveContent: true);
            }

            mainTimeRemaining = mainTimeLimit;
            bossBattleTime = 0f;
            bossDefeated = false;
            IsBossTransitionPending = false;
            IsBossIntroActive = false;
            BossIntroTimeRemaining = 0f;
            pendingBoss = null;
            ClearOpeningIntro();
            pendingCultivationReward = 0;
            pendingCopperReward = 0;
            currentChoices.Clear();
            martialArtRerollsRemaining = 1;
            phaseBeforeLevelUp = GamePhase.MainMapRunning;
            BeginOpeningIntro();
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
            int rank = playerStats.ApplyMartialArt(art);
            currentChoices.Clear();
            statusMessage = $"{art} 修至第 {rank} 重，继续探索。";

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

        private void BeginOpeningIntro()
        {
            OpeningDialogueIndex = 0;
            SetPhase(GamePhase.OpeningIntro);
            statusMessage = "狐火初现：完成序章后，主地图六十息倒计时开始。";
        }

        public void AdvanceOpeningIntro()
        {
            if (!IsOpeningIntroActive)
            {
                return;
            }

            if (OpeningDialogueIndex < OpeningDialogueCount - 1)
            {
                OpeningDialogueIndex += 1;
                return;
            }

            ClearOpeningIntro();
            SetPhase(GamePhase.MainMapRunning);
            statusMessage = "主香已点燃：碰怪会自动战斗，主时间继续流逝。";
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

            if (CurrentPhase == GamePhase.LevelUpPaused)
            {
                return;
            }

            if (mainTimeRemaining <= 0f)
            {
                BeginBossBattle();
                return;
            }

            SetPhase(GamePhase.MainMapRunning);
            statusMessage = "战斗胜利，返回主地图。";
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
            List<string> candidates = GetEligibleMartialArts();
            if (candidates.Count == 0)
            {
                return "武学已臻化境";
            }

            string art = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            playerStats.ApplyMartialArt(art);
            return art;
        }

        public void RerollMartialArtChoices()
        {
            if (CurrentPhase != GamePhase.LevelUpPaused || martialArtRerollsRemaining <= 0)
            {
                return;
            }

            martialArtRerollsRemaining -= 1;
            GenerateMartialArtChoices();
            statusMessage = "重观武学残页：选择已刷新。";
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

            IsBossTransitionPending = false;
            caveRoom?.ResetRoom();
            battleManager.CancelBattle();
            ClearOpeningIntro();
            if (playerStats != null && playerStats.runtimeStats != null)
            {
                playerStats.runtimeStats.ResetHealth();
            }

            bossBattleTime = 0f;
            pendingBoss = bossStats.Clone();
            pendingBoss.ResetHealth();
            SetPhase(GamePhase.BossBattle);
            BossIntroTimeRemaining = Mathf.Max(0f, bossIntroDuration);
            IsBossIntroActive = BossIntroTimeRemaining > 0f;
            statusMessage = $"妖气压境：{bossStats.displayName}即将现身。";

            if (!IsBossIntroActive)
            {
                BeginBossCombat();
            }
        }

        private void BeginBossCombat()
        {
            if (CurrentPhase != GamePhase.BossBattle)
            {
                return;
            }

            IsBossIntroActive = false;
            BossIntroTimeRemaining = 0f;
            CombatantStats boss = pendingBoss ?? bossStats.Clone();
            pendingBoss = null;
            boss.ResetHealth();
            statusMessage = "气血已恢复，最终 Boss 战开始：不再消耗主地图 60 秒时间。";
            battleManager.BeginBattle(boss, OnBossBattleFinished);
        }

        private void OnBossBattleFinished(bool playerWon)
        {
            bossDefeated = playerWon;
            EndRun(playerWon, playerWon ? $"击败{bossStats.displayName}" : "Boss 战失败");
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
            GenerateMartialArtChoices();

            SetPhase(GamePhase.LevelUpPaused);
            statusMessage = "修为突破：选择一门武学。所有时间暂停。";
        }

        private void GenerateMartialArtChoices()
        {
            currentChoices.Clear();
            List<string> available = GetEligibleMartialArts();
            if (available.Count == 0)
            {
                return;
            }

            bool hasAnySchool = playerStats.martialArtRanks.Count > 0;
            if (!hasAnySchool)
            {
                foreach (MartialArtSchool school in Enum.GetValues(typeof(MartialArtSchool)))
                {
                    AddRandomChoice(available.Where(id =>
                        MartialArtCatalog.Get(id).school == school &&
                        MartialArtCatalog.Get(id).isStarter).ToList());
                }
            }
            else
            {
                MartialArtSchool dominantSchool = GetDominantSchool();
                AddRandomChoice(available.Where(id =>
                    MartialArtCatalog.Get(id).school == dominantSchool).ToList());

                AddRandomChoice(available.Where(id =>
                    MartialArtCatalog.Get(id).school != dominantSchool &&
                    MartialArtCatalog.Get(id).isStarter).ToList());

                AddRandomChoice(available);
            }

            while (currentChoices.Count < 3)
            {
                int before = currentChoices.Count;
                AddRandomChoice(available);
                if (currentChoices.Count == before)
                {
                    break;
                }
            }

            for (int i = currentChoices.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (currentChoices[i], currentChoices[swapIndex]) =
                    (currentChoices[swapIndex], currentChoices[i]);
            }
        }

        private List<string> GetEligibleMartialArts()
        {
            List<string> available = new List<string>();
            foreach (string artId in allMartialArts)
            {
                MartialArtDefinition definition = MartialArtCatalog.Get(artId);
                if (definition == null || playerStats.GetMartialArtRank(artId) >= definition.maxRank)
                {
                    continue;
                }

                if (definition.isStarter || playerStats.HasMartialArtSchool(definition.school))
                {
                    available.Add(artId);
                }
            }

            return available;
        }

        private MartialArtSchool GetDominantSchool()
        {
            MartialArtSchool dominant = MartialArtSchool.SwiftSword;
            int highestRank = -1;
            foreach (MartialArtSchool school in Enum.GetValues(typeof(MartialArtSchool)))
            {
                int rank = playerStats.GetMartialArtSchoolRank(school);
                if (rank > highestRank)
                {
                    highestRank = rank;
                    dominant = school;
                }
            }

            return dominant;
        }

        private void AddRandomChoice(List<string> candidates)
        {
            candidates.RemoveAll(currentChoices.Contains);
            if (candidates.Count == 0)
            {
                return;
            }

            currentChoices.Add(candidates[UnityEngine.Random.Range(0, candidates.Count)]);
        }

        private void EndRun(bool victory, string reason)
        {
            IsBossTransitionPending = false;
            IsBossIntroActive = false;
            BossIntroTimeRemaining = 0f;
            pendingBoss = null;
            ClearOpeningIntro();
            if (battleManager != null)
            {
                battleManager.CancelBattle();
            }

            caveRoom?.ResetRoom();
            bossDefeated = victory;
            SetPhase(GamePhase.Result);
            statusMessage = reason;
        }

        private void MarkBossTransitionPending()
        {
            if (IsBossTransitionPending)
            {
                return;
            }

            IsBossTransitionPending = true;
            statusMessage = "主地图时间已尽：完成当前战斗后进入最终 Boss 战。";
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

        private void ClearOpeningIntro()
        {
            OpeningDialogueIndex = 0;
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
