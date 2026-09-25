using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.GameFlow
{
    public partial class GameFlowController
    {
        private EncounterTrigger pendingRouteEncounter, currentRouteCave;
        private readonly HashSet<EncounterTrigger> routeVictories = new HashSet<EncounterTrigger>();
        private readonly HashSet<RouteSpecialty> claimedRoutes = new HashSet<RouteSpecialty>();
        public int SelectedPursuit { get; private set; }
        public bool PursuitComplete => claimedRoutes.Contains(RouteSpecialty.Camp);
        public int CampVictoryCount => routeVictories.Count(e => e != null && PingchuanRouteCatalog.ForEncounter(e) == RouteSpecialty.Camp);
        public void SelectRunPursuit(int index)
        {
            if (!HasRunChallenge || !IsChallengeBriefingActive || !isOpeningMartialArtChoice ||
                CurrentPhase != GamePhase.LevelUpPaused || index < 0 || index >= RunPursuitCatalog.Count) return;
            SelectedPursuit = index;
        }
        public string PursuitSummary => PursuitComplete
            ? $"装备追求已达成 · {RunPursuitCatalog.Name(SelectedPursuit)}"
            : $"营地两胜 {CampVictoryCount}/2 → {playerStats.equipment?.GetTemplate(RunPursuitCatalog.ItemId(SelectedPursuit))?.displayName}";
        public bool HasRouteSpecialties => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == LevelSequence.LevelTwoSceneName;
        public string RouteOpeningHint => HasRouteSpecialties && isOpeningMartialArtChoice ? "东侧练武场精进 · 南侧营地装备 · 洞穴跨派补修" : string.Empty;

        public string RouteProgress(RouteSpecialty route)
        {
            string period = IsEndlessMode ? "每轮一次" : "每局一次";
            if (claimedRoutes.Contains(route)) return IsEndlessMode ? "本轮奖励已领取" : "本局奖励已领取";
            if (route == RouteSpecialty.Cave) return "完成事件后离洞领取·" + period;
            int count = routeVictories.Count(e => e != null && PingchuanRouteCatalog.ForEncounter(e) == route);
            return $"连胜进度 {count}/{PingchuanRouteCatalog.RequiredWins} · {period}";
        }

        private void ResetRouteProgress(bool resetRunReview = true)
        {
            pendingRouteEncounter = currentRouteCave = null;
            if (resetRunReview) SelectedPursuit = 0;
            routeVictories.Clear(); claimedRoutes.Clear();
            if (resetRunReview) battleManager?.ResetRunReview();
            if (!HasRouteSpecialties) return;
            var encounters = FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var route in new[] { RouteSpecialty.Practice, RouteSpecialty.Camp, RouteSpecialty.Cave })
            {
                var e = encounters.Where(x => PingchuanRouteCatalog.ForEncounter(x) == route)
                    .OrderBy(x => Vector3.Distance(x.transform.position, playerController.transform.position)).FirstOrDefault();
                if (e == null) continue;
                string name = "RouteGuide_" + route;
                if (GameObject.Find(name) != null) continue;
                var go = new GameObject(name);
                go.transform.position = e.transform.position + new Vector3(-2, 0, 1);
                var guide = go.AddComponent<MainMapRegionGuide>();
                guide.regionName = PingchuanRouteCatalog.Name(route); guide.specialty = route;
                guide.routeTheme = PingchuanRouteCatalog.Reward(route); guide.riskLabel = IsEndlessMode ? "每轮一次" : "每局一次";
                guide.detailDistance = 15; guide.maxVisibleDistance = 22;
            }
        }

        private string ResolveRouteVictory()
        {
            var e = pendingRouteEncounter; pendingRouteEncounter = null;
            var route = PingchuanRouteCatalog.ForEncounter(e);
            if (!HasRouteSpecialties || route == RouteSpecialty.None || !routeVictories.Add(e)) return string.Empty;
            if (claimedRoutes.Contains(route)) return string.Empty;
            if (routeVictories.Count(x => PingchuanRouteCatalog.ForEncounter(x) == route) < PingchuanRouteCatalog.RequiredWins)
                return $"{PingchuanRouteCatalog.Name(route)}：{RouteProgress(route)}";
            claimedRoutes.Add(route);
            string reward = route == RouteSpecialty.Practice ? playerStats.UpgradeRandomMartialArt() : GrantPursuitEquipment();
            if (string.IsNullOrEmpty(reward))
            {
                playerStats.GainCopper(ExplorationRewardTuning.MaxRankFallbackCopper);
                reward = $"无可领取项目，铜钱 +{ExplorationRewardTuning.MaxRankFallbackCopper}";
            }
            return $"{PingchuanRouteCatalog.Name(route)}达成：{reward}";
        }

        private string GrantPursuitEquipment()
        {
            var equipment = playerStats.equipment;
            if (equipment == null) return string.Empty;
            string id = RunPursuitCatalog.ItemId(SelectedPursuit);
            // Already found the target? Award another unowned item; never duplicate or discard gear.
            if (equipment.HasItem(id)) return equipment.AddTreasureItem();
            string reward = equipment.AddItemById(id);
            var item = equipment.inventory.Find(x => x.id == id);
            return item == null ? reward : reward + " · " + item.effectSummary;
        }

        private string ResolveRouteCave(bool completed)
        {
            var e = currentRouteCave; currentRouteCave = null;
            if (!HasRouteSpecialties || !completed || e == null || !claimedRoutes.Add(RouteSpecialty.Cave)) return string.Empty;
            // Choose a compatible secondary school nearest to its first secret. Never downgrade learned ranks.
            var main = GetDominantSchool();
            var partnerSchools = MartialArtCatalog.AllSecretIds.Select(MartialArtCatalog.GetSecret)
                .Where(s => s.firstSchool == main || s.secondSchool == main)
                .Select(s => s.firstSchool == main ? s.secondSchool : s.firstSchool)
                .OrderByDescending(s => playerStats.GetMartialArtSchoolRank(s)).ToArray();
            var candidates = partnerSchools.SelectMany(s => allMartialArts.Where(id =>
                MartialArtCatalog.Get(id).school == s && MartialArtCatalog.Get(id).isStarter &&
                playerStats.GetMartialArtRank(id) < MartialArtCatalog.Get(id).maxRank)).ToArray();
            if (candidates.Length == 0)
            {
                playerStats.GainCopper(ExplorationRewardTuning.MaxRankFallbackCopper);
                return "洞穴补修已满，铜钱 +20";
            }
            string art = candidates[0]; int before = playerStats.GetMartialArtRank(art);
            playerStats.ApplyMartialArt(art); playerStats.ApplyMartialArt(art);
            return $"洞穴传承：《{art}》{before} → {playerStats.GetMartialArtRank(art)}重";
        }

        public void RetryCurrentLevel()
        {
            if (CurrentPhase != GamePhase.Result || LevelLoadingScreen.IsLoading) return;
            Time.timeScale = 1;
            if (IsTutorialLevel)
            {
                StartRun(); ClearOpeningIntro(); SetPhase(GamePhase.Ready);
                IsTutorialNoticeActive = true; tutorialNoticeOpenedAt = Time.unscaledTime;
                return;
            }
            BeginLevelTwoAfterTransition();
        }
    }
}
