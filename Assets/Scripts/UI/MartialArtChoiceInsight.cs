using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    // Read-only offer explanations shared by both orientations. Never apply an art to preview it.
    public static class MartialArtChoiceInsight
    {
        public readonly struct FusionPreview
        {
            public readonly MartialArtSecretDefinition secret;
            public readonly int currentRank, targetRank, threshold, firstBefore, secondBefore, firstAfter, secondAfter;
            public bool Mastered => currentRank >= secret.maxRank;
            public bool Ready => !Mastered && firstAfter >= threshold && secondAfter >= threshold;
            public int Missing => Mathf.Max(0, threshold - firstAfter) + Mathf.Max(0, threshold - secondAfter);

            public FusionPreview(PlayerStats player, MartialArtDefinition art, MartialArtSecretDefinition secret)
            {
                this.secret = secret;
                currentRank = player.GetSecretRank(secret.id);
                targetRank = Mathf.Min(currentRank + 1, secret.maxRank);
                threshold = targetRank == 1 ? 2 : 4;
                firstBefore = player.GetMartialArtSchoolRank(secret.firstSchool);
                secondBefore = player.GetMartialArtSchoolRank(secret.secondSchool);
                int gain = player.GetMartialArtRank(art.id) < art.maxRank ? 1 : 0;
                firstAfter = firstBefore + (art.school == secret.firstSchool ? gain : 0);
                secondAfter = secondBefore + (art.school == secret.secondSchool ? gain : 0);
            }
        }

        // Both recipes are visible, including completed ones. Reading never mutates the build.
        public static void FusionPreviews(PlayerStats player, string id, List<FusionPreview> results)
        {
            results.Clear();
            var art = MartialArtCatalog.Get(id);
            if (player == null || art == null) return;
            foreach (string secretId in MartialArtCatalog.AllSecretIds)
            {
                var secret = MartialArtCatalog.GetSecret(secretId);
                if (secret.firstSchool == art.school || secret.secondSchool == art.school)
                    results.Add(new FusionPreview(player, art, secret));
            }
        }

        public static string Benefit(PlayerStats player, string id)
        {
            var art = MartialArtCatalog.Get(id);
            if (player == null || art == null) return string.Empty;
            int rank = player.GetMartialArtRank(id);
            if (rank >= art.maxRank) return "已达最高重数";
            var stats = player.runtimeStats;
            if (stats != null)
            {
                switch (id)
                {
                    case "疾剑式": return $"攻速 {stats.attackSpeed:0.00} → {stats.attackSpeed + .12f:0.00}";
                    case "金钟罩": return $"按当前防御，开战护盾 +{CombatNumberDisplay.Format(8f + stats.defense * 1.5f)}";
                    case "铁布衫": return $"气血上限 +{CombatNumberDisplay.Format(stats.maxHealth * .15f)}，立即补足；防御 +{CombatNumberDisplay.Format(WuxiaRoguelite.Battle.BossTalentCatalog.IronBodyDefensePerRank)}";
                    case "吸星诀": return $"吸血 {stats.lifeSteal:P0} → {Mathf.Clamp01(stats.lifeSteal + .04f):P0}";
                    case "踏雪无痕": return $"闪避 {stats.dodgeChance:P0} → {Mathf.Clamp01(stats.dodgeChance + .04f):P0}";
                }
            }
            return art.GetEffectSummary(rank + 1);
        }

        public static string BuildLink(PlayerStats player, string id)
        {
            var art = MartialArtCatalog.Get(id);
            if (player == null || art == null || player.GetMartialArtRank(id) >= art.maxRank) return string.Empty;
            int depth = player.GetMartialArtSchoolRank(art.school);
            string link = $"{MartialArtCatalog.SchoolName(art.school)}总重数 {depth} → {depth + 1}";
            if (id == "百毒心经") link = player.GetMartialArtRank("毒砂掌") > 0
                ? "强化已学毒砂掌的毒层与毒伤" : "需搭配施毒武学，才能发挥毒伤收益";
            else if (id == "疾剑式" && player.GetMartialArtRank("剑气诀") > 0)
                link = "出手更快，更频繁触发已学剑气诀";
            else if (id == "反震诀") link = "以当前防御反击，护盾挡伤也触发";

            string closest = null;
            int bestScore = int.MaxValue;
            foreach (string secretId in MartialArtCatalog.AllSecretIds)
            {
                var secret = MartialArtCatalog.GetSecret(secretId);
                if (secret.firstSchool != art.school && secret.secondSchool != art.school) continue;
                int current = player.GetSecretRank(secretId);
                if (current >= secret.maxRank) continue;
                int threshold = current == 0 ? 2 : 4;
                var other = secret.firstSchool == art.school ? secret.secondSchool : secret.firstSchool;
                int otherDepth = player.GetMartialArtSchoolRank(other);
                int ownMissing = Mathf.Max(0, threshold - depth - 1);
                int otherMissing = Mathf.Max(0, threshold - otherDepth);
                int score = ownMissing + otherMissing;
                // Prefer an already developing pairing when distances tie.
                int priority = score * 100 - Mathf.Min(otherDepth, 10);
                if (priority >= bestScore) continue;
                bestScore = priority;
                if (score == 0)
                    closest = $"选择后{(current == 0 ? "解锁" : "提升")}秘传《{secretId}》至{current + 1}重";
                else
                {
                    string missing = ownMissing > 0 ? $"{MartialArtCatalog.SchoolName(art.school)}{ownMissing}重" : string.Empty;
                    if (otherMissing > 0) missing += (missing.Length > 0 ? "、" : string.Empty) + $"{MartialArtCatalog.SchoolName(other)}{otherMissing}重";
                    closest = $"选后距《{secretId}》{current + 1}重：还差{missing}";
                }
            }
            string tag = EnemyMatchupInsight.ChoiceTag(id);
            if (!string.IsNullOrEmpty(tag)) link = tag;
            return closest == null ? link : link + "\n" + closest;
        }
    }
}
