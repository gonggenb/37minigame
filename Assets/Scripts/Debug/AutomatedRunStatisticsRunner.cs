#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;

/// <summary>
/// Editor-only, scene-driven balance probe. It continuously drives the real player Rigidbody,
/// consumes real EncounterTrigger objects and lets the production battle/flow code resolve each run.
/// Cave navigation and merchant shopping are intentionally excluded: cave events are started and
/// exited immediately so the report measures rewards/combat rather than synthetic UI dexterity.
/// </summary>
[DefaultExecutionOrder(10000)]
public sealed class AutomatedRunStatisticsRunner : MonoBehaviour
{
    private const int DefaultRunCount = 20;
    private const float SimulationTimeScale = 10f;
    private const float TargetArrivalDistance = 1.1f;
    private const float TargetStallLimit = 2.5f;
    private const float RunTimeout = 210f;
    private const string MenuRoot = "37 MiniGame/Automated Run Statistics/";
    private const string SessionRunCountKey = "37MiniGame.AutomatedRunStatistics.RunCount";
    private const string SessionRunModeKey = "37MiniGame.AutomatedRunStatistics.RunMode";
    private const string StandardMode = "standard";
    private const string PairedBalancedMode = "paired_balanced";
    private const string BattlePriorityMode = "battle_priority";

    private static readonly string[] StarterArts =
    {
        "剑气诀", "毒砂掌", "铁布衫", "踏雪无痕", "饮血刀法"
    };

    private static readonly string[] RoutePolicies =
    {
        "均衡", "战斗优先", "洞穴优先", "就近探索"
    };

    private static readonly FieldInfo MoveInputField = typeof(PlayerController).GetField(
        "moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo BeginCaveEventMethod = typeof(CaveRoomController).GetMethod(
        "BeginEvent", BindingFlags.Instance | BindingFlags.NonPublic);

    private sealed class RunRecord
    {
        public int index;
        public int seed;
        public string starter;
        public string policy;
        public bool victory;
        public string resultReason;
        public bool midBossVictory;
        public float simulatedDuration;
        public float mapMovementTime;
        public float normalBattleTime;
        public float caveTime;
        public float levelUpTime;
        public float midBossTime;
        public float bossPhaseTime;
        public float distanceTravelled;
        public int normalEnemies;
        public int eliteEnemies;
        public int caves;
        public int treasures;
        public int herbs;
        public int visionRelics;
        public int mysteryHerbs;
        public int martialChoices;
        public int level;
        public int mapBattleVictories;
        public int momentumRank;
        public int copper;
        public int extraEquipment;
        public int secretCount;
        public bool hasCapstone;
        public float finalHealthRatio;
        public float mainTimeRemaining;
        public float midBossBattleTime;
        public float bossBattleTime;
        public float normalTimerDrop;
        public float caveTimerMaxDrift;
        public float bossTimerMaxDrift;
        public string martialArts;
        public string route;
    }

    private readonly List<RunRecord> records = new List<RunRecord>();
    private readonly Dictionary<EncounterTrigger, bool> consumedState =
        new Dictionary<EncounterTrigger, bool>();
    private readonly Dictionary<EncounterTrigger, float> blockedUntil =
        new Dictionary<EncounterTrigger, float>();
    private readonly List<string> routeEvents = new List<string>();

    private GameFlowController flow;
    private PlayerStats player;
    private PlayerController playerController;
    private CaveRoomController cave;
    private BattleManager battle;
    private EncounterTrigger[] encounters;
    private RunRecord current;
    private EncounterTrigger target;
    private GamePhase previousPhase;
    private Vector3 previousPlayerPosition;
    private Vector2 automatedMoveInput;
    private float previousMainTime;
    private float targetBestDistance;
    private float targetStallTime;
    private float runSimulatedTime;
    private bool caveEventStarted;
    private bool finishing;
    private int requestedRunCount;
    private string requestedRunMode;

    [MenuItem(MenuRoot + "Run 20 Fixed-Seed Runs")]
    private static void RunTwentyFromMenu()
    {
        QueueRuns(DefaultRunCount, StandardMode);
    }

    [MenuItem(MenuRoot + "Run 5-Run Smoke Probe")]
    private static void RunFiveFromMenu()
    {
        QueueRuns(5, StandardMode);
    }

    [MenuItem(MenuRoot + "Run 25 Paired Balanced Runs")]
    private static void RunPairedBalancedFromMenu()
    {
        QueueRuns(StarterArts.Length * 5, PairedBalancedMode);
    }

    [MenuItem(MenuRoot + "Run 5 Battle-Priority Runs")]
    private static void RunBattlePriorityFromMenu()
    {
        QueueRuns(StarterArts.Length, BattlePriorityMode);
    }

