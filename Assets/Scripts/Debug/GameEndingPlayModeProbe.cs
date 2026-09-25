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

/// <summary>Opt-in ending integration check. Switches scenes only during Play Mode; never saves fixtures.</summary>
public sealed class GameEndingPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.GameEnding", Output="docs/validation/game_ending";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private readonly List<string> checks=new(),errors=new();
    private bool restored,background; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Game Ending Play Mode")]
    public static void Queue()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Key+".Background",Application.runInBackground);
        SessionState.SetFloat(Key+".TimeScale",Time.timeScale);
        Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod]private static void Install(){EditorApplication.playModeStateChanged-=Boot;EditorApplication.playModeStateChanged+=Boot;}
    private static void Boot(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Game ending probe").AddComponent<GameEndingPlayModeProbe>();}}
    private void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        background=SessionState.GetBool(Key+".Background",false);timeScale=SessionState.GetFloat(Key+".TimeScale",1);
        Time.timeScale=1;Application.logMessageReceived+=OnLog;Directory.CreateDirectory(Output);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}
            if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        if(error==null&&errors.Count>0)error="Runtime console errors.";
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray(),runtimeErrors=errors.ToArray()},true));
        Restore();Debug.Log("GAME_ENDING_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
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
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(kind,1),width,height,"Ending probe temporary"});
        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{size});addedSizes++;
        int total=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null);type.GetProperty("selectedSizeIndex",Flags).SetValue(gameView,total-1);gameView.Repaint();
    }
    private IEnumerator Capture(string file){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"/"+file+".png");yield return null;}
    private IEnumerator Load(string scene)
    {
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene);
        yield return null;
    }
    private void Begin()
    {
        Invoke(F, "BeginLevelTwoAfterTransition");
        F.ConfirmChallengeBriefing(); F.ChooseMartialArt(0);
        F.playerStats.runtimeStats.maxHealth = F.playerStats.runtimeStats.currentHealth = 100000;
        F.bossIntroDuration = 0;
    }
    private IEnumerator WaitForResult()
    {
        float start = Time.realtimeSinceStartup;
        while (F.CurrentPhase != GamePhase.Result)
        {
            if (Time.realtimeSinceStartup - start > 10) throw new Exception("Result timeout");
            yield return null;
        }
    }
    private IEnumerator Suite()
    {
        while (StudioSplashScreen.IsBlocking) yield return null;
        yield return Load(LevelSequence.LevelTwoSceneName);
        Invoke(F, "EndRun", true, "Validation");
        Check(!F.IsEndingOpen && !F.IsGameCompleted && F.CanContinueToNextLevel, "Level two victory retains Next and does not show ending");
        yield return Load(LevelSequence.TutorialSceneName);
        Invoke(F, "EndRun", true, "Validation");
        Check(!F.IsEndingOpen && !F.IsGameCompleted, "Tutorial result does not show ending");
        yield return Load(LevelSequence.LevelThreeSceneName);
        Resize(960, 540); yield return null; Begin();
        var fixture = new CombatantStats { displayName = GameTextCatalog.FinalBossName, maxHealth = 100000, currentHealth = 100000, attack = .1f, attackSpeed = .5f };
        Invoke(F, "BeginNormalBattle", fixture.Clone(), 0, 0, EncounterType.NormalEnemy);
        float time = F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);
        Check(F.mainTimeRemaining < time, "Normal combat continues main countdown");
        F.battleManager.CancelBattle();
        Invoke(F, "SetPhase", GamePhase.CaveRunning);
        F.BeginCaveBattle(fixture.Clone(), 0, 0, null);
        time = F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);
        Check(F.battleManager.IsBattleActive && F.mainTimeRemaining == time, "Cave combat pauses main countdown");
        F.bossStats = fixture.Clone(); F.ForceEnterBoss(); time = F.mainTimeRemaining;
        yield return new WaitForSeconds(.3f);
        Check(F.mainTimeRemaining == time && F.bossBattleTime > 0, "Boss combat uses independent timer");
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return WaitForResult();
        Check(F.IsGameCompleted && F.IsEndingOpen && !F.IsCreditsVisible && !F.CanContinueToNextLevel, "Actual third boss victory first shows completion notice and has no fourth level");
        yield return Capture("notice_landscape");
        time = F.mainTimeRemaining; float bossTime = F.bossBattleTime;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(3.2f);
        Check(F.IsCreditsVisible, "Credits open automatically after three real seconds even with timeScale zero");
        Check(F.mainTimeRemaining == time && F.bossBattleTime == bossTime, "Ending does not advance either gameplay timer");
        yield return Capture("credits_landscape");
        Resize(540, 960); yield return null;
        yield return Capture("credits_portrait");
        Check(F.IsCreditsVisible, "Orientation change preserves credits state");
        F.DismissEnding(); yield return null;
        Check(F.IsGameCompleted && !F.IsEndingOpen, "Closing credits returns to the completed run review");
        yield return Capture("review_portrait");
        F.ShowEndingCredits(); Check(F.IsCreditsVisible, "Review can reopen credits immediately");
        Time.timeScale = 1;
        F.RetryCurrentLevel(); yield return null;
        Check(!F.IsEndingOpen && !F.IsGameCompleted, "Retry resets ending state");
        F.ChooseMartialArt(0); F.ForceEnterBoss();
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return WaitForResult();
        Check(F.IsEndingOpen && !F.IsCreditsVisible, "A replay victory shows the notice again");
        yield return Capture("notice_portrait");
        F.DismissEnding(); F.RetryCurrentLevel(); F.ChooseMartialArt(0); F.ForceEnterBoss();
        F.playerStats.runtimeStats.currentHealth = 0;
        yield return WaitForResult();
        Check(!F.IsGameCompleted && !F.IsEndingOpen, "Actual third boss defeat does not trigger ending");
        F.ShowEndingCredits(); Check(!F.IsEndingOpen, "Defeat cannot reopen credits");
        F.ReturnToMainMenu();
        while (LevelLoadingScreen.IsLoading) yield return null;
        yield return null;
        Check(F.CurrentPhase == GamePhase.Ready && !F.IsEndingOpen, "Return home clears ending presentation");
    }
}
#endif
