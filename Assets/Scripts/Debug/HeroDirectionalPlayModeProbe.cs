#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

// Opt-in integration probe. Temporary input/collision fixtures never modify saved scenes.
public sealed class HeroDirectionalPlayModeProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.HeroProbe";
    private const string Output = "docs/validation/hero_eight_directions";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private GameFlowController F => GameFlowController.Instance;
    private bool restored;
    private bool previousBackground;
    private int previousSize;
    private EditorWindow gameView;
    private object sizeGroup;
    private int addedSizes;
    private readonly List<string> errors = new List<string>();
    [Serializable] private class Report
    {
        public bool success;
        public string error;
        public string[] checks;
        public string[] runtimeErrors;
        public string scope = "Unity Editor Play Mode with controlled inputs, collision isolation and combat fixtures. Device/touch feel and final art approval remain separate.";
    }

    [MenuItem("37 MiniGame/Validate Hero Eight Directions Play Mode")]
    public static void Queue()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Key + ".Background", Application.runInBackground);
        Application.runInBackground = true;
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    [InitializeOnLoadMethod] private static void Install()
    {
        EditorApplication.playModeStateChanged -= Boot;
        EditorApplication.playModeStateChanged += Boot;
    }
    private static void Boot(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false);
        var go = new GameObject("Hero animation validation");
        DontDestroyOnLoad(go);
        go.AddComponent<HeroDirectionalPlayModeProbe>();
    }
    private void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
    private IEnumerator Start()
    {
        previousBackground = SessionState.GetBool(Key + ".Background", false);
        Application.logMessageReceived += OnLog;
        Directory.CreateDirectory(Output);
        var stack = new Stack<IEnumerator>(); stack.Push(Suite()); string error = null;
        while (stack.Count > 0)
        {
            object next = null; bool moved = false;
            try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
            catch (Exception e) { error = e.ToString(); }
            if (error != null) break;
            if (!moved) { stack.Pop(); continue; }
            if (next is IEnumerator nested) stack.Push(nested); else yield return next;
        }
        if (error == null && errors.Count > 0) error = "Runtime console contains errors.";
        File.WriteAllText(Output + "/playmode_report.json", JsonUtility.ToJson(new Report
            { success = error == null, error = error, checks = checks.ToArray(), runtimeErrors = errors.ToArray() }, true));
        Restore();
        Debug.Log("HERO_PLAYMODE_" + (error == null ? "PASS" : "FAIL: " + error));
        EditorApplication.isPlaying = false;
    }
    private void Restore()
    {
        if (restored) return; restored = true;
        Application.logMessageReceived -= OnLog;
        InputMove(Vector2.zero);
        Application.runInBackground = previousBackground;
        if (gameView != null)
        {
            gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(gameView, previousSize);
            for (int i = 0; i < addedSizes; i++)
            {
                int total = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
                sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup, new object[] { total - 1 });
            }
        }
        Time.timeScale = 1f;
    }
    private void OnDestroy() { Restore(); }
    private void Check(bool passed, string message)
    { if (!passed) throw new Exception(message); checks.Add(message); }
    private static void InputMove(Vector2 input) => typeof(MobileInputController).GetProperty("MoveInput").GetSetMethod(true).Invoke(null, new object[] { input });
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    private static Vector2 Direction(int index) => new Vector2(Mathf.Cos(index * Mathf.PI / 4f), Mathf.Sin(index * Mathf.PI / 4f));

    private void Resize(int width, int height)
    {
        var assembly = typeof(Editor).Assembly;
        var type = assembly.GetType("UnityEditor.GameView");
        if (gameView == null)
        {
            gameView = EditorWindow.GetWindow(type);
            previousSize = (int)type.GetProperty("selectedSizeIndex", Flags).GetValue(gameView);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            sizeGroup = sizesType.GetProperty("currentGroup", Flags).GetValue(sizes);
        }
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kind = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new object[] { Enum.ToObject(kind, 1), width, height, "Hero probe temporary" });
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
    private IEnumerator Suite()
    {
        yield return null;
        Check(HeroDirectionalArt.Available, "All 72 sprites load through the build-safe Resources path");
        var boundary = new HeroDirectionalPlayback();
        boundary.Tick(Vector2.right, .01f); boundary.Tick(DirectionAngle(24f), .01f);
        Check(boundary.Direction == 0, "Joystick sector boundary has hysteresis");
        boundary.Tick(DirectionAngle(29f), .01f);
        Check(boundary.Direction == 1, "Joystick crossing hysteresis enters diagonal");
        var slow = new HeroDirectionalPlayback(); var fast = new HeroDirectionalPlayback();
        slow.Tick(Vector2.right, 0); fast.Tick(Vector2.right, 0);
        slow.Tick(Vector2.right, .2f, .5f); fast.Tick(Vector2.right, .2f, 1f);
        Check(Mathf.Approximately(fast.FrameClock, slow.FrameClock * 2f), "Playback phase scales with movement speed");
        fast.Tick(Vector2.up, 0f);
        Check(Mathf.Approximately(fast.FrameClock, 2.4f), "Direction changes preserve stride phase");

        foreach (string scene in new[] { "MainPrototype", "TutorialLevel", "BambooValleyLevel" })
        {
            if (SceneManager.GetActiveScene().name != scene) yield return SceneManager.LoadSceneAsync(scene);
            yield return new WaitForSeconds(.25f);
            if (F.IsTutorialNoticeActive) F.DismissTutorialNotice();
            Invoke(F, "SetPhase", GamePhase.MainMapRunning);
            F.mainTimeRemaining = 600f;
            foreach (var mobile in FindObjectsByType<MobileInputController>(FindObjectsSortMode.None)) mobile.enabled = false;
            foreach (var collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (collider.GetComponentInParent<WuxiaRoguelite.Player.PlayerController>() == null) collider.enabled = false;
            var player = F.playerController;
            var animator = player.GetComponentInChildren<SpriteFrameAnimator>();
            var renderer = animator.GetComponent<SpriteRenderer>();
            Check(animator.movementSource == player, scene + ": animator is bound to player input");
            Resize(960, 540); yield return new WaitForSeconds(.15f);
            Check(Screen.width == 960 && Screen.height == 540, scene + ": actual Game View is 960x540");
            Check(Mathf.Approximately(player.visualRoot.localScale.x, player.landscapeVisualScale), scene + ": landscape visual scale applied");
            for (int d = 0; d < 8; d++)
            {
                player.ResetToSpawn();
                Vector3 start = player.transform.position;
                var visited = new HashSet<string>();
                float end = Time.time + .8f;
                while (Time.time < end)
                {
                    InputMove(Direction(d)); yield return null;
                    if (renderer.sprite != null) visited.Add(renderer.sprite.name);
                }
                Check(animator.HeroPlayback.Direction == d && renderer.sprite.name.Contains("_run_" + HeroDirectionalArt.Directions[d] + "_"), scene + ": run direction " + HeroDirectionalArt.Directions[d]);
                Check(visited.Count >= 4, scene + ": multiple moving frames " + HeroDirectionalArt.Directions[d] + " (" + visited.Count + ")");
                Check(Vector3.Distance(start, player.transform.position) > .3f, scene + ": Rigidbody moves " + HeroDirectionalArt.Directions[d]);
                Check(!renderer.flipX && renderer.sprite.pivot == new Vector2(128, 32), scene + ": explicit direction and foot pivot " + HeroDirectionalArt.Directions[d]);
                InputMove(Vector2.zero); yield return null; yield return null;
                Check(renderer.sprite.name.Contains("_idle_" + HeroDirectionalArt.Directions[d] + "_"), scene + ": stop retains " + HeroDirectionalArt.Directions[d]);
            }
            player.ResetToSpawn(); F.cameraFollow.ResetVision(); yield return new WaitForSeconds(.35f);
            Vector3 ground = player.transform.position;
            float expectedLift = player.followPingchuanTownHeight
                ? PingchuanTownLayout.SurfaceHeight(ground.x, ground.z)
                : player.followTutorialRestStopHeight
                ? TutorialRestStopLayout.SurfaceHeight(ground.x, ground.z)
                : player.followBambooValleyHeight ? BambooValleyLayout.SurfaceHeight(ground.x, ground.z)
                : MainMapBridgeSurface.GetVisualLift(ground);
            Check(Mathf.Abs(player.visualRoot.localPosition.y - expectedLift) < .03f,
                scene + ": feet anchor follows the authored surface height");
            var shadow = player.transform.Find("Actor Ground Shadow");
            if (player.GetComponent<ActorGroundShadow>() != null)
                Check(shadow != null && Mathf.Abs(shadow.localPosition.y - expectedLift - .025f) < .03f,
                    scene + ": authored shadow follows the same surface after anchor migration");
            else checks.Add(scene + ": no authored ground-shadow component (existing scene configuration)");
            yield return Capture(scene + "_landscape");
            Resize(540, 960); yield return new WaitForSeconds(.2f);
            Check(Screen.width == 540 && Screen.height == 960, scene + ": actual Game View is 540x960");
            Check(Mathf.Approximately(player.visualRoot.localScale.x, player.portraitVisualScale), scene + ": portrait visual scale applied");
            yield return Capture(scene + "_portrait");
        }

        // Cave uses screen-space input, its own clock and the same sprite bank.
        F.DebugEnterCave(CaveContentType.Merchant);
        Check(F.CurrentPhase == GamePhase.CaveRunning, "Cave fixture enters exploration");
        var room = F.caveRoom;
        Set(room, "eventCompleted", true);
        var playback = (HeroDirectionalPlayback)room.GetType().GetField("heroPlayback", Flags).GetValue(room);
        float caveTime = F.mainTimeRemaining;
        for (int d = 0; d < 8; d++)
        {
            Set(room, "playerPosition", new Vector2(.4f, .65f));
            InputMove(Direction(d)); yield return new WaitForSeconds(.15f);
            Check(playback.Direction == d && playback.CurrentSprite.name.Contains("_run_"), "Cave direction " + HeroDirectionalArt.Directions[d]);
            InputMove(Vector2.zero); yield return null; yield return null;
            Check(!playback.IsMoving && playback.CurrentSprite.name.Contains("_idle_"), "Cave stops facing " + HeroDirectionalArt.Directions[d]);
        }
        Set(room, "playerPosition", new Vector2(.35f, .72f));
        yield return Capture("cave_portrait");
        Resize(960, 540); yield return new WaitForSeconds(.2f);
        yield return Capture("cave_landscape");
        InputMove(Vector2.right); yield return new WaitForSeconds(.1f);
        Set(room, "merchantOpen", true); yield return null; yield return null;
        Check(!playback.IsMoving, "Opening merchant stops cave locomotion animation");
        Set(room, "merchantOpen", false); InputMove(Vector2.zero);
        Check(F.mainTimeRemaining == caveTime, "Cave exploration preserves main countdown");
        var enemy = new CombatantStats { displayName = GameTextCatalog.FinalBossName, maxHealth = 100000, currentHealth = 100000, attack = .1f, attackSpeed = .5f };
        F.BeginCaveBattle(enemy.Clone(), 0, 0, null); yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining == caveTime && !playback.IsMoving, "Cave combat freezes main countdown and movement");
        F.battleManager.CancelBattle(); room.ResetRoom();
        Invoke(F, "BeginNormalBattle", enemy.Clone(), 0, 0, EncounterType.NormalEnemy);
        float normalTime = F.mainTimeRemaining; yield return new WaitForSeconds(.25f);
        Check(F.mainTimeRemaining < normalTime, "Normal combat continues main countdown");
        F.battleManager.CancelBattle(); F.bossIntroDuration = 0f; F.bossStats = enemy.Clone();
        Invoke(F, "BeginBossBattle"); float bossMainTime = F.mainTimeRemaining;
        yield return new WaitForSeconds(.35f);
        Check(F.CurrentPhase == GamePhase.BossBattle && F.bossBattleTime > 0f && F.mainTimeRemaining == bossMainTime, "Boss uses independent timer");
    }
    private static Vector2 DirectionAngle(float degrees) => new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
}
#endif
