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

/// <summary>Opt-in integration check using saved encounters and real combat; never saves fixtures.</summary>
public sealed class ChallengeReplayPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.ChallengeReplay", Output="docs/validation/replay_challenges";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private readonly List<string> checks=new(),errors=new();
    private readonly string[] prefKeys={ChallengeProgress.UnlockKey,"WuxiaRoguelite.LevelTwoCompleted.v1"};
    private readonly bool[] prefExists=new bool[2];
    private readonly int[] prefValues=new int[2];
    private bool prefsCaptured;
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Replay Challenges Play Mode")]
    public static void Queue()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!=LevelSequence.LevelTwoSceneName)
            throw new InvalidOperationException("Open MainPrototype first.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);
        SessionState.SetFloat(Key+".TimeScale",Time.timeScale);
        Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod]private static void Install(){EditorApplication.playModeStateChanged-=Boot;EditorApplication.playModeStateChanged+=Boot;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeBoot()=>Boot(PlayModeStateChange.EnteredPlayMode);
    private static void Boot(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Level two monster probe").AddComponent<ChallengeReplayPlayModeProbe>();}}
    private void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    private IEnumerator Start()
    {
        background=SessionState.GetBool(Key+".Background",false);timeScale=SessionState.GetFloat(Key+".TimeScale",1);
        for(int i=0;i<2;i++){prefExists[i]=PlayerPrefs.HasKey(prefKeys[i]);prefValues[i]=PlayerPrefs.GetInt(prefKeys[i]);}
        prefsCaptured=true;
        Time.timeScale=1;Application.logMessageReceived+=OnLog;Directory.CreateDirectory(Output);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}
            if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        if(error==null&&errors.Count>0)error="Runtime console errors.";
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray(),runtimeErrors=errors.ToArray()},true));
        Restore();Debug.Log("REPLAY_CHALLENGES_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
    }
    private void Restore()
    {
        if(restored)return;restored=true;Application.logMessageReceived-=OnLog;Application.runInBackground=background;Time.timeScale=timeScale;
        if(prefsCaptured){for(int i=0;i<2;i++){if(prefExists[i])PlayerPrefs.SetInt(prefKeys[i],prefValues[i]);else PlayerPrefs.DeleteKey(prefKeys[i]);}PlayerPrefs.Save();}
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
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(kind,1),width,height,"Monster probe temporary"});
        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{size});addedSizes++;
        int total=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null);type.GetProperty("selectedSizeIndex",Flags).SetValue(gameView,total-1);gameView.Repaint();
    }
    private IEnumerator Capture(string file){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"/"+file+".png");yield return null;}
    private BattleManager B => F.battleManager;
    private void SetField(string name, object value) => typeof(GameFlowController).GetField(name,Flags).SetValue(F,value);
    private void Brief()
    {
        B.CancelBattle(); Invoke(F,"BeginLevelTwoAfterTransition");
        F.playerController.enabled=false;
        foreach(var e in FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            e.GetComponent<Collider>().enabled=false;
    }
    private IEnumerator Suite()
    {
        yield return null;
        Check(F != null, "MainPrototype game flow initialized");
        PlayerPrefs.DeleteKey(prefKeys[0]); PlayerPrefs.DeleteKey(prefKeys[1]);
        SetField("selectedChallengeTier",-1); Brief();
        Check(F.HasRunChallenge && F.IsChallengeBriefingActive && F.ChallengeRun.tier==0,"fresh run opens briefing at training tier");
        Check(ChallengeProgress.HighestUnlocked==0,"fresh save locks higher tiers");
        F.SelectChallengeTier(2); Check(F.ChallengeRun.tier==0,"locked tier cannot be selected");
        var run=F.ChallengeRun;var targets=run.bounties.Select(b=>b.target).ToArray();var approach=run.approach;
        Check(targets.Length==2 && targets.Distinct().Count()==2 && targets.All(e=>e.encounterType==EncounterType.EliteEnemy),"two unique existing elite bounties");
        Check(PingchuanRouteCatalog.ForEncounter(targets[0])!=PingchuanRouteCatalog.ForEncounter(targets[1]),"bounties use distinct branches");
        PlayerPrefs.SetInt(prefKeys[0],2);F.SelectChallengeTier(2);
        Check(run.tier==2 && run.approach==approach && targets.SequenceEqual(run.bounties.Select(b=>b.target)),"tier change preserves intelligence and targets");
        var source=targets[0].enemyStats;float sourceHp=source.maxHealth,sourceAtk=source.attack;
        var clone=targets[0].CreateEnemyStats();var again=targets[0].CreateEnemyStats();
        Check(Mathf.Approximately(clone.maxHealth,sourceHp*1.12f*1.45f) && Mathf.Approximately(clone.attack,sourceAtk*1.08f*1.2f),"bounty and tier strength applied to battle clone");
        Check(source.maxHealth==sourceHp && source.attack==sourceAtk && clone.maxHealth==again.maxHealth,"authored stats unchanged and repeated cloning does not stack");
        Check(clone.enemyTrait==EnemyTrait.OpeningArmor && targets[1].CreateEnemyStats().enemyTrait==EnemyTrait.HeavyOpening,"each bounty retains its announced trait");
        var elite=FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None).First(e=>e.encounterType==EncounterType.EliteEnemy && !targets.Contains(e));
        Check(elite.CreateEnemyStats().enemyTrait==EnemyTrait.HeavyOpening,"desperate elite uses heavy opening");
        F.SelectChallengeTier(1);Check(elite.CreateEnemyStats().enemyTrait==EnemyTrait.OpeningArmor,"dangerous elite uses opening armor");
        foreach(string id in new[]{"challenge_badge","intel_armor","intel_flurry","intel_burst","bounty_writ"})
            Check(ChallengeArt.Get(id)!=null && ChallengeArt.Get(id).width==128,"runtime icon loaded: "+id);
        Resize(540,960); yield return null;yield return Capture("brief_portrait");
        Resize(960,540); yield return null;yield return Capture("brief_landscape");
        F.ConfirmChallengeBriefing();F.ChooseMartialArt(0);F.SelectChallengeTier(0);
        Check(run.tier==1 && !F.IsChallengeBriefingActive,"tier immutable after briefing closes");
        var hud=FindFirstObjectByType<PrototypeHUDController>();
        typeof(PrototypeHUDController).GetField("challengeLedgerOpen",Flags).SetValue(hud,true);
        F.SetCharacterMenuPaused(true);float before=F.mainTimeRemaining;
        yield return new WaitForSecondsRealtime(.2f);Check(F.mainTimeRemaining==before,"reopened intelligence pauses main timer");
        Invoke(hud,"CloseChallengeLedger");yield return new WaitForSeconds(.15f);
        Check(F.mainTimeRemaining<before,"closing intelligence resumes main timer");
        F.playerStats.runtimeStats.maxHealth=F.playerStats.runtimeStats.currentHealth=100000;
        F.playerStats.runtimeStats.attack=100000;
        int ranks=F.playerStats.martialArtRanks.Values.Sum();
        F.HandleEncounter(targets[0]); float deadline=Time.realtimeSinceStartup+10;
        while(B.IsBattleActive && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!B.IsBattleActive && run.bounties[0].completed && F.playerStats.martialArtRanks.Values.Sum()>ranks,"real bounty victory grants learned martial rank");
        SetField("pendingBountyEncounter",targets[0]);Check((string)Invoke(F,"ResolveBountyVictory")==string.Empty,"completed bounty cannot pay twice");
        foreach(string id in F.playerStats.learnedMartialArts.ToArray())
            while(F.playerStats.GetMartialArtRank(id)<MartialArtCatalog.Get(id).maxRank)F.playerStats.ApplyMartialArt(id);
        int copper=F.playerStats.copper;SetField("pendingBountyEncounter",targets[1]);Invoke(F,"ResolveBountyVictory");
        Check(F.playerStats.copper==copper+40 && run.bounties[1].completed,"fully ranked main school receives fallback copper");
        Brief();Check(F.ChallengeRun.approach!=approach && F.ChallengeRun.bounties.All(b=>!b.completed),"retry changes approach and resets bounties");
        F.ChooseMartialArt(0);SetField("pendingBountyEncounter",F.ChallengeRun.bounties[0].target);
        Invoke(F,"OnNormalBattleFinished",false);Check(!F.ChallengeRun.bounties[0].completed,"defeat does not pay bounty");
        PlayerPrefs.DeleteKey(prefKeys[0]);PlayerPrefs.DeleteKey(prefKeys[1]);SetField("selectedChallengeTier",0);Brief();F.ChooseMartialArt(0);
        Invoke(F,"OnBossBattleFinished",true);
        Check(ChallengeProgress.HighestUnlocked==1 && F.ChallengeResult.Contains("解锁"),"training clear permanently unlocks dangerous");
        yield return Capture("result_unlock_landscape");F.RetryNextChallenge();
        while(F.CurrentPhase==GamePhase.OpeningIntro)F.AdvanceOpeningIntro();
        Check(F.ChallengeRun.tier==1 && F.IsChallengeBriefingActive,"next challenge starts newly unlocked tier");
        F.ChooseMartialArt(0);Invoke(F,"OnBossBattleFinished",false);
        Check(ChallengeProgress.HighestUnlocked==1,"boss loss does not unlock desperate");
        Brief();F.ChooseMartialArt(0);Invoke(F,"OnBossBattleFinished",true);
        Check(ChallengeProgress.HighestUnlocked==2,"dangerous clear unlocks desperate");
        ChallengeProgress.RecordWin(2);Check(ChallengeProgress.HighestUnlocked==2,"highest tier capped");
        PlayerPrefs.DeleteKey(prefKeys[0]);PlayerPrefs.SetInt(prefKeys[1],1);Check(ChallengeProgress.HighestUnlocked==1,"existing second-level clear migrates to dangerous");
        Brief();F.ChooseMartialArt(0);F.playerStats.runtimeStats.maxHealth=F.playerStats.runtimeStats.currentHealth=100000;
        foreach(var kind in new[]{BossApproach.Armored,BossApproach.Rapid,BossApproach.Fierce})
        {
            B.CancelBattle();var boss=F.bossStats.Clone();RunChallengeCatalog.ApplyBoss(boss,2,kind,true);
            B.BeginBossBattle(boss,null);B.StopAllCoroutines();
            Check(B.currentEnemy.bossApproach==kind && B.currentEnemy.challengeTier==2,"actual boss receives approach and tier: "+kind);
            Check(kind==BossApproach.Armored ? B.BossWard>0 : B.BossWard==0,"opening ward matches approach: "+kind);
            B.DebugSetBossHealthRatio(.69f);
            Check(B.BossWard>0 && B.BossSkillCooldownRemaining<=2.4f,"dangerous phase two queues follow-up: "+kind);
            B.DebugSetBossHealthRatio(.34f);Check(B.BossSkillCooldownRemaining<=2f,"desperate phase three queues follow-up: "+kind);
        }
        B.CancelBattle();F.bossIntroDuration=0;Invoke(F,"BeginBossBattle");before=F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);Check(F.mainTimeRemaining==before && F.bossBattleTime>0,"final boss retains independent timer");
        Resize(540,960);yield return null;yield return Capture("boss_portrait");
        B.CancelBattle();Brief();F.ChooseMartialArt(0);
        var bounty=F.ChallengeRun.bounties[0];F.playerController.transform.position=bounty.target.transform.position+Vector3.forward*3;
        yield return new WaitForSeconds(.5f);yield return Capture("bounty_map_portrait");
    }
}
#endif
