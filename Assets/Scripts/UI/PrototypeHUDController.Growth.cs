using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class PrototypeHUDController
    {
        private int growthRun = -1, observedCultivation, growthAmount;
        private float growthAge = 2f;
        // Inspector fallback for reduced-motion QA without changing combat effects or time.
        public bool reduceGrowthMotion;

        private bool GrowthVisible => gameFlow != null && playerStats != null &&
            growthRun == playerStats.RunRevision && !gameFlow.IsChallengeBriefingActive &&
            !settingsOpen && !characterPanelOpen && !challengeLedgerOpen &&
            !LevelLoadingScreen.IsLoading && !StudioSplashScreen.IsBlocking &&
            !gameFlow.IsOpeningIntroActive && !gameFlow.IsBossIntroActive &&
            !gameFlow.IsTutorialNoticeActive && !gameFlow.IsTutorialLessonActive &&
            gameFlow.CurrentPhase != GamePhase.Ready && gameFlow.CurrentPhase != GamePhase.Result;

        private void UpdateGrowthFeedback()
        {
            if (playerStats == null) return;
            if (growthRun != playerStats.RunRevision)
            {
                growthRun = playerStats.RunRevision;
                observedCultivation = 0;
                growthAge = 2f;
                growthAmount = 0;
            }
            int gained = playerStats.CultivationEarned - observedCultivation;
            observedCultivation = playerStats.CultivationEarned;
            if (gained > 0)
            {
                growthAmount = growthAge < 1.8f ? growthAmount + gained : gained;
                growthAge = 0;
            }
            if (GrowthVisible) growthAge += Time.unscaledDeltaTime;
        }

        private void DrawGrowthFeedback()
        {
            if (!GrowthVisible || growthAge >= 1.8f || growthAmount <= 0) return;
            Rect s = ResponsiveGui.SafeArea;
            bool choosing = gameFlow.CurrentPhase == GamePhase.LevelUpPaused;
            float width = Mathf.Min(292, s.width - 32);
            // Short upward drift ties the award to the HUD; it never delays reward application.
            float drift = reduceGrowthMotion ? 0 : 18 * Mathf.SmoothStep(0, 1, growthAge / .7f);
            Rect r = new Rect(s.center.x - width / 2, s.y + (choosing ? 8 : 154) - drift, width, 52);
            if (choosing) r.y = s.y + 8; // Keep a modal breakthrough banner inside the safe area.
            Color old = GUI.color;
            GUI.color = new Color(old.r, old.g, old.b, old.a * Mathf.Clamp01((1.8f - growthAge) / .3f));
            WuxiaUiTheme.DrawCompactSurface(r, Ink, Jade);
            if (cultivationHudIcon != null) GUI.DrawTexture(new Rect(r.x + 8, r.y + 10, 28, 28), cultivationHudIcon, ScaleMode.ScaleToFit, true);
            WuxiaUiComponents.Text(new Rect(r.x + 44, r.y + 2, r.width - 52, 26), $"修为 +{growthAmount}", 18, Paper);
            string progress = choosing ? "修为突破 · 可选武学" :
                $"再获 {Mathf.Max(0, playerStats.NextLevelRequirement - playerStats.cultivation)} 修为可选武学";
            WuxiaUiComponents.Text(new Rect(r.x + 44, r.y + 28, r.width - 52, 22), progress, 14, Gold);
            GUI.color = old;
        }

        private void DrawRunObjective()
        {
            if (!gameFlow.HasRunChallenge) return;
            Rect s = ResponsiveGui.SafeArea;
            float width = Mathf.Min(370, s.width - 32);
            Rect r = new Rect(s.center.x - width / 2, s.yMax - (ResponsiveGui.IsPortrait ? 196 : 116), width, 64);
            WuxiaUiTheme.DrawCompactSurface(r, Ink, Gold);
            WuxiaUiComponents.Text(new Rect(r.x + 10, r.y + 4, r.width - 20, 26),
                gameFlow.PursuitSummary, 15, Gold);
            string next = !gameFlow.midBossDefeated
                ? "备战中期强敌 · 修为升武学，装备补打法"
                : "备战最终强敌 · " + RunChallengeCatalog.ApproachName(gameFlow.ChallengeRun.approach);
            WuxiaUiComponents.Text(new Rect(r.x + 10, r.y + 32, r.width - 20, 24), next, 14, Paper);
        }

        private void DrawPursuitChoices(Rect area, bool opening)
        {
            WuxiaUiComponents.Text(new Rect(area.x, area.y, area.width, 26),
                "本局装备追求 · 营地击败两个不同目标", 16, Gold);
            bool portrait = ResponsiveGui.IsPortrait;
            float width = portrait ? area.width : (area.width - 16) / 3;
            for (int i = 0; i < RunPursuitCatalog.Count; i++)
            {
                var item = playerStats.equipment?.GetTemplate(RunPursuitCatalog.ItemId(i));
                if (item == null) continue;
                Rect card = new Rect(area.x + (portrait ? 0 : i * (width + 8)),
                    area.y + 34 + (portrait ? i * 90 : 0), width, portrait ? 82 : 108);
                bool selected = i == gameFlow.SelectedPursuit;
                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && opening;
                if (GUI.Button(card, GUIContent.none, selected ? activeTabStyle : actionButtonStyle)) gameFlow.SelectRunPursuit(i);
                GUI.enabled = wasEnabled;
                var icon = FindEquipmentIcon(item.id);
                if (icon != null) GUI.DrawTexture(new Rect(card.x + 8, card.y + 8, 36, 36), icon, ScaleMode.ScaleToFit, true);
                WuxiaUiComponents.Text(new Rect(card.x + 50, card.y + 6, card.width - 58, 26),
                    (selected ? "已选·" : "") + RunPursuitCatalog.Name(i) + "·" + item.displayName, 14, Paper);
                WuxiaUiComponents.Text(new Rect(card.x + 10, card.y + 46, card.width - 20, card.height - 48),
                    item.effectSummary, 14, Gold, TextAnchor.UpperLeft, true);
            }
            WuxiaUiComponents.Text(new Rect(area.x, area.yMax - 28, area.width, 28),
                opening ? "可与任意功法搭配；已持有目标时改给未持有装备。" : gameFlow.PursuitSummary,
                13, Muted, TextAnchor.UpperLeft, true);
        }
    }
}
