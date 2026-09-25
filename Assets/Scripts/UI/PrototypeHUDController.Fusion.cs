using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.MartialArts;

namespace WuxiaRoguelite.UI
{
    public partial class PrototypeHUDController
    {
        private readonly List<MartialArtChoiceInsight.FusionPreview> fusionPreviews = new();
        private readonly Dictionary<string, int> observedSecretRanks = new();
        private readonly Queue<(string id, int rank)> fusionNotices = new();
        private int fusionRun = -1;
        private float fusionNoticeAge = 3f;
        private (string id, int rank) fusionNotice;

        private void DrawFusionChoiceCard(Rect card, string id, bool expanded = true)
        {
            var art = MartialArtCatalog.Get(id);
            if (art == null) return;
            DrawIcon(new Rect(card.x + 10, card.y + 10, 44, 44), FindMartialArtIcon(id), MartialArtIconRenderer.Accent(id));
            WuxiaUiComponents.Text(new Rect(card.x + 62, card.y + 8, card.width - 72, 28), id, 18);
            int rank = playerStats.GetMartialArtRank(id);
            WuxiaUiComponents.Text(new Rect(card.x + 62, card.y + 36, card.width - 72, 22),
                $"{MartialArtCatalog.SchoolName(art.school)} · {rank} → {Mathf.Min(rank + 1, art.maxRank)}重", 14, Gold);
            WuxiaUiComponents.Text(new Rect(card.x + 12, card.y + 62, card.width - 24, 44),
                MartialArtChoiceInsight.Benefit(playerStats, id), 14, Paper, TextAnchor.UpperLeft, true);
            MartialArtChoiceInsight.FusionPreviews(playerStats, id, fusionPreviews);
            if (!expanded)
            {
                int ready = 0;
                foreach (var preview in fusionPreviews) if (preview.Ready) ready++;
                WuxiaUiComponents.Text(new Rect(card.x + 12, card.y + 103, card.width - 24, 20),
                    ready > 0 ? $"可领悟 / 升重 {ready} 项秘传 · 点选查看" : "点选查看融合路线", 14, ready > 0 ? Gold : Muted);
                return;
            }
            for (int i = 0; i < fusionPreviews.Count; i++)
                DrawFusionRecipe(new Rect(card.x + 10, card.y + 110 + i * 118, card.width - 20, 112), fusionPreviews[i]);
            WuxiaUiComponents.Text(new Rect(card.x + 12, card.yMax - 24, card.width - 24, 18),
                "流派总重数达标自动领悟 · 原武学保留", 11, Muted);
        }

        private void DrawFusionRecipe(Rect r, MartialArtChoiceInsight.FusionPreview preview)
        {
            Color accent = preview.Ready ? Gold : WuxiaUiTheme.Brass;
            WuxiaUiTheme.DrawCompactSurface(r, Ink, accent);
            if (preview.Ready) WuxiaUiTheme.DrawOutline(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), Gold, 1);
            string state = preview.Mastered ? "已圆满" : preview.Ready
                ? (preview.currentRank == 0 ? "可领悟" : "可升重") : $"选后还差 {preview.Missing} 重";
            WuxiaUiComponents.Text(new Rect(r.x + 8, r.y + 2, r.width - 16, 18),
                $"秘传{preview.targetRank}重 · {state}", 14, preview.Ready ? Gold : Paper);
            float nodeWidth = (r.width - 42) / 3f;
            Rect first = new Rect(r.x + 6, r.y + 22, nodeWidth, 58);
            Rect second = new Rect(first.xMax + 12, first.y, nodeWidth, 58);
            Rect result = new Rect(second.xMax + 18, first.y, nodeWidth, 58);
            DrawFusionSchool(first, preview.secret.firstSchool, preview.firstBefore, preview.firstAfter, preview.threshold);
            WuxiaUiComponents.Text(new Rect(first.xMax, first.y + 8, 12, 24), "+", 14, Muted, TextAnchor.MiddleCenter);
            DrawFusionSchool(second, preview.secret.secondSchool, preview.secondBefore, preview.secondAfter, preview.threshold);
            WuxiaUiComponents.Text(new Rect(second.xMax, second.y + 8, 18, 24), "→", 18, accent, TextAnchor.MiddleCenter);
            Color old = GUI.color;
            if (!preview.Ready && !preview.Mastered) GUI.color = new Color(old.r, old.g, old.b, old.a * .55f);
            DrawIcon(new Rect(result.center.x - 17, result.y, 34, 34), FindMartialArtIcon(preview.secret.id), accent);
            GUI.color = old;
            WuxiaUiComponents.Text(new Rect(result.x, result.y + 34, result.width, 20), preview.secret.id, 14,
                preview.Ready ? Gold : Paper, TextAnchor.MiddleCenter);
            WuxiaUiComponents.Text(new Rect(r.x + 8, r.y + 82, r.width - 16, 28),
                preview.secret.GetEffectSummary(preview.targetRank), 13, Muted, TextAnchor.UpperLeft, true);
        }

