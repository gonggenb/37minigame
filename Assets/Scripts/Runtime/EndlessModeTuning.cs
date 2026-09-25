using UnityEngine;

namespace WuxiaRoguelite.Runtime
{
    /// <summary>Always scale a fresh enemy copy, never an authored template or last round's copy.</summary>
    public static class EndlessModeTuning
    {
        public const float RoundSeconds = 60f;
        public const float HealthGrowth = 1.30f;
        public const float AttackGrowth = 1.20f;
        public const float DefenseGrowth = 1.12f;
        public const float AttackSpeedGrowth = .05f;

        public static void Apply(CombatantStats stats, int round)
        {
            if (stats == null || round <= 1) return;
            float steps = round - 1f;
            stats.level = (int)System.Math.Min(int.MaxValue, (long)stats.DisplayLevel + round - 1);
            stats.maxHealth = Scale(stats.maxHealth, HealthGrowth, steps);
            stats.attack = Scale(stats.attack, AttackGrowth, steps);
            float defenseOffset = 1f / (DefenseGrowth - 1f);
            stats.defense = Scale(stats.defense + defenseOffset, DefenseGrowth, steps) - defenseOffset;
            // Keep attack cadence playable and arithmetic finite even in synthetic extreme-round tests.
            stats.attackSpeed *= Mathf.Min(5f, 1f + AttackSpeedGrowth * steps);
            stats.ResetHealth();
        }

        private static float Scale(float value, float growth, float steps) =>
            (float)System.Math.Min(1e20, System.Math.Max(0, value) *
                System.Math.Pow(growth, System.Math.Min(steps, 500)));
    }
}
