using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class PrototypeHUDController
    {
        private bool challengeLedgerOpen;
        private Vector2 challengeBriefScroll;

        private void CloseChallengeLedger()
        {
            if (!challengeLedgerOpen) return;
            challengeLedgerOpen = false;
            gameFlow?.SetCharacterMenuPaused(false);
        }

        private void DrawChallengeButton()
        {
            if (!gameFlow.HasBossTalents) return;
            Rect safe = ResponsiveGui.SafeArea;
            Rect button = new Rect(safe.xMax - 58, safe.y + 176, 48, 48);
            if (GUI.Button(button, new GUIContent(ChallengeArt.Get("bounty_writ"), "本局情报与悬赏"), iconButtonStyle))
            {
                challengeLedgerOpen = true;
                challengeBriefScroll = Vector2.zero;
                gameFlow.SetCharacterMenuPaused(true);
            }
            WuxiaUiComponents.Text(new Rect(button.x - 4, button.yMax, 56, 20), "情报", 12, Gold, TextAnchor.MiddleCenter);
        }

        private void DrawChallengeBriefing()
        {
            var run = gameFlow.ChallengeRun;
            if (run == null) { DrawBossTalentBriefing(); return; }
            bool opening = gameFlow.IsChallengeBriefingActive;
            bool portrait = ResponsiveGui.IsPortrait;
            PortraitBackdrop();
            Rect panel = PortraitUiLayout.Modal(portrait ? 780 : 500, portrait ? 510 : 860);
            DrawPanel(panel, Ink, Gold, WuxiaPanelKind.Paper);
            var badge = ChallengeArt.Get("challenge_badge");
            if (badge != null) GUI.DrawTexture(new Rect(panel.x + 20, panel.y + 16, 54, 54), badge, ScaleMode.ScaleToFit, true);
            WuxiaUiComponents.Text(new Rect(panel.x + 86, panel.y + 18, panel.width - 106, 32),
                gameFlow.IsEndlessMode ? gameFlow.EndlessRoundLabel : opening ? "入局前 · 先观敌势" : "本局情报与悬赏", 25, Paper);
            WuxiaUiComponents.Text(new Rect(panel.x + 86, panel.y + 50, panel.width - 106, 22),
                gameFlow.IsEndlessMode ? "每轮六十息 · 胜后保留成长继续 · 死亡结束" : opening ? "选武学后开始六十息 · 逐档通关解锁" : "查看期间暂停 · 关闭后继续探索", 13, Muted);
            float tileWidth = (panel.width - 56) / 3;
            for (int i = 0; !gameFlow.IsEndlessMode && i < RunChallengeCatalog.TierCount; i++)
            {
                bool unlocked = i <= ChallengeProgress.HighestUnlocked;
                GUI.enabled = opening && unlocked;
                string label = RunChallengeCatalog.TierName(i) + (unlocked ? "" : " · 未解锁");
                if (GUI.Button(new Rect(panel.x + 20 + i * (tileWidth + 8), panel.y + 84, tileWidth, portrait ? PortraitUiLayout.ActionHeight : 46),
                    label, portrait ? WuxiaUiComponents.TouchTab(i == run.tier) : i == run.tier ? activeTabStyle : actionButtonStyle)) gameFlow.SelectChallengeTier(i);
            }
            if (gameFlow.IsEndlessMode)
                WuxiaUiComponents.Text(new Rect(panel.x + 24, panel.y + 84, panel.width - 48, portrait ? 68 : 46),
                    "每轮气血×1.30 · 攻击×1.20\n防御×1.12+1 · 攻速递增（最高五倍）", 14, Gold, TextAnchor.UpperLeft, true);
            GUI.enabled = true;
            WuxiaUiComponents.Text(new Rect(panel.x + 24, panel.y + (portrait ? 160 : 140), panel.width - 48, 76),
                RunChallengeCatalog.TierDescription(run.tier) + "\n中期：" + BossTalentCatalog.Summary(gameFlow.MidBossTalent) +
                    "\n终局：" + BossTalentCatalog.Summary(gameFlow.FinalBossTalent), 14, Gold, TextAnchor.UpperLeft, true);

            Rect view = new Rect(panel.x + 20, panel.y + (portrait ? 246 : 226), panel.width - 40, panel.height - (portrait ? 346 : 302));
            float pursuitHeight = portrait ? 340 : 176;
            float contentHeight = pursuitHeight + (portrait ? 882 : 540);
            bool scrolling = contentHeight > view.height;
            float width = view.width - (scrolling ? 20 : 0);
            challengeBriefScroll = GUI.BeginScrollView(view, challengeBriefScroll,
                new Rect(0, 0, width, Mathf.Max(view.height, contentHeight)));
            DrawPursuitChoices(new Rect(0, 0, width, pursuitHeight), opening);
            GUI.BeginGroup(new Rect(0, pursuitHeight, width, contentHeight - pursuitHeight));
            float intelWidth = portrait ? width : width * .47f;
            Rect intel = new Rect(0, 0, intelWidth, 540);
            WuxiaUiTheme.DrawCompactSurface(intel, Panel, Gold);
            var icon = ChallengeArt.Get(RunChallengeCatalog.ApproachIcon(run.approach));
            if (icon != null) GUI.DrawTexture(new Rect(intel.x + 12, 12, 62, 62), icon, ScaleMode.ScaleToFit, true);
            WuxiaUiComponents.Text(new Rect(84, 12, intelWidth - 98, 28), RunChallengeCatalog.ApproachName(run.approach), 21, Gold);
            WuxiaUiComponents.Text(new Rect(84, 43, intelWidth - 98, 26), "终局强敌 · 本局固定", 13, Muted);
            WuxiaUiComponents.Text(new Rect(14, 84, intelWidth - 28, 62), RunChallengeCatalog.ApproachDescription(run.approach), 15, Paper, TextAnchor.UpperLeft, true);
            WuxiaUiComponents.Text(new Rect(14, 150, intelWidth - 28, 70), RunChallengeCatalog.ApproachAdvice(run.approach), 14, Color.Lerp(Jade, Paper, .32f), TextAnchor.UpperLeft, true);

            float bountyX = portrait ? 0 : intelWidth + 16;
            float bountyWidth = portrait ? width : width - bountyX;
            DrawTalentDetail(new Rect(14, 240, intelWidth - 28, 136), "中期强敌", gameFlow.MidBossTalent);
            DrawTalentDetail(new Rect(14, 386, intelWidth - 28, 136), "终局强敌", gameFlow.FinalBossTalent);

            float bountyTop = portrait ? 554 : 0;
            for (int i = 0; i < run.bounties.Count; i++)
            {
                var bounty = run.bounties[i];
                float rowStep = portrait ? 164 : 152;
                Rect row = new Rect(bountyX, bountyTop + i * rowStep, bountyWidth, portrait ? 156 : 144);
                WuxiaUiTheme.DrawCompactSurface(row, Panel, bounty.completed ? Jade : Crimson);
                var writ = ChallengeArt.Get("bounty_writ");
                if (writ != null) GUI.DrawTexture(new Rect(row.x + 8, row.y + 8, 42, 42), writ, ScaleMode.ScaleToFit, true);
                WuxiaUiComponents.Text(new Rect(row.x + 58, row.y + 8, row.width - 68, 25),
                    $"悬赏{i + 1} · {bounty.Region}" + (bounty.completed ? " · 已完成" : ""), 16, Gold);
                WuxiaUiComponents.Text(new Rect(row.x + 58, row.y + 34, row.width - 68, 22),
                    (bounty.target != null ? bounty.target.enemyStats.displayName : "强敌") + " · " + bounty.Risk, 14);
                WuxiaUiComponents.Text(new Rect(row.x + 12, row.y + (portrait ? 64 : 56), row.width - 24, 26),
                    "额外气血 +45% · 攻击 +20%", 14, Color.Lerp(Crimson, Paper, .38f));
                WuxiaUiComponents.Text(new Rect(row.x + 12, row.y + (portrait ? 94 : 82), row.width - 24, 28),
                    bounty.completed ? bounty.rewardReceived : "胜利：主修已学武学升一重；满重补40铜钱", 13, Color.Lerp(Jade, Paper, .32f),
                    TextAnchor.UpperLeft, true);
                WuxiaUiComponents.Text(new Rect(row.x + 12, row.yMax - 30, row.width - 24, 28),
                    EnemyMatchupInsight.Weakness(bounty.trait), 14, Paper);
            }
            GUI.EndGroup();
            GUI.EndScrollView();
            if (GUI.Button(portrait ? PortraitUiLayout.BottomAction(panel) : new Rect(panel.x + 24, panel.yMax - 60, panel.width - 48, 44),
                opening ? "情报已明 · 选择起手武学" : "继续探索", mainMenuButtonStyle))
            {
                gameFlow.ConfirmChallengeBriefing();
                CloseChallengeLedger();
                challengeBriefScroll = Vector2.zero;
            }
        }
        private void DrawTalentDetail(Rect rect, string boss, BossTalent talent)
        {
            WuxiaUiComponents.Text(new Rect(rect.x, rect.y, rect.width, 28),
                boss + " · " + BossTalentCatalog.Name(talent), 19, Gold);
            WuxiaUiComponents.Text(new Rect(rect.x, rect.y + 32, rect.width, 24),
                "克制" + BossTalentCatalog.Target(talent) + " · 开局随机，本局固定", 14, Muted);
            WuxiaUiComponents.Text(new Rect(rect.x, rect.y + 62, rect.width, rect.height - 62),
                BossTalentCatalog.Description(talent), 15, Paper, TextAnchor.UpperLeft, true);
        }

        private void DrawBossTalentBriefing()
        {
            PortraitBackdrop();
            Rect panel = PortraitUiLayout.Modal(400, 510);
            DrawPanel(panel, Ink, Gold, WuxiaPanelKind.Paper);
            WuxiaUiComponents.Text(new Rect(panel.x + 24, panel.y + 20, panel.width - 48, 36),
                "本局强敌天赋", 25, Gold);
            DrawTalentDetail(new Rect(panel.x + 24, panel.y + 80, panel.width - 48, 170),
                "终局强敌", gameFlow.FinalBossTalent);
            if (GUI.Button(ResponsiveGui.IsPortrait ? PortraitUiLayout.BottomAction(panel) : new Rect(panel.x + 24, panel.yMax - 68, panel.width - 48, 44),
                gameFlow.IsChallengeBriefingActive ? "情报已明 · 选择起手武学" : "继续探索", mainMenuButtonStyle))
            {
                gameFlow.ConfirmChallengeBriefing();
                CloseChallengeLedger();
            }
        }
    }
}
