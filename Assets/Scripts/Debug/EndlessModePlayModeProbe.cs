#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.MartialArts;

/// <summary>Controlled integration checks with real battle callbacks; no balance or device approval.</summary>
public sealed partial class EndlessModePlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.EndlessMode", Output="docs/validation/endless_mode";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private readonly List<string> checks=new(),errors=new();
    private readonly Dictionary<string, int> savedPrefs = new();
    private IDisposable progressionScope;
    private void SavePrefs()
    {
        progressionScope = EndlessProgression.IsolateForTests();
        foreach (string key in new[] { "WuxiaRoguelite.TutorialCompleted.v1", "WuxiaRoguelite.LevelTwoCompleted.v1", ChallengeProgress.UnlockKey })
            savedPrefs[key] = PlayerPrefs.GetInt(key, -1);
    }
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Endless Mode Play Mode")]
    public static void Queue()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Key+".Background",Application.runInBackground);
        SessionState.SetFloat(Key+".TimeScale",Time.timeScale);
        Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod]private static void Install(){EditorApplication.playModeStateChanged-=Boot;EditorApplication.playModeStateChanged+=Boot;}
    private static void Boot(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Endless mode probe").AddComponent<EndlessModePlayModeProbe>();}}
    private void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject); SavePrefs();
        background=SessionState.GetBool(Key+".Background",false);timeScale=SessionState.GetFloat(Key+".TimeScale",1);
        Time.timeScale=1;Application.logMessageReceived+=OnLog;Directory.CreateDirectory(Output);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}
            if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        if(error==null&&errors.Count>0)error="Runtime console errors.";
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray(),runtimeErrors=errors.ToArray()},true));
        Restore();Debug.Log("ENDLESS_MODE_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
    }
    private void Restore()
    {
        if(restored)return;restored=true;
        progressionScope?.Dispose(); progressionScope = null;
        foreach (var pair in savedPrefs) { if (pair.Value < 0) PlayerPrefs.DeleteKey(pair.Key); else PlayerPrefs.SetInt(pair.Key, pair.Value); }
        PlayerPrefs.Save(); LevelSequence.CancelPendingRequest();Application.logMessageReceived-=OnLog;Application.runInBackground=background;Time.timeScale=timeScale;
        if(gameView!=null){gameView.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(gameView,previousSize);
            for(int i=0;i<addedSizes;i++){int total=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null);sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup,new object[]{total-1});}}
    }
    private void OnDestroy()=>Restore();
    private void Check(bool ok,string note){if(!ok)throw new Exception(note);checks.Add(note);}
    private static object Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
    private void Resize(int width,int height)
    {
        var assembly=typeof(Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");
        if(gameView==null){gameView=EditorWindow.GetWindow(type);previousSize=(int)type.GetProperty("selectedSizeIndex",Flags).GetValue(gameView);
            var st=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(st);
            var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);sizeGroup=st.GetProperty("currentGroup",Flags).GetValue(sizes);}
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(kind,1),width,height,"Endless probe temporary"});
        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{size});addedSizes++;
        int total=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null);type.GetProperty("selectedSizeIndex",Flags).SetValue(gameView,total-1);gameView.Repaint();
    }
    private IEnumerator Capture(string file){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"/"+file+".png");yield return null;}
    private IEnumerator Load(string scene)
    {
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene);
        yield return null;
    }
    private IEnumerator Until(Func<bool> condition, string label, float timeout = 30f)
    {
        float start = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (Time.realtimeSinceStartup - start > timeout) throw new Exception("Timeout: " + label);
            yield return null;
        }
    }
    private void ChooseOpening()
    {
        F.ConfirmChallengeBriefing(); F.ChooseMartialArt(0); F.bossIntroDuration = 0;
        F.playerStats.runtimeStats.maxHealth = F.playerStats.runtimeStats.currentHealth = 100000;
    }
    private void SameStats(CombatantStats actual, CombatantStats expected, string label)
    {
        Check(Mathf.Approximately(actual.maxHealth, expected.maxHealth) &&
              Mathf.Approximately(actual.attack, expected.attack) &&
              Mathf.Approximately(actual.defense, expected.defense) &&
              Mathf.Approximately(actual.attackSpeed, expected.attackSpeed), label);
    }
    private IEnumerator Suite()
    {
        CheckProgressionEconomy();
        yield return Until(() => !StudioSplashScreen.IsBlocking, "splash");
        yield return Load(LevelSequence.MenuSceneName);
        PlayerPrefs.SetInt("WuxiaRoguelite.TutorialCompleted.v1", 1);
        PlayerPrefs.SetInt("WuxiaRoguelite.LevelTwoCompleted.v1", 0);
        PlayerPrefs.SetInt(ChallengeProgress.UnlockKey, 2);
        F.OpenLevelSelection(); Resize(960, 540); yield return null;
        yield return Capture("selection_landscape");
        Resize(540, 960); yield return null; yield return Capture("selection_portrait");
        F.SelectEndlessMode();
        yield return Until(() => !LevelLoadingScreen.IsLoading && F != null && F.IsEndlessMode, "endless entry");
        Check(F.EndlessRound == 1 && F.mainTimeRemaining == 60 && F.CurrentPhase == GamePhase.LevelUpPaused,
            "Selection loads MainPrototype in endless mode, round one, paused opening choice and 60 seconds");
        F.SelectChallengeTier(2);
        Check(F.ChallengeRun.tier == 0, "Endless starts from training independently of unlocked challenge tiers");
        yield return Capture("briefing_portrait");
        Resize(960, 540); yield return null; yield return Capture("briefing_landscape");
        Check(!F.ExchangeEndlessPoint() && !F.UpgradeEndlessSkill(EndlessSkill.Power), "Opening choice cannot spend permanent currency");
        ChooseOpening();
        var encounters = FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var normal = encounters.First(e => e.encounterType == EncounterType.NormalEnemy);
        var elite = encounters.First(e => e.encounterType == EncounterType.EliteEnemy);
        var normalBase = normal.CreateEnemyStats(); var eliteBase = elite.CreateEnemyStats();
        var authoredNormal = normal.enemyStats.Clone();
        float timer = F.mainTimeRemaining;
        yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining < timer, "Exploration advances the main clock");
        F.mainTimeRemaining = 30.05f;
        yield return Until(() => F.CurrentPhase == GamePhase.MidBossBattle, "mid boss at 30 seconds");
        var midBase = F.battleManager.currentEnemy.Clone(); timer = F.mainTimeRemaining;
        yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining == timer && F.midBossBattleTime > 0, "Mid boss pauses main clock and uses its own clock");
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == GamePhase.MainMapRunning, "mid boss win");
        Check(F.midBossDefeated && F.mainTimeRemaining <= timer, "Mid boss win resumes exploration in the same round");
        F.playerStats.cultivation = F.playerStats.NextLevelRequirement - 1;
        F.mainTimeRemaining = .1f; F.HandleEncounter(normal);
        F.battleManager.currentEnemy.maxHealth = F.battleManager.currentEnemy.currentHealth = 100000;
        yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining == 0 && F.CurrentPhase == GamePhase.NormalBattleRunning && F.IsBossTransitionPending,
            "Normal battle consumes the main clock and survives expiry until its result");
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == GamePhase.LevelUpPaused, "reward choice at expiry");
        Check(F.IsBossTransitionPending, "Expiry preserves the reward choice before final boss");
        F.ChooseMartialArt(0);
        Check(F.CurrentPhase == GamePhase.BossBattle, "Reward choice hands off to the final boss");
        var bossBase = F.battleManager.currentEnemy.Clone(); timer = F.mainTimeRemaining;
        yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining == timer && F.bossBattleTime > 0, "Final boss uses an independent timer");
        F.playerStats.ApplyMartialArt("剑气诀"); F.playerStats.ApplyMartialArt("剑气诀");
        F.playerStats.ApplyMartialArt("毒砂掌"); F.playerStats.ApplyMartialArt("毒砂掌");
        var player = F.playerStats; var stats = player.runtimeStats;
        var ranks = new Dictionary<string, int>(player.martialArtRanks);
        var secrets = new Dictionary<string, int>(player.secretRanks);
        Check(secrets.Count > 0, "Preservation fixture includes an unlocked secret");
        int revision = player.RunRevision, level = player.level, copper = player.copper, xp = player.cultivation;
        int inventory = player.equipment.inventory.Count;
        int pursuit = F.SelectedPursuit;
        F.ChallengeRun.bounties[0].completed = true;
        var pickups = encounters.Where(e => e.encounterType == EncounterType.Treasure || e.encounterType == EncounterType.Herb ||
            e.encounterType == EncounterType.HiddenCave).ToArray();
        foreach (var pickup in pickups) pickup.Consume();
        float damageBeforeRollover = F.battleManager.RunReview.damage.Sum();
        int killsBeforeRollover = player.killCount;
        stats.currentHealth = 12345;
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.EndlessRound == 2, "first rollover");
        Check(F.CurrentPhase == GamePhase.MainMapRunning && F.mainTimeRemaining > 59.5f && !F.midBossDefeated && !F.bossDefeated,
            "Final boss win directly starts round two with a fresh 60-second clock and both bosses reset");
        Check(ReferenceEquals(stats, player.runtimeStats) && player.RunRevision == revision && stats.currentHealth == 12345 &&
            player.level == level && player.copper == copper && player.cultivation == xp && player.equipment.inventory.Count == inventory &&
            ranks.All(p => player.GetMartialArtRank(p.Key) == p.Value) && secrets.All(p => player.secretRanks[p.Key] == p.Value),
            "Rollover preserves HP, martial ranks, secrets, equipment, level, XP and copper without resetting PlayerStats");
        Check(normal.gameObject.activeSelf && F.ChallengeRun.bounties.All(b => !b.completed) && F.SelectedPursuit == pursuit,
            "Consumed encounters and bounties refresh while the selected pursuit persists");
        Check(pickups.Length > 0 && pickups.All(p => !p.consumed && p.gameObject.activeSelf), "Treasure, herb and cave nodes refresh after boss victory");
        Check(F.battleManager.RunReview.damage.Sum() == damageBeforeRollover && player.killCount == killsBeforeRollover + 1,
            "Damage review persists across rounds and final boss kills are counted");
        Check(!LevelSequence.LevelTwoCompleted && ChallengeProgress.HighestUnlocked == 2,
            "Endless victory does not unlock level three or mutate normal challenge progression");
        var expected = normalBase.Clone(); EndlessModeTuning.Apply(expected, 2);
        SameStats(normal.CreateEnemyStats(), expected, "Round two normal enemy scales exactly once");
        SameStats(normal.CreateEnemyStats(), expected, "Repeated enemy previews do not compound scaling");
        expected = eliteBase.Clone(); EndlessModeTuning.Apply(expected, 2);
        SameStats(elite.CreateEnemyStats(), expected, "Round two elite and bounty scale exactly once");
        SameStats(normal.enemyStats, authoredNormal, "Authored enemy template stays unchanged");
        Time.timeScale = 0; yield return Capture("round2_landscape");
        Resize(540, 960); yield return null; yield return Capture("round2_portrait"); Time.timeScale = 1;
        Invoke(F, "SetPhase", GamePhase.CaveRunning);
        var cave = new CombatantStats { maxHealth = 10000, currentHealth = 10000, attack = .1f, defense = 2, attackSpeed = .5f };
        F.BeginCaveBattle(cave, 0, 0, null); timer = F.mainTimeRemaining;
        expected = cave.Clone(); EndlessModeTuning.Apply(expected, 2);
        SameStats(F.battleManager.currentEnemy, expected, "Cave opponents receive the same round scaling once");
        yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining == timer && cave.maxHealth == 10000, "Cave combat pauses main clock without mutating its source");
        F.battleManager.CancelBattle(); Invoke(F, "SetPhase", GamePhase.MainMapRunning);
        F.ForceEnterMidBoss(); expected = midBase.Clone(); EndlessModeTuning.Apply(expected, 2);
        SameStats(F.battleManager.currentEnemy, expected, "Mid boss scales and returns in round two");
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == GamePhase.MainMapRunning, "round two mid win");
        F.ForceEnterBoss(); expected = bossBase.Clone(); EndlessModeTuning.Apply(expected, 2);
        SameStats(F.battleManager.currentEnemy, expected, "Final boss scales exactly once in round two");
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.EndlessRound == 3, "second rollover");
        Check(F.EndlessRoundsCleared == 2 && F.CurrentPhase == GamePhase.MainMapRunning, "Consecutive boss wins continue into round three");
        F.HandleEncounter(normal); player.runtimeStats.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == GamePhase.Result, "normal death");
        Check(F.EndlessRound == 3 && F.EndlessRoundsCleared == 2 && !F.CanContinueToNextLevel, "Normal death ends the run at round three with two cleared rounds");
        timer = F.mainTimeRemaining; yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining == timer, "Death freezes the main clock");
        yield return Capture("result_portrait"); Resize(960, 540); yield return null; yield return Capture("result_landscape");
        yield return CheckProgressionResultAndUpgrade();
        F.RetryCurrentLevel();
        CheckProgressionApplied();
        Check(F.IsEndlessMode && F.EndlessRound == 1 && F.mainTimeRemaining == 60 && player.learnedMartialArts.Count == 0 && player.level == 1,
            "Retry stays endless but resets progress and starts round one");
        foreach (var phase in new[] { GamePhase.CaveRunning, GamePhase.MidBossBattle, GamePhase.BossBattle })
        {
            ChooseOpening();
            if (phase == GamePhase.CaveRunning) { Invoke(F, "SetPhase", phase); F.BeginCaveBattle(cave, 0, 0, null); }
            else if (phase == GamePhase.MidBossBattle) F.ForceEnterMidBoss();
            else F.ForceEnterBoss();
            player.runtimeStats.currentHealth = 0;
            yield return Until(() => F.CurrentPhase == GamePhase.Result, "death in " + phase);
            Check(F.EndlessRound == 1 && F.EndlessRoundsCleared == 0 && !F.bossDefeated, "Death ends endless in " + phase);
            F.RetryCurrentLevel();
        }
        for (int round = 2; round <= 15; round++)
        {
            var previous = normalBase.Clone(); EndlessModeTuning.Apply(previous, round - 1);
            var next = normalBase.Clone(); EndlessModeTuning.Apply(next, round);
            Check(next.maxHealth > previous.maxHealth && next.attack > previous.attack && next.defense > previous.defense && next.attackSpeed > previous.attackSpeed,
                "Enemy stats increase at round " + round);
        }
        var extreme = normalBase.Clone(); EndlessModeTuning.Apply(extreme, int.MaxValue);
        Check(!float.IsInfinity(extreme.maxHealth) && !float.IsNaN(extreme.attack) && Mathf.Approximately(extreme.attackSpeed, normalBase.attackSpeed * 5f),
            "Extreme round arithmetic remains finite with capped attack cadence");
        ChooseOpening();
        foreach (string id in MartialArtCatalog.AllIds)
            for (int rank = 0; rank < MartialArtCatalog.Get(id).maxRank; rank++) player.ApplyMartialArt(id);
        player.cultivation = player.NextLevelRequirement - 1;
        F.midBossDefeated = true; F.mainTimeRemaining = .1f;
        var maxedEnemy = cave.Clone(); maxedEnemy.maxHealth = maxedEnemy.currentHealth = 1000000;
        Invoke(F, "BeginNormalBattle", maxedEnemy, 80, 0, EncounterType.NormalEnemy);
        yield return new WaitForSeconds(.2f);
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == GamePhase.BossBattle, "maxed martial arts expiry handoff");
        Check(F.currentChoices.Count == 0 && F.CurrentPhase == GamePhase.BossBattle,
            "All arts at max rank cannot trap an endless run in an empty upgrade selection");
        yield return CheckEliteAndCaveRewards();
        int walletBeforeLeaving = EndlessProgression.Read().coins;
        F.ReturnToMainMenu();
        yield return Until(() => !LevelLoadingScreen.IsLoading && F != null && LevelSequence.IsMenuScene, "return home");
        Check(!F.IsEndlessMode && F.EndlessRound == 0, "Returning home clears endless state");
        Check(EndlessProgression.Read().coins == walletBeforeLeaving, "Leaving an active run preserves already earned coins without double credit");
        F.SelectLevelTwo();
        yield return Until(() => !LevelLoadingScreen.IsLoading && F != null && F.CurrentPhase == GamePhase.LevelUpPaused, "normal second level");
        Check(!F.IsEndlessMode && F.EndlessRound == 0, "Normal second-level selection does not inherit endless mode");
        CheckNormalModeHasNoProgression();
        ChooseOpening(); F.ForceEnterBoss(); F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == GamePhase.Result, "normal second-level win");
        Check(F.CanContinueToNextLevel && LevelSequence.LevelTwoCompleted, "Ordinary second-level win still ends and unlocks level three");
        Check(EndlessProgression.Read().coins == walletBeforeLeaving, "Ordinary boss victory never awards endless currency");
        yield return Load(LevelSequence.TutorialSceneName);
        Check(!F.IsEndlessMode && Mathf.Approximately(F.playerStats.runtimeStats.attack, untrainedAttack),
            "Tutorial initialization ignores saved endless training");
    }
}
#endif
