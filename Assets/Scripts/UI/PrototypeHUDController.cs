using System;
using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Audio;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.UI
{
    /// <summary>
    /// Shared scale for the prototype IMGUI surfaces.
    /// The original WebGL layout was authored around a 960 x 540 16:9 canvas.
    /// Larger canvases scale the whole interface proportionally, while smaller
    /// editor windows keep the existing pixel sizes so controls remain usable.
    /// </summary>
    public static class ResponsiveGui
    {
        public const float ReferenceWidth = 960f;
        public const float ReferenceHeight = 540f;
        public const float PortraitReferenceWidth = 540f;
        public const float PortraitReferenceHeight = 960f;

        public static float Scale => CalculateScale(Screen.width, Screen.height);
        public static float Width => Screen.width / Scale;
        public static float Height => Screen.height / Scale;
        public static bool IsPortrait
        {
            get
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null && mainCamera.pixelWidth > 0 && mainCamera.pixelHeight > 0)
                {
                    return mainCamera.pixelHeight > mainCamera.pixelWidth;
                }

                return Screen.height > Screen.width;
            }
        }
        public static Rect SafeArea
        {
            get
            {
                return CalculateSafeArea(Screen.safeArea, Screen.width, Screen.height);
            }
        }

        public static Rect CalculateSafeArea(
            Rect safePixels,
            float screenWidth,
            float screenHeight)
        {
            float scale = CalculateScale(screenWidth, screenHeight);
            return new Rect(
                safePixels.x / scale,
                (screenHeight - safePixels.yMax) / scale,
                safePixels.width / scale,
                safePixels.height / scale);
        }

        public static float CalculateScale(float screenWidth, float screenHeight)
        {
            bool portrait = screenHeight > screenWidth;
            float referenceWidth = portrait ? PortraitReferenceWidth : ReferenceWidth;
            float referenceHeight = portrait ? PortraitReferenceHeight : ReferenceHeight;
            float widthScale = screenWidth / referenceWidth;
            float heightScale = screenHeight / referenceHeight;
            return Mathf.Max(1f, Mathf.Min(widthScale, heightScale));
        }

        public static Matrix4x4 ApplyScale(float scale)
        {
            Matrix4x4 original = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * original;
            return original;
        }

        public static Vector2 ScreenPointToGui(Vector3 screenPoint, float scale)
        {
            return new Vector2(screenPoint.x / scale, (Screen.height - screenPoint.y) / scale);
        }

        public static Vector2 MousePosition(float scale)
        {
            Vector3 mouse = Input.mousePosition;
            return new Vector2(mouse.x / scale, (Screen.height - mouse.y) / scale);
        }

        public static float PreferredSingleLineWidth(string text, GUIStyle style, float padding = 0f)
        {
            if (style == null)
            {
                return padding;
            }

            bool originalWordWrap = style.wordWrap;
            TextClipping originalClipping = style.clipping;
            style.wordWrap = false;
            style.clipping = TextClipping.Overflow;
            float width = style.CalcSize(new GUIContent(text ?? string.Empty)).x + padding;
            style.wordWrap = originalWordWrap;
            style.clipping = originalClipping;
            return width;
        }

        public static void DrawSingleLineLabel(Rect rect, string text, GUIStyle style, int minimumFontSize = 10)
        {
            if (style == null)
            {
                return;
            }

            int originalFontSize = style.fontSize;
            bool originalWordWrap = style.wordWrap;
            TextClipping originalClipping = style.clipping;
            int startingFontSize = Mathf.Max(minimumFontSize,
                originalFontSize > 0 ? originalFontSize : GUI.skin.label.fontSize);
            GUIContent content = new GUIContent(text ?? string.Empty);

            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            style.fontSize = startingFontSize;
            // CJK fonts usually report a taller line box than Latin fonts. Shrinking
            // against that reported height made otherwise valid Chinese labels tiny.
            // The label rect already clips vertically, so only fit the single line
            // against the available width here.
            while (style.fontSize > minimumFontSize)
            {
                Vector2 measured = style.CalcSize(content);
                if (measured.x <= rect.width)
                {
                    break;
                }

                style.fontSize -= 1;
            }

            GUI.Label(rect, content, style);
            style.fontSize = originalFontSize;
            style.wordWrap = originalWordWrap;
            style.clipping = originalClipping;
        }
    }

    [DefaultExecutionOrder(-1000)]
    public class PrototypeHUDController : MonoBehaviour
    {
        [Serializable]
        public class IconEntry
        {
            public string id;
            public Texture2D icon;
        }

        private enum CharacterView
        {
            Status,
            Equipment
        }

        public GameFlowController gameFlow;
        public PlayerStats playerStats;
        public BattleManager battleManager;
        public MainMapMusicController musicController;
        public Texture2D statusIcon;
        public Texture2D equipmentIcon;
        public Texture2D healthBarBase;
        public Texture2D healthBarFill;
        public Texture2D mainMenuBackground;
        [Header("Main Map HUD")]
        public Texture2D playerPortrait;
        public Texture2D playerPortraitFrame;
        public Texture2D timeHudIcon;
        public Texture2D copperHudIcon;
        public Texture2D cultivationHudIcon;
        public Texture2D bossPortrait;
        public Texture2D bossPortraitFrame;
        public IconEntry[] martialArtIcons;
        public IconEntry[] equipmentItemIcons;

        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle centeredStyle;
        private GUIStyle iconButtonStyle;
        private GUIStyle tabStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle actionButtonStyle;
        private GUIStyle tooltipEffectStyle;
        private GUIStyle mainMenuTitleStyle;
        private GUIStyle mainMenuSubtitleStyle;
        private GUIStyle mainMenuButtonStyle;
        private GUIStyle tutorialNoticeStyle;
        private GUIStyle levelCardTitleStyle;
        private GUIStyle settingsToggleStyle;
        private GUIStyle warningHeadingStyle;
        private GUIStyle dangerHeadingStyle;
        private GUIStyle timeSecondsStyle;
        private GUIStyle timerCaptionStyle;
        private GUIStyle bossWarningStyle;
        private GUIStyle bossCountdownStyle;
        private GUIStyle bossIntroTitleStyle;
        private GUIStyle bossIntroNameStyle;
        private GUIStyle bossDialogueSpeakerStyle;
        private GUIStyle bossDialogueBodyStyle;
        private GUIStyle hudValueStyle;
        private GUIStyle skillBadgeStyle;
        private GUIStyle skillCooldownStyle;
        private GUIStyle skillReadyStyle;
        private Texture2D runtimeSettingsIcon;
        private Texture2D runtimeHomeIcon;
        private Texture2D runtimeSkipTutorialIcon;
        private Texture2D runtimePortraitBackdrop;
        private BattleScreenController battleScreen;
        private readonly System.Collections.Generic.List<PlayerStats.TimedBuffSnapshot> timedBuffBuffer =
            new System.Collections.Generic.List<PlayerStats.TimedBuffSnapshot>(4);
        private WuxiaRoguelite.Runtime.CombatantStats trackedHudStats;
        private float previousHudHealth;
        private float healthBeforeDamageRatio;
        private float healthDamageStartedAt = -10f;
        private bool characterPanelOpen;
        private bool settingsOpen;
        private bool debugVisible;
        private CharacterView currentView;
        private Vector2 statusScroll;
        private Vector2 inventoryScroll;
        private float timeScaleBeforeSettings = 1f;
        private static int settingsEscapeFrame = -1;

        public static bool IsSettingsOpen { get; private set; }
        public static bool BlocksGameplayEscape =>
            IsSettingsOpen || Time.frameCount == settingsEscapeFrame;

        private static readonly Color Ink = WuxiaUiTheme.BackgroundInk;
        private static readonly Color Panel = WuxiaUiTheme.SurfaceIron;
        private static readonly Color PanelLight = WuxiaUiTheme.SurfaceRaised;
        private static readonly Color Jade = WuxiaUiTheme.Jade;
        private static readonly Color Gold = WuxiaUiTheme.Gold;
        private static readonly Color Paper = WuxiaUiTheme.TextPrimary;
        private static readonly Color Muted = WuxiaUiTheme.TextSecondary;
        private static readonly Color Crimson = WuxiaUiTheme.Danger;
        private const float SkillReadyHighlightDuration = 0.72f;
        private const float HealthLossTrailDuration = 0.82f;

        private void Awake()
        {
            if (musicController == null)
            {
                musicController = FindAnyObjectByType<MainMapMusicController>();
            }

            battleScreen = FindAnyObjectByType<BattleScreenController>();
            ResetHealthFeedbackTracking();
        }

        private void Update()
        {
            UpdateHealthFeedback();

            if (gameFlow != null &&
                (gameFlow.IsLevelTwoDifficultyNoticeActive ||
                 gameFlow.IsBossIntroActive || gameFlow.IsOpeningIntroActive))
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                settingsEscapeFrame = Time.frameCount;
                SetSettingsOpen(!settingsOpen);
                return;
            }

            if (settingsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                debugVisible = !debugVisible;
            }

            if (gameFlow != null && gameFlow.CurrentPhase == GamePhase.MainMapRunning)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    ToggleCharacterPanel(CharacterView.Status);
                }
                else if (Input.GetKeyDown(KeyCode.B))
                {
                    ToggleCharacterPanel(CharacterView.Equipment);
                }
            }

            if (gameFlow != null && gameFlow.CurrentPhase != GamePhase.MainMapRunning)
            {
                SetCharacterScreenOpen(false);
            }
        }

        private void OnDisable()
        {
            SetSettingsOpen(false);
            SetCharacterScreenOpen(false);
            IsSettingsOpen = false;
            settingsEscapeFrame = -1;
        }

        private void OnDestroy()
        {
            if (runtimeSettingsIcon != null)
            {
                Destroy(runtimeSettingsIcon);
            }

            if (runtimeHomeIcon != null)
            {
                Destroy(runtimeHomeIcon);
            }

            if (runtimeSkipTutorialIcon != null)
            {
                Destroy(runtimeSkipTutorialIcon);
            }

            if (runtimePortraitBackdrop != null)
            {
                Destroy(runtimePortraitBackdrop);
            }
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            if (gameFlow == null || playerStats == null || playerStats.runtimeStats == null)
            {
                return;
            }

            GUI.depth = settingsOpen ? -2000 : -1500;
            EnsureStyles();
            float guiScale = ResponsiveGui.Scale;
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(guiScale);
            try
            {
                if (gameFlow.IsLevelTwoDifficultyNoticeActive)
                {
                    DrawLevelTwoDifficultyNotice();
                    return;
                }

                if (settingsOpen)
                {
                    DrawSettingsPanel();
                    return;
                }

                if (gameFlow.IsTutorialNoticeActive)
                {
                    DrawTutorialNotice();
                    return;
                }

                if (gameFlow.IsOpeningIntroActive)
                {
                    DrawTutorialSkipButton();
                    return;
                }

                if (battleManager != null && battleManager.IsBattleActive)
                {
                    DrawUnifiedCombatHud();
                    DrawTutorialSkipButton();
                    DrawSettingsButton();
                    return;
                }

                if (gameFlow.CurrentPhase == GamePhase.Ready)
                {
                    if (gameFlow.IsLevelSelectionOpen)
                    {
                        DrawLevelSelection();
                    }
                    else
                    {
                        DrawMainMenu();
                    }
                    DrawTutorialSkipButton();
                    DrawSettingsButton();
                    return;
                }

                if (gameFlow.IsBossIntroActive)
                {
                    DrawBossIntroOverlay();
                    DrawTutorialSkipButton();
                    return;
                }

                if (gameFlow.CurrentPhase != GamePhase.Result && gameFlow.CurrentPhase != GamePhase.CaveRunning && !characterPanelOpen)
                {
                    DrawCompactHud();
                }

                if (gameFlow.CurrentPhase == GamePhase.MainMapRunning)
                {
                    DrawBossApproachWarning();
                    if (characterPanelOpen)
                    {
                        DrawCharacterScreen();
                    }
                    else
                    {
                        DrawCharacterButtons();
                    }
                }

                if (gameFlow.CurrentPhase == GamePhase.Result)
                {
                    DrawResultPanel();
                    return;
                }

                if (gameFlow.CurrentPhase == GamePhase.LevelUpPaused)
                {
                    DrawLevelUpPanel();
                }

                if (debugVisible && !characterPanelOpen)
                {
                    DrawDebugControls();
                }

                DrawTutorialSkipButton();
                DrawSettingsButton();
            }
            finally
            {
                GUI.matrix = originalGuiMatrix;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = LabelStyle(22, FontStyle.Bold, TextAnchor.MiddleLeft, Paper);
            headingStyle = LabelStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, Paper);
            bodyStyle = LabelStyle(14, FontStyle.Normal, TextAnchor.MiddleLeft, Paper);
            mutedStyle = LabelStyle(12, FontStyle.Normal, TextAnchor.MiddleLeft, Muted);
            centeredStyle = LabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, Paper);

            iconButtonStyle = WuxiaUiTheme.CreateButtonStyle(13, WuxiaButtonKind.Icon);
            iconButtonStyle.fixedWidth = 48f;
            iconButtonStyle.fixedHeight = 48f;
            tabStyle = WuxiaUiTheme.CreateButtonStyle(14, WuxiaButtonKind.Tab);
            activeTabStyle = WuxiaUiTheme.CreateButtonStyle(14, WuxiaButtonKind.Tab,
                TextAnchor.MiddleCenter, true);
            actionButtonStyle = WuxiaUiTheme.CreateButtonStyle(13, WuxiaButtonKind.Secondary);
            tooltipEffectStyle = LabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft,
                new Color(1f, 0.80f, 0.35f));
            mainMenuTitleStyle = LabelStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.97f, 0.91f, 0.72f));
            mainMenuSubtitleStyle = LabelStyle(15, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Color(0.84f, 0.84f, 0.78f));
            mainMenuButtonStyle = WuxiaUiTheme.CreateButtonStyle(18, WuxiaButtonKind.Primary);
            tutorialNoticeStyle = LabelStyle(46, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.97f, 0.91f, 0.72f));
            levelCardTitleStyle = LabelStyle(21, FontStyle.Bold, TextAnchor.MiddleCenter, Paper);
            settingsToggleStyle = WuxiaUiTheme.CreateButtonStyle(15, WuxiaButtonKind.Secondary);
            warningHeadingStyle = LabelStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.76f, 0.32f));
            dangerHeadingStyle = LabelStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.30f, 0.20f));
            timeSecondsStyle = LabelStyle(31, FontStyle.Bold, TextAnchor.MiddleCenter, Gold);
            timerCaptionStyle = LabelStyle(10, FontStyle.Bold, TextAnchor.MiddleCenter, Muted);
            bossWarningStyle = LabelStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.82f, 0.46f));
            bossCountdownStyle = LabelStyle(64, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.22f, 0.14f));
            bossIntroTitleStyle = LabelStyle(21, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.78f, 0.38f));
            bossIntroNameStyle = LabelStyle(42, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.94f, 0.78f));
            bossDialogueSpeakerStyle = LabelStyle(18, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.78f, 0.38f));
            bossDialogueBodyStyle = LabelStyle(17, FontStyle.Normal, TextAnchor.UpperLeft, Paper);
            hudValueStyle = LabelStyle(12, FontStyle.Bold, TextAnchor.MiddleCenter, Paper);
            skillBadgeStyle = LabelStyle(9, FontStyle.Bold, TextAnchor.MiddleCenter, Paper);
            skillCooldownStyle = LabelStyle(12, FontStyle.Bold, TextAnchor.MiddleCenter, Paper);
            skillReadyStyle = LabelStyle(9, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.94f, 0.70f));
            runtimeSettingsIcon = CreateSettingsIcon(64);
            runtimeHomeIcon = CreateHomeIcon(64);
            runtimeSkipTutorialIcon = CreateSkipTutorialIcon(64);
            runtimePortraitBackdrop = CreateCircleTexture(64,
                new Color(0.045f, 0.055f, 0.052f, 0.96f));
        }

        private void SetSettingsOpen(bool open)
        {
            if (settingsOpen == open)
            {
                return;
            }

            if (open && characterPanelOpen)
            {
                SetCharacterScreenOpen(false);
            }

            settingsOpen = open;
            IsSettingsOpen = open;
            if (open)
            {
                timeScaleBeforeSettings = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = timeScaleBeforeSettings;
            }
        }

        private void DrawSettingsPanel()
        {
            Rect screen = new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height);
            FillRect(screen, new Color(0.015f, 0.02f, 0.02f, 0.82f));

            Rect panel = CenteredRect(440f, 404f);
            DrawPanel(panel, Panel, Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 22f, panel.y + 14f, panel.width - 44f, 38f),
                "设置", titleStyle, 18);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 22f, panel.y + 52f, panel.width - 44f, 22f),
                "游戏已暂停", mutedStyle, 10);

            Rect musicRow = new Rect(panel.x + 22f, panel.y + 78f, panel.width - 44f, 62f);
            DrawPanel(musicRow, PanelLight, Jade);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(musicRow.x + 14f, musicRow.y + 5f, musicRow.width - 150f, 28f),
                "背景音乐", headingStyle, 11);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(musicRow.x + 14f, musicRow.y + 31f, musicRow.width - 150f, 22f),
                "包含主地图、战斗、洞穴与决战音乐", mutedStyle, 9);

            bool musicEnabled = musicController == null || musicController.MusicEnabled;
            string musicButtonText = musicEnabled ? "已开启" : "已关闭";
            if (GUI.Button(
                    new Rect(musicRow.xMax - 118f, musicRow.y + 14f, 102f, 34f),
                    musicButtonText, settingsToggleStyle))
            {
                if (musicController == null)
                {
                    musicController = FindAnyObjectByType<MainMapMusicController>();
                }

                musicController?.SetMusicEnabled(!musicEnabled);
            }

            Rect orientationRow = new Rect(panel.x + 22f, panel.y + 154f, panel.width - 44f, 92f);
            DrawPanel(orientationRow, PanelLight, Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(orientationRow.x + 14f, orientationRow.y + 5f, orientationRow.width - 28f, 26f),
                "画面方向", headingStyle, 11);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(orientationRow.x + 14f, orientationRow.y + 29f, orientationRow.width - 214f, 44f),
                "手机端会记住选择，并立即切换横屏或竖屏。", mutedStyle, 9);

            bool portraitSelected = MobileDisplaySettings.PrefersPortrait;
            float orientationButtonWidth = 82f;
            if (GUI.Button(
                    new Rect(orientationRow.xMax - 184f, orientationRow.y + 32f, orientationButtonWidth, 38f),
                    "竖屏", portraitSelected ? activeTabStyle : tabStyle))
            {
                MobileDisplaySettings.SetPortrait(true);
            }

            if (GUI.Button(
                    new Rect(orientationRow.xMax - 94f, orientationRow.y + 32f, orientationButtonWidth, 38f),
                    "横屏", !portraitSelected ? activeTabStyle : tabStyle))
            {
                MobileDisplaySettings.SetPortrait(false);
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 22f, panel.y + 260f, panel.width - 44f, 24f),
                "操作", headingStyle, 11);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 22f, panel.y + 288f, panel.width - 44f, 22f),
                "移动：竖屏滑动 / 横屏摇杆 / 键盘 W、A、S、D", bodyStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 22f, panel.y + 312f, panel.width - 44f, 22f),
                "快捷键：P 角色状态 · B 装备背包 · Esc 设置", mutedStyle, 9);

            Rect bottomRow = new Rect(panel.x + 22f, panel.yMax - 52f, panel.width - 44f, 34f);
            const float homeButtonWidth = 136f;
            if (GUI.Button(
                    new Rect(bottomRow.x, bottomRow.y, homeButtonWidth, bottomRow.height),
                    new GUIContent("返回首页", runtimeHomeIcon, "返回游戏开始画面"), settingsToggleStyle))
            {
                SetSettingsOpen(false);
                gameFlow.ReturnToMainMenu();
            }

            if (GUI.Button(
                    new Rect(bottomRow.x + homeButtonWidth + 10f, bottomRow.y,
                        bottomRow.width - homeButtonWidth - 10f, bottomRow.height),
                    "返回游戏", actionButtonStyle))
            {
                SetSettingsOpen(false);
            }
        }

        private void DrawSettingsButton()
        {
            Rect safe = ResponsiveGui.SafeArea;
            Rect settingsRect = new Rect(safe.xMax - 58f, safe.y + 10f, 48f, 48f);
            if (GUI.Button(settingsRect, new GUIContent(runtimeSettingsIcon, "设置"), iconButtonStyle))
            {
                SetSettingsOpen(true);
            }
        }

        private void DrawTutorialSkipButton()
        {
            if (!gameFlow.IsTutorialLevel)
            {
                return;
            }

            Rect buttonRect = GetTutorialSkipButtonRect();
            if (GUI.Button(buttonRect,
                    new GUIContent(runtimeSkipTutorialIcon, "跳过关卡1，确认难度提示后进入关卡2"),
                    iconButtonStyle))
            {
                gameFlow.SkipTutorialLevel();
                return;
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(buttonRect.x - 6f, buttonRect.yMax + 1f, buttonRect.width + 12f, 18f),
                "跳过", mutedStyle, 9);
        }

        private static Rect GetTutorialSkipButtonRect()
        {
            Rect safe = ResponsiveGui.SafeArea;
            return new Rect(safe.xMax - 58f, safe.y + 68f, 48f, 48f);
        }

        private void DrawMainMenu()
        {
            Rect screen = new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height);
            if (mainMenuBackground != null)
            {
                GUI.DrawTexture(screen, mainMenuBackground, ScaleMode.ScaleAndCrop, true);
            }
            else
            {
                FillRect(screen, new Color(0.08f, 0.10f, 0.10f));
            }

            FillRect(screen, new Color(0.025f, 0.035f, 0.035f, 0.28f));

            Rect safe = ResponsiveGui.SafeArea;
            float panelWidth = Mathf.Min(460f, safe.width - 32f);
            float panelHeight = Mathf.Min(268f, safe.height - 32f);
            Rect panel = new Rect(
                safe.x + (safe.width - panelWidth) * 0.5f,
                safe.y + (safe.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);
            DrawPanel(panel, new Color(0.045f, 0.055f, 0.052f, 0.82f), Gold);

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 20f, panel.y + 22f, panel.width - 40f, 54f),
                "一炷江湖", mainMenuTitleStyle, 28);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 28f, panel.y + 78f, panel.width - 56f, 28f),
                "六十息择路 · 历战成长 · 终迎强敌", mainMenuSubtitleStyle, 11);

            float lineWidth = Mathf.Min(250f, panel.width - 80f);
            FillRect(new Rect(panel.center.x - lineWidth * 0.5f, panel.y + 116f, lineWidth, 1f),
                new Color(Gold.r, Gold.g, Gold.b, 0.65f));

            Rect startButton = new Rect(panel.center.x - 94f, panel.yMax - 86f, 188f, 46f);
            if (GUI.Button(startButton, "选择关卡", mainMenuButtonStyle))
            {
                gameFlow.OpenLevelSelection();
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 24f, panel.yMax - 34f, panel.width - 48f, 20f),
                "移动探索 · 碰怪自动战斗 · 寻找洞穴与宝箱", mainMenuSubtitleStyle, 10);
        }

        private void DrawLevelSelection()
        {
            Rect screen = new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height);
            if (mainMenuBackground != null)
            {
                GUI.DrawTexture(screen, mainMenuBackground, ScaleMode.ScaleAndCrop, true);
            }
            else
            {
                FillRect(screen, new Color(0.08f, 0.10f, 0.10f));
            }
            FillRect(screen, new Color(0.025f, 0.035f, 0.035f, 0.52f));

            Rect safe = ResponsiveGui.SafeArea;
            float width = Mathf.Min(720f, safe.width - 28f);
            float height = Mathf.Min(360f, safe.height - 28f);
            Rect panel = new Rect(safe.center.x - width * 0.5f, safe.center.y - height * 0.5f, width, height);
            DrawPanel(panel, new Color(0.045f, 0.055f, 0.052f, 0.93f), Gold);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 16f, panel.width - 48f, 42f), "选择关卡", mainMenuTitleStyle);

            float gap = 14f;
            float cardWidth = (panel.width - 52f - gap) * 0.5f;
            Rect tutorialCard = new Rect(panel.x + 26f, panel.y + 74f, cardWidth, 194f);
            Rect levelTwoCard = new Rect(tutorialCard.xMax + gap, tutorialCard.y, cardWidth, tutorialCard.height);
            DrawPanel(tutorialCard, Ink, Jade);
            DrawPanel(levelTwoCard, Ink, gameFlow.IsLevelTwoUnlocked ? Gold : WuxiaUiTheme.TextDisabled);

            GUI.Label(new Rect(tutorialCard.x + 12f, tutorialCard.y + 14f, tutorialCard.width - 24f, 34f),
                "关卡1 · 初入江湖", levelCardTitleStyle);
            GUI.Label(new Rect(tutorialCard.x + 22f, tutorialCard.y + 54f, tutorialCard.width - 44f, 62f),
                "东南西北各有一处互动目标\n三十息，自由探索", centeredStyle);
            if (GUI.Button(new Rect(tutorialCard.x + 22f, tutorialCard.yMax - 52f, tutorialCard.width - 44f, 36f),
                    gameFlow.IsLevelTwoUnlocked ? "重温教学" : "开始教学", mainMenuButtonStyle))
            {
                gameFlow.SelectTutorialLevel();
            }

            GUI.Label(new Rect(levelTwoCard.x + 12f, levelTwoCard.y + 14f, levelTwoCard.width - 24f, 34f),
                "关卡2 · 驿路风云", levelCardTitleStyle);
            GUI.Label(new Rect(levelTwoCard.x + 22f, levelTwoCard.y + 54f, levelTwoCard.width - 44f, 62f),
                gameFlow.IsLevelTwoUnlocked
                    ? $"完整地图与构筑路线\n最终迎战{GameTextCatalog.FinalBossName}"
                    : "完成关卡1后解锁",
                centeredStyle);
            GUI.enabled = gameFlow.IsLevelTwoUnlocked;
            if (GUI.Button(new Rect(levelTwoCard.x + 22f, levelTwoCard.yMax - 52f, levelTwoCard.width - 44f, 36f),
                    gameFlow.IsLevelTwoUnlocked ? "进入关卡2" : "尚未解锁", mainMenuButtonStyle))
            {
                gameFlow.SelectLevelTwo();
            }
            GUI.enabled = true;

            if (GUI.Button(new Rect(panel.x + 24f, panel.yMax - 48f, 112f, 30f), "返回", actionButtonStyle))
            {
                gameFlow.CloseLevelSelection();
            }
        }

        private void DrawTutorialNotice()
        {
            DrawCenteredClickNotice("你只有30秒!", gameFlow.DismissTutorialNotice, true);
        }

        private void DrawLevelTwoDifficultyNotice()
        {
            DrawCenteredClickNotice("难度飙升！！！", gameFlow.DismissLevelTwoDifficultyNotice, false);
        }

        private void DrawCenteredClickNotice(string message, Action dismissAction, bool showTutorialSkip)
        {
            Rect screen = new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height);
            FillRect(screen, new Color(0.02f, 0.025f, 0.025f, 0.78f));
            Rect safe = ResponsiveGui.SafeArea;
            float width = Mathf.Min(560f, safe.width - 32f);
            float height = Mathf.Min(210f, safe.height - 32f);
            Rect card = new Rect(safe.center.x - width * 0.5f, safe.center.y - height * 0.5f, width, height);
            DrawPanel(card, new Color(0.10f, 0.085f, 0.065f, 0.98f), Gold);
            GUI.Label(new Rect(card.x + 24f, card.y + 42f, card.width - 48f, 76f),
                message, tutorialNoticeStyle);

            if (showTutorialSkip)
            {
                DrawTutorialSkipButton();
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                (!showTutorialSkip || !GetTutorialSkipButtonRect().Contains(Event.current.mousePosition)))
            {
                dismissAction?.Invoke();
                Event.current.Use();
            }
        }

        private void DrawCompactHud()
        {
            Rect safe = ResponsiveGui.SafeArea;
            bool portraitLayout = ResponsiveGui.IsPortrait;
            bool timePaused = gameFlow.CurrentPhase == GamePhase.LevelUpPaused;
            float hudWidth = portraitLayout
                ? Mathf.Min(324f, safe.width - 112f)
                : Mathf.Min(318f, safe.width - 92f);
            float portraitSize = portraitLayout ? 58f : 64f;
            float portraitInset = 4f;
            float topHeight = portraitLayout ? 60f : 66f;
            float detailHeight = portraitLayout ? 24f : 26f;
            float loadoutHeight = portraitLayout ? 44f : 50f;
            float hudY = portraitLayout ? safe.y + 82f : safe.y + 12f;
            Rect hud = new Rect(safe.x + 12f, hudY, hudWidth,
                topHeight + 4f + detailHeight + 4f + loadoutHeight);
            float timeRatio = GetMainTimeRatio();
            Color accent = timePaused ? WuxiaUiTheme.Paused : GetMainTimeColor(timeRatio);

            DrawMainTimerWidget(safe, portraitLayout, timeRatio, timePaused, accent);

            Rect card = new Rect(
                hud.x + portraitSize * 0.66f,
                hud.y + 4f,
                hud.width - portraitSize * 0.66f,
                topHeight - 4f);
            DrawPanel(card, Ink, accent);

            Rect portrait = new Rect(hud.x, hud.y, portraitSize, portraitSize);
            if (runtimePortraitBackdrop != null)
            {
                GUI.DrawTexture(new Rect(portrait.x + 5f, portrait.y + 5f,
                    portrait.width - 10f, portrait.height - 10f), runtimePortraitBackdrop,
                    ScaleMode.ScaleToFit, true);
            }
            if (playerPortrait != null)
            {
                GUI.DrawTexture(new Rect(
                        portrait.x + portraitInset,
                        portrait.y + portraitInset,
                        portrait.width - portraitInset * 2f,
                        portrait.height - portraitInset * 2f),
                    playerPortrait, ScaleMode.ScaleAndCrop, true);
            }
            if (playerPortraitFrame != null)
            {
                GUI.DrawTexture(portrait, playerPortraitFrame, ScaleMode.ScaleToFit, true);
            }

            float badgeInset = 10f;
            float badgeHeight = portraitLayout ? 13f : 14f;
            Rect levelBadge = new Rect(portrait.x + badgeInset, portrait.yMax - badgeHeight,
                portrait.width - badgeInset * 2f, badgeHeight);
            FillRect(levelBadge, new Color(0.025f, 0.03f, 0.03f, 0.94f));
            ResponsiveGui.DrawSingleLineLabel(levelBadge, $"{playerStats.level}级", centeredStyle, 8);

            float contentX = portrait.xMax - 5f;
            float contentWidth = hud.xMax - contentX - 7f;
            string playerName = string.IsNullOrEmpty(playerStats.baseStats.displayName)
                ? "无名少侠"
                : playerStats.baseStats.displayName;
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(contentX + 7f, hud.y + 4f, contentWidth - 14f, 20f),
                playerName, headingStyle, 10);

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(contentX + 7f, hud.y + 26f, contentWidth - 14f, 15f),
                $"气血  {playerStats.runtimeStats.currentHealth:0}/{playerStats.runtimeStats.maxHealth:0}",
                hudValueStyle, 8);
            Rect healthRect = new Rect(contentX + 7f, hud.y + 43f, contentWidth - 14f, 13f);
            DrawHealthBar(healthRect, playerStats.runtimeStats.HealthRatio);

            Rect detailRow = new Rect(hud.x, hud.y + topHeight + 4f, hud.width, detailHeight);
            DrawHudDetailRow(detailRow, portraitLayout, accent);

            Rect loadoutRow = new Rect(hud.x, detailRow.yMax + 4f, hud.width, loadoutHeight);
            DrawLoadoutStrip(loadoutRow, false);

            float preferredStatusWidth =
                ResponsiveGui.PreferredSingleLineWidth(gameFlow.statusMessage, bodyStyle, 28f);
            float statusWidth = Mathf.Clamp(preferredStatusWidth,
                ResponsiveGui.IsPortrait ? 260f : 360f, safe.width - 28f);
            float messageY = ResponsiveGui.IsPortrait ? safe.yMax - 48f : safe.yMax - 44f;
            Rect message = new Rect(safe.x + (safe.width - statusWidth) * 0.5f,
                messageY, statusWidth, 30f);
            DrawPanel(message, new Color(0.03f, 0.04f, 0.04f, 0.84f), Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(message.x + 12f, message.y + 3f, message.width - 24f, message.height - 6f),
                gameFlow.statusMessage, bodyStyle, 10);
        }

        private void DrawMainTimerWidget(
            Rect safe,
            bool portraitLayout,
            float timeRatio,
            bool paused,
            Color accent)
        {
            float dialSize = portraitLayout ? 64f : 86f;
            float top = safe.y + 4f;
            Rect dial = new Rect(safe.center.x - dialSize * 0.5f, top, dialSize, dialSize);
            WuxiaUiTheme.DrawTimerDial(dial, accent);

            timeSecondsStyle.normal.textColor = accent;
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(dial.x + 10f, dial.y + (portraitLayout ? 19f : 24f),
                    dial.width - 20f, portraitLayout ? 31f : 38f),
                Mathf.CeilToInt(gameFlow.mainTimeRemaining).ToString("00"), timeSecondsStyle,
                portraitLayout ? 18 : 20);
            timerCaptionStyle.normal.textColor = paused ? WuxiaUiTheme.Paused : Muted;
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(dial.x + 8f, dial.y + (portraitLayout ? 7f : 10f),
                    dial.width - 16f, 15f),
                paused ? "主时间暂停" : "江湖时限", timerCaptionStyle, 8);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(dial.x + 8f, dial.yMax - (portraitLayout ? 19f : 25f),
                    dial.width - 16f, 13f),
                "秒", timerCaptionStyle, 8);

            float trackWidth = portraitLayout ? 112f : 138f;
            Rect track = new Rect(safe.center.x - trackWidth * 0.5f,
                dial.yMax - (portraitLayout ? 3f : 4f), trackWidth,
                portraitLayout ? 16f : 20f);
            DrawMainTimeTrack(track, timeRatio, paused);
        }

        private void DrawUnifiedCombatHud()
        {
            Rect safe = ResponsiveGui.SafeArea;
            bool portraitLayout = ResponsiveGui.IsPortrait;
            float width = portraitLayout
                ? Mathf.Max(218f, (safe.width - 44f) * 0.5f)
                : Mathf.Min(286f, safe.width * 0.30f);
            float healthTop = portraitLayout
                ? safe.y + 68f
                : safe.y + 89f;
            float height = portraitLayout ? 96f : 112f;
            Rect hud = new Rect(safe.x + (portraitLayout ? 16f : Mathf.Clamp(ResponsiveGui.Width * 0.055f, 34f, 72f)),
                healthTop, width, height);
            Color accent = gameFlow.CurrentPhase == GamePhase.BossBattle
                ? new Color(0.88f, 0.27f, 0.18f)
                : gameFlow.CurrentPhase == GamePhase.CaveRunning
                    ? new Color(0.30f, 0.66f, 0.90f)
                    : Jade;

            DrawPanel(hud, new Color(0.025f, 0.035f, 0.038f, 0.94f), accent);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(hud.x + 10f, hud.y + 3f, hud.width * 0.58f, 18f),
                playerStats.runtimeStats.displayName, headingStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(hud.x + hud.width * 0.57f, hud.y + 3f, hud.width * 0.38f, 18f),
                $"等级 {playerStats.level}", mutedStyle, 8);

            Rect health = new Rect(hud.x + 10f, hud.y + 23f, hud.width - 20f, 19f);
            DrawHealthBar(health, playerStats.runtimeStats.HealthRatio);
            ResponsiveGui.DrawSingleLineLabel(health,
                $"气血  {playerStats.runtimeStats.currentHealth:0} / {playerStats.runtimeStats.maxHealth:0}",
                hudValueStyle, 8);

            Rect loadout = new Rect(hud.x + 6f, hud.y + (portraitLayout ? 45f : 48f),
                hud.width - 12f, hud.height - (portraitLayout ? 51f : 54f));
            DrawLoadoutStrip(loadout, true);
        }

        private void DrawHudDetailRow(Rect rect, bool portraitLayout, Color accent)
        {
            if (portraitLayout)
            {
                WuxiaUiTheme.DrawCompactSurface(
                    rect, new Color(0.03f, 0.04f, 0.04f, 0.88f), accent);
            }
            else
            {
                DrawPanel(rect, new Color(0.04f, 0.05f, 0.05f, 0.92f), accent);
            }
            float inset = portraitLayout ? 5f : 4f;
            float gap = 4f;
            float contentWidth = rect.width - inset * 2f;
            float copperWidth = portraitLayout ? 92f : 104f;
            float cultivationWidth = contentWidth - copperWidth - gap;
            Rect cultivation = new Rect(rect.x + inset, rect.y + 3f,
                cultivationWidth, rect.height - 6f);
            Rect copper = new Rect(cultivation.xMax + gap, cultivation.y,
                copperWidth, cultivation.height);

            DrawResourceChip(cultivation, cultivationHudIcon, portraitLayout ? "修" : "修为",
                $"{playerStats.cultivation}/{playerStats.NextLevelRequirement}", Jade, portraitLayout);
            DrawResourceChip(copper, copperHudIcon, portraitLayout ? "钱" : "铜钱",
                playerStats.copper.ToString(), Gold, portraitLayout);
        }

        private void DrawResourceChip(
            Rect rect,
            Texture2D icon,
            string label,
            string value,
            Color accent,
            bool compact)
        {
            if (compact)
            {
                FillRect(rect, new Color(0.025f, 0.03f, 0.03f, 0.72f));
                FillRect(new Rect(rect.x, rect.y + 3f, 2f, rect.height - 6f),
                    new Color(accent.r, accent.g, accent.b, 0.82f));
            }
            else
            {
                WuxiaUiTheme.DrawSlot(rect, new Color(0.025f, 0.03f, 0.03f, 0.92f), accent);
            }
            float iconSize = Mathf.Min(compact ? 17f : 22f, rect.height - 4f);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + (rect.height - iconSize) * 0.5f,
                    iconSize, iconSize), icon, ScaleMode.ScaleToFit, true);
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + iconSize + 5f, rect.y, rect.width - iconSize - 7f, rect.height),
                $"{label} {value}", mutedStyle, 8);
        }

        private void DrawLoadoutStrip(Rect rect, bool combatLayout)
        {
            playerStats.GetTimedBuffSnapshots(timedBuffBuffer);
            bool compactPortrait = ResponsiveGui.IsPortrait;
            if (compactPortrait)
            {
                WuxiaUiTheme.DrawCompactSurface(
                    rect, new Color(0.018f, 0.024f, 0.025f, 0.88f),
                    combatLayout ? Jade : Gold);
            }
            else
            {
                WuxiaUiTheme.DrawPanel(rect, new Color(0.018f, 0.024f, 0.025f, 0.94f),
                    combatLayout ? Jade : Gold);
            }

            float headerHeight = compactPortrait ? 0f : combatLayout ? 12f : 13f;
            float gap = 3f;
            float slotSize = Mathf.Min(
                compactPortrait ? 36f : combatLayout ? 37f : 40f,
                rect.height - headerHeight - (compactPortrait ? 8f : 4f));
            int buffCount = Mathf.Min(timedBuffBuffer.Count,
                combatLayout && ResponsiveGui.IsPortrait ? 1 : 2);
            float buffWidth = buffCount > 0 ? buffCount * slotSize + Mathf.Max(0, buffCount - 1) * gap + 8f : 0f;
            float skillWidth = Mathf.Max(slotSize, rect.width - 10f - buffWidth);
            int capacity = Mathf.Max(1, Mathf.FloorToInt((skillWidth + gap) / (slotSize + gap)));
            int learnedCount = playerStats.learnedMartialArts.Count;
            bool needsOverflow = learnedCount > capacity;
            int visibleSkills = Mathf.Min(learnedCount, needsOverflow ? Mathf.Max(0, capacity - 1) : capacity);

            if (!compactPortrait)
            {
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(rect.x + 6f, rect.y, skillWidth - 8f, headerHeight),
                    battleManager != null && battleManager.IsBattleActive ? "武学 · 自动运转" : "武学 · 流派与品类",
                    mutedStyle, 8);
                if (buffCount > 0)
                {
                    ResponsiveGui.DrawSingleLineLabel(
                        new Rect(rect.xMax - buffWidth, rect.y, buffWidth - 2f, headerHeight),
                        "限时增益", mutedStyle, 8);
                }
            }

            float y = rect.y + headerHeight + (compactPortrait ? 4f : 2f);
            float x = rect.x + 5f;
            Rect hoveredRect = default;
            string hoveredTitle = string.Empty;
            string hoveredBody = string.Empty;
            Vector2 mouse = ResponsiveGui.MousePosition(ResponsiveGui.Scale);

            if (learnedCount == 0)
            {
                int emptySlots = Mathf.Min(3, capacity);
                for (int i = 0; i < emptySlots; i++)
                {
                    Rect slot = new Rect(x + i * (slotSize + gap), y, slotSize, slotSize);
                    WuxiaUiTheme.DrawSlot(slot, new Color(0.07f, 0.08f, 0.08f, 0.80f),
                        WuxiaUiTheme.TextDisabled);
                    ResponsiveGui.DrawSingleLineLabel(slot, i == 1 ? "待习" : "·", mutedStyle, 8);
                }
            }
            else
            {
                for (int i = 0; i < visibleSkills; i++)
                {
                    string artId = playerStats.learnedMartialArts[i];
                    Rect slot = new Rect(x + i * (slotSize + gap), y, slotSize, slotSize);
                    DrawSkillSlot(slot, artId);
                    if (slot.Contains(mouse))
                    {
                        MartialArtDefinition definition = MartialArtCatalog.Get(artId);
                        hoveredRect = slot;
                        hoveredTitle = definition == null ? artId : $"{artId} · {definition.category}";
                        hoveredBody = definition == null
                            ? string.Empty
                            : $"{RankName(playerStats.GetMartialArtRank(artId))} · {definition.GetEffectSummary(playerStats.GetMartialArtRank(artId))}";
                    }
                }

                if (needsOverflow)
                {
                    int remaining = learnedCount - visibleSkills;
                    Rect overflow = new Rect(x + visibleSkills * (slotSize + gap), y, slotSize, slotSize);
                    WuxiaUiTheme.DrawSlot(overflow, new Color(0.08f, 0.09f, 0.085f, 0.96f), Gold);
                    ResponsiveGui.DrawSingleLineLabel(overflow, $"+{remaining}", centeredStyle, 10);
                    if (overflow.Contains(mouse))
                    {
                        hoveredRect = overflow;
                        hoveredTitle = "其余武学";
                        hoveredBody = string.Join(" · ", playerStats.learnedMartialArts.Skip(visibleSkills));
                    }
                }
            }

            if (buffCount > 0)
            {
                float dividerX = rect.xMax - buffWidth - 1f;
                FillRect(new Rect(dividerX, y, 1f, slotSize), new Color(Jade.r, Jade.g, Jade.b, 0.38f));
                float buffX = dividerX + 7f;
                for (int i = 0; i < buffCount; i++)
                {
                    PlayerStats.TimedBuffSnapshot buff = timedBuffBuffer[i];
                    Rect slot = new Rect(buffX + i * (slotSize + gap), y, slotSize, slotSize);
                    DrawTimedBuffSlot(slot, buff);
                    if (slot.Contains(mouse))
                    {
                        hoveredRect = slot;
                        hoveredTitle = buff.displayName;
                        hoveredBody = $"{buff.effectSummary} · 剩余 {buff.remainingDuration:0.0} 秒";
                    }
                }
            }

            if (!string.IsNullOrEmpty(hoveredTitle))
            {
                DrawLoadoutTooltip(hoveredRect, hoveredTitle, hoveredBody);
            }
        }

        private void DrawSkillSlot(Rect rect, string artId)
        {
            MartialArtDefinition definition = MartialArtCatalog.Get(artId);
            string category = definition != null ? definition.category : "武学";
            Color featureColor = definition != null
                ? SchoolColor(definition.school)
                : Gold;
            int rank = Mathf.Max(1, playerStats.GetMartialArtRank(artId));
            GetSkillCooldownState(artId, out float cooldownRatio, out bool unavailable, out string cooldownText);

            float activationAge = battleManager == null
                ? 100f
                : Time.unscaledTime - battleManager.GetMartialArtLastActivationTime(artId);
            bool highlighted = activationAge >= 0f && activationAge < SkillReadyHighlightDuration;
            if (highlighted)
            {
                float pulse = 1f - activationAge / SkillReadyHighlightDuration;
                Rect aura = new Rect(rect.x - 3f - pulse * 2f, rect.y - 3f - pulse * 2f,
                    rect.width + 6f + pulse * 4f, rect.height + 6f + pulse * 4f);
                DrawRectOutline(aura, new Color(featureColor.r, featureColor.g, featureColor.b, 0.35f + pulse * 0.65f),
                    2f);
            }

            WuxiaUiTheme.DrawSlot(rect, new Color(0.035f, 0.045f, 0.045f, 0.98f),
                featureColor, highlighted);

            Texture2D icon = FindMartialArtIcon(artId);
            if (icon != null)
            {
                Color previous = GUI.color;
                GUI.color = unavailable
                    ? new Color(0.40f, 0.42f, 0.41f, 0.76f)
                    : Color.white;
                GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f),
                    icon, ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }

            if (unavailable && cooldownRatio > 0f)
            {
                float maskHeight = (rect.height - 4f) * Mathf.Clamp01(cooldownRatio);
                FillRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, maskHeight),
                    new Color(0.04f, 0.05f, 0.05f, 0.68f));
            }

            string badge = CategoryBadge(category);
            Rect categoryBadge = new Rect(rect.x + 2f, rect.yMax - 14f, 17f, 12f);
            FillRect(categoryBadge, new Color(featureColor.r, featureColor.g, featureColor.b, 0.90f));
            ResponsiveGui.DrawSingleLineLabel(categoryBadge, badge, skillBadgeStyle, 7);

            Rect rankBadge = new Rect(rect.xMax - 17f, rect.y + 2f, 15f, 13f);
            FillRect(rankBadge, new Color(0.02f, 0.025f, 0.025f, 0.92f));
            ResponsiveGui.DrawSingleLineLabel(rankBadge, rank.ToString(), skillBadgeStyle, 7);

            if (!string.IsNullOrEmpty(cooldownText) && unavailable)
            {
                FillRect(new Rect(rect.x + 3f, rect.center.y - 9f, rect.width - 6f, 18f),
                    new Color(0.02f, 0.025f, 0.025f, 0.72f));
                ResponsiveGui.DrawSingleLineLabel(rect, cooldownText, skillCooldownStyle, 8);
            }

            if (highlighted)
            {
                Rect ready = new Rect(rect.x + 2f, rect.yMax - 13f, rect.width - 4f, 11f);
                FillRect(ready, new Color(featureColor.r, featureColor.g, featureColor.b, 0.88f));
                ResponsiveGui.DrawSingleLineLabel(ready, SkillFeatureLabel(artId), skillReadyStyle, 7);
            }
        }

        private void DrawTimedBuffSlot(Rect rect, PlayerStats.TimedBuffSnapshot buff)
        {
            float pulse = buff.remainingDuration <= 0.7f
                ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f))
                : 0f;
            Color accent = new Color(0.28f + pulse * 0.22f, 0.78f, 0.59f, 1f);
            WuxiaUiTheme.DrawSlot(rect, new Color(0.025f, 0.045f, 0.04f, 0.98f),
                accent, pulse > 0f);

            Texture2D icon = FindMartialArtIcon(buff.iconId);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f),
                    icon, ScaleMode.ScaleToFit, true);
            }

            float elapsedRatio = 1f - buff.RemainingRatio;
            if (elapsedRatio > 0f)
            {
                FillRect(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f,
                        (rect.height - 4f) * elapsedRatio),
                    new Color(0.02f, 0.035f, 0.03f, 0.42f));
            }

            Rect timer = new Rect(rect.x + 2f, rect.yMax - 15f, rect.width - 4f, 13f);
            FillRect(timer, new Color(0.015f, 0.025f, 0.022f, 0.88f));
            ResponsiveGui.DrawSingleLineLabel(timer, $"{buff.remainingDuration:0.0}秒", skillCooldownStyle, 7);
            if (buff.stackCount > 1)
            {
                Rect stack = new Rect(rect.xMax - 18f, rect.y + 2f, 16f, 13f);
                FillRect(stack, new Color(0.02f, 0.025f, 0.025f, 0.92f));
                ResponsiveGui.DrawSingleLineLabel(stack, $"×{buff.stackCount}", skillBadgeStyle, 7);
            }
        }

        private void GetSkillCooldownState(
            string artId,
            out float cooldownRatio,
            out bool unavailable,
            out string cooldownText)
        {
            cooldownRatio = 0f;
            unavailable = false;
            cooldownText = string.Empty;
            if (battleManager == null || !battleManager.IsBattleActive)
            {
                return;
            }

            switch (artId)
            {
                case "剑气诀":
                    int rank = Mathf.Max(1, playerStats.GetMartialArtRank(artId));
                    int interval = rank == 1 ? 3 : 2;
                    int progress = battleManager.PlayerSuccessfulHits % interval;
                    int hitsRemaining = interval - progress;
                    cooldownRatio = hitsRemaining / (float)interval;
                    unavailable = hitsRemaining > 0;
                    cooldownText = $"{hitsRemaining}招";
                    break;
                case "毒砂掌":
                case "破甲掌":
                    cooldownRatio = battleManager.PlayerAttackCooldownRatio;
                    unavailable = cooldownRatio > 0.02f;
                    cooldownText = battleManager.PlayerAttackCooldownRemaining > 0.05f
                        ? $"{battleManager.PlayerAttackCooldownRemaining:0.0}秒"
                        : string.Empty;
                    break;
                case "反震诀":
                    cooldownRatio = battleManager.EnemyAttackCooldownRatio;
                    unavailable = cooldownRatio > 0.02f;
                    cooldownText = battleManager.EnemyAttackCooldownRemaining > 0.05f
                        ? $"{battleManager.EnemyAttackCooldownRemaining:0.0}秒"
                        : string.Empty;
                    break;
                case "金钟罩":
                    cooldownRatio = 1f;
                    unavailable = true;
                    cooldownText = "本场";
                    break;
            }
        }

        private void DrawLoadoutTooltip(Rect source, string title, string body)
        {
            float width = Mathf.Min(306f, ResponsiveGui.SafeArea.width - 24f);
            float height = string.IsNullOrEmpty(body) ? 34f : 57f;
            float x = Mathf.Clamp(source.center.x - width * 0.5f, ResponsiveGui.SafeArea.x + 8f,
                ResponsiveGui.SafeArea.xMax - width - 8f);
            float y = source.yMax + 8f;
            if (y + height > ResponsiveGui.SafeArea.yMax - 8f)
            {
                y = source.y - height - 8f;
            }

            Rect tooltip = new Rect(x, y, width, height);
            DrawPanel(tooltip, new Color(0.025f, 0.032f, 0.03f, 0.98f), Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(tooltip.x + 10f, tooltip.y + 3f, tooltip.width - 20f, 24f),
                title, headingStyle, 10);
            if (!string.IsNullOrEmpty(body))
            {
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(tooltip.x + 10f, tooltip.y + 27f, tooltip.width - 20f, 24f),
                    body, mutedStyle, 8);
            }
        }

        private void UpdateHealthFeedback()
        {
            if (playerStats == null || playerStats.runtimeStats == null)
            {
                trackedHudStats = null;
                return;
            }

            if (!ReferenceEquals(trackedHudStats, playerStats.runtimeStats))
            {
                ResetHealthFeedbackTracking();
                return;
            }

            float current = playerStats.runtimeStats.currentHealth;
            if (current < previousHudHealth - 0.01f)
            {
                healthBeforeDamageRatio = playerStats.runtimeStats.maxHealth <= 0f
                    ? 0f
                    : Mathf.Clamp01(previousHudHealth / playerStats.runtimeStats.maxHealth);
                healthDamageStartedAt = Time.unscaledTime;
            }

            previousHudHealth = current;
        }

        private void ResetHealthFeedbackTracking()
        {
            trackedHudStats = playerStats != null ? playerStats.runtimeStats : null;
            previousHudHealth = trackedHudStats != null ? trackedHudStats.currentHealth : 0f;
            healthBeforeDamageRatio = trackedHudStats != null ? trackedHudStats.HealthRatio : 0f;
            healthDamageStartedAt = -10f;
        }

        private static string CategoryBadge(string category)
        {
            switch (category)
            {
                case "外功": return "外";
                case "内功": return "内";
                case "身法": return "身";
                case "心法": return "心";
                default: return "武";
            }
        }

        private static string SkillFeatureLabel(string artId)
        {
            switch (artId)
            {
                case "剑气诀": return "剑气";
                case "毒砂掌": return "施毒";
                case "破甲掌": return "破甲";
                case "反震诀": return "反震";
                case "金钟罩": return "护盾";
                case "吸星诀": return "回气";
                case "百毒心经": return "毒发";
                default: return "就绪";
            }
        }

        private static Color SchoolColor(MartialArtSchool school)
        {
            switch (school)
            {
                case MartialArtSchool.SwiftSword:
                    return new Color(0.42f, 0.72f, 0.96f, 1f);
                case MartialArtSchool.VenomPalm:
                    return new Color(0.42f, 0.82f, 0.48f, 1f);
                default:
                    return new Color(0.92f, 0.52f, 0.25f, 1f);
            }
        }

        private void DrawBossApproachWarning()
        {
            BossApproachStage stage = gameFlow.CurrentBossApproachStage;
            if (gameFlow.mainTimeRemaining > 0f &&
                gameFlow.mainTimeRemaining <= 20f &&
                stage != BossApproachStage.FinalCountdown)
            {
                float urgentPulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 5f));
                DrawDangerEdges(0.10f + urgentPulse * 0.10f);
            }

            if (stage == BossApproachStage.None)
            {
                return;
            }

            Rect safe = ResponsiveGui.SafeArea;
            float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * (stage == BossApproachStage.FinalCountdown ? 7f : 3.5f)));
            DrawDangerEdges(0.18f + pulse * (stage == BossApproachStage.FinalCountdown ? 0.24f : 0.10f));

            if (stage == BossApproachStage.FinalCountdown)
            {
                int seconds = Mathf.Max(1, Mathf.CeilToInt(gameFlow.mainTimeRemaining));
                float panelSize = ResponsiveGui.IsPortrait ? 132f : 118f;
                float panelY = ResponsiveGui.IsPortrait ? safe.y + 166f : safe.y + 104f;
                Rect countdownPanel = new Rect(
                    safe.x + (safe.width - panelSize) * 0.5f,
                    panelY,
                    panelSize,
                    panelSize);
                FillRect(countdownPanel, new Color(0.08f, 0.015f, 0.012f, 0.78f));
                FillRect(new Rect(countdownPanel.x, countdownPanel.y, countdownPanel.width, 3f),
                    new Color(0.95f, 0.18f, 0.10f, 0.75f + pulse * 0.25f));
                GUI.Label(countdownPanel, seconds.ToString(), bossCountdownStyle);

                Rect warning = new Rect(
                    safe.x + (safe.width - Mathf.Min(360f, safe.width - 32f)) * 0.5f,
                    countdownPanel.yMax + 8f,
                    Mathf.Min(360f, safe.width - 32f),
                    34f);
                FillRect(warning, new Color(0.05f, 0.02f, 0.018f, 0.84f));
                ResponsiveGui.DrawSingleLineLabel(
                    warning,
                    "终局强敌即将降临",
                    bossWarningStyle,
                    12);
                return;
            }

            string warningText = stage == BossApproachStage.Imminent
                ? $"强敌将在 {Mathf.CeilToInt(gameFlow.mainTimeRemaining)} 息后降临"
                : "妖气逼近 · 尽快完成最后准备";
            float width = Mathf.Min(stage == BossApproachStage.Imminent ? 380f : 330f, safe.width - 32f);
            float y = ResponsiveGui.IsPortrait ? safe.y + 166f : safe.y + 18f;
            Rect banner = new Rect(safe.x + (safe.width - width) * 0.5f, y, width, 40f);
            FillRect(banner, new Color(0.04f, 0.025f, 0.02f, 0.86f));
            FillRect(new Rect(banner.x, banner.y, banner.width, 2f),
                new Color(0.95f, 0.34f, 0.13f, 0.60f + pulse * 0.30f));
            ResponsiveGui.DrawSingleLineLabel(banner, warningText, bossWarningStyle, 11);
        }

        private void DrawBossIntroOverlay()
        {
            if (battleScreen == null)
            {
                battleScreen = FindAnyObjectByType<BattleScreenController>();
            }

            Rect screen = new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height);
            Texture2D background = battleScreen != null ? battleScreen.bossBattleBackground : null;
            if (background != null)
            {
                GUI.DrawTexture(screen, background, ScaleMode.ScaleAndCrop, true);
            }
            else
            {
                FillRect(screen, new Color(0.055f, 0.015f, 0.018f));
            }

            float duration = Mathf.Max(0.01f, gameFlow.bossIntroDuration);
            float elapsed = Mathf.Clamp(duration - gameFlow.BossIntroTimeRemaining, 0f, duration);
            float progress = Mathf.Clamp01(elapsed / duration);
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.34f));
            float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(elapsed * 4.5f));
            FillRect(screen, new Color(0.01f, 0.008f, 0.01f, Mathf.Lerp(0.92f, 0.40f, reveal)));
            DrawDangerEdges(0.30f + pulse * 0.16f);

            Rect safe = ResponsiveGui.SafeArea;
            Rect identityCard = DrawBossIdentityCard(safe, progress, reveal);
            DrawBossIntroDialogue(safe, identityCard, progress, reveal);
        }

        private Rect DrawBossIdentityCard(Rect safe, float progress, float reveal)
        {
            bool portraitLayout = ResponsiveGui.IsPortrait;
            float cardWidth = Mathf.Min(portraitLayout ? 500f : 720f, safe.width - 30f);
            float cardHeight = portraitLayout ? 190f : 224f;
            float targetX = portraitLayout
                ? safe.x + (safe.width - cardWidth) * 0.5f
                : safe.x + Mathf.Min(32f, safe.width * 0.04f);
            float desiredY = safe.y + Mathf.Max(
                portraitLayout ? 150f : 74f,
                (safe.height - cardHeight) * (portraitLayout ? 0.34f : 0.42f));
            float targetY = Mathf.Clamp(
                desiredY,
                safe.y + 18f,
                Mathf.Max(safe.y + 18f, safe.yMax - cardHeight - 18f));
            float slideStartX = safe.x - cardWidth - 24f;
            float slide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.30f));
            Rect identityCard = new Rect(
                Mathf.Lerp(slideStartX, targetX, slide),
                targetY,
                cardWidth,
                cardHeight);
            DrawPanel(identityCard, new Color(0.025f, 0.012f, 0.014f, 0.94f),
                new Color(0.82f, 0.20f, 0.13f, 0.94f), WuxiaPanelKind.Boss);
            FillRect(
                new Rect(identityCard.x + 8f, identityCard.y + 7f, identityCard.width - 16f, 2f),
                new Color(0.94f, 0.62f, 0.20f, 0.82f));

            float portraitSize = portraitLayout
                ? Mathf.Min(180f, cardHeight - 14f)
                : Mathf.Min(232f, cardHeight + 8f);
            Rect portraitRect = new Rect(
                identityCard.x + 5f,
                identityCard.y + (identityCard.height - portraitSize) * 0.5f,
                portraitSize,
                portraitSize);
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, reveal);
            if (bossPortrait != null)
            {
                GUI.DrawTexture(portraitRect, bossPortrait, ScaleMode.ScaleToFit, true);
            }
            else
            {
                Sprite bossSprite = battleScreen != null
                    ? battleScreen.GetPreviewSprite(gameFlow.bossStats.visualId)
                    : null;
                if (bossSprite != null)
                {
                    DrawSpriteFrame(portraitRect, bossSprite);
                }
            }

            if (bossPortraitFrame != null)
            {
                GUI.DrawTexture(portraitRect, bossPortraitFrame, ScaleMode.ScaleToFit, true);
            }

            float textReveal = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01((progress - 0.12f) / 0.24f));
            GUI.color = new Color(1f, 1f, 1f, textReveal);
            float textX = portraitRect.xMax + (portraitLayout ? 6f : 16f);
            float textWidth = Mathf.Max(150f, identityCard.xMax - textX - 18f);
            float titleY = identityCard.y + (portraitLayout ? 18f : 24f);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(textX, titleY, textWidth, 32f),
                "终局强敌",
                bossIntroTitleStyle,
                13);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(textX, titleY + 32f, textWidth, portraitLayout ? 58f : 66f),
                gameFlow.bossStats.displayName,
                bossIntroNameStyle,
                portraitLayout ? 24 : 30);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(textX, titleY + (portraitLayout ? 92f : 112f), textWidth, 28f),
                progress < 0.60f ? "妖气压境 · 杀意已至" : "气血已复 · 决战将启",
                bossWarningStyle,
                11);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(textX, identityCard.yMax - 32f, textWidth, 20f),
                $"距离交锋  {gameFlow.BossIntroTimeRemaining:0.0} 秒",
                mutedStyle,
                9);
            GUI.color = previousColor;
            return identityCard;
        }

        private void DrawBossIntroDialogue(Rect safe, Rect identityCard, float progress, float reveal)
        {
            bool portraitLayout = ResponsiveGui.IsPortrait;
            float panelHeight = portraitLayout ? 130f : 118f;
            float panelY = Mathf.Min(
                identityCard.yMax + (portraitLayout ? 18f : 14f),
                safe.yMax - panelHeight - 18f);
            Rect dialoguePanel = new Rect(
                identityCard.x,
                Mathf.Max(safe.y + 18f, panelY),
                identityCard.width,
                panelHeight);

            int dialogueIndex = gameFlow.BossIntroDialogueIndex;
            float phaseStart = dialogueIndex switch
            {
                0 => 0f,
                1 => 0.34f,
                _ => 0.68f
            };
            float phaseEnd = dialogueIndex switch
            {
                0 => 0.34f,
                1 => 0.68f,
                _ => 1f
            };
            float localReveal = Mathf.InverseLerp(
                phaseStart,
                Mathf.Min(phaseStart + 0.08f, phaseEnd),
                progress);
            float textAlpha = reveal * Mathf.Lerp(
                0.35f,
                1f,
                Mathf.SmoothStep(0f, 1f, localReveal));

            Color accent = dialogueIndex switch
            {
                0 => Gold,
                1 => new Color(0.92f, 0.24f, 0.16f, 1f),
                _ => Jade
            };
            DrawPanel(
                dialoguePanel,
                new Color(0.025f, 0.018f, 0.016f, 0.94f),
                new Color(accent.r, accent.g, accent.b, 0.90f),
                WuxiaPanelKind.Boss);
            FillRect(
                new Rect(dialoguePanel.x + 8f, dialoguePanel.y + 10f, 4f, dialoguePanel.height - 20f),
                new Color(accent.r, accent.g, accent.b, 0.90f));

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, textAlpha);
            float textX = dialoguePanel.x + 24f;
            float textWidth = dialoguePanel.width - 44f;
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(textX, dialoguePanel.y + 12f, textWidth, 26f),
                gameFlow.CurrentBossIntroSpeaker,
                bossDialogueSpeakerStyle,
                12);
            GUI.Label(
                new Rect(textX, dialoguePanel.y + 42f, textWidth, dialoguePanel.height - 52f),
                gameFlow.CurrentBossIntroDialogue,
                bossDialogueBodyStyle);
            GUI.color = previousColor;
        }

        private static void DrawDangerEdges(float alpha)
        {
            float width = ResponsiveGui.Width;
            float height = ResponsiveGui.Height;
            float edge = ResponsiveGui.IsPortrait ? 18f : 14f;
            Color red = new Color(0.78f, 0.04f, 0.025f, Mathf.Clamp01(alpha));
            FillRect(new Rect(0f, 0f, width, edge), red);
            FillRect(new Rect(0f, height - edge, width, edge), red);
            FillRect(new Rect(0f, edge, edge, height - edge * 2f), red);
            FillRect(new Rect(width - edge, edge, edge, height - edge * 2f), red);
        }

        private static void DrawSpriteFrame(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect spriteRect = sprite.rect;
            Rect uv = new Rect(
                spriteRect.x / sprite.texture.width,
                spriteRect.y / sprite.texture.height,
                spriteRect.width / sprite.texture.width,
                spriteRect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        }

        private void DrawCharacterButtons()
        {
            Rect safe = ResponsiveGui.SafeArea;
            Rect statusRect = new Rect(safe.xMax - 58f, safe.y + 68f, 48f, 48f);
            Rect equipmentRect = new Rect(safe.xMax - 58f, safe.y + 122f, 48f, 48f);
            if (GUI.Button(statusRect, new GUIContent(statusIcon), iconButtonStyle))
            {
                ToggleCharacterPanel(CharacterView.Status);
            }
            if (GUI.Button(equipmentRect, new GUIContent(equipmentIcon), iconButtonStyle))
            {
                ToggleCharacterPanel(CharacterView.Equipment);
            }

            Vector2 mouse = ResponsiveGui.MousePosition(ResponsiveGui.Scale);
            string hoveredButton = statusRect.Contains(mouse)
                ? "角色状态"
                : equipmentRect.Contains(mouse)
                    ? "装备背包"
                    : string.Empty;
            if (!string.IsNullOrEmpty(hoveredButton))
            {
                float tooltipWidth = Mathf.Clamp(
                    ResponsiveGui.PreferredSingleLineWidth(hoveredButton, bodyStyle, 20f),
                    82f, ResponsiveGui.Width - 20f);
                const float tooltipHeight = 28f;
                float tooltipX = Mathf.Clamp(mouse.x - tooltipWidth - 12f, 10f,
                    ResponsiveGui.Width - tooltipWidth - 10f);
                float tooltipY = Mathf.Clamp(mouse.y + 10f, 10f,
                    ResponsiveGui.Height - tooltipHeight - 10f);
                Rect tooltip = new Rect(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
                FillRect(tooltip, Ink);
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(tooltip.x + 8f, tooltip.y + 2f, tooltip.width - 16f, tooltip.height - 4f),
                    hoveredButton, bodyStyle, 10);
            }
        }

        private void ToggleCharacterPanel(CharacterView view)
        {
            bool sameOpenView = characterPanelOpen && currentView == view;
            currentView = view;
            SetCharacterScreenOpen(!sameOpenView);
        }

        private void SetCharacterScreenOpen(bool open)
        {
            if (gameFlow == null)
            {
                characterPanelOpen = false;
                return;
            }

            if (open && gameFlow.CurrentPhase != GamePhase.MainMapRunning)
            {
                return;
            }

            characterPanelOpen = open;
            gameFlow.SetCharacterMenuPaused(open);
        }

        private void DrawCharacterScreen()
        {
            FillRect(new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height), new Color(0.025f, 0.03f, 0.03f, 0.98f));
            Rect panel = CenteredRect(760f, 460f);
            DrawPanel(panel, Panel, currentView == CharacterView.Status ? Jade : Gold);

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 18f, panel.y + 9f, panel.width - 210f, 34f),
                "侠客档案", titleStyle, 16);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.xMax - 174f, panel.y + 11f, 116f, 28f),
                "江湖暂停", centeredStyle, 10);
            if (GUI.Button(new Rect(panel.xMax - 44f, panel.y + 10f, 28f, 28f), "×", actionButtonStyle))
            {
                SetCharacterScreenOpen(false);
                return;
            }

            float tabY = panel.y + 50f;
            float tabWidth = Mathf.Min(150f, (panel.width - 36f) * 0.5f);
            if (GUI.Button(new Rect(panel.x + 18f, tabY, tabWidth, 32f), "角色状态", currentView == CharacterView.Status ? activeTabStyle : tabStyle))
            {
                currentView = CharacterView.Status;
            }
            if (GUI.Button(new Rect(panel.x + 22f + tabWidth, tabY, tabWidth, 32f), "装备背包", currentView == CharacterView.Equipment ? activeTabStyle : tabStyle))
            {
                currentView = CharacterView.Equipment;
            }

            Rect content = new Rect(panel.x + 18f, tabY + 44f, panel.width - 36f, panel.height - 108f);
            if (currentView == CharacterView.Status)
            {
                DrawStatus(content);
            }
            else
            {
                DrawEquipment(content);
            }
        }


        private void DrawStatus(Rect rect)
        {
            int artRows = Mathf.Max(1, Mathf.CeilToInt(playerStats.learnedMartialArts.Count / 3f));
            int secretRows = Mathf.Max(0, Mathf.CeilToInt(playerStats.unlockedSecrets.Count / 3f));
            float contentHeight = 286f + artRows * 54f + (secretRows > 0 ? 34f + secretRows * 54f : 0f);
            bool needsScroll = rect.height < contentHeight;
            if (needsScroll)
            {
                statusScroll = GUI.BeginScrollView(rect, statusScroll, new Rect(0f, 0f, rect.width - 18f, contentHeight));
                rect = new Rect(0f, 0f, rect.width - 22f, contentHeight);
            }
            if (statusIcon != null)
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y, 58f, 58f), statusIcon, ScaleMode.ScaleToFit, true);
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 68f, rect.y, rect.width - 68f, 24f),
                playerStats.runtimeStats.displayName, headingStyle, 12);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 68f, rect.y + 25f, rect.width - 68f, 18f),
                $"等级 {playerStats.level}  ·  击杀 {playerStats.killCount}  ·  洞穴 {playerStats.caveEntries}",
                mutedStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 68f, rect.y + 44f, rect.width - 68f, 18f),
                $"修为 {playerStats.cultivation}/{playerStats.NextLevelRequirement}  ·  铜钱 {playerStats.copper}",
                bodyStyle, 9);

            float y = rect.y + 72f;
            DrawStatRow(new Rect(rect.x, y, rect.width, 27f), "气血", $"{playerStats.runtimeStats.currentHealth:0}/{playerStats.runtimeStats.maxHealth:0}", "攻击", playerStats.runtimeStats.attack.ToString("0"));
            DrawStatRow(new Rect(rect.x, y + 31f, rect.width, 27f), "防御", playerStats.runtimeStats.defense.ToString("0"), "攻速", playerStats.runtimeStats.attackSpeed.ToString("0.00"));
            DrawStatRow(new Rect(rect.x, y + 62f, rect.width, 27f), "暴击", $"{playerStats.runtimeStats.critChance * 100f:0.#}%", "闪避", $"{playerStats.runtimeStats.dodgeChance * 100f:0.#}%");
            DrawStatRow(new Rect(rect.x, y + 93f, rect.width, 27f), "吸血", $"{playerStats.runtimeStats.lifeSteal * 100f:0.#}%", "移速", playerStats.CurrentMoveSpeed.ToString("0.0"));

            GUI.Label(new Rect(rect.x, y + 130f, rect.width, 22f), "已习武学", headingStyle);
            if (playerStats.learnedMartialArts.Count == 0)
            {
                GUI.Label(new Rect(rect.x, y + 154f, rect.width, 38f), "尚未习得", bodyStyle);
            }
            else
            {
                string[] learned = playerStats.learnedMartialArts.Take(20).ToArray();
                const float gap = 8f;
                float tileWidth = (rect.width - gap * 2f) / 3f;
                for (int i = 0; i < learned.Length; i++)
                {
                    int column = i % 3;
                    int row = i / 3;
                    Rect tile = new Rect(rect.x + column * (tileWidth + gap),
                        y + 154f + row * 54f, tileWidth, 48f);
                    DrawMartialArtTile(tile, learned[i]);
                }
            }

            if (playerStats.unlockedSecrets.Count > 0)
            {
                float secretsY = y + 160f + artRows * 54f;
                GUI.Label(new Rect(rect.x, secretsY, rect.width, 22f), "已悟秘传", headingStyle);
                const float gap = 8f;
                float tileWidth = (rect.width - gap * 2f) / 3f;
                for (int i = 0; i < playerStats.unlockedSecrets.Count; i++)
                {
                    string secretId = playerStats.unlockedSecrets[i];
                    int column = i % 3;
                    int row = i / 3;
                    Rect tile = new Rect(rect.x + column * (tileWidth + gap),
                        secretsY + 26f + row * 54f, tileWidth, 48f);
                    FillRect(tile, new Color(0.12f, 0.095f, 0.15f, 1f));
                    DrawIcon(new Rect(tile.x + 5f, tile.y + 5f, 38f, 38f),
                        FindMartialArtIcon(secretId), new Color(0.72f, 0.46f, 0.86f));
                    ResponsiveGui.DrawSingleLineLabel(
                        new Rect(tile.x + 49f, tile.y + 3f, tile.width - 54f, 22f),
                        secretId, headingStyle, 8);
                    ResponsiveGui.DrawSingleLineLabel(
                        new Rect(tile.x + 49f, tile.y + 24f, tile.width - 54f, 20f),
                        $"秘传 {playerStats.GetSecretRank(secretId)} 重", mutedStyle, 8);
                }
            }
            if (needsScroll)
            {
                GUI.EndScrollView();
            }
        }

        private void DrawStatRow(Rect rect, string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            FillRect(rect, PanelLight);
            float half = rect.width * 0.5f;
            ResponsiveGui.DrawSingleLineLabel(new Rect(rect.x + 10f, rect.y, half - 20f, rect.height),
                $"{leftLabel}  {leftValue}", bodyStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + half + 10f, rect.y, half - 20f, rect.height),
                $"{rightLabel}  {rightValue}", bodyStyle, 9);
        }

        private void DrawEquipment(Rect rect)
        {
            PlayerEquipment equipment = playerStats.equipment;
            if (equipment == null)
            {
                GUI.Label(rect, "装备系统未连接", centeredStyle);
                return;
            }

            GUI.Label(new Rect(rect.x, rect.y, rect.width, 24f), "当前穿戴", headingStyle);
            const float slotGap = 8f;
            float slotWidth = (rect.width - slotGap * 2f) / 3f;
            DrawEquippedSlot(new Rect(rect.x, rect.y + 27f, slotWidth, 58f), equipment, EquipmentSlot.Weapon);
            DrawEquippedSlot(new Rect(rect.x + slotWidth + slotGap, rect.y + 27f, slotWidth, 58f), equipment, EquipmentSlot.Armor);
            DrawEquippedSlot(new Rect(rect.x + (slotWidth + slotGap) * 2f, rect.y + 27f, slotWidth, 58f), equipment, EquipmentSlot.Accessory);

            float inventoryY = rect.y + 94f;
            GUI.Label(new Rect(rect.x, inventoryY, rect.width, 24f), $"背包  {equipment.inventory.Count}", headingStyle);
            Rect viewport = new Rect(rect.x, inventoryY + 28f, rect.width, Mathf.Max(68f, rect.yMax - inventoryY - 28f));
            float contentHeight = equipment.inventory.Count * 66f;
            inventoryScroll = GUI.BeginScrollView(viewport, inventoryScroll, new Rect(0f, 0f, viewport.width - 18f, contentHeight));
            for (int i = 0; i < equipment.inventory.Count; i++)
            {
                EquipmentItem item = equipment.inventory[i];
                DrawInventoryItem(new Rect(0f, i * 66f, viewport.width - 22f, 60f), equipment, item);
            }
            GUI.EndScrollView();
        }

        private void DrawEquippedSlot(Rect rect, PlayerEquipment equipment, EquipmentSlot slot)
        {
            FillRect(rect, PanelLight);
            EquipmentItem item = equipment.GetEquipped(slot);
            GUI.Label(new Rect(rect.x + 7f, rect.y + 1f, rect.width - 14f, 18f), SlotName(slot), mutedStyle);
            if (item != null)
            {
                DrawIcon(new Rect(rect.x + 7f, rect.y + 20f, 32f, 32f), FindEquipmentIcon(item.id),
                    RarityColor(item.rarity));
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 45f, rect.y + 19f, rect.width - 103f, 34f),
                item == null ? "未装备" : item.displayName, bodyStyle, 9);
            if (item != null && GUI.Button(new Rect(rect.xMax - 54f, rect.y + 23f, 47f, 25f), "卸下", actionButtonStyle))
            {
                equipment.Unequip(slot);
            }
        }

        private void DrawInventoryItem(Rect rect, PlayerEquipment equipment, EquipmentItem item)
        {
            FillRect(rect, new Color(0.12f, 0.14f, 0.13f, 1f));
            DrawIcon(new Rect(rect.x + 7f, rect.y + 6f, 48f, 48f), FindEquipmentIcon(item.id),
                RarityColor(item.rarity));
            Color previous = GUI.contentColor;
            GUI.contentColor = RarityColor(item.rarity);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 63f, rect.y + 5f, rect.width - 146f, 23f),
                item.displayName, headingStyle, 10);
            GUI.contentColor = previous;
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 63f, rect.y + 30f, rect.width - 146f, 22f),
                item.BonusSummary, mutedStyle, 9);

            bool equipped = equipment.IsEquipped(item);
            GUI.enabled = !equipped;
            if (GUI.Button(new Rect(rect.xMax - 72f, rect.y + 15f, 62f, 30f), equipped ? "已装备" : "装备", actionButtonStyle))
            {
                equipment.Equip(item);
            }
            GUI.enabled = true;
        }

        private void DrawLevelUpPanel()
        {
            FillRect(new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height),
                new Color(0.02f, 0.025f, 0.025f, 0.72f));
            Rect panel = CenteredRect(660f, 340f);
            DrawPanel(panel, new Color(0.09f, 0.105f, 0.105f, 1f), Gold,
                WuxiaPanelKind.Paper);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 32f), "修为突破", titleStyle);

            float detailsWidth = Mathf.Clamp(panel.width * 0.39f, 210f, 244f);
            Rect choicesArea = new Rect(panel.x + 18f, panel.y + 54f,
                panel.width - detailsWidth - 50f, panel.height - 112f);
            Rect detailsArea = new Rect(choicesArea.xMax + 14f, choicesArea.y,
                detailsWidth, choicesArea.height);
            string hoveredArt = null;

            for (int i = 0; i < gameFlow.currentChoices.Count; i++)
            {
                string artId = gameFlow.currentChoices[i];
                Rect card = new Rect(choicesArea.x, choicesArea.y + i * 72f,
                    choicesArea.width, 62f);
                if (card.Contains(Event.current.mousePosition))
                {
                    hoveredArt = artId;
                }

                bool selected = GUI.Button(card, GUIContent.none, actionButtonStyle);
                DrawIcon(new Rect(card.x + 7f, card.y + 7f, 48f, 48f),
                    FindMartialArtIcon(artId), CategoryColor(MartialArtCatalog.Get(artId)?.category));
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(card.x + 65f, card.y + 5f, card.width - 74f, 26f),
                    GetOfferName(artId), headingStyle, 10);
                MartialArtDefinition definition = MartialArtCatalog.Get(artId);
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(card.x + 65f, card.y + 31f, card.width - 74f, 22f),
                    definition?.GetEffectSummary(playerStats.GetMartialArtRank(artId) + 1) ?? "查看效果",
                    mutedStyle, 8);

                if (selected)
                {
                    gameFlow.ChooseMartialArt(i);
                    return;
                }
            }

            if (hoveredArt == null && gameFlow.currentChoices.Count > 0)
            {
                hoveredArt = gameFlow.currentChoices[0];
            }
            DrawMartialArtTooltip(detailsArea, hoveredArt);

            GUI.enabled = gameFlow.martialArtRerollsRemaining > 0;
            if (GUI.Button(new Rect(choicesArea.x, panel.yMax - 44f, choicesArea.width, 30f),
                    $"重观残页（剩余 {gameFlow.martialArtRerollsRemaining}）", actionButtonStyle))
            {
                gameFlow.RerollMartialArtChoices();
            }
            GUI.enabled = true;
        }

        private void DrawMartialArtTooltip(Rect rect, string artId)
        {
            DrawPanel(rect, new Color(0.055f, 0.065f, 0.06f, 0.98f), Gold,
                WuxiaPanelKind.Paper);
            MartialArtDefinition definition = MartialArtCatalog.Get(artId);
            if (definition == null)
            {
                GUI.Label(new Rect(rect.x + 14f, rect.y + 22f, rect.width - 28f, 28f),
                    "悬停武学查看效果", headingStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 58f, rect.width - 28f, 78f),
                    "选择后会立即获得本局属性加成；突破期间所有时间暂停。", mutedStyle);
                return;
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 14f, rect.y + 10f, rect.width - 28f, 28f),
                GetOfferName(definition.id), headingStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 14f, rect.y + 40f, rect.width - 28f, 20f),
                $"{MartialArtCatalog.SchoolName(definition.school)} · {definition.category}", mutedStyle, 9);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 68f, rect.width - 28f, 44f),
                definition.GetEffectSummary(playerStats.GetMartialArtRank(artId) + 1), tooltipEffectStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 116f, rect.width - 28f, rect.height - 126f),
                definition.description, bodyStyle);
        }

        private void DrawMartialArtTile(Rect rect, string artId)
        {
            FillRect(rect, PanelLight);
            MartialArtDefinition definition = MartialArtCatalog.Get(artId);
            DrawIcon(new Rect(rect.x + 5f, rect.y + 5f, 38f, 38f),
                FindMartialArtIcon(artId), CategoryColor(definition?.category));
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 49f, rect.y + 2f, rect.width - 54f, 22f),
                $"{artId} · {RankName(playerStats.GetMartialArtRank(artId))}", bodyStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 49f, rect.y + 23f, rect.width - 54f, 20f),
                definition?.GetEffectSummary(playerStats.GetMartialArtRank(artId)) ?? string.Empty,
                mutedStyle, 8);
        }

        private string GetOfferName(string artId)
        {
            return $"{artId} → {RankName(playerStats.GetMartialArtRank(artId) + 1)}";
        }

        private static string RankName(int rank)
        {
            switch (rank)
            {
                case 1:
                    return "一重";
                case 2:
                    return "二重";
                case 3:
                    return "三重";
                default:
                    return $"{rank} 重";
            }
        }

        private static void DrawIcon(Rect rect, Texture2D icon, Color accent)
        {
            FillRect(rect, new Color(0.035f, 0.04f, 0.04f, 0.95f));
            FillRect(new Rect(rect.x, rect.y, 2f, rect.height), accent);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f),
                    icon, ScaleMode.ScaleToFit, true);
            }
        }

        private Texture2D FindMartialArtIcon(string id)
        {
            string resourceId = ContentIconCatalog.MartialArt(id);
            Texture2D resourceIcon = string.IsNullOrEmpty(resourceId)
                ? null
                : Resources.Load<Texture2D>("Icons/" + resourceId);
            if (resourceIcon != null)
            {
                return resourceIcon;
            }

            Texture2D icon = FindIcon(martialArtIcons, id);
            if (icon != null)
            {
                return icon;
            }

            if (id == "百毒心经")
            {
                return FindIcon(martialArtIcons, "毒砂掌");
            }

            if (id == "金钟罩" || id == "反震诀")
            {
                return FindIcon(martialArtIcons, "铁布衫");
            }

            return null;
        }

        private Texture2D FindEquipmentIcon(string id)
        {
            string resourceId = ContentIconCatalog.Equipment(id);
            Texture2D resourceIcon = string.IsNullOrEmpty(resourceId)
                ? null
                : Resources.Load<Texture2D>("Icons/" + resourceId);
            if (resourceIcon != null)
            {
                return resourceIcon;
            }

            Texture2D icon = FindIcon(equipmentItemIcons, id);
            return icon != null || id != "poison_dart_pouch"
                ? icon
                : FindIcon(equipmentItemIcons, "black_iron_ring");
        }

        private static Texture2D FindIcon(IconEntry[] entries, string id)
        {
            if (entries == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (IconEntry entry in entries)
            {
                if (entry != null && entry.id == id)
                {
                    return entry.icon;
                }
            }

            return null;
        }

        private static Color CategoryColor(string category)
        {
            switch (category)
            {
                case "内功":
                    return new Color(0.38f, 0.72f, 0.58f);
                case "身法":
                    return new Color(0.36f, 0.64f, 0.86f);
                default:
                    return new Color(0.82f, 0.34f, 0.24f);
            }
        }

        private void DrawResultPanel()
        {
            FillRect(new Rect(0f, 0f, ResponsiveGui.Width, ResponsiveGui.Height), new Color(0.02f, 0.025f, 0.025f, 0.78f));
            Rect safe = ResponsiveGui.SafeArea;
            float width = Mathf.Min(460f, safe.width - 32f);
            float height = Mathf.Min(280f, safe.height - 32f);
            Rect panel = new Rect(
                safe.center.x - width * 0.5f,
                safe.center.y - height * 0.5f,
                width,
                height);
            bool cleared = gameFlow.IsTutorialCompletionSummary || gameFlow.bossDefeated;
            DrawPanel(panel, Panel,
                cleared ? Jade : new Color(0.72f, 0.25f, 0.20f),
                WuxiaPanelKind.Boss);
            GUI.Label(
                new Rect(panel.x + 24f, panel.y + 16f, panel.width - 48f, 36f),
                gameFlow.IsTutorialCompletionSummary ? "教学完成" : gameFlow.bossDefeated ? "闯关功成" : "江湖路断",
                titleStyle);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 24f, panel.y + 58f, panel.width - 48f, 24f),
                gameFlow.CurrentLevelDisplayName, headingStyle, 11);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 24f, panel.y + 90f, panel.width - 48f, 24f),
                gameFlow.statusMessage, bodyStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 24f, panel.y + 122f, panel.width - 48f, 22f),
                $"等级 {playerStats.level}  ·  击杀 {playerStats.killCount}  ·  洞穴 {playerStats.caveEntries}",
                mutedStyle, 9);
            string buildSummary = playerStats.learnedMartialArts.Count > 0
                ? string.Join(" · ", playerStats.learnedMartialArts.Take(3))
                : "尚未习得武学";
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 24f, panel.y + 152f, panel.width - 48f, 22f),
                $"本关武学：{buildSummary}", mutedStyle, 9);

            float buttonGap = 12f;
            float buttonWidth = (panel.width - 48f - buttonGap) * 0.5f;
            Rect homeButton = new Rect(panel.x + 24f, panel.yMax - 64f, buttonWidth, 44f);
            Rect nextButton = new Rect(homeButton.xMax + buttonGap, homeButton.y, buttonWidth, 44f);
            if (GUI.Button(homeButton, "返回主页", actionButtonStyle))
            {
                gameFlow.ReturnToMainMenu();
            }

            GUI.enabled = gameFlow.CanContinueToNextLevel;
            if (GUI.Button(
                    nextButton,
                    gameFlow.CanContinueToNextLevel ? "下一关" : "下一关尚未开放",
                    mainMenuButtonStyle))
            {
                gameFlow.ContinueToNextLevel();
            }
            GUI.enabled = true;
        }

        private void DrawDebugControls()
        {
            Rect safe = ResponsiveGui.SafeArea;
            Rect panel = new Rect(safe.x + 14f, safe.y + 158f, 180f, 238f);
            DrawPanel(panel, Ink, new Color(0.55f, 0.55f, 0.55f));
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, 26f), "重新开始", actionButtonStyle)) gameFlow.StartRun();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 40f, panel.width - 16f, 26f), "增加修为", actionButtonStyle)) gameFlow.AddDebugCultivation();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 72f, panel.width - 16f, 26f), "增加战力", actionButtonStyle)) gameFlow.AddDebugPower();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 104f, panel.width - 16f, 26f), "进入决战", actionButtonStyle)) gameFlow.ForceEnterBoss();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 136f, panel.width - 16f, 26f), "敌人洞穴", actionButtonStyle)) gameFlow.DebugEnterCave(CaveContentType.Enemy);
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 168f, panel.width - 16f, 26f), "商人洞穴", actionButtonStyle)) gameFlow.DebugEnterCave(CaveContentType.Merchant);
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 200f, panel.width - 16f, 26f), "宝箱洞穴", actionButtonStyle)) gameFlow.DebugEnterCave(CaveContentType.Treasure);
        }

        private float GetMainTimeRatio()
        {
            return Mathf.Clamp01(gameFlow.mainTimeRemaining / Mathf.Max(0.01f, gameFlow.mainTimeLimit));
        }

        private static Color GetMainTimeColor(float ratio)
        {
            if (ratio <= 1f / 3f)
            {
                return new Color(0.94f, 0.18f, 0.11f);
            }

            if (ratio <= 2f / 3f)
            {
                return Gold;
            }

            return Jade;
        }

        private static void DrawMainTimeTrack(Rect rect, float ratio, bool paused)
        {
            TimePressureBarRenderer.Draw(rect, ratio, paused);
        }

        private void DrawHealthBar(Rect rect, float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            float lowHealthPulse = ratio <= 0.25f
                ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7f))
                : 0f;
            Color borderColor = ratio <= 0.25f
                ? new Color(0.88f, 0.15f + lowHealthPulse * 0.18f, 0.10f, 1f)
                : new Color(0.68f, 0.49f, 0.24f, 1f);
            FillRect(rect, new Color(0.025f, 0.02f, 0.018f, 0.98f));
            DrawRectOutline(rect, borderColor, ratio <= 0.25f ? 2f : 1f);

            float inset = Mathf.Clamp(rect.height * 0.16f, 2f, 4f);
            Rect track = new Rect(rect.x + inset, rect.y + inset,
                Mathf.Max(0f, rect.width - inset * 2f), Mathf.Max(1f, rect.height - inset * 2f));
            FillRect(track, new Color(0.13f, 0.045f, 0.035f, 1f));

            float damageAge = Time.unscaledTime - healthDamageStartedAt;
            if (damageAge >= 0f && damageAge < HealthLossTrailDuration && healthBeforeDamageRatio > ratio)
            {
                float trailAlpha = 1f - damageAge / HealthLossTrailDuration;
                Rect loss = new Rect(track.x + track.width * ratio, track.y,
                    track.width * (healthBeforeDamageRatio - ratio), track.height);
                FillRect(loss, new Color(1f, 0.67f, 0.20f, 0.88f * trailAlpha));
            }

            Color healthColor = ratio <= 0.25f
                ? new Color(0.88f, 0.12f, 0.09f)
                : ratio <= 0.5f
                    ? new Color(0.88f, 0.34f, 0.12f)
                    : Crimson;
            Rect current = new Rect(track.x, track.y, track.width * ratio, track.height);
            FillRect(current, healthColor);
            if (current.width > 2f)
            {
                FillRect(new Rect(current.x, current.y, current.width, Mathf.Max(1f, current.height * 0.25f)),
                    new Color(1f, 0.62f, 0.42f, 0.40f));
                FillRect(new Rect(current.xMax - 2f, current.y, 2f, current.height),
                    new Color(1f, 0.83f, 0.56f, 0.72f));
            }

            for (int i = 1; i < 4; i++)
            {
                float markerX = track.x + track.width * i * 0.25f;
                FillRect(new Rect(markerX, track.y, 1f, track.height),
                    new Color(0.02f, 0.02f, 0.018f, 0.38f));
            }
        }

        private static void DrawRectOutline(Rect rect, Color color, float thickness)
        {
            WuxiaUiTheme.DrawOutline(rect, color, thickness);
        }

        private static void DrawPanel(Rect rect, Color background, Color accent)
        {
            WuxiaUiTheme.DrawPanel(rect, background, accent);
        }

        private static void DrawPanel(
            Rect rect,
            Color background,
            Color accent,
            WuxiaPanelKind kind)
        {
            WuxiaUiTheme.DrawPanel(rect, background, accent, kind);
        }

        private static GUIStyle LabelStyle(int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            return RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = true,
                normal = { textColor = color }
            });
        }

        private static Rect CenteredRect(float width, float height)
        {
            Rect safe = ResponsiveGui.SafeArea;
            width = Mathf.Min(width, safe.width - 28f);
            height = Mathf.Min(height, safe.height - 28f);
            return new Rect(
                safe.x + (safe.width - width) * 0.5f,
                safe.y + (safe.height - height) * 0.5f,
                width,
                height);
        }

        private static string SlotName(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return "兵器";
                case EquipmentSlot.Armor:
                    return "护甲";
                default:
                    return "饰物";
            }
        }

        private static Color RarityColor(EquipmentRarity rarity)
        {
            switch (rarity)
            {
                case EquipmentRarity.Rare:
                    return new Color(0.72f, 0.55f, 0.92f);
                case EquipmentRarity.Fine:
                    return new Color(0.35f, 0.76f, 0.63f);
                default:
                    return Color.white;
            }
        }

        private static void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Texture2D CreateSettingsIcon(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PLACEHOLDER_UI_RuntimeSettingsIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            Color gearColor = new Color(0.92f, 0.79f, 0.48f, 1f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    float normalizedRadius = offset.magnitude / radius;
                    float angle = Mathf.Atan2(offset.y, offset.x);
                    float teeth = Mathf.Cos(angle * 8f) > 0.25f ? 0.92f : 0.78f;
                    bool gearBody = normalizedRadius <= teeth && normalizedRadius >= 0.28f;
                    bool centerRing = normalizedRadius <= 0.46f && normalizedRadius >= 0.28f;
                    pixels[y * size + x] = gearBody || centerRing ? gearColor : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateHomeIcon(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PLACEHOLDER_UI_RuntimeHomeIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.5f;
            Color homeColor = new Color(0.92f, 0.79f, 0.48f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = (new Vector2(x, y) - center) / radius;
                    bool roof = point.y >= 0f && point.y <= 0.72f &&
                                Mathf.Abs(point.x) <= (0.72f - point.y) * 0.92f + 0.08f;
                    bool body = Mathf.Abs(point.x) <= 0.50f && point.y >= -0.62f && point.y <= 0.10f;
                    bool door = Mathf.Abs(point.x) <= 0.14f && point.y >= -0.62f && point.y <= -0.20f;
                    pixels[y * size + x] = (roof || body) && !door ? homeColor : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateSkipTutorialIcon(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PLACEHOLDER_UI_RuntimeSkipTutorialIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.5f;
            Color skipColor = new Color(0.92f, 0.79f, 0.48f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = (new Vector2(x, y) - center) / radius;
                    bool firstChevron = point.x >= -0.66f && point.x <= 0.05f &&
                                          Mathf.Abs(point.y) <= 0.50f - Mathf.Abs(point.x + 0.30f) * 0.36f &&
                                          Mathf.Abs(point.y) >= Mathf.Max(0f, Mathf.Abs(point.x + 0.30f) * 0.62f - 0.08f);
                    bool secondChevron = point.x >= -0.05f && point.x <= 0.66f &&
                                           Mathf.Abs(point.y) <= 0.50f - Mathf.Abs(point.x - 0.30f) * 0.36f &&
                                           Mathf.Abs(point.y) >= Mathf.Max(0f, Mathf.Abs(point.x - 0.30f) * 0.62f - 0.08f);
                    bool endBar = point.x >= 0.62f && point.x <= 0.75f && Mathf.Abs(point.y) <= 0.52f;
                    pixels[y * size + x] = firstChevron || secondChevron || endBar
                        ? skipColor
                        : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateCircleTexture(int size, Color fill)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PLACEHOLDER_UI_RuntimePortraitBackdrop",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.49f;
            float feather = Mathf.Max(1f, size * 0.035f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 1f - Mathf.Clamp01((distance - radius + feather) / feather);
                    pixels[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
