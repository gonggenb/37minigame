using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class PrototypeHUDController
    {
        private Vector2 reviewScroll;
        private void DrawRunReview()
        {
            FillRect(new Rect(0, 0, ResponsiveGui.Width, ResponsiveGui.Height), WithAlpha(WuxiaUiTheme.BackgroundInk, .94f));
            Rect p = PortraitUiLayout.Modal(ResponsiveGui.IsPortrait ? 790 : 490, ResponsiveGui.IsPortrait ? 492 : 660);
            bool won = gameFlow.IsTutorialCompletionSummary || gameFlow.bossDefeated;
            bool showChallengeActions = gameFlow.HasRunChallenge && !gameFlow.IsEndlessMode;
            DrawPanel(p, Ink, won ? Gold : Crimson, WuxiaPanelKind.Boss);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 14, p.width - 48, 38), gameFlow.IsEndlessMode ? GameTextCatalog.EndlessModeName + " · 此行战果" : gameFlow.IsGameCompleted ? GameTextCatalog.GameCompletedTitle + " · 此行战果" : won ? "闯关功成 · 此行战果" : "江湖路断 · 此行战果", 26);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 54, p.width - 48, 34), string.IsNullOrEmpty(gameFlow.ChallengeResult) ? gameFlow.statusMessage : gameFlow.ChallengeResult, 15,
                string.IsNullOrEmpty(gameFlow.ChallengeResult) ? Muted : Gold, TextAnchor.UpperLeft, true);
            Rect view = new Rect(p.x + 24, p.y + 96, p.width - 48, Mathf.Max(60, p.height - (ResponsiveGui.IsPortrait ? 272 : 240)));
            float width = view.width - 20;
            float contentHeight = 280 + playerStats.learnedMartialArts.Count * 46 + (gameFlow.HasRouteSpecialties ? 150 : 0);
            if (gameFlow.HasRunChallenge) contentHeight += 144;
            if (gameFlow.HasBossTalents) contentHeight += 84;
            if (gameFlow.IsEndlessMode) contentHeight += 144;
            reviewScroll = GUI.BeginScrollView(view, reviewScroll, new Rect(0, 0, width, Mathf.Max(view.height, contentHeight)));
            if (gameFlow.IsEndlessMode) DrawEndlessRewardSummary(width);
            GUI.BeginGroup(new Rect(0, gameFlow.IsEndlessMode ? 144 : 0, width, contentHeight));
            var review = gameFlow.battleManager.RunReview;
            WuxiaUiComponents.Text(new Rect(0, 0, width, 28), $"等级 {playerStats.level} · 击杀 {playerStats.killCount} · 洞穴 {playerStats.caveEntries} · 决战 {gameFlow.bossBattleTime:0.0}秒", 15);
            WuxiaUiComponents.Text(new Rect(0, 36, width, 24), "本局输出前三项（含破盾）", 17, Gold);
            WuxiaUiComponents.Text(new Rect(0, 62, width, 42), review.DamageSummary, 16, Paper, TextAnchor.UpperLeft, true);
            WuxiaUiComponents.Text(new Rect(0, 110, width, 46), review.DefenseSummary, 15, Paper, TextAnchor.UpperLeft, true);
            WuxiaUiComponents.Text(new Rect(0, 162, width, 42), review.EnemySummary, 16, won ? Gold : Crimson, TextAnchor.UpperLeft, true);
            WuxiaUiComponents.Text(new Rect(0, 207, width, 38), "按实际结算统计；吸血仅计有效回血。", 13, Muted, TextAnchor.UpperLeft, true);
            float y = 250;
            if (gameFlow.HasBossTalents)
            {
                string talents = "终局：" + BossTalentCatalog.Summary(gameFlow.FinalBossTalent);
                if (gameFlow.MidBossTalent != BossTalent.None)
                    talents = "中期：" + BossTalentCatalog.Summary(gameFlow.MidBossTalent) + "\n" + talents;
                WuxiaUiComponents.Text(new Rect(0, y, width, 76), talents, 15, Gold, TextAnchor.UpperLeft, true);
                y += 84;
            }
            if (gameFlow.HasRunChallenge)
            {
                var run = gameFlow.ChallengeRun;
                WuxiaUiComponents.Text(new Rect(0, y, width, 28),
                    RunChallengeCatalog.TierName(run.tier) + " · " + RunChallengeCatalog.ApproachName(run.approach), 18, Gold);
                WuxiaUiComponents.Text(new Rect(0, y + 32, width, 26),
                    $"悬赏完成 {run.bounties.FindAll(b => b.completed).Count}/{run.bounties.Count}", 15);
                WuxiaUiComponents.Text(new Rect(0, y + 64, width, 40), gameFlow.ChallengeResult, 15, Jade, TextAnchor.UpperLeft, true);
                WuxiaUiComponents.Text(new Rect(0, y + 106, width, 32), gameFlow.PursuitSummary, 15, Gold);
                y += 144;
            }
            if (gameFlow.HasRouteSpecialties)
            {
                foreach (var route in new[] { RouteSpecialty.Practice, RouteSpecialty.Camp, RouteSpecialty.Cave })
                {
                    WuxiaUiComponents.Text(new Rect(0, y, width, 44), PingchuanRouteCatalog.Name(route) + " · " + gameFlow.RouteProgress(route), 14, Muted, TextAnchor.UpperLeft, true);
                    y += 50;
                }
            }
            WuxiaUiComponents.Text(new Rect(0, y, width, 28), "本局武学", 18, Gold); y += 30;
            foreach (string id in playerStats.learnedMartialArts)
            {
                DrawIcon(new Rect(0, y, 38, 38), FindMartialArtIcon(id), MartialArtIconRenderer.Accent(id));
                WuxiaUiComponents.Text(new Rect(50, y, width - 50, 38), $"{id} · {RankName(playerStats.GetMartialArtRank(id))}", 16);
                y += 46;
            }
            GUI.EndGroup();
            GUI.EndScrollView();
            float nextWidth = showChallengeActions ? (p.width - 60) / 2 : p.width - 48;
            float nextX = p.x + 24;
            if (showChallengeActions)
            {
                int tier = gameFlow.ChallengeRun.tier;
                GUI.enabled = won && tier < 2 && tier + 1 <= GameFlow.ChallengeProgress.HighestUnlocked;
                if (GUI.Button(ResponsiveGui.IsPortrait ? PortraitUiLayout.BottomAction(p, 1, nextX > p.x + 24 ? 1 : 0, gameFlow.HasRunChallenge ? 2 : 1) : new Rect(nextX, p.yMax - 130, nextWidth, 48),
                    tier < 2 ? "挑战" + RunChallengeCatalog.TierName(tier + 1) : won ? "绝境已登顶" : "绝境待破", ResponsiveGui.IsPortrait ? WuxiaUiComponents.TouchButton(true) : mainMenuButtonStyle))
                {
                    reviewScroll = Vector2.zero; portraitSelectedArt = null;
                    gameFlow.RetryNextChallenge();
                    return;
                }
                nextX += nextWidth + 12;
            }
            GUI.enabled = gameFlow.IsEndlessMode || gameFlow.CanContinueToNextLevel || gameFlow.IsGameCompleted;
            if (GUI.Button(ResponsiveGui.IsPortrait ? PortraitUiLayout.BottomAction(p, 1, nextX > p.x + 24 ? 1 : 0, showChallengeActions ? 2 : 1) : new Rect(nextX, p.yMax - 130, nextWidth, 48),
                gameFlow.IsEndlessMode ? EndlessProgressionCatalog.Title + " · 兑换与升级" : gameFlow.IsGameCompleted ? GameTextCatalog.CreditsTitle : gameFlow.CanContinueToNextLevel ? "下一关" : "下一关尚未开放", ResponsiveGui.IsPortrait ? WuxiaUiComponents.TouchButton(true) : mainMenuButtonStyle))
            {
                if (gameFlow.IsEndlessMode) OpenEndlessProgression();
                else if (gameFlow.IsGameCompleted) gameFlow.ShowEndingCredits();
                else gameFlow.ContinueToNextLevel();
            }
            GUI.enabled = true;
            float buttonWidth = (p.width - 60) / 2;
            if (GUI.Button(ResponsiveGui.IsPortrait ? PortraitUiLayout.BottomAction(p, 0, 0, 2) : new Rect(p.x + 24, p.yMax - 70, buttonWidth, 46), gameFlow.IsEndlessMode ? "从第一轮再战" : GameTextCatalog.RetryCurrentLevel, WuxiaUiComponents.TouchButton()))
            {
                reviewScroll = Vector2.zero; portraitSelectedArt = null;
                gameFlow.RetryCurrentLevel();
            }
            if (GUI.Button(ResponsiveGui.IsPortrait ? PortraitUiLayout.BottomAction(p, 0, 1, 2) : new Rect(p.x + 36 + buttonWidth, p.yMax - 70, buttonWidth, 46), "返回主页", WuxiaUiComponents.TouchButton())) gameFlow.ReturnToMainMenu();
        }
    }
}
