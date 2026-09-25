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
public sealed class ForegroundOcclusionPlayModeProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.ForegroundOcclusionProbe";
    private const string Output = "docs/validation/foreground_occlusion";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<string> errors = new List<string>();
    private bool restored, background;
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
        public string capturedUtc = DateTime.UtcNow.ToString("O");
        public string scope = "Unity Editor rendered images and controlled play; not mobile GPU or WebGL device acceptance.";
    }

    [MenuItem("37 MiniGame/Validate Foreground Occlusion Play Mode")]
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
        new GameObject("Foreground occlusion validation").AddComponent<ForegroundOcclusionPlayModeProbe>();
    }

    private void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
    }

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        background = SessionState.GetBool(Key + ".Background", false);
        previousTimeScale = SessionState.GetFloat(Key + ".TimeScale", 1f);
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
        Debug.Log("FOREGROUND_OCCLUSION_" + (failure == null ? "PASS" : "FAIL: " + failure));
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
            new object[] { Enum.ToObject(kind, 1), width, height, "Occlusion temporary" });
        sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { size });
        addedSizes++;
        int total = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
        type.GetProperty("selectedSizeIndex", Flags).SetValue(gameView, total - 1);
        gameView.Repaint();
    }

    private Color32[] Render(Camera camera, string name, int width, int height)
    {
        var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            if (name != null) File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
            return image.GetPixels32();
        }
        finally
        {
            camera.targetTexture = previous; RenderTexture.active = active;
            Destroy(image); RenderTexture.ReleaseTemporary(target);
        }
    }

    private void Teleport(Vector3 position)
    {
        Flow.playerController.transform.position = position;
        var body = Flow.playerController.GetComponent<Rigidbody>();
        body.position = position; body.linearVelocity = Vector3.zero;
        var visual = Flow.playerController.visualRoot;
        float height = Flow.playerController.followBambooValleyHeight
            ? BambooValleyLayout.SurfaceHeight(position.x, position.z)
            : PingchuanTownLayout.SurfaceHeight(position.x, position.z);
        visual.localPosition = Vector3.up * height;
        Physics.SyncTransforms(); Flow.cameraFollow.ResetVision();
    }

    private IEnumerator CompareScene(string name, Vector3 position, bool portrait)
    {
        int width = portrait ? 540 : 960, height = portrait ? 960 : 540;
        Resize(width, height); yield return null; yield return null;
        Teleport(position); yield return null;
        var camera = Flow.cameraFollow.GetComponent<Camera>();
        var effect = camera.GetComponent<ForegroundOcclusion>();
        Check(effect != null, name + ": camera automatically attaches occlusion component");
        effect.enabled = false;
        var before = Render(camera, name + "_before", width, height);
        effect.enabled = true;
        yield return new WaitForSecondsRealtime(.3f);
        var after = Render(camera, name + "_after", width, height);
        int changed = 0;
        for (int i = 0; i < before.Length; i++)
            if (Math.Abs(before[i].r - after[i].r) + Math.Abs(before[i].g - after[i].g) + Math.Abs(before[i].b - after[i].b) > 12) changed++;
        Check(changed > 30, name + ": foreground pixels actually revealed: " + changed);
        Check(Shader.GetGlobalVector("_ForegroundAnchor").w == 0, name + ": shader globals cleared after camera render");
    }

    private IEnumerator CheckLayeredOccluders()
    {
        var camera = Flow.cameraFollow.GetComponent<Camera>();
        var effect = camera.GetComponent<ForegroundOcclusion>();
        int mask = camera.cullingMask;
        var clear = camera.clearFlags;
        Color backgroundColor = camera.backgroundColor;
        bool fog = RenderSettings.fog;
        var tilt = camera.GetComponent<TiltShiftEffect>();
        var fixtures = new List<GameObject>();
        try
        {
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.1f, .3f, .7f);
            RenderSettings.fog = false;
            tilt.enabled = false;
            var anchor = Flow.playerController.visualRoot.position + Vector3.up * effect.centreHeight;
            var direction = (camera.transform.position - anchor).normalized;
            var material = new Material(Shader.Find("Wuxia Roguelite/Bamboo Valley Vertex Surface"));
            material.SetColor("_Color", Color.red);
            material.SetFloat("_Emission", 1);
            for (int i = 0; i < 4; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Temporary layered occluder"; cube.layer = 30;
                cube.transform.position = anchor + direction * (3 + i);
                cube.transform.rotation = camera.transform.rotation;
                cube.transform.localScale = new Vector3(8, 8, .1f);
                cube.GetComponent<Renderer>().sharedMaterial = material;
                fixtures.Add(cube);
            }
            effect.enabled = false;
            var blocked = Render(camera, null, 960, 540);
            effect.enabled = true;
            yield return new WaitForSecondsRealtime(.3f);
            // CameraFollow re-enables tilt shift each update.
            tilt.enabled = false;
            var revealed = Render(camera, "layered_fixture", 960, 540);
            foreach (var fixture in fixtures) fixture.SetActive(false);
            var reference = Render(camera, null, 960, 540);
            Vector3 screen = camera.WorldToViewportPoint(anchor);
            int x = Mathf.Clamp((int)(screen.x * 960), 5, 954), y = Mathf.Clamp((int)(screen.y * 540), 5, 534);
            int removed = 0, revealedCount = 0;
            for (int dy = -4; dy <= 4; dy++) for (int dx = -4; dx <= 4; dx++)
            {
                int i = (y + dy) * 960 + x + dx;
                if (revealed[i].Equals(reference[i])) revealedCount++;
                if (!blocked[i].Equals(reference[i])) removed++;
            }
            Check(removed == 81 && revealedCount == 81, "Four overlapping solid layers fully clear the centre (81/81 pixels)");
            // Geometry behind the actor must remain solid.
            fixtures[0].SetActive(true);
            fixtures[0].transform.position = anchor - direction * 3;
            var behindOn = Render(camera, null, 960, 540);
            effect.enabled = false;
            var behindOff = Render(camera, null, 960, 540);
            Check(behindOn.SequenceEqual(behindOff), "Background geometry behind actor remains unchanged");
            effect.enabled = true;
            yield return new WaitForSecondsRealtime(.3f);
            tilt.enabled = false;
            fixtures[0].transform.position = Flow.playerController.visualRoot.position + direction * 3;
            var p = fixtures[0].transform.position;
            p.y = Flow.playerController.visualRoot.position.y - .05f;
            fixtures[0].transform.position = p;
            fixtures[0].transform.rotation = Quaternion.identity;
            fixtures[0].transform.localScale = new Vector3(20, .1f, 20);
            var groundOn = Render(camera, null, 960, 540);
            effect.enabled = false;
            var groundOff = Render(camera, null, 960, 540);
            Check(groundOn.SequenceEqual(groundOff), "Walkable ground at actor foot height remains unchanged");
            effect.enabled = true;
            Destroy(material);
        }
        finally
        {
            foreach (var fixture in fixtures) Destroy(fixture);
            camera.cullingMask = mask; camera.clearFlags = clear;
            camera.backgroundColor = backgroundColor; RenderSettings.fog = fog;
            tilt.RefreshEnabledState(); effect.enabled = true;
        }
    }

    private void CheckInactive(string context)
    {
        var effect = Flow.cameraFollow.GetComponent<ForegroundOcclusion>();
        Invoke(effect, "OnPreRender");
        Check(Shader.GetGlobalVector("_ForegroundAnchor").w == 0, context + ": no opening during camera render");
        Invoke(effect, "OnPostRender");
    }

    private IEnumerator Suite()
    {
        yield return null;
        float splashDeadline = Time.realtimeSinceStartup + 15f;
        while (StudioSplashScreen.IsBlocking && Time.realtimeSinceStartup < splashDeadline) yield return null;
        Check(!StudioSplashScreen.IsBlocking, "Startup splash completed before validation");
        Time.timeScale = 1;
        Invoke(Flow, "BeginLevelTwoAfterTransition"); Flow.ChooseMartialArt(0);
        Flow.playerController.enabled = false;
        var encounters = FindObjectsByType<EncounterTrigger>();
        foreach (var encounter in encounters) encounter.GetComponent<Collider>().enabled = false;
        Time.timeScale = 0;
        yield return CompareScene("town_gate_landscape", new Vector3(-11.2f, 0, -12.3f), false);
        yield return CompareScene("town_gate_portrait", new Vector3(-11.2f, 0, -12.3f), true);
        Resize(960, 540); yield return null; Teleport(new Vector3(-11.2f, 0, -12.3f)); yield return null;
        yield return CheckLayeredOccluders();
        var effect = Flow.cameraFollow.GetComponent<ForegroundOcclusion>();
        Time.timeScale = 1;
        var enemy = encounters.First(e => e.encounterType == EncounterType.NormalEnemy);
        enemy.enemyStats.maxHealth = enemy.enemyStats.currentHealth = 100000;
        enemy.enemyStats.attack = .1f;
        Flow.mainTimeRemaining = 59;
        Flow.HandleEncounter(enemy);
        float remaining = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(.2f);
        Check(Shader.GetGlobalVector("_ForegroundAnchor").w == 0 && Flow.mainTimeRemaining < remaining, "Ordinary battle disables effect and keeps consuming main time");
        CheckInactive("Ordinary battle");
        Flow.battleManager.CancelBattle(); Invoke(Flow, "SetPhase", GamePhase.MainMapRunning);
        Flow.HandleEncounter(encounters.First(e => e.encounterType == EncounterType.HiddenCave));
        Flow.BeginCaveBattle(enemy.CreateEnemyStats(), 0, 0, null);
        remaining = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(.2f);
        Check(Shader.GetGlobalVector("_ForegroundAnchor").w == 0 && Flow.mainTimeRemaining == remaining && Flow.battleManager.IsBattleActive &&
            Flow.battleManager.BattleElapsed > 0, "Active cave battle disables effect and pauses main time");
        Flow.battleManager.CancelBattle();
        CheckInactive("Cave battle");
        Invoke(Flow.caveRoom, "LeaveCave"); yield return null;
        Check(effect.enabled, "Returning from cave keeps camera occlusion available");
        Flow.bossIntroDuration = 0; Invoke(Flow, "BeginBossBattle");
        remaining = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(.2f);
        Check(Shader.GetGlobalVector("_ForegroundAnchor").w == 0 && Flow.mainTimeRemaining == remaining && Flow.bossBattleTime > 0, "Boss disables effect and uses independent time");
        Flow.battleManager.CancelBattle();
        CheckInactive("Boss battle");
        var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("BambooValleyLevel");
        while (!load.isDone) yield return null;
        yield return null; yield return null;
        Invoke(Flow, "BeginLevelTwoAfterTransition"); Flow.ChooseMartialArt(0);
        Flow.playerController.enabled = false;
        foreach (var encounter in FindObjectsByType<EncounterTrigger>()) encounter.GetComponent<Collider>().enabled = false;
        Time.timeScale = 0;
        yield return CompareScene("bamboo_landscape", new Vector3(-15, 0, -7), false);
        yield return CompareScene("bamboo_portrait", new Vector3(-15, 0, -7), true);
        var follow = Flow.cameraFollow;
        follow.enabled = false;
        var camera = follow.GetComponent<Camera>();
        CheckInactive("Disabled camera follow");
        Render(camera, null, 540, 960);
        Check(Shader.GetGlobalVector("_ForegroundAnchor").w == 0, "Disabling follow leaves no stale global opening");
        follow.enabled = true;

    }

    private void Restore()
    {
        if (restored) return;
        restored = true;
        Application.logMessageReceived -= Log;
        Application.runInBackground = background; Time.timeScale = previousTimeScale;
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
