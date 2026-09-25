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

/// <summary>Opt-in startup integration check; never saves scenes or changes unlock preferences.</summary>
public sealed class StudioSplashPlayModeProbe : MonoBehaviour
{
    private const string Key="37MiniGame.StudioSplash", Output="docs/validation/studio_splash";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private readonly List<string> checks=new(),errors=new();
    private bool restored,background,audioPause; private float timeScale;
    private EditorWindow gameView; private object sizeGroup; private int previousSize,addedSizes;
    private GameFlowController F=>GameFlowController.Instance;
    [Serializable] private class Report {public bool success;public string error;public string[] checks,runtimeErrors;public string scope="Controlled Unity Editor Play Mode in both orientations; not natural-route balance or device acceptance.";}
    [MenuItem("37 MiniGame/Validate Studio Splash Play Mode")]
    public static void Queue()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Key+".Background",Application.runInBackground);
        SessionState.SetFloat(Key+".TimeScale",Time.timeScale);
        SessionState.SetBool(Key+".AudioPause",AudioListener.pause);
        Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod]private static void Install(){EditorApplication.playModeStateChanged-=Boot;EditorApplication.playModeStateChanged+=Boot;}
    private static void Boot(PlayModeStateChange state)
    {if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Studio splash probe").AddComponent<StudioSplashPlayModeProbe>();}}
    private void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        background=SessionState.GetBool(Key+".Background",false);timeScale=SessionState.GetFloat(Key+".TimeScale",1);
        audioPause=SessionState.GetBool(Key+".AudioPause",false);
        Application.logMessageReceived+=OnLog;Directory.CreateDirectory(Output);
        var stack=new Stack<IEnumerator>();stack.Push(Suite());string error=null;
        while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e.ToString();}
            if(error!=null)break;if(!moved){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
        if(error==null&&errors.Count>0)error="Runtime console errors.";
        File.WriteAllText(Output+"/playmode_report.json",JsonUtility.ToJson(new Report{success=error==null,error=error,checks=checks.ToArray(),runtimeErrors=errors.ToArray()},true));
        Restore();Debug.Log("STUDIO_SPLASH_"+(error==null?"PASS":"FAIL: "+error));EditorApplication.isPlaying=false;
    }
    private void Restore()
    {
        if(restored)return;restored=true;Application.logMessageReceived-=OnLog;Application.runInBackground=background;Time.timeScale=timeScale;AudioListener.pause=audioPause;
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
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(kind,1),width,height,"Splash probe temporary"});
        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{size});addedSizes++;
        int total=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null);type.GetProperty("selectedSizeIndex",Flags).SetValue(gameView,total-1);gameView.Repaint();
    }
    private IEnumerator Capture(string file){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"/"+file+".png");yield return null;}
    private static void SplashStatic(string method)
    {
        typeof(StudioSplashScreen).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
    private IEnumerator WaitForSplashEnd()
    {
        float start = Time.realtimeSinceStartup;
        while (StudioSplashScreen.IsBlocking)
        {
            if (Time.realtimeSinceStartup - start > 8) throw new Exception("Splash timeout");
            yield return null;
        }
    }
    private IEnumerator Suite()
    {
        Resize(960, 540); yield return null;
        Check(StudioSplashScreen.IsBlocking && StudioSplashScreen.HasPresentedThisSession, "Splash automatically starts once at app startup");
        Check(Resources.Load<Texture2D>(StudioSplashScreen.LogoResource) != null, "Packaged original logo loads from Resources");
        Check(Time.timeScale == 0 && AudioListener.pause, "Startup holds gameplay and existing audio");
        float main = F.mainTimeRemaining, boss = F.bossBattleTime;
        var splash = FindFirstObjectByType<StudioSplashScreen>();
        while (splash.Elapsed < .8f) yield return null;
        yield return Capture("splash_landscape");
        Resize(540, 960); yield return null;
        Check(StudioSplashScreen.IsBlocking, "Orientation change preserves active splash");
        yield return Capture("splash_portrait");
        yield return WaitForSplashEnd();
        Check(F.mainTimeRemaining == main && F.bossBattleTime == boss, "Splash consumes neither gameplay timer");
        Check(Time.timeScale == 1 && !AudioListener.pause, "Automatic completion restores time scale and audio");
        Check(!F.IsLevelSelectionOpen && !PrototypeHUDController.IsSettingsOpen, "Startup leaves the main menu inactive until presentation completes");
        yield return Capture("main_menu_after_splash");
        SplashStatic("PresentAtStartup");
        Check(!StudioSplashScreen.IsBlocking, "Duplicate startup request in the same session is ignored");
        F.ReturnToMainMenu(); yield return null;
        Check(!StudioSplashScreen.IsBlocking, "Returning home does not replay the splash");
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(LevelSequence.LevelThreeSceneName);
        yield return null;
        Check(!StudioSplashScreen.IsBlocking, "Changing levels does not replay the splash");
        // Simulate a fresh app session in the test only, to check skip and restoration of nondefault globals.
        SplashStatic("ResetSession");
        Time.timeScale = .75f; AudioListener.pause = true;
        SplashStatic("PresentAtStartup");
        splash = FindFirstObjectByType<StudioSplashScreen>();
        while (splash.Elapsed < .65f) yield return null;
        float started = Time.realtimeSinceStartup;
        splash.Skip();
        yield return WaitForSplashEnd();
        Check(Time.realtimeSinceStartup - started < 1.2f, "Skip closes with a short fade without waiting for the full duration");
        Check(Time.timeScale == .75f && AudioListener.pause, "Skip restores the previous time and audio state exactly");
        Check(!F.IsLevelSelectionOpen && !PrototypeHUDController.IsSettingsOpen, "Skip does not open underlying menu or settings");
        Time.timeScale = 1; AudioListener.pause = false;
    }
}
#endif
