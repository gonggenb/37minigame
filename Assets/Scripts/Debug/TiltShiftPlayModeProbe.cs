#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.CameraTools;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.UI;

/// <summary>Opt-in actual-render checks; restores preferences and Game View without saving scenes.</summary>
public sealed class TiltShiftPlayModeProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.TiltShiftProbe";
    private const string Output = "docs/validation/tilt_shift";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<string> errors = new List<string>();
    private bool restored, background;
    private int preference;
    private float previousTimeScale;
    private EditorWindow gameView;
    private object sizeGroup;
    private int previousSize, addedSizes;
    private GameFlowController Flow => GameFlowController.Instance;
    [Serializable] private class Report
    {
        public bool success;
        public string error;
        public string[] checks, runtimeErrors;
        public string scope = "Unity Editor rendered images and controlled play; not mobile GPU or WebGL device acceptance.";
    }

    [MenuItem("37 MiniGame/Validate Tilt Shift Play Mode")]
    public static void Queue()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainPrototype")
            throw new InvalidOperationException("Open MainPrototype first.");
        SessionState.SetBool(Key + ".Background", Application.runInBackground);
        SessionState.SetFloat(Key + ".TimeScale", Time.timeScale);
        SessionState.SetBool(Key, true);
        Application.runInBackground = true;
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Install()
    {
        EditorApplication.playModeStateChanged -= Boot;
        EditorApplication.playModeStateChanged += Boot;
    }

    private static void Boot(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false);
        new GameObject("Tilt shift validation").AddComponent<TiltShiftPlayModeProbe>();
    }

    private void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
    }

    private IEnumerator Start()
    {
        background = SessionState.GetBool(Key + ".Background", false);
        previousTimeScale = SessionState.GetFloat(Key + ".TimeScale", 1f);
        preference = PlayerPrefs.HasKey(TiltShiftEffect.PreferenceKey) ? PlayerPrefs.GetInt(TiltShiftEffect.PreferenceKey) : -1;
        Application.logMessageReceived += Log;
        Directory.CreateDirectory(Output);
        var stack = new Stack<IEnumerator>();
        stack.Push(Suite());
        string failure = null;
        while (stack.Count > 0)
        {
            object next = null;
            bool moved = false;
            try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
            catch (Exception e) { failure = e.ToString(); }
            if (failure != null) break;
            if (!moved) { stack.Pop(); continue; }
            if (next is IEnumerator nested) stack.Push(nested); else yield return next;
        }
        if (failure == null && errors.Count > 0) failure = "Runtime console errors.";
        File.WriteAllText(Output + "/playmode_report.json", JsonUtility.ToJson(new Report
        { success = failure == null, error = failure, checks = checks.ToArray(), runtimeErrors = errors.ToArray() }, true));
        Restore();
        Debug.Log("TILT_SHIFT_" + (failure == null ? "PASS" : "FAIL: " + failure));
        EditorApplication.isPlaying = false;
    }

    private void Check(bool passed, string note)
    {
        if (!passed) throw new InvalidOperationException(note);
        checks.Add(note);
    }

    private static object Invoke(object owner, string method, params object[] args) =>
        owner.GetType().GetMethod(method, Flags).Invoke(owner, args);

    private void Resize(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        var type = assembly.GetType("UnityEditor.GameView");
        if (gameView == null)
        {
            gameView = EditorWindow.GetWindow(type);
            previousSize = (int)type.GetProperty("selectedSizeIndex", Flags).GetValue(gameView);
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(assembly.GetType("UnityEditor.GameViewSizes"));
            var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            sizeGroup = sizes.GetType().GetProperty("currentGroup", Flags).GetValue(sizes);
        }
        var kind = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),
            new object[] { Enum.ToObject(kind, 1), width, height, "Tilt shift temporary" });
        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { size });
        addedSizes++;
        int total = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
        type.GetProperty("selectedSizeIndex", Flags).SetValue(gameView, total - 1);
        gameView.Repaint();
    }

    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Output + "/" + name + ".png");
        yield return null;
    }

    private void CompareRenderedImages(Camera camera, TiltShiftEffect effect, int width, int height, int samples)
    {
        var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default, samples);
        var before = camera.targetTexture;
        var active = RenderTexture.active;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            effect.enabled = false;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            Color32[] sharp = pixels.GetPixels32();
            effect.enabled = true;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            Color32[] soft = pixels.GetPixels32();
            double center = 0, edge = 0;
            int centerCount = 0, edgeCount = 0;
            float focusY = camera.WorldToViewportPoint(Flow.cameraFollow.target.position + Vector3.up * Flow.cameraFollow.lookAtHeight).y;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                double difference = (Math.Abs(sharp[i].r - soft[i].r) + Math.Abs(sharp[i].g - soft[i].g) + Math.Abs(sharp[i].b - soft[i].b)) / 3.0;
                float distance = Mathf.Abs((y + .5f) / height - focusY);
                if (distance < effect.focusHalfHeight - .03f) { center += difference; centerCount++; }
                if (distance > effect.focusHalfHeight + .1f) { edge += difference; edgeCount++; }
            }
            center /= Math.Max(1, centerCount); edge /= Math.Max(1, edgeCount);
            Check(center < .6, width + "x" + height + " MSAA " + samples + ": sharp band mean difference " + center.ToString("F4") + "/255");
            Check(edge > .1, width + "x" + height + " MSAA " + samples + ": scenery edges actually blurred, difference " + edge.ToString("F4") + "/255");
        }
        finally
        {
            camera.targetTexture = before; RenderTexture.active = active;
            Destroy(pixels); RenderTexture.ReleaseTemporary(target); effect.RefreshEnabledState();
        }
    }

    private IEnumerator Suite()
    {
        yield return null;
        float splashDeadline = Time.realtimeSinceStartup + 15f;
        while (StudioSplashScreen.IsBlocking && Time.realtimeSinceStartup < splashDeadline) yield return null;
        Check(!StudioSplashScreen.IsBlocking, "Startup splash completed before validation");
        Time.timeScale = 1;
        Vector3 authoredPosition = Flow.playerController.transform.position;
        Invoke(Flow, "BeginLevelTwoAfterTransition"); Flow.ChooseMartialArt(0);
        Flow.playerController.enabled = false;
        // Keep the camera sample on the authored open road instead of a restart fixture's origin.
        Flow.playerController.transform.position = authoredPosition;
        var body = Flow.playerController.GetComponent<Rigidbody>();
        if (body != null) { body.position = authoredPosition; body.linearVelocity = Vector3.zero; }
        Physics.SyncTransforms();
        var encounters = FindObjectsByType<EncounterTrigger>();
        foreach (var encounter in encounters) encounter.GetComponent<Collider>().enabled = false;
        TiltShiftEffect.SetUserEnabled(true);
        var camera = Flow.cameraFollow.GetComponent<Camera>();
        var effect = camera.GetComponent<TiltShiftEffect>();
        yield return null;
        Check(effect != null && effect.enabled, "CameraFollow automatically attaches and enables effect in exploration");
        Check(Resources.Load<Shader>("Camera/TiltShift").isSupported, "Packaged shader supported by current graphics device");
        var sprites = Flow.playerController.GetComponentsInChildren<SpriteRenderer>();
        Check(sprites.Length > 0 && sprites.All(r => r.sharedMaterial.renderQueue > 2500), "Player sprites render after opaque scenery effect");
        Time.timeScale = 0;
        foreach (bool portrait in new[] { false, true })
        {
            int width = portrait ? 540 : 960, height = portrait ? 960 : 540;
            string orientation = portrait ? "portrait" : "landscape";
            Resize(width, height);
            yield return null; yield return null;
            Flow.cameraFollow.ResetVision(); yield return null;
            CompareRenderedImages(camera, effect, width, height, 1);
            CompareRenderedImages(camera, effect, width, height, 4);
            TiltShiftEffect.SetUserEnabled(false); yield return null;
            Check(!effect.enabled, orientation + ": preference disables image effect entirely");
            yield return Capture(orientation + "_off");
            TiltShiftEffect.SetUserEnabled(true); yield return null;
            yield return Capture(orientation + "_on");
            var hud = FindAnyObjectByType<PrototypeHUDController>();
            Invoke(hud, "SetSettingsOpen", true);
            yield return Capture(orientation + "_settings");
            Invoke(hud, "SetSettingsOpen", false);
        }
        Time.timeScale = 1;
        var enemy = encounters.First(e => e.encounterType == EncounterType.NormalEnemy);
        enemy.enemyStats.maxHealth = enemy.enemyStats.currentHealth = 100000;
        enemy.enemyStats.attack = .1f;
        Flow.mainTimeRemaining = 59;
        Flow.HandleEncounter(enemy);
        float remaining = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(.2f);
        Check(!effect.enabled && Flow.mainTimeRemaining < remaining, "Ordinary battle disables effect and keeps consuming main time");
        Flow.battleManager.CancelBattle(); Invoke(Flow, "SetPhase", GamePhase.MainMapRunning);
        Flow.HandleEncounter(encounters.First(e => e.encounterType == EncounterType.HiddenCave));
        Flow.BeginCaveBattle(enemy.CreateEnemyStats(), 0, 0, null);
        remaining = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(.2f);
        Check(!effect.enabled && Flow.mainTimeRemaining == remaining && Flow.battleManager.IsBattleActive &&
            Flow.battleManager.BattleElapsed > 0, "Active cave battle disables effect and pauses main time");
        Flow.battleManager.CancelBattle();
        Invoke(Flow.caveRoom, "LeaveCave"); yield return null;
        Check(effect.enabled, "Returning from cave restores effect");
        Flow.bossIntroDuration = 0; Invoke(Flow, "BeginBossBattle");
        remaining = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(.2f);
        Check(!effect.enabled && Flow.mainTimeRemaining == remaining && Flow.bossBattleTime > 0, "Boss disables effect and uses independent time");
    }

    private void Restore()
    {
        if (restored) return;
        restored = true;
        Application.logMessageReceived -= Log;
        Application.runInBackground = background; Time.timeScale = previousTimeScale;
        TiltShiftEffect.SetUserEnabled(preference != 0);
        if (preference < 0) PlayerPrefs.DeleteKey(TiltShiftEffect.PreferenceKey);
        PlayerPrefs.Save();
        if (gameView == null) return;
        gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(gameView, previousSize);
        for (int i = 0; i < addedSizes; i++)
        {
            int total = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
            sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup, new object[] { total - 1 });
        }
    }

    private void OnDestroy() => Restore();
}
#endif
