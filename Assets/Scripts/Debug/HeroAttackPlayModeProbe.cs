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
public sealed partial class HeroAttackPlayModeProbe : MonoBehaviour
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
    private readonly int[] basicVariantCounts = new int[3];
    [Serializable] private class Report
    {
        public bool success;
        public string error;
        public string[] checks;
        public string[] runtimeErrors;
        public int[] basicVariantCounts;
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
            { success = error == null, error = error, checks = checks.ToArray(), runtimeErrors = errors.ToArray(),
              basicVariantCounts = basicVariantCounts }, true));
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
        Check(HeroExternalVfxArt.Frames(HeroAttackForm.Basic).Length == 0,
            "external atlas never adds another layer to baked basic attacks");
        foreach (HeroAttackForm form in new[] { HeroAttackForm.SwordQi, HeroAttackForm.VenomPalm, HeroAttackForm.BloodCleave })
        {
            Sprite[] external = HeroExternalVfxArt.Frames(form);
            Check(external.Length == 6, form + ": six approved external VFX frames load from Resources");
            for (int frame = 0; frame < external.Length; frame++)
            {
                Sprite sprite = external[frame];
                Check(sprite.rect.size == new Vector2(256, 256) && sprite.pivot == new Vector2(128, 128) &&
                    sprite.pixelsPerUnit == 256 && sprite.texture.width == 1536 && sprite.texture.height == 256 &&
                    sprite.texture.filterMode == FilterMode.Point && sprite.texture.wrapMode == TextureWrapMode.Clamp &&
                    sprite.texture.mipmapCount == 1, sprite.name + ": external atlas dimensions, center pivot, Point, Clamp, no mipmaps");
                Check(HeroExternalVfxArt.Frame(form, (frame + .5f) / 6f) == sprite,
                    form + ": authored external frame order " + frame);
            }
            Check(HeroExternalVfxArt.Frame(form, -0.01f) == null && HeroExternalVfxArt.Frame(form, 1f) == null,
                form + ": external animation never wraps before release or after recovery");
        }
        foreach (HeroAttackForm form in Enum.GetValues(typeof(HeroAttackForm)))
        {
            var frames = HeroAttackArt.Frames(form);
            Check(frames.Length == 8, form + ": eight build-safe Resources sprites");
            foreach (var frame in frames)
                Check(frame.rect.size == new Vector2(256,256) && frame.pivot == new Vector2(128,32) &&
                    frame.pixelsPerUnit == 160 && frame.texture.filterMode == FilterMode.Point,
                    frame.name + ": dimensions, foot pivot and pixel filtering");
        }
        for (int variant = 0; variant < HeroAttackArt.BasicVariantCount; variant++)
        {
            var frames = HeroAttackArt.BasicFrames(variant);
            Check(frames.Length == 8, "basic variant " + variant + ": eight Resources sprites");
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
                Check(form == HeroAttackForm.Basic ? screen.CurrentHeroBasicVariant >= 0 : screen.CurrentHeroBasicVariant == -1,
                    form + ": only ordinary attacks select a random basic variant");
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
                Check(screen.HeroSelfVfxCues == cues, form + ": enemy hit preserves actor-attached effect cues");
                if (form == HeroAttackForm.VenomPalm)
                {
                    Invoke(battle, "ApplyPoisonTick"); Invoke(screen, "TrackHeroAttack");
                    Check(battle.PlayerAttackVisualSequence == sequence && screen.CurrentHeroAttackForm == form,
                        "periodic poison does not retrigger or replace player animation");
                    Check(screen.ActiveHeroSkillVfxCount == vfxCount, "poison tick does not enqueue duplicate skill effect");
                    Check(screen.HeroSelfVfxCues == cues, "poison tick preserves actor-attached effect cues");
                }
                if (form != HeroAttackForm.Basic)
                {
                    PinHeroSample(screen, .15f);
                    Check(screen.ActiveHeroSelfVfxCount == 1, form + ": self effect follows windup");
                    yield return Capture("Self_" + form + "_charge" + (portrait ? "_portrait" : "_landscape"));
                    PinHeroSample(screen, 0f);
                }
                yield return new WaitForSecondsRealtime(HeroAttackArt.Duration(form) * .55f);
                // Editor compilation/tooling can stall a frame past a short swing.
                // Pin screenshot samples; expiry is checked separately below.
                PinHeroSample(screen, .55f);
                Check(screen.ActiveHeroSelfVfxCount == (form == HeroAttackForm.Basic ? 0 : 1),
                    form + ": self effect present only on active skill swing");
                yield return Capture(form + (portrait ? "_portrait" : "_landscape"));
                yield return new WaitForSecondsRealtime(.24f);
                yield return Capture(form + (portrait ? "_impact_portrait" : "_impact_landscape"));
                yield return new WaitForSecondsRealtime(.65f);
                Check(!screen.IsHeroAttackPlaying && screen.HeroAttackProgress == 1f, form + ": non-looping playback finishes");
                Check(screen.ActiveHeroSkillVfxCount == 0, form + ": independent effect expires");
                Check(screen.ActiveHeroSelfVfxCount == 0, form + ": actor-attached effect expires with pose");
                if (form == HeroAttackForm.Basic)
                    yield return CheckBasicVariants(screen, battle, player, portrait);
            }
            yield return CheckSelfSkillVariants(screen, battle, player, portrait);
        }
        Check(battle.UltimateVisualSequence == 0, "ordinary arts and rank-one capstones never trigger ultimate presentation");
        yield return CheckVfxCohesion(screen, battle, player);
        yield return CheckUltimates(screen, battle, player, enemy);
        Check(HeroAttackArt.Select(BattleVfxCue.BloodBurst | BattleVfxCue.SwordQi | BattleVfxCue.PoisonApplied) == HeroAttackForm.BloodCleave &&
              HeroAttackArt.Select(BattleVfxCue.SwordQi | BattleVfxCue.PoisonApplied) == HeroAttackForm.SwordQi,
              "mixed-build burst priority is stable");
        battle.CancelBattle(); Invoke(screen,"TrackHeroAttack");
        Check(!screen.IsHeroAttackPlaying && battle.PlayerAttackVisualSequence == 0 && screen.ActiveHeroSkillVfxCount == 0 &&
            screen.CurrentHeroBasicVariant == -1 && screen.ActiveHeroSelfVfxCount == 0 && screen.HeroSelfVfxCues == BattleVfxCue.None,
            "cancel clears pose and effect state");
        player.ResetRun(); player.runtimeStats.maxHealth=1000000;player.runtimeStats.currentHealth=1000000;
        player.runtimeStats.attackSpeed=10f;player.runtimeStats.attack=1f;
        F.mainTimeRemaining=60f;
        Invoke(F,"BeginNormalBattle",enemy.Clone(),0,0,EncounterType.NormalEnemy);
        float main=F.mainTimeRemaining;
        int peakEffects = 0;
        float observeUntil = Time.time + .6f;
        while (Time.time < observeUntil)
        {
            peakEffects = Mathf.Max(peakEffects, screen.ActiveHeroSkillVfxCount);
            yield return null;
        }
        Check(F.mainTimeRemaining<main && battle.PlayerAttackVisualSequence>=2, "real high-speed combat advances attacks and main timer");
        Check(peakEffects > 0 && peakEffects <= 3,
            "real high-speed sword effects appear and remain within fixed capacity");
        Check(screen.ActiveHeroSelfVfxCount <= 1, "high-speed casts never stack self effects on the actor");
        battle.StopAllCoroutines();
        yield return new WaitForSecondsRealtime(.2f);
        Check(screen.ActiveHeroSkillVfxCount == 0, "real high-speed attack stop leaves no old projectile tail");
        battle.CancelBattle();Invoke(F,"SetPhase",GamePhase.CaveRunning);
        F.BeginCaveBattle(enemy.Clone(),0,0,null);main=F.mainTimeRemaining;
        yield return new WaitForSeconds(.35f);
        Check(F.mainTimeRemaining==main && battle.PlayerAttackVisualSequence>0,"cave attacks animate while main timer pauses");
        battle.CancelBattle();F.bossIntroDuration=0f;F.bossStats=enemy.Clone();
        Invoke(F,"BeginBossBattle");main=F.mainTimeRemaining;
        yield return new WaitForSeconds(.35f);
        Check(F.CurrentPhase==GamePhase.BossBattle && F.bossBattleTime>0f && F.mainTimeRemaining==main,
            "final Boss advances independent timer");
        for(int rank=0;rank<3;rank++)player.ApplyMartialArt("无影连环剑");
        typeof(BattleManager).GetProperty("PlayerSuccessfulHits").SetValue(battle,2);
        battle.DebugSetBossHealthRatio(.65f);
        Invoke(battle,"DoAttack",player.runtimeStats,battle.currentEnemy);
        Invoke(screen,"TrackUltimate");
        Check(screen.IsUltimatePlaying && battle.LastBossSkill==BossSkillId.DemonArmor,
            "ultimate and actual Boss phase warning can coexist");
        yield return new WaitForSecondsRealtime(.44f);
        Check(battle.FinalBossActionElapsed>0f,"Boss warning advances naturally alongside ultimate presentation");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot("docs/validation/hero_ultimates/boss_warning_portrait.png");
        yield return null;
        battle.CancelBattle();
    }

    // Sample the pose and its emitted effect on the SAME clock. Pinning only the
    // pose produced misleading screenshots of a new swing with an old projectile.
    private static void PinHeroSample(BattleScreenController screen, float progress)
    {
        float duration = (float)screen.GetType().GetField("heroAttackDuration", Flags).GetValue(screen);
        float started = Time.time - duration * progress;
        Set(screen, "heroAttackStartedAt", started);
        var effects = (Array)screen.GetType().GetField("heroSkillEffects", Flags).GetValue(screen);
        int next = (int)screen.GetType().GetField("nextHeroSkillEffect", Flags).GetValue(screen);
        int index = (next + effects.Length - 1) % effects.Length;
        object effect = effects.GetValue(index);
        effect.GetType().GetField("startedAt").SetValue(effect, started + duration * .22f);
        effects.SetValue(effect, index);
    }

    private IEnumerator CheckVfxCohesion(BattleScreenController screen, BattleManager battle,
        WuxiaRoguelite.Player.PlayerStats player)
    {
        F.mainTimeRemaining = 600f;
        foreach (bool portrait in new[] { false, true })
        {
            Resize(portrait ? 540 : 960, portrait ? 960 : 540);
            yield return new WaitForSecondsRealtime(.2f);
            foreach (bool fast in new[] { false, true })
            foreach (HeroAttackForm form in new[] { HeroAttackForm.SwordQi, HeroAttackForm.VenomPalm, HeroAttackForm.BloodCleave })
            {
                player.ResetRun();
                if (player.equipment != null) player.equipment.Unequip(WuxiaRoguelite.Player.EquipmentSlot.Weapon);
                player.ApplyMartialArt(form == HeroAttackForm.SwordQi ? "剑气诀" :
                    form == HeroAttackForm.VenomPalm ? "毒砂掌" : "惊鸿一式");
                player.runtimeStats.maxHealth = 1000000; player.runtimeStats.currentHealth = 1000000;
                player.runtimeStats.attack = 1f; player.runtimeStats.critChance = 0f;
                typeof(BattleManager).GetProperty("PlayerSuccessfulHits").SetValue(battle, form == HeroAttackForm.SwordQi ? 2 : 0);
                typeof(BattleManager).GetProperty("PlayerAttackCooldownDuration").SetValue(battle, fast ? .10f : 1f);
                Invoke(screen, "ClearHeroSkillVfx");
                Invoke(battle, "DoAttack", player.runtimeStats, battle.currentEnemy);
                Invoke(screen, "TrackHeroAttack");
                string label = form + (fast ? "_fast" : "_normal") + (portrait ? "_portrait" : "_landscape");
                Check(screen.CurrentHeroAttackForm == form && screen.ActiveHeroSkillVfxCount == 1,
                    "cohesion actual skill trigger: " + label);
                foreach (float sample in new[] { .40f, .68f })
                {
                    PinHeroSample(screen, sample);
                    yield return Capture("Cohesion_" + label + (sample < .5f ? "_flight" : "_hit"));
                }
                // Resume from a fresh release and observe real elapsed time, without pinning expiry.
                PinHeroSample(screen, .24f);
                float poseDuration = (float)screen.GetType().GetField("heroAttackDuration", Flags).GetValue(screen);
                yield return new WaitForSecondsRealtime(poseDuration);
                Check(!screen.IsHeroAttackPlaying && screen.ActiveHeroSkillVfxCount == 0,
                    "cohesion no effect left after recovery: " + label);
            }
        }
    }

    private IEnumerator CheckSelfSkillVariants(BattleScreenController screen, BattleManager battle,
        WuxiaRoguelite.Player.PlayerStats player, bool portrait)
    {
        foreach (bool armor in new[] { true, false })
        {
            player.ResetRun();
            if (player.equipment != null) player.equipment.Unequip(WuxiaRoguelite.Player.EquipmentSlot.Weapon);
            player.ApplyMartialArt(armor ? "破甲掌" : "无影连环剑");
            player.runtimeStats.maxHealth=1000000; player.runtimeStats.currentHealth=1000000;
            player.runtimeStats.attack=1f; player.runtimeStats.critChance=0f;
            // The combo requires its learned rank's interval; use actual resolver state.
            typeof(BattleManager).GetProperty("PlayerSuccessfulHits").SetValue(battle,
                armor ? 0 : 5-player.GetMartialArtRank("无影连环剑"));
            Invoke(battle,"DoAttack",player.runtimeStats,battle.currentEnemy);
            Invoke(screen,"TrackHeroAttack");
            BattleVfxCue cue=armor ? BattleVfxCue.ArmorBreak : BattleVfxCue.SwiftCombo;
            Check((screen.HeroSelfVfxCues & cue) != 0, "self cue from actual skill resolver: " + cue);
            Color color=(Color)Invoke(screen,"HeroSelfColor");
            Check(armor ? color.r > color.g && color.g > color.b : color.b > color.r,
                "self effect palette matches " + cue);
            yield return new WaitForSecondsRealtime(HeroAttackArt.Duration(screen.CurrentHeroAttackForm)*.52f);
            PinHeroSample(screen, .52f);
            Check(screen.ActiveHeroSelfVfxCount == 1,"special self variant active: " + cue);
            yield return Capture("Self_" + cue + (portrait ? "_portrait" : "_landscape"));
            yield return new WaitForSecondsRealtime(.8f);
            Check(screen.ActiveHeroSelfVfxCount == 0,"special self variant expires: " + cue);
        }
    }

    private IEnumerator CheckBasicVariants(BattleScreenController screen, BattleManager battle,
        WuxiaRoguelite.Player.PlayerStats player, bool portrait)
    {
        // Starter sword adds SwordQi on every third hit; isolate ordinary attacks here.
        // The main suite resets equipment and separately verifies that skill priority.
        if (player.equipment != null) player.equipment.Unequip(WuxiaRoguelite.Player.EquipmentSlot.Weapon);
        var seen = new bool[HeroAttackArt.BasicVariantCount];
        bool stable = true, independent = true, correctlyBound = true;
        for (int sample = 0; sample < 120; sample++)
        {
            Invoke(battle, "DoAttack", player.runtimeStats, battle.currentEnemy);
            string randomBefore = JsonUtility.ToJson(UnityEngine.Random.state);
            Invoke(screen, "TrackHeroAttack");
            independent &= randomBefore == JsonUtility.ToJson(UnityEngine.Random.state);
            int variant = screen.CurrentHeroBasicVariant;
            Check(variant >= 0 && variant < HeroAttackArt.BasicVariantCount, "basic random index in range " + portrait + "/" + sample);
            basicVariantCounts[variant]++;
            var frames = (Sprite[])Invoke(screen, "CurrentHeroAttackFrames");
            correctlyBound &= ReferenceEquals(frames, HeroAttackArt.BasicFrames(variant));
            // OnGUI and Update may both observe the same event; neither may reroll it.
            for (int repeat = 0; repeat < 4; repeat++)
            {
                Invoke(screen, "TrackHeroAttack");
                stable &= variant == screen.CurrentHeroBasicVariant &&
                    ReferenceEquals(frames, Invoke(screen, "CurrentHeroAttackFrames"));
            }
            if (!seen[variant])
            {
                seen[variant] = true;
                yield return new WaitForSecondsRealtime(.26f);
                yield return Capture("BasicVariant_" + variant + (portrait ? "_portrait" : "_landscape"));
            }
        }
        Check(Array.TrueForAll(seen, value => value), "all three ordinary attacks observed " + portrait);
        Check(independent, "cosmetic random selection preserves gameplay random state " + portrait);
        Check(stable && correctlyBound, "selected strip stays bound for the entire attack " + portrait);
        yield return new WaitForSecondsRealtime(.60f);
        Check(!screen.IsHeroAttackPlaying && screen.ActiveHeroSkillVfxCount == 0,
            "random basic attack ends without a persistent skill effect " + portrait);
    }
}
#endif
