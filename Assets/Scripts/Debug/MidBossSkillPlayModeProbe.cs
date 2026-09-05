#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

/// <summary>Editor-only integration assertions using the actual scene and battle coroutine.</summary>
public sealed class MidBossSkillPlayModeProbe : MonoBehaviour
{
    private const string SessionKey = "37MiniGame.MidBossSkillProbe";
    private const string ReportPath = "docs/validation/midboss_skill_pack_2026-09-05.json";
    private GameFlowController flow;
    private BattleManager battle;
    private PlayerStats player;
    private readonly List<string> passed = new List<string>();
    private readonly List<string> skills = new List<string>();
    private float originalSpeed;
    private bool originalRunInBackground;
    private UnityEngine.Random.State randomState;
    private IEnumerator suite;

    [Serializable] private class Report
    {
        public bool success;
        public string error;
        public string[] passed;
        public string[] observedSkills;
        public string scope = "Actual MainPrototype Play Mode; controlled fixtures, not natural-route balance approval";
    }

    [MenuItem("37 MiniGame/Validate Mid Boss Skill Pack")]
    private static void Queue()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(SessionKey, true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod] private static void Install()
    {
        EditorApplication.playModeStateChanged -= Bootstrap;
        EditorApplication.playModeStateChanged += Bootstrap;
    }

