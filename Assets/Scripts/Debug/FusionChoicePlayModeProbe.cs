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
public sealed class FusionChoicePlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.FusionChoice", Output="docs/validation/fusion_choice";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private readonly List<string> checks=new(),errors=new(),externalToolErrors=new();
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors,externalToolErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Fusion Choice Play Mode")]
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
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Fusion choice probe").AddComponent<FusionChoicePlayModeProbe>();}}
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
        Restore();Debug.Log("FUSION_CHOICE_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
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
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(kind,1),width,height,"Fusion probe temporary"});
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
        Invoke(F,"BeginLevelTwoAfterTransition");F.ChooseMartialArt(0);F.playerController.enabled=false;
        foreach(var e in FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            e.GetComponent<Collider>().enabled=false;
        var p=F.playerStats;
        var hud=FindAnyObjectByType<PrototypeHUDController>();
        var previews=new List<MartialArtChoiceInsight.FusionPreview>();
        foreach(string secretId in MartialArtCatalog.AllSecretIds)
        {
            var secret=MartialArtCatalog.GetSecret(secretId);
            foreach(int threshold in new[]{2,4})
            {
                p.ResetRun();Depth(secret.firstSchool,threshold-1);Depth(secret.secondSchool,threshold);
                string choice=MartialArtCatalog.AllIds.First(id=>MartialArtCatalog.Get(id).school==secret.firstSchool && p.GetMartialArtRank(id)<MartialArtCatalog.Get(id).maxRank && !MartialArtCatalog.Get(id).isCapstone);
                int before=p.GetMartialArtSchoolRank(secret.firstSchool),rank=p.GetSecretRank(secretId);
                MartialArtChoiceInsight.FusionPreviews(p,choice,previews);
                var preview=previews.First(x=>x.secret.id==secretId);
                Check(preview.Ready&&preview.targetRank==threshold/2,secretId+" preview threshold "+threshold);
                Check(before==p.GetMartialArtSchoolRank(secret.firstSchool)&&rank==p.GetSecretRank(secretId),secretId+" read-only preview "+threshold);
                p.ApplyMartialArt(choice);
                Check(p.GetSecretRank(secretId)==preview.targetRank,secretId+" actual rank matches preview "+threshold);
                Check(Resources.Load<Texture2D>("Icons/"+ContentIconCatalog.MartialArt(secretId))!=null,secretId+" existing icon available "+threshold);
            }
        }
        p.ResetRun();Depth(MartialArtSchool.SwiftSword,3);
        MartialArtChoiceInsight.FusionPreviews(p,"剑气诀",previews);
        Check(previews.All(x=>x.firstBefore==x.firstAfter&&x.secondBefore==x.secondAfter),"rank cap preserves both schools");
        p.ResetRun();Choices("剑气诀","毒砂掌","铁布衫");
        MartialArtChoiceInsight.FusionPreviews(p,"剑气诀",previews);
        Check(previews.Count==2&&previews.All(x=>!x.Ready&&x.Missing==3),"empty build shows both routes and missing partner");
        Resize(960,540);yield return new WaitForSecondsRealtime(.4f);yield return Capture("missing_landscape");
        p.ResetRun();Depth(MartialArtSchool.SwiftSword,1);Depth(MartialArtSchool.VenomPalm,2);Depth(MartialArtSchool.ShadowSteps,2);
        Choices("疾剑式","百毒心经","流云步");
        MartialArtChoiceInsight.FusionPreviews(p,"疾剑式",previews);
        Check(previews.Count(x=>x.Ready)==2,"one choice can visibly unlock two secrets");
        yield return new WaitForSecondsRealtime(.3f);yield return Capture("ready_landscape");
        float main=F.mainTimeRemaining;
        Resize(540,960);yield return new WaitForSecondsRealtime(.4f);yield return Capture("ready_portrait");
        Check(F.mainTimeRemaining==main&&F.CurrentPhase==GamePhase.LevelUpPaused,"orientation change and preview preserve paused timer and choices");
        typeof(PrototypeHUDController).GetField("portraitSelectedArt",Flags).SetValue(hud,"流云步");
        typeof(PrototypeHUDController).GetField("portraitChoiceScroll",Flags).SetValue(hud,new Vector2(0,46));
        yield return null;yield return Capture("third_choice_portrait");
        Check(p.GetMartialArtRank("流云步")==0,"previewing another card does not grant it");
        typeof(PrototypeHUDController).GetField("portraitSelectedArt",Flags).SetValue(hud,"疾剑式");
        typeof(PrototypeHUDController).GetField("portraitChoiceScroll",Flags).SetValue(hud,Vector2.zero);
        F.ChooseMartialArt(0);
        Check(p.GetSecretRank("青锋淬毒")==1&&p.GetSecretRank("无影追风")==1,"real choice awards both secrets");
        Check(p.GetMartialArtRank("剑气诀")==1&&p.GetMartialArtRank("疾剑式")==1,"fusion preserves original arts");
        yield return new WaitForSecondsRealtime(.25f);yield return Capture("learned_portrait");
        Check(F.mainTimeRemaining<main,"fusion feedback does not pause exploration");
        yield return new WaitForSecondsRealtime(2.7f);yield return Capture("second_learned_portrait");
        Check((float)typeof(PrototypeHUDController).GetField("fusionNoticeAge",Flags).GetValue(hud)<2.6f,"simultaneous unlock notices queue instead of overwriting");
        p.ResetRun();Depth(MartialArtSchool.SwiftSword,3);Depth(MartialArtSchool.VenomPalm,4);
        Choices("疾剑式","百毒心经","流云步");
        hud.reduceGrowthMotion=true;
        yield return new WaitForSecondsRealtime(.2f);yield return Capture("rank_two_portrait");
        F.ChooseMartialArt(0);Check(p.GetSecretRank("青锋淬毒")==2,"real choice upgrades secret to rank two");
        yield return new WaitForSecondsRealtime(.3f);yield return Capture("reduced_motion_feedback");
        Choices("疾剑式","百毒心经","流云步");
        MartialArtChoiceInsight.FusionPreviews(p,"疾剑式",previews);
        Check(previews.First(x=>x.secret.id=="青锋淬毒").Mastered,"max-rank secret displays completion");
        Resize(960,540);yield return new WaitForSecondsRealtime(.3f);yield return Capture("mastered_landscape");
        p.ResetRun();yield return null;
        Check((float)typeof(PrototypeHUDController).GetField("fusionNoticeAge",Flags).GetValue(hud)>=2.6f,"restart clears previous fusion notices");
        // Real battle clocks remain independent of this presentation-only UI.
        F.ChooseMartialArt(0);F.mainTimeRemaining=60;
        var battle=F.battleManager;
        p.runtimeStats.maxHealth=p.runtimeStats.currentHealth=100000;
        Invoke(F,"SetPhase",GamePhase.NormalBattleRunning);
        battle.BeginBattle(new CombatantStats { displayName="验证敌人", maxHealth=100000, currentHealth=100000,
            attack=1, attackSpeed=1, defense=0 },null);
        main=F.mainTimeRemaining;yield return new WaitForSeconds(.2f);
        Check(F.mainTimeRemaining<main,"normal combat continues main countdown");battle.CancelBattle();
        Invoke(F,"SetPhase",GamePhase.MainMapRunning);
        var cave=FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .First(x=>x.encounterType==EncounterType.HiddenCave);
        cave.ResetEncounter();cave.caveContent=CaveContentType.Enemy;F.HandleEncounter(cave);Invoke(F.caveRoom,"BeginEvent");
        main=F.mainTimeRemaining;yield return new WaitForSeconds(.2f);
        Check(F.CurrentPhase==GamePhase.CaveRunning&&battle.IsBattleActive&&F.mainTimeRemaining==main,"cave combat pauses main countdown");
        battle.CancelBattle();Invoke(F.caveRoom,"LeaveCave");
        F.bossIntroDuration=0;Invoke(F,"BeginBossBattle");main=F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);
        Check(battle.IsBattleActive&&F.mainTimeRemaining==main&&F.bossBattleTime>0,"final boss uses independent clock");
    }
}
#endif
