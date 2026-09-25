#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

public sealed class WebMobilePlayModeProbe : MonoBehaviour
{
    private readonly List<string> passed = new List<string>();
    private readonly List<float> durations = new List<float>();
    private const string TutorialKey = "WuxiaRoguelite.TutorialCompleted.v1";
    private const string ThirdKey = "WuxiaRoguelite.LevelTwoCompleted.v1";
    private GameFlowController Flow => GameFlowController.Instance;
    [Serializable] private sealed class Report {
        public bool success; public string error; public string[] passed; public float[] seconds;
        public WebMemoryDiagnostics.Snapshot[] samples;
    }

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        int tutorial = PlayerPrefs.GetInt(TutorialKey, -1), third = PlayerPrefs.GetInt(ThirdKey, -1);
        var stack = new Stack<IEnumerator>();
        stack.Push(Suite());
        string error = null;
        while (stack.Count > 0)
        {
            object next = null; bool moved = false;
            try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
            catch (Exception e) { error = e.ToString(); break; }
            if (!moved) { stack.Pop(); continue; }
            if (next is IEnumerator nested) stack.Push(nested); else yield return next;
        }
        Restore(TutorialKey, tutorial); Restore(ThirdKey, third); PlayerPrefs.Save();
        Application.runInBackground = background;
        Time.timeScale = 1;
        Directory.CreateDirectory("docs/validation/webgl_mobile");
        File.WriteAllText("docs/validation/webgl_mobile/playmode.json", JsonUtility.ToJson(new Report {
            success = error == null, error = error, passed = passed.ToArray(), seconds = durations.ToArray(),
            samples = WebMemoryDiagnostics.Samples.ToArray()
        }, true));
        Debug.Log("WebMobileProbe " + (error == null ? "PASS" : "FAIL " + error));
        EditorApplication.isPlaying = false;
    }

    private IEnumerator Suite()
    {
        float deadline = Time.realtimeSinceStartup + 10;
        while (StudioSplashScreen.IsBlocking && Time.realtimeSinceStartup < deadline) yield return null;
        Check(!StudioSplashScreen.IsBlocking, "startup splash completes");
        LevelSequence.LoadMainMenu(); yield return WaitForLoad(LevelSequence.MenuSceneName);
        CheckMenu();
        PlayerPrefs.DeleteKey(TutorialKey);
        PlayerPrefs.DeleteKey(ThirdKey);
        LevelSequence.LoadLevelTwoFromSelection();
        Check(!LevelLoadingScreen.IsLoading, "locked level two cannot load");
        Flow.OpenLevelSelection();
        Check(Flow.IsLevelSelectionOpen, "light menu opens selection");
        Flow.SelectTutorialLevel();
        Check(Flow.IsOpeningIntroActive, "tutorial selection retains full opening dialogue");
        int dialogueCount = Flow.OpeningDialogueCount;
        for (int i = 0; i < dialogueCount; i++) Flow.AdvanceOpeningIntro();
        yield return WaitForLoad(LevelSequence.TutorialSceneName);
        Check(Flow.IsTutorialNoticeActive && Flow.mainTimeRemaining == 30, "tutorial waits at 30 seconds");
        Time.timeScale = 0; Flow.SkipTutorialLevel();
        Flow.SkipTutorialLevel(); Flow.ReturnToMainMenu(); LevelSequence.LoadLevelSelection();
        yield return WaitForLoad(LevelSequence.LevelTwoSceneName);
        Check(Flow.CurrentPhase == GamePhase.LevelUpPaused && Flow.currentChoices.Count == 3 &&
            Flow.mainTimeRemaining == 60, "skip and repeated input preserve opening choice and timer");
        Check(!LevelSequence.ConsumeAutoStartRequest() && Time.timeScale == 1, "intent consumed once and time restored");
        Flow.ConfirmChallengeBriefing(); Flow.ChooseMartialArt(0);
        var enemy = Flow.bossStats.Clone(); enemy.maxHealth = enemy.currentHealth = 100000; enemy.attack = 0.1f;
        Invoke("BeginNormalBattle", enemy, 0, 0, EncounterType.NormalEnemy);
        float main = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(Flow.CurrentPhase == GamePhase.NormalBattleRunning && Flow.mainTimeRemaining < main,
            "normal battle consumes main time");
        Flow.battleManager.CancelBattle(); Invoke("SetPhase", GamePhase.CaveRunning);
        Flow.BeginCaveBattle(enemy.Clone(), 0, 0, null); main = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(Flow.CurrentPhase == GamePhase.CaveRunning && Flow.mainTimeRemaining == main, "cave pauses main time");
        Flow.battleManager.CancelBattle(); Flow.bossIntroDuration = 0; Flow.bossStats = enemy.Clone();
        Invoke("BeginBossBattle"); main = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(Flow.CurrentPhase == GamePhase.BossBattle && Flow.bossBattleTime > 0 && Flow.mainTimeRemaining == main,
            "final boss timer is independent");
        Flow.battleManager.CancelBattle();
        PlayerPrefs.SetInt(ThirdKey, 1);
        LevelSequence.LoadLevelThree(); yield return WaitForLoad(LevelSequence.LevelThreeSceneName);
        Check(Flow.currentChoices.Count == 3 && Flow.mainTimeRemaining == 60, "third level starts with opening choice");
        Flow.ReturnToMainMenu(); yield return WaitForLoad(LevelSequence.MenuSceneName); CheckMenu();
        for (int i = 0; i < 2; i++)
        {
            LevelSequence.LoadLevelTwoFromSelection(); yield return WaitForLoad(LevelSequence.LevelTwoSceneName);
            Flow.ReturnToMainMenu(); yield return WaitForLoad(LevelSequence.MenuSceneName); CheckMenu();
        }
        Check(StudioSplashScreen.HasPresentedThisSession && !StudioSplashScreen.IsBlocking,
            "scene transitions do not replay studio splash");
    }

    private void CheckMenu()
    {
        Check(LevelSequence.IsMenuScene && Flow.CurrentPhase == GamePhase.Ready, "menu remains interactive");
        Check(FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
            "menu has no encounter objects");
        var screen = FindFirstObjectByType<BattleScreenController>();
        Check(screen.playerAttackFrames == null || screen.playerAttackFrames.Length == 0,
            "menu dialogue view does not preload hero combat strips");
    }

    private IEnumerator WaitForLoad(string destination)
    {
        float start = Time.realtimeSinceStartup, progress = 0;
        bool full = false;
        while (LevelLoadingScreen.IsLoading)
        {
            if (Time.realtimeSinceStartup - start > 60) throw new Exception("Transition timeout " + destination);
            var loader = FindFirstObjectByType<LevelLoadingScreen>();
            CheckQuiet(loader.Progress >= progress && loader.Progress <= 1, "progress regressed");
            progress = loader.Progress; full |= progress == 1;
            if (Flow != null && !LevelSequence.IsMenuScene)
                CheckQuiet(Flow.mainTimeRemaining == (LevelSequence.IsTutorialScene ? 30 : 60) ||
                    !LevelLoadingScreen.LastStages.Contains("destination-ready"), "destination timer ran during loading");
            yield return null;
        }
        durations.Add(Time.realtimeSinceStartup - start);
        Check(durations.Last() >= 5 && full, "minimum five seconds and visible 100 percent: " + destination);
        Check(SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == destination,
            "only destination scene remains: " + destination);
        Check(LevelLoadingScreen.LastStages.SequenceEqual(new[] { "before-unload", "old-scenes-released", "destination-ready", "revealed" }),
            "old scene released before destination: " + destination);
        Check(WebMemoryDiagnostics.Samples.Last(s => s.stage == "old-scenes-released").scene == "LevelTransition",
            "resource sweep runs in empty transition scene");
    }
    private void Invoke(string method, params object[] args) => typeof(GameFlowController)
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Flow, args);
    private void Check(bool value, string message) { CheckQuiet(value, message); passed.Add(message); }
    private static void CheckQuiet(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Restore(string key, int value) { if (value < 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, value); }
}
#endif
