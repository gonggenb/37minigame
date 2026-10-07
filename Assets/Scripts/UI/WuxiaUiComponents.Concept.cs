using UnityEngine;

namespace WuxiaRoguelite.UI
{
    /// <summary>Shared presentation primitives for the approved October UI concept.</summary>
    public static partial class WuxiaUiComponents
    {
        private static Texture2D paperTexture;
        private static GUIStyle paperStyle;
        private static GUIStyle iconActionStyle;

        public static bool IconAction(Rect rect, Texture icon, string label)
        {
            iconActionStyle ??= WuxiaUiTheme.CreateButtonStyle(12, WuxiaButtonKind.Icon);
            bool pressed = GUI.Button(rect, GUIContent.none, iconActionStyle);
            if (icon != null) GUI.DrawTexture(new Rect(rect.center.x - 12, rect.y + 4, 24, 24),
                icon, ScaleMode.ScaleToFit, true);
            Text(new Rect(rect.x + 2, rect.yMax - 20, rect.width - 4, 18),
                label, 12, WuxiaUiTheme.TextPrimary, TextAnchor.MiddleCenter);
            return pressed;
        }

        public static void PaperInset(Rect rect)
        {
            paperTexture ??= Resources.Load<Texture2D>("UI/Theme/tex_ui_panel_paper_v02");
            WuxiaUiTheme.FillRect(rect, WuxiaUiTheme.Paper);
            if (paperTexture != null)
            {
                Color previous = GUI.color;
                GUI.color = Color.white;
                paperStyle ??= new GUIStyle { border = new RectOffset(14, 14, 14, 14) };
                paperStyle.normal.background = paperTexture;
                GUI.Box(rect, GUIContent.none, paperStyle);
                GUI.color = previous;
            }
            WuxiaUiTheme.DrawOutline(rect, WuxiaUiTheme.Brass, 1);
        }

        public static void StatusBadge(Rect rect, string label, Color accent)
        {
            WuxiaUiTheme.DrawCompactSurface(rect, WuxiaUiTheme.BackgroundInk, accent);
            Text(new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height),
                label, 14, WuxiaUiTheme.TextPrimary, TextAnchor.MiddleCenter);
        }

        public static void Selection(Rect rect, bool selected, bool sold = false)
        {
            if (selected)
            {
                WuxiaUiTheme.DrawOutline(new Rect(rect.x + 3, rect.y + 3,
                    rect.width - 6, rect.height - 6), WuxiaUiTheme.Gold, 2);
                Rect seal = new Rect(rect.xMax - 52, rect.y + 7, 44, 22);
                WuxiaUiTheme.FillRect(seal, WuxiaUiTheme.Brass);
                Text(seal, "已选", 12, WuxiaUiTheme.BackgroundInk, TextAnchor.MiddleCenter);
            }
            if (sold)
            {
                Rect seal = new Rect(rect.xMax - 78, rect.yMax - 30, 66, 22);
                WuxiaUiTheme.DrawOutline(seal, WuxiaUiTheme.Danger, 2);
                Text(seal, "已售罄", 14, WuxiaUiTheme.TextSecondary, TextAnchor.MiddleCenter);
            }
        }

        public static string ElapsedText(float seconds)
        {
            int elapsed = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{elapsed / 60:00}:{elapsed % 60:00}";
        }

        public static void ElapsedTimer(Rect rect, float seconds)
        {
            WuxiaUiTheme.DrawPanel(rect, WuxiaUiTheme.BackgroundInk,
                WuxiaUiTheme.Brass, WuxiaPanelKind.Boss);
            Text(new Rect(rect.x + 10, rect.y + 4, rect.width - 20, 18),
                "战斗用时", 12, WuxiaUiTheme.TextSecondary, TextAnchor.MiddleCenter);
            Text(new Rect(rect.x + 8, rect.y + 23, rect.width - 16, rect.height - 28),
                ElapsedText(seconds), 24, WuxiaUiTheme.TextPrimary, TextAnchor.MiddleCenter);
        }
    }
}
