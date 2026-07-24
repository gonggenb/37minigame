using System;
using System.Collections.Generic;

namespace WuxiaRoguelite.MartialArts
{
    public enum MartialArtSchool
    {
        SwiftSword,
        VenomPalm,
        IronBody
    }

    [Serializable]
    public sealed class MartialArtDefinition
    {
        public string id;
        public string category;
        public MartialArtSchool school;
        public bool isStarter;
        public int maxRank;
        public string effectSummary;
        public string description;
        public string[] rankEffectSummaries;

        public MartialArtDefinition(
            string id,
            string category,
            MartialArtSchool school,
            bool isStarter,
            string description,
            params string[] rankEffectSummaries)
        {
            this.id = id;
            this.category = category;
            this.school = school;
            this.isStarter = isStarter;
            this.description = description;
            this.rankEffectSummaries = rankEffectSummaries ?? Array.Empty<string>();
            maxRank = Math.Max(1, this.rankEffectSummaries.Length);
            effectSummary = this.rankEffectSummaries.Length > 0
                ? this.rankEffectSummaries[0]
                : "获得构筑效果";
        }

        public string GetEffectSummary(int rank)
        {
            if (rankEffectSummaries == null || rankEffectSummaries.Length == 0)
            {
                return effectSummary;
            }

            int index = Math.Max(0, Math.Min(rankEffectSummaries.Length - 1, rank - 1));
            return rankEffectSummaries[index];
        }
    }

    public static class MartialArtCatalog
    {
        public static readonly string[] AllIds =
        {
            "剑气诀", "疾剑式", "破甲掌",
            "毒砂掌", "百毒心经", "吸星诀",
            "铁布衫", "金钟罩", "反震诀"
        };

        private static readonly Dictionary<string, MartialArtDefinition> Definitions =
            new Dictionary<string, MartialArtDefinition>
            {
                {
                    "剑气诀",
                    new MartialArtDefinition(
                        "剑气诀", "外功", MartialArtSchool.SwiftSword, true,
                        "快剑流核心。以攻击次数积蓄剑势，稳定追加无视防御的剑气伤害。",
                        "每 3 次命中追加 60% 攻击剑气",
                        "每 2 次命中追加 80% 攻击剑气",
                        "每 2 次命中追加 100% 攻击剑气")
                },
                {
                    "疾剑式",
                    new MartialArtDefinition(
                        "疾剑式", "身法", MartialArtSchool.SwiftSword, false,
                        "加快自动攻击频率，更快触发剑气、破甲、毒伤与装备效果。",
                        "攻速 +0.12",
                        "攻速再 +0.12",
                        "攻速再 +0.12")
                },
                {
                    "破甲掌",
                    new MartialArtDefinition(
                        "破甲掌", "外功", MartialArtSchool.SwiftSword, false,
                        "每次命中削弱当前敌人的防御，本场战斗持续有效。",
                        "每次命中破甲 0.35",
                        "每次命中破甲 0.70",
                        "每次命中破甲 1.05")
                },
                {
                    "毒砂掌",
                    new MartialArtDefinition(
                        "毒砂掌", "外功", MartialArtSchool.VenomPalm, true,
                        "毒掌流核心。每次命中叠加毒层，毒伤每秒结算一次。",
                        "命中施加 1 层毒",
                        "命中施加 2 层毒",
                        "命中施加 3 层毒")
                },
                {
                    "百毒心经",
                    new MartialArtDefinition(
                        "百毒心经", "心法", MartialArtSchool.VenomPalm, false,
                        "提高毒层上限和每层伤害，让高频施毒可以持续成长。",
                        "毒上限 +4 · 每层伤害 +0.25",
                        "毒上限再 +4 · 每层伤害再 +0.25",
                        "毒上限再 +4 · 每层伤害再 +0.25")
                },
                {
                    "吸星诀",
                    new MartialArtDefinition(
                        "吸星诀", "内功", MartialArtSchool.VenomPalm, false,
                        "普攻与毒伤都能转化为恢复，使持续伤害构筑获得续航。",
                        "吸血 +4% · 毒伤回血 10%",
                        "吸血再 +4% · 毒伤回血 20%",
                        "吸血再 +4% · 毒伤回血 30%")
                },
                {
                    "铁布衫",
                    new MartialArtDefinition(
                        "铁布衫", "内功", MartialArtSchool.IronBody, true,
                        "铁壁流核心。提高气血和防御，并立即补充新增的气血。",
                        "最大气血 +15% · 防御 +1",
                        "最大气血再 +15% · 防御再 +1",
                        "最大气血再 +15% · 防御再 +1")
                },
                {
                    "金钟罩",
                    new MartialArtDefinition(
                        "金钟罩", "内功", MartialArtSchool.IronBody, false,
                        "每场战斗开始获得护盾；防御越高，护盾越厚。",
                        "开战护盾：8 + 1.5×防御",
                        "开战护盾：16 + 3×防御",
                        "开战护盾：24 + 4.5×防御")
                },
                {
                    "反震诀",
                    new MartialArtDefinition(
                        "反震诀", "心法", MartialArtSchool.IronBody, false,
                        "受到实际伤害时以自身防御反击，形成攻防一体的成长路线。",
                        "受击反伤：0.65×防御",
                        "受击反伤：0.85×防御",
                        "受击反伤：1.05×防御")
                }
            };

        public static MartialArtDefinition Get(string id)
        {
            return !string.IsNullOrEmpty(id) && Definitions.TryGetValue(id, out MartialArtDefinition definition)
                ? definition
                : null;
        }

        public static string SchoolName(MartialArtSchool school)
        {
            switch (school)
            {
                case MartialArtSchool.SwiftSword:
                    return "快剑";
                case MartialArtSchool.VenomPalm:
                    return "毒掌";
                default:
                    return "铁壁";
            }
        }
    }
}
