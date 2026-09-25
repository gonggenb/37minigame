using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>Rewards use a baked route from spawn, never distance travelled this run.</summary>
    public static class ExplorationRewardTuning
    {
        public const float MidRouteDistance = 35f;
        public const float FarRouteDistance = 70f;
        public const int MaxRankFallbackCopper = 20;

        public static int Tier(float routeDistance) => routeDistance >= FarRouteDistance ? 2 :
            routeDistance >= MidRouteDistance ? 1 : 0;

        public static float Multiplier(float routeDistance) => Tier(routeDistance) switch
        {
            2 => 2f,
            1 => 1.5f,
            _ => 1f
        };

        public static int Scale(int baseReward, float routeDistance) =>
            Mathf.RoundToInt(baseReward * Multiplier(routeDistance));
    }
}
