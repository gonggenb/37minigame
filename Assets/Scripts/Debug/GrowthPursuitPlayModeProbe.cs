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
public sealed class GrowthPursuitPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.GrowthPursuit", Output="docs/validation/growth_pursuit";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static readonly string[] Ids={"iron_tusk_boar","scarlet_viper","strawhat_bandit","gourd_rogue_monk","lantern_wraith","moss_mushroom_imp"};
    private readonly List<string> checks=new(),errors=new(),externalToolErrors=new();
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors,externalToolErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Growth Pursuit Play Mode")]
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
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Growth pursuit probe").AddComponent<GrowthPursuitPlayModeProbe>();}}
    private void OnLog(string message,string stack,LogType type)
    {
        if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
        // Concurrent MCP menu commands are editor tooling diagnostics, retained separately.
        // Never suppress gameplay exceptions, even if another validation produced them.
        if ((message.Contains("ExecuteMenuItem failed") || message.Contains("[MenuItemExecutor]")) &&
            !message.Contains("Validate Growth Pursuit")) externalToolErrors.Add(message);
        else errors.Add(message);
    }
    private IEnumerator Start()
    {
        background=SessionState.GetBool(Key+".Background",false);timeScale=SessionState.GetFloat(Key+".TimeScale",1);
        Time.timeScale=1;Application.logMessageReceived+=OnLog;Directory.CreateDirectory(Output);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}
            if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        if(error==null&&errors.Count>0)error="Runtime console errors.";
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray(),runtimeErrors=errors.ToArray(),externalToolErrors=externalToolErrors.ToArray()},true));
        Restore();Debug.Log("GROWTH_PURSUIT_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
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
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(kind,1),width,height,"Growth probe temporary"});
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
        yield return new WaitForSecondsRealtime(3.5f);
        BeginRun();
        var player=F.playerStats;
        var hud=FindAnyObjectByType<PrototypeHUDController>();
        var camps=FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(e=>e.encounterType==EncounterType.NormalEnemy&&PingchuanRouteCatalog.ForEncounter(e)==RouteSpecialty.Camp).OrderBy(e=>e.name).ToArray();
        Check(camps.Length>=3,"real camp targets available");
        for(int choice=0;choice<RunPursuitCatalog.Count;choice++)
        {
            Invoke(F,"BeginLevelTwoAfterTransition");
            F.SelectRunPursuit(choice);
            Check(F.SelectedPursuit==choice,"opening accepts pursuit "+choice);
            F.ChooseMartialArt(0);F.playerController.enabled=false;
            F.SelectRunPursuit((choice+1)%3);
            Check(F.SelectedPursuit==choice,"pursuit is locked after opening");
            yield return Fight(camps[0]);Check(F.CampVictoryCount==1&&!F.PursuitComplete,"first win advances without claiming");
            yield return Fight(camps[1]);
            string id=RunPursuitCatalog.ItemId(choice);
            Check(F.PursuitComplete&&player.equipment.HasItem(id),"two real camp wins grant selected item "+id);
            int count=player.equipment.inventory.Count;
            SetField("pendingRouteEncounter",camps[1]);Invoke(F,"ResolveRouteVictory");
            Check(player.equipment.inventory.Count==count,"duplicate callback cannot grant another reward");
            yield return Fight(camps[2]);Check(player.equipment.inventory.Count==count,"third camp win cannot pay pursuit again");
        }
        Invoke(F,"BeginLevelTwoAfterTransition");F.SelectRunPursuit(2);F.ChooseMartialArt(0);
        player.equipment.AddItemById(RunPursuitCatalog.ItemId(2));int before=player.equipment.inventory.Count;
        yield return Fight(camps[0]);yield return Fight(camps[1]);
        Check(player.equipment.inventory.Count==before+1&&player.equipment.inventory.Select(x=>x.id).Distinct().Count()==player.equipment.inventory.Count,
            "already-owned target gives another unowned equipment");
        BeginRun();
        Check(player.CultivationEarned==0&&F.CampVictoryCount==0&&!F.PursuitComplete,"restart clears growth and pursuit");
        player.ResetRun(); // Random starter may grant dodge; matchup baseline must be explicit.
        Check(EnemyMatchupInsight.Counter(EnemyTrait.HeavyOpening,player).Contains("护盾"),"equipped opening shield is a real heavy-hit counter");
        Check(EnemyMatchupInsight.Counter(EnemyTrait.VenomBite,player)=="","base dodge alone is not advertised as a build counter");
        player.ApplyMartialArt("踏雪无痕");
        Check(EnemyMatchupInsight.Counter(EnemyTrait.VenomBite,player).Contains("非免毒"),"enhanced dodge counter does not claim immunity");
        Check(EnemyMatchupInsight.Counter(EnemyTrait.OpeningArmor,player)=="","unbuilt poison/chain counter is not claimed");
        player.equipment.AddItemById("poison_needle_case");
        Check(EnemyMatchupInsight.Counter(EnemyTrait.OpeningArmor,player).Contains("毒伤"),"equipped poison gear drives counter tag");
        player.equipment.Unequip(WuxiaRoguelite.Player.EquipmentSlot.Accessory);
        Check(EnemyMatchupInsight.Counter(EnemyTrait.OpeningArmor,player)=="","unequipped gear cannot advertise counter");
        player.ApplyMartialArt("无影连环剑");
        Check(EnemyMatchupInsight.Counter(EnemyTrait.OpeningArmor,player).Contains("连击"),"actual chain art drives armor counter");
        player.GainCultivation(-2);Check(player.CultivationEarned==0,"negative award produces no feedback");
        Invoke(F,"GiveRewards",5,0);yield return null;
        Check(player.CultivationEarned==5&&player.cultivation==5,"normal award updates resource and feedback exactly once");
        Invoke(F,"GiveRewards",13,0);yield return null;
        Check(player.level==2&&player.cultivation==0&&player.CultivationEarned==18&&F.CurrentPhase==GamePhase.LevelUpPaused,"breakthrough retains full earned feedback across XP wrap");
        F.ChooseMartialArt(0);
        player.GrantRelic("meditation_mat");player.ApplyConsumable("insight_incense");
        Check(player.CultivationEarned==48&&player.cultivation==30,"relic and consumable resource gains share feedback without changing deferred levels");
        foreach(bool portrait in new[]{false,true})
        {
            Resize(portrait?540:960,portrait?960:540);yield return new WaitForSeconds(.25f);
            Invoke(F,"BeginLevelTwoAfterTransition");
            hud.GetType().GetField("challengeBriefScroll",Flags).SetValue(hud,Vector2.zero);
            yield return Capture("pursuits_"+(portrait?"portrait":"landscape"));
            Check(!(bool)hud.GetType().GetProperty("GrowthVisible",Flags).GetValue(hud),"briefing suppresses stale reward banners");
            F.ChooseMartialArt(0);F.playerController.enabled=false;
            Invoke(F,"GiveRewards",5,0);yield return new WaitForSecondsRealtime(.25f);
            yield return Capture("growth_"+(portrait?"portrait":"landscape"));
            Invoke(F,"GiveRewards",13,0);yield return new WaitForSecondsRealtime(.25f);
            yield return Capture("breakthrough_"+(portrait?"portrait":"landscape"));F.ChooseMartialArt(0);
            var enemy=FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None).First(e=>e.Trait==EnemyTrait.HeavyOpening&&!e.consumed);
            F.playerController.transform.position=enemy.transform.position+new Vector3(0,0,-5);
            yield return new WaitForSeconds(.6f);yield return Capture("weakness_"+(portrait?"portrait":"landscape"));
        }
        B.CancelBattle();Invoke(F,"SetPhase",GamePhase.NormalBattleRunning);B.BeginBattle(Fixture(EnemyTrait.VenomBite),null);
        float remaining=F.mainTimeRemaining;yield return new WaitForSeconds(.3f);
        Check(F.mainTimeRemaining<remaining,"normal battle still consumes main timer");
        B.CancelBattle();Invoke(F,"SetPhase",GamePhase.CaveRunning);B.BeginBattle(Fixture(),null);
        remaining=F.mainTimeRemaining;yield return new WaitForSeconds(.3f);
        Check(F.mainTimeRemaining==remaining,"cave battle pauses main timer");
        B.CancelBattle();F.bossIntroDuration=0;Invoke(F,"BeginBossBattle");remaining=F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);
        Check(F.mainTimeRemaining==remaining&&F.bossBattleTime>0,"boss battle uses independent timer");
        B.CancelBattle();
    }
}
#endif