    private static void Bootstrap(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, false);
        new GameObject("MidBossSkillPlayModeProbe").AddComponent<MidBossSkillPlayModeProbe>();
    }

    private void Start()
    {
        flow = FindAnyObjectByType<GameFlowController>();
        battle = flow.battleManager;
        player = flow.playerStats;
        originalSpeed = battle.battleSpeedMultiplier;
        originalRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(90530);
        Time.timeScale = 1f;
        battle.battleSpeedMultiplier = 1.5f;
        suite = RunSuite();
        StartCoroutine(Drive());
    }

    private IEnumerator Drive()
    {
        while (true)
        {
            object next = null;
            bool moved = false;
            string error = null;
            try { moved = suite.MoveNext(); if (moved) next = suite.Current; }
            catch (Exception ex) { error = ex.ToString(); }
            if (error != null || !moved)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
                { success = error == null, error = error, passed = passed.ToArray(), observedSkills = skills.ToArray() }, true));
                battle.CancelBattle();
                battle.battleSpeedMultiplier = originalSpeed;
                Application.runInBackground = originalRunInBackground;
                UnityEngine.Random.state = randomState;
                Time.timeScale = 1f;
                Debug.Log("MidBossSkillProbe " + (error == null ? "PASS" : "FAIL: " + error));
                EditorApplication.isPlaying = false;
                yield break;
            }
            yield return next;
        }
    }

    private void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        passed.Add(label);
    }

    private static object Invoke(object target, string method, params object[] args)
    {
        return target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }

    private void FreshMap()
    {
        Time.timeScale = 1f;
        flow.StartRun();
        for (int i = 0; i < 12 && flow.IsOpeningIntroActive; i++) flow.AdvanceOpeningIntro();
        for (int i = 0; i < 8 && flow.CurrentPhase == GamePhase.LevelUpPaused; i++) flow.ChooseMartialArt(0);
        // Controlled fixture: no learned effects, armor or random crits obscure the assertions.
        player.ResetRun();
        player.runtimeStats.maxHealth = 1000f;
        player.runtimeStats.currentHealth = 700f;
        player.runtimeStats.attack = 1f;
        player.runtimeStats.defense = 3f;
        player.runtimeStats.attackSpeed = 1f;
        player.runtimeStats.dodgeChance = 0f;
        player.runtimeStats.critChance = 0f;
        flow.mainTimeRemaining = 60f;
        if (flow.CurrentPhase != GamePhase.MainMapRunning) throw new Exception("FreshMap did not enter map");
    }

    private IEnumerator RunSuite()
    {
        yield return null;
        var screen = FindAnyObjectByType<BattleScreenController>();
        var profile = Array.Find(screen.enemyVisualProfiles, x => x.id == GameTextCatalog.MidBossVisualId);
        Check(profile != null && !profile.flipHorizontally && profile.doubleCleaveFrames.Length == 8 &&
              profile.ironGuardFrames.Length == 8 && screen.doubleCleaveEffectFrames.Length == 6 &&
              screen.ironGuardEffectFrames.Length == 6, "scene bindings: left-facing 8+8 character / 6+6 VFX frames");
        Check(Mathf.Approximately(flow.midBossStats.maxHealth, 290f) && Mathf.Approximately(flow.midBossStats.attack, 13f),
            "scene tuning: 290 health / 13 attack");

        FreshMap();
        flow.mainTimeRemaining = 30.15f;
        yield return new WaitForSeconds(0.30f);
        Check(flow.CurrentPhase == GamePhase.MidBossBattle && battle.IsMidBossBattle, "automatic elapsed-30-second checkpoint");
        Check(player.runtimeStats.currentHealth <= 700f, "midboss entrance does not heal player");
        float main = flow.mainTimeRemaining;
        float deadline = Time.time + 18f;
        int previousSequence = -1;
        bool captured = false;
        while (Time.time < deadline && skills.Count < 3)
        {
            if (battle.BossSkillSequence != previousSequence && battle.LastBossSkill != BossSkillId.None)
            {
                previousSequence = battle.BossSkillSequence;
                skills.Add(battle.LastBossSkill.ToString());
            }
            if (!captured && battle.CurrentMidBossSkill == BossSkillId.DoubleCleave && battle.MidBossSkillElapsed > 0.40f)
            {
                Directory.CreateDirectory("docs/validation/midboss_skill_pack_images");
                ScreenCapture.CaptureScreenshot("docs/validation/midboss_skill_pack_images/double_cleave" +
                    (Screen.width > Screen.height ? "_landscape" : "") + ".png");
                captured = true;
            }
            yield return null;
        }
        Check(skills.Count >= 3 && skills[0] == "MountainBreaker" && skills[1] == "DoubleCleave" && skills[2] == "MountainBreaker",
            "shared-cooldown rotation: MountainBreaker -> DoubleCleave -> MountainBreaker");
        Check(Mathf.Abs(flow.mainTimeRemaining - main) < 0.001f && flow.midBossBattleTime > 8f,
            "midboss pauses main time and advances its independent timer");

        // Queue guard during a strike; let the strike complete, then verify one-shot guard.
        battle.currentEnemy.currentHealth = battle.currentEnemy.maxHealth * 0.49f;
        Check(battle.CurrentMidBossSkill != BossSkillId.IronGuard, "half-health does not interrupt current attack");
        deadline = Time.time + 2f;
        while (Time.time < deadline && !battle.MidBossGuardUsed) yield return null;
        Check(battle.MidBossGuardUsed && battle.CurrentMidBossSkill == BossSkillId.IronGuard, "guard begins after active strike");
        Check(Mathf.Abs(battle.BossWardMax - 23.2f) < 0.01f, "guard grants 8 percent maximum-health armor");
        float hp = battle.currentEnemy.currentHealth;
        Invoke(battle, "ApplyDamageToCurrentEnemy", 5f);
        Check(Mathf.Approximately(hp, battle.currentEnemy.currentHealth) && battle.BossWard <= 18.21f,
            "damage removes armor before health");
        yield return new WaitForSeconds(0.35f);
        ScreenCapture.CaptureScreenshot("docs/validation/midboss_skill_pack_images/iron_guard" +
            (Screen.width > Screen.height ? "_landscape" : "") + ".png");
        float remaining = battle.MidBossWardRemaining;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.25f);
        Check(Mathf.Abs(remaining - battle.MidBossWardRemaining) < 0.001f, "pause freezes guard lifetime");
        Time.timeScale = 1f;
        yield return new WaitForSeconds(3.1f);
        Check(battle.BossWard == 0f && battle.MidBossGuardUsed, "guard expires within three seconds");
        int guardSequence = battle.BossSkillSequence;
        yield return new WaitForSeconds(0.20f);
        Check(battle.BossSkillSequence == guardSequence, "half-health guard does not immediately retrigger");

        // Real player attacks continue during an active defensive pose.
        battle.CancelBattle();
        battle.BeginMidBossBattle(flow.midBossStats.Clone(), null);
        battle.currentEnemy.currentHealth = 140f;
        player.runtimeStats.attack = 40f;
        yield return new WaitForSeconds(0.40f);
        Check(battle.MidBossGuardUsed && battle.PlayerSuccessfulHits > 0 && battle.BossWard < 23.2f,
            "player can attack and break armor during guard pose");
        battle.CancelBattle();
        Check(!battle.IsMidBossSkillActive && battle.BossWard == 0f && !battle.MidBossGuardUsed &&
              battle.MidBossImpactSequence == 0, "cancel resets skill, armor and impact state");

        // Two independent hits use separate defense, evasion, shield and retaliation paths.
        player.ResetRun(); player.runtimeStats.maxHealth = 500f; player.runtimeStats.currentHealth = 500f;
        player.runtimeStats.attack = 1f; player.runtimeStats.defense = 3f; player.runtimeStats.dodgeChance = 0f;
        battle.BeginMidBossBattle(flow.midBossStats.Clone(), null);
        Invoke(battle, "BeginMidBossSkill", BossSkillId.DoubleCleave);
        float before = player.runtimeStats.currentHealth;
        float shieldBefore = battle.PlayerShield;
        int attempts = battle.EnemyAttackAttempts;
        yield return new WaitForSeconds(0.90f);
        Check(battle.MidBossImpactSequence == 2 && battle.EnemyAttackAttempts == attempts + 2 &&
              Mathf.Abs(before - player.runtimeStats.currentHealth + shieldBefore - battle.PlayerShield - 10.9f) < 0.05f,
              "two independent 65-percent hits each subtract defense" +
              " (hits=" + battle.MidBossImpactSequence + ", attempts=" + (battle.EnemyAttackAttempts - attempts) +
              ", health loss=" + (before - player.runtimeStats.currentHealth) + ", shield=" + battle.PlayerShield + ")");
        battle.CancelBattle();
        player.runtimeStats.currentHealth = 500f; player.runtimeStats.dodgeChance = 1f;
        battle.BeginMidBossBattle(flow.midBossStats.Clone(), null);
        Invoke(battle, "BeginMidBossSkill", BossSkillId.DoubleCleave);
        yield return new WaitForSeconds(0.90f);
        Check(battle.EnemyAttackAttempts == 2 && player.runtimeStats.currentHealth == 500f,
            "both cleave hits independently honor dodge");
        battle.CancelBattle();
        player.runtimeStats.dodgeChance = 0f; player.runtimeStats.currentHealth = 1f;
        bool? won = null;
        battle.BeginMidBossBattle(flow.midBossStats.Clone(), value => won = value);
        typeof(BattleManager).GetProperty("PlayerShield").SetValue(battle, 0f);
        Invoke(battle, "BeginMidBossSkill", BossSkillId.DoubleCleave);
        deadline = Time.time + 2.5f;
        while (!won.HasValue && Time.time < deadline) yield return null;
        Check(won == false && battle.EnemyAttackAttempts == 1,
            "lethal first cleave cancels second hit and resolves defeat (won=" + won +
            ", attempts=" + battle.EnemyAttackAttempts + ", hp=" + player.runtimeStats.currentHealth + ")");

        // Normal encounter crossing the threshold must finish and resolve choices first.
        FreshMap(); flow.mainTimeRemaining = 30.15f;
        var enemy = flow.midBossStats.Clone(); enemy.maxHealth = 500f; enemy.currentHealth = 500f; enemy.attack = 1f;
        Invoke(flow, "BeginNormalBattle", enemy, 0, 0, EncounterType.NormalEnemy);
        main = flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(flow.CurrentPhase == GamePhase.NormalBattleRunning && flow.IsMidBossTransitionPending &&
              flow.mainTimeRemaining < main, "normal combat consumes main time and queues checkpoint without replacing enemy");
        battle.currentEnemy.currentHealth = 0f;
        deadline = Time.time + 2.5f;
        while (flow.CurrentPhase == GamePhase.NormalBattleRunning && Time.time < deadline) yield return null;
        Check(flow.CurrentPhase == GamePhase.MidBossBattle, "queued checkpoint starts after normal battle settlement");
        battle.currentEnemy.currentHealth = 0f;
        deadline = Time.time + 2.5f;
        while (flow.CurrentPhase == GamePhase.MidBossBattle && Time.time < deadline) yield return null;
        Check(flow.CurrentPhase == GamePhase.MainMapRunning && flow.midBossDefeated, "midboss victory returns to map");

        // Cave and final boss retain the three core timing rules.
        FreshMap(); Invoke(flow, "SetPhase", GamePhase.CaveRunning);
        enemy.ResetHealth();
        flow.BeginCaveBattle(enemy.Clone(), 0, 0, null);
        main = flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.35f);
        Check(flow.CurrentPhase == GamePhase.CaveRunning && Mathf.Abs(flow.mainTimeRemaining-main)<0.001f,
            "cave combat pauses main time");
        battle.CancelBattle(); Invoke(flow, "SetPhase", GamePhase.MainMapRunning);
        flow.midBossDefeated = true; flow.mainTimeRemaining = 0.1f; flow.bossIntroDuration = 0f;
        yield return new WaitForSeconds(0.45f);
        Check(flow.CurrentPhase == GamePhase.BossBattle && battle.IsBossBattle && !battle.IsMidBossBattle &&
              flow.mainTimeRemaining == 0f && flow.bossBattleTime > 0f && !battle.MidBossGuardUsed,
            "final boss enters at zero with independent timer and no midboss skill state");
    }
}
#endif
