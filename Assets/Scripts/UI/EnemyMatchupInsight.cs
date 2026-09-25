using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Player;

namespace WuxiaRoguelite.UI
{
    // Explanations of real combat rules, not hidden damage bonuses or a win prediction.
    public static class EnemyMatchupInsight
    {
        public static string Weakness(EnemyTrait trait) => trait switch
        {
            EnemyTrait.HeavyOpening => "（弱点）护盾挡首击，闪避避重撞",
            EnemyTrait.VenomBite => "（弱点）闪避毒咬可阻止挂毒",
            EnemyTrait.OpeningArmor => "（弱点）连击、毒伤均可削盾",
            _ => string.Empty
        };

        public static string Counter(EnemyTrait trait, PlayerStats player)
        {
            if (player?.runtimeStats == null) return string.Empty;
            var gear = player.equipment;
            bool shield = player.GetMartialArtRank("金钟罩") > 0 || (gear != null && gear.GetOpeningShield() > 0);
            bool dodge = player.runtimeStats.dodgeChance > player.baseStats.dodgeChance + .001f;
            bool poison = player.GetMartialArtRank("毒砂掌") > 0 || (gear != null && gear.GetPoisonStacksPerHit() > 0);
            return trait switch
            {
                EnemyTrait.HeavyOpening when shield => "（克制）已有开场护盾",
                EnemyTrait.HeavyOpening when dodge => "（克制）已强化闪避",
                EnemyTrait.VenomBite when dodge => "（克制）已强化闪避，非免毒",
                EnemyTrait.OpeningArmor when poison => "（克制）已有毒伤削盾",
                EnemyTrait.OpeningArmor when player.GetMartialArtRank("无影连环剑") > 0 => "（克制）已有连击削盾",
                _ => string.Empty
            };
        }

        public static string ChoiceTag(string id) => id switch
        {
            "金钟罩" => "（克制重撞）",
            "踏雪无痕" => "（应对重撞、毒咬）",
            "毒砂掌" => "（削减开场护甲）",
            _ => string.Empty
        };
    }
}
