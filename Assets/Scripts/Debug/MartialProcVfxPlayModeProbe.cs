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
public sealed class MartialProcVfxPlayModeProbe : MonoBehaviour
{
    private const string Key = "37MiniGame.MartialProcVfx";
    private const string Output = "docs/validation/martial_proc_vfx/playmode";
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

    [MenuItem("37 MiniGame/Validate Martial Proc VFX Play Mode")]
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
        var go = new GameObject("Martial proc VFX validation");
        DontDestroyOnLoad(go);
        go.AddComponent<MartialProcVfxPlayModeProbe>();
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
        Debug.Log("MARTIAL_PROC_VFX_" + (error == null ? "PASS" : "FAIL: " + error));
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
        B.battleSpeedMultiplier = 1;
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
        Invoke(screen, "TrackMartialProcs");
        Set(screen, "playerDamageAmount", 0f); Set(screen, "enemyDamageAmount", 0f);
    }
    private void Attack(bool player = true)
    {
        Invoke(B, "DoAttack", player ? F.playerStats.runtimeStats : B.currentEnemy,
            player ? B.currentEnemy : F.playerStats.runtimeStats);
        Invoke(screen, "TrackHeroAttack"); Invoke(screen, "TrackUltimate");
        Invoke(screen, "TrackMartialProcs");
    }
    private IEnumerator Sequence(string name, float[] times)
    {
        float start = Time.time;
        foreach (float time in times)
        {
            while (Time.time - start < time) yield return null;
            yield return Capture(name + "_" + Mathf.RoundToInt(time * 1000));
        }
    }
    private bool Has(MartialProcKind kind)
    {
        for (int i = Mathf.Max(1, B.MartialProcSequence - BattleManager.MartialProcCapacity + 1); i <= B.MartialProcSequence; i++)
            if (B.TryGetMartialProc(i, out var e) && e.kind == kind) return true;
        return false;
    }
    private void MixedBuild()
    {
        foreach (string art in new[] { "破甲掌", "无影连环剑", "反震诀", "饮血刀法", "毒砂掌", "吸星诀" })
            F.playerStats.ApplyMartialArt(art);
        F.playerStats.runtimeStats.currentHealth = 1000;
        F.playerStats.runtimeStats.defense = 10;
        Set(screen,"previousPlayerHealth",F.playerStats.runtimeStats.currentHealth);
    }
    private IEnumerator Suite()
    {
        yield return new WaitForSecondsRealtime(3.2f);
        screen = FindAnyObjectByType<BattleScreenController>();
        F.playerController.enabled = false;
        foreach (var e in FindObjectsByType<EncounterTrigger>(FindObjectsSortMode.None)) e.gameObject.SetActive(false);
        foreach (MartialProcKind kind in Enum.GetValues(typeof(MartialProcKind)))
        {
            var frames = MartialProcVfxArt.Frames(kind);
            Check(frames.Length == 6, kind + ": six Resources sprites loaded");
            for (int i = 0; i < 6; i++)
            {
                var f = frames[i];
                Check(f.rect.size == new Vector2(256,256) && f.pivot == new Vector2(128,128) &&
                    f.pixelsPerUnit == 256 && f.texture.width == 1536 && f.texture.height == 256 &&
                    f.texture.filterMode == FilterMode.Point && f.texture.mipmapCount == 1 &&
                    f.texture.wrapMode == TextureWrapMode.Clamp, kind + ": import settings frame " + i);
                Check(MartialProcVfxArt.Frame(kind, (i+.5f)/6) == f, kind + ": sequence frame " + i);
            }
            Check(MartialProcVfxArt.Frame(kind,-.1f)==null && MartialProcVfxArt.Frame(kind,1f)==null,
                kind + ": no looping outside lifetime");
        }
        foreach (bool portrait in new[] { false, true })
        {
            Resize(portrait ? 540 : 960, portrait ? 960 : 540);
            yield return new WaitForSecondsRealtime(.2f);
            string suffix = portrait ? "_portrait" : "_landscape";
            Check(Screen.width == (portrait ? 540 : 960), "viewport " + suffix);
            Fresh(); Attack(); Attack(false);
            Check(B.MartialProcSequence == 0 && screen.ActiveMartialProcVfxCount == 0, "unlearned basic attacks have no martial proc " + suffix);

            Fresh(); F.playerStats.ApplyMartialArt("破甲掌"); Attack();
            Check(Has(MartialProcKind.ArmorBreak) && B.EnemyArmorBreak > 0, "armor palm real reduction emits impact " + suffix);
            Check(screen.ActiveHeroSkillVfxCount == 0, "armor proc replaces old generic palm layer " + suffix);
            yield return Sequence("armor" + suffix, new[]{.08f,.20f,.36f});

            Fresh(); F.playerStats.ApplyMartialArt("无影连环剑");
            for (int i=0;i<4;i++) Attack();
            Check(!Has(MartialProcKind.SwiftCombo), "rank one combo cannot fire before fifth hit " + suffix);
            Attack();
            Check(Has(MartialProcKind.SwiftCombo), "rank one combo emits on fifth successful hit " + suffix);
            Check(screen.ActiveHeroSkillVfxCount == 0, "combo replaces generic sword layer " + suffix);
            yield return Sequence("combo" + suffix, new[]{.08f,.20f,.34f});

            Fresh(); F.playerStats.ApplyMartialArt("金钟罩"); F.playerStats.ApplyMartialArt("反震诀");
            Property("PlayerShield", 80f); float hp = F.playerStats.runtimeStats.currentHealth; Attack(false);
            Check(Has(MartialProcKind.Retaliation) && hp == F.playerStats.runtimeStats.currentHealth && B.PlayerShield < 80,
                "shield fully absorbs hit and still triggers retaliation " + suffix);
            yield return Sequence("retaliation" + suffix, new[]{.08f,.20f,.36f});

            Fresh(); F.playerStats.ApplyMartialArt("饮血刀法"); Attack();
            Check(!Has(MartialProcKind.LifeDrain), "full health emits no false lifesteal " + suffix);
            F.playerStats.runtimeStats.currentHealth = 1000; hp = F.playerStats.runtimeStats.currentHealth;
            Set(screen,"previousPlayerHealth",hp); Attack();
            Check(Has(MartialProcKind.LifeDrain) && F.playerStats.runtimeStats.currentHealth > hp,
                "blood blade actual healing emits return " + suffix);
            yield return Sequence("lifedrain" + suffix, new[]{.05f,.20f,.39f});

            Fresh(); F.playerStats.ApplyMartialArt("吸星诀"); F.playerStats.ApplyMartialArt("毒砂掌"); Attack();
            Check(!Has(MartialProcKind.LifeDrain), "full health poison build emits no drain " + suffix);
            F.playerStats.runtimeStats.currentHealth = 1000;
            Set(screen,"previousPlayerHealth",F.playerStats.runtimeStats.currentHealth);
            Invoke(B,"ApplyPoisonTick"); Invoke(screen,"TrackMartialProcs");
            Check(Has(MartialProcKind.LifeDrain), "absorb star poison healing shares return effect " + suffix);
            yield return Sequence("poison_drain" + suffix,new[]{.20f,.39f});

            Fresh(); MixedBuild(); F.playerStats.runtimeStats.dodgeChance = 1; B.currentEnemy.dodgeChance = 1;
            Attack(); Attack(false);
            Check(B.MartialProcSequence == 0, "dodged attacks cannot proc armor combo retaliation or drain " + suffix);
            Fresh(); MixedBuild(); Property("PlayerSuccessfulHits",4); Attack(); Attack(false);
            Invoke(B,"ApplyPoisonTick"); Invoke(screen,"TrackMartialProcs");
            foreach (MartialProcKind kind in Enum.GetValues(typeof(MartialProcKind)))
                Check(Has(kind), "same-frame journal retains " + kind + suffix);
            Check(screen.ActiveMartialProcVfxCount >= 4 && screen.ActiveMartialProcVfxCount <= 8,
                "mixed effects have independent bounded slots " + suffix);
            float playerHp = F.playerStats.runtimeStats.currentHealth, enemyHp = B.currentEnemy.currentHealth;
            var rng = UnityEngine.Random.state;
            for(int i=0;i<20;i++) Invoke(screen,"TrackMartialProcs");
            Check(playerHp == F.playerStats.runtimeStats.currentHealth && enemyHp == B.currentEnemy.currentHealth &&
                JsonUtility.ToJson(rng) == JsonUtility.ToJson(UnityEngine.Random.state), "presentation does not change damage or gameplay RNG " + suffix);
            yield return new WaitForSeconds(.18f); Time.timeScale = 0;
            int active = screen.ActiveMartialProcVfxCount; float frozen = Time.time;
            yield return Capture("pause_start"+suffix); yield return new WaitForSecondsRealtime(.6f);
            Check(Time.time == frozen && active == screen.ActiveMartialProcVfxCount, "pause freezes proc lifetimes " + suffix);
            yield return Capture("pause_end"+suffix); Time.timeScale = .25f;
            yield return new WaitForSecondsRealtime(.3f);
            Check(Time.time-frozen < .15f && screen.ActiveMartialProcVfxCount > 0, "slow motion retains proc sequence " + suffix);
            Time.timeScale = 1; yield return new WaitForSeconds(.6f);
            Check(screen.ActiveMartialProcVfxCount == 0, "all effects expire after resume " + suffix);
            Fresh(); MixedBuild();
            for(int i=0;i<45;i++) Attack();
            Check(!B.TryGetMartialProc(1,out _) && B.TryGetMartialProc(B.MartialProcSequence,out _), "bounded journal drops old events and keeps newest " + suffix);
            Check(screen.ActiveMartialProcVfxCount <= 8, "event burst respects eight sprite limit " + suffix);
            B.CancelBattle(); Invoke(screen,"TrackMartialProcs");
            Check(screen.ActiveMartialProcVfxCount == 0 && B.MartialProcSequence == 0, "cancel clears event journal and visuals " + suffix);
        }
        foreach (int rank in new[] { 2, 3 })
        {
            Fresh(); for(int i=0;i<rank;i++) F.playerStats.ApplyMartialArt("无影连环剑");
            for(int i=0;i<5-rank;i++) Attack();
            Check(!Has(MartialProcKind.SwiftCombo), "rank " + rank + " combo waits for correct interval");
            Attack(); Check(Has(MartialProcKind.SwiftCombo), "rank " + rank + " combo emits at correct interval");
            if(rank==3) Check(B.UltimateVisualSequence>0 && screen.ActiveMartialProcVfxCount>0,
                "rank three ultimate and settlement combo coexist");
        }
        foreach (string id in new[] { "black_iron_ring", "bone_rot_gloves" })
        {
            Fresh(); var equipment=F.playerStats.equipment;
            equipment.Equip(equipment.GetTemplate(id)); F.playerStats.runtimeStats.critChance=0; Attack();
            Check(Has(MartialProcKind.ArmorBreak) && screen.ActiveHeroSkillVfxCount==0,
                id + ": equipment armor break shares dedicated impact without old palm layer");
        }
        Fresh(); F.playerStats.equipment.Equip(F.playerStats.equipment.GetTemplate("wanderer_cloak"));
        F.playerStats.runtimeStats.currentHealth=1000; F.playerStats.runtimeStats.dodgeChance=1; Attack(false);
        Check((B.LastVfxCues & BattleVfxCue.Heal)!=0 && !Has(MartialProcKind.LifeDrain),
            "dodge healing keeps ordinary healing instead of enemy-to-player drain");
        Fresh(); B.currentEnemy.currentHealth=1000; B.currentEnemy.lifeSteal=.2f; Property("PlayerShield",0f); Attack(false);
        Check(!Has(MartialProcKind.LifeDrain),"enemy lifesteal never sends energy to player");
        Fresh(); F.playerStats.ApplyMartialArt("破甲掌"); Attack();
        Resize(960,540); yield return new WaitForSecondsRealtime(.08f); Invoke(screen,"TrackMartialProcs");
        Check(screen.ActiveMartialProcVfxCount==0,"viewport rotation clears old-layout effects");
        Resize(540,960); yield return new WaitForSecondsRealtime(.1f);
        // Real coroutine combat, not manually injected visual cues, at high attack speed.
        Fresh(); MixedBuild(); var enemy = B.currentEnemy.Clone(); B.CancelBattle();
        F.playerStats.runtimeStats.maxHealth = 100000; F.playerStats.runtimeStats.currentHealth = 50000;
        F.playerStats.runtimeStats.attackSpeed = 10; enemy.maxHealth = enemy.currentHealth = 100000;
        enemy.attackSpeed = 10; enemy.attack = 20; F.mainTimeRemaining = 60;
        Invoke(F,"BeginNormalBattle",enemy.Clone(),0,0,EncounterType.NormalEnemy);
        float main = F.mainTimeRemaining; int peak = 0; float until = Time.time + 1.2f;
        while(Time.time < until) { peak = Mathf.Max(peak,screen.ActiveMartialProcVfxCount); yield return null; }
        Check(B.PlayerAttackVisualSequence >= 5 && peak > 0 && peak <= 8,"real high-speed mixed combat emits bounded effects");
        Check(F.mainTimeRemaining < main, "normal combat consumes main timer");
        yield return Capture("real_fast_mixed_portrait");
        B.StopAllCoroutines(); yield return new WaitForSeconds(.6f);
        Check(screen.ActiveMartialProcVfxCount == 0,"stopped high-speed combat leaves no stale effects");
        B.CancelBattle(); Invoke(F,"SetPhase",GamePhase.CaveRunning); F.BeginCaveBattle(enemy.Clone(),0,0,null);
        main = F.mainTimeRemaining; yield return new WaitForSeconds(.35f);
        Check(F.mainTimeRemaining == main && B.PlayerAttackVisualSequence > 0, "cave combat animates while main timer pauses");
        B.CancelBattle(); F.bossIntroDuration=0; F.bossStats=enemy.Clone(); Invoke(F,"BeginBossBattle");
        main = F.mainTimeRemaining; yield return new WaitForSeconds(.35f);
        Check(F.CurrentPhase == GamePhase.BossBattle && F.bossBattleTime > 0 && F.mainTimeRemaining == main,
            "final boss advances independent timer");
        B.CancelBattle(); Invoke(screen,"TrackMartialProcs");
    }
}
#endif
