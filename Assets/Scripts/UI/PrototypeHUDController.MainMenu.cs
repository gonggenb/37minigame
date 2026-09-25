using UnityEngine;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    // Cover and chapter selection share existing flow, theme and bundled fonts.
    public partial class PrototypeHUDController
    {
        private Texture2D menuCover;
        private Texture2D menuScrim;
        private bool menuCoverLoaded;
        private GUIStyle coverTitle;
        private GUIStyle coverHeading;
        private GUIStyle coverBody;
        private GUIStyle coverCaption;
        private GUIStyle coverCenter;
        private GUIStyle coverNumber;

        private void EnsureCoverStyles()
        {
            if (coverTitle != null) return;
            coverTitle = LabelStyle(40, FontStyle.Bold, TextAnchor.MiddleLeft, WuxiaUiTheme.TextPrimary);
            coverHeading = LabelStyle(22, FontStyle.Bold, TextAnchor.MiddleLeft, WuxiaUiTheme.TextPrimary);
            coverBody = LabelStyle(14, FontStyle.Normal, TextAnchor.MiddleLeft, WuxiaUiTheme.TextPrimary);
            coverCaption = LabelStyle(12, FontStyle.Normal, TextAnchor.MiddleLeft, WuxiaUiTheme.TextSecondary);
            coverCenter = LabelStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, WuxiaUiTheme.TextPrimary);
            coverNumber = LabelStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, WuxiaUiTheme.Gold);
        }

        private void DrawCoverBackground(bool selection)
        {
            EnsureCoverStyles();
            if (!menuCoverLoaded)
            {
                menuCover = Resources.Load<Texture2D>("UI/MainMenu/bg_mainmenu_mountain_pass_v02");
                menuCoverLoaded = true;
            }
            Rect screen = new Rect(0, 0, ResponsiveGui.Width, ResponsiveGui.Height);
            Texture2D background = menuCover != null ? menuCover : mainMenuBackground;
            if (background != null)
            {
                // Portrait favors the lit pavilion instead of cropping the landscape's quiet left side.
                float screenAspect = screen.width / screen.height;
                float imageAspect = (float)background.width / background.height;
                Rect uv = new Rect(0, 0, 1, 1);
                if (screenAspect < imageAspect)
                {
                    uv.width = screenAspect / imageAspect;
                    uv.x = Mathf.Clamp01((ResponsiveGui.IsPortrait ? 0.66f : 0.5f) - uv.width * 0.5f);
                    uv.x = Mathf.Min(uv.x, 1f - uv.width);
                }
                else
                {
                    uv.height = imageAspect / screenAspect;
                    uv.y = (1f - uv.height) * 0.5f;
                }
                GUI.DrawTextureWithTexCoords(screen, background, uv);
            }
            else
            {
                // PLACEHOLDER_UI: emergency missing-cover fallback uses the shared material.
                WuxiaUiTheme.DrawPanel(screen, WuxiaUiTheme.BackgroundInk, WuxiaUiTheme.Brass);
            }

            if (selection) FillRect(screen, WithAlpha(WuxiaUiTheme.BackgroundInk, 0.66f));
            // A single bilinear alpha texture avoids seams between overlapping gradient bands.
            if (menuScrim == null)
            {
                menuScrim = new Texture2D(1, 128, TextureFormat.RGBA32, false)
                {
                    name = "MainMenuReadabilityScrim",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
                Color[] pixels = new Color[128];
                for (int i = 0; i < pixels.Length; i++)
                {
                    float t = i / 127f;
                    float alpha = Mathf.Max(0, 1f - t / 0.30f) * 0.86f
                        + Mathf.Max(0, (t - 0.70f) / 0.30f) * 0.54f;
                    pixels[i] = WithAlpha(WuxiaUiTheme.BackgroundInk, alpha);
                }
                menuScrim.SetPixels(pixels);
                menuScrim.Apply(false, true);
            }
            GUI.DrawTexture(screen, menuScrim);
            Rect safe = ResponsiveGui.SafeArea;
            WuxiaUiTheme.DrawOutline(new Rect(safe.x + 16, safe.y + 16, safe.width - 32, safe.height - 32),
                WithAlpha(WuxiaUiTheme.Brass, 0.32f), 1);
        }

        private void DrawMainMenu()
        {
            DrawCoverBackground(false);
            Rect safe = ResponsiveGui.SafeArea;
            if (ResponsiveGui.IsPortrait) DrawPortraitCover(safe);
            else DrawLandscapeCover(safe);
        }

        private void DrawLandscapeCover(Rect safe)
        {
            float x = safe.x + 64;
            float y = safe.y + Mathf.Max(66, safe.height * 0.17f);
            float width = Mathf.Min(352, safe.width * 0.44f);
            DrawCoverIdentity(new Rect(x, y, width, 146), false);
            DrawCoverTime(new Rect(x, y + 152, 72, 72));
            GUI.Label(new Rect(x + 88, y + 158, width - 88, 28), "六十息择路，成就一身武学", coverBody);
            GUI.Label(new Rect(x + 88, y + 185, width - 88, 34), "探索 · 构筑 · 挑战强敌", coverCaption);
            DrawCoverAction(new Rect(x, y + 254, Mathf.Min(width, 280), 56));
            GUI.Label(new Rect(x, y + 318, width, 24),
                gameFlow.IsLevelTwoUnlocked ? "教学已完成 · 可选择" + GameTextCatalog.MainLevelName : "初次入江湖 · 从三十息教学启程", coverCaption);
            DrawCoverFooter(new Rect(x, safe.yMax - 58, safe.width - 128, 24));
        }

        private void DrawPortraitCover(Rect safe)
        {
            float width = Mathf.Min(400, safe.width - 64);
            float x = safe.center.x - width * 0.5f;
            DrawCoverIdentity(new Rect(x, safe.y + 84, width, 150), true);
            float bottom = safe.yMax - 64;
            DrawCoverTime(new Rect(safe.center.x - 36, bottom - 280, 72, 72));
            GUI.Label(new Rect(x, bottom - 196, width, 28), "六十息择路，成就一身武学", coverCenter);
            DrawCoverAction(new Rect(x + 16, bottom - 146, width - 32, 60));
            GUI.Label(new Rect(x, bottom - 78, width, 28),
                gameFlow.IsLevelTwoUnlocked ? "教学已完成 · 可选择" + GameTextCatalog.MainLevelName : "初次入江湖 · 从三十息教学启程", coverCenter);
            DrawCoverFooter(new Rect(x, bottom - 18, width, 28));
        }

        private void DrawCoverIdentity(Rect rect, bool centered)
        {
            coverTitle.alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            coverCaption.alignment = centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 24), "一 分 钟 武 侠  ·  自 动 战 斗", coverCaption);
            GUI.Label(new Rect(rect.x, rect.y + 32, rect.width, 60), GameTextCatalog.GameTitle, coverTitle);
            float lineWidth = Mathf.Min(240, rect.width);
            float lineX = centered ? rect.center.x - lineWidth * 0.5f : rect.x;
            FillRect(new Rect(lineX, rect.y + 106, lineWidth, 1), WithAlpha(WuxiaUiTheme.Brass, 0.8f));
            FillRect(new Rect(lineX + lineWidth * 0.5f - 3, rect.y + 103, 6, 7), WuxiaUiTheme.Brass);
            GUI.Label(new Rect(rect.x, rect.y + 120, rect.width, 26), "山河有路，江湖由你", centered ? coverCenter : coverBody);
            coverCaption.alignment = TextAnchor.MiddleLeft;
        }

        private void DrawCoverTime(Rect rect)
        {
            WuxiaUiTheme.DrawTimerDial(rect, WuxiaUiTheme.Brass);
            GUI.Label(new Rect(rect.x, rect.y + 10, rect.width, 34), "60", coverNumber);
            GUI.Label(new Rect(rect.x, rect.y + 43, rect.width, 20), "息", coverCenter);
        }

        private void DrawCoverAction(Rect rect)
        {
            if (GUI.Button(rect, "踏入江湖  ·  选择关卡", mainMenuButtonStyle)) gameFlow.OpenLevelSelection();
        }

        private void DrawCoverFooter(Rect rect)
        {
            FillRect(new Rect(rect.x, rect.y - 8, rect.width, 1), WithAlpha(WuxiaUiTheme.Brass, 0.4f));
            GUI.Label(rect, "择路探索    /    历战成长    /    洞穴寻宝", coverCenter);
        }

        private Vector2 chapterScroll;

        private void DrawLevelSelection()
        {
            DrawCoverBackground(true);
            Rect safe = ResponsiveGui.SafeArea;
            bool portrait = ResponsiveGui.IsPortrait;
            float width = Mathf.Min(portrait ? 432 : 920, safe.width - 48);
            float height = Mathf.Min(portrait ? 760 : 396, safe.height - 64);
            Rect area = new Rect(safe.center.x - width * 0.5f, safe.center.y - height * 0.5f, width, height);
            GUI.Label(new Rect(area.x, area.y, width, 24), "江 湖 行 卷", coverCaption);
            GUI.Label(new Rect(area.x, area.y + 28, width, 42), "选择关卡", coverHeading);
            FillRect(new Rect(area.x, area.y + 78, width, 1), WithAlpha(WuxiaUiTheme.Brass, 0.6f));
            Rect viewport = new Rect(area.x, area.y + 94, width, height - (portrait ? 174 : 154));
            if (portrait)
            {
                float cardWidth = width - 20;
                chapterScroll = GUI.BeginScrollView(viewport, chapterScroll, new Rect(0, 0, cardWidth, 720));
                for (int i = 0; i < 3; i++) DrawChapterCard(new Rect(0, i * 240, cardWidth, 228), i + 1);
                GUI.EndScrollView();
            }
            else
            {
                float cardWidth = (width - 24) / 3;
                for (int i = 0; i < 3; i++)
                    DrawChapterCard(new Rect(area.x + i * (cardWidth + 12), viewport.y, cardWidth, viewport.height), i + 1);
            }
            if (GUI.Button(new Rect(area.x, area.yMax - (portrait ? PortraitUiLayout.ActionHeight : 44),
                portrait ? width : 120, portrait ? PortraitUiLayout.ActionHeight : 44), "返回主页", actionButtonStyle))
                gameFlow.CloseLevelSelection();
        }

        private void DrawChapterCard(Rect rect, int chapter)
        {
            bool unlocked = chapter == 1 || (chapter == 2 ? gameFlow.IsLevelTwoUnlocked : gameFlow.IsLevelThreeUnlocked);
            string title = chapter == 1 ? GameTextCatalog.TutorialLevelName :
                chapter == 2 ? GameTextCatalog.MainLevelName : GameTextCatalog.BambooValleyLevelName;
            string caption = chapter == 1 ? "卷一  /  三十息教学" : chapter == 2 ? "卷二  /  六十息历练" : "卷三  /  竹谷寻踪";
            string description = chapter == 1 ? "药草、宝箱、洞穴与对手\n从第一次探索开始" :
                chapter == 2 ? (unlocked ? "择路探索，搭配武学\n最终迎战" + GameTextCatalog.FinalBossName : "完成或跳过教学后解锁") :
                (unlocked ? "竹林过桥，武馆历练\n探寻幽谷深处的山洞" : "通关" + GameTextCatalog.MainLevelName + "后解锁");
            Color accent = unlocked ? WuxiaUiTheme.Brass : WuxiaUiTheme.TextDisabled;
            WuxiaUiTheme.DrawPanel(rect, WuxiaUiTheme.BackgroundBrown, accent);
            GUI.Label(new Rect(rect.x + 16, rect.y + 12, rect.width - 32, 24), caption, coverCaption);
            GUI.Label(new Rect(rect.x + 16, rect.y + 42, rect.width - 32, 34), title, coverHeading);
            GUI.Label(new Rect(rect.x + 16, rect.y + 80, rect.width - 32, rect.height - (ResponsiveGui.IsPortrait ? 164 : 144)), description, coverBody);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && unlocked;
            string action = !unlocked ? "尚未解锁" : chapter == 1 ? "开始教学" : "进入关卡";
            if (GUI.Button(new Rect(rect.x + 16, rect.yMax - (ResponsiveGui.IsPortrait ? 76 : 56), rect.width - 32,
                ResponsiveGui.IsPortrait ? PortraitUiLayout.ActionHeight : 44), action,
                    unlocked && chapter > 1 ? mainMenuButtonStyle : actionButtonStyle))
            {
                if (chapter == 1) gameFlow.SelectTutorialLevel();
                else if (chapter == 2) gameFlow.SelectLevelTwo();
                else gameFlow.SelectLevelThree();
            }
            GUI.enabled = wasEnabled;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
