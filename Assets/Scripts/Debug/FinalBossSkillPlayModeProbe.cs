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

/// <summary>Controlled integration evidence for the actual final boss coroutine and rendering.</summary>
public sealed class FinalBossSkillPlayModeProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.FinalBossSkillProbe";
    private const string Output = "docs/validation/final_boss_skill_pack_2026-09-06";
    private GameFlowController flow;
    private BattleManager battle;
    private PlayerStats player;
    private BattleScreenController screen;
    private IEnumerator suite;
    private readonly List<string> passed = new List<string>();
    private readonly List<float> hitTimes = new List<float>();
    private bool originalBackground;
    private float originalTimeScale;
    private UnityEngine.Random.State randomState;
    private string orientation;

    [Serializable] private class Report
    {
        public bool success;
        public string error;
        public string orientation;
        public string[] passed;
        public float[] impactTimes;
        public string scope = "MainPrototype Play Mode controlled fixtures; not natural-build balance or device approval";
    }

    [MenuItem("37 MiniGame/Validate Final Boss Skill Pack")]
    private static void Queue()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/MainPrototype.unity")
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
        new GameObject("FinalBossSkillPlayModeProbe").AddComponent<FinalBossSkillPlayModeProbe>();
    }
    private void Start()
    {
        flow = FindAnyObjectByType<GameFlowController>();
        battle = flow.battleManager;
        player = flow.playerStats;
        screen = FindAnyObjectByType<BattleScreenController>();
        orientation = Screen.width > Screen.height ? "landscape" : "portrait";
        originalBackground = Application.runInBackground;
        originalTimeScale = Time.timeScale;
        randomState = UnityEngine.Random.state;
        Application.runInBackground = true;
        UnityEngine.Random.InitState(90631);
        Directory.CreateDirectory(Output);
        suite = RunSuite();
        StartCoroutine(Drive());
    }
    private IEnumerator Drive()
    {
        while (true)
        {
            bool moved = false;
            object next = null;
            string error = null;
            try { moved = suite.MoveNext(); if (moved) next = suite.Current; }
            catch (Exception ex) { error = ex.ToString(); }
            if (error != null || !moved)
            {
                File.WriteAllText(Output + "/" + orientation + ".json", JsonUtility.ToJson(new Report
                { success = error == null, error = error, orientation = orientation, passed = passed.ToArray(), impactTimes = hitTimes.ToArray() }, true));
                battle.CancelBattle();
                Application.runInBackground = originalBackground;
                UnityEngine.Random.state = randomState;
                Time.timeScale = originalTimeScale;
                Debug.Log("FinalBossSkillProbe " + (error == null ? "PASS" : "FAIL: " + error));
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
    private static object Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    private void Set(string property, object value) => typeof(BattleManager).GetProperty(property).SetValue(battle, value);
    private void JumpPhase(float ratio)
    {
        battle.DebugSetBossHealthRatio(ratio);
        // A fixture health jump is not combat damage. Keep screenshots free of its synthetic huge popup.
        typeof(BattleScreenController).GetField("previousEnemyHealth", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(screen, battle.currentEnemy.currentHealth);
    }
    private void Capture(string name) => ScreenCapture.CaptureScreenshot(Output + "/" + orientation + "_" + name + ".png");

    private void FreshMap()
    {
        Time.timeScale = 1f;
        flow.StartRun();
        for (int i=0; i<12 && flow.IsOpeningIntroActive; i++) flow.AdvanceOpeningIntro();
        for (int i=0; i<8 && flow.CurrentPhase == GamePhase.LevelUpPaused; i++) flow.ChooseMartialArt(0);
        player.ResetRun();
        player.runtimeStats.maxHealth = player.runtimeStats.currentHealth = 1000f;
        player.runtimeStats.attack = 1f;
        player.runtimeStats.defense = 3f;
        player.runtimeStats.dodgeChance = player.runtimeStats.critChance = 0f;
        flow.mainTimeRemaining = 60f;
        if (flow.CurrentPhase != GamePhase.MainMapRunning) throw new Exception("FreshMap failed");
    }
    private void FreshBoss()
    {
        FreshMap();
        flow.bossIntroDuration = 0f;
        flow.ForceEnterBoss();
        Set("PlayerAttackCooldownRemaining", 100f);
        Set("EnemyAttackCooldownRemaining", 100f);
        Set("BossSkillCooldownRemaining", 100f);
        Set("PlayerShield", 0f);
        battle.battleSpeedMultiplier = 1.5f;
    }
    private IEnumerator RunSuite()
    {
        yield return null;
        var profile = Array.Find(screen.enemyVisualProfiles, p => p.id == GameTextCatalog.FinalBossVisualId);
        Check(profile != null && profile.flipHorizontally && profile.foxfireFrames.Length == 8 &&
            profile.demonArmorFrames.Length == 8 && profile.bloodFrenzyFrames.Length == 8,
            "three eight-frame action bindings with right masters flipped toward player");
        Check(screen.foxfireEffectFrames.Length == 6 && screen.demonArmorEffectFrames.Length == 6 &&
            screen.bloodFrenzyEffectFrames.Length == 6, "three six-frame effect bindings");
        foreach (Sprite[] frames in new[] { profile.foxfireFrames, profile.demonArmorFrames, profile.bloodFrenzyFrames })
            foreach (Sprite sprite in frames)
                if (sprite == null || sprite.rect.size != new Vector2(256,256) || sprite.pivot != new Vector2(128,32))
                    throw new Exception("Invalid character slice or foot pivot");
        Check(true, "24 character sprites: 256-square and bottom-center foot pivot");

        FreshBoss();
        float hp = player.runtimeStats.currentHealth;
        float expectedHit = Mathf.Max(1f, battle.currentEnemy.attack * 0.32f - player.runtimeStats.defense * 0.42f);
        Invoke(battle, "CastFoxfireBarrage");
        Check(battle.IsFinalBossActionActive && battle.FoxfireImpactSequence == 0, "cast starts windup without immediate damage");
        yield return new WaitForSeconds(0.33f);
        Capture("foxfire_flight");
        Check(player.runtimeStats.currentHealth == hp, "no damage before first projectile arrival");
        float action = battle.FinalBossActionElapsed;
        float bossTime = flow.bossBattleTime;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.22f);
        Check(Mathf.Abs(action-battle.FinalBossActionElapsed)<0.001f && Mathf.Abs(bossTime-flow.bossBattleTime)<0.001f &&
            battle.FoxfireImpactSequence == 0, "pause freezes action, pending hits and independent boss time");
        Time.timeScale = 1f;
        int lastSequence = 0;
        float deadline = Time.time + 1.4f;
        while (Time.time < deadline && battle.IsFinalBossActionActive)
        {
            if (battle.FoxfireImpactSequence != lastSequence)
            {
                lastSequence = battle.FoxfireImpactSequence;
                hitTimes.Add(battle.FinalBossActionElapsed);
                if (lastSequence == 2) Capture("foxfire_impact");
            }
            yield return null;
        }
        Check(hitTimes.Count == 3 && Mathf.Abs(hitTimes[0]-0.45f)<0.07f && Mathf.Abs(hitTimes[1]-0.65f)<0.07f &&
            Mathf.Abs(hitTimes[2]-0.85f)<0.07f, "three separate impacts at 0.45 / 0.65 / 0.85 seconds");
        Check(Mathf.Abs(hp-player.runtimeStats.currentHealth-expectedHit*3f)<0.02f,
            "original 32-percent attack / 42-percent defense formula preserved for each hit");
        Check(flow.mainTimeRemaining == 0f && flow.bossBattleTime > 1f, "final boss advances independent timer, not main time");
        Check((Sprite[])Invoke(screen,"SelectFinalBossActionFrames", profile, profile.attackFrames) != null,
            "action renderer resolves its bound sprite arrays");

        // Ordinary attacks must not override either phase or offensive action presentation.
        JumpPhase(0.70f);
        Check(battle.CurrentBossPhase == BossBattlePhase.DemonArmor && battle.CurrentFinalBossAction == BossSkillId.DemonArmor &&
            Mathf.Abs(battle.BossWard-battle.currentEnemy.maxHealth*0.12f)<0.01f, "70-percent phase grants 12-percent armor and starts tail-guard action");
        yield return new WaitForSeconds(0.35f);
        Capture("demon_armor");
        yield return null; // CaptureScreenshot samples end-of-frame, before the explicit break below.
        int playerHitsBefore = battle.PlayerSuccessfulHits;
        Set("PlayerAttackCooldownRemaining", 0f);
        yield return new WaitForSeconds(0.10f);
        Check(battle.CurrentFinalBossAction == BossSkillId.DemonArmor && battle.PlayerSuccessfulHits > playerHitsBefore,
            "player attacks continue without interrupting phase action");
        Set("PlayerAttackCooldownRemaining", 100f);
        float ward = battle.BossWard;
        int phaseSequence = battle.BossSkillSequence;
        JumpPhase(0.65f);
        Check(battle.BossWard == ward && battle.BossSkillSequence == phaseSequence, "armor phase is one-shot");
        hp = battle.currentEnemy.currentHealth;
        Invoke(battle,"ApplyDamageToCurrentEnemy",ward+2f);
        Check(battle.BossWard == 0f && Mathf.Abs(hp-battle.currentEnemy.currentHealth-2f)<0.01f && battle.FinalBossWardBreakAge == 0f,
            "all-damage path cracks armor and carries excess damage to health");
        yield return new WaitForSeconds(0.05f);
        Capture("armor_break");
        yield return new WaitForSeconds(0.45f);
        float attack = battle.currentEnemy.attack;
        float attackSpeed = battle.currentEnemy.attackSpeed;
        JumpPhase(0.35f);
        Check(battle.CurrentBossPhase == BossBattlePhase.BloodFrenzy &&
            Mathf.Abs(battle.currentEnemy.attack-attack*1.15f)<0.001f &&
            Mathf.Abs(battle.currentEnemy.attackSpeed-attackSpeed*1.25f)<0.001f,
            "35-percent frenzy preserves 15-percent attack and 25-percent attack-speed increases");
        yield return new WaitForSeconds(0.43f);
        Capture("blood_frenzy");
        yield return new WaitForSeconds(0.50f);
        Capture("frenzy_sustained");
        Check(!battle.IsFinalBossActionActive && battle.CurrentBossPhase == BossBattlePhase.BloodFrenzy,
            "frenzy cast recovers to idle while phase persists");

        FreshBoss(); Invoke(battle,"CastFoxfireBarrage");
        JumpPhase(0.70f);
        JumpPhase(0.35f);
        Check(battle.CurrentFinalBossAction == BossSkillId.FoxfireBarrage && battle.CurrentBossPhase == BossBattlePhase.BloodFrenzy,
            "crossing both thresholds during cast applies stats immediately and queues presentation");
        yield return new WaitForSeconds(1.22f);
        Check(battle.CurrentFinalBossAction == BossSkillId.DemonArmor && battle.FoxfireImpactSequence == 3,
            "queued armor starts after all three foxfire hits");
        yield return new WaitForSeconds(0.82f);
        Check(battle.CurrentFinalBossAction == BossSkillId.BloodFrenzy, "queued frenzy follows armor without losing phase action");
        battle.CancelBattle();
        Check(!battle.IsFinalBossActionActive && battle.FoxfireImpactSequence == 0 && battle.BossWard == 0f &&
            battle.FinalBossWardBreakAge >= 100f, "cancel resets action, queue, armor and impact effects");

        FreshBoss(); player.runtimeStats.dodgeChance = 1f; hp = player.runtimeStats.currentHealth;
        Invoke(battle,"CastFoxfireBarrage");
        yield return new WaitForSeconds(0.92f);
        Check(battle.FoxfireImpactSequence == 3 && battle.EnemyAttackAttempts == 3 && player.runtimeStats.currentHealth == hp &&
            battle.FoxfireImpactDodged, "each foxfire hit independently honors evasion and records miss VFX");

        FreshBoss(); Set("PlayerShield", 100f); hp = player.runtimeStats.currentHealth;
        Invoke(battle,"CastFoxfireBarrage"); yield return new WaitForSeconds(0.92f);
        Check(player.runtimeStats.currentHealth == hp && battle.PlayerShield < 100f && battle.FoxfireImpactSequence == 3,
            "each foxfire impact consumes shield before health");

        FreshBoss(); player.runtimeStats.currentHealth = 1f;
        Invoke(battle,"CastFoxfireBarrage");
        int lethalHits = 0;
        deadline = Time.time + 2f;
        while (Time.time < deadline && flow.CurrentPhase != GamePhase.Result)
        {
            lethalHits = Mathf.Max(lethalHits, battle.FoxfireImpactSequence);
            yield return null;
        }
        Check(lethalHits == 1 && player.runtimeStats.IsDead && flow.CurrentPhase == GamePhase.Result,
            "lethal first impact cancels remaining hits and resolves defeat");
        FreshBoss(); Invoke(battle,"CastFoxfireBarrage"); battle.currentEnemy.currentHealth = 0f;
        yield return new WaitForSeconds(0.65f);
        Check(battle.FoxfireImpactSequence == 0 && flow.CurrentPhase == GamePhase.Result,
            "boss death during windup cancels projectiles and resolves victory");

        FreshBoss(); Set("BossSkillCooldownRemaining", 0.05f);
        yield return new WaitForSeconds(0.10f);
        Check(battle.CurrentFinalBossAction == BossSkillId.FoxfireBarrage && battle.BossSkillSequence == 1 &&
            Mathf.Abs(battle.BossSkillCooldownDuration-5.8f)<0.001f, "natural cooldown starts new cast and preserves phase-one cooldown");

        FreshMap(); var enemy = flow.bossStats.Clone(); enemy.attack=1f;
        Invoke(flow,"BeginNormalBattle",enemy,0,0,EncounterType.NormalEnemy);
        float main = flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.30f);
        Check(flow.CurrentPhase == GamePhase.NormalBattleRunning && flow.mainTimeRemaining < main &&
            battle.CurrentFinalBossAction == BossSkillId.None, "normal combat consumes main time without final-boss actions");
        battle.CancelBattle(); Invoke(flow,"SetPhase",GamePhase.CaveRunning);
        flow.BeginCaveBattle(enemy.Clone(),0,0,null); main=flow.mainTimeRemaining;
        yield return new WaitForSeconds(0.30f);
        Check(flow.CurrentPhase == GamePhase.CaveRunning && Mathf.Abs(flow.mainTimeRemaining-main)<0.001f,
            "cave combat pauses main time");
    }
}
#endif
