using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.GameFlow
{
    public sealed class BountyContract
    {
        public EncounterTrigger target;
        public EnemyTrait trait;
        public bool completed;
        public string rewardReceived;
        public string Region => PingchuanRouteCatalog.Name(PingchuanRouteCatalog.ForEncounter(target)) is string name &&
            !string.IsNullOrEmpty(name) ? name : "城镇支路";
        public string Risk => trait == EnemyTrait.OpeningArmor ? "护甲强敌" : "重撞强敌";
        public const float HealthMultiplier = 1.45f;
        public const float AttackMultiplier = 1.20f;
    }

    public sealed class RunChallengeState
    {
        public int tier;
        public int seed;
        public BossApproach approach;
        public readonly List<BountyContract> bounties = new List<BountyContract>(2);
        public BountyContract Find(EncounterTrigger target) => bounties.Find(b => b.target == target);
    }

    public static class ChallengeProgress
    {
        public const string UnlockKey = "WuxiaRoguelite.ChallengeTierUnlocked.v1";
        // Earlier second-level clears count as completing the first tier.
        public static int HighestUnlocked => Mathf.Clamp(Mathf.Max(PlayerPrefs.GetInt(UnlockKey, 0),
            LevelSequence.LevelTwoCompleted ? 1 : 0), 0, RunChallengeCatalog.TierCount - 1);
        public static bool RecordWin(int tier)
        {
            int next = Mathf.Clamp(tier + 1, 0, RunChallengeCatalog.TierCount - 1);
            bool unlocked = next > HighestUnlocked;
            PlayerPrefs.SetInt(UnlockKey, Mathf.Max(HighestUnlocked, next));
            PlayerPrefs.Save();
            return unlocked;
        }
    }
}
