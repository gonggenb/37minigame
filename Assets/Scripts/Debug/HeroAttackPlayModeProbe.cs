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
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

// Opt-in integration probe. Temporary input/collision fixtures never modify saved scenes.
public sealed class HeroAttackPlayModeProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.HeroAttackProbe";
    private const string Output = "docs/validation/hero_attacks";
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

    [MenuItem("37 MiniGame/Validate Hero Attack Pack Play Mode")]
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
        go.AddComponent<HeroAttackPlayModeProbe>();
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
        Debug.Log("HERO_ATTACK_" + (error == null ? "PASS" : "FAIL: " + error));
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
        foreach (HeroAttackForm form in Enum.GetValues(typeof(HeroAttackForm)))
        {
            var frames = HeroAttackArt.Frames(form);
            Check(frames.Length == 8, form + ": eight build-safe Resources sprites");
            foreach (var frame in frames)
                Check(frame.rect.size == new Vector2(256,256) && frame.pivot == new Vector2(128,32) &&
                    frame.pixelsPerUnit == 160 && frame.texture.filterMode == FilterMode.Point,
                    frame.name + ": dimensions, foot pivot and pixel filtering");
        }
        var screen = FindAnyObjectByType<BattleScreenController>();
        var battle = F.battleManager;
        var player = F.playerStats;
        Check(screen.playerAttackFrames[0] == HeroAttackArt.Frames(HeroAttackForm.Basic)[0] &&
              screen.playerIdleFrames[0] == screen.playerAttackFrames[0], "runtime replacement and matching idle are bound");
        Invoke(F, "SetPhase", GamePhase.MainMapRunning);
        F.mainTimeRemaining = 60f;
        F.playerController.enabled = false;
        var enemy = new CombatantStats { displayName = "Training fixture", maxHealth = 10000000,
            currentHealth = 10000000, attack = .1f, attackSpeed = .2f, defense = 0f };
        Invoke(F, "BeginNormalBattle", enemy.Clone(), 0, 0, EncounterType.NormalEnemy);
        // Controlled fixtures exercise the actual DoAttack resolver; stop cadence only for screenshots.
        battle.StopAllCoroutines();
        foreach (bool portrait in new[] { false, true })
        {
            Resize(portrait ? 540 : 960, portrait ? 960 : 540);
            yield return new WaitForSeconds(.2f);
            Check(Screen.width == (portrait ? 540 : 960), "actual Game View orientation " + portrait);
            foreach (HeroAttackForm form in Enum.GetValues(typeof(HeroAttackForm)))
            {
                player.ResetRun();
                if (player.equipment != null) player.equipment.ResetRun(player);
                if (form == HeroAttackForm.SwordQi) player.ApplyMartialArt("剑气诀");
                if (form == HeroAttackForm.VenomPalm) player.ApplyMartialArt("毒砂掌");
                if (form == HeroAttackForm.BloodCleave) player.ApplyMartialArt(portrait ? "修罗血域" : "惊鸿一式");
                player.runtimeStats.maxHealth = 1000000; player.runtimeStats.currentHealth = 1000000;
                player.runtimeStats.attack = 1f; player.runtimeStats.critChance = 0f;
                if (portrait && form == HeroAttackForm.BloodCleave) player.runtimeStats.critChance = 1f;
                player.runtimeStats.dodgeChance = 0f;
                typeof(BattleManager).GetProperty("PlayerSuccessfulHits").SetValue(battle, form == HeroAttackForm.SwordQi ? 2 : 0);
                typeof(BattleManager).GetProperty("PlayerAttackCooldownDuration").SetValue(battle, 1f);
                Invoke(battle, "DoAttack", player.runtimeStats, battle.currentEnemy);
                Invoke(screen, "TrackHeroAttack");
                Check(screen.CurrentHeroAttackForm == form && screen.IsHeroAttackPlaying, form + ": actual skill resolver selects pose");
                int sequence = battle.PlayerAttackVisualSequence;
                var cues = battle.PlayerAttackVisualCues;
                int vfxCount = screen.ActiveHeroSkillVfxCount;
                Check(form == HeroAttackForm.Basic ? vfxCount == 0 : vfxCount > 0,
                    form + ": only skill attacks create independent effect instances");
                Invoke(battle, "DoAttack", battle.currentEnemy, player.runtimeStats);
                Invoke(screen, "TrackHeroAttack");
                Check(battle.PlayerAttackVisualSequence == sequence && battle.PlayerAttackVisualCues == cues &&
                    screen.IsHeroAttackPlaying && screen.CurrentHeroAttackForm == form, form + ": same-frame enemy hit preserves player pose");
                Check(screen.ActiveHeroSkillVfxCount == vfxCount, form + ": enemy hit preserves skill effect");
                if (form == HeroAttackForm.VenomPalm)
                {
                    Invoke(battle, "ApplyPoisonTick"); Invoke(screen, "TrackHeroAttack");
                    Check(battle.PlayerAttackVisualSequence == sequence && screen.CurrentHeroAttackForm == form,
                        "periodic poison does not retrigger or replace player animation");
                    Check(screen.ActiveHeroSkillVfxCount == vfxCount, "poison tick does not enqueue duplicate skill effect");
                }
                yield return new WaitForSecondsRealtime(HeroAttackArt.Duration(form) * .55f);
                yield return Capture(form + (portrait ? "_portrait" : "_landscape"));
                yield return new WaitForSecondsRealtime(.24f);
                yield return Capture(form + (portrait ? "_impact_portrait" : "_impact_landscape"));
                yield return new WaitForSecondsRealtime(.65f);
                Check(!screen.IsHeroAttackPlaying && screen.HeroAttackProgress == 1f, form + ": non-looping playback finishes");
                Check(screen.ActiveHeroSkillVfxCount == 0, form + ": independent effect expires");
            }
        }
        Check(HeroAttackArt.Select(BattleVfxCue.BloodBurst | BattleVfxCue.SwordQi | BattleVfxCue.PoisonApplied) == HeroAttackForm.BloodCleave &&
              HeroAttackArt.Select(BattleVfxCue.SwordQi | BattleVfxCue.PoisonApplied) == HeroAttackForm.SwordQi,
              "mixed-build burst priority is stable");
        battle.CancelBattle(); Invoke(screen,"TrackHeroAttack");
        Check(!screen.IsHeroAttackPlaying && battle.PlayerAttackVisualSequence == 0 && screen.ActiveHeroSkillVfxCount == 0,
            "cancel clears pose and effect state");
        player.ResetRun(); player.runtimeStats.maxHealth=1000000;player.runtimeStats.currentHealth=1000000;
        player.runtimeStats.attackSpeed=10f;player.runtimeStats.attack=1f;
        F.mainTimeRemaining=60f;
        Invoke(F,"BeginNormalBattle",enemy.Clone(),0,0,EncounterType.NormalEnemy);
        float main=F.mainTimeRemaining;
        yield return new WaitForSeconds(.6f);
        Check(F.mainTimeRemaining<main && battle.PlayerAttackVisualSequence>=2, "real high-speed combat advances attacks and main timer");
        Check(screen.ActiveHeroSkillVfxCount > 0 && screen.ActiveHeroSkillVfxCount <= 3,
            "real high-speed sword effects remain visible within fixed capacity");
        battle.CancelBattle();Invoke(F,"SetPhase",GamePhase.CaveRunning);
        F.BeginCaveBattle(enemy.Clone(),0,0,null);main=F.mainTimeRemaining;
        yield return new WaitForSeconds(.35f);
        Check(F.mainTimeRemaining==main && battle.PlayerAttackVisualSequence>0,"cave attacks animate while main timer pauses");
        battle.CancelBattle();F.bossIntroDuration=0f;F.bossStats=enemy.Clone();
        Invoke(F,"BeginBossBattle");main=F.mainTimeRemaining;
        yield return new WaitForSeconds(.35f);
        Check(F.CurrentPhase==GamePhase.BossBattle && F.bossBattleTime>0f && F.mainTimeRemaining==main,
            "final Boss advances independent timer");
        battle.CancelBattle();
    }
}
#endif
