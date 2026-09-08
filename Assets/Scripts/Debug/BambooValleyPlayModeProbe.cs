#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

/// <summary>Editor-only, opt-in integration probe. Restores unlock preferences after the run.</summary>
public sealed class BambooValleyPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.BambooValleyProbe";
    private const string Output="docs/validation/bamboo_valley";
    private const string TutorialKey="WuxiaRoguelite.TutorialCompleted.v1";
    private const string UnlockKey="WuxiaRoguelite.LevelTwoCompleted.v1";
    private readonly List<string> checks=new List<string>();
    private int tutorial,unlock;
    private bool background,restored;
    private GameFlowController Flow=>GameFlowController.Instance;
    [Serializable] private class Report { public bool success; public string error; public string[] checks; public string scope="Controlled real Play Mode; not natural-run balance or device performance approval"; }

    [MenuItem("37 MiniGame/Validate Bamboo Valley Play Mode")]
    public static void Queue()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(SceneManager.GetActiveScene().name!=LevelSequence.LevelThreeSceneName)throw new InvalidOperationException("Open BambooValleyLevel first.");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod] private static void Install()
    {
        EditorApplication.playModeStateChanged-=Bootstrap;EditorApplication.playModeStateChanged+=Bootstrap;
    }
    private static void Bootstrap(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;
        SessionState.SetBool(Key,false);new GameObject("Bamboo Valley controlled validation").AddComponent<BambooValleyPlayModeProbe>();
    }
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Output);
        tutorial=PlayerPrefs.GetInt(TutorialKey,-1);unlock=PlayerPrefs.GetInt(UnlockKey,-1);
        background=Application.runInBackground;Application.runInBackground=true;
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0)
        {
            object next=null;bool moved=false;
            try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception ex){error=ex.ToString();}
            if(error!=null)break;
            if(!moved){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray()},true));
        Restore();Debug.Log("BAMBOO_PLAYMODE_"+(error==null?"PASS":"FAIL "+error));EditorApplication.isPlaying=false;
    }
    private void Restore()
    {
        if(restored)return;restored=true;
        if(tutorial<0)PlayerPrefs.DeleteKey(TutorialKey);else PlayerPrefs.SetInt(TutorialKey,tutorial);
        if(unlock<0)PlayerPrefs.DeleteKey(UnlockKey);else PlayerPrefs.SetInt(UnlockKey,unlock);
        PlayerPrefs.Save();Application.runInBackground=background;Time.timeScale=1;SetInput(Vector2.zero);
    }
    private void OnDestroy(){Restore();}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
    private static void Invoke(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);
    private static void SetInput(Vector2 input)=>typeof(MobileInputController).GetProperty("MoveInput").GetSetMethod(true).Invoke(null,new object[]{input});
    private void Teleport(Vector3 p)
    {
        Flow.playerController.transform.position=p;var body=Flow.playerController.GetComponent<Rigidbody>();body.position=p;body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
    }
    private IEnumerator Move(Vector2 input,float seconds)
    {
        // Count simulated physics steps: screenshot/readback stalls must not consume the movement budget.
        SetInput(input);
        for(int step=0;step<Mathf.CeilToInt(seconds/Time.fixedDeltaTime);step++)
        {SetInput(input);yield return new WaitForFixedUpdate();}
        SetInput(Vector2.zero);yield return new WaitForFixedUpdate();
    }
    private IEnumerator Load(string expected)
    {
        float start=Time.realtimeSinceStartup;
        while(LevelLoadingScreen.IsLoading){if(Time.realtimeSinceStartup-start>45)throw new Exception("Scene load timeout");yield return null;}
        yield return null;
        Check(SceneManager.GetActiveScene().name==expected,"loaded "+expected);
    }
    private void Opening(string context)=>Check(Flow.CurrentPhase==GamePhase.LevelUpPaused&&Flow.mainTimeRemaining==60&&Flow.currentChoices.Count==3,context+": opening choice freezes 60 seconds");
    private IEnumerator Suite()
    {
        yield return null;
        PlayerPrefs.DeleteKey(UnlockKey);Flow.OpenLevelSelection();Flow.SelectLevelThree();
        Check(!LevelLoadingScreen.IsLoading&&!Flow.IsLevelThreeUnlocked,"third level locked before level two victory");
        ScreenCapture.CaptureScreenshot(Output+"/selection_locked.png");yield return null;Flow.CloseLevelSelection();
        Invoke(Flow,"BeginLevelTwoAfterTransition");Opening("direct third level");Flow.ChooseMartialArt(0);
        Check(Flow.CurrentPhase==GamePhase.MainMapRunning,"third level begins exploration after choice");
        var mobile=FindFirstObjectByType<MobileInputController>();mobile.enabled=false;
        Flow.playerController.movementReference=null;
        var encounterColliders=new List<Collider>();
        foreach(var e in FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None))
        {var c=e.GetComponent<Collider>();encounterColliders.Add(c);c.enabled=false;}
        foreach(float z in BambooValleyLayout.BridgeZ)
        {
            float x=BambooValleyLayout.RiverX(z);Teleport(new Vector3(x-3.8f,0,z));
            yield return Move(Vector2.right,1.85f);
            Check(Flow.playerController.transform.position.x>x+3.3f,"Rigidbody movement crosses bridge at z="+z);
        }
        float river=BambooValleyLayout.RiverX(0);Teleport(new Vector3(river-4,0,0));
        yield return Move(Vector2.right,1.4f);
        Check(Flow.playerController.transform.position.x<river-2,"Rigidbody cannot cross water away from bridge");
        Teleport(new Vector3(-2.7f,0,-5.3f));Flow.cameraFollow.ResetVision();
        ScreenCapture.CaptureScreenshot(Output+"/playmode_bridge.png");yield return null;
        foreach(var c in encounterColliders)if(c!=null)c.enabled=true;
        var enemy=Array.Find(FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None),e=>e.name=="山贼喽啰 BV 1");
        enemy.enemyStats.maxHealth=enemy.enemyStats.currentHealth=100000;enemy.enemyStats.attack=.1f;
        Teleport(new Vector3(enemy.transform.position.x-2.5f,0,enemy.transform.position.z));
        yield return Move(Vector2.right,.7f);
        Check(Flow.CurrentPhase==GamePhase.NormalBattleRunning,"touching authored enemy starts normal combat (phase="+Flow.CurrentPhase+", position="+Flow.playerController.transform.position+")");
        float time=Flow.mainTimeRemaining;yield return new WaitForSeconds(.4f);
        Check(Flow.mainTimeRemaining<time,"normal combat consumes main countdown");
        Flow.battleManager.CancelBattle();Invoke(Flow,"SetPhase",GamePhase.MainMapRunning);
        var cave=Array.Find(FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None),e=>e.encounterType==EncounterType.HiddenCave);
        Teleport(new Vector3(20,0,-5));yield return Move(Vector2.up,.65f);
        Check(Flow.CurrentPhase==GamePhase.CaveRunning,"touching authored rock-cave entrance enters cave");
        var fixture=new CombatantStats{displayName=GameTextCatalog.FinalBossName,maxHealth=100000,currentHealth=100000,attack=.1f,attackSpeed=.5f};
        Flow.BeginCaveBattle(fixture.Clone(),0,0,null);time=Flow.mainTimeRemaining;yield return new WaitForSeconds(.4f);
        Check(Flow.mainTimeRemaining==time,"cave combat freezes main countdown");
        Flow.battleManager.CancelBattle();Flow.caveRoom.ResetRoom();Flow.bossIntroDuration=0;Flow.bossStats=fixture.Clone();
        Invoke(Flow,"BeginBossBattle");time=Flow.mainTimeRemaining;yield return new WaitForSeconds(.4f);
        Check(Flow.CurrentPhase==GamePhase.BossBattle&&Flow.bossBattleTime>0&&Flow.mainTimeRemaining==time,"final boss has independent timer");
        Flow.battleManager.currentEnemy.currentHealth=0;yield return WaitForResult();
        Check(Flow.bossDefeated&&!Flow.CanContinueToNextLevel,"third level victory is terminal and does not load nonexistent fourth level");

        LevelSequence.MarkTutorialCompleted();LevelSequence.LoadLevelTwoFromSelection();yield return Load(LevelSequence.LevelTwoSceneName);
        Opening("second level regression");Flow.ChooseMartialArt(0);Flow.bossIntroDuration=0;Flow.ForceEnterBoss();
        yield return new WaitForSeconds(.2f);Flow.battleManager.currentEnemy.currentHealth=0;yield return WaitForResult();
        Check(LevelSequence.LevelTwoCompleted&&Flow.CanContinueToNextLevel,"actual level two boss completion unlocks third level and Next");
        Flow.ContinueToNextLevel();Flow.ContinueToNextLevel();Flow.ReturnToMainMenu();
        yield return Load(LevelSequence.LevelThreeSceneName);Opening("second-to-third handoff");
        Check(!LevelSequence.ConsumeAutoStartRequest(),"third-level auto-start consumed exactly once");
        Flow.ChooseMartialArt(0);Flow.ReturnToMainMenu();Flow.OpenLevelSelection();
        Check(Flow.IsLevelThreeUnlocked,"third level remains selectable after returning home");
        Flow.SelectLevelThree();yield return Load(LevelSequence.LevelThreeSceneName);Opening("third-level replay from selection");
        Flow.ChooseMartialArt(0);
        Teleport(new Vector3(0,0,-5));Flow.cameraFollow.ResetVision();
        ScreenCapture.CaptureScreenshot(Output+"/playmode_final.png");yield return null;
    }
    private IEnumerator WaitForResult()
    {
        float start=Time.realtimeSinceStartup;
        while(Flow.CurrentPhase!=GamePhase.Result){if(Time.realtimeSinceStartup-start>8)throw new Exception("Battle result timeout");yield return null;}
    }
}
#endif
