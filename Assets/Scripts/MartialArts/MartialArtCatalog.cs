using System;
using System.Collections.Generic;

namespace WuxiaRoguelite.MartialArts
{
    [Serializable]
    public sealed class MartialArtDefinition
    {
        public string id;
        public string category;
        public string effectSummary;
        public string description;

        public MartialArtDefinition(string id, string category, string effectSummary, string description)
        {
            this.id = id;
            this.category = category;
            this.effectSummary = effectSummary;
            this.description = description;
        }
    }

    public static class MartialArtCatalog
    {
        private static readonly Dictionary<string, MartialArtDefinition> Definitions =
            new Dictionary<string, MartialArtDefinition>
            {
                {
                    "剑气诀",
                    new MartialArtDefinition("剑气诀", "外功",
                        "攻击 +5",
                        "凝气附于剑锋，直接提高每次自动攻击造成的伤害。")
                },
                {
                    "疾剑式",
                    new MartialArtDefinition("疾剑式", "身法",
                        "攻速 +0.18",
                        "缩短连续出剑的间隔，适合依靠高频攻击触发暴击与吸血。")
                },
                {
                    "铁布衫",
                    new MartialArtDefinition("铁布衫", "内功",
                        "最大气血 +25% · 防御 +2",
                        "强化护体真气；增加的气血会立即补充到当前气血。")
                },
                {
                    "吸星诀",
                    new MartialArtDefinition("吸星诀", "内功",
                        "吸血 +8%",
                        "每次造成伤害时按比例恢复气血，攻击越高收益越明显。")
                },
                {
                    "毒砂掌",
                    new MartialArtDefinition("毒砂掌", "外功",
                        "攻击 +3 · 暴击 +3%",
                        "以毒砂扰乱对手气机，同时提高基础伤害与暴击机会。")
                },
                {
                    "破甲掌",
                    new MartialArtDefinition("破甲掌", "外功",
                        "攻击 +7",
                        "当前原型效果为高额直接攻击提升，尚未加入独立破甲状态。")
                }
            };

        public static MartialArtDefinition Get(string id)
        {
            return !string.IsNullOrEmpty(id) && Definitions.TryGetValue(id, out MartialArtDefinition definition)
                ? definition
                : null;
        }
    }
}
