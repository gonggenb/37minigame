using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    public partial class PrototypeHUDController
    {
        private void DrawGameEnding()
        {
            // Result freezes gameplay; draw this in place of the review so its buttons cannot receive input.
            FillRect(new Rect(0, 0, ResponsiveGui.Width, ResponsiveGui.Height), WuxiaUiTheme.BackgroundInk);
            bool credits = gameFlow.IsCreditsVisible;
            bool portrait = ResponsiveGui.IsPortrait;
            Rect p = PortraitUiLayout.Modal(credits ? (portrait ? 720 : 500) : 320,
                credits && !portrait ? 620 : 492);
            DrawPanel(p, Ink, Gold, WuxiaPanelKind.Boss);
            float x = p.x + 24;
            float width = p.width - 48;
            WuxiaUiComponents.Text(new Rect(x, p.y + (credits ? 14 : 24), width, 30), GameTextCatalog.GameTitle,
                18, Muted, TextAnchor.MiddleCenter);
            WuxiaUiComponents.Text(new Rect(x, p.y + (credits ? 46 : 66), width, 44),
                credits ? GameTextCatalog.CreditsTitle : GameTextCatalog.GameCompletedTitle,
                30, Gold, TextAnchor.MiddleCenter);
            if (credits)
            {
                WuxiaUiComponents.Text(new Rect(x, p.y + 94, width, 36), GameTextCatalog.CreditsTeamName,
                    26, Paper, TextAnchor.MiddleCenter);
                float rowTop = p.y + (portrait ? 155 : 143);
                float rowHeight = portrait ? 48 : 32;
                DrawCreditRole(new Rect(x, rowTop, width, rowHeight), "组长", GameTextCatalog.CreditsLeaderName);
                DrawCreditRole(new Rect(x, rowTop + rowHeight, width, rowHeight), "策划", GameTextCatalog.CreditsDesignerName);
                DrawCreditRole(new Rect(x, rowTop + rowHeight * 2, width, rowHeight), "程序", GameTextCatalog.CreditsProgrammerName);
                DrawCreditRole(new Rect(x, rowTop + rowHeight * 3, width, rowHeight), "美术", GameTextCatalog.CreditsArtistName);
                WuxiaUiComponents.Text(new Rect(x, p.y + (portrait ? 366 : 283), width, 24),
                    "物料与宣发", 16, Muted, TextAnchor.MiddleCenter);
                WuxiaUiComponents.Text(new Rect(x, p.y + (portrait ? 399 : 311), width, portrait ? 68 : 50),
                    GameTextCatalog.CreditsPromotionNames, 20, Paper, TextAnchor.MiddleCenter, true);
                if (portrait)
                    WuxiaUiComponents.Text(new Rect(x, p.y + 490, width, 28), "感谢游玩，江湖再会。", 16, Muted, TextAnchor.MiddleCenter);
                if (GUI.Button(portrait ? PortraitUiLayout.BottomAction(p, 1) : new Rect(x, p.yMax - 112, width, 48), "查看通关战果", WuxiaUiComponents.TouchButton(true)))
                    gameFlow.DismissEnding();
                if (GUI.Button(portrait ? PortraitUiLayout.BottomAction(p) : new Rect(x, p.yMax - 56, width, 44), "返回主页", WuxiaUiComponents.TouchButton()))
                    gameFlow.ReturnToMainMenu();
            }
            else
            {
                WuxiaUiComponents.Text(new Rect(x, p.y + 132, width, 56), GameTextCatalog.GameCompletedMessage,
                    22, Paper, TextAnchor.MiddleCenter, true);
                WuxiaUiComponents.Text(new Rect(x, p.y + 204, width, 28), "即将播放制作人员名单", 15, Muted, TextAnchor.MiddleCenter);
                if (GUI.Button(portrait ? PortraitUiLayout.BottomAction(p) : new Rect(x, p.yMax - 64, width, 48), "查看制作人员", WuxiaUiComponents.TouchButton(true)))
                    gameFlow.ShowEndingCredits();
            }
        }

        private void DrawCreditRole(Rect row, string role, string names)
        {
            float middle = row.center.x;
            WuxiaUiComponents.Text(new Rect(row.x, row.y, row.width * .5f - 24, row.height),
                role, 16, Muted, TextAnchor.MiddleRight);
            WuxiaUiComponents.Text(new Rect(middle, row.y, row.width * .5f, row.height),
                names, 22, Paper, TextAnchor.MiddleLeft);
        }
    }
}
