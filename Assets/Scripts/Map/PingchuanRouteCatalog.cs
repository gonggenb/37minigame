using System;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.Map
{
    public enum RouteSpecialty { None, Practice, Camp, Cave }

    public static class PingchuanRouteCatalog
    {
        public const int RequiredWins = 2;
        public static RouteSpecialty ForEncounter(EncounterTrigger e)
        {
            if (e == null || e.gameObject.scene.name != LevelSequence.LevelTwoSceneName) return RouteSpecialty.None;
            if (e.encounterType == EncounterType.HiddenCave) return RouteSpecialty.Cave;
            // Stable authored placement slots shared with PingchuanTownLevelBuilder.
            int split = e.name.LastIndexOf(" PC ", StringComparison.Ordinal);
            if (split < 0 || !int.TryParse(e.name.Substring(split + 4), out int slot)) return RouteSpecialty.None;
            if ((slot >= 13 && slot <= 22) || slot == 45 || slot == 46) return RouteSpecialty.Practice;
            if ((slot >= 23 && slot <= 30) || slot == 47 || slot == 48) return RouteSpecialty.Camp;
            return RouteSpecialty.None;
        }
        public static string Name(RouteSpecialty route) => route switch
        {
            RouteSpecialty.Practice => GameTextCatalog.PracticeRouteName,
            RouteSpecialty.Camp => GameTextCatalog.CampRouteName,
            RouteSpecialty.Cave => GameTextCatalog.CaveRouteName,
            _ => string.Empty
        };
        public static string Reward(RouteSpecialty route) => route switch
        {
            RouteSpecialty.Practice => "两胜·已学武学升重",
            RouteSpecialty.Camp => "两胜·额外装备一件",
            RouteSpecialty.Cave => "首次完成·跨派补修两重",
            _ => string.Empty
        };
    }
}
