#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

public sealed partial class EndlessModePlayModeProbe
{
    private float untrainedAttack;
    private void CheckProgressionEconomy()
    {
        using (EndlessProgression.IsolateForTests())
        {
            Check(EndlessProgression.Read().coins == 0 && !EndlessProgression.TryExchange() &&
                !EndlessProgression.TryUpgrade(EndlessSkill.Power), "New wallet starts empty and rejects unfunded transactions");
            string id = EndlessProgression.BeginRun();
            Check(EndlessProgression.CreditRun(id, 24, 0) == 24 && !EndlessProgression.TryExchange(), "24 coins cannot buy a 25-coin skill point");
            Check(EndlessProgression.CreditRun(id, 25, 0) == 1 && EndlessProgression.TryExchange(), "Cumulative checkpoint credits only the delta and exact-price exchange succeeds");
            Check(EndlessProgression.Read().coins == 0 && EndlessProgression.Read().skillPoints == 1,
                "Exchange persists both debit and point after reloading the wallet");
            Check(EndlessProgression.TryUpgrade(EndlessSkill.Power) && EndlessProgression.Read().Rank(EndlessSkill.Power) == 1 &&
                EndlessProgression.Read().skillPoints == 0, "Upgrade persists rank and point consumption");
            Check(EndlessProgression.CreditRun(id, 25, 0) == 0 && EndlessProgression.CreditRun(id, 2, 0) == 0 &&
                EndlessProgression.Read().coins == 0, "Duplicate and older results cannot re-credit spent coins");
            Check(!EndlessProgression.TryUpgrade((EndlessSkill)(-1)) && !EndlessProgression.TryUpgrade((EndlessSkill)6), "Invalid skill ids are rejected");
            string next = EndlessProgression.BeginRun();
            Check(EndlessProgression.CreditRun(id, 1000, 10) == 0 && EndlessProgression.CreditRun(next, 10000, 3) == 10000,
                "A stale run cannot claim after a new run; best rounds persist with the current receipt");
            for (int i = 0; i < 200; i++) CheckExchange();
            int points = EndlessProgression.Read().skillPoints;
            Check(EndlessProgressionCatalog.UpgradeCost(2) == 1 && EndlessProgressionCatalog.UpgradeCost(3) == 2 &&
                EndlessProgressionCatalog.UpgradeCost(6) == 3 && EndlessProgressionCatalog.UpgradeCost(9) == 4,
                "Upgrade costs increase at ranks 3, 6 and 9");
            foreach (var skill in EndlessProgressionCatalog.Skills)
            {
                int before = EndlessProgression.Read().Rank(skill);
                for (int rank = before; rank < EndlessProgressionCatalog.MaxRank; rank++)
                    if (!EndlessProgression.TryUpgrade(skill)) throw new Exception("Funded upgrade failed");
                Check(!EndlessProgression.TryUpgrade(skill), "Max rank rejects further spending for " + skill);
                Check(Resources.Load<Texture2D>("Icons/" + EndlessProgressionCatalog.IconId(skill)) != null,
                    "Shipped skill icon resolves for " + skill);
            }
            Check(EndlessProgression.Read().skillPoints == points - 131 && !EndlessProgression.TryExchange(),
                "All six skills cost 132 points in total; fully trained wallet stops further exchange");
            var stats = new CombatantStats { maxHealth = 100, attack = 20, defense = 3, attackSpeed = 1, critChance = .05f, dodgeChance = .03f };
            EndlessProgressionCatalog.Apply(stats, EndlessProgression.Read());
            Check(Mathf.Approximately(stats.maxHealth, 150) && Mathf.Approximately(stats.currentHealth, 150) &&
                Mathf.Approximately(stats.attack, 26) && Mathf.Approximately(stats.defense, 8) && Mathf.Approximately(stats.attackSpeed, 1.2f) &&
                Mathf.Approximately(stats.critChance, .15f) && Mathf.Approximately(stats.dodgeChance, .13f), "All six max-rank effects match the displayed progression curve");
            Check(EndlessProgression.Read().bestRound == 3, "Best completed round survives purchases and upgrades");
            // Malformed/future data is preserved, never silently replaced with a blank wallet.
            string key = (string)typeof(EndlessProgression).GetField("storageKey", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            const string future = "{\"version\":99,\"coins\":123}";
            PlayerPrefs.SetString(key, future);
            Check(EndlessProgression.BeginRun() == null && !EndlessProgression.TryExchange() &&
                !EndlessProgression.TryUpgrade(EndlessSkill.Power) && PlayerPrefs.GetString(key) == future,
                "Unsupported save version blocks mutation and preserves the original data");
        }
    }
    private void CheckExchange() { if (!EndlessProgression.TryExchange()) throw new Exception("Funded exchange failed"); }

    private IEnumerator CheckProgressionResultAndUpgrade()
    {
        int expected = 0;
        foreach (EndlessReward kind in Enum.GetValues(typeof(EndlessReward)))
            expected += F.EndlessRewardCount(kind) * EndlessProgressionCatalog.RewardCoins(kind);
        Check(expected == 86 && F.EndlessCoinsEarned == expected && EndlessProgression.Read().coins == expected &&
            EndlessProgression.Read().bestRound == 2,
            "Real battle callbacks award 2 normal + 24 mid-boss + 60 round coins, with best round 2");
        Invoke(F, "SaveEndlessRewards"); Invoke(F, "SaveEndlessRewards");
        Check(EndlessProgression.Read().coins == expected, "Repeated death/result checkpoints cannot duplicate the wallet reward");
        F.playerStats.ResetRun(); untrainedAttack = F.playerStats.runtimeStats.attack;
        Check(F.ExchangeEndlessPoint() && F.UpgradeEndlessSkill(EndlessSkill.Power), "Result supports coin exchange and permanent skill upgrade");
        Check(Mathf.Approximately(F.playerStats.runtimeStats.attack, untrainedAttack), "Buying a skill does not mutate the completed run's stats");
        var hud = FindFirstObjectByType<PrototypeHUDController>();
        Invoke(hud, "OpenEndlessProgression");
        Resize(960, 540); yield return null; yield return Capture("progression_landscape");
        Resize(540, 960); yield return null; yield return Capture("progression_portrait");
        typeof(PrototypeHUDController).GetField("endlessProgressionOpen", Flags).SetValue(hud, false);
        Check(EndlessProgression.Read().coins == 61 && EndlessProgression.Read().skillPoints == 0,
            "Transaction reload keeps remaining coins, consumed point and permanent rank");
    }
    private void CheckProgressionApplied()
    {
        Check(Mathf.Approximately(F.playerStats.runtimeStats.attack, untrainedAttack * 1.03f) && F.EndlessCoinsEarned == 0,
            "Retry resets run earnings and applies saved permanent attack exactly once");
        var snapshot = F.playerStats.runtimeStats.Clone();
        Invoke(F, "BeginNextEndlessRound");
        Check(Mathf.Approximately(snapshot.attack, F.playerStats.runtimeStats.attack), "Round rollover does not reapply permanent bonuses");
        // Restore a fresh opening fixture for the remaining original endless regression checks.
        Invoke(F, "EndRun", false, "progression fixture"); F.RetryCurrentLevel();
    }
    private IEnumerator CheckEliteAndCaveRewards()
    {
        F.playerStats.runtimeStats.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == WuxiaRoguelite.GameFlow.GamePhase.Result, "end prior fixture");
        F.RetryCurrentLevel(); ChooseOpening();
        Invoke(F, "EndRun", false, "empty run fixture");
        Check(F.EndlessCoinsEarned == 0, "Zero-victory death gives no currency");
        F.RetryCurrentLevel(); ChooseOpening();
        var enemy = new CombatantStats { maxHealth = 10000, currentHealth = 10000, attack = .1f, defense = 0, attackSpeed = .5f };
        Invoke(F, "BeginNormalBattle", enemy, 0, 0, WuxiaRoguelite.Map.EncounterType.EliteEnemy);
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => F.CurrentPhase == WuxiaRoguelite.GameFlow.GamePhase.MainMapRunning, "elite victory reward");
        Check(F.EndlessCoinsEarned == 5 && F.EndlessRewardCount(EndlessReward.Elite) == 1 &&
            F.EndlessRewardCount(EndlessReward.Normal) == 0, "Elite victory pays five coins without a duplicate normal reward");
        Invoke(F, "SetPhase", WuxiaRoguelite.GameFlow.GamePhase.CaveRunning);
        F.BeginCaveBattle(enemy, 0, 0, null);
        F.battleManager.currentEnemy.currentHealth = 0;
        yield return Until(() => !F.battleManager.IsBattleActive, "cave victory reward");
        Check(F.EndlessCoinsEarned == 9 && F.EndlessRewardCount(EndlessReward.Cave) == 1,
            "Cave victory pays four coins through its actual battle callback");
    }

    private void CheckNormalModeHasNoProgression()
    {
        Check(EndlessProgression.Read().Rank(EndlessSkill.Power) == 1 && Mathf.Approximately(F.playerStats.runtimeStats.attack, untrainedAttack),
            "Ordinary level two ignores permanent endless bonuses while retaining the saved skill");
    }
}
#endif
