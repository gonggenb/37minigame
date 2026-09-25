namespace WuxiaRoguelite.Runtime
{
    // A separate equipment pursuit, compatible with every martial-art school.
    public static class RunPursuitCatalog
    {
        public const int Count = 3;
        public static string ItemId(int index) => index switch
        {
            1 => "black_tortoise_armor",
            2 => "poison_needle_case",
            _ => "bone_rot_gloves"
        };
        public static string Name(int index) => index switch
        {
            1 => "护体",
            2 => "淬毒",
            _ => "破甲"
        };
    }
}
