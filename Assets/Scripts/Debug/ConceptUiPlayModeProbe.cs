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
public sealed class ConceptUiPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.ConceptUi", Output="docs/validation/ui_concept_20261006";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private readonly List<string> checks=new(),errors=new(),externalToolErrors=new();
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors,externalToolErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Concept UI Play Mode")]
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
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Concept UI probe").AddComponent<ConceptUiPlayModeProbe>();}}
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
        Restore();Debug.Log("CONCEPT_UI_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
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
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(kind,1),width,height,"Concept UI temporary"});
        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{size});addedSizes++;
        int total=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null);type.GetProperty("selectedSizeIndex",Flags).SetValue(gameView,total-1);gameView.Repaint();
    }
    private IEnumerator Capture(string file){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"/"+file+".png");yield return null;}
    private void Depth(MartialArtSchool school, int depth)
    {
        foreach (string id in MartialArtCatalog.AllIds)
        {
            var art = MartialArtCatalog.Get(id);
            if (art.school != school || art.isCapstone) continue;
            for (int i = 0; i < art.maxRank && depth > 0; i++, depth--) F.playerStats.ApplyMartialArt(id);
            if (depth == 0) return;
        }
    }
    private void Choices(params string[] ids)
    {
        typeof(GameFlowController).GetField("phaseBeforeLevelUp",Flags).SetValue(F,GamePhase.MainMapRunning);
        F.currentChoices.Clear();F.currentChoices.AddRange(ids);Invoke(F,"SetPhase",GamePhase.LevelUpPaused);
    }
    private IEnumerator Suite()
    {
        yield return new WaitForSecondsRealtime(3.5f);
        Check(Resources.Load<Texture2D>("UI/Theme/tex_ui_panel_default_v03") != null, "v03 shared panel imported");
        Check(Resources.Load<Texture2D>("UI/Theme/tex_ui_panel_boss_v03") != null, "v03 boss frame imported");
        foreach (string state in new[] { "normal", "hover", "pressed", "selected", "primary", "primary_hover" })
            Check(Resources.Load<Texture2D>("UI/Theme/tex_ui_button_" + state + "_v03") != null, state + " button imported");
        Resize(960,540); yield return new WaitForSecondsRealtime(.4f); yield return Capture("01_menu_landscape");
        Resize(540,960); yield return new WaitForSecondsRealtime(.3f); yield return Capture("02_menu_portrait");
        Invoke(F,"BeginLevelTwoAfterTransition"); F.ConfirmChallengeBriefing();
        var hud = FindAnyObjectByType<PrototypeHUDController>();
        var p = F.playerStats;
        F.playerController.enabled = false;
        foreach (var encounter in FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            encounter.GetComponent<Collider>().enabled = false;
        p.ResetRun(); Depth(MartialArtSchool.SwiftSword,1); Depth(MartialArtSchool.VenomPalm,2);
        Choices("疾剑式","百毒心经","流云步");
        float main = F.mainTimeRemaining;
        yield return new WaitForSecondsRealtime(.3f); yield return Capture("03_choices_portrait");
        Resize(960,540); yield return new WaitForSecondsRealtime(.3f); yield return Capture("04_choices_landscape");
        Check(F.mainTimeRemaining == main && F.CurrentPhase == GamePhase.LevelUpPaused, "choice preview and orientation preserve paused timer");
        typeof(PrototypeHUDController).GetField("portraitSelectedArt",Flags).SetValue(hud,"流云步");
        yield return null;
        Check(p.GetMartialArtRank("流云步") == 0, "selection does not grant a martial art before confirmation");
        Resize(375,667); yield return new WaitForSecondsRealtime(.3f); yield return Capture("20_choices_narrow");
        F.ChooseMartialArt(0);
        Check(p.GetSecretRank("青锋淬毒") == 1, "confirmed choice matches real 2+2 fusion threshold");
        F.mainTimeRemaining = 59;
        Resize(540,960); yield return new WaitForSecondsRealtime(4.2f); yield return Capture("05_exploration_portrait");
        Check((float)typeof(PrototypeHUDController).GetField("statusNoticeRemaining",Flags).GetValue(hud) <= 0, "exploration status fades instead of occupying the playfield permanently");
        Resize(960,540); yield return new WaitForSecondsRealtime(.3f); yield return Capture("06_exploration_landscape");
        foreach (var dims in new[] { new Vector2(540,960), new Vector2(390,844), new Vector2(375,667) })
        {
            Rect safe = ResponsiveGui.CalculateSafeArea(new Rect(0,34,dims.x,dims.y-78),dims.x,dims.y);
            var player = (Rect)typeof(PrototypeHUDController).GetMethod("ConceptExplorationPlayerRect",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[] {safe});
            var timer = (Rect)typeof(PrototypeHUDController).GetMethod("ConceptExplorationTimerRect",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[] {safe});
            Check(player.width > 180 && player.xMax <= timer.x && timer.xMax < safe.xMax-58 && safe.Contains(player.min) && safe.Contains(timer.max), "player and timer fit safe area " + dims);
        }
        Invoke(hud,"SetSettingsOpen",true);
        Resize(540,960); yield return new WaitForSecondsRealtime(.3f); yield return Capture("07_settings_portrait");
        main = F.mainTimeRemaining; yield return new WaitForSecondsRealtime(.2f);
        Check(F.mainTimeRemaining == main, "settings pause exploration");
        Invoke(hud,"SetSettingsOpen",false);
        var cave = FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.encounterType==EncounterType.HiddenCave);
        cave.ResetEncounter(); cave.caveContent=CaveContentType.Merchant; F.HandleEncounter(cave); Invoke(F.caveRoom,"BeginEvent");
        p.copper = 100;
        main = F.mainTimeRemaining;
        Resize(540,960); yield return new WaitForSecondsRealtime(.4f); yield return Capture("08_merchant_portrait");
        Check(F.caveRoom.IsModalUiOpen && F.mainTimeRemaining == main && MobileInputController.MoveInput == Vector2.zero, "merchant hides movement and pauses main time");
        int copper = p.copper;
        typeof(CaveRoomController).GetField("portraitMerchantSelection",Flags).SetValue(F.caveRoom,1);
        Resize(960,540); yield return new WaitForSecondsRealtime(.3f); yield return Capture("09_merchant_landscape");
        Check(p.copper == copper, "selecting merchant stock does not spend copper");
        var offers = (System.Collections.IList)typeof(CaveRoomController).GetField("merchantOffers",Flags).GetValue(F.caveRoom);
        object offer = offers[0]; int price = (int)offer.GetType().GetField("price",Flags).GetValue(offer);
        Invoke(F.caveRoom,"PurchaseOffer",offer);
        Check(p.copper == copper-price && (bool)offer.GetType().GetField("sold",Flags).GetValue(offer), "purchase deducts actual price and marks stock sold");
        Invoke(F.caveRoom,"PurchaseOffer",offer);
        Check(p.copper == copper-price, "sold offer cannot be purchased twice");
        typeof(CaveRoomController).GetField("portraitMerchantSelection",Flags).SetValue(F.caveRoom,0);
        Resize(375,667); yield return new WaitForSecondsRealtime(.3f); yield return Capture("10_merchant_narrow_sold");
        Invoke(F.caveRoom,"FinishMerchantEvent"); Invoke(F.caveRoom,"LeaveCave");
        var battle = F.battleManager;
        p.runtimeStats.maxHealth=p.runtimeStats.currentHealth=100000;
        Invoke(F,"SetPhase",GamePhase.NormalBattleRunning);
        battle.BeginBattle(new CombatantStats { displayName="验证敌人", maxHealth=100000, currentHealth=100000,
            attack=1, attackSpeed=1, defense=0 },null);
        main=F.mainTimeRemaining; yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining < main, "normal combat continues main countdown");
        Resize(540,960); yield return new WaitForSecondsRealtime(.3f); yield return Capture("11_normal_battle_portrait");
        Resize(960,540); yield return new WaitForSecondsRealtime(.3f); yield return Capture("12_normal_battle_landscape");
        battle.CancelBattle(); Invoke(F,"SetPhase",GamePhase.MainMapRunning);
        cave.ResetEncounter(); cave.caveContent=CaveContentType.Enemy; F.HandleEncounter(cave); Invoke(F.caveRoom,"BeginEvent");
        main=F.mainTimeRemaining; yield return new WaitForSeconds(.25f);
        Check(F.CurrentPhase==GamePhase.CaveRunning && battle.IsBattleActive && F.mainTimeRemaining==main, "real cave combat pauses main countdown");
        Resize(540,960); yield return new WaitForSecondsRealtime(.3f); yield return Capture("13_cave_battle_portrait");
        battle.CancelBattle(); Invoke(F.caveRoom,"LeaveCave");
        F.bossIntroDuration=0; Invoke(F,"BeginBossBattle"); main=F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);
        Check(battle.IsBattleActive && F.mainTimeRemaining==main && F.bossBattleTime>0, "final boss uses independent elapsed clock");
        yield return Capture("14_boss_portrait");
        Resize(960,540); yield return new WaitForSecondsRealtime(.3f); yield return Capture("15_boss_landscape");
        Check(WuxiaUiComponents.ElapsedText(78) == "01:18", "elapsed timer supports durations beyond sixty seconds");
        battle.CancelBattle(); Invoke(F,"SetPhase",GamePhase.Result);
        Resize(540,960); yield return new WaitForSecondsRealtime(.3f); yield return Capture("16_result_portrait");
        Resize(960,540); yield return new WaitForSecondsRealtime(.3f); yield return Capture("17_result_landscape");
    }
}
#endif
