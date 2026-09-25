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
public sealed class CombatVfxAuditProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.CombatVfxAudit";
    private static string Output => "docs/validation/combat_vfx_audit/" + SessionState.GetString(Key + ".Pass", "after");
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
    private readonly List<string> findings = new List<string>();
    [Serializable] private class Report
    {
        public bool success;
        public string error;
        public string[] checks;
        public string[] runtimeErrors;
        public string[] findings; public string capturedAt;
        public string scope = "Unity Editor Play Mode with controlled inputs, collision isolation and combat fixtures. Device/touch feel and final art approval remain separate.";
    }

    [MenuItem("37 MiniGame/Validate All Combat VFX Play Mode")]
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
        var go = new GameObject("Combat VFX audit");
        DontDestroyOnLoad(go);
        go.AddComponent<CombatVfxAuditProbe>();
    }
    private void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); }
    private IEnumerator Start()
    {
        UnityEngine.Random.InitState(9252026);
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
        if (error == null && findings.Count > 0) error = "Visual regression findings remain.";
        if (error == null && errors.Count > 0) error = "Runtime console contains errors.";
        File.WriteAllText(Output + "/playmode_report.json", JsonUtility.ToJson(new Report
            { success = error == null && findings.Count == 0, error = error, checks = checks.ToArray(), runtimeErrors = errors.ToArray(),
              findings = findings.ToArray(), capturedAt = DateTime.UtcNow.ToString("O") }, true));
        Restore();
        Debug.Log("COMBAT_VFX_AUDIT_" + (error == null ? "PASS" : "FAIL: " + error));
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
    private BattleScreenController screen;
    private BattleManager B => F.battleManager;
    private void Property(string name, object value) => typeof(BattleManager).GetProperty(name).SetValue(B, value);
    private void Observe(bool ok, string note) { if (ok) checks.Add(note); else findings.Add(note); }
    private void Fresh(EnemyTrait trait = EnemyTrait.None, string visual = "strawhat_bandit")
    {
        Time.timeScale = 1;
        B.CancelBattle();
        F.playerStats.ResetRun();
        if (F.playerStats.equipment != null) F.playerStats.equipment.Unequip(WuxiaRoguelite.Player.EquipmentSlot.Weapon);
        var p = F.playerStats.runtimeStats;
        p.maxHealth = p.currentHealth = 2000; p.attack = 10; p.critChance = p.dodgeChance = 0;
        Invoke(F, "SetPhase", GamePhase.MainMapRunning); F.mainTimeRemaining = 600;
        Invoke(F, "BeginNormalBattle", new CombatantStats { displayName = "VFX audit", visualId = visual,
            enemyTrait = trait, maxHealth = 2000, currentHealth = 2000, attack = 25, defense = 2, attackSpeed = .2f },
            0, 0, EncounterType.NormalEnemy);
        B.StopAllCoroutines();
        Invoke(screen, "TrackHeroAttack"); Invoke(screen, "TrackUltimate"); Invoke(screen, "TrackLatestAttack");
        Invoke(screen, "TrackHealthChanges"); Invoke(screen, "TrackEnemyTraitFeedback");
        Set(screen, "playerDamageAmount", 0f); Set(screen, "enemyDamageAmount", 0f);
    }
    private void Attack(bool player = true)
    {
        Invoke(B, "DoAttack", player ? F.playerStats.runtimeStats : B.currentEnemy,
            player ? B.currentEnemy : F.playerStats.runtimeStats);
        Invoke(screen, "TrackHeroAttack"); Invoke(screen, "TrackUltimate");
    }
    private IEnumerator Sequence(string name, float[] times)
    {
        float previous = 0;
        foreach (float time in times)
        {
            yield return new WaitForSeconds(time - previous); previous = time;
            yield return Capture(name + "_" + Mathf.RoundToInt(time * 1000));
        }
    }
    private IEnumerator Suite()
    {
        yield return new WaitForSecondsRealtime(3.2f);
        screen = FindAnyObjectByType<BattleScreenController>();
        F.playerController.enabled = false;
        foreach (var e in FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
        foreach (bool portrait in new[] { false, true })
        {
            Resize(portrait ? 540 : 960, portrait ? 960 : 540);
            yield return new WaitForSecondsRealtime(.2f);
            string suffix = portrait ? "_portrait" : "_landscape";
            Check(Screen.width == (portrait ? 540 : 960), "actual viewport " + suffix);
            Fresh();
            F.playerStats.ApplyMartialArt("剑气诀"); Property("PlayerSuccessfulHits", 2);
            Property("PlayerAttackCooldownDuration", 1f); Attack();
            yield return new WaitForSeconds(.15f);
            Time.timeScale = 0;
            float pose = screen.HeroAttackProgress;
            int external = screen.ActiveHeroSkillVfxCount;
            yield return Capture("pause_sword_start" + suffix);
            yield return new WaitForSecondsRealtime(.7f);
            Observe(Mathf.Abs(pose - screen.HeroAttackProgress) < .001f && external == screen.ActiveHeroSkillVfxCount,
                "pause freezes hero pose and emitted sword" + suffix);
            yield return Capture("pause_sword_end" + suffix);
            Time.timeScale = 1;
            yield return new WaitForSeconds(.6f);
            Check(!screen.IsHeroAttackPlaying && screen.ActiveHeroSkillVfxCount == 0, "resume completes sword" + suffix);

            Fresh();
            for (int i=0;i<3;i++) F.playerStats.ApplyMartialArt("无影连环剑");
            Property("PlayerSuccessfulHits", 2); Attack();
            yield return new WaitForSeconds(.28f); Time.timeScale = 0;
            float ultimate = screen.UltimateProgress;
            yield return Capture("pause_ultimate_start" + suffix);
            yield return new WaitForSecondsRealtime(4.1f);
            Observe(Mathf.Abs(ultimate - screen.UltimateProgress) < .001f, "pause freezes ultimate pose and backdrop" + suffix);
            yield return Capture("pause_ultimate_end" + suffix); Time.timeScale = 1;
            int ultimateSequence = B.UltimateVisualSequence;
            Invoke(B, "RegisterMartialArtActivation", "无影连环剑");
            Check(B.UltimateVisualSequence == ultimateSequence, "pause cannot spend ultimate visual cooldown" + suffix);

            Fresh(); F.playerStats.ApplyMartialArt("毒砂掌"); F.playerStats.ApplyMartialArt("化功毒雾");
            for (int i=0;i<4;i++) Attack();
            Invoke(B, "ApplyPoisonTick");
            Check((B.LastVfxCues & BattleVfxCue.PoisonTick) != 0 && B.EnemyPoisonStacks > 0, "real poison tick and persistent stacks " + suffix);
            yield return Sequence("poison_tick" + suffix, new[]{ .08f,.20f,.40f,.65f });
            Fresh(); F.playerStats.ApplyMartialArt("金钟罩"); F.playerStats.ApplyMartialArt("反震诀");
            Property("PlayerShield", 80f); Attack(false);
            Check((B.LastVfxCues & (BattleVfxCue.ShieldImpact | BattleVfxCue.Retaliation)) == (BattleVfxCue.ShieldImpact | BattleVfxCue.Retaliation), "shield absorption and retaliation hooks " + suffix);
            yield return Sequence("shield_retaliation" + suffix, new[]{ .08f,.20f,.40f });
            Fresh(); F.playerStats.runtimeStats.dodgeChance = 1; Attack(false);
            Check((B.LastVfxCues & BattleVfxCue.Dodge) != 0, "real dodge hook " + suffix);
            yield return Sequence("dodge" + suffix, new[]{ .08f,.20f });
            Fresh(); F.playerStats.ApplyMartialArt("血战八方"); F.playerStats.runtimeStats.currentHealth = 600;
            Set(screen, "previousPlayerHealth", F.playerStats.runtimeStats.currentHealth);
            F.playerStats.runtimeStats.lifeSteal = .3f; F.playerStats.runtimeStats.critChance = 1;
            Attack(); Check((B.LastVfxCues & (BattleVfxCue.Heal | BattleVfxCue.CriticalHit)) == (BattleVfxCue.Heal | BattleVfxCue.CriticalHit), "healing and critical hooks " + suffix); yield return Sequence("blood_heal_crit" + suffix, new[]{ .08f,.20f,.4f });

            foreach (EnemyTrait trait in new[]{EnemyTrait.HeavyOpening, EnemyTrait.VenomBite, EnemyTrait.OpeningArmor})
            {
                Fresh(trait, trait == EnemyTrait.HeavyOpening ? "iron_tusk_boar" : trait == EnemyTrait.VenomBite ? "scarlet_viper" : "clockwork_guard");
                Property("EnemyAttackCooldownRemaining", .20f);
                yield return Capture(trait + "_ready" + suffix);
                if (trait == EnemyTrait.OpeningArmor) Invoke(B, "ApplyDamageToCurrentEnemy", B.EnemyOpeningArmor + 1);
                else Attack(false);
                Check(trait == EnemyTrait.HeavyOpening ? B.EnemyAttackAttempts == 1 :
                    trait == EnemyTrait.VenomBite ? B.PlayerVenomTicks > 0 : B.OpeningArmorBreakSequence == 1,
                    "enemy trait real activation " + trait + suffix);
                yield return Sequence(trait + "_release" + suffix, new[]{ .08f,.25f,.5f });
            }
            Fresh(); B.CancelBattle(); Invoke(F,"SetPhase", GamePhase.MidBossBattle);
            B.BeginMidBossBattle(F.midBossStats.Clone(), null); B.battleSpeedMultiplier = 1;
            Property("PlayerAttackCooldownRemaining", 100f); Property("EnemyAttackCooldownRemaining", 100f);
            Property("BossSkillCooldownRemaining", 100f);
            foreach (BossSkillId skill in new[]{BossSkillId.MountainBreaker, BossSkillId.DoubleCleave, BossSkillId.IronGuard})
            {
                Invoke(B, "BeginMidBossSkill", skill);
                yield return Sequence("mid_" + skill + suffix, skill == BossSkillId.DoubleCleave ? new[]{ .2f,.4f,.85f,1.05f } : new[]{ .2f,.52f,.68f,.85f });
                Check(skill == BossSkillId.IronGuard ? B.BossWard > 0 : B.MidBossImpactSkill == skill,
                    "actual midboss skill " + skill + suffix);
                if (skill == BossSkillId.IronGuard)
                { Invoke(B,"ApplyDamageToCurrentEnemy", B.BossWard + 1); yield return Sequence("mid_break" + suffix,new[]{ .08f,.2f }); }
                yield return new WaitForSeconds(.5f);
            }
            Fresh(); B.CancelBattle(); Invoke(F,"SetPhase", GamePhase.BossBattle);
            B.BeginBossBattle(F.bossStats.Clone(), null); B.battleSpeedMultiplier = 1;
            Property("PlayerAttackCooldownRemaining", 100f); Property("EnemyAttackCooldownRemaining", 100f);
            Property("BossSkillCooldownRemaining", 100f);
            Invoke(B,"CastFoxfireBarrage");
            yield return Sequence("fox_fire" + suffix,new[]{ .30f,.46f,.67f,.86f,1.05f });
            Check(B.FoxfireImpactSequence == 3, "three timed foxfire impacts " + suffix);
            yield return new WaitForSeconds(.3f);
            B.DebugSetBossHealthRatio(.70f); Set(screen,"previousEnemyHealth",B.currentEnemy.currentHealth);
            yield return Sequence("fox_armor" + suffix,new[]{ .2f,.5f,.85f });
            Invoke(B,"ApplyDamageToCurrentEnemy",B.BossWard + 1);
            yield return Sequence("fox_break" + suffix,new[]{ .08f,.2f });
            B.DebugSetBossHealthRatio(.35f); Set(screen,"previousEnemyHealth",B.currentEnemy.currentHealth);
            yield return Sequence("fox_frenzy" + suffix,new[]{ .2f,.5f,.9f });
            Check(B.CurrentBossPhase == BossBattlePhase.BloodFrenzy, "real boss phase transition " + suffix);
            B.CancelBattle(); Invoke(screen, "TrackHeroAttack"); Invoke(screen, "TrackUltimate");
            Check(screen.ActiveHeroSkillVfxCount == 0 && !screen.IsUltimatePlaying && !B.IsFinalBossActionActive,
                "cancel clears all combat presentations " + suffix);

            // Actual mixed build against a moving boss: ordinary attacks cannot restart the ultimate.
            Fresh(); B.CancelBattle();
            foreach (string id in new[]{"无影连环剑", "毒砂掌", "化功毒雾", "金钟罩", "反震诀"})
                for (int i=0;i<3;i++) F.playerStats.ApplyMartialArt(id);
            F.playerStats.runtimeStats.attack = 1; F.playerStats.runtimeStats.attackSpeed = 10;
            F.playerStats.runtimeStats.maxHealth = F.playerStats.runtimeStats.currentHealth = 2000;
            var durableBoss = F.bossStats.Clone(); durableBoss.maxHealth = durableBoss.currentHealth = 20000;
            Invoke(F,"SetPhase",GamePhase.BossBattle); B.BeginBossBattle(durableBoss,null);
            B.battleSpeedMultiplier = 1.5f; B.DebugSetBossHealthRatio(.65f);
            float until = Time.time + 3f; int peak = 0; bool sawUltimate = false, sawPoison = false;
            int shot = 0;
            while (Time.time < until)
            {
                peak = Mathf.Max(peak, screen.ActiveHeroSkillVfxCount);
                sawUltimate |= screen.IsUltimatePlaying; sawPoison |= B.EnemyPoisonStacks > 0;
                if (shot < 3 && Time.time > until - 2.7f + shot * .75f)
                { yield return Capture("mixed_fast" + suffix + "_" + shot); shot++; }
                yield return null;
            }
            Check(sawUltimate && sawPoison && B.PlayerAttackVisualSequence > 8 && peak <= 3,
                "real fast mixed build: poison, ultimate, bounded effects, attacks continue " + suffix);
            B.CancelBattle();
        }
        // Actual movement with a temporary speed buff; runtime-only collider isolation.
        Invoke(F,"SetPhase",GamePhase.MainMapRunning); F.mainTimeRemaining = 600;
        foreach (var input in FindObjectsByType<MobileInputController>(FindObjectsSortMode.None)) input.enabled = false;
        F.playerController.enabled = true; F.playerController.SetMovementEnabled(true);
        F.playerStats.ApplyTemporaryMoveSpeedBuff(.25f, 8f);
        var trail = F.playerController.GetComponent<WuxiaRoguelite.Player.PlayerSpeedBoostVfx>();
        foreach (bool portrait in new[]{false,true})
        {
            F.playerStats.ClearTemporaryMoveSpeedBuffs(); F.playerStats.ApplyTemporaryMoveSpeedBuff(.25f, 3f);
            Resize(portrait ? 540:960, portrait ? 960:540);
            InputMove(Vector2.up); yield return new WaitForSeconds(.6f);
            Check(trail.IsEffectEmitting,"speed buff emits while moving " + portrait);
            yield return Capture("map_trail_" + (portrait ? "portrait":"landscape"));
            InputMove(Vector2.zero); yield return new WaitForSeconds(.8f);
            Check(!trail.IsEffectEmitting,"trail stops on idle " + portrait);
        }
    }
}
#endif
