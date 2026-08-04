using System.Collections.Generic;
using WuxiaRoguelite.Cave;

namespace WuxiaRoguelite.UI
{
    public static class ContentIconCatalog
    {
        private static readonly Dictionary<string, string> MartialArts = new Dictionary<string, string>
        {
            { "剑气诀", "art_sword_qi" }, { "疾剑式", "art_swift_sword" },
            { "破甲掌", "art_armor_break" }, { "无影连环剑", "art_shadow_chain_sword" },
            { "毒砂掌", "art_venom_palm" }, { "百毒心经", "art_hundred_venoms" },
            { "吸星诀", "art_star_drain" }, { "化功毒雾", "art_poison_mist" },
            { "铁布衫", "art_iron_shirt" }, { "金钟罩", "art_golden_bell" },
            { "反震诀", "art_retaliation" }, { "不动明王身", "art_immovable_king" },
            { "踏雪无痕", "art_snowless_step" }, { "惊鸿一式", "art_swan_strike" },
            { "流云步", "art_cloud_step" }, { "无相残影", "art_formless_shadow" },
            { "饮血刀法", "art_blood_drinking_blade" }, { "血战八方", "art_bloody_battle" },
            { "沸血诀", "art_boiling_blood" }, { "修罗血域", "art_asura_domain" },
            { "青锋淬毒", "secret_poisoned_edge" }, { "以毒养血", "secret_poison_blood" },
            { "血铸金身", "secret_blood_armor" }, { "虚实金钟", "secret_shadow_bell" },
            { "无影追风", "secret_wind_pursuit" }
        };

        private static readonly Dictionary<string, string> EquipmentIcons = new Dictionary<string, string>
        {
            { "qinggang_sword", "equipment_qinggang_sword" },
            { "light_scale", "equipment_light_scale" },
            { "practice_bracer", "equipment_practice_bracer" },
            { "black_iron_ring", "equipment_black_iron_ring" },
            { "wanderer_cloak", "equipment_wanderer_cloak" },
            { "poison_dart_pouch", "equipment_poison_dart_pouch" },
            { "wind_chaser_sword", "equipment_wind_chaser_sword" },
            { "bone_rot_gloves", "equipment_bone_rot_gloves" },
            { "black_tortoise_armor", "equipment_black_tortoise_armor" },
            { "nightwalker_cloak", "equipment_nightwalker_cloak" },
            { "blood_drinking_blade", "equipment_blood_drinking_blade" },
            { "poison_needle_case", "equipment_poison_needle_case" },
            { "mountain_bracer", "equipment_mountain_bracer" },
            { "swallow_boots", "equipment_swallow_boots" },
            { "crimson_heart_pendant", "equipment_crimson_heart_pendant" }
        };

        public static string MartialArt(string id)
        {
            return !string.IsNullOrEmpty(id) && MartialArts.TryGetValue(id, out string iconId)
                ? iconId
                : string.Empty;
        }

        public static string Equipment(string id)
        {
            return !string.IsNullOrEmpty(id) && EquipmentIcons.TryGetValue(id, out string iconId)
                ? iconId
                : string.Empty;
        }

        public static string Cave(CaveContentType content)
        {
            switch (content)
            {
                case CaveContentType.Enemy: return "cave_enemy";
                case CaveContentType.Merchant: return "cave_merchant";
                case CaveContentType.Treasure: return "cave_treasure";
                case CaveContentType.Altar: return "cave_altar";
                case CaveContentType.Trial: return "cave_trial";
                case CaveContentType.Healer: return "cave_healer";
                case CaveContentType.Library: return "cave_library";
                case CaveContentType.Forge: return "cave_forge";
                case CaveContentType.Gambler: return "cave_gambler";
                case CaveContentType.HerbGarden: return "cave_herb_garden";
                case CaveContentType.RelicShrine: return "cave_relic_shrine";
                default: return "cave_random";
            }
        }
    }
}
