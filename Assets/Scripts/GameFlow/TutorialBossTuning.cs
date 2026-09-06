using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.GameFlow
{
    public static class TutorialBossTuning
    {
        // Fixed, forgiving baseline: even a player who found no upgrades can win.
        public static CombatantStats CreateStats() => new CombatantStats
        {
            displayName = GameTextCatalog.TutorialBossName,
            visualId = GameTextCatalog.TutorialBossVisualId,
            level = 2,
            maxHealth = 100f,
            currentHealth = 100f,
            attack = 5f,
            defense = 1f,
            attackSpeed = 0.65f,
            critChance = 0f,
            dodgeChance = 0f
        };
    }
}
