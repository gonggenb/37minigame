using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class PrototypeHUDController
    {
        private bool endlessProgressionOpen;
        private Vector2 endlessProgressionScroll;
        private string endlessProgressionFeedback = "";

        private void OpenEndlessProgression()
        {
            if (!gameFlow.CanManageEndlessProgression) return;
            endlessProgressionOpen = true;
            endlessProgressionScroll = Vector2.zero;
            endlessProgressionFeedback = "选择修炼方向，属性从下一次无尽挑战开始生效。";
        }

        private void DrawEndlessProgression()
        {
            DrawCoverBackground(true);
            Rect p = PortraitUiLayout.Modal(ResponsiveGui.IsPortrait ? 800 : 490, ResponsiveGui.IsPortrait ? 492 : 760);
            DrawPanel(p, Ink, Gold);
            var data = EndlessProgression.Read();
            if (EndlessProgression.LoadFailed) endlessProgressionFeedback = "修炼存档无法读取，已保留原档并停止兑换。";
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 16, p.width - 48, 32), EndlessProgressionCatalog.Title, 26, Gold);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 56, p.width - 48, 30),
                $"无尽金币 {data.coins}    技能点 {data.skillPoints}", 20);
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.y + 90, p.width - 48, 32),
                $"仅无尽生效 · 永久保留 · 最高通过 {data.bestRound} 轮", 14, Muted);
            Rect viewport = new Rect(p.x + 24, p.y + 132, p.width - 48, p.height - 268);
            int columns = ResponsiveGui.IsPortrait ? 1 : 2;
            float width = viewport.width - 20, cardWidth = (width - 12 * (columns - 1)) / columns;
            const float rowHeight = 156;
            endlessProgressionScroll = GUI.BeginScrollView(viewport, endlessProgressionScroll,
                new Rect(0, 0, width, rowHeight * (EndlessProgressionCatalog.SkillCount / columns)));
            for (int i = 0; i < EndlessProgressionCatalog.SkillCount; i++)
            {
                var skill = EndlessProgressionCatalog.Skills[i];
                int rank = data.Rank(skill), cost = EndlessProgressionCatalog.UpgradeCost(rank);
                bool maxed = rank >= EndlessProgressionCatalog.MaxRank;
                Rect card = new Rect((i % columns) * (cardWidth + 12), (i / columns) * rowHeight, cardWidth, rowHeight - 12);
                DrawPanel(card, WuxiaUiTheme.BackgroundBrown, maxed ? Jade : WuxiaUiTheme.Brass);
                DrawIcon(new Rect(card.x + 12, card.y + 12, 44, 44),
                    Resources.Load<Texture2D>("Icons/" + EndlessProgressionCatalog.IconId(skill)), Gold);
                WuxiaUiComponents.Text(new Rect(card.x + 66, card.y + 10, card.width - 78, 26),
                    EndlessProgressionCatalog.Name(skill) + $"  {rank}/{EndlessProgressionCatalog.MaxRank}", 18);
                WuxiaUiComponents.Text(new Rect(card.x + 66, card.y + 36, card.width - 78, 24),
                    "当前 " + EndlessProgressionCatalog.Effect(skill, rank), 14, Muted);
                WuxiaUiComponents.Text(new Rect(card.x + 12, card.y + 66, card.width - 24, 22),
                    maxed ? "已满级" : "下级 " + EndlessProgressionCatalog.Effect(skill, rank + 1), 14, Gold);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && !maxed && data.skillPoints >= cost;
                if (GUI.Button(new Rect(card.x + 12, card.y + 92, card.width - 24, 44),
                    maxed ? "修炼圆满" : $"升级 · {cost} 技能点" + (data.skillPoints < cost ? "（不足）" : ""), WuxiaUiComponents.TouchButton()))
                {
                    endlessProgressionFeedback = gameFlow.UpgradeEndlessSkill(skill)
                        ? EndlessProgressionCatalog.Name(skill) + $"升至 {rank + 1} 级 · 下一次无尽生效"
                        : "技能点不足或已满级。";
                }
                GUI.enabled = enabled;
            }
            GUI.EndScrollView();
            WuxiaUiComponents.Text(new Rect(p.x + 24, p.yMax - 124, p.width - 48, 40),
                endlessProgressionFeedback, 14, Gold, TextAnchor.UpperLeft, true);
            float closeWidth = 104, actionHeight = ResponsiveGui.IsPortrait ? 64 : 48;
            float actionY = p.yMax - actionHeight - 24;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && data.coins >= EndlessProgressionCatalog.CoinsPerPoint && data.HasUpgradesRemaining;
            if (GUI.Button(new Rect(p.x + 24, actionY, p.width - closeWidth - 60, actionHeight),
                data.HasUpgradesRemaining ? $"{EndlessProgressionCatalog.CoinsPerPoint} 金币兑换 1 技能点" : "全部修炼已满级", WuxiaUiComponents.TouchButton(true)))
                endlessProgressionFeedback = gameFlow.ExchangeEndlessPoint() ? "兑换成功：技能点 +1，请选择技能升级。" : "无尽金币不足。";
            GUI.enabled = wasEnabled;
            if (GUI.Button(new Rect(p.xMax - closeWidth - 24, actionY, closeWidth, actionHeight),
                "返回", WuxiaUiComponents.TouchButton())) endlessProgressionOpen = false;
        }

        private void DrawEndlessRewardSummary(float width)
        {
            WuxiaUiComponents.Text(new Rect(0, 0, width, 30), $"本次获得无尽金币 +{gameFlow.EndlessCoinsEarned}", 20, Gold);
            string breakdown = $"普通 {gameFlow.EndlessRewardCount(EndlessReward.Normal)}×2 · 精英 {gameFlow.EndlessRewardCount(EndlessReward.Elite)}×5 · 守洞 {gameFlow.EndlessRewardCount(EndlessReward.Cave)}×4\n"
                + $"中期强敌 {gameFlow.EndlessRewardCount(EndlessReward.MidBoss)}×12 · 通关轮数 {gameFlow.EndlessRewardCount(EndlessReward.Round)}×30";
            WuxiaUiComponents.Text(new Rect(0, 34, width, 52), breakdown, 14, Paper, TextAnchor.UpperLeft, true);
            var data = EndlessProgression.Read();
            WuxiaUiComponents.Text(new Rect(0, 92, width, 42),
                $"已自动保存 · 当前金币 {data.coins} · 技能点 {data.skillPoints}", 14, Muted, TextAnchor.UpperLeft, true);
        }
    }
}
