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
using WuxiaRoguelite.UI;

/// <summary>Opt-in controlled runtime integration checks; never ships in a player build.</summary>
public sealed class TutorialRestStopPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.RestStopProbe", Output="docs/validation/tutorial_reststop";
    private const string Pref="WuxiaRoguelite.TutorialCompleted.v1";
    private readonly List<string> checks=new List<string>();
    private int previous;private bool background,restored;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report{public bool success;public string error;public string[] checks;public string scope="Real Play Mode, controlled input and fixtures. Not device performance or natural player balance acceptance.";}
    [MenuItem("37 MiniGame/Validate Tutorial Rest Stop Play Mode")]
    public static void Queue()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="TutorialLevel")throw new Exception("Open TutorialLevel first.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod]private static void Install(){EditorApplication.playModeStateChanged-=Boot;EditorApplication.playModeStateChanged+=Boot;}
    private static void Boot(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Tutorial validation").AddComponent<TutorialRestStopPlayModeProbe>();}}
    private IEnumerator Start()
    {
        previous=PlayerPrefs.GetInt(Pref,-1);background=SessionState.GetBool(Key+".Background",false);Application.runInBackground=true;
        Directory.CreateDirectory(Output);var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray()},true));
        Restore();Debug.Log("REST_STOP_PLAYMODE_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
    }
    private void Restore(){if(restored)return;restored=true;if(previous<0)PlayerPrefs.DeleteKey(Pref);else PlayerPrefs.SetInt(Pref,previous);PlayerPrefs.Save();Application.runInBackground=background;Time.timeScale=1;InputMove(Vector2.zero);}
    private void OnDestroy()=>Restore();
    private void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add(message);}
    private static void Invoke(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,args);
    private static void InputMove(Vector2 v)=>typeof(MobileInputController).GetProperty("MoveInput").GetSetMethod(true).Invoke(null,new object[]{v});
    private void Teleport(Vector3 p){F.playerController.transform.position=p;var b=F.playerController.GetComponent<Rigidbody>();b.position=p;b.linearVelocity=Vector3.zero;Physics.SyncTransforms();}
    private IEnumerator Move(Vector2 v,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/Time.fixedDeltaTime);i++){InputMove(v);yield return new WaitForFixedUpdate();}InputMove(Vector2.zero);yield return new WaitForFixedUpdate();}
    private IEnumerator Wait(Func<bool> condition,string name,float limit=12){float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>limit)throw new Exception(name+" timeout: "+F.CurrentPhase);yield return null;}}
    private IEnumerator Lesson(string title)
    {
        Check(F.IsTutorialLessonActive&&F.CurrentTutorialLesson.Title.Contains(title),"first touch lesson: "+title);
        float t=F.mainTimeRemaining;yield return new WaitForSeconds(.35f);Check(F.mainTimeRemaining==t,"reading freezes timer: "+title);F.ConfirmTutorialLesson();yield return null;
    }
    private IEnumerator Suite()
    {
        yield return new WaitForSeconds(.4f);
        Check(F.IsTutorialNoticeActive&&F.mainTimeRemaining==30,"opening notice starts at 30 seconds");
        Check(Mathf.Approximately(F.playerStats.runtimeStats.maxHealth,F.playerStats.baseStats.maxHealth+18)&&F.playerStats.equipment.equippedArmor!=null,"tutorial initializes base health plus starting armor");
        var encounters=FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Check(encounters.Length==4,"exactly four encounter objects including inactive objects");
        var herb=encounters.First(e=>e.encounterType==EncounterType.Herb);var chest=encounters.First(e=>e.encounterType==EncounterType.Treasure);
        var enemy=encounters.First(e=>e.encounterType==EncounterType.NormalEnemy);var cave=encounters.First(e=>e.encounterType==EncounterType.HiddenCave);
        var props=FindFirstObjectByType<TutorialRestStopProps>();
        Check(props.chestRenderers.Length>0&&props.herbRenderers.Length>0,"authored consumable meshes mapped");
        F.DismissTutorialNotice();FindFirstObjectByType<MobileInputController>().enabled=false;F.playerController.movementReference=null;
        // Isolate locomotion from encounters; these fixtures never persist to the scene.
        foreach(var e in encounters)e.GetComponent<Collider>().enabled=false;
        yield return Move(Vector2.up,.8f);Check(F.playerController.transform.position.z>-5.7f,"Rigidbody crosses entry bridge");
        Teleport(new Vector3(-3,0,-5.7f));yield return Move(Vector2.down,.8f);Check(F.playerController.transform.position.z>-6.45f,"stream bank blocks walking into water");
        Teleport(new Vector3(0,0,-2.4f));yield return Move(Vector2.up,.8f);Check(F.playerController.transform.position.z<-1.15f,"central pine furniture blocks walking through");
        Teleport(new Vector3(-5.2f,0,2.1f));yield return Move(Vector2.up,.55f);Check(F.playerController.transform.position.z>4,"pavilion staircase is traversable");
        Check(F.playerController.visualRoot.localPosition.y>.6f,"player visual rises onto pavilion floor");
        Teleport(new Vector3(0,0,-4.4f));F.cameraFollow.ResetVision();ScreenCapture.CaptureScreenshot(Output+"/playmode_portrait.png");yield return null;
        foreach(var e in encounters)e.GetComponent<Collider>().enabled=true;
        Teleport(new Vector3(-5.55f,0,-3.5f));yield return Move(Vector2.up,.18f);yield return Lesson("药草");yield return null;
        Check(herb.consumed&&props.herbRenderers.All(r=>!r.enabled),"herb consumed and authored plant hidden");
        Teleport(new Vector3(-5.2f,0,2.6f));yield return Move(Vector2.up,.25f);yield return Lesson("宝箱");yield return null;
        Check(chest.consumed&&props.chestRenderers.All(r=>!r.enabled),"chest consumed and authored chest hidden");
        yield return Wait(()=>F.IsTutorialLessonActive,"martial lesson");yield return Lesson("选择武学");
        Check(F.CurrentPhase==GamePhase.LevelUpPaused&&F.currentChoices.Count==3,"first chest guarantees martial choice");F.ChooseMartialArt(0);
        enemy.enemyStats.maxHealth=enemy.enemyStats.currentHealth=10000;enemy.enemyStats.attack=.1f;
        Teleport(new Vector3(3.2f,0,-2.15f));yield return Move(Vector2.right,.18f);yield return Lesson("自动战斗");
        Check(F.CurrentPhase==GamePhase.NormalBattleRunning,"enemy touch confirms into auto battle");
        float t=F.mainTimeRemaining;yield return new WaitForSeconds(.4f);Check(F.mainTimeRemaining<t,"normal battle consumes main timer");
        F.battleManager.currentEnemy.currentHealth=0;yield return Wait(()=>F.CurrentPhase==GamePhase.MainMapRunning||F.IsTutorialLessonActive||F.CurrentPhase==GamePhase.LevelUpPaused,"normal win");
        if(F.CurrentPhase==GamePhase.LevelUpPaused)F.ChooseMartialArt(0);
        Teleport(new Vector3(5.65f,0,2.65f));yield return Move(Vector2.up,.2f);yield return Lesson("隐藏洞穴");
        Check(F.CurrentPhase==GamePhase.CaveRunning,"authored cave entrance enters cave room");t=F.mainTimeRemaining;
        yield return new WaitForSeconds(.4f);Check(F.mainTimeRemaining==t,"cave pauses main timer");
        F.BeginCaveBattle(new WuxiaRoguelite.Runtime.CombatantStats{displayName="Probe",maxHealth=10000,currentHealth=10000,attack=.1f,attackSpeed=.5f},0,0,null);
        yield return new WaitForSeconds(.4f);Check(F.mainTimeRemaining==t,"cave battle pauses main timer");
        F.battleManager.CancelBattle();F.ExitHiddenCave(false);yield return null;
        F.mainTimeRemaining=.1f;yield return Wait(()=>F.IsTutorialLessonActive,"timer expiry boss lesson");yield return Lesson("新手守关");
        Check(F.CurrentPhase==GamePhase.BossBattle&&F.playerStats.runtimeStats.currentHealth==F.playerStats.runtimeStats.maxHealth,"timer expiry starts healed tutorial guard");
        yield return new WaitForSeconds(.4f);Check(F.bossBattleTime>0&&F.mainTimeRemaining==0,"guard uses independent boss timer");
        F.battleManager.currentEnemy.currentHealth=0;yield return Wait(()=>F.CurrentPhase==GamePhase.Result,"guard result");
        Check(F.IsTutorialCompletionSummary&&LevelSequence.TutorialCompleted,"guard victory completes tutorial and unlocks level two");
        F.StartRun();yield return null;
        Check(FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==4,"replay cannot reactivate legacy enemies");
        Check(!herb.consumed&&!chest.consumed&&props.chestRenderers.All(r=>r.enabled)&&props.herbRenderers.All(r=>r.enabled),"replay restores authored consumables");
    }
}
#endif
