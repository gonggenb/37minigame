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
public sealed class RouteCombatReviewPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.RouteCombatReview", Output="docs/validation/route_combat_review";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static readonly string[] Ids={"iron_tusk_boar","scarlet_viper","strawhat_bandit","gourd_rogue_monk","lantern_wraith","moss_mushroom_imp"};
    private readonly List<string> checks=new(),errors=new();
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Routes Traits and Review Play Mode")]
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
    private static void Boot(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Level two monster probe").AddComponent<RouteCombatReviewPlayModeProbe>();}}
    private void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    private IEnumerator Start()
    {
        background=SessionState.GetBool(Key+".Background",false);timeScale=SessionState.GetFloat(Key+".TimeScale",1);
        Time.timeScale=1;Application.logMessageReceived+=OnLog;Directory.CreateDirectory(Output);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}
            if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        if(error==null&&errors.Count>0)error="Runtime console errors.";
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray(),runtimeErrors=errors.ToArray()},true));
        Restore();Debug.Log("ROUTE_COMBAT_REVIEW_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
    }
    private void Restore()
    {
        if(restored)return;restored=true;Application.logMessageReceived-=OnLog;Application.runInBackground=background;Time.timeScale=timeScale;
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
    private void BeginRun()
    {
        Invoke(F,"BeginLevelTwoAfterTransition"); F.ChooseMartialArt(0);
        F.playerController.enabled=false;
        foreach(var e in FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None)) e.GetComponent<Collider>().enabled=false;
    }
    private CombatantStats Fixture(EnemyTrait trait=EnemyTrait.None) => new CombatantStats
    {displayName=GameTextCatalog.IronTuskBoarName,visualId="iron_tusk_boar",enemyTrait=trait,maxHealth=1000,currentHealth=1000,attack=10,attackSpeed=1,defense=0,critChance=0,dodgeChance=0};
    private void ControlledBattle(EnemyTrait trait)
    {
        B.CancelBattle();var p=F.playerStats.runtimeStats;p.maxHealth=p.currentHealth=10000;p.defense=0;p.dodgeChance=0;p.lifeSteal=0;p.critChance=0;
        B.BeginBattle(Fixture(trait),null);B.StopAllCoroutines();
        if(trait==EnemyTrait.HeavyOpening||trait==EnemyTrait.VenomBite)Invoke(B,"AbsorbWithShield",100000f);
    }
    private IEnumerator Fight(EncounterTrigger e)
    {
        e.ResetEncounter();e.enemyStats.maxHealth=e.enemyStats.currentHealth=1;e.cultivationReward=e.copperReward=0;
        Invoke(F,"SetPhase",GamePhase.MainMapRunning);F.mainTimeRemaining=60;F.HandleEncounter(e);
        float deadline=Time.realtimeSinceStartup+8;
        while(B.IsBattleActive && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!B.IsBattleActive,"real encounter combat completes: "+e.name);
        while(F.CurrentPhase==GamePhase.LevelUpPaused)F.ChooseMartialArt(0);
    }
    private IEnumerator Suite()
    {
        yield return null;BeginRun();
        var all=FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var normals=all.Where(e=>e.encounterType==EncounterType.NormalEnemy).ToArray();
        Check(normals.Length==40&&normals.Count(e=>Ids.Contains(e.enemyStats.visualId))==18,"old/new mixture remains 22+18");
        Check(all.Count(e=>PingchuanRouteCatalog.ForEncounter(e)==RouteSpecialty.Practice)==12,"practice route has 10 normal and 2 elite targets");
        Check(all.Count(e=>PingchuanRouteCatalog.ForEncounter(e)==RouteSpecialty.Camp)==10,"camp route has 8 normal and 2 elite targets");
        Check(FindObjectsByType<MainMapRegionGuide>(FindObjectsSortMode.None).Count(g=>g.specialty!=RouteSpecialty.None)==3,"three runtime route guides");
        foreach(var route in new[]{RouteSpecialty.Practice,RouteSpecialty.Camp})
        {
            var es=normals.Where(e=>PingchuanRouteCatalog.ForEncounter(e)==route).ToArray();
            yield return Fight(es[0]);Check(F.RouteProgress(route).Contains("1/2"),route+": first win progresses");
            int ranks=F.playerStats.martialArtRanks.Values.Sum(), items=F.playerStats.equipment.inventory.Count;
            yield return Fight(es[1]);Check(F.RouteProgress(route).Contains("已领取"),route+": second win claims reward");
            Check(route==RouteSpecialty.Practice?F.playerStats.martialArtRanks.Values.Sum()>ranks:F.playerStats.equipment.inventory.Count>items,route+": real reward applied");
            SetField("pendingRouteEncounter",es[0]);Check((string)Invoke(F,"ResolveRouteVictory")==string.Empty,route+": repeated encounter cannot pay again");
            SetField("pendingRouteEncounter",es[2]);Check((string)Invoke(F,"ResolveRouteVictory")==string.Empty,route+": third target cannot pay again");
        }
        BeginRun();F.playerStats.ApplyMartialArt("剑气诀");F.playerStats.ApplyMartialArt("剑气诀");
        var cave=all.First(e=>e.encounterType==EncounterType.HiddenCave);cave.caveContent=CaveContentType.Healer;
        F.HandleEncounter(cave);float main=F.mainTimeRemaining;yield return new WaitForSeconds(.15f);
        Check(F.mainTimeRemaining==main,"cave timer stays paused");Invoke(F.caveRoom,"LeaveCave");
        Check(!F.RouteProgress(RouteSpecialty.Cave).Contains("已领取"),"retreat does not pay cave reward");
        cave.ResetEncounter();F.HandleEncounter(cave);Invoke(F.caveRoom,"BeginEvent");Invoke(F.caveRoom,"LeaveCave");
        Check(F.RouteProgress(RouteSpecialty.Cave).Contains("已领取")&&F.playerStats.unlockedSecrets.Count>0,"completed cave grants compatible ranks and unlocks an actual secret");
        int after=F.playerStats.martialArtRanks.Values.Sum();SetField("currentRouteCave",cave);Invoke(F,"ResolveRouteCave",true);
        Check(F.playerStats.martialArtRanks.Values.Sum()==after,"cave reward cannot be farmed");
        BeginRun();F.playerStats.learnedMartialArts.Clear();F.playerStats.martialArtRanks.Clear();F.playerStats.secretRanks.Clear();
        ControlledBattle(EnemyTrait.HeavyOpening);Invoke(B,"DoAttack",B.currentEnemy,F.playerStats.runtimeStats);
        Check(B.LastDamage>=13.3f&&B.LastDamage<=14.7f,"boar first strike is 140 percent before shields");
        Invoke(B,"DoAttack",B.currentEnemy,F.playerStats.runtimeStats);Check(B.LastDamage>=9.5f&&B.LastDamage<=10.5f,"boar later strike is normal");
        ControlledBattle(EnemyTrait.HeavyOpening);F.playerStats.runtimeStats.dodgeChance=1;Invoke(B,"DoAttack",B.currentEnemy,F.playerStats.runtimeStats);
        F.playerStats.runtimeStats.dodgeChance=0;Invoke(B,"DoAttack",B.currentEnemy,F.playerStats.runtimeStats);
        Check(B.LastDamage<=10.5f,"dodging consumes boar opening attack");
        ControlledBattle(EnemyTrait.OpeningArmor);B.ResetRunReview();float hp=B.currentEnemy.currentHealth;
        Invoke(B,"ApplyAttributedDamage",50f,DamageSource.Poison);
        Check(Mathf.Approximately(B.EnemyOpeningArmor,70)&&B.currentEnemy.currentHealth==hp,"poison can reduce opening armor");
        Invoke(B,"ApplyAttributedDamage",2000f,DamageSource.Basic);
        Check(B.EnemyOpeningArmor==0&&B.currentEnemy.IsDead&&Mathf.Approximately(B.RunReview.damage.Sum(),1120),"review caps overkill and includes real armor absorption");
        ControlledBattle(EnemyTrait.VenomBite);Invoke(B,"DoAttack",B.currentEnemy,F.playerStats.runtimeStats);
        Check(B.PlayerVenomTicks==3,"viper landed hit applies three venom ticks");
        float health=F.playerStats.runtimeStats.currentHealth;Invoke(B,"TickEnemyVenom",1f);
        Check(B.PlayerVenomTicks==2&&F.playerStats.runtimeStats.currentHealth<health,"venom actually damages after one combat second");
        B.CancelBattle();Check(B.PlayerVenomTicks==0&&B.EnemyOpeningArmor==0,"cancel clears venom and armor");
        ControlledBattle(EnemyTrait.VenomBite);F.playerStats.runtimeStats.dodgeChance=1;Invoke(B,"DoAttack",B.currentEnemy,F.playerStats.runtimeStats);
        Check(B.PlayerVenomTicks==0,"dodged bite never applies venom");
        B.CancelBattle();B.BeginBossBattle(Fixture(EnemyTrait.OpeningArmor),null);B.StopAllCoroutines();
        Check(B.CurrentEnemyTrait==EnemyTrait.None&&B.EnemyOpeningArmor==0,"boss ignores ordinary monster traits");
        ControlledBattle(EnemyTrait.None);B.ResetRunReview();var player=F.playerStats.runtimeStats;player.attack=20;player.lifeSteal=.5f;player.currentHealth=player.maxHealth;
        Invoke(B,"DoAttack",player,B.currentEnemy);Check(B.RunReview.lifeStealHealing==0,"full-health lifesteal is not counted");
        player.currentHealth-=100;health=player.currentHealth;Invoke(B,"DoAttack",player,B.currentEnemy);
        Check(Mathf.Approximately(B.RunReview.lifeStealHealing,player.currentHealth-health)&&B.RunReview.lifeStealHealing>0,"review counts actual lifesteal healing");
        float shield=B.PlayerShield;Invoke(B,"AbsorbWithShield",10000f);Check(Mathf.Approximately(B.RunReview.shieldAbsorbed,shield),"shield contribution caps at available shield");
        B.CancelBattle();Invoke(F,"SetPhase",GamePhase.NormalBattleRunning);B.BeginBattle(Fixture(EnemyTrait.VenomBite),null);
        float remaining=F.mainTimeRemaining;yield return new WaitForSeconds(.8f);Check(F.mainTimeRemaining<remaining,"ordinary combat still consumes main time");
        Time.timeScale=0;int ticks=B.PlayerVenomTicks;float elapsed=B.BattleElapsed;yield return new WaitForSecondsRealtime(.2f);
        Check(B.PlayerVenomTicks==ticks&&B.BattleElapsed==elapsed,"pause freezes venom and combat");Time.timeScale=1;
        B.CancelBattle();F.bossIntroDuration=0;Invoke(F,"BeginBossBattle");remaining=F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);Check(F.mainTimeRemaining==remaining&&F.bossBattleTime>0,"final boss uses independent timer");
        foreach(bool portrait in new[]{false,true})
        {
            Resize(portrait?540:960,portrait?960:540);yield return new WaitForSeconds(.2f);
            Check(Screen.width==(portrait?540:960),"correct orientation width");
            B.currentEnemy.currentHealth=B.currentEnemy.maxHealth*.27f;Invoke(F,"EndRun",false,"决战落败");
            Check(F.CurrentPhase==GamePhase.Result&&B.RunReview.hasFinalEnemy&&Mathf.Approximately(B.RunReview.finalEnemyHealthRatio,.27f),"result freezes real opponent HP");
            yield return Capture("review_"+(portrait?"portrait":"landscape"));
            F.RetryCurrentLevel();Check(F.CurrentPhase==GamePhase.LevelUpPaused&&F.mainTimeRemaining==60&&!F.midBossDefeated&&!F.bossDefeated,"retry returns directly to opening choice");
            Check(B.RunReview.damage.Sum()==0&&!B.RunReview.hasFinalEnemy&&F.RouteProgress(RouteSpecialty.Practice).Contains("0/2")&&all.All(e=>!e.consumed),"retry resets review, route rewards and all encounters");
            yield return Capture("opening_"+(portrait?"portrait":"landscape"));F.ChooseMartialArt(0);
            var armor=normals.First(e=>e.Trait==EnemyTrait.OpeningArmor);F.HandleEncounter(armor);yield return new WaitForSeconds(.12f);
            yield return Capture("trait_"+(portrait?"portrait":"landscape"));B.CancelBattle();
            Invoke(F,"SetPhase",GamePhase.MainMapRunning);var guide=FindObjectsByType<MainMapRegionGuide>(FindObjectsSortMode.None).First(g=>g.specialty==RouteSpecialty.Practice);
            F.playerController.transform.position=guide.transform.position+new Vector3(0,0,-4);yield return new WaitForSeconds(.3f);
            yield return Capture("route_"+(portrait?"portrait":"landscape"));
            B.BeginBossBattle(Fixture(),null);B.StopAllCoroutines();
        }
        B.CancelBattle();F.ReturnToMainMenu();
    }
}
#endif
