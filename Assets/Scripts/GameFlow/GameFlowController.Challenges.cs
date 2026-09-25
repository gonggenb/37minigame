using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.GameFlow
{
    public partial class GameFlowController
    {
        public BossTalent FinalBossTalent { get; private set; }
        public BossTalent MidBossTalent { get; private set; }
        public bool HasBossTalents => FinalBossTalent != BossTalent.None;

        public RunChallengeState ChallengeRun { get; private set; }
        public bool IsChallengeBriefingActive { get; private set; }
        public string ChallengeResult { get; private set; } = string.Empty;
        private int selectedChallengeTier = -1;
        private BossApproach previousApproach;
        private EncounterTrigger pendingBountyEncounter;
        public bool HasRunChallenge => HasRouteSpecialties && ChallengeRun != null;

        private void PrepareRunChallenge()
        {
            pendingBountyEncounter = null;
            ChallengeResult = string.Empty;
            IsChallengeBriefingActive = false;
            ChallengeRun = null;
            FinalBossTalent = IsTutorialLevel ? BossTalent.None : BossTalentCatalog.Roll();
            MidBossTalent = HasRouteSpecialties ? BossTalentCatalog.Roll() : BossTalent.None;
            IsChallengeBriefingActive = HasBossTalents;
            if (!HasRouteSpecialties) return;
            if (IsEndlessMode) selectedChallengeTier = 0;
            else if (selectedChallengeTier < 0) selectedChallengeTier = ChallengeProgress.HighestUnlocked;
            selectedChallengeTier = Mathf.Clamp(selectedChallengeTier, 0, ChallengeProgress.HighestUnlocked);
            int seed = Random.Range(1, int.MaxValue);
            var random = new System.Random(seed);
            var approach = (BossApproach)random.Next(1, 4);
            if (approach == previousApproach) approach = (BossApproach)((int)approach % 3 + 1);
            previousApproach = approach;
            ChallengeRun = new RunChallengeState { tier = selectedChallengeTier, seed = seed, approach = approach };
            var elites = FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(e => e.gameObject.scene.name == LevelSequence.LevelTwoSceneName && e.encounterType == EncounterType.EliteEnemy)
                .OrderBy(e => e.name).ToList();
            // One contract near the practice grounds, another on a different branch.
            var practice = elites.Where(e => PingchuanRouteCatalog.ForEncounter(e) == RouteSpecialty.Practice).ToList();
            var firstPool = practice.Count > 0 ? practice : elites;
            if (firstPool.Count > 0)
            {
                var first = firstPool[random.Next(firstPool.Count)];
                ChallengeRun.bounties.Add(new BountyContract { target = first, trait = EnemyTrait.OpeningArmor });
                elites.Remove(first);
                var other = elites.Where(e => PingchuanRouteCatalog.ForEncounter(e) != RouteSpecialty.Practice).ToList();
                if (other.Count == 0) other = elites;
                if (other.Count > 0) ChallengeRun.bounties.Add(new BountyContract
                    { target = other[random.Next(other.Count)], trait = EnemyTrait.HeavyOpening });
            }
            IsChallengeBriefingActive = true;
        }

        public void SelectChallengeTier(int tier)
        {
            if (IsEndlessMode) return;
            if (!HasRunChallenge || !IsChallengeBriefingActive || CurrentPhase != GamePhase.LevelUpPaused ||
                !isOpeningMartialArtChoice || tier < 0 || tier > ChallengeProgress.HighestUnlocked) return;
            selectedChallengeTier = ChallengeRun.tier = tier;
        }

        public void ConfirmChallengeBriefing() => IsChallengeBriefingActive = false;

        public EnemyTrait ChallengeTrait(EncounterTrigger encounter, EnemyTrait authored)
        {
            if (!HasRunChallenge || encounter == null || encounter.gameObject.scene.name != LevelSequence.LevelTwoSceneName) return authored;
            var bounty = ChallengeRun.Find(encounter);
            if (bounty != null) return bounty.trait;
            return encounter.encounterType == EncounterType.EliteEnemy
                ? RunChallengeCatalog.EliteTrait(ChallengeRun.tier) : authored;
        }

        public void ApplyChallengeEncounter(EncounterTrigger encounter, CombatantStats clone)
        {
            if (!HasRunChallenge || encounter == null || encounter.gameObject.scene.name != LevelSequence.LevelTwoSceneName ||
                (encounter.encounterType != EncounterType.NormalEnemy && encounter.encounterType != EncounterType.EliteEnemy)) return;
            clone.maxHealth *= RunChallengeCatalog.EnemyHealth(ChallengeRun.tier);
            clone.attack *= RunChallengeCatalog.EnemyAttack(ChallengeRun.tier);
            if (ChallengeRun.Find(encounter) != null)
            {
                clone.maxHealth *= BountyContract.HealthMultiplier;
                clone.attack *= BountyContract.AttackMultiplier;
            }
            clone.ResetHealth();
        }

        private string ResolveBountyVictory()
        {
            var encounter = pendingBountyEncounter;
            pendingBountyEncounter = null;
            var bounty = HasRunChallenge ? ChallengeRun.Find(encounter) : null;
            if (bounty == null || bounty.completed) return string.Empty;
            bounty.completed = true;
            var school = GetDominantSchool();
            string art = playerStats.learnedMartialArts
                .Where(id => MartialArtCatalog.Get(id).school == school &&
                    playerStats.GetMartialArtRank(id) < MartialArtCatalog.Get(id).maxRank)
                .OrderByDescending(playerStats.GetMartialArtRank).ThenBy(id => id).FirstOrDefault();
            if (art != null)
            {
                playerStats.ApplyMartialArt(art);
                bounty.rewardReceived = $"《{art}》提升至{playerStats.GetMartialArtRank(art)}重";
            }
            else
            {
                playerStats.GainCopper(40);
                bounty.rewardReceived = "主修已满，铜钱 +40";
            }
            return "悬赏完成：" + bounty.rewardReceived;
        }

        public void RetryNextChallenge()
        {
            if (CurrentPhase != GamePhase.Result || !bossDefeated || !HasRunChallenge ||
                ChallengeRun.tier >= RunChallengeCatalog.TierCount - 1 || ChallengeRun.tier + 1 > ChallengeProgress.HighestUnlocked) return;
            selectedChallengeTier = ChallengeRun.tier + 1;
            RetryCurrentLevel();
        }

        private void RecordChallengeVictory()
        {
            if (!HasRunChallenge) return;
            bool unlocked = ChallengeProgress.RecordWin(ChallengeRun.tier);
            ChallengeResult = unlocked ? $"新挑战已解锁：{RunChallengeCatalog.TierName(ChallengeRun.tier + 1)}"
                : ChallengeRun.tier < 2 ? "可继续挑战下一档险境" : "绝境已破，可换一种强敌情报再战";
        }
    }
}
