using System;
using System.Collections.Generic;

namespace WuxiaRoguelite.MartialArts
{
    public enum MartialArtSchool
    {
        SwiftSword,
        VenomPalm,
        IronBody,
        ShadowSteps,
        BloodBlade
    }

    [Serializable]
    public sealed class MartialArtDefinition
    {
        public string id;
        public string category;
        public MartialArtSchool school;
        public bool isStarter;
        public bool isCapstone;
        public int requiredSchoolRank;
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
            string[] rankEffectSummaries,
            bool isCapstone = false,
            int requiredSchoolRank = 0)
        {
            this.id = id;
            this.category = category;
            this.school = school;
            this.isStarter = isStarter;
            this.description = description;
            this.rankEffectSummaries = rankEffectSummaries ?? Array.Empty<string>();
            this.isCapstone = isCapstone;
            this.requiredSchoolRank = Math.Max(0, requiredSchoolRank);
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

    [Serializable]
    public sealed class MartialArtSecretDefinition
    {
        public string id;
        public MartialArtSchool firstSchool;
        public MartialArtSchool secondSchool;
        public string description;
        public string[] rankEffectSummaries;
        public int maxRank => rankEffectSummaries == null ? 1 : Math.Max(1, rankEffectSummaries.Length);

        public MartialArtSecretDefinition(
            string id,
            MartialArtSchool firstSchool,
            MartialArtSchool secondSchool,
            string description,
            params string[] rankEffectSummaries)
        {
            this.id = id;
            this.firstSchool = firstSchool;
            this.secondSchool = secondSchool;
            this.description = description;
            this.rankEffectSummaries = rankEffectSummaries ?? Array.Empty<string>();
        }

        public string GetEffectSummary(int rank)
        {
            if (rankEffectSummaries == null || rankEffectSummaries.Length == 0)
            {
                return description;
            }

            int index = Math.Max(0, Math.Min(rankEffectSummaries.Length - 1, rank - 1));
            return rankEffectSummaries[index];
        }
    }

    public static class MartialArtCatalog
    {
        public static readonly string[] AllIds =
        {
            "剑气诀", "疾剑式", "破甲掌", "无影连环剑",
            "毒砂掌", "百毒心经", "吸星诀", "化功毒雾",
            "铁布衫", "金钟罩", "反震诀", "不动明王身",
            "踏雪无痕", "惊鸿一式", "流云步", "无相残影",
            "饮血刀法", "血战八方", "沸血诀", "修罗血域"
        };

        public static readonly string[] AllSecretIds =
        {
            "青锋淬毒", "以毒养血", "血铸金身", "虚实金钟", "无影追风"
        };

        private static MartialArtDefinition Art(
            string id, string category, MartialArtSchool school, bool starter,
            string description, bool capstone, int requiredSchoolRank, params string[] ranks)
        {
            return new MartialArtDefinition(
                id, category, school, starter, description, ranks, capstone, requiredSchoolRank);
        }

        private static readonly Dictionary<string, MartialArtDefinition> Definitions =
            new Dictionary<string, MartialArtDefinition>
            {
                { "剑气诀", Art("剑气诀", "外功", MartialArtSchool.SwiftSword, true,
                    "以攻击次数积蓄剑势，稳定追加无视防御的剑气伤害。", false, 0,
                    "每 3 次命中追加 60% 攻击剑气", "每 2 次命中追加 80% 攻击剑气", "每 2 次命中追加 100% 攻击剑气") },
                { "疾剑式", Art("疾剑式", "身法", MartialArtSchool.SwiftSword, false,
                    "加快自动攻击频率，更快触发命中类效果。", false, 0,
                    "攻速 +0.12", "攻速再 +0.12", "攻速再 +0.12") },
                { "破甲掌", Art("破甲掌", "外功", MartialArtSchool.SwiftSword, false,
                    "每次命中削弱当前敌人的防御。", false, 0,
                    "每次命中破甲 0.35", "每次命中破甲 0.70", "每次命中破甲 1.05") },
                { "无影连环剑", Art("无影连环剑", "绝学", MartialArtSchool.SwiftSword, false,
                    "快剑流绝学，连击终段爆发并缩短下一次出手。", true, 5,
                    "每 5 击追加 90% 攻击并提速", "每 4 击追加 110% 攻击并提速", "每 3 击追加 130% 攻击并提速") },

                { "毒砂掌", Art("毒砂掌", "外功", MartialArtSchool.VenomPalm, true,
                    "每次命中叠加毒层，毒伤每秒结算一次。", false, 0,
                    "命中施加 1 层毒", "命中施加 2 层毒", "命中施加 3 层毒") },
                { "百毒心经", Art("百毒心经", "心法", MartialArtSchool.VenomPalm, false,
                    "提高毒层上限和每层伤害。", false, 0,
                    "毒上限 +4 · 每层伤害 +0.25", "毒上限再 +4 · 每层伤害再 +0.25", "毒上限再 +4 · 每层伤害再 +0.25") },
                { "吸星诀", Art("吸星诀", "内功", MartialArtSchool.VenomPalm, false,
                    "普攻与毒伤都能转化为恢复。", false, 0,
                    "吸血 +4% · 毒伤回血 10%", "吸血再 +4% · 毒伤回血 20%", "吸血再 +4% · 毒伤回血 30%") },
                { "化功毒雾", Art("化功毒雾", "绝学", MartialArtSchool.VenomPalm, false,
                    "毒发时腐蚀防御，并按现有毒层追加伤害。", true, 5,
                    "毒发追加 20% 伤害并破甲", "毒发追加 35% 伤害并破甲", "毒发追加 50% 伤害并破甲") },

                { "铁布衫", Art("铁布衫", "内功", MartialArtSchool.IronBody, true,
                    "提高气血和防御，并立即补充新增气血。", false, 0,
                    "最大气血 +15% · 防御 +1", "最大气血再 +15% · 防御再 +1", "最大气血再 +15% · 防御再 +1") },
                { "金钟罩", Art("金钟罩", "内功", MartialArtSchool.IronBody, false,
                    "每场战斗开始获得随防御成长的护盾。", false, 0,
                    "开战护盾：8 + 1.5×防御", "开战护盾：16 + 3×防御", "开战护盾：24 + 4.5×防御") },
                { "反震诀", Art("反震诀", "心法", MartialArtSchool.IronBody, false,
                    "受到实际伤害时以自身防御反击。", false, 0,
                    "受击反伤：0.65×防御", "受击反伤：0.85×防御", "受击反伤：1.05×防御") },
                { "不动明王身", Art("不动明王身", "绝学", MartialArtSchool.IronBody, false,
                    "铁壁流绝学，减免伤害并强化开战护盾。", true, 5,
                    "减伤 8% · 开战护盾 +12", "减伤 14% · 开战护盾 +24", "减伤 20% · 开战护盾 +36") },

                { "踏雪无痕", Art("踏雪无痕", "身法", MartialArtSchool.ShadowSteps, true,
                    "轻身流核心，提高闪避并以闪避撬动后续收益。", false, 0,
                    "闪避 +4%", "闪避再 +4%", "闪避再 +4%") },
                { "惊鸿一式", Art("惊鸿一式", "外功", MartialArtSchool.ShadowSteps, false,
                    "每场战斗的首击造成额外伤害。", false, 0,
                    "首击伤害 +45%", "首击伤害 +70%", "首击伤害 +95%") },
                { "流云步", Art("流云步", "身法", MartialArtSchool.ShadowSteps, false,
                    "提高移动与出手速度，适合短时间路线压缩。", false, 0,
                    "移速 +8% · 攻速 +0.06", "移速再 +8% · 攻速再 +0.06", "移速再 +8% · 攻速再 +0.06") },
                { "无相残影", Art("无相残影", "绝学", MartialArtSchool.ShadowSteps, false,
                    "轻身流绝学，周期性留下残影规避一次攻击。", true, 5,
                    "每 6 次敌袭必定闪避", "每 5 次敌袭必定闪避", "每 4 次敌袭必定闪避") },

                { "饮血刀法", Art("饮血刀法", "外功", MartialArtSchool.BloodBlade, true,
                    "血刀流核心，以持续吸血支撑高风险压血打法。", false, 0,
                    "吸血 +3%", "吸血再 +3%", "吸血再 +3%") },
                { "血战八方", Art("血战八方", "外功", MartialArtSchool.BloodBlade, false,
                    "气血越低，造成的伤害越高。", false, 0,
                    "半血以下伤害 +18%", "半血以下伤害 +30%", "半血以下伤害 +42%") },
                { "沸血诀", Art("沸血诀", "心法", MartialArtSchool.BloodBlade, false,
                    "永久提高攻击与攻速，让回血转化为进攻节奏。", false, 0,
                    "攻击 +8% · 攻速 +0.05", "攻击再 +8% · 攻速再 +0.05", "攻击再 +8% · 攻速再 +0.05") },
                { "修罗血域", Art("修罗血域", "绝学", MartialArtSchool.BloodBlade, false,
                    "血刀流绝学，低血量暴击并从爆发中获得额外恢复。", true, 5,
                    "半血以下暴击 +10% · 吸血 +3%", "暴击 +16% · 吸血 +5%", "暴击 +22% · 吸血 +7%") }
            };

        private static readonly Dictionary<string, MartialArtSecretDefinition> Secrets =
            new Dictionary<string, MartialArtSecretDefinition>
            {
                { "青锋淬毒", new MartialArtSecretDefinition("青锋淬毒", MartialArtSchool.SwiftSword, MartialArtSchool.VenomPalm,
                    "剑气也会淬入剧毒。", "剑气触发时额外施加 1 层毒", "剑气触发时额外施加 2 层毒") },
                { "以毒养血", new MartialArtSecretDefinition("以毒养血", MartialArtSchool.VenomPalm, MartialArtSchool.BloodBlade,
                    "毒发会返还更多气血。", "毒伤额外回复 8%", "毒伤额外回复 16%") },
                { "血铸金身", new MartialArtSecretDefinition("血铸金身", MartialArtSchool.BloodBlade, MartialArtSchool.IronBody,
                    "低血量时获得额外减伤。", "半血以下额外减伤 6%", "半血以下额外减伤 12%") },
                { "虚实金钟", new MartialArtSecretDefinition("虚实金钟", MartialArtSchool.IronBody, MartialArtSchool.ShadowSteps,
                    "闪避时重铸部分护盾。", "闪避恢复 4% 最大气血等值护盾", "闪避恢复 8% 最大气血等值护盾") },
                { "无影追风", new MartialArtSecretDefinition("无影追风", MartialArtSchool.ShadowSteps, MartialArtSchool.SwiftSword,
                    "闪避后立刻抢回出手机会。", "闪避后攻击冷却缩短 35%", "闪避后攻击冷却缩短 70%") }
            };

        public static MartialArtDefinition Get(string id)
        {
            return !string.IsNullOrEmpty(id) && Definitions.TryGetValue(id, out MartialArtDefinition definition)
                ? definition
                : null;
        }

        public static MartialArtSecretDefinition GetSecret(string id)
        {
            return !string.IsNullOrEmpty(id) && Secrets.TryGetValue(id, out MartialArtSecretDefinition definition)
                ? definition
                : null;
        }

        public static string SchoolName(MartialArtSchool school)
        {
            switch (school)
            {
                case MartialArtSchool.SwiftSword: return "快剑";
                case MartialArtSchool.VenomPalm: return "毒掌";
                case MartialArtSchool.IronBody: return "铁壁";
                case MartialArtSchool.ShadowSteps: return "轻身";
                default: return "血刀";
            }
        }
    }
}
