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

/// <summary>Opt-in integration check using saved encounters and real combat; never saves fixtures.</summary>
public sealed class LevelTwoMonsterPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.LevelTwoMonsters", Output="docs/validation/level2_monster_pack";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static readonly string[] Ids={"iron_tusk_boar","scarlet_viper","strawhat_bandit","gourd_rogue_monk","lantern_wraith","moss_mushroom_imp"};
    private readonly List<string> checks=new(),errors=new();
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Level 2 Monster Pack Play Mode")]
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
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Level two monster probe").AddComponent<LevelTwoMonsterPlayModeProbe>();}}
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
        Restore();Debug.Log("LEVEL2_MONSTER_PLAYMODE_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
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
    private IEnumerator Suite()
    {
        yield return null;
        var all=FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var normals=all.Where(e=>e.encounterType==EncounterType.NormalEnemy).ToArray();
        Check(normals.Length==40&&normals.Count(e=>Ids.Contains(e.enemyStats.visualId))==18,"40 normal encounters mix 22 original and 18 new monsters");
        Check(Ids.All(id=>normals.Count(e=>e.enemyStats.visualId==id)==3),"each new species appears at exactly three locations");
        var originalIds=normals.ToDictionary(e=>e,e=>e.enemyStats.visualId);
        var battleIds=originalIds.Values.Distinct().OrderBy(id=>id).ToArray();
        Check(all.Count(e=>e.encounterType==EncounterType.EliteEnemy)==10,"ten existing elites retained");
        var screen=FindFirstObjectByType<BattleScreenController>();
        foreach(string id in Ids)
        {
            var p=screen.enemyVisualProfiles.Single(v=>v.id==id);
            Check(p.idleFrames.Length==8&&p.attackFrames.Length==8&&p.flipHorizontally,id+": idle/attack profiles and left-facing battle orientation");
            Check(p.idleFrames.Concat(p.attackFrames).All(s=>s!=null&&s.rect.size==new Vector2(256,256)&&s.pivot==new Vector2(128,32)&&s.pixelsPerUnit==160&&s.texture.filterMode==FilterMode.Point),id+": imported frame sizes and foot pivots");
            var e=normals.First(x=>x.enemyStats.visualId==id);var animator=e.GetComponentInChildren<SpriteFrameAnimator>();
            Check(animator.idleFrames.SequenceEqual(p.idleFrames),id+": map and battle share idle art");
            var initial=animator.GetComponent<SpriteRenderer>().sprite;yield return new WaitForSeconds(.15f);
            Check(animator.GetComponent<SpriteRenderer>().sprite!=initial,id+": map idle animation advances");
        }
        Invoke(F,"BeginLevelTwoAfterTransition");F.ChooseMartialArt(0);F.playerController.enabled=false;
        foreach(var e in all)e.GetComponent<Collider>().enabled=false;
        Resize(540,960);yield return new WaitForSeconds(.2f);
        F.HandleEncounter(normals.First(e=>e.enemyStats.visualId=="iron_tusk_boar"));
        yield return new WaitForSeconds(.3f);yield return Capture("natural_stats_battle");F.battleManager.CancelBattle();
        var player=F.playerStats.runtimeStats;player.maxHealth=player.currentHealth=1000000;player.attack=1;player.attackSpeed=.2f;
        foreach(bool portrait in new[]{false,true})
        {
            Resize(portrait?540:960,portrait?960:540);yield return new WaitForSeconds(.2f);
            Check(Screen.width==(portrait?540:960)&&Screen.height==(portrait?960:540),"actual Game View size "+Screen.width+"x"+Screen.height);
            foreach(string id in battleIds)
            {
                var e=normals.First(x=>x.enemyStats.visualId==id);e.ResetEncounter();
                Invoke(F,"SetPhase",GamePhase.MainMapRunning);F.mainTimeRemaining=60;
                F.HandleEncounter(e);
                Check(F.CurrentPhase==GamePhase.NormalBattleRunning&&F.battleManager.currentEnemy.visualId==id&&e.consumed,id+": saved encounter enters matching normal combat");
                var profile=(BattleScreenController.EnemyVisualProfile)Invoke(screen,"SelectEnemyVisualProfile");
                Check(profile.id==id,id+": battle selects correct profile, no fallback");
                F.battleManager.currentEnemy.maxHealth=F.battleManager.currentEnemy.currentHealth=100000;
                float main=F.mainTimeRemaining;int seq=F.battleManager.AttackSequence;
                yield return new WaitForSeconds(2.0f);
                Check(F.mainTimeRemaining<main&&F.battleManager.AttackSequence>seq,id+": real combat and main timer advance");
                // Freeze cadence after observing real attacks; use the real resolver for a stable action screenshot.
                F.battleManager.StopAllCoroutines();Invoke(F.battleManager,"DoAttack",F.battleManager.currentEnemy,player);
                yield return new WaitForSecondsRealtime(.24f);
                yield return Capture(id+(portrait?"_portrait":"_landscape"));
                F.battleManager.CancelBattle();
            }
        }
        Invoke(F,"SetPhase",GamePhase.CaveRunning);
        var fixture=new CombatantStats{displayName=GameTextCatalog.LanternWraithName,visualId="lantern_wraith",maxHealth=100000,currentHealth=100000,attack=.1f,attackSpeed=.5f};
        F.BeginCaveBattle(fixture.Clone(),0,0,null);float remaining=F.mainTimeRemaining;yield return new WaitForSeconds(.4f);
        Check(F.mainTimeRemaining==remaining,"cave combat pauses main timer");F.battleManager.CancelBattle();
        F.bossIntroDuration=0;F.bossStats.maxHealth=F.bossStats.currentHealth=100000;Invoke(F,"BeginBossBattle");
        remaining=F.mainTimeRemaining;yield return new WaitForSeconds(.4f);
        Check(F.CurrentPhase==GamePhase.BossBattle&&F.bossBattleTime>0&&F.mainTimeRemaining==remaining,"final boss has independent timer");
        F.battleManager.CancelBattle();F.ReturnToMainMenu();Invoke(F,"BeginLevelTwoAfterTransition");F.ChooseMartialArt(0);
        Check(normals.All(e=>!e.consumed&&e.enemyStats.visualId==originalIds[e]),"restart restores all 40 encounters with the same old/new mixture");
        Resize(960,540);yield return new WaitForSeconds(.2f);yield return Capture("level2_map");
    }
}
#endif
