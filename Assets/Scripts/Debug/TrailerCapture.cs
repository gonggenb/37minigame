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

/// <summary>Opt-in promotional capture. Only changes Play Mode objects; never saves scenes.</summary>
[DefaultExecutionOrder(-100)]
public sealed class TrailerCapture : MonoBehaviour
{
    const string Key = "37MiniGame.TrailerCapture";
    const string Output = "ArtSource/Promotional/Trailer_v01/capture";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    GameFlowController F => GameFlowController.Instance;
    EditorWindow view; object sizes; int oldSize, oldRate; bool oldBackground, restored;
    Vector2 input; readonly List<string> errors = new List<string>();
    [Serializable] class Report { public bool success; public string error; public string[] runtimeErrors; public int fps=30; public int width=720,height=1280; public string scope="Unity Editor Play Mode, controlled inputs and staged legal martial-art build; not an uninterrupted natural run. No damage or enemy-stat overrides."; }
    [MenuItem("37 MiniGame/Promotional/Capture Vertical Trailer")]
    public static void Queue()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="MainPrototype" || scene.isDirty) throw new Exception("Open the saved MainPrototype scene first.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);
        SessionState.SetInt(Key+".Rate",Time.captureFramerate);
        Application.runInBackground=true; SessionState.SetBool(Key,true); EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod] static void Install() { EditorApplication.playModeStateChanged-=Boot; EditorApplication.playModeStateChanged+=Boot; }
    static void Boot(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
        { SessionState.SetBool(Key,false); new GameObject("Promotional capture (temporary)").AddComponent<TrailerCapture>(); }
    }
    void Update() { typeof(MobileInputController).GetProperty("MoveInput").GetSetMethod(true).Invoke(null,new object[]{input}); }
    static object Invoke(object target,string method,params object[] args) => target.GetType().GetMethod(method,Flags).Invoke(target,args);
    void Log(string message,string stack,LogType type) { if(type==LogType.Error||type==LogType.Exception) errors.Add(message); }
    IEnumerator Start()
    {
        oldBackground=SessionState.GetBool(Key+".Background",false); oldRate=SessionState.GetInt(Key+".Rate",0);
        Application.logMessageReceived+=Log; Directory.CreateDirectory(Output);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0)
        {
            object next=null;bool moved=false;
            try { moved=stack.Peek().MoveNext(); if(moved)next=stack.Peek().Current; } catch(Exception e){error=e.ToString();}
            if(error!=null)break; if(!moved){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        File.WriteAllText(Output+"/report.json",JsonUtility.ToJson(new Report{success=error==null&&errors.Count==0,error=error,runtimeErrors=errors.ToArray()},true));
        Restore();EditorApplication.isPlaying=false;
    }
    void Resize()
    {
        var asm=typeof(Editor).Assembly;var vt=asm.GetType("UnityEditor.GameView");view=EditorWindow.GetWindow(vt);
        oldSize=(int)vt.GetProperty("selectedSizeIndex",Flags).GetValue(view);
        var st=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(st);
        sizes=st.GetProperty("currentGroup",Flags).GetValue(singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null));
        var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"),1),720,1280,"Trailer temporary"});
        sizes.GetType().GetMethod("AddCustomSize").Invoke(sizes,new[]{size});
        int count=(int)sizes.GetType().GetMethod("GetTotalCount").Invoke(sizes,null);
        vt.GetProperty("selectedSizeIndex",Flags).SetValue(view,count-1);view.Focus();view.Repaint();
    }
    IEnumerator Shot(string name,int count)
    {
        string dir=Output+"/"+name;Directory.CreateDirectory(dir);
        for(int i=0;i<count;i++)
        {
            yield return new WaitForEndOfFrame();
            var tex=ScreenCapture.CaptureScreenshotAsTexture();
            if(tex.width!=720||tex.height!=1280)throw new Exception("Unexpected capture size "+tex.width+"x"+tex.height);
            File.WriteAllBytes(dir+"/"+i.ToString("D4")+".png",tex.EncodeToPNG());Destroy(tex);
        }
    }
    IEnumerator Suite()
    {
        Resize();yield return new WaitForSecondsRealtime(3.5f);Time.captureFramerate=30;
        Invoke(F,"BeginLevelTwoAfterTransition");F.ConfirmChallengeBriefing();yield return null;
        yield return Shot("choice",90);F.ChooseMartialArt(0);
        input=new Vector2(.59f,.81f);yield return Shot("explore",150);input=Vector2.zero;
        // Stage an attainable build using the same application path as rewards, without stat overrides.
        Invoke(F,"BeginLevelTwoAfterTransition");F.ConfirmChallengeBriefing();F.ChooseMartialArt(0);
        foreach(string art in new[]{"剑气诀","疾剑式","铁布衫","金钟罩"})
            F.playerStats.ApplyMartialArt(art);
        var normal=FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(x=>x.encounterType==EncounterType.EliteEnemy);
        F.HandleEncounter(normal);yield return Shot("combat",90);
        F.battleManager.CancelBattle();Invoke(F,"SetPhase",GamePhase.MainMapRunning);
        F.bossIntroDuration=0;Invoke(F,"BeginBossBattle");yield return Shot("boss",210);
    }
    void Restore()
    {
        if(restored)return;restored=true;input=Vector2.zero;
        typeof(MobileInputController).GetProperty("MoveInput").GetSetMethod(true).Invoke(null,new object[]{Vector2.zero});
        Application.logMessageReceived-=Log;Application.runInBackground=oldBackground;Time.captureFramerate=oldRate;
        if(view!=null && sizes!=null)
        {
            view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,oldSize);
            int count=(int)sizes.GetType().GetMethod("GetTotalCount").Invoke(sizes,null);
            sizes.GetType().GetMethod("RemoveCustomSize").Invoke(sizes,new object[]{count-1});
        }
    }
    void OnDestroy(){Restore();}
}
#endif
