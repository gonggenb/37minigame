using System;
using System.Collections.Generic;

namespace WuxiaRoguelite.Player
{
    [Serializable]
    public sealed class RunRelicDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public string iconId;

        public RunRelicDefinition(string id, string displayName, string description, string iconId)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.iconId = iconId;
        }
    }

    [Serializable]
    public sealed class RunConsumableDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public string iconId;

        public RunConsumableDefinition(string id, string displayName, string description, string iconId)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.iconId = iconId;
        }
    }

    public static class RunContentCatalog
    {
        public static readonly string[] AllRelicIds =
        {
            "compass", "abacus", "meditation_mat", "broken_sword_tassel",
            "toad_jade", "mountain_bell", "shadow_jade", "blood_marrow_pearl"
        };

        public static readonly string[] AllConsumableIds =
        {
            "healing_salve", "tiger_bone_pill", "lightness_powder",
            "red_sun_pill", "foundation_pill", "insight_incense"
        };

        private static readonly Dictionary<string, RunRelicDefinition> Relics =
            new Dictionary<string, RunRelicDefinition>
            {
                { "compass", new RunRelicDefinition("compass", "寻龙司南", "本局移动速度 +8%", "relic_compass") },
                { "abacus", new RunRelicDefinition("abacus", "玲珑铁算盘", "立即获得 4 铜钱", "relic_abacus") },
                { "meditation_mat", new RunRelicDefinition("meditation_mat", "悟道蒲团", "立即获得 12 修为", "relic_meditation_mat") },
                { "broken_sword_tassel", new RunRelicDefinition("broken_sword_tassel", "断剑穗", "攻速 +0.08", "relic_broken_sword_tassel") },
                { "toad_jade", new RunRelicDefinition("toad_jade", "毒蟾玉", "吸血 +3%", "relic_toad_jade") },
                { "mountain_bell", new RunRelicDefinition("mountain_bell", "镇岳铃", "防御 +1.5", "relic_mountain_bell") },
                { "shadow_jade", new RunRelicDefinition("shadow_jade", "影纹玉佩", "闪避 +4%", "relic_shadow_jade") },
                { "blood_marrow_pearl", new RunRelicDefinition("blood_marrow_pearl", "血髓珠", "最大气血 +12%", "relic_blood_marrow_pearl") }
            };

        private static readonly Dictionary<string, RunConsumableDefinition> Consumables =
            new Dictionary<string, RunConsumableDefinition>
            {
                { "healing_salve", new RunConsumableDefinition("healing_salve", "金疮药", "恢复 45% 最大气血", "consumable_healing_salve") },
                { "tiger_bone_pill", new RunConsumableDefinition("tiger_bone_pill", "虎骨丸", "本局防御 +1.5", "consumable_tiger_bone_pill") },
                { "lightness_powder", new RunConsumableDefinition("lightness_powder", "轻灵散", "本局移动速度 +12%", "consumable_lightness_powder") },
                { "red_sun_pill", new RunConsumableDefinition("red_sun_pill", "赤阳丹", "本局攻击 +12%", "consumable_red_sun_pill") },
                { "foundation_pill", new RunConsumableDefinition("foundation_pill", "培元丹", "最大气血 +10%", "consumable_foundation_pill") },
                { "insight_incense", new RunConsumableDefinition("insight_incense", "悟道香", "立即获得 18 修为", "consumable_insight_incense") }
            };

        public static RunRelicDefinition GetRelic(string id)
        {
            return !string.IsNullOrEmpty(id) && Relics.TryGetValue(id, out RunRelicDefinition value)
                ? value
                : null;
        }

        public static RunConsumableDefinition GetConsumable(string id)
        {
            return !string.IsNullOrEmpty(id) && Consumables.TryGetValue(id, out RunConsumableDefinition value)
                ? value
                : null;
        }
    }
}