        private void DrawFusionSchool(Rect r, MartialArtSchool school, int before, int after, int threshold)
        {
            // A representative school icon is not a requirement for that particular art.
            string iconId = school switch
            {
                MartialArtSchool.SwiftSword => "剑气诀",
                MartialArtSchool.VenomPalm => "毒砂掌",
                MartialArtSchool.IronBody => "铁布衫",
                MartialArtSchool.ShadowSteps => "踏雪无痕",
                _ => "饮血刀法"
            };
            Color color = MartialArtIconRenderer.SchoolColor(school);
            DrawIcon(new Rect(r.center.x - 16, r.y, 32, 32), FindMartialArtIcon(iconId), color);
            if (after > before)
                WuxiaUiComponents.Text(new Rect(r.xMax - 22, r.y, 22, 18), "+1", 12, Gold, TextAnchor.MiddleRight);
            WuxiaUiComponents.Text(new Rect(r.x, r.y + 32, r.width, 18),
                $"{MartialArtCatalog.SchoolName(school)} {after}/{threshold}", 14, Paper, TextAnchor.MiddleCenter);
            float step = (r.width - 4) / threshold;
            for (int i = 0; i < threshold; i++)
            {
                Rect pip = new Rect(r.x + 2 + i * step, r.y + 51, step - 3, 5);
                WuxiaUiTheme.DrawOutline(pip, Muted, 1);
                if (i < before) FillRect(pip, color);
                else if (i < after)
                {
                    FillRect(pip, Gold);
                    // A white-paper inset distinguishes the offered rank in grayscale too.
                    FillRect(new Rect(pip.center.x - 1, pip.y, 2, pip.height), Paper);
                }
            }
        }

        private bool FusionFeedbackVisible => GrowthVisible && gameFlow.CurrentPhase != GamePhase.LevelUpPaused;

        private void UpdateFusionFeedback()
        {
            if (playerStats == null) return;
            if (fusionRun != playerStats.RunRevision)
            {
                fusionRun = playerStats.RunRevision;
                observedSecretRanks.Clear();
                fusionNotices.Clear();
                fusionNoticeAge = 3f;
            }
            foreach (string id in MartialArtCatalog.AllSecretIds)
            {
                observedSecretRanks.TryGetValue(id, out int previous);
                int current = playerStats.GetSecretRank(id);
                if (current > previous) fusionNotices.Enqueue((id, current));
                observedSecretRanks[id] = current;
            }
            if (!FusionFeedbackVisible) return;
            if (fusionNoticeAge >= 2.6f && fusionNotices.Count > 0)
            {
                fusionNotice = fusionNotices.Dequeue();
                fusionNoticeAge = 0;
            }
            else fusionNoticeAge += Time.unscaledDeltaTime;
        }

        private void DrawFusionFeedback()
        {
            if (!FusionFeedbackVisible || fusionNoticeAge >= 2.6f || string.IsNullOrEmpty(fusionNotice.id)) return;
            Rect safe = ResponsiveGui.SafeArea;
            float width = Mathf.Min(360, safe.width - 32);
            Rect r = new Rect(safe.center.x - width / 2, safe.y + (ResponsiveGui.IsPortrait ? 222 : 112), width, 78);
            Color old = GUI.color;
            GUI.color = new Color(old.r, old.g, old.b, old.a * Mathf.Clamp01((2.6f - fusionNoticeAge) / .2f));
            DrawPanel(r, Ink, Gold);
            DrawIcon(new Rect(r.x + 10, r.y + 15, 48, 48), FindMartialArtIcon(fusionNotice.id), Gold);
            WuxiaUiComponents.Text(new Rect(r.x + 68, r.y + 8, r.width - 78, 28),
                $"{(fusionNotice.rank == 1 ? "秘传领悟" : "秘传升重")} · {fusionNotice.id}", 18, Gold);
            WuxiaUiComponents.Text(new Rect(r.x + 68, r.y + 38, r.width - 78, 34),
                MartialArtCatalog.GetSecret(fusionNotice.id).GetEffectSummary(fusionNotice.rank), 14, Paper, TextAnchor.UpperLeft, true);
            if (!reduceGrowthMotion && fusionNoticeAge < .6f)
            {
                float x = Mathf.Lerp(r.x + 8, r.xMax - 8, fusionNoticeAge / .6f);
                FillRect(new Rect(x, r.y + 5, 2, 2), Gold);
                FillRect(new Rect(x, r.yMax - 7, 2, 2), Gold);
            }
            GUI.color = old;
        }
    }
}
