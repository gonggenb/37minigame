using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Battle
{
    public enum EnemyTrait { None, HeavyOpening, VenomBite, OpeningArmor }

    public static class EnemyTraits
    {
        public const float OpeningAttackMultiplier = 1.4f;
        public const float ArmorHealthRatio = .12f;
        public const int VenomTicks = 3;
        public const float VenomAttackRatio = .25f;

        public static EnemyTrait ForVisual(string id) => id switch
        {
            "iron_tusk_boar" => EnemyTrait.HeavyOpening,
            "scarlet_viper" => EnemyTrait.VenomBite,
            "bamboo_puppet" or "ballista" or "rider" => EnemyTrait.OpeningArmor,
            _ => EnemyTrait.None
        };

        public static string Label(EnemyTrait trait) => trait switch
        {
            EnemyTrait.HeavyOpening => "重撞·首击增强",
            EnemyTrait.VenomBite => "毒咬·短时毒伤",
            EnemyTrait.OpeningArmor => "机关·开场护甲",
            _ => string.Empty
        };
    }
}
