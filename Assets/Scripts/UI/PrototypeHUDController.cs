using System;
using System.Linq;
using UnityEngine;
using WuxiaRoguelite.Audio;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;

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
        public static bool IsPortrait => Screen.height > Screen.width;
        public static Rect SafeArea
        {
            get
            {
                float scale = Scale;
                Rect safe = Screen.safeArea;
                return new Rect(
                    safe.x / scale,
                    (Screen.height - safe.yMax) / scale,
                    safe.width / scale,
                    safe.height / scale);
            }
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
            while (style.fontSize > minimumFontSize)
            {
                Vector2 measured = style.CalcSize(content);
                if (measured.x <= rect.width && measured.y <= rect.height)
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
        private GUIStyle settingsToggleStyle;
        private GUIStyle warningHeadingStyle;
        private GUIStyle dangerHeadingStyle;
        private GUIStyle bossWarningStyle;
        private GUIStyle bossCountdownStyle;
        private GUIStyle bossIntroTitleStyle;
        private GUIStyle bossIntroNameStyle;
        private Texture2D runtimeSettingsIcon;
        private BattleScreenController battleScreen;
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

        private static readonly Color Ink = new Color(0.055f, 0.065f, 0.07f, 0.94f);
        private static readonly Color Panel = new Color(0.09f, 0.105f, 0.105f, 0.97f);
        private static readonly Color PanelLight = new Color(0.14f, 0.16f, 0.15f, 0.98f);
        private static readonly Color Jade = new Color(0.27f, 0.68f, 0.53f, 1f);
        private static readonly Color Gold = new Color(0.86f, 0.68f, 0.32f, 1f);
        private static readonly Color Paper = new Color(0.92f, 0.88f, 0.74f, 1f);
        private static readonly Color Muted = new Color(0.66f, 0.70f, 0.67f, 1f);

        private void Awake()
        {
            if (musicController == null)
            {
                musicController = FindAnyObjectByType<MainMapMusicController>();
            }

            battleScreen = FindAnyObjectByType<BattleScreenController>();
        }

        private void Update()
        {
            if (gameFlow != null && gameFlow.IsBossIntroActive)
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
                if (settingsOpen)
                {
                    DrawSettingsPanel();
                    return;
                }

                if (battleManager != null && battleManager.IsBattleActive)
                {
                    DrawSettingsButton();
                    return;
                }

                if (gameFlow.CurrentPhase == GamePhase.Ready)
                {
                    DrawMainMenu();
                    DrawSettingsButton();
                    return;
                }

                if (gameFlow.IsBossIntroActive)
                {
                    DrawBossIntroOverlay();
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

                if (gameFlow.CurrentPhase == GamePhase.LevelUpPaused)
                {
                    DrawLevelUpPanel();
                }
                else if (gameFlow.CurrentPhase == GamePhase.Result)
                {
                    DrawResultPanel();
                }

                if (debugVisible && !characterPanelOpen)
                {
                    DrawDebugControls();
                }

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
            headingStyle = LabelStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            bodyStyle = LabelStyle(14, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            mutedStyle = LabelStyle(12, FontStyle.Normal, TextAnchor.MiddleLeft, Muted);
            centeredStyle = LabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            iconButtonStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(7, 7, 7, 7),
                fixedWidth = 48f,
                fixedHeight = 48f
            });
            tabStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Muted }
            });
            activeTabStyle = new GUIStyle(tabStyle);
            activeTabStyle.normal.textColor = Paper;
            actionButtonStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
            tooltipEffectStyle = LabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft,
                new Color(1f, 0.80f, 0.35f));
            mainMenuTitleStyle = LabelStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.97f, 0.91f, 0.72f));
            mainMenuSubtitleStyle = LabelStyle(15, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Color(0.84f, 0.84f, 0.78f));
            mainMenuButtonStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.91f, 0.70f) },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            });
            settingsToggleStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                hover = { textColor = Paper },
                active = { textColor = Color.white }
            });
            warningHeadingStyle = LabelStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.76f, 0.32f));
            dangerHeadingStyle = LabelStyle(16, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.30f, 0.20f));
            bossWarningStyle = LabelStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.82f, 0.46f));
            bossCountdownStyle = LabelStyle(64, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.22f, 0.14f));
            bossIntroTitleStyle = LabelStyle(21, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.78f, 0.38f));
            bossIntroNameStyle = LabelStyle(42, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.94f, 0.78f));
            runtimeSettingsIcon = CreateSettingsIcon(64);
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
                "包含主地图、战斗、山洞与 Boss 音乐", mutedStyle, 9);

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
                "移动：左下角虚拟摇杆 / WASD", bodyStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 22f, panel.y + 312f, panel.width - 44f, 22f),
                "P 角色状态 · B 装备背包 · Esc 设置", mutedStyle, 9);

            if (GUI.Button(
                    new Rect(panel.x + 22f, panel.yMax - 52f, panel.width - 44f, 34f),
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
            if (GUI.Button(startButton, "踏入江湖", mainMenuButtonStyle))
            {
                gameFlow.StartRun();
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 24f, panel.yMax - 34f, panel.width - 48f, 20f),
                "移动探索 · 碰怪自动战斗 · 寻找洞穴与宝箱", mainMenuSubtitleStyle, 10);
        }

        private void DrawCompactHud()
        {
            Rect safe = ResponsiveGui.SafeArea;
            float hudWidth = ResponsiveGui.IsPortrait ? Mathf.Min(330f, safe.width - 86f) : 258f;
            Rect hud = new Rect(safe.x + 14f, safe.y + 14f, hudWidth, 104f);
            BossApproachStage approachStage = gameFlow.CurrentBossApproachStage;
            float timeRatio = GetMainTimeRatio();
            Color accent = GetMainTimeColor(timeRatio);
            GUIStyle timerLabelStyle = approachStage == BossApproachStage.FinalCountdown ||
                                       approachStage == BossApproachStage.Arrived
                ? dangerHeadingStyle
                : approachStage == BossApproachStage.Imminent ||
                  approachStage == BossApproachStage.Omen
                    ? warningHeadingStyle
                    : headingStyle;
            DrawPanel(hud, Ink, accent);
            ResponsiveGui.DrawSingleLineLabel(new Rect(hud.x + 12f, hud.y + 4f, hud.width - 74f, 25f),
                "一炷江湖", timerLabelStyle, 12);
            ResponsiveGui.DrawSingleLineLabel(new Rect(hud.xMax - 60f, hud.y + 5f, 48f, 23f),
                $"Lv.{playerStats.level}", centeredStyle, 10);

            Rect timeTrack = new Rect(hud.x + 12f, hud.y + 32f, hud.width - 24f, 14f);
            DrawMainTimeTrack(timeTrack, timeRatio, accent, false);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(hud.x + 12f, hud.y + 49f, hud.width - 82f, 18f),
                GetMainTimeStateText(timeRatio), mutedStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(hud.xMax - 74f, hud.y + 49f, 62f, 18f),
                $"余 {Mathf.CeilToInt(gameFlow.mainTimeRemaining)} 息", centeredStyle, 9);

            ResponsiveGui.DrawSingleLineLabel(new Rect(hud.x + 12f, hud.y + 70f, hud.width - 24f, 16f),
                $"气血  {playerStats.runtimeStats.currentHealth:0}/{playerStats.runtimeStats.maxHealth:0}",
                mutedStyle, 9);
            Rect healthRect = new Rect(hud.x + 12f, hud.y + 88f, hud.width - 24f, 9f);
            DrawHealthBar(healthRect, playerStats.runtimeStats.HealthRatio);

            Rect resources = new Rect(safe.x + 14f, safe.y + 124f, hudWidth, 25f);
            DrawPanel(resources, new Color(0.04f, 0.05f, 0.05f, 0.9f), Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(resources.x + 10f, resources.y + 1f, resources.width - 20f, resources.height - 2f),
                $"修为 {playerStats.cultivation}/{playerStats.NextLevelRequirement}    铜钱 {playerStats.copper}",
                mutedStyle, 9);

            float preferredStatusWidth =
                ResponsiveGui.PreferredSingleLineWidth(gameFlow.statusMessage, bodyStyle, 28f);
            float statusWidth = Mathf.Clamp(preferredStatusWidth,
                ResponsiveGui.IsPortrait ? 260f : 360f, safe.width - 28f);
            float messageY = ResponsiveGui.IsPortrait ? safe.yMax - 204f : safe.yMax - 44f;
            Rect message = new Rect(safe.x + (safe.width - statusWidth) * 0.5f,
                messageY, statusWidth, 30f);
            DrawPanel(message, new Color(0.03f, 0.04f, 0.04f, 0.84f), Gold);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(message.x + 12f, message.y + 3f, message.width - 24f, message.height - 6f),
                gameFlow.statusMessage, bodyStyle, 10);
        }

        private void DrawBossApproachWarning()
        {
            BossApproachStage stage = gameFlow.CurrentBossApproachStage;
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

            Sprite bossSprite = battleScreen != null
                ? battleScreen.GetPreviewSprite(gameFlow.bossStats.visualId)
                : null;
            Rect safe = ResponsiveGui.SafeArea;
            float figureSize = ResponsiveGui.IsPortrait
                ? Mathf.Min(300f, safe.width * 0.66f)
                : Mathf.Min(340f, safe.height * 0.62f);
            float figureY = ResponsiveGui.IsPortrait
                ? safe.y + 96f
                : safe.y + (safe.height - figureSize) * 0.36f;
            Rect figureRect = new Rect(
                safe.x + (safe.width - figureSize) * 0.5f,
                figureY,
                figureSize,
                figureSize);
            if (bossSprite != null)
            {
                Color previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, reveal);
                DrawSpriteFrame(figureRect, bossSprite);
                GUI.color = previous;
            }

            float cardWidth = Mathf.Min(520f, safe.width - 34f);
            float cardHeight = ResponsiveGui.IsPortrait ? 180f : 158f;
            Rect titleCard = new Rect(
                safe.x + (safe.width - cardWidth) * 0.5f,
                safe.yMax - cardHeight - (ResponsiveGui.IsPortrait ? 82f : 30f),
                cardWidth,
                cardHeight);
            DrawPanel(titleCard, new Color(0.025f, 0.012f, 0.014f, 0.90f),
                new Color(0.82f, 0.20f, 0.13f, 0.90f));
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(titleCard.x + 18f, titleCard.y + 10f, titleCard.width - 36f, 34f),
                "终局强敌",
                bossIntroTitleStyle,
                13);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(titleCard.x + 18f, titleCard.y + 42f, titleCard.width - 36f, 58f),
                gameFlow.bossStats.displayName,
                bossIntroNameStyle,
                24);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(titleCard.x + 18f, titleCard.y + 106f, titleCard.width - 36f, 28f),
                progress < 0.60f ? "妖气压境 · 杀意已至" : "气血已复 · 决战将启",
                bossWarningStyle,
                11);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(titleCard.x + 18f, titleCard.yMax - 27f, titleCard.width - 36f, 20f),
                $"距离交锋  {gameFlow.BossIntroTimeRemaining:0.0}s",
                mutedStyle,
                9);
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
            const float contentHeight = 405f;
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
            DrawStatRow(new Rect(rect.x, y + 93f, rect.width, 27f), "吸血", $"{playerStats.runtimeStats.lifeSteal * 100f:0.#}%", "移速", playerStats.runtimeStats.moveSpeed.ToString("0.0"));

            GUI.Label(new Rect(rect.x, y + 130f, rect.width, 22f), "已习武学", headingStyle);
            if (playerStats.learnedMartialArts.Count == 0)
            {
                GUI.Label(new Rect(rect.x, y + 154f, rect.width, 38f), "尚未习得", bodyStyle);
            }
            else
            {
                string[] learned = playerStats.learnedMartialArts.Take(9).ToArray();
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
            DrawPanel(panel, new Color(0.09f, 0.105f, 0.105f, 1f), Gold);
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
            FillRect(rect, new Color(0.055f, 0.065f, 0.06f, 0.98f));
            FillRect(new Rect(rect.x, rect.y, 3f, rect.height), Gold);
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
            Rect panel = CenteredRect(380f, 190f);
            DrawPanel(panel, Panel, gameFlow.bossDefeated ? Jade : new Color(0.72f, 0.25f, 0.20f));
            GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 36f), gameFlow.bossDefeated ? "闯关功成" : "江湖路断", titleStyle);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 18f, panel.y + 54f, panel.width - 36f, 24f),
                gameFlow.statusMessage, bodyStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 18f, panel.y + 82f, panel.width - 36f, 22f),
                $"等级 {playerStats.level}  ·  击杀 {playerStats.killCount}  ·  洞穴 {playerStats.caveEntries}",
                mutedStyle, 9);
            if (GUI.Button(new Rect(panel.x + 18f, panel.yMax - 52f, panel.width - 36f, 36f), "再入江湖", actionButtonStyle))
            {
                gameFlow.StartRun();
            }
        }

        private void DrawDebugControls()
        {
            Rect safe = ResponsiveGui.SafeArea;
            Rect panel = new Rect(safe.x + 14f, safe.y + 158f, 180f, 238f);
            DrawPanel(panel, Ink, new Color(0.55f, 0.55f, 0.55f));
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, 26f), "重新开始")) gameFlow.StartRun();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 40f, panel.width - 16f, 26f), "增加修为")) gameFlow.AddDebugCultivation();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 72f, panel.width - 16f, 26f), "增加战力")) gameFlow.AddDebugPower();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 104f, panel.width - 16f, 26f), "进入 Boss")) gameFlow.ForceEnterBoss();
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 136f, panel.width - 16f, 26f), "敌人洞穴")) gameFlow.DebugEnterCave(CaveContentType.Enemy);
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 168f, panel.width - 16f, 26f), "商人洞穴")) gameFlow.DebugEnterCave(CaveContentType.Merchant);
            if (GUI.Button(new Rect(panel.x + 8f, panel.y + 200f, panel.width - 16f, 26f), "宝箱洞穴")) gameFlow.DebugEnterCave(CaveContentType.Treasure);
        }

        private float GetMainTimeRatio()
        {
            return Mathf.Clamp01(gameFlow.mainTimeRemaining / Mathf.Max(0.01f, gameFlow.mainTimeLimit));
        }

        private static string GetMainTimeStateText(float ratio)
        {
            if (ratio <= 0f)
            {
                return "香尽 · 强敌已至";
            }

            if (ratio <= 1f / 12f)
            {
                return "一线余火";
            }

            if (ratio <= 0.25f)
            {
                return "收束路线";
            }

            if (ratio <= 0.5f)
            {
                return "加紧成长";
            }

            return "从容择路";
        }

        private static Color GetMainTimeColor(float ratio)
        {
            if (ratio <= 1f / 12f)
            {
                return new Color(0.94f, 0.18f, 0.11f);
            }

            if (ratio <= 0.25f)
            {
                return new Color(0.96f, 0.48f, 0.16f);
            }

            if (ratio <= 0.5f)
            {
                return Gold;
            }

            return Jade;
        }

        private static void DrawMainTimeTrack(Rect rect, float ratio, Color color, bool paused)
        {
            ratio = Mathf.Clamp01(ratio);
            FillRect(rect, new Color(0.02f, 0.025f, 0.025f, 0.95f));

            const float border = 2f;
            Rect inner = new Rect(
                rect.x + border,
                rect.y + border,
                Mathf.Max(0f, rect.width - border * 2f),
                Mathf.Max(0f, rect.height - border * 2f));
            FillRect(inner, new Color(0.20f, 0.20f, 0.18f, 0.48f));

            float remainingWidth = inner.width * ratio;
            if (remainingWidth > 0f)
            {
                Color fillColor = paused
                    ? new Color(0.38f, 0.72f, 0.86f, 0.95f)
                    : new Color(color.r, color.g, color.b, 0.95f);
                FillRect(new Rect(inner.x, inner.y, remainingWidth, inner.height), fillColor);

                float emberPulse = paused ? 0f : 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 5f));
                float emberWidth = paused ? 2f : 3f + emberPulse * 2f;
                float emberX = Mathf.Clamp(inner.x + remainingWidth - emberWidth * 0.5f, inner.x, inner.xMax - emberWidth);
                Color ember = paused
                    ? new Color(0.75f, 0.92f, 1f, 0.85f)
                    : new Color(1f, 0.82f, 0.32f, 0.75f + emberPulse * 0.25f);
                FillRect(new Rect(emberX, inner.y - 1f, emberWidth, inner.height + 2f), ember);
            }

            for (int i = 1; i < 4; i++)
            {
                float markerX = inner.x + inner.width * i / 4f;
                FillRect(new Rect(markerX, inner.y, 1f, inner.height),
                    new Color(0f, 0f, 0f, 0.30f));
            }
        }

        private void DrawHealthBar(Rect rect, float ratio)
        {
            if (healthBarBase != null)
            {
                GUI.DrawTexture(rect, healthBarBase, ScaleMode.StretchToFill, true);
            }
            else
            {
                FillRect(rect, new Color(0.12f, 0.08f, 0.07f));
            }

            float border = Mathf.Clamp(rect.height * 0.22f, 2f, 5f);
            Rect fill = new Rect(rect.x + border, rect.y + border, Mathf.Max(0f, (rect.width - border * 2f) * ratio), rect.height - border * 2f);
            if (healthBarFill != null)
            {
                GUI.DrawTexture(fill, healthBarFill, ScaleMode.StretchToFill, true);
            }
            else
            {
                FillRect(fill, new Color(0.72f, 0.18f, 0.16f));
            }
        }

        private static void DrawPanel(Rect rect, Color background, Color accent)
        {
            FillRect(rect, background);
            FillRect(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, 0.12f));
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
                name = "RuntimeSettingsIcon",
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
    }
}
