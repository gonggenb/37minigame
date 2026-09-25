using System;
using UnityEngine;

namespace WuxiaRoguelite.Runtime
{
    [Serializable]
    public sealed class EndlessProgressionData
    {
        public int version = 1;
        public int coins, skillPoints, bestRound;
        public int[] ranks = new int[EndlessProgressionCatalog.SkillCount];
        public string activeRunId = "";
        public int creditedRunCoins;
        public int Rank(EndlessSkill skill) => EndlessProgressionCatalog.IsValid(skill) && ranks != null &&
            (int)skill < ranks.Length ? Mathf.Clamp(ranks[(int)skill], 0, EndlessProgressionCatalog.MaxRank) : 0;
        public bool HasUpgradesRemaining
        {
            get { foreach (var skill in EndlessProgressionCatalog.Skills) if (Rank(skill) < EndlessProgressionCatalog.MaxRank) return true; return false; }
        }
    }

    /// <summary>A single versioned local wallet; each transaction persists coins and ranks together.
    /// Cumulative run receipts make repeated checkpoint/end-result calls idempotent.</summary>
    public static class EndlessProgression
    {
        public const string SaveKey = "WuxiaRoguelite.EndlessProgression.v1";
        private static string storageKey = SaveKey;
        public static bool LoadFailed { get; private set; }
        public static EndlessProgressionData Read()
        {
            LoadFailed = false;
            if (!PlayerPrefs.HasKey(storageKey)) return new EndlessProgressionData();
            try
            {
                var data = JsonUtility.FromJson<EndlessProgressionData>(PlayerPrefs.GetString(storageKey));
                if (data == null || data.version != 1) throw new InvalidOperationException("Unsupported endless save");
                data.coins = Mathf.Clamp(data.coins, 0, EndlessProgressionCatalog.BalanceLimit);
                data.skillPoints = Mathf.Clamp(data.skillPoints, 0, EndlessProgressionCatalog.BalanceLimit);
                data.bestRound = Mathf.Max(0, data.bestRound);
                data.creditedRunCoins = Mathf.Clamp(data.creditedRunCoins, 0, EndlessProgressionCatalog.BalanceLimit);
                var ranks = new int[EndlessProgressionCatalog.SkillCount];
                foreach (var skill in EndlessProgressionCatalog.Skills) ranks[(int)skill] = data.Rank(skill);
                data.ranks = ranks;
                return data;
            }
            catch (Exception ex)
            {
                // Keep malformed data for diagnosis; never erase a save just by opening the page.
                LoadFailed = true;
                Debug.LogWarning("Endless progression save could not be read: " + ex.Message);
                return new EndlessProgressionData();
            }
        }
        private static void Save(EndlessProgressionData data)
        {
            PlayerPrefs.SetString(storageKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
        public static string BeginRun()
        {
            var data = Read();
            if (LoadFailed) return null;
            data.activeRunId = Guid.NewGuid().ToString("N");
            data.creditedRunCoins = 0;
            Save(data);
            return data.activeRunId;
        }
        public static int CreditRun(string runId, int totalEarned, int roundsCleared)
        {
            var data = Read();
            if (LoadFailed || string.IsNullOrEmpty(runId) || data.activeRunId != runId) return 0;
            int total = Mathf.Clamp(totalEarned, 0, EndlessProgressionCatalog.BalanceLimit);
            int delta = Mathf.Max(0, total - data.creditedRunCoins);
            int best = Mathf.Max(data.bestRound, roundsCleared);
            if (delta == 0 && best == data.bestRound) return 0;
            int credited = Mathf.Min(delta, EndlessProgressionCatalog.BalanceLimit - data.coins);
            data.coins += credited;
            data.creditedRunCoins = Mathf.Max(total, data.creditedRunCoins);
            data.bestRound = best;
            Save(data);
            return credited;
        }
        public static bool TryExchange()
        {
            var data = Read();
            if (LoadFailed || !data.HasUpgradesRemaining || data.coins < EndlessProgressionCatalog.CoinsPerPoint ||
                data.skillPoints >= EndlessProgressionCatalog.BalanceLimit) return false;
            data.coins -= EndlessProgressionCatalog.CoinsPerPoint;
            data.skillPoints++;
            Save(data);
            return true;
        }
        public static bool TryUpgrade(EndlessSkill skill)
        {
            if (!EndlessProgressionCatalog.IsValid(skill)) return false;
            var data = Read();
            if (LoadFailed) return false;
            int rank = data.Rank(skill), cost = EndlessProgressionCatalog.UpgradeCost(rank);
            if (rank >= EndlessProgressionCatalog.MaxRank || data.skillPoints < cost) return false;
            data.skillPoints -= cost;
            data.ranks[(int)skill] = rank + 1;
            Save(data);
            return true;
        }
#if UNITY_EDITOR
        // Play Mode fixtures use a disposable wallet and cannot award/spend the player's coins.
        public static IDisposable IsolateForTests() => new TestScope();
        private sealed class TestScope : IDisposable
        {
            private readonly string previous = storageKey;
            public TestScope() { storageKey = SaveKey + ".test." + Guid.NewGuid().ToString("N"); }
            public void Dispose() { PlayerPrefs.DeleteKey(storageKey); storageKey = previous; PlayerPrefs.Save(); }
        }
#endif
    }
}
