using System;

namespace WuxiaRoguelite.Battle
{
    [Flags]
    public enum BattleVfxCue
    {
        None = 0,
        BasicHit = 1 << 0,
        CriticalHit = 1 << 1,
        Dodge = 1 << 2,
        ShadowDodge = 1 << 3,
        SwordQi = 1 << 4,
        SwiftCombo = 1 << 5,
        PoisonApplied = 1 << 6,
        PoisonTick = 1 << 7,
        PoisonMist = 1 << 8,
        ArmorBreak = 1 << 9,
        ShieldImpact = 1 << 10,
        Retaliation = 1 << 11,
        Heal = 1 << 12,
        OpeningStrike = 1 << 13,
        BloodPower = 1 << 14,
        BloodBurst = 1 << 15,
        Foxfire = 1 << 16,
        MountainBreaker = 1 << 17
    }
}