    [MenuItem(MenuRoot + "Run 20 Fixed-Seed Runs", true)]
    [MenuItem(MenuRoot + "Run 5-Run Smoke Probe", true)]
    [MenuItem(MenuRoot + "Run 25 Paired Balanced Runs", true)]
    [MenuItem(MenuRoot + "Run 5 Battle-Priority Runs", true)]
    private static bool ValidateRunMenu()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void QueueRuns(int count, string mode)
    {
        SessionState.SetInt(SessionRunCountKey, Mathf.Max(1, count));
        SessionState.SetString(SessionRunModeKey, mode);
        Debug.Log($"[AutoRunStats] Queued {count} scene-driven runs ({mode}).");
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void InstallPlayModeBootstrap()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        int count = SessionState.GetInt(SessionRunCountKey, 0);
        if (count <= 0)
        {
            return;
        }

        CreateRunner(count, SessionState.GetString(SessionRunModeKey, StandardMode));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeBootstrap()
    {
        int count = SessionState.GetInt(SessionRunCountKey, 0);
        if (count > 0)
        {
            CreateRunner(count, SessionState.GetString(SessionRunModeKey, StandardMode));
        }
    }

    private static void CreateRunner(int count, string mode)
    {
        if (FindFirstObjectByType<AutomatedRunStatisticsRunner>() != null)
        {
            return;
        }

        GameObject host = new GameObject("Automated Run Statistics Runner");
        DontDestroyOnLoad(host);
        AutomatedRunStatisticsRunner runner = host.AddComponent<AutomatedRunStatisticsRunner>();
        runner.requestedRunCount = count;
        runner.requestedRunMode = mode;
    }

    private void Start()
    {
        if (requestedRunCount <= 0)
        {
            requestedRunCount = SessionState.GetInt(SessionRunCountKey, DefaultRunCount);
        }
        if (string.IsNullOrEmpty(requestedRunMode))
        {
            requestedRunMode = SessionState.GetString(SessionRunModeKey, StandardMode);
        }

        flow = FindFirstObjectByType<GameFlowController>();
        if (flow == null || flow.IsTutorialLevel)
        {
            Abort("MainPrototype 场景中的 GameFlowController 不可用。");
            return;
        }

        player = flow.playerStats;
        playerController = flow.playerController;
        cave = flow.caveRoom;
        battle = flow.battleManager;
        if (player == null || playerController == null || cave == null || battle == null ||
            MoveInputField == null || BeginCaveEventMethod == null)
        {
            Abort("自动跑局依赖的正式玩法引用或测试反射入口缺失。");
            return;
        }

        flow.bossIntroDuration = 0f;
        Application.runInBackground = true;
        Time.timeScale = SimulationTimeScale;
        StartNextRun();
    }

    private void Update()
    {
        if (finishing || current == null || flow == null)
        {
            return;
        }

        float delta = Time.deltaTime;
        TrackPhase(delta);
        TrackEncounterConsumption();
        runSimulatedTime += delta;

        if (runSimulatedTime >= RunTimeout)
        {
            current.resultReason = "自动跑局超时";
            FinalizeRun();
            return;
        }

        switch (flow.CurrentPhase)
        {
            case GamePhase.OpeningIntro:
                AdvanceOpeningIntro();
                break;
            case GamePhase.LevelUpPaused:
                ChooseMartialArt();
                break;
            case GamePhase.MainMapRunning:
                UpdateMainMapMovement(delta);
                break;
            case GamePhase.CaveRunning:
                UpdateCave();
                break;
            case GamePhase.Result:
                FinalizeRun();
                break;
            default:
                automatedMoveInput = Vector2.zero;
                break;
        }

        previousPhase = flow.CurrentPhase;
        previousMainTime = flow.mainTimeRemaining;
    }

    private void LateUpdate()
    {
        if (playerController != null && MoveInputField != null)
        {
            MoveInputField.SetValue(playerController, automatedMoveInput);
        }
    }

    private void TrackPhase(float delta)
    {
        switch (flow.CurrentPhase)
        {
            case GamePhase.MainMapRunning:
                current.mapMovementTime += delta;
                break;
            case GamePhase.NormalBattleRunning:
                current.normalBattleTime += delta;
                break;
            case GamePhase.CaveRunning:
                current.caveTime += delta;
                break;
            case GamePhase.LevelUpPaused:
                current.levelUpTime += delta;
                break;
            case GamePhase.MidBossBattle:
                current.midBossTime += delta;
                break;
            case GamePhase.BossBattle:
                current.bossPhaseTime += delta;
                break;
        }

        if (previousPhase == flow.CurrentPhase)
        {
            float timerChange = Mathf.Abs(previousMainTime - flow.mainTimeRemaining);
            if (flow.CurrentPhase == GamePhase.NormalBattleRunning && previousMainTime > flow.mainTimeRemaining)
            {
                current.normalTimerDrop += previousMainTime - flow.mainTimeRemaining;
            }
            else if (flow.CurrentPhase == GamePhase.CaveRunning)
            {
                current.caveTimerMaxDrift = Mathf.Max(current.caveTimerMaxDrift, timerChange);
            }
            else if (flow.CurrentPhase == GamePhase.BossBattle)
            {
                current.bossTimerMaxDrift = Mathf.Max(current.bossTimerMaxDrift, timerChange);
            }
        }

        if (flow.CurrentPhase == GamePhase.MainMapRunning && playerController != null)
        {
            Vector3 position = playerController.transform.position;
            if (previousPhase == GamePhase.MainMapRunning)
            {
                current.distanceTravelled += Vector3.Distance(previousPlayerPosition, position);
            }
            previousPlayerPosition = position;
        }
    }

    private void TrackEncounterConsumption()
    {
        foreach (EncounterTrigger encounter in encounters)
        {
            if (encounter == null)
            {
                continue;
            }

            bool wasConsumed = consumedState.TryGetValue(encounter, out bool value) && value;
            if (!wasConsumed && encounter.consumed)
            {
                consumedState[encounter] = true;
                routeEvents.Add(EncounterLabel(encounter));
                switch (encounter.encounterType)
                {
                    case EncounterType.NormalEnemy: current.normalEnemies++; break;
                    case EncounterType.EliteEnemy: current.eliteEnemies++; break;
                    case EncounterType.HiddenCave: current.caves++; break;
                    case EncounterType.Treasure: current.treasures++; break;
                    case EncounterType.Herb: current.herbs++; break;
                    case EncounterType.VisionRelic: current.visionRelics++; break;
                    case EncounterType.MysteryHerb: current.mysteryHerbs++; break;
                }
            }
        }
    }

    private void AdvanceOpeningIntro()
    {
        automatedMoveInput = Vector2.zero;
        for (int i = 0; i < 8 && flow.CurrentPhase == GamePhase.OpeningIntro; i++)
        {
            flow.AdvanceOpeningIntro();
        }

        if (flow.CurrentPhase == GamePhase.LevelUpPaused)
        {
            ForceStarterChoice();
        }
    }

    private void ForceStarterChoice()
    {
        flow.currentChoices.Clear();
        flow.currentChoices.Add(current.starter);
        flow.ChooseMartialArt(0);
        current.martialChoices++;
        previousPlayerPosition = playerController.transform.position;
    }

    private void ChooseMartialArt()
    {
        automatedMoveInput = Vector2.zero;
        if (flow.currentChoices.Count == 0)
        {
            return;
        }

        MartialArtSchool preferredSchool = MartialArtCatalog.Get(current.starter).school;
        int bestIndex = 0;
        float bestScore = float.MinValue;
        for (int i = 0; i < flow.currentChoices.Count; i++)
        {
            MartialArtDefinition definition = MartialArtCatalog.Get(flow.currentChoices[i]);
            if (definition == null)
            {
                continue;
            }

            float score = definition.school == preferredSchool ? 100f : 0f;
            score += definition.isCapstone ? 35f : 0f;
            score += player.GetMartialArtRank(definition.id) * 4f;
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        flow.ChooseMartialArt(bestIndex);
        current.martialChoices++;
    }

    private void UpdateMainMapMovement(float delta)
    {
        caveEventStarted = false;
        if (target == null || target.consumed || !target.gameObject.activeInHierarchy ||
            (blockedUntil.TryGetValue(target, out float blockedTime) && blockedTime > runSimulatedTime))
        {
            target = SelectTarget();
            ResetTargetProgress();
        }

        if (target == null)
        {
            float angle = (current.seed * 17f + runSimulatedTime * 31f) * Mathf.Deg2Rad;
            automatedMoveInput = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return;
        }

        Vector3 playerPosition = playerController.transform.position;
        Vector3 offset = target.transform.position - playerPosition;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance <= TargetArrivalDistance)
        {
            flow.HandleEncounter(target);
            automatedMoveInput = Vector2.zero;
            target = null;
            return;
        }

        if (distance < targetBestDistance - 0.12f)
        {
            targetBestDistance = distance;
            targetStallTime = 0f;
        }
        else
        {
            targetStallTime += delta;
        }

        if (targetStallTime >= TargetStallLimit)
        {
            blockedUntil[target] = runSimulatedTime + 5f;
            target = null;
            automatedMoveInput = Vector2.zero;
            return;
        }

        Vector3 desiredWorldDirection = SteerAroundObstacle(offset.normalized);
        automatedMoveInput = WorldDirectionToInput(desiredWorldDirection);
    }

    private EncounterTrigger SelectTarget()
    {
        Vector3 position = playerController.transform.position;
        float healthRatio = player.runtimeStats == null || player.runtimeStats.maxHealth <= 0f
            ? 0f
            : player.runtimeStats.currentHealth / player.runtimeStats.maxHealth;
        EncounterTrigger best = null;
        float bestScore = float.MinValue;

        foreach (EncounterTrigger candidate in encounters)
        {
            if (candidate == null || candidate.consumed || !candidate.gameObject.activeInHierarchy)
            {
                continue;
            }
            if (blockedUntil.TryGetValue(candidate, out float blockedTime) && blockedTime > runSimulatedTime)
            {
                continue;
            }

            float distance = Vector3.Distance(position, candidate.transform.position);
            float value = EncounterValue(candidate, healthRatio);
            float score = value - distance * 1.15f;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private float EncounterValue(EncounterTrigger encounter, float healthRatio)
    {
        if (current.policy == "战斗优先")
        {
            switch (encounter.encounterType)
            {
                case EncounterType.NormalEnemy: return 85f + encounter.cultivationReward * 0.35f;
                case EncounterType.EliteEnemy:
                    return healthRatio >= 0.6f && encounter.enemyStats.DisplayLevel <= player.level + 1
                        ? 96f + encounter.cultivationReward * 0.4f
                        : -24f;
                case EncounterType.Herb: return healthRatio < 0.6f ? 70f : 10f;
                case EncounterType.Treasure: return 24f;
                case EncounterType.HiddenCave: return 8f;
                default: return 12f;
            }
        }

        if (current.policy == "洞穴优先")
        {
            if (encounter.encounterType == EncounterType.HiddenCave)
            {
                return player.caveEntries < 2 ? 130f : 28f;
            }
            if (encounter.encounterType == EncounterType.Herb && healthRatio < 0.6f)
            {
                return 78f;
            }
            return encounter.encounterType == EncounterType.NormalEnemy ? 35f : 22f;
        }

        if (current.policy == "就近探索")
        {
            if (encounter.encounterType == EncounterType.EliteEnemy &&
                (healthRatio < 0.6f || encounter.enemyStats.DisplayLevel > player.level + 1))
            {
                return -20f;
            }
            return 32f;
        }

        switch (encounter.encounterType)
        {
            case EncounterType.NormalEnemy:
                return 23f + encounter.cultivationReward * 0.25f;
            case EncounterType.EliteEnemy:
                if (healthRatio < 0.55f || encounter.enemyStats.DisplayLevel > player.level + 1)
                {
                    return -18f;
                }
                return 36f + encounter.cultivationReward * 0.3f;
            case EncounterType.HiddenCave:
                return player.caveEntries < 2 ? 42f : 14f;
            case EncounterType.Treasure:
                return 38f;
            case EncounterType.Herb:
                return healthRatio < 0.65f ? 48f : 18f;
            case EncounterType.VisionRelic:
                return 26f;
            case EncounterType.MysteryHerb:
                return healthRatio > 0.7f ? 30f : -12f;
            default:
                return 0f;
        }
    }

    private Vector3 SteerAroundObstacle(Vector3 desired)
    {
        Vector3 origin = playerController.transform.position + Vector3.up * 0.45f + desired * 0.25f;
        float[] offsets = { 0f, 35f, -35f, 70f, -70f, 105f, -105f, 145f, -145f };
        foreach (float angle in offsets)
        {
            Vector3 candidate = Quaternion.Euler(0f, angle, 0f) * desired;
            if (!Physics.SphereCast(origin, 0.22f, candidate, out RaycastHit _, 1.25f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return candidate;
            }
        }
        return -desired;
    }

    private Vector2 WorldDirectionToInput(Vector3 direction)
    {
        Transform reference = playerController.movementReference;
        if (reference == null)
        {
            return new Vector2(direction.x, direction.z).normalized;
        }

        Vector3 right = reference.right;
        Vector3 forward = reference.forward;
        right.y = 0f;
        forward.y = 0f;
        right.Normalize();
        forward.Normalize();
        return new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)).normalized;
    }

    private void UpdateCave()
    {
        automatedMoveInput = Vector2.zero;
        if (battle != null && battle.IsBattleActive)
        {
            return;
        }

        if (!caveEventStarted)
        {
            caveEventStarted = true;
            BeginCaveEventMethod.Invoke(cave, null);
            return;
        }

        // Merchant inventory decisions are not part of this neutral route policy.
        // Other cave types have already resolved their real event or battle reward here.
        cave.ResetRoom();
        flow.ExitHiddenCave(true);
        caveEventStarted = false;
        target = null;
    }

    private void StartNextRun()
    {
        int index = records.Count + 1;
        if (index > requestedRunCount)
        {
            CompleteAllRuns();
            return;
        }

        bool pairedBalanced = requestedRunMode == PairedBalancedMode;
        bool battlePriority = requestedRunMode == BattlePriorityMode;
        int seed = pairedBalanced
            ? 47000 + ((index - 1) / StarterArts.Length) * 97
            : battlePriority ? 37582 + (index - 1) * 97 : 37000 + index * 97;
        string starter = StarterArts[(index - 1) % StarterArts.Length];
        int policyIndex = requestedRunCount <= StarterArts.Length
            ? (index - 1) % RoutePolicies.Length
            : ((index - 1) / StarterArts.Length) % RoutePolicies.Length;
        string policy = pairedBalanced ? "均衡" : battlePriority ? "战斗优先" : RoutePolicies[policyIndex];
        UnityEngine.Random.InitState(seed);
        current = new RunRecord
        {
            index = index,
            seed = seed,
            starter = starter,
            policy = policy
        };
        routeEvents.Clear();
        consumedState.Clear();
        blockedUntil.Clear();
        target = null;
        automatedMoveInput = Vector2.zero;
        targetStallTime = 0f;
        runSimulatedTime = 0f;
        caveEventStarted = false;

        flow.StartRun();
        encounters = FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EncounterTrigger encounter in encounters)
        {
            if (encounter != null)
            {
                consumedState[encounter] = encounter.consumed;
            }
        }

        previousPhase = flow.CurrentPhase;
        previousMainTime = flow.mainTimeRemaining;
        previousPlayerPosition = playerController.transform.position;
        AdvanceOpeningIntro();
        Debug.Log($"[AutoRunStats] Run {index}/{requestedRunCount}, seed {seed}, starter {starter}, policy {policy}.");
    }

    private void ResetTargetProgress()
    {
        targetBestDistance = target == null
            ? float.MaxValue
            : Vector3.Distance(playerController.transform.position, target.transform.position);
        targetStallTime = 0f;
    }

    private void FinalizeRun()
    {
        automatedMoveInput = Vector2.zero;
        current.victory = flow.bossDefeated;
        current.resultReason = string.IsNullOrEmpty(current.resultReason) ? flow.statusMessage : current.resultReason;
        current.midBossVictory = flow.midBossDefeated;
        current.simulatedDuration = runSimulatedTime;
        current.level = player.level;
        current.mapBattleVictories = player.mapBattleVictories;
        current.momentumRank = player.combatMomentumRank;
        current.copper = player.copper;
        current.extraEquipment = player.equipment == null ? 0 : Mathf.Max(0, player.equipment.inventory.Count - 3);
        current.secretCount = player.unlockedSecrets.Count;
        current.hasCapstone = player.learnedMartialArts.Any(id => MartialArtCatalog.Get(id)?.isCapstone == true);
        current.finalHealthRatio = player.runtimeStats == null || player.runtimeStats.maxHealth <= 0f
            ? 0f
            : player.runtimeStats.currentHealth / player.runtimeStats.maxHealth;
        current.mainTimeRemaining = flow.mainTimeRemaining;
        current.midBossBattleTime = flow.midBossBattleTime;
        current.bossBattleTime = flow.bossBattleTime;
        current.martialArts = string.Join(" / ", player.martialArtRanks
            .OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}{pair.Value}"));
        current.route = string.Join(" > ", routeEvents);
        records.Add(current);

        Debug.Log($"[AutoRunStats] Completed {current.index}/{requestedRunCount}: " +
                  $"{(current.victory ? "WIN" : "LOSS")}, kills {current.normalEnemies + current.eliteEnemies}, " +
                  $"caves {current.caves}, level {current.level}, boss {current.bossBattleTime:0.00}s.");
        current = null;

        if (records.Count >= requestedRunCount)
        {
            CompleteAllRuns();
        }
        else
        {
            StartNextRun();
        }
    }

    private void CompleteAllRuns()
    {
        if (finishing)
        {
            return;
        }
        finishing = true;
        automatedMoveInput = Vector2.zero;
        Time.timeScale = 1f;

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        string outputDirectory = Path.Combine(projectRoot, "docs", "validation");
        Directory.CreateDirectory(outputDirectory);
        string date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string modeSuffix = requestedRunMode == PairedBalancedMode
            ? "_paired_balanced"
            : requestedRunMode == BattlePriorityMode ? "_battle_priority" : "_optimized";
        string csvPath = Path.Combine(outputDirectory, $"automated_run_stats{modeSuffix}_{date}.csv");
        string markdownPath = Path.Combine(outputDirectory, $"automated_run_stats{modeSuffix}_{date}.md");
        File.WriteAllText(csvPath, BuildCsv(), new UTF8Encoding(true));
        File.WriteAllText(markdownPath, BuildMarkdown(), new UTF8Encoding(false));
        Debug.Log($"[AutoRunStats] COMPLETE. CSV: {csvPath}\nReport: {markdownPath}");

        SessionState.EraseInt(SessionRunCountKey);
        SessionState.SetString(SessionRunModeKey, string.Empty);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
    }

    private string BuildCsv()
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("run,seed,starter,policy,victory,result,mid_boss_victory,simulated_duration,map_movement_time,normal_battle_time,cave_time,level_up_time,mid_boss_phase_time,boss_phase_time,distance,normal_enemies,elite_enemies,caves,treasures,herbs,vision_relics,mystery_herbs,martial_choices,level,map_battle_victories,momentum_rank,copper,extra_equipment,secrets,capstone,final_hp_ratio,main_time_remaining,mid_boss_battle_time,boss_battle_time,normal_timer_drop,cave_timer_max_drift,boss_timer_max_drift,martial_arts,route");
        foreach (RunRecord r in records)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                r.index.ToString(CultureInfo.InvariantCulture), r.seed.ToString(CultureInfo.InvariantCulture), Csv(r.starter),
                Csv(r.policy), r.victory ? "1" : "0", Csv(r.resultReason), r.midBossVictory ? "1" : "0",
                F(r.simulatedDuration), F(r.mapMovementTime), F(r.normalBattleTime), F(r.caveTime),
                F(r.levelUpTime), F(r.midBossTime), F(r.bossPhaseTime), F(r.distanceTravelled),
                r.normalEnemies.ToString(), r.eliteEnemies.ToString(), r.caves.ToString(), r.treasures.ToString(),
                r.herbs.ToString(), r.visionRelics.ToString(), r.mysteryHerbs.ToString(), r.martialChoices.ToString(),
                r.level.ToString(), r.mapBattleVictories.ToString(), r.momentumRank.ToString(), r.copper.ToString(),
                r.extraEquipment.ToString(), r.secretCount.ToString(),
                r.hasCapstone ? "1" : "0", F(r.finalHealthRatio), F(r.mainTimeRemaining), F(r.midBossBattleTime),
                F(r.bossBattleTime), F(r.normalTimerDrop), F(r.caveTimerMaxDrift), F(r.bossTimerMaxDrift),
                Csv(r.martialArts), Csv(r.route)
            }));
        }
        return csv.ToString();
    }

    private string BuildMarkdown()
    {
        int wins = records.Count(r => r.victory);
        int midWins = records.Count(r => r.midBossVictory);
        List<RunRecord> midBossRuns = records.Where(r => r.midBossTime > 0.05f).ToList();
        int normalBattleRuns = records.Count(r => r.normalEnemies + r.eliteEnemies > 0);
        bool normalRulePassed = records.Where(r => r.normalEnemies + r.eliteEnemies > 0)
            .All(r => r.normalTimerDrop > 0.05f);
        List<RunRecord> caveRuns = records.Where(r => r.caves > 0).ToList();
        List<RunRecord> bossRuns = records.Where(r => r.bossPhaseTime > 0.05f).ToList();
        bool caveRulePassed = caveRuns.Count > 0 && caveRuns.All(r => r.caveTimerMaxDrift <= 0.02f);
        bool bossRulePassed = bossRuns.Count > 0 && bossRuns.All(r => r.bossTimerMaxDrift <= 0.02f);
        StringBuilder md = new StringBuilder();
        md.AppendLine("# 自动跑局统计");
        md.AppendLine();
        md.AppendLine($"- 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine(requestedRunMode == PairedBalancedMode
            ? $"- 样本：{records.Count} 局五流派配对样本；每组五个起手共享同一基础种子"
            : requestedRunMode == BattlePriorityMode
                ? $"- 样本：{records.Count} 局战斗优先定向样本；五种起手各 1 局"
                : $"- 样本：{records.Count} 局固定种子，五种起手循环覆盖");
        md.AppendLine($"- 运行方式：MainPrototype 正式场景、正式遭遇与正式战斗，Time.timeScale={SimulationTimeScale:0}");
        md.AppendLine(requestedRunMode == PairedBalancedMode
            ? "- 路线策略：全部采用均衡策略；同流派武学优先，用于降低策略分组对流派结果的混杂"
            : requestedRunMode == BattlePriorityMode
                ? "- 路线策略：全部采用战斗优先；同流派武学优先，用于定向观察连战回报与战损"
                : "- 路线策略：均衡、战斗优先、洞穴优先、就近探索各 5 局；同流派武学优先");
        md.AppendLine("- 洞穴边界：使用正式洞穴事件/战斗/奖励，但跳过洞穴内移动和商店购买，不代表洞穴操作体验");
        md.AppendLine("- 复现边界：固定 UnityEngine.Random 种子用于控制随机来源；移动与碰撞仍由帧/物理调度驱动，不是逐帧锁步回放");
        md.AppendLine("- 结论边界：这是自动策略的系统和平衡探针，不是真人体验、UI 可用性或真机验收");
        md.AppendLine();
        md.AppendLine("## 总览");
        md.AppendLine();
        md.AppendLine($"- 整体通关：{wins}/{records.Count}（{Percent(wins, records.Count)}）");
        md.AppendLine($"- 到达中期 Boss：{midBossRuns.Count}/{records.Count}；通过 {midWins}/{midBossRuns.Count}（{Percent(midWins, midBossRuns.Count)}）");
        md.AppendLine($"- 到达最终 Boss：{bossRuns.Count}/{records.Count}；战胜 {wins}/{bossRuns.Count}（{Percent(wins, bossRuns.Count)}）");
        md.AppendLine($"- 平均普通/精英：{Average(r => r.normalEnemies):0.00} / {Average(r => r.eliteEnemies):0.00}");
        md.AppendLine($"- 平均洞穴：{Average(r => r.caves):0.00}");
        md.AppendLine($"- 平均武学选择：{Average(r => r.martialChoices):0.00}");
        md.AppendLine($"- 平均等级：{Average(r => r.level):0.00}");
        md.AppendLine($"- 平均主地图战斗胜利：{Average(r => r.mapBattleVictories):0.00}；平均连战磨砺 {Average(r => r.momentumRank):0.00}/{PlayerStats.MaxCombatMomentumRank}");
        md.AppendLine($"- 平均新增装备：{Average(r => r.extraEquipment):0.00}");
        md.AppendLine($"- 绝学出现：{records.Count(r => r.hasCapstone)}/{records.Count}");
        md.AppendLine($"- 秘传出现：{records.Count(r => r.secretCount > 0)}/{records.Count}");
        md.AppendLine($"- 平均主地图移动距离：{Average(r => r.distanceTravelled):0.0}");
        md.AppendLine($"- 平均最终 Boss 战时：{Average(r => r.bossBattleTime):0.00} 秒");
        md.AppendLine();
        md.AppendLine("## 流派结果");
        md.AppendLine();
        md.AppendLine("| 起手 | 样本 | 中期 Boss 胜/到达 | 最终 Boss 胜/到达 | 平均击杀 | 平均洞穴 | 平均等级 | 平均磨砺 | 绝学 | 秘传 |");
        md.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (string starter in StarterArts)
        {
            List<RunRecord> group = records.Where(r => r.starter == starter).ToList();
            if (group.Count == 0) continue;
            int groupMidAttempts = group.Count(r => r.midBossTime > 0.05f);
            int groupBossAttempts = group.Count(r => r.bossPhaseTime > 0.05f);
            md.AppendLine($"| {starter} | {group.Count} | {group.Count(r => r.midBossVictory)}/{groupMidAttempts} | " +
                          $"{group.Count(r => r.victory)}/{groupBossAttempts} | {group.Average(r => r.normalEnemies + r.eliteEnemies):0.00} | " +
                          $"{group.Average(r => r.caves):0.00} | {group.Average(r => r.level):0.00} | {group.Average(r => r.momentumRank):0.00} | " +
                          $"{group.Count(r => r.hasCapstone)} | {group.Count(r => r.secretCount > 0)} |");
        }
        md.AppendLine();
        md.AppendLine("## 路线策略结果");
        md.AppendLine();
        md.AppendLine("| 策略 | 样本 | 中期 Boss 胜/到达 | 最终 Boss 胜/到达 | 平均击杀 | 平均洞穴 | 平均等级 | 平均移动距离 |");
        md.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (string policy in RoutePolicies)
        {
            List<RunRecord> group = records.Where(r => r.policy == policy).ToList();
            if (group.Count == 0) continue;
            int groupMidAttempts = group.Count(r => r.midBossTime > 0.05f);
            int groupBossAttempts = group.Count(r => r.bossPhaseTime > 0.05f);
            md.AppendLine($"| {policy} | {group.Count} | {group.Count(r => r.midBossVictory)}/{groupMidAttempts} | " +
                          $"{group.Count(r => r.victory)}/{groupBossAttempts} | {group.Average(r => r.normalEnemies + r.eliteEnemies):0.00} | " +
                          $"{group.Average(r => r.caves):0.00} | {group.Average(r => r.level):0.00} | " +
                          $"{group.Average(r => r.distanceTravelled):0.0} |");
        }
        md.AppendLine();
        md.AppendLine("## 单局明细");
        md.AppendLine();
        md.AppendLine("| 局 | 种子 | 起手 | 策略 | 结果 | 普通/精英 | 洞穴 | 选择 | 等级 | 磨砺 | 新装备 | 秘传/绝学 | 中期/最终 Boss 秒 | 移动距离 |");
        md.AppendLine("| ---: | ---: | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | --- | --- | ---: |");
        foreach (RunRecord r in records)
        {
            md.AppendLine($"| {r.index} | {r.seed} | {r.starter} | {r.policy} | {(r.victory ? "胜" : "负")} | " +
                          $"{r.normalEnemies}/{r.eliteEnemies} | {r.caves} | {r.martialChoices} | {r.level} | {r.momentumRank} | " +
                          $"{r.extraEquipment} | {r.secretCount}/{(r.hasCapstone ? 1 : 0)} | " +
                          $"{r.midBossBattleTime:0.00}/{r.bossBattleTime:0.00} | {r.distanceTravelled:0.0} |");
        }
        md.AppendLine();
        md.AppendLine("## 三条时间规则（自动观测）");
        md.AppendLine();
        md.AppendLine($"- 普通战斗消耗主时间：{(normalRulePassed ? "通过" : "失败")}（有战斗样本 {normalBattleRuns} 局）");
        md.AppendLine($"- 洞穴暂停主时间：{RuleResult(caveRuns.Count, caveRulePassed)}（覆盖 {caveRuns.Count} 局）");
        md.AppendLine($"- 最终 Boss 主时间保持为零、独立计时：{RuleResult(bossRuns.Count, bossRulePassed)}（覆盖 {bossRuns.Count} 局）");
        md.AppendLine();
        List<RunRecord> balanced = records.Where(r => r.policy == "均衡").ToList();
        md.AppendLine("## 自动样本提示");
        md.AppendLine();
        if (balanced.Count > 0)
        {
            double averageBattles = balanced.Average(r => r.normalEnemies + r.eliteEnemies);
            double averageCaves = balanced.Average(r => r.caves);
            double averageChoices = balanced.Average(r => r.martialChoices);
            double averageEquipment = balanced.Average(r => r.extraEquipment);
            bool routeInTarget = averageBattles >= 5f && averageBattles <= 8f && averageCaves <= 2f;
            bool rewardsInTarget = averageChoices >= 2f && averageChoices <= 4f &&
                                   averageEquipment >= 1f && averageEquipment <= 3f;
            md.AppendLine($"- 均衡策略平均战斗 {averageBattles:0.00} 场、洞穴 {averageCaves:0.00} 个，" +
                          $"{(routeInTarget ? "处于" : "未达到")}当前 5–8 战和 0–2 洞目标；平均武学选择 {averageChoices:0.00} 次、" +
                          $"新增装备 {averageEquipment:0.00} 件，{(rewardsInTarget ? "处于" : "超出")} 2–4 次选择与 1–3 件装备目标。");
        }
        md.AppendLine(bossRuns.Count > 0
            ? $"- 最终 Boss 到达后胜率仅 {Percent(wins, bossRuns.Count)}，是当前自动策略漏斗中最强的失败节点。"
            : "- 本组没有样本到达最终 Boss；应先检查更早的失败节点，不能据此评价最终 Boss 难度。");
        md.AppendLine($"- 跨派秘传出现 {records.Count(r => r.secretCount > 0)}/{records.Count}；若目标是让玩家在一局内体验秘传，当前触发密度不足。");
        md.AppendLine("- 战斗优先策略用更多遭遇换取成长，但会同时承受更多普通战损；击杀数不能单独作为构筑强度指标。");
        md.AppendLine();
        md.AppendLine("完整逐局路线、武学与计时字段见同名 CSV。");
        return md.ToString();
    }

    private float Average(Func<RunRecord, float> selector)
    {
        return records.Count == 0 ? 0f : records.Average(selector);
    }

    private static string EncounterLabel(EncounterTrigger encounter)
    {
        switch (encounter.encounterType)
        {
            case EncounterType.NormalEnemy: return "普通";
            case EncounterType.EliteEnemy: return "精英";
            case EncounterType.HiddenCave: return "洞穴";
            case EncounterType.Treasure: return "宝箱";
            case EncounterType.Herb: return "药草";
            case EncounterType.VisionRelic: return "望气";
            case EncounterType.MysteryHerb: return "奇草";
            default: return encounter.encounterType.ToString();
        }
    }

    private static string Csv(string value)
    {
        return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Percent(int numerator, int denominator)
    {
        return denominator <= 0 ? "0%" : $"{numerator * 100f / denominator:0.#}%";
    }

    private static string RuleResult(int coveredRuns, bool passed)
    {
        return coveredRuns == 0 ? "未覆盖" : passed ? "通过" : "失败";
    }

    private void Abort(string reason)
    {
        finishing = true;
        Time.timeScale = 1f;
        SessionState.EraseInt(SessionRunCountKey);
        SessionState.SetString(SessionRunModeKey, string.Empty);
        Debug.LogError("[AutoRunStats] " + reason);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
    }
}
#endif
