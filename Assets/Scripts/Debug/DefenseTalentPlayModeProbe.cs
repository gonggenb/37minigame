#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;

/// <summary>Actual combat entry points, run lifetime, and core timers; no saved asset changes.</summary>
public sealed class DefenseTalentPlayModeProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.DefenseTalentProbe";
    private const string Output = "docs/validation/defense_boss_talents";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private GameFlowController flow;
    private BattleManager battle;
    private PlayerStats player;
    private UnityEngine.Random.State savedRandom;
    private float savedTime;
    private bool savedBackground;
    private IEnumerator suite;
    [Serializable] private class Report { public bool success; public string error; public string[] checks; }

    [MenuItem("37 MiniGame/Validate Defense and Boss Talents Play Mode")]
    private static void Queue()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainPrototype")
            throw new InvalidOperationException("Open MainPrototype first.");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    [InitializeOnLoadMethod] private static void Install()
    {
        EditorApplication.playModeStateChanged -= Bootstrap;
        EditorApplication.playModeStateChanged += Bootstrap;
    }
    private static void Bootstrap(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false);
        new GameObject("DefenseTalentPlayModeProbe").AddComponent<DefenseTalentPlayModeProbe>();
    }
    private void Start()
    {
        savedRandom = UnityEngine.Random.state; savedTime = Time.timeScale;
        savedBackground = Application.runInBackground; Application.runInBackground = true;
        Time.timeScale = 1;
        flow = FindAnyObjectByType<GameFlowController>(); battle = flow.battleManager; player = flow.playerStats;
        Directory.CreateDirectory(Output); suite = Suite(); StartCoroutine(Drive());
    }
    private IEnumerator Drive()
    {
        while (true)
        {
            bool more = false; object next = null; string error = null;
            try { more = suite.MoveNext(); if (more) next = suite.Current; }
            catch (Exception e) { error = e.ToString(); }
            if (!more || error != null)
            {
                File.WriteAllText(Output + "/mechanics.json", JsonUtility.ToJson(new Report
                    { success = error == null, error = error, checks = checks.ToArray() }, true));
                battle.CancelBattle(); Time.timeScale = savedTime; UnityEngine.Random.state = savedRandom;
                Application.runInBackground = savedBackground;
                Debug.Log("DefenseTalentProbe " + (error == null ? "PASS" : error));
                EditorApplication.isPlaying = false; yield break;
            }
            yield return next;
        }
    }
    private static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Flags).Invoke(obj, args);
    private void Set(string property, object value) => typeof(BattleManager).GetProperty(property, Flags).SetValue(battle, value);
    private void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks.Add(label); }
    private void Near(float value, float expected, string label) => Check(Mathf.Abs(value - expected) < .003f, label);
    private void Fixture(BossTalent talent, bool mid = false)
    {
        battle.CancelBattle(); player.ResetRun();
        player.runtimeStats.maxHealth = player.runtimeStats.currentHealth = 1000;
        player.runtimeStats.attack = 20; player.runtimeStats.defense = 5;
        player.runtimeStats.critChance = player.runtimeStats.dodgeChance = 0;
        var enemy = new CombatantStats { maxHealth = 10000, currentHealth = 10000, attack = 20,
            defense = 0, critChance = 0, dodgeChance = 0, bossTalent = talent };
        if (mid) battle.BeginMidBossBattle(enemy, null); else battle.BeginBossBattle(enemy, null);
        battle.StopAllCoroutines(); // Resolve specific hits synchronously, without incidental auto-attacks.
        battle.ResetRunReview(); Set("PlayerShield", 0f); Set("BossWard", 0f);
        Set("EnemyAttackAttempts", 0); Set("PlayerSuccessfulHits", 0);
    }
    private float Incoming(string hit, BossTalent talent, bool shield = false)
    {
        Fixture(talent, hit == "mid"); player.ApplyMartialArt("反震诀");
        Set("PlayerShield", shield ? 100f : 0f);
        UnityEngine.Random.InitState(921);
        float hp = player.runtimeStats.currentHealth;
        if (hit == "basic") Call(battle, "DoAttack", battle.currentEnemy, player.runtimeStats);
        else if (hit == "mid") Call(battle, "ResolveMidBossHit", 1.3f, 0);
        else Call(battle, "ResolveFoxfireHit", 0);
        Check(battle.RunReview.damage[(int)DamageSource.Retaliation] > 0, hit + ": retaliation on " + (shield ? "shield" : "health"));
        if (shield) Near(player.runtimeStats.currentHealth, hp, hit + ": full shield prevents health loss");
        return hp - player.runtimeStats.currentHealth;
    }
    private void FreshMap()
    {
        battle.CancelBattle(); flow.StartRun();
        for (int i = 0; i < 12 && flow.IsOpeningIntroActive; i++) flow.AdvanceOpeningIntro();
        flow.ConfirmChallengeBriefing();
        for (int i = 0; i < 8 && flow.CurrentPhase == GamePhase.LevelUpPaused; i++) flow.ChooseMartialArt(0);
        player.ResetRun(); player.runtimeStats.maxHealth = player.runtimeStats.currentHealth = 10000;
        player.runtimeStats.attack = 1; player.runtimeStats.dodgeChance = 0; flow.mainTimeRemaining = 60;
        Check(flow.CurrentPhase == GamePhase.MainMapRunning, "map initialized");
    }
    private IEnumerator Suite()
    {
        yield return null;
        float readyDeadline = Time.realtimeSinceStartup + 15;
        while ((WuxiaRoguelite.UI.StudioSplashScreen.IsBlocking || WuxiaRoguelite.UI.LevelLoadingScreen.IsLoading) &&
            Time.realtimeSinceStartup < readyDeadline) yield return null;
        Check(!WuxiaRoguelite.UI.StudioSplashScreen.IsBlocking, "startup overlay finished before timer checks");
        // Keep the real flow idle during controlled hit tests.
        Call(flow, "SetPhase", GamePhase.Ready);
        Fixture(BossTalent.None); player.ResetRun(); float defense = player.runtimeStats.defense;
        player.ApplyMartialArt("铁布衫"); Near(player.runtimeStats.defense - defense, 1.5f, "IronBody grants 1.5 defense");
        Fixture(BossTalent.None); Near((float)typeof(BattleManager).GetProperty("EffectivePlayerDefense", Flags).GetValue(battle), 6, "5 player defense mitigates 6 raw damage");
        foreach (string hit in new[] { "basic", "mid", "foxfire" })
        {
            float normal = Incoming(hit, BossTalent.None);
            float pierced = Incoming(hit, BossTalent.ArmorPiercer);
            Check(pierced > normal, hit + ": armor talent reduces defense without removing it");
            Incoming(hit, BossTalent.None, true);
        }
        // Every damage channel and boss ward use the same counter and actual accounting.
        foreach (BossTalent talent in Enum.GetValues(typeof(BossTalent)))
        foreach (DamageSource source in Enum.GetValues(typeof(DamageSource)))
        {
            Fixture(talent);
            float expected = talent == BossTalent.SwordGuard && (source == DamageSource.SwordQi || source == DamageSource.Combo) ? 75 :
                talent == BossTalent.VenomWard && source == DamageSource.Poison ? 70 :
                talent == BossTalent.ArmorPiercer && source == DamageSource.Retaliation ? 80 : 100;
            Set("BossWard", 40f); float hp = battle.currentEnemy.currentHealth;
            float actual = (float)Call(battle, "ApplyAttributedDamage", 100f, source);
            Near(actual, expected, talent + "/" + source + ": resolved damage");
            Near(hp - battle.currentEnemy.currentHealth, expected - 40, talent + "/" + source + ": ward then health");
            Near(battle.RunReview.damage[(int)source], actual, talent + "/" + source + ": review matches");
        }
        Fixture(BossTalent.BloodSeal); player.runtimeStats.currentHealth = 100;
        Call(battle, "HealPlayerInBattle", 100f); Near(player.runtimeStats.currentHealth, 170, "blood seal retains 70 percent healing");
        player.runtimeStats.lifeSteal = .5f; player.runtimeStats.critChance = 1; player.runtimeStats.critMultiplier = 2;
        float before = player.runtimeStats.currentHealth; UnityEngine.Random.InitState(22);
        Call(battle, "DoAttack", player.runtimeStats, battle.currentEnemy);
        Check(battle.LastDamage >= 33.25f && battle.LastDamage <= 36.75f, "blood seal reduces only critical bonus to 75 percent");
        Near(player.runtimeStats.currentHealth - before, battle.LastDamage * .5f * .7f, "lifesteal uses actual damage then healing penalty");
        Fixture(BossTalent.VenomWard); player.ApplyMartialArt("毒砂掌"); player.ApplyMartialArt("吸星诀");
        player.runtimeStats.currentHealth = 100; Set("EnemyPoisonStacks", 4); Call(battle, "ApplyPoisonTick");
        Near(player.runtimeStats.currentHealth - 100, battle.LastPoisonDamage * .1f, "poison healing uses resisted poison damage");
        Fixture(BossTalent.KeenSight); player.runtimeStats.dodgeChance = .5f;
        Near((float)Call(battle, "PlayerDodgeChance", 0f), .35f, "keen sight retains 70 percent random evasion");
        player.ApplyMartialArt("无相残影"); Set("EnemyAttackAttempts", 5); player.runtimeStats.dodgeChance = 0;
        Call(battle, "DoAttack", battle.currentEnemy, player.runtimeStats);
        Check(battle.LastAttackWasDodged, "guaranteed shadow dodge is preserved");
        Fixture(BossTalent.SwordGuard); battle.currentEnemy.currentHealth = 2;
        Near((float)Call(battle, "ApplyAttributedDamage", 100f, DamageSource.SwordQi), 2, "overkill excluded from resolved damage");
        battle.CancelBattle(); Check(battle.CurrentBossTalent == BossTalent.None, "cancel clears active counter");
        battle.BeginBattle(new CombatantStats { bossTalent = BossTalent.BloodSeal }, null); battle.StopAllCoroutines();
        Check(battle.CurrentBossTalent == BossTalent.None, "normal enemies never apply boss talents");
        var finalSet = new HashSet<BossTalent>(); var midSet = new HashSet<BossTalent>();
        UnityEngine.Random.InitState(92121);
        for (int i = 0; i < 80; i++) { Call(flow, "PrepareRunChallenge"); finalSet.Add(flow.FinalBossTalent); midSet.Add(flow.MidBossTalent); }
        Check(finalSet.Count == 5 && midSet.Count == 5 && !finalSet.Contains(BossTalent.None), "both boss pools cover five talents over seeded runs");
        var final = flow.FinalBossTalent; var mid = flow.MidBossTalent;
        flow.ConfirmChallengeBriefing(); flow.SelectChallengeTier(0); player.ApplyMartialArt("铁布衫");
        Check(flow.FinalBossTalent == final && flow.MidBossTalent == mid, "talents stay fixed across briefing, difficulty and build changes");
        FreshMap(); final = flow.FinalBossTalent; mid = flow.MidBossTalent;
        var enemy = new CombatantStats { maxHealth = 10000, currentHealth = 10000, attack = 1 };
        Call(flow, "BeginNormalBattle", enemy, 0, 0, EncounterType.NormalEnemy);
        float time = flow.mainTimeRemaining; yield return new WaitForSeconds(.25f);
        Check(flow.mainTimeRemaining < time && flow.CurrentPhase == GamePhase.NormalBattleRunning, "normal battle consumes main time");
        battle.CancelBattle(); Call(flow, "SetPhase", GamePhase.CaveRunning);
        flow.BeginCaveBattle(enemy.Clone(), 0, 0, null); time = flow.mainTimeRemaining;
        yield return new WaitForSeconds(.25f); Near(flow.mainTimeRemaining, time, "cave battle pauses main time");
        battle.CancelBattle(); Call(flow, "SetPhase", GamePhase.MainMapRunning); flow.ForceEnterMidBoss();
        Check(battle.CurrentBossTalent == mid, "mid boss receives announced talent");
        time = flow.mainTimeRemaining; yield return new WaitForSeconds(.25f);
        Near(flow.mainTimeRemaining, time, "mid boss pauses main timer");
        battle.CancelBattle(); Call(flow, "SetPhase", GamePhase.MainMapRunning); flow.bossIntroDuration = 0; flow.ForceEnterBoss();
        Check(battle.CurrentBossTalent == final, "final boss receives announced talent");
        float bossTime = flow.bossBattleTime; time = flow.mainTimeRemaining;
        yield return new WaitForSeconds(.25f);
        Near(flow.mainTimeRemaining, time, "final boss leaves main timer unchanged");
        Check(flow.bossBattleTime > bossTime, "final boss uses independent timer");
        // Resolve full fights through the actual combat coroutine; this is a controlled build fixture.
        battle.CancelBattle(); Call(flow, "SetPhase", GamePhase.Ready);
        for (int i = 1; i <= 5; i++)
        {
            Fixture((BossTalent)i); player.ApplyMartialArt("铁布衫"); player.ApplyMartialArt("金钟罩");
            player.ApplyMartialArt("反震诀"); player.ApplyMartialArt("剑气诀"); player.ApplyMartialArt("毒砂掌");
            var boss = flow.bossStats.Clone(); boss.bossTalent = (BossTalent)i;
            bool finished = false; battle.BeginBossBattle(boss, won => finished = true);
            float deadline = Time.realtimeSinceStartup + 60;
            while (!finished && Time.realtimeSinceStartup < deadline) yield return null;
            Check(finished, "actual full boss coroutine terminates: " + (BossTalent)i);
        }
    }
}
#endif
