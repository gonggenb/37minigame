#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

/// <summary>Controlled real-physics integration check; does not claim natural-player balance.</summary>
public sealed class PingchuanTownPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.PingchuanProbe", Output="docs/validation/pingchuan_town";
    private readonly List<string> checks=new();
    private bool background;private int unlock;private bool restored;
    private GameFlowController Flow=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks;public string scope="Controlled Unity Play Mode: Rigidbody crossings and timing integration; not device or human balance approval.";}
    [MenuItem("37 MiniGame/Validate Pingchuan Town Play Mode")]
    public static void Queue(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    [InitializeOnLoadMethod]private static void Install(){EditorApplication.playModeStateChanged-=Boot;EditorApplication.playModeStateChanged+=Boot;}
    private static void Boot(PlayModeStateChange s){if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);new GameObject("Pingchuan integration probe").AddComponent<PingchuanTownPlayModeProbe>();}
    private IEnumerator Start()
    {
        background=Application.runInBackground;Application.runInBackground=true;unlock=PlayerPrefs.GetInt("WuxiaRoguelite.LevelTwoCompleted.v1",-1);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0)
        {
            object next=null;bool moved=false;
            try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}
            if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/exploration_playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray()},true));
        Restore();Debug.Log("PINGCHUAN_PLAYMODE_"+(error==null?"PASS":"FAIL "+error));EditorApplication.isPlaying=false;
    }
    private void Restore(){if(restored)return;restored=true;Application.runInBackground=background;Time.timeScale=1;Input(Vector2.zero);if(unlock<0)PlayerPrefs.DeleteKey("WuxiaRoguelite.LevelTwoCompleted.v1");else PlayerPrefs.SetInt("WuxiaRoguelite.LevelTwoCompleted.v1",unlock);PlayerPrefs.Save();}
    private void OnDestroy()=>Restore();
    private void Check(bool ok,string note){if(!ok)throw new Exception(note);checks.Add(note);}
    private static void Invoke(object o,string m,params object[] args)=>o.GetType().GetMethod(m,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    private static void Input(Vector2 p)=>typeof(MobileInputController).GetProperty("MoveInput").GetSetMethod(true).Invoke(null,new object[]{p});
    private void Teleport(Vector3 p){p.y=0;Flow.playerController.transform.position=p;var rb=Flow.playerController.GetComponent<Rigidbody>();rb.position=p;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();}
    private IEnumerator Move(Vector3 direction,float distance)
    {
        int count=Mathf.CeilToInt(distance/Flow.playerStats.CurrentMoveSpeed/Time.fixedDeltaTime);
        for(int i=0;i<count;i++){Input(new Vector2(direction.x,direction.z));yield return new WaitForFixedUpdate();}
        Input(Vector2.zero);yield return new WaitForFixedUpdate();
    }
    private IEnumerator Suite()
    {
        yield return null;
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==LevelSequence.LevelTwoSceneName,"MainPrototype is still level two");
        Invoke(Flow,"BeginLevelTwoAfterTransition");
        Check(Flow.CurrentPhase==GamePhase.LevelUpPaused&&Flow.mainTimeRemaining==60,"opening martial choice pauses the 60-second clock");
        Flow.ChooseMartialArt(0);FindFirstObjectByType<MobileInputController>().enabled=false;Flow.playerController.movementReference=null;
        var es=FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None);foreach(var e in es)e.GetComponent<Collider>().enabled=false;
        yield return CheckExplorationRewards(es);
        foreach(var b in PingchuanTownLayout.Data.bridges)
        {
            Flow.mainTimeRemaining=59;Teleport(b.Position-b.Forward*(b.length*.5f+.5f));
            yield return Move(b.Forward,b.length+1.2f);
            float progress=Vector3.Dot(Flow.playerController.transform.position-b.Position,b.Forward);
            Check(progress>b.length*.5f+.2f,"crossed bridge "+b.name+" using Rigidbody (progress="+progress.ToString("F2")+")");
        }
        var river=PingchuanTownLayout.Data.rivers[0];int ri=40;Vector2 a=river.points[ri],bb=river.points[ri+1],mid=(a+bb)*.5f;
        Vector3 tangent=new Vector3(bb.x-a.x,0,bb.y-a.y).normalized,across=new Vector3(tangent.z,0,-tangent.x),center=new Vector3(mid.x,0,mid.y);
        Teleport(center-across*(river.width*.5f+.8f));yield return Move(across,river.width+2);
        Check(Vector3.Dot(Flow.playerController.transform.position-center,across)<0,"river blocks movement outside the bridge gaps");
        Teleport(PingchuanTownLayout.Point(-25,0));Flow.cameraFollow.ResetVision();Flow.mainTimeRemaining=59;
        yield return new WaitForSeconds(.15f);ScreenCapture.CaptureScreenshot(Output+"/exploration_playmode_market.png");yield return null;
        foreach(var e in es)e.GetComponent<Collider>().enabled=true;
        var enemy=es.First(e=>e.encounterType==EncounterType.NormalEnemy&&e.name=="山贼喽啰 PC 1");
        enemy.enemyStats.maxHealth=enemy.enemyStats.currentHealth=100000;enemy.enemyStats.attack=.1f;
        Teleport(enemy.transform.position-Vector3.forward*2);yield return Move(Vector3.forward,1.5f);
        Check(Flow.CurrentPhase==GamePhase.NormalBattleRunning,"walking into an authored enemy starts real normal combat");
        float time=Flow.mainTimeRemaining;yield return new WaitForSeconds(.3f);Check(Flow.mainTimeRemaining<time,"normal battle consumes main timer");
        Flow.battleManager.CancelBattle();Invoke(Flow,"SetPhase",GamePhase.MainMapRunning);
        var cave=es.First(e=>e.encounterType==EncounterType.HiddenCave);Teleport(cave.transform.position-Vector3.forward*2);yield return Move(Vector3.forward,1.5f);
        Check(Flow.CurrentPhase==GamePhase.CaveRunning,"walking into physical cave mouth enters cave");
        var fixture=new CombatantStats{displayName=GameTextCatalog.FinalBossName,maxHealth=100000,currentHealth=100000,attack=.1f,attackSpeed=.5f};
        Flow.BeginCaveBattle(fixture.Clone(),0,0,null);time=Flow.mainTimeRemaining;yield return new WaitForSeconds(.3f);Check(Flow.mainTimeRemaining==time,"cave combat freezes main timer");
        Flow.battleManager.CancelBattle();Flow.caveRoom.ResetRoom();Teleport(PingchuanTownLayout.Spawn);Invoke(Flow,"SetPhase",GamePhase.MainMapRunning);
        Flow.mainTimeRemaining=30.1f;float hp=Flow.playerStats.runtimeStats.currentHealth;yield return new WaitForSeconds(.25f);
        Check(Flow.CurrentPhase==GamePhase.MidBossBattle,"elapsed 30 seconds starts the level-two midboss");
        Check(Flow.playerStats.runtimeStats.currentHealth<=hp,"midboss transition does not heal to full");
        time=Flow.mainTimeRemaining;yield return new WaitForSeconds(.3f);Check(Flow.mainTimeRemaining==time&&Flow.midBossBattleTime>0,"midboss has independent time and freezes exploration");
        Flow.battleManager.currentEnemy.currentHealth=0;float until=Time.time+5;
        while(Flow.CurrentPhase==GamePhase.MidBossBattle&&Time.time<until)yield return null;
        while(Flow.CurrentPhase==GamePhase.LevelUpPaused){Flow.ChooseMartialArt(0);yield return null;}
        Check(Flow.midBossDefeated&&Flow.CurrentPhase==GamePhase.MainMapRunning,"midboss victory resumes exploration");
        Flow.bossIntroDuration=0;Flow.bossStats=fixture.Clone();Flow.mainTimeRemaining=.1f;yield return new WaitForSeconds(.4f);
        time=Flow.mainTimeRemaining;yield return new WaitForSeconds(.3f);
        Check(Flow.CurrentPhase==GamePhase.BossBattle&&Flow.bossBattleTime>0&&Flow.mainTimeRemaining==time,"60-second expiry enters independently timed final boss");
        Flow.battleManager.currentEnemy.currentHealth=0;until=Time.time+5;
        while(Flow.CurrentPhase!=GamePhase.Result&&Time.time<until)yield return null;
        Check(Flow.bossDefeated&&Flow.CanContinueToNextLevel,"level two victory still unlocks continuation to level three");
        Flow.ReturnToMainMenu();Invoke(Flow,"BeginLevelTwoAfterTransition");Flow.ChooseMartialArt(0);
        Check(Vector3.Distance(Flow.playerController.transform.position,PingchuanTownLayout.Spawn)<.1f&&es.All(e=>!e.consumed),"restart restores spawn and every encounter");
        foreach(var e in es)e.GetComponent<Collider>().enabled=false;
        Teleport(PingchuanTownLayout.Point(35,-10));Flow.cameraFollow.ResetVision();yield return new WaitForSeconds(.15f);ScreenCapture.CaptureScreenshot(Output+"/exploration_playmode_plain.png");yield return null;
    }

    private IEnumerator CheckExplorationRewards(EncounterTrigger[] es)
    {
        int Ranks()=>Flow.playerStats.learnedMartialArts.Sum(Flow.playerStats.GetMartialArtRank);
        var chests=es.Where(e=>e.encounterType==EncounterType.Treasure).ToArray();
        Check(chests.Any(e=>e.ExplorationRewardTier==0)&&chests.Any(e=>e.ExplorationRewardTier==1)&&chests.Any(e=>e.ExplorationRewardTier==2),"authored chests cover near, mid and far reward tiers");
        foreach(int tier in new[]{0,1,2})
        {
            var chest=chests.First(e=>e.ExplorationRewardTier==tier);
            int copper=Flow.playerStats.copper, ranks=Ranks(), xp=Flow.playerStats.cultivation;
            int level=Flow.playerStats.level, requirement=Flow.playerStats.NextLevelRequirement;
            float time=Flow.mainTimeRemaining;
            Flow.HandleEncounter(chest);
            int expectedCopper=Mathf.RoundToInt(chest.copperReward*(tier==0?1f:tier==1?1.5f:2f));
            int expectedXp=Mathf.RoundToInt(chest.cultivationReward*(tier==0?1f:tier==1?1.5f:2f));
            Check(Flow.playerStats.copper-copper==expectedCopper&&
                Flow.playerStats.cultivation-xp+(Flow.playerStats.level>level?requirement:0)==expectedXp,
                "tier "+tier+" chest grants scaled copper and cultivation through production flow");
            Check(Ranks()-ranks==(tier==2?1:0),"tier "+tier+" chest upgrades learned martial art only at far distance");
            if(Flow.CurrentPhase==GamePhase.LevelUpPaused)
            {
                yield return new WaitForSeconds(.1f);
                Check(Flow.mainTimeRemaining==time,"treasure level-up choices pause exploration time");
            }
            while(Flow.CurrentPhase==GamePhase.LevelUpPaused)Flow.ChooseMartialArt(0);
            copper=Flow.playerStats.copper;ranks=Ranks();
            Flow.HandleEncounter(chest);
            Check(Flow.playerStats.copper==copper&&Ranks()==ranks,"consumed tier "+tier+" chest cannot grant rewards twice");
        }
        var far=chests.First(e=>e.GrantsMartialArtUpgrade);
        float bakedDistance=far.rewardRouteDistance;int bakedXp=far.GrantedCultivationReward;
        Flow.ReturnToMainMenu();Invoke(Flow,"BeginLevelTwoAfterTransition");Flow.ChooseMartialArt(0);
        Check(!far.consumed&&far.rewardRouteDistance==bakedDistance&&far.GrantedCultivationReward==bakedXp,
            "new run restores far treasure without stacking its baked multiplier");
        foreach(string art in Flow.playerStats.learnedMartialArts.ToArray())
            for(int i=0;i<3;i++)Flow.playerStats.ApplyMartialArt(art);
        int beforeCopper=Flow.playerStats.copper,beforeRanks=Ranks();
        Flow.HandleEncounter(far);
        Check(Flow.playerStats.copper-beforeCopper==far.GrantedCopperReward+ExplorationRewardTuning.MaxRankFallbackCopper&&Ranks()==beforeRanks,
            "max-rank far treasure converts unavailable upgrade to 20 copper");
        while(Flow.CurrentPhase==GamePhase.LevelUpPaused)Flow.ChooseMartialArt(0);
        var enemy=es.First(e=>e.encounterType==EncounterType.NormalEnemy&&e.ExplorationRewardTier==2);
        Flow.HandleEncounter(enemy);
        Check(Flow.pendingCultivationReward==enemy.GrantedCultivationReward&&Flow.pendingCopperReward==enemy.GrantedCopperReward,
            "far normal combat receives scaled pending rewards");
        Flow.battleManager.CancelBattle();
        Flow.ReturnToMainMenu();Invoke(Flow,"BeginLevelTwoAfterTransition");Flow.ChooseMartialArt(0);
        Check(es.Where(e=>e.encounterType==EncounterType.HiddenCave||e.encounterType==EncounterType.Herb).All(e=>e.rewardRouteDistance<0),
            "caves and herbs retain original reward rules");
    }
}
#endif
