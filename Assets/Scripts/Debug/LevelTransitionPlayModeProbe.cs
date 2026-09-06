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
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.UI;

/// <summary>Real scene reload regression. Attach only in Editor Play Mode.</summary>
public sealed class LevelTransitionPlayModeProbe : MonoBehaviour
{
    private readonly List<string> passed = new List<string>();
    private readonly List<float> durations = new List<float>();
    private bool originalBackground;
    private int originalUnlock;
    private float loadStarted;
    private bool capture;
    private string screenshot;
    private GameFlowController Flow => GameFlowController.Instance;
    private const string UnlockKey = "WuxiaRoguelite.TutorialCompleted.v1";
    [Serializable] private class Report
    {
        public bool success;
        public string error;
        public string[] passed;
        public float[] loadingSeconds;
    }

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        originalBackground = Application.runInBackground;
        originalUnlock = PlayerPrefs.GetInt(UnlockKey, -1);
        Application.runInBackground = true;
        var stack = new Stack<IEnumerator>();
        stack.Push(Suite());
        string error = null;
        while (stack.Count > 0)
        {
            object next = null;
            bool moved = false;
            try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
            catch (Exception ex) { error = ex.ToString(); }
            if (error != null) break;
            if (!moved) { stack.Pop(); continue; }
            if (next is IEnumerator nested) stack.Push(nested);
            else yield return next;
        }
        Directory.CreateDirectory("docs/validation");
        File.WriteAllText("docs/validation/level_loading_2026-09-06.json", JsonUtility.ToJson(new Report
        { success = error == null, error = error, passed = passed.ToArray(), loadingSeconds = durations.ToArray() }, true));
        if (originalUnlock < 0) PlayerPrefs.DeleteKey(UnlockKey);
        else PlayerPrefs.SetInt(UnlockKey, originalUnlock);
        PlayerPrefs.Save();
        Application.runInBackground = originalBackground;
        Time.timeScale = 1f;
        Debug.Log("LevelTransitionProbe " + (error == null ? "PASS" : "FAIL: " + error));
        EditorApplication.isPlaying = false;
    }

    private IEnumerator Suite()
    {
        PlayerPrefs.DeleteKey(UnlockKey);
        LevelSequence.LoadLevelTwoFromSelection();
        Check(!LevelLoadingScreen.IsLoading, "locked level two cannot load");
        LevelSequence.MarkTutorialCompleted();
        Begin(); LevelSequence.LoadLevelTwoFromSelection();
        yield return WaitForLoad(LevelSequence.LevelTwoSceneName);
        CheckOpeningChoice("selection starts level two");

        Begin(); LevelSequence.LoadTutorial();
        yield return WaitForLoad(LevelSequence.TutorialSceneName);
        Check(Flow.IsTutorialNoticeActive && Flow.mainTimeRemaining == 30f, "tutorial remains at 30 before confirmation");
        Invoke(Flow, "CompleteTutorial");
        Check(Flow.CanContinueToNextLevel, "tutorial victory enables next level");
        Begin(); Flow.ContinueToNextLevel();
        Flow.ContinueToNextLevel(); Flow.ReturnToMainMenu(); LevelSequence.LoadLevelSelection();
        capture = true; screenshot = "next_level_loading.png";
        yield return WaitForLoad(LevelSequence.LevelTwoSceneName);
        CheckOpeningChoice("next level plus repeated next/home input starts level two");
        Check(!LevelSequence.ConsumeLevelTwoAutoStartRequest(), "auto-start request consumed once");
        ScreenCapture.CaptureScreenshot("docs/validation/level_loading_images/next_level_ready.png");
        yield return null;

        Begin(); LevelSequence.LoadTutorial();
        yield return WaitForLoad(LevelSequence.TutorialSceneName);
        Time.timeScale = 0f;
        Begin(); Flow.SkipTutorialLevel();
        yield return WaitForLoad(LevelSequence.LevelTwoSceneName);
        CheckOpeningChoice("skip from paused tutorial starts level two");
        Check(Time.timeScale == 1f, "loading restores playable time scale");

        Flow.ChooseMartialArt(0);
        var enemy = Flow.bossStats.Clone(); enemy.maxHealth = enemy.currentHealth = 100000f; enemy.attack = 0.1f;
        Invoke(Flow, "BeginNormalBattle", enemy, 0, 0, EncounterType.NormalEnemy);
        float main = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(Flow.CurrentPhase == GamePhase.NormalBattleRunning && Flow.mainTimeRemaining < main,
            "normal combat consumes main countdown");
        Flow.battleManager.CancelBattle(); Invoke(Flow, "SetPhase", GamePhase.CaveRunning);
        Flow.BeginCaveBattle(enemy.Clone(), 0, 0, null); main = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(Flow.CurrentPhase == GamePhase.CaveRunning && Flow.mainTimeRemaining == main,
            "cave combat freezes main countdown");
        Flow.battleManager.CancelBattle(); Flow.bossIntroDuration = 0f;
        Flow.bossStats = enemy.Clone(); Invoke(Flow, "BeginBossBattle"); main = Flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(Flow.CurrentPhase == GamePhase.BossBattle && Flow.bossBattleTime > 0f && Flow.mainTimeRemaining == main,
            "final boss uses independent timer");
        Flow.battleManager.CancelBattle(); Invoke(Flow, "SetPhase", GamePhase.Result);
        Check(!Flow.CanContinueToNextLevel, "final level next remains disabled");

        Begin(); LevelSequence.LoadLevelSelection();
        yield return WaitForLoad(LevelSequence.LevelTwoSceneName);
        Check(Flow.CurrentPhase == GamePhase.Ready && Flow.currentChoices.Count == 0,
            "explicit home request returns home without stale auto-start");
    }

    private void Begin() { loadStarted = Time.realtimeSinceStartup; }

    private IEnumerator WaitForLoad(string destination)
    {
        Check(LevelLoadingScreen.IsLoading, "overlay starts immediately for " + destination);
        var source = Flow;
        float main = source.mainTimeRemaining;
        float lastProgress = 0f;
        bool sawIntermediate = false;
        bool sawComplete = false;
        while (LevelLoadingScreen.IsLoading)
        {
            if (Time.realtimeSinceStartup - loadStarted > 30f) throw new Exception("Scene transition timeout");
            var overlay = FindFirstObjectByType<LevelLoadingScreen>();
            float progress = overlay.Progress;
            if (progress < lastProgress || progress < 0f || progress > 1f) throw new Exception("Invalid progress");
            lastProgress = progress;
            sawIntermediate |= progress > 0f && progress < 1f;
            sawComplete |= progress == 1f;
            if (source != null && source.mainTimeRemaining != main) throw new Exception("Loading consumed main time");
            if (capture && progress > 0.4f && progress < 0.9f)
            {
                Directory.CreateDirectory("docs/validation/level_loading_images");
                ScreenCapture.CaptureScreenshot("docs/validation/level_loading_images/" + screenshot);
                capture = false;
            }
            yield return null;
        }
        float elapsed = Time.realtimeSinceStartup - loadStarted;
        durations.Add(elapsed);
        Check(elapsed >= 5f && sawIntermediate && sawComplete, "five real seconds, monotonic progress and visible 100 percent");
        Check(SceneManager.GetActiveScene().name == destination, "loaded requested scene " + destination);
    }

    private void CheckOpeningChoice(string label)
    {
        Check(Flow.CurrentPhase == GamePhase.LevelUpPaused && Flow.currentChoices.Count == 3 &&
              Flow.mainTimeRemaining == 60f && !Flow.IsLevelSelectionOpen, label);
    }
    private void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        passed.Add(label);
    }
    private static void Invoke(object target, string method, params object[] args)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
#endif
