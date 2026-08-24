using System;
using UnityEngine;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.UI
{
    [DisallowMultipleComponent]
    public class BattleScreenController : MonoBehaviour
    {
        [Serializable]
        public class EnemyVisualProfile
        {
            public string id;
            public Sprite[] idleFrames;
            public Sprite[] attackFrames;
            [Min(0.5f)] public float scale = ActorVisualScale.Medium;
            public bool flipHorizontally;
        }

        public GameFlowController gameFlow;
        public PlayerStats playerStats;
        public BattleManager battleManager;
        public Texture2D actorTexture;
        public Texture2D[] normalBattleBackgrounds;
        public Texture2D bossBattleBackground;
        public Sprite[] playerIdleFrames;
        public Sprite[] playerAttackFrames;
        public Sprite[] enemyIdleFrames;
        public Sprite[] enemyAttackFrames;
        public Sprite[] eliteIdleFrames;
        public Sprite[] eliteAttackFrames;
        public Sprite[] caveIdleFrames;
        public Sprite[] caveAttackFrames;
        public Sprite[] impactEffectFrames;
        public Sprite[] swordQiEffectFrames;
        public Sprite[] poisonEffectFrames;
        public EnemyVisualProfile[] enemyVisualProfiles;
        [Min(0.5f)] public float playerSpriteScale = ActorVisualScale.Medium;
        [Min(0.5f)] public float bossSpriteScale = ActorVisualScale.Large;
        [Header("战斗角色尺寸")]
        [Tooltip("统一放大战斗中的玩家与普通敌人。Boss 会使用较保守的增幅，避免遮挡顶部信息。")]
        [Range(1f, 1.35f)] public float battleActorScale = 1.18f;
        [Header("窗口适配")]
        [Tooltip("战斗 UI 的设计基准宽度。实际窗口会在不裁切 UI 的前提下等比缩放。")]
        [Min(320f)] public float referenceWidth = ResponsiveGui.ReferenceWidth;
        [Tooltip("战斗 UI 的设计基准高度。实际窗口会在不裁切 UI 的前提下等比缩放。")]
        [Min(180f)] public float referenceHeight = ResponsiveGui.ReferenceHeight;
        [Header("战斗视觉节奏")]
        [Tooltip("只延长攻击动画的视觉呈现，不改变 BattleManager 的攻击间隔或战斗计时。")]
        [Range(0.32f, 0.65f)] public float attackVisualDuration = 0.46f;
        [Tooltip("仅暴击或闪避时使用的屏幕抖动时长。")]
        [Range(0.12f, 0.35f)] public float exceptionalScreenShakeDuration = 0.24f;

        private GUIStyle titleStyle;
        private GUIStyle leftNameStyle;
        private GUIStyle rightNameStyle;
        private GUIStyle centerStyle;
        private GUIStyle timerStyle;
        private GUIStyle detailStyle;
        private GUIStyle captionStyle;
        private GUIStyle actorMarkStyle;
        private GUIStyle damageStyle;
        private GUIStyle damageShadowStyle;
        private GUIStyle criticalDamageStyle;
        private GUIStyle criticalDamageShadowStyle;
        private GUIStyle poisonValueStyle;
        private GUIStyle poisonValueShadowStyle;
        private GUIStyle poisonCaptionStyle;
        private GUIStyle skillCalloutStyle;
        private GUIStyle skillCalloutCaptionStyle;
        private GUIStyle bossWarningStyle;
        private GUIStyle bossCountdownStyle;
        private GUIStyle dialogueKickerStyle;
        private GUIStyle dialogueTitleStyle;
        private GUIStyle dialogueSpeakerStyle;
        private GUIStyle dialogueBodyStyle;
        private GUIStyle dialogueHintStyle;
        private GUIStyle dialogueButtonStyle;
        private int observedAttackSequence;
        private float attackStartedAt = -10f;
        private CombatantStats trackedPlayer;
        private CombatantStats trackedEnemy;
        private float previousPlayerHealth;
        private float previousEnemyHealth;
        private float playerDamageAmount;
        private float enemyDamageAmount;
        private float playerDamageStartedAt = -10f;
        private float enemyDamageStartedAt = -10f;
        private bool playerDamageWasCritical;
        private bool enemyDamageWasCritical;
        private float hitFeedbackStartedAt = -10f;
        private bool hitFeedbackWasCritical;
        private bool hitFeedbackWasDodged;
        private bool hitFeedbackTargetedPlayer;
        private BattleVfxCue activeVfxCues;
        private string activeSkillVfxName = string.Empty;
        private int activePoisonStackDelta;
        private int activePoisonStacks;
        private float activePoisonDamage;
        private BattleVfxCue debugPreviewVfxCues;
        private float debugPreviewVfxUntil = -10f;
        private CombatantStats backgroundTrackedEnemy;
        private Texture2D activeBattleBackground;
        private int lastNormalBackgroundIndex = -1;
        private bool introBackgroundPrepared;
        private Texture2D bossFoxfireIcon;
        private Texture2D bossArmorIcon;
        private Texture2D bossFrenzyIcon;

        private const float DamageDisplayDuration = 0.72f;
        private const float HealthFlashDuration = 0.34f;
        private const float ImpactMarkerDuration = 0.28f;
        private const float PoisonPopupDuration = 0.96f;
        private const float SkillCalloutDuration = 0.72f;

        private static readonly Color Backdrop = new Color(0.055f, 0.075f, 0.09f, 1f);
        private static readonly Color DistantMountain = new Color(0.11f, 0.19f, 0.20f, 1f);
        private static readonly Color Ground = new Color(0.18f, 0.16f, 0.13f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.075f, 1f);
        private static readonly Color PlayerColor = new Color(0.18f, 0.68f, 0.88f, 1f);
        private static readonly Color EnemyColor = new Color(0.82f, 0.22f, 0.17f, 1f);
        private static readonly Color Gold = new Color(0.82f, 0.66f, 0.32f, 1f);
        private static readonly Color Poison = new Color(0.43f, 0.78f, 0.34f, 1f);
        private static readonly Color PoisonDark = new Color(0.10f, 0.20f, 0.11f, 0.94f);
        private static readonly Color Panel = new Color(0.025f, 0.035f, 0.045f, 0.78f);

        private void Update()
        {
            if (gameFlow != null && gameFlow.IsOpeningIntroActive &&
                (Input.GetKeyDown(KeyCode.Space) ||
                 Input.GetKeyDown(KeyCode.Return) ||
                 Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                gameFlow.AdvanceOpeningIntro();
            }

            if (gameFlow == null || !gameFlow.IsOpeningIntroActive)
            {
                introBackgroundPrepared = false;
            }
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            bool introActive = gameFlow != null && gameFlow.IsOpeningIntroActive;
            if (gameFlow == null || playerStats == null || playerStats.runtimeStats == null ||
                (!introActive &&
                 (battleManager == null || !battleManager.IsBattleActive || battleManager.currentEnemy == null)))
            {
                return;
            }

            GUI.depth = -1000;
            EnsureStyles();
            if (introActive)
            {
                DrawOpeningIntro();
                return;
            }

            TrackLatestAttack();
            TrackHealthChanges();
            UpdateBattleBackground();

            float guiScale = CalculateGuiScale(Screen.width, Screen.height);
            float width = Screen.width / guiScale;
            float height = Screen.height / guiScale;
            bool portrait = ResponsiveGui.IsPortrait;
            Rect safe = ResponsiveGui.SafeArea;
            Matrix4x4 originalGuiMatrix = GUI.matrix;
            Vector2 screenShake = CalculateScreenShake();
            Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f));
            Matrix4x4 shakeMatrix = Matrix4x4.TRS(new Vector3(screenShake.x, screenShake.y, 0f),
                Quaternion.identity, Vector3.one);
            GUI.matrix = scaleMatrix * shakeMatrix * originalGuiMatrix;
            DrawBackdrop(width, height);
            DrawHeader(width);

            float sidePadding = portrait ? safe.x + 16f : Mathf.Clamp(width * 0.055f, 34f, 72f);
            float healthTop = safe.y + (portrait ? 68f : 89f);
            float healthHeight = portrait ? 64f : 72f;
            float healthWidth = portrait
                ? (safe.width - 44f) * 0.5f
                : Mathf.Min(284f, width * 0.30f);
            Rect enemyHealthRect = portrait
                ? new Rect(safe.xMax - 16f - healthWidth, healthTop, healthWidth, healthHeight)
                : new Rect(width - sidePadding - healthWidth, healthTop, healthWidth, healthHeight);
            // The player health bar now lives in PrototypeHUDController's unified
            // health + martial-art HUD. Keep the enemy panel here so combat still
            // has a clear target readout without duplicating the player's health.
            DrawEnemyHealthPanel(enemyHealthRect, battleManager.currentEnemy,
                enemyDamageAmount, enemyDamageStartedAt);
            if (battleManager.IsBossBattle)
            {
                DrawBossSkillStrip(new Rect(
                    enemyHealthRect.x,
                    enemyHealthRect.yMax + 4f,
                    enemyHealthRect.width,
                    portrait ? 29f : 34f));
            }
            float duelTop = portrait ? healthTop + 100f : healthTop;
            float duelHeight = portrait ? 34f : 52f;
            DrawDuelFocus(width, duelTop, duelHeight);

            float messageHeight = portrait ? 54f : 50f;
            Rect messageRect = portrait
                ? new Rect(safe.x + 10f, safe.yMax - messageHeight - 10f, safe.width - 20f, messageHeight)
                : new Rect(width * 0.07f, height - messageHeight - 10f, width * 0.86f, messageHeight);
            float stageTop = portrait
                ? duelTop + duelHeight + 4f
                : healthTop + 116f;
            float stageBottom = messageRect.y - 2f;
            float stageHeight = Mathf.Max(80f, stageBottom - stageTop);
            Rect stageRect = new Rect(0f, stageTop, width, stageHeight);

            float baseActorSize = portrait
                ? Mathf.Clamp(Mathf.Min(width * 0.28f, stageHeight * 0.54f), 104f, 164f)
                : Mathf.Clamp(Mathf.Min(width * 0.25f, stageHeight * 0.94f), 120f, 290f);
            float actorSize = baseActorSize * battleActorScale;
            EnemyVisualProfile enemyVisual = SelectEnemyVisualProfile();
            Sprite[] currentEnemyIdleFrames = SelectEnemyFrames(enemyVisual, false);
            Sprite[] currentEnemyAttackFrames = SelectEnemyFrames(enemyVisual, true);
            float playerActorSize = actorSize * playerSpriteScale;
            float enemySpriteScale = gameFlow.CurrentPhase == GamePhase.BossBattle
                ? bossSpriteScale
                : enemyVisual != null ? enemyVisual.scale : ActorVisualScale.Medium;
            float enemyBaseSize = gameFlow.CurrentPhase == GamePhase.BossBattle
                ? baseActorSize * Mathf.Min(battleActorScale, 1.06f)
                : actorSize;
            float enemyActorSize = enemyBaseSize * enemySpriteScale;
            float tallestActorSize = Mathf.Max(playerActorSize, enemyActorSize);
            float baseY = portrait
                ? Mathf.Clamp(
                    safe.center.y + tallestActorSize * 0.5f,
                    stageTop + tallestActorSize,
                    messageRect.y - 24f)
                : stageBottom - 6f;
            // Visual pacing is intentionally independent from BattleManager's attack cadence.
            // A new resolved attack can replace the current pose, but never delays damage or timers.
            float attackDuration = Mathf.Max(0.01f, attackVisualDuration);
            float actionProgress = Mathf.Clamp01((Time.unscaledTime - attackStartedAt) / attackDuration);
            float lunge = Mathf.Sin(actionProgress * Mathf.PI) * Mathf.Min(54f, width * 0.05f);
            bool shouldShakeTarget = battleManager.LastAttackWasCritical ||
                                     battleManager.LastAttackWasDodged;
            float shake = shouldShakeTarget && actionProgress > 0.38f && actionProgress < 0.78f
                ? Mathf.Sin(actionProgress * 70f) * 7f
                : 0f;

            float playerX = width * (portrait ? 0.25f : 0.30f) - actorSize * 0.5f;
            float enemyX = width * (portrait ? 0.75f : 0.70f) - actorSize * 0.5f;
            if (actionProgress < 1f)
            {
                playerX += lunge;
                enemyX -= lunge;
                if (battleManager.LastAttackWasPlayer)
                {
                    enemyX += shake;
                }
                else
                {
                    playerX += shake;
                }
            }

            bool playerAttacking = actionProgress < 1f;
            bool enemyAttacking = actionProgress < 1f;
            Rect playerRect = new Rect(playerX + (actorSize - playerActorSize) * 0.5f,
                baseY - playerActorSize, playerActorSize, playerActorSize);
            Rect enemyRect = new Rect(enemyX + (actorSize - enemyActorSize) * 0.5f,
                baseY - enemyActorSize, enemyActorSize, enemyActorSize);
            DrawPersistentBattleAuras(playerRect, enemyRect);
            DrawFighter(playerRect, PlayerColor, "侠", false,
                playerAttacking ? playerAttackFrames : playerIdleFrames, playerAttacking, actionProgress);
            if (battleManager.IsBossBattle)
            {
                DrawBossAura(enemyRect);
            }
            DrawFighter(enemyRect, EnemyColor, "敌", enemyVisual != null ? enemyVisual.flipHorizontally : true,
                enemyAttacking ? currentEnemyAttackFrames : currentEnemyIdleFrames, enemyAttacking, actionProgress,
                GetEnemySpriteTint());
            DrawBattleSkillVfx(playerRect, enemyRect,
                enemyVisual != null ? enemyVisual.flipHorizontally : true);
            DrawSkillCallout(playerRect, enemyRect);
            DrawImpactMarker(playerRect, playerDamageAmount, playerDamageStartedAt, playerDamageWasCritical, true);
            DrawImpactMarker(enemyRect, enemyDamageAmount, enemyDamageStartedAt, enemyDamageWasCritical, false);
            DrawDamagePopup(playerRect, playerDamageAmount, playerDamageStartedAt, playerDamageWasCritical, true);
            if ((activeVfxCues & BattleVfxCue.PoisonTick) == 0)
            {
                DrawDamagePopup(enemyRect, enemyDamageAmount, enemyDamageStartedAt, enemyDamageWasCritical, false);
            }
            DrawPoisonPopup(enemyRect);
            DrawPoisonStatusBadge(enemyRect);
            DrawCombatMessage(stageRect, messageRect, actionProgress);
            DrawBossSkillBanner(stageRect);
            DrawBossApproachOverlay(width, height);
            DrawPlayerHitOverlay(width, height);
            GUI.matrix = originalGuiMatrix;
        }

        private float CalculateGuiScale(float screenWidth, float screenHeight)
        {
            return ResponsiveGui.CalculateScale(screenWidth, screenHeight);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = CreateStyle(24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            leftNameStyle = CreateStyle(17, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            rightNameStyle = CreateStyle(17, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
            centerStyle = CreateStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            timerStyle = CreateStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.58f));
            detailStyle = CreateStyle(13, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.86f, 0.89f, 0.90f));
            captionStyle = CreateStyle(11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.70f, 0.73f, 0.74f));
            actorMarkStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            damageStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            damageShadowStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.9f));
            criticalDamageStyle = CreateStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            criticalDamageShadowStyle = CreateStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.95f));
            ConfigureFloatingNumberStyle(damageStyle);
            ConfigureFloatingNumberStyle(damageShadowStyle);
            ConfigureFloatingNumberStyle(criticalDamageStyle);
            ConfigureFloatingNumberStyle(criticalDamageShadowStyle);
            poisonValueStyle = CreateStyle(34, FontStyle.Bold, TextAnchor.MiddleCenter, Poison);
            poisonValueShadowStyle = CreateStyle(34, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.015f, 0.03f, 0.012f, 0.96f));
            poisonCaptionStyle = CreateStyle(13, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.78f, 0.92f, 0.66f));
            skillCalloutStyle = CreateStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.95f, 0.91f, 0.76f));
            skillCalloutCaptionStyle = CreateStyle(10, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.72f, 0.73f, 0.66f));
            bossWarningStyle = CreateStyle(22, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.76f, 0.36f));
            bossCountdownStyle = CreateStyle(72, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.20f, 0.12f));
            dialogueKickerStyle = CreateStyle(12, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.72f, 0.34f));
            dialogueTitleStyle = CreateStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            dialogueSpeakerStyle = CreateStyle(17, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(1f, 0.84f, 0.48f));
            dialogueBodyStyle = CreateStyle(18, FontStyle.Normal, TextAnchor.UpperLeft,
                new Color(0.94f, 0.92f, 0.84f));
            dialogueBodyStyle.wordWrap = true;
            dialogueBodyStyle.clipping = TextClipping.Clip;
            dialogueHintStyle = CreateStyle(11, FontStyle.Normal, TextAnchor.MiddleLeft,
                new Color(0.68f, 0.72f, 0.72f));
            dialogueButtonStyle = WuxiaUiTheme.CreateButtonStyle(
                16, WuxiaButtonKind.Primary);
        }

        private void DrawOpeningIntro()
        {
            if (!introBackgroundPrepared)
            {
                activeBattleBackground = bossBattleBackground != null
                    ? bossBattleBackground
                    : SelectRandomNormalBackground();
                backgroundTrackedEnemy = null;
                introBackgroundPrepared = true;
            }

            float guiScale = CalculateGuiScale(Screen.width, Screen.height);
            float width = Screen.width / guiScale;
            float height = Screen.height / guiScale;
            bool portrait = ResponsiveGui.IsPortrait;
            Rect safe = ResponsiveGui.SafeArea;
            Matrix4x4 originalGuiMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(guiScale, guiScale, 1f)) * originalGuiMatrix;

            DrawBackdrop(width, height);
            FillRect(new Rect(0f, 0f, width, height), new Color(0.025f, 0.012f, 0.025f, 0.30f));
            FillRect(new Rect(0f, 0f, width, height * 0.20f), new Color(0f, 0f, 0f, 0.42f));
            FillRect(new Rect(0f, height * 0.72f, width, height * 0.28f), new Color(0f, 0f, 0f, 0.34f));

            float titleWidth = Mathf.Min(portrait ? safe.width - 32f : 540f, safe.width - 24f);
            Rect titlePanel = new Rect(
                safe.x + (safe.width - titleWidth) * 0.5f,
                safe.y + 12f,
                titleWidth,
                portrait ? 86f : 76f);
            WuxiaUiTheme.DrawPanel(titlePanel,
                new Color(0.025f, 0.018f, 0.025f, 0.80f), Gold,
                WuxiaPanelKind.Paper);
            GUI.Label(new Rect(titlePanel.x, titlePanel.y + 5f, titlePanel.width, 20f),
                "江湖序章", dialogueKickerStyle);
            GUI.Label(new Rect(titlePanel.x, titlePanel.y + 25f, titlePanel.width, 42f),
                "狐 火 初 现", dialogueTitleStyle);

            float panelHeight = portrait ? 236f : 150f;
            Rect dialoguePanel = new Rect(
                safe.x + (portrait ? 12f : 30f),
                safe.yMax - panelHeight - (portrait ? 14f : 18f),
                safe.width - (portrait ? 24f : 60f),
                panelHeight);
            DrawOpeningPortraits(width, height, safe, dialoguePanel, portrait);

            WuxiaUiTheme.DrawPanel(dialoguePanel,
                new Color(0.018f, 0.022f, 0.025f, 0.94f), Gold,
                WuxiaPanelKind.Paper);
            FillRect(new Rect(dialoguePanel.x + 18f, dialoguePanel.y + 39f,
                dialoguePanel.width - 36f, 1f), new Color(Gold.r, Gold.g, Gold.b, 0.38f));

            GUI.Label(new Rect(dialoguePanel.x + 22f, dialoguePanel.y + 8f,
                dialoguePanel.width - 44f, 28f), gameFlow.CurrentOpeningSpeaker, dialogueSpeakerStyle);

            float buttonWidth = portrait ? dialoguePanel.width - 40f : 150f;
            float buttonHeight = portrait ? 44f : 40f;
            Rect buttonRect = new Rect(
                portrait ? dialoguePanel.x + 20f : dialoguePanel.xMax - buttonWidth - 20f,
                dialoguePanel.yMax - buttonHeight - 16f,
                buttonWidth,
                buttonHeight);
            float bodyBottom = portrait ? buttonRect.y - 10f : dialoguePanel.yMax - 18f;
            float bodyWidth = portrait
                ? dialoguePanel.width - 44f
                : buttonRect.x - dialoguePanel.x - 42f;
            Rect bodyRect = new Rect(
                dialoguePanel.x + 22f,
                dialoguePanel.y + 48f,
                bodyWidth,
                Mathf.Max(48f, bodyBottom - dialoguePanel.y - 48f));
            GUI.Label(bodyRect, gameFlow.CurrentOpeningDialogue, dialogueBodyStyle);

            string buttonText = gameFlow.OpeningDialogueIndex >= gameFlow.OpeningDialogueCount - 1
                ? "点燃主香"
                : "继续";
            if (GUI.Button(buttonRect, buttonText, dialogueButtonStyle))
            {
                gameFlow.AdvanceOpeningIntro();
            }

            if (!portrait)
            {
                GUI.Label(new Rect(dialoguePanel.x + 22f, dialoguePanel.yMax - 28f,
                    Mathf.Max(120f, bodyWidth), 18f), "点击按钮或按 空格 / 回车 继续", dialogueHintStyle);
            }

            GUI.matrix = originalGuiMatrix;
        }

        private void DrawOpeningPortraits(
            float width,
            float height,
            Rect safe,
            Rect dialoguePanel,
            bool portrait)
        {
            EnemyVisualProfile foxProfile = FindEnemyVisualProfile("fox_demon_boss");
            Sprite[] foxFrames = foxProfile != null ? foxProfile.idleFrames : null;
            float portraitSize = portrait
                ? Mathf.Clamp(safe.width * 0.43f, 150f, 210f)
                : Mathf.Clamp(height * 0.46f, 190f, 270f);
            float bottom = dialoguePanel.y + (portrait ? 14f : 8f);
            Rect playerRect = new Rect(
                portrait ? safe.x + 6f : width * 0.09f,
                bottom - portraitSize,
                portraitSize,
                portraitSize);
            Rect foxRect = new Rect(
                portrait ? safe.xMax - portraitSize * 1.10f - 6f : width * 0.91f - portraitSize * 1.10f,
                bottom - portraitSize * 1.10f,
                portraitSize * 1.10f,
                portraitSize * 1.10f);

            int dialogueIndex = gameFlow.OpeningDialogueIndex;
            bool playerSpeaking = dialogueIndex == 1 || dialogueIndex == 3;
            bool foxSpeaking = dialogueIndex == 2;
            DrawDialoguePortrait(playerRect, playerIdleFrames, false, "侠",
                playerSpeaking ? 1f : 0.62f, playerSpeaking);
            DrawDialoguePortrait(foxRect, foxFrames, foxProfile != null && foxProfile.flipHorizontally, "妖",
                foxSpeaking ? 1f : 0.62f, foxSpeaking);
        }

        private void DrawDialoguePortrait(
            Rect rect,
            Sprite[] frames,
            bool facesLeft,
            string fallbackMark,
            float alpha,
            bool highlighted)
        {
            if (highlighted)
            {
                FillRect(new Rect(rect.x - 5f, rect.y - 5f, rect.width + 10f, rect.height + 10f),
                    new Color(Gold.r, Gold.g, Gold.b, 0.25f));
            }

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Sprite frame = GetFrame(frames, false, 0f);
            if (frame != null)
            {
                DrawSprite(rect, frame, facesLeft);
            }
            else
            {
                GUI.DrawTexture(rect, actorTexture != null ? actorTexture : Texture2D.whiteTexture,
                    ScaleMode.StretchToFill, true);
                GUI.Label(rect, fallbackMark, actorMarkStyle);
            }

            GUI.color = previous;
            FillRect(new Rect(rect.x + rect.width * 0.16f, rect.yMax - 3f,
                rect.width * 0.68f, 7f), new Color(0f, 0f, 0f, 0.40f));
        }

        private void TrackLatestAttack()
        {
            if (battleManager.AttackSequence <= 0)
            {
                observedAttackSequence = 0;
                attackStartedAt = -10f;
                activeSkillVfxName = string.Empty;
                activePoisonStackDelta = 0;
                activePoisonStacks = 0;
                activePoisonDamage = 0f;
                return;
            }

            if (observedAttackSequence == battleManager.AttackSequence)
            {
                return;
            }

            observedAttackSequence = battleManager.AttackSequence;
            attackStartedAt = Time.unscaledTime;
            activeVfxCues = battleManager.LastVfxCues;
            activeSkillVfxName = battleManager.LastSkillVfxName;
            activePoisonStackDelta = battleManager.LastPoisonStackDelta;
            activePoisonStacks = battleManager.EnemyPoisonStacks;
            activePoisonDamage = battleManager.LastPoisonDamage;
            hitFeedbackStartedAt = Time.unscaledTime;
            hitFeedbackWasCritical = battleManager.LastAttackWasCritical;
            hitFeedbackWasDodged = battleManager.LastAttackWasDodged;
            hitFeedbackTargetedPlayer = !battleManager.LastAttackWasPlayer;
        }

        private Vector2 CalculateScreenShake()
        {
            if (!hitFeedbackWasCritical && !hitFeedbackWasDodged)
            {
                return Vector2.zero;
            }

            float age = Time.unscaledTime - hitFeedbackStartedAt;
            float shakeDuration = Mathf.Max(0.01f, exceptionalScreenShakeDuration);
            if (age < 0f || age >= shakeDuration)
            {
                return Vector2.zero;
            }

            float strength = hitFeedbackWasCritical ? 11f : 1.6f;
            if (hitFeedbackTargetedPlayer)
            {
                strength *= 1.12f;
            }

            float envelope = 1f - age / shakeDuration;
            float phase = age * 92f + observedAttackSequence * 1.71f;
            return new Vector2(Mathf.Sin(phase), Mathf.Cos(phase * 1.19f)) * strength * envelope;
        }

        private void TrackHealthChanges()
        {
            CombatantStats currentPlayer = playerStats.runtimeStats;
            CombatantStats currentEnemy = battleManager.currentEnemy;
            if (!ReferenceEquals(trackedPlayer, currentPlayer) || !ReferenceEquals(trackedEnemy, currentEnemy))
            {
                trackedPlayer = currentPlayer;
                trackedEnemy = currentEnemy;
                previousPlayerHealth = currentPlayer.currentHealth;
                previousEnemyHealth = currentEnemy.currentHealth;
                playerDamageAmount = 0f;
                enemyDamageAmount = 0f;
                playerDamageStartedAt = -10f;
                enemyDamageStartedAt = -10f;
                return;
            }

            if (currentPlayer.currentHealth < previousPlayerHealth - 0.01f)
            {
                playerDamageAmount = previousPlayerHealth - currentPlayer.currentHealth;
                playerDamageStartedAt = Time.unscaledTime;
                playerDamageWasCritical = !battleManager.LastAttackWasPlayer && battleManager.LastAttackWasCritical;
            }

            if (currentEnemy.currentHealth < previousEnemyHealth - 0.01f)
            {
                enemyDamageAmount = previousEnemyHealth - currentEnemy.currentHealth;
                enemyDamageStartedAt = Time.unscaledTime;
                enemyDamageWasCritical = battleManager.LastAttackWasPlayer && battleManager.LastAttackWasCritical;
            }

            previousPlayerHealth = currentPlayer.currentHealth;
            previousEnemyHealth = currentEnemy.currentHealth;
        }

        private void DrawBackdrop(float width, float height)
        {
            const float shakeOverscan = 24f;
            float overscannedWidth = width + shakeOverscan * 2f;
            Rect backgroundRect = new Rect(-shakeOverscan, -shakeOverscan, overscannedWidth,
                height + shakeOverscan * 2f);
            if (activeBattleBackground != null)
            {
                GUI.DrawTexture(backgroundRect, activeBattleBackground, ScaleMode.ScaleAndCrop, true);
                FillRect(new Rect(-shakeOverscan, -shakeOverscan, overscannedWidth,
                    height * 0.22f + shakeOverscan), new Color(0f, 0f, 0f, 0.28f));
                FillRect(new Rect(-shakeOverscan, height * 0.84f, overscannedWidth,
                    height * 0.16f + shakeOverscan), new Color(0f, 0f, 0f, 0.12f));
                return;
            }

            FillRect(backgroundRect, Backdrop);
            FillRect(new Rect(-shakeOverscan, height * 0.34f, overscannedWidth,
                height * 0.26f), DistantMountain);
            FillRect(new Rect(-shakeOverscan, height * 0.58f, overscannedWidth,
                height * 0.42f + shakeOverscan), Ground);
            FillRect(new Rect(-shakeOverscan, height * 0.575f, overscannedWidth, 5f),
                new Color(0.65f, 0.52f, 0.28f));

            float moonSize = Mathf.Clamp(height * 0.13f, 64f, 110f);
            FillRect(new Rect(width * 0.5f - moonSize * 0.5f, height * 0.37f - moonSize * 0.5f, moonSize, moonSize), new Color(0.82f, 0.78f, 0.63f, 0.23f));
        }

        private void UpdateBattleBackground()
        {
            CombatantStats currentEnemy = battleManager.currentEnemy;
            if (ReferenceEquals(backgroundTrackedEnemy, currentEnemy))
            {
                return;
            }

            backgroundTrackedEnemy = currentEnemy;
            activeBattleBackground = gameFlow.CurrentPhase == GamePhase.BossBattle &&
                                     bossBattleBackground != null
                ? bossBattleBackground
                : SelectRandomNormalBackground();
        }

        private Texture2D SelectRandomNormalBackground()
        {
            if (normalBattleBackgrounds == null || normalBattleBackgrounds.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            int onlyValidIndex = -1;
            for (int i = 0; i < normalBattleBackgrounds.Length; i++)
            {
                if (normalBattleBackgrounds[i] == null)
                {
                    continue;
                }

                validCount++;
                onlyValidIndex = i;
            }

            if (validCount == 0)
            {
                return null;
            }

            if (validCount == 1)
            {
                lastNormalBackgroundIndex = onlyValidIndex;
                return normalBattleBackgrounds[onlyValidIndex];
            }

            int startIndex = UnityEngine.Random.Range(0, normalBattleBackgrounds.Length);
            for (int offset = 0; offset < normalBattleBackgrounds.Length; offset++)
            {
                int index = (startIndex + offset) % normalBattleBackgrounds.Length;
                if (index == lastNormalBackgroundIndex || normalBattleBackgrounds[index] == null)
                {
                    continue;
                }

                lastNormalBackgroundIndex = index;
                return normalBattleBackgrounds[index];
            }

            lastNormalBackgroundIndex = onlyValidIndex;
            return normalBattleBackgrounds[onlyValidIndex];
        }

        private void DrawHeader(float width)
        {
            Rect safe = ResponsiveGui.SafeArea;
            bool portrait = ResponsiveGui.IsPortrait;
            BossApproachStage approachStage = gameFlow.CurrentBossApproachStage;
            float headerWidth = portrait
                ? Mathf.Max(260f, safe.width - 86f)
                : Mathf.Min(480f, width * 0.50f);
            float headerX = portrait
                ? safe.x + 14f
                : (width - headerWidth) * 0.5f;
            Rect headerRect = new Rect(headerX, safe.y + 7f, headerWidth, portrait ? 56f : 74f);
            Color headerAccent = approachStage == BossApproachStage.FinalCountdown ||
                                 approachStage == BossApproachStage.Arrived
                ? new Color(0.94f, 0.18f, 0.11f)
                : approachStage == BossApproachStage.Imminent ||
                  approachStage == BossApproachStage.Omen
                    ? new Color(0.95f, 0.55f, 0.18f)
                    : Gold;
            if (portrait)
            {
                WuxiaUiTheme.DrawCompactSurface(
                    headerRect, new Color(0.02f, 0.03f, 0.04f, 0.76f), headerAccent);
            }
            else
            {
                WuxiaUiTheme.DrawPanel(headerRect,
                    new Color(0.02f, 0.03f, 0.04f, 0.70f), headerAccent,
                    gameFlow.CurrentPhase == GamePhase.BossBattle
                        ? WuxiaPanelKind.Boss
                        : WuxiaPanelKind.Combat);
            }
            FillRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 2f), headerAccent);
            FillRect(new Rect(headerRect.x + headerRect.width * 0.18f, headerRect.yMax - 1f,
                headerRect.width * 0.64f, 1f),
                new Color(headerAccent.r, headerAccent.g, headerAccent.b, 0.55f));

            string title = gameFlow.CurrentPhase == GamePhase.BossBattle
                ? $"决战 · {gameFlow.bossStats.displayName}"
                : gameFlow.CurrentPhase == GamePhase.CaveRunning
                    ? "秘境 · 自动战斗"
                    : "遭遇 · 自动战斗";

            if (portrait)
            {
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(headerRect.x + 10f, headerRect.y + 2f,
                        headerRect.width * 0.52f, 24f),
                    title, titleStyle, 11);

                if (gameFlow.CurrentPhase == GamePhase.BossBattle)
                {
                    ResponsiveGui.DrawSingleLineLabel(
                        new Rect(headerRect.x + headerRect.width * 0.49f, headerRect.y + 3f,
                            headerRect.width * 0.48f, 22f),
                        $"独立 {gameFlow.bossBattleTime:0.0} 秒", timerStyle, 9);
                    ResponsiveGui.DrawSingleLineLabel(
                        new Rect(headerRect.x + 10f, headerRect.y + 28f,
                            headerRect.width - 20f, 18f),
                        $"{GetBossPhaseOrdinal()} · {battleManager.CurrentBossPhaseName}", captionStyle, 8);
                    return;
                }

                bool compactPaused = gameFlow.CurrentPhase == GamePhase.CaveRunning;
                float compactRatio = Mathf.Clamp01(
                    gameFlow.mainTimeRemaining / Mathf.Max(0.01f, gameFlow.mainTimeLimit));
                string compactTimer = compactPaused
                    ? "主香暂停"
                    : approachStage == BossApproachStage.Arrived
                        ? "香尽 · 战后决战"
                        : $"余 {Mathf.CeilToInt(gameFlow.mainTimeRemaining)} 息";
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(headerRect.x + headerRect.width * 0.55f, headerRect.y + 3f,
                        headerRect.width * 0.42f, 22f),
                    compactTimer, timerStyle, 9);
                DrawMainTimeTrack(
                    new Rect(headerRect.x + 12f, headerRect.y + 31f,
                        headerRect.width - 24f, 16f),
                    compactRatio, compactPaused);
                return;
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(headerRect.x, headerRect.y + 3f, headerRect.width, 27f),
                title, titleStyle, 13);

            if (gameFlow.CurrentPhase == GamePhase.BossBattle)
            {
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(headerRect.x, headerRect.y + 30f, headerRect.width, 20f),
                    $"决战独立计时  {gameFlow.bossBattleTime:0.0} 秒", timerStyle, 9);
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(headerRect.x, headerRect.y + 51f, headerRect.width, 15f),
                    $"主时间停止 · {GetBossPhaseOrdinal()} · {battleManager.CurrentBossPhaseName}", captionStyle, 8);
                return;
            }

            bool timerPaused = gameFlow.CurrentPhase == GamePhase.CaveRunning;
            float timeRatio = Mathf.Clamp01(
                gameFlow.mainTimeRemaining / Mathf.Max(0.01f, gameFlow.mainTimeLimit));
            string timerText = timerPaused
                ? "洞中凝时 · 主香暂停"
                : approachStage == BossApproachStage.Arrived
                    ? "香已燃尽 · 胜此战后即入决战"
                    : timeRatio <= 1f / 3f
                        ? $"丧钟已鸣 · 仅余 {Mathf.CeilToInt(gameFlow.mainTimeRemaining)} 息"
                        : $"交锋耗时 · 仅余 {Mathf.CeilToInt(gameFlow.mainTimeRemaining)} 息";
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(headerRect.x + 12f, headerRect.y + 28f, headerRect.width - 24f, 16f),
                timerText, timerStyle, 9);

            Rect timeTrack = new Rect(
                headerRect.x + 16f,
                headerRect.y + 43f,
                headerRect.width - 32f,
                18f);
            DrawMainTimeTrack(timeTrack, timeRatio, timerPaused);

            string stateText = timerPaused
                ? "香火停驻 · 返图后续燃"
                : GetMainTimeStateText(timeRatio);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(headerRect.x, headerRect.y + 61f, headerRect.width, 12f),
                stateText, captionStyle, 8);
        }

        private void DrawBossApproachOverlay(float width, float height)
        {
            if (gameFlow.CurrentPhase != GamePhase.NormalBattleRunning)
            {
                return;
            }

            BossApproachStage stage = gameFlow.CurrentBossApproachStage;
            if (stage != BossApproachStage.FinalCountdown && stage != BossApproachStage.Arrived)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 7f));
            float edge = ResponsiveGui.IsPortrait ? 18f : 14f;
            Color danger = new Color(0.80f, 0.035f, 0.02f, 0.22f + pulse * 0.22f);
            FillRect(new Rect(0f, 0f, width, edge), danger);
            FillRect(new Rect(0f, height - edge, width, edge), danger);
            FillRect(new Rect(0f, edge, edge, height - edge * 2f), danger);
            FillRect(new Rect(width - edge, edge, edge, height - edge * 2f), danger);

            Rect safe = ResponsiveGui.SafeArea;
            if (stage == BossApproachStage.Arrived)
            {
                float bannerWidth = Mathf.Min(520f, safe.width - 32f);
                Rect banner = new Rect(
                    safe.x + (safe.width - bannerWidth) * 0.5f,
                    safe.y + safe.height * 0.43f,
                    bannerWidth,
                    92f);
                FillRect(banner, new Color(0.05f, 0.012f, 0.012f, 0.90f));
                FillRect(new Rect(banner.x, banner.y, banner.width, 3f),
                    new Color(0.95f, 0.15f, 0.08f, 0.85f));
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(banner.x + 16f, banner.y + 9f, banner.width - 32f, 40f),
                    gameFlow.bossStats != null
                        ? $"{gameFlow.bossStats.displayName}已至"
                        : "终局强敌已至",
                    bossWarningStyle,
                    15);
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(banner.x + 16f, banner.y + 49f, banner.width - 32f, 28f),
                    "胜此战后即入决战",
                    timerStyle,
                    11);
                return;
            }

            int seconds = Mathf.Max(1, Mathf.CeilToInt(gameFlow.mainTimeRemaining));
            float size = ResponsiveGui.IsPortrait ? 126f : 112f;
            Rect countdown = new Rect(
                safe.x + (safe.width - size) * 0.5f,
                safe.y + safe.height * (ResponsiveGui.IsPortrait ? 0.44f : 0.42f),
                size,
                size);
            FillRect(countdown, new Color(0.055f, 0.012f, 0.012f, 0.82f));
            FillRect(new Rect(countdown.x, countdown.y, countdown.width, 3f),
                new Color(0.96f, 0.16f, 0.08f, 0.76f + pulse * 0.24f));
            GUI.Label(countdown, seconds.ToString(), bossCountdownStyle);
        }

        private void DrawFighter(Rect rect, Color color, string mark, bool facesLeft, Sprite[] frames,
            bool attacking, float actionProgress, Color? spriteTint = null)
        {
            FillRect(new Rect(rect.x + rect.width * 0.14f, rect.yMax + 4f, rect.width * 0.72f, 8f), new Color(0f, 0f, 0f, 0.42f));

            Sprite frame = GetFrame(frames, attacking, actionProgress);
            if (frame != null)
            {
                Color spriteColorPrevious = GUI.color;
                GUI.color = spriteTint ?? Color.white;
                DrawSprite(rect, frame, facesLeft);
                GUI.color = spriteColorPrevious;
                return;
            }

            Color textureColorPrevious = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, actorTexture != null ? actorTexture : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = textureColorPrevious;
            GUI.Label(rect, mark, actorMarkStyle);
        }

        private static Sprite GetFrame(Sprite[] frames, bool attacking, float actionProgress)
        {
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            int index = attacking
                ? Mathf.Min(Mathf.FloorToInt(actionProgress * frames.Length), frames.Length - 1)
                : Mathf.FloorToInt(Time.unscaledTime * 10f) % frames.Length;
            return frames[index];
        }

        private EnemyVisualProfile SelectEnemyVisualProfile()
        {
            string visualId = battleManager.currentEnemy.visualId;
            return FindEnemyVisualProfile(visualId);
        }

        private EnemyVisualProfile FindEnemyVisualProfile(string visualId)
        {
            if (string.IsNullOrEmpty(visualId) || enemyVisualProfiles == null)
            {
                return null;
            }

            foreach (EnemyVisualProfile profile in enemyVisualProfiles)
            {
                if (profile != null && profile.id == visualId)
                {
                    return profile;
                }
            }

            return null;
        }

        public Sprite GetPreviewSprite(string visualId)
        {
            if (string.IsNullOrEmpty(visualId) || enemyVisualProfiles == null)
            {
                return null;
            }

            foreach (EnemyVisualProfile profile in enemyVisualProfiles)
            {
                if (profile == null || profile.id != visualId ||
                    profile.idleFrames == null || profile.idleFrames.Length == 0)
                {
                    continue;
                }

                return profile.idleFrames[0];
            }

            return null;
        }

        private Sprite[] SelectEnemyFrames(EnemyVisualProfile profile, bool attacking)
        {
            if (profile != null)
            {
                return attacking ? profile.attackFrames : profile.idleFrames;
            }

            string enemyName = battleManager.currentEnemy.displayName;
            if (enemyName.Contains("守洞"))
            {
                return attacking ? caveAttackFrames : caveIdleFrames;
            }

            if (enemyName.Contains("黑风"))
            {
                return attacking ? eliteAttackFrames : eliteIdleFrames;
            }

            return attacking ? enemyAttackFrames : enemyIdleFrames;
        }

        private static void DrawSprite(Rect rect, Sprite sprite, bool facesLeft)
        {
            // Use the full sliced frame instead of Unity's tight-mesh textureRect.
            // Tight bounds vary per animation frame and would be stretched to this
            // fixed actor rect, making the character visibly resize while attacking.
            Rect textureRect = sprite.rect;
            Rect visibleRect = sprite.textureRect;
            float bottomPaddingRatio = Mathf.Max(0f, visibleRect.yMin - textureRect.yMin) / textureRect.height;
            rect.y += rect.height * bottomPaddingRatio;
            Rect uv = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);
            if (facesLeft)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }

            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        }

        private void DrawEnemyHealthPanel(
            Rect rect,
            CombatantStats stats,
            float recentDamage,
            float damageStartedAt)
        {
            float hitAge = Time.unscaledTime - damageStartedAt;
            float flash = 1f - Mathf.Clamp01(hitAge / HealthFlashDuration);
            if (recentDamage > 0f && flash > 0f)
            {
                FillRect(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f),
                    new Color(1f, 0.16f, 0.08f, 0.72f * flash));
            }

            bool boss = gameFlow.CurrentPhase == GamePhase.BossBattle;
            bool compact = ResponsiveGui.IsPortrait || rect.height <= 66f;
            Color accent = boss ? new Color(0.92f, 0.50f, 0.18f) : EnemyColor;
            WuxiaUiTheme.DrawPanel(rect,
                new Color(0.035f, 0.025f, 0.025f, 0.92f), accent,
                boss ? WuxiaPanelKind.Boss : WuxiaPanelKind.Combat);

            Rect threatBadge = new Rect(rect.x + 8f, rect.y + (compact ? 4f : 5f),
                boss ? (compact ? 36f : 42f) : (compact ? 24f : 28f), compact ? 15f : 17f);
            FillRect(threatBadge, new Color(accent.r, accent.g, accent.b, 0.88f));
            ResponsiveGui.DrawSingleLineLabel(threatBadge, boss ? "强敌" : "敌", captionStyle, 8);

            float levelWidth = Mathf.Min(68f, rect.width * 0.24f);
            Rect displayNameRect = new Rect(threatBadge.xMax + 6f, rect.y + (compact ? 2f : 3f),
                rect.width - threatBadge.width - levelWidth - 28f, compact ? 19f : 21f);
            Rect levelRect = new Rect(rect.xMax - levelWidth - 8f, rect.y + (compact ? 2f : 3f),
                levelWidth, compact ? 19f : 21f);
            ResponsiveGui.DrawSingleLineLabel(displayNameRect, stats.displayName, leftNameStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(levelRect, $"等级 {stats.DisplayLevel}", rightNameStyle, 8);

            Rect bar = new Rect(rect.x + 9f, rect.y + (compact ? 23f : 27f),
                rect.width - 21f, compact ? 16f : 18f);
            FillRect(bar, Ink);
            float innerWidth = bar.width - 4f;
            float currentRatio = stats.HealthRatio;
            Color currentHealthColor = currentRatio <= 0.25f
                ? new Color(0.88f, 0.19f, 0.13f)
                : currentRatio <= 0.5f
                    ? new Color(0.92f, 0.62f, 0.16f)
                    : boss
                        ? new Color(0.76f, 0.12f, 0.16f)
                        : new Color(0.82f, 0.23f, 0.14f);
            FillRect(new Rect(bar.x + 2f, bar.y + 2f, innerWidth * currentRatio, bar.height - 4f),
                currentHealthColor);
            if (currentRatio > 0f)
            {
                FillRect(new Rect(bar.x + 2f, bar.y + 2f, innerWidth * currentRatio, 3f),
                    new Color(1f, 0.62f, 0.38f, 0.42f));
            }

            float damageAge = Time.unscaledTime - damageStartedAt;
            if (recentDamage > 0f && damageAge < DamageDisplayDuration && stats.maxHealth > 0f)
            {
                float beforeHitRatio = Mathf.Clamp01((stats.currentHealth + recentDamage) / stats.maxHealth);
                float lossWidth = innerWidth * Mathf.Max(0f, beforeHitRatio - currentRatio);
                float lossAlpha = 1f - Mathf.Clamp01(damageAge / DamageDisplayDuration);
                FillRect(new Rect(bar.x + 2f + innerWidth * currentRatio, bar.y + 2f, lossWidth, bar.height - 4f),
                    new Color(1f, 0.76f, 0.30f, 0.95f * lossAlpha));
            }

            for (int i = 1; i < 4; i++)
            {
                FillRect(new Rect(bar.x + 2f + innerWidth * i * 0.25f, bar.y + 2f, 1f, bar.height - 4f),
                    new Color(0.02f, 0.02f, 0.02f, 0.40f));
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(bar.x, bar.y - 1f, bar.width, bar.height + 2f),
                $"气血 {CombatNumberDisplay.Format(stats.currentHealth)} / {CombatNumberDisplay.Format(stats.maxHealth)}", centerStyle, 8);

            string statText = $"攻 {CombatNumberDisplay.Format(stats.attack)} · 防 {CombatNumberDisplay.Format(stats.defense)}";
            string effectText;
            if (boss && battleManager.BossWard > 0f)
            {
                effectText = $"妖甲 {CombatNumberDisplay.Format(battleManager.BossWard)} · 毒 {battleManager.EnemyPoisonStacks} · 破甲 {CombatNumberDisplay.Format(battleManager.EnemyArmorBreak)}";
            }
            else
            {
                effectText = battleManager.EnemyPoisonStacks > 0 || battleManager.EnemyArmorBreak > 0f
                    ? $"毒 {battleManager.EnemyPoisonStacks} · 破甲 {CombatNumberDisplay.Format(battleManager.EnemyArmorBreak)}"
                    : boss ? battleManager.CurrentBossPhaseName : "状态正常";
            }
            if (compact)
            {
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(rect.x + 10f, rect.y + 41f, rect.width - 20f, 15f),
                    effectText, detailStyle, 7);
                return;
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 10f, rect.y + 48f, rect.width * 0.45f, 17f),
                statText, detailStyle, 7);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + rect.width * 0.43f, rect.y + 48f, rect.width * 0.52f, 17f),
                effectText, detailStyle, 7);
        }

        private void DrawDuelFocus(float width, float top, float height)
        {
            bool portrait = ResponsiveGui.IsPortrait;
            float focusWidth = portrait
                ? Mathf.Min(210f, ResponsiveGui.SafeArea.width - 32f)
                : 132f;
            Rect focusRect = new Rect((width - focusWidth) * 0.5f, top + 3f,
                focusWidth, portrait ? 28f : Mathf.Max(36f, height - 6f));
            if (portrait)
            {
                WuxiaUiTheme.DrawCompactSurface(
                    focusRect, new Color(0.025f, 0.03f, 0.035f, 0.78f), Gold);
            }
            else
            {
                WuxiaUiTheme.DrawPanel(focusRect,
                    new Color(0.025f, 0.03f, 0.035f, 0.82f), Gold,
                    WuxiaPanelKind.Combat);
            }
            int exchange = Mathf.Max(1, battleManager.AttackSequence);
            if (portrait)
            {
                string state = battleManager.LastAttackWasCritical ? "暴击" :
                    battleManager.LastAttackWasDodged ? "闪避" : "演武";
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(focusRect.x + 6f, focusRect.y + 2f,
                        focusRect.width - 12f, focusRect.height - 4f),
                    $"第 {exchange} 招 · {battleManager.BattleElapsed:0.0} 秒 · {state}",
                    detailStyle, 8);
                return;
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(focusRect.x, focusRect.y + 4f, focusRect.width, 19f),
                $"第 {exchange} 招 · {battleManager.BattleElapsed:0.0} 秒", detailStyle, 8);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(focusRect.x, focusRect.y + 22f, focusRect.width, 15f),
                battleManager.LastAttackWasCritical ? "暴击交锋" :
                battleManager.LastAttackWasDodged ? "身法闪避" : "自动演武", captionStyle, 8);
        }

        private void DrawDamagePopup(Rect targetRect, float damage, float startedAt, bool critical, bool playerTarget)
        {
            float age = Time.unscaledTime - startedAt;
            if (damage <= 0f || age < 0f || age >= DamageDisplayDuration)
            {
                return;
            }

            float progress = age / DamageDisplayDuration;
            float alpha = 1f - Mathf.Clamp01((progress - 0.58f) / 0.42f);
            float rise = Mathf.Lerp(0f, 48f, progress);
            string displayedDamage = CombatNumberDisplay.Format(damage);
            string text = critical ? $"暴击  -{displayedDamage}" : $"受击  -{displayedDamage}";
            GUIStyle foreground = critical ? criticalDamageStyle : damageStyle;
            GUIStyle shadow = critical ? criticalDamageShadowStyle : damageShadowStyle;
            GUIContent content = new GUIContent(text);
            float minimumWidth = critical ? 240f : 190f;
            float maximumWidth = Mathf.Max(minimumWidth, ResponsiveGui.SafeArea.width - 12f);
            float measuredWidth = Mathf.Max(foreground.CalcSize(content).x, shadow.CalcSize(content).x) + 24f;
            float width = Mathf.Clamp(measuredWidth, minimumWidth, maximumWidth);
            float height = critical ? 52f : 44f;
            float popupX = Mathf.Clamp(
                targetRect.center.x - width * 0.5f,
                ResponsiveGui.SafeArea.x + 6f,
                ResponsiveGui.SafeArea.xMax - width - 6f);
            Rect popup = new Rect(popupX, targetRect.y - 16f - rise, width, height);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(popup.x + 3f, popup.y + 4f, popup.width, popup.height), text, shadow);
            GUI.color = playerTarget
                ? new Color(1f, 0.25f, 0.20f, alpha)
                : new Color(1f, 0.78f, 0.24f, alpha);
            GUI.Label(popup, text, foreground);
            GUI.color = previous;
        }

        private void DrawImpactMarker(Rect targetRect, float damage, float startedAt, bool critical, bool playerTarget)
        {
            float age = Time.unscaledTime - startedAt;
            if (damage <= 0f || age < 0f || age >= ImpactMarkerDuration)
            {
                return;
            }

            float progress = age / ImpactMarkerDuration;
            float alpha = 1f - progress;
            if (impactEffectFrames != null && impactEffectFrames.Length > 0)
            {
                int frameIndex = Mathf.Min(Mathf.FloorToInt(progress * impactEffectFrames.Length),
                    impactEffectFrames.Length - 1);
                float effectSize = targetRect.width * (critical ? 0.92f : 0.72f);
                Vector2 effectCenter = new Vector2(targetRect.center.x,
                    targetRect.y + targetRect.height * 0.43f);
                Rect effectRect = new Rect(effectCenter.x - effectSize * 0.5f,
                    effectCenter.y - effectSize * 0.5f, effectSize, effectSize);
                Color tint = playerTarget
                    ? new Color(1f, 0.48f, 0.38f, alpha)
                    : new Color(1f, 1f, 1f, alpha);
                DrawEffectSprite(effectRect, impactEffectFrames[frameIndex], tint);
                return;
            }

            float reach = Mathf.Lerp(12f, critical ? 42f : 30f, progress);
            float thickness = critical ? 6f : 4f;
            Vector2 center = new Vector2(targetRect.center.x, targetRect.y + targetRect.height * 0.42f);
            Color color = playerTarget
                ? new Color(1f, 0.18f, 0.12f, alpha)
                : new Color(1f, 0.86f, 0.42f, alpha);
            FillRect(new Rect(center.x - reach, center.y - thickness * 0.5f, reach * 2f, thickness), color);
            FillRect(new Rect(center.x - thickness * 0.5f, center.y - reach, thickness, reach * 2f), color);

            float coreSize = Mathf.Lerp(18f, 5f, progress);
            FillRect(new Rect(center.x - coreSize * 0.5f, center.y - coreSize * 0.5f, coreSize, coreSize),
                new Color(1f, 1f, 1f, alpha * 0.88f));
        }

        private void DrawPersistentBattleAuras(Rect playerRect, Rect enemyRect)
        {
            if (battleManager.EnemyPoisonStacks > 0 && poisonEffectFrames != null &&
                poisonEffectFrames.Length > 0)
            {
                int frameIndex = Mathf.FloorToInt(Time.unscaledTime * 7f) % poisonEffectFrames.Length;
                float stackRatio = Mathf.Clamp01(
                    battleManager.EnemyPoisonStacks /
                    (float)Mathf.Max(1, battleManager.EnemyPoisonMaxStacks));
                float pulse = 0.12f + stackRatio * 0.10f +
                              Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) * 0.08f;
                float size = enemyRect.width * Mathf.Lerp(1.02f, 1.18f, stackRatio);
                Rect auraRect = new Rect(enemyRect.center.x - size * 0.5f,
                    enemyRect.center.y - size * 0.5f, size, size);
                DrawEffectSprite(auraRect, poisonEffectFrames[frameIndex],
                    new Color(1f, 1f, 1f, pulse));
                DrawPoisonMotes(enemyRect, Time.unscaledTime, stackRatio, pulse * 1.7f);
            }

            if (battleManager.PlayerShield > 0f)
            {
                float pulse = 0.32f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.18f;
                float inset = playerRect.width * 0.06f;
                DrawOutline(new Rect(playerRect.x - inset, playerRect.y - inset,
                        playerRect.width + inset * 2f, playerRect.height + inset * 2f),
                    new Color(0.96f, 0.78f, 0.30f, pulse), 3f);
            }

            bool bloodAura = playerStats.runtimeStats.HealthRatio <= 0.5f &&
                             (playerStats.GetMartialArtRank("血战八方") > 0 ||
                              playerStats.GetMartialArtRank("修罗血域") > 0 ||
                              playerStats.GetSecretRank("血铸金身") > 0);
            if (bloodAura && impactEffectFrames != null && impactEffectFrames.Length > 0)
            {
                float pulse = 0.10f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 0.09f;
                float size = playerRect.width * 1.15f;
                DrawEffectSprite(new Rect(playerRect.center.x - size * 0.5f,
                        playerRect.center.y - size * 0.5f, size, size),
                    impactEffectFrames[0], new Color(0.90f, 0.12f, 0.08f, pulse));
            }
        }

        private void DrawBattleSkillVfx(Rect playerRect, Rect enemyRect, bool enemyFacesLeft)
        {
            bool debugPreview = Time.unscaledTime < debugPreviewVfxUntil;
            float age = debugPreview
                ? Mathf.Repeat(Time.unscaledTime, 0.48f)
                : Time.unscaledTime - attackStartedAt;
            if (age < 0f || age > 0.82f || DisplayVfxCues == BattleVfxCue.None)
            {
                return;
            }

            if (HasCue(BattleVfxCue.Dodge))
            {
                Rect target = battleManager.LastAttackWasPlayer ? enemyRect : playerRect;
                Sprite[] frames = battleManager.LastAttackWasPlayer ? SelectEnemyFrames(SelectEnemyVisualProfile(), false) : playerIdleFrames;
                Sprite ghost = GetFrame(frames, false, 0f);
                if (ghost != null)
                {
                    float alpha = 1f - Mathf.Clamp01(age / 0.42f);
                    Color previous = GUI.color;
                    GUI.color = HasCue(BattleVfxCue.ShadowDodge)
                        ? new Color(0.46f, 0.90f, 1f, alpha * 0.38f)
                        : new Color(0.82f, 0.90f, 0.94f, alpha * 0.24f);
                    float direction = battleManager.LastAttackWasPlayer ? 1f : -1f;
                    DrawSprite(OffsetRect(target, direction * target.width * 0.20f, 0f), ghost,
                        battleManager.LastAttackWasPlayer && enemyFacesLeft);
                    GUI.color = previous;
                }
            }

            if (HasCue(BattleVfxCue.SwordQi) && swordQiEffectFrames != null &&
                swordQiEffectFrames.Length > 0)
            {
                float progress = Mathf.Clamp01(age / 0.54f);
                Vector2 center = Vector2.Lerp(
                    new Vector2(playerRect.center.x, playerRect.center.y),
                    new Vector2(enemyRect.center.x, enemyRect.center.y),
                    Mathf.SmoothStep(0.08f, 0.92f, progress));
                float width = Mathf.Max(playerRect.width, enemyRect.width) * 1.18f;
                DrawEffectSprite(new Rect(center.x - width * 0.5f, center.y - width * 0.36f,
                        width, width * 0.72f),
                    EffectFrame(swordQiEffectFrames, progress),
                    new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((progress - 0.70f) / 0.30f)));
                DrawSlashTrail(enemyRect, progress,
                    new Color(0.58f, 0.90f, 0.94f, 0.80f), -18f, 1.16f);
            }

            if (HasCue(BattleVfxCue.SwiftCombo))
            {
                DrawBurst(enemyRect, swordQiEffectFrames, age, 0.62f,
                    new Color(0.74f, 0.94f, 1f, 0.90f), 1.12f, -0.18f);
                DrawBurst(enemyRect, swordQiEffectFrames, age - 0.08f, 0.54f,
                    new Color(1f, 0.86f, 0.38f, 0.78f), 0.90f, 0.16f);
                DrawSlashTrail(enemyRect, Mathf.Clamp01(age / 0.50f),
                    new Color(0.66f, 0.92f, 1f, 0.86f), -24f, 1.22f);
                DrawSlashTrail(enemyRect, Mathf.Clamp01((age - 0.07f) / 0.50f),
                    new Color(0.92f, 0.73f, 0.28f, 0.74f), 20f, 1.05f);
            }

            if (HasCue(BattleVfxCue.PoisonApplied) || HasCue(BattleVfxCue.PoisonTick))
            {
                float scale = HasCue(BattleVfxCue.PoisonMist) ? 1.28f : 0.92f;
                DrawBurst(enemyRect, poisonEffectFrames, age, 0.66f, Color.white, scale, 0f);
                float progress = Mathf.Clamp01(age / 0.66f);
                DrawPulseOutline(enemyRect, progress, Poison,
                    HasCue(BattleVfxCue.PoisonMist) ? 1.34f : 1.05f);
                DrawPoisonMotes(enemyRect, age * 2.4f,
                    HasCue(BattleVfxCue.PoisonMist) ? 1f : 0.58f, 0.84f * (1f - progress));
            }

            if (HasCue(BattleVfxCue.ArmorBreak))
            {
                DrawBurst(enemyRect, impactEffectFrames, age, 0.38f,
                    new Color(0.94f, 0.72f, 0.30f, 0.86f), 0.72f, 0.08f);
                DrawRadialShards(enemyRect, Mathf.Clamp01(age / 0.38f),
                    new Color(0.94f, 0.72f, 0.30f, 0.82f));
            }

            if (HasCue(BattleVfxCue.ShieldImpact))
            {
                DrawBurst(playerRect, impactEffectFrames, age, 0.46f,
                    new Color(0.98f, 0.82f, 0.34f, 0.82f), 1.06f, 0f);
                DrawPulseOutline(playerRect, Mathf.Clamp01(age / 0.46f),
                    new Color(0.98f, 0.82f, 0.34f, 0.90f), 1.16f);
            }

            if (HasCue(BattleVfxCue.Retaliation))
            {
                DrawBurst(enemyRect, impactEffectFrames, age, 0.44f,
                    new Color(0.82f, 0.92f, 1f, 0.88f), 0.82f, -0.12f);
                DrawSlashTrail(enemyRect, Mathf.Clamp01(age / 0.44f),
                    new Color(0.68f, 0.88f, 0.94f, 0.82f), 34f, 0.94f);
            }

            if (HasCue(BattleVfxCue.Heal))
            {
                DrawBurst(playerRect, impactEffectFrames, age, 0.58f,
                    new Color(0.38f, 1f, 0.64f, 0.72f), 0.62f, -0.22f);
                DrawRisingMotes(playerRect, Mathf.Clamp01(age / 0.58f),
                    new Color(0.38f, 0.82f, 0.62f, 0.82f));
            }

            if (HasCue(BattleVfxCue.OpeningStrike))
            {
                DrawBurst(enemyRect, impactEffectFrames, age, 0.42f,
                    new Color(1f, 0.88f, 0.42f, 0.92f), 1.00f, -0.18f);
                DrawSlashTrail(enemyRect, Mathf.Clamp01(age / 0.42f),
                    new Color(1f, 0.88f, 0.42f, 0.94f), -42f, 1.30f);
            }

            if (HasCue(BattleVfxCue.BloodPower) || HasCue(BattleVfxCue.BloodBurst))
            {
                DrawBurst(enemyRect, impactEffectFrames, age, 0.52f,
                    new Color(1f, 0.16f, 0.10f, 0.84f),
                    HasCue(BattleVfxCue.BloodBurst) ? 1.12f : 0.78f, 0f);
                DrawPulseOutline(enemyRect, Mathf.Clamp01(age / 0.52f),
                    new Color(0.82f, 0.12f, 0.08f, 0.82f),
                    HasCue(BattleVfxCue.BloodBurst) ? 1.28f : 0.94f);
            }

            if (HasCue(BattleVfxCue.Foxfire))
            {
                float progress = Mathf.Clamp01(age / 0.62f);
                for (int index = 0; index < 3; index++)
                {
                    float staggered = Mathf.Clamp01(progress * 1.35f - index * 0.12f);
                    Vector2 center = Vector2.Lerp(enemyRect.center, playerRect.center,
                        Mathf.SmoothStep(0f, 1f, staggered));
                    float size = playerRect.width * (0.34f + index * 0.04f);
                    DrawEffectSprite(new Rect(center.x - size * 0.5f,
                            center.y - size * 0.5f + (index - 1) * size * 0.34f, size, size),
                        EffectFrame(impactEffectFrames, staggered),
                        new Color(1f, 0.34f, 0.12f, 1f - staggered * 0.62f));
                }
            }
        }

        private void DrawSkillCallout(Rect playerRect, Rect enemyRect)
        {
            if (string.IsNullOrEmpty(activeSkillVfxName) ||
                (activeVfxCues & BattleVfxCue.Foxfire) != 0)
            {
                return;
            }

            float age = Time.unscaledTime - attackStartedAt;
            if (age < 0f || age >= SkillCalloutDuration)
            {
                return;
            }

            float fadeIn = Mathf.Clamp01(age / 0.10f);
            float fadeOut = 1f - Mathf.Clamp01((age - 0.48f) / 0.24f);
            float alpha = Mathf.Min(fadeIn, fadeOut);
            bool playerSkill = battleManager.LastAttackWasPlayer ||
                               (activeVfxCues & (BattleVfxCue.Retaliation |
                                                  BattleVfxCue.ShadowDodge |
                                                  BattleVfxCue.Heal)) != 0;
            Rect anchor = playerSkill ? playerRect : enemyRect;
            float width = Mathf.Clamp(anchor.width * 1.04f, 118f, 172f);
            float slide = Mathf.Lerp(8f, 0f, Mathf.SmoothStep(0f, 1f, fadeIn));
            Rect panel = new Rect(
                Mathf.Clamp(anchor.center.x - width * 0.5f,
                    ResponsiveGui.SafeArea.x + 6f,
                    ResponsiveGui.SafeArea.xMax - width - 6f),
                anchor.y - 42f - slide,
                width,
                36f);
            Color accent = GetSkillAccent(activeVfxCues);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            WuxiaUiTheme.DrawCompactSurface(panel,
                new Color(accent.r * 0.10f, accent.g * 0.10f, accent.b * 0.10f, 0.94f),
                accent);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 5f, panel.y + 1f, panel.width - 10f, 12f),
                "武学触发", skillCalloutCaptionStyle, 7);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 5f, panel.y + 12f, panel.width - 10f, 22f),
                activeSkillVfxName, skillCalloutStyle, 9);
            GUI.color = previous;
        }

        private void DrawPoisonPopup(Rect enemyRect)
        {
            bool poisonApplied = (activeVfxCues & BattleVfxCue.PoisonApplied) != 0;
            bool poisonTick = (activeVfxCues & BattleVfxCue.PoisonTick) != 0;
            if ((!poisonApplied && !poisonTick) || activePoisonStacks <= 0)
            {
                return;
            }

            float age = Time.unscaledTime - attackStartedAt;
            if (age < 0f || age >= PoisonPopupDuration)
            {
                return;
            }

            float progress = age / PoisonPopupDuration;
            float alpha = 1f - Mathf.Clamp01((progress - 0.58f) / 0.42f);
            float rise = Mathf.Lerp(0f, 52f, Mathf.SmoothStep(0f, 1f, progress));
            Rect popup = new Rect(enemyRect.center.x - 92f,
                enemyRect.y + enemyRect.height * 0.32f - rise, 184f, 58f);
            string value;
            string caption;
            if (poisonTick)
            {
                value = $"-{CombatNumberDisplay.Format(activePoisonDamage)}";
                caption = $"毒发  ×{activePoisonStacks}";
            }
            else if (activePoisonStackDelta > 0)
            {
                value = $"+{activePoisonStackDelta}";
                caption = $"毒层  ×{activePoisonStacks}";
            }
            else
            {
                value = $"×{activePoisonStacks}";
                caption = "毒层已满";
            }

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(popup.x + 3f, popup.y + 4f, popup.width, 40f),
                value, poisonValueShadowStyle);
            GUI.color = new Color(0.54f, 0.88f, 0.38f, alpha);
            GUI.Label(new Rect(popup.x, popup.y, popup.width, 40f), value, poisonValueStyle);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(popup.x, popup.y + 34f, popup.width, 20f),
                caption, poisonCaptionStyle);
            GUI.color = previous;
        }

        private void DrawPoisonStatusBadge(Rect enemyRect)
        {
            if (battleManager.EnemyPoisonStacks <= 0)
            {
                return;
            }

            int stacks = battleManager.EnemyPoisonStacks;
            int maxStacks = Mathf.Max(1, battleManager.EnemyPoisonMaxStacks);
            float ratio = Mathf.Clamp01(stacks / (float)maxStacks);
            float pulse = 0.82f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f)) * 0.18f;
            float width = 78f;
            Rect badge = new Rect(enemyRect.xMax - width * 0.82f,
                enemyRect.y + enemyRect.height * 0.64f, width, 31f);
            FillRect(badge, PoisonDark);
            DrawOutline(badge, new Color(Poison.r, Poison.g, Poison.b, 0.72f * pulse), 2f);
            FillRect(new Rect(badge.x + 4f, badge.yMax - 5f, badge.width - 8f, 2f),
                new Color(0.03f, 0.055f, 0.03f, 0.95f));
            FillRect(new Rect(badge.x + 4f, badge.yMax - 5f, (badge.width - 8f) * ratio, 2f),
                new Color(Poison.r, Poison.g, Poison.b, 0.92f));
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(badge.x + 4f, badge.y + 1f, badge.width - 8f, 23f),
                $"毒  ×{stacks}", poisonCaptionStyle, 9);
        }

        private static Color GetSkillAccent(BattleVfxCue cues)
        {
            if ((cues & (BattleVfxCue.PoisonApplied | BattleVfxCue.PoisonTick |
                         BattleVfxCue.PoisonMist)) != 0)
            {
                return Poison;
            }

            if ((cues & (BattleVfxCue.BloodPower | BattleVfxCue.BloodBurst)) != 0)
            {
                return new Color(0.80f, 0.18f, 0.12f, 1f);
            }

            if ((cues & (BattleVfxCue.SwordQi | BattleVfxCue.SwiftCombo |
                         BattleVfxCue.ShadowDodge)) != 0)
            {
                return new Color(0.44f, 0.74f, 0.78f, 1f);
            }

            return Gold;
        }

        private static void DrawSlashTrail(Rect target, float progress, Color color,
            float angle, float scale)
        {
            if (progress < 0f || progress > 1f)
            {
                return;
            }

            float alpha = Mathf.Sin(progress * Mathf.PI) * color.a;
            float width = target.width * scale * Mathf.Lerp(0.58f, 1f, progress);
            float thickness = Mathf.Lerp(8f, 2f, progress);
            Vector2 center = target.center + new Vector2(0f,
                target.height * Mathf.Lerp(0.18f, -0.12f, progress));
            DrawRotatedRect(new Rect(center.x - width * 0.5f,
                    center.y - thickness * 0.5f, width, thickness),
                new Color(color.r, color.g, color.b, alpha), angle);
            DrawRotatedRect(new Rect(center.x - width * 0.38f,
                    center.y - 1f, width * 0.76f, 2f),
                new Color(1f, 0.96f, 0.78f, alpha * 0.72f), angle);
        }

        private static void DrawPulseOutline(Rect target, float progress, Color color, float scale)
        {
            if (progress < 0f || progress > 1f)
            {
                return;
            }

            float currentScale = Mathf.Lerp(0.62f, scale, Mathf.SmoothStep(0f, 1f, progress));
            float width = target.width * currentScale;
            float height = target.height * currentScale;
            Rect outline = new Rect(target.center.x - width * 0.5f,
                target.center.y - height * 0.5f, width, height);
            Color pulseColor = new Color(
                color.r, color.g, color.b, color.a * (1f - progress));
            float thickness = Mathf.Lerp(4f, 1f, progress);
            float arm = Mathf.Min(outline.width, outline.height) * 0.18f;
            FillRect(new Rect(outline.x, outline.y, arm, thickness), pulseColor);
            FillRect(new Rect(outline.x, outline.y, thickness, arm), pulseColor);
            FillRect(new Rect(outline.xMax - arm, outline.y, arm, thickness), pulseColor);
            FillRect(new Rect(outline.xMax - thickness, outline.y, thickness, arm), pulseColor);
            FillRect(new Rect(outline.x, outline.yMax - thickness, arm, thickness), pulseColor);
            FillRect(new Rect(outline.x, outline.yMax - arm, thickness, arm), pulseColor);
            FillRect(new Rect(outline.xMax - arm, outline.yMax - thickness, arm, thickness), pulseColor);
            FillRect(new Rect(outline.xMax - thickness, outline.yMax - arm, thickness, arm), pulseColor);
        }

        private static void DrawRadialShards(Rect target, float progress, Color color)
        {
            float alpha = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * color.a;
            for (int index = 0; index < 7; index++)
            {
                float angle = index * (360f / 7f) + 12f;
                float radians = angle * Mathf.Deg2Rad;
                float radius = target.width * Mathf.Lerp(0.12f, 0.54f, progress);
                Vector2 center = target.center + new Vector2(
                    Mathf.Cos(radians) * radius,
                    Mathf.Sin(radians) * radius);
                DrawRotatedRect(new Rect(center.x - 8f, center.y - 2f, 16f, 4f),
                    new Color(color.r, color.g, color.b, alpha), angle);
            }
        }

        private static void DrawRisingMotes(Rect target, float progress, Color color)
        {
            float alpha = (1f - progress) * color.a;
            for (int index = 0; index < 6; index++)
            {
                float phase = index * 1.83f;
                float x = target.center.x + Mathf.Sin(phase + progress * 4f) *
                    target.width * (0.10f + index * 0.035f);
                float y = target.yMax - target.height * (0.18f + progress * 0.74f) +
                          Mathf.Cos(phase) * 8f;
                float size = 3f + index % 3;
                FillRect(new Rect(x - size * 0.5f, y - size * 0.5f, size, size),
                    new Color(color.r, color.g, color.b, alpha));
            }
        }

        private static void DrawPoisonMotes(Rect target, float clock, float intensity, float alpha)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(3f + intensity * 5f), 3, 8);
            for (int index = 0; index < count; index++)
            {
                float phase = index * 2.17f + clock * (0.72f + index * 0.025f);
                float radius = target.width * (0.26f + index % 3 * 0.08f);
                float x = target.center.x + Mathf.Sin(phase) * radius;
                float y = target.center.y + Mathf.Cos(phase * 0.83f) * target.height * 0.34f -
                          Mathf.Repeat(clock * 9f + index * 7f, target.height * 0.20f);
                float size = 3f + index % 3 * 1.5f;
                FillRect(new Rect(x - size * 0.5f, y - size * 0.5f, size, size),
                    new Color(0.47f, 0.82f, 0.30f, alpha * (0.58f + index % 2 * 0.22f)));
            }
        }

        private static void DrawRotatedRect(Rect rect, Color color, float angle)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, rect.center);
            FillRect(rect, color);
            GUI.matrix = previousMatrix;
        }

        private bool HasCue(BattleVfxCue cue)
        {
            return (DisplayVfxCues & cue) != 0;
        }

        private BattleVfxCue DisplayVfxCues => Time.unscaledTime < debugPreviewVfxUntil
            ? debugPreviewVfxCues
            : activeVfxCues;

        public void DebugPreviewVfx(BattleVfxCue cues, float duration = 3f)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugPreviewVfxCues = cues;
            debugPreviewVfxUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
#endif
        }

        private static Sprite EffectFrame(Sprite[] frames, float progress)
        {
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            int index = Mathf.Min(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length),
                frames.Length - 1);
            return frames[index];
        }

        private static void DrawBurst(Rect target, Sprite[] frames, float age, float duration,
            Color tint, float scale, float verticalOffsetRatio)
        {
            if (age < 0f || age >= duration || frames == null || frames.Length == 0)
            {
                return;
            }

            float progress = age / duration;
            float size = target.width * scale;
            Rect rect = new Rect(target.center.x - size * 0.5f,
                target.center.y - size * 0.5f + target.height * verticalOffsetRatio, size, size);
            DrawEffectSprite(rect, EffectFrame(frames, progress),
                new Color(tint.r, tint.g, tint.b, tint.a * (1f - progress * 0.72f)));
        }

        private static Rect OffsetRect(Rect rect, float x, float y)
        {
            rect.x += x;
            rect.y += y;
            return rect;
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.y + thickness, thickness, rect.height - thickness * 2f), color);
            FillRect(new Rect(rect.xMax - thickness, rect.y + thickness, thickness, rect.height - thickness * 2f), color);
        }

        private static void DrawEffectSprite(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null)
            {
                return;
            }

            Rect source = sprite.rect;
            Rect uv = new Rect(
                source.x / sprite.texture.width,
                source.y / sprite.texture.height,
                source.width / sprite.texture.width,
                source.height / sprite.texture.height);
            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.color = previous;
        }

        private void DrawPlayerHitOverlay(float width, float height)
        {
            float age = Time.unscaledTime - playerDamageStartedAt;
            if (playerDamageAmount <= 0f || age < 0f || age >= ImpactMarkerDuration)
            {
                return;
            }

            float alpha = (1f - age / ImpactMarkerDuration) * (playerDamageWasCritical ? 0.58f : 0.34f);
            float edge = Mathf.Clamp(Mathf.Min(width, height) * 0.035f, 8f, 18f);
            Color red = new Color(0.9f, 0.05f, 0.025f, alpha);
            FillRect(new Rect(0f, 0f, width, edge), red);
            FillRect(new Rect(0f, height - edge, width, edge), red);
            FillRect(new Rect(0f, edge, edge, height - edge * 2f), red);
            FillRect(new Rect(width - edge, edge, edge, height - edge * 2f), red);
        }

        private void DrawCombatMessage(Rect stageRect, Rect messageRect, float actionProgress)
        {
            bool portrait = ResponsiveGui.IsPortrait;
            if (portrait)
            {
                WuxiaUiTheme.DrawCompactSurface(
                    messageRect, new Color(0.015f, 0.02f, 0.025f, 0.82f), Gold);
            }
            else
            {
                WuxiaUiTheme.DrawPanel(messageRect,
                    new Color(0.015f, 0.02f, 0.025f, 0.84f), Gold,
                    WuxiaPanelKind.Combat);
            }

            string effectSummary =
                battleManager.IsBossBattle
                    ? $"侠盾 {CombatNumberDisplay.Format(battleManager.PlayerShield)} · 妖甲 {CombatNumberDisplay.Format(battleManager.BossWard)} · 毒 {battleManager.EnemyPoisonStacks} · 破甲 {CombatNumberDisplay.Format(battleManager.EnemyArmorBreak)}"
                    : $"护盾 {CombatNumberDisplay.Format(battleManager.PlayerShield)}  ·  毒 {battleManager.EnemyPoisonStacks} 层  ·  破甲 {CombatNumberDisplay.Format(battleManager.EnemyArmorBreak)}";
            if (portrait)
            {
                Rect logRect = new Rect(messageRect.x + 10f, messageRect.y + 3f,
                    messageRect.width - 20f, 27f);
                Rect effectRect = new Rect(messageRect.x + 10f, messageRect.y + 31f,
                    messageRect.width - 20f, 18f);
                ResponsiveGui.DrawSingleLineLabel(logRect, battleManager.battleLog, centerStyle, 8);
                FillRect(new Rect(effectRect.x, effectRect.y - 2f, effectRect.width, 1f),
                    new Color(1f, 1f, 1f, 0.12f));
                ResponsiveGui.DrawSingleLineLabel(effectRect, effectSummary, detailStyle, 8);
            }
            else
            {
                const float labelWidth = 48f;
                float effectWidth = Mathf.Min(232f, messageRect.width * 0.30f);
                Rect labelRect = new Rect(messageRect.x + 8f, messageRect.y + 4f,
                    labelWidth, messageRect.height - 8f);
                Rect effectRect = new Rect(messageRect.xMax - effectWidth - 8f, messageRect.y + 4f,
                    effectWidth, messageRect.height - 8f);
                Rect logRect = new Rect(labelRect.xMax + 4f, messageRect.y + 4f,
                    effectRect.x - labelRect.xMax - 8f, messageRect.height - 8f);
                ResponsiveGui.DrawSingleLineLabel(labelRect, "战况", captionStyle, 8);
                ResponsiveGui.DrawSingleLineLabel(logRect, battleManager.battleLog, centerStyle, 8);
                FillRect(new Rect(effectRect.x - 5f, messageRect.y + 9f, 1f, messageRect.height - 18f),
                    new Color(1f, 1f, 1f, 0.13f));
                ResponsiveGui.DrawSingleLineLabel(effectRect, effectSummary, detailStyle, 8);
            }

            if (battleManager.AttackSequence > 0 && actionProgress < 1f && battleManager.LastAttackWasDodged)
            {
                float targetX = battleManager.LastAttackWasPlayer ? stageRect.width * 0.70f : stageRect.width * 0.16f;
                float rise = actionProgress * 34f;
                GUI.Label(new Rect(targetX, stageRect.y + stageRect.height * 0.26f - rise, stageRect.width * 0.14f, 42f), "闪避", damageStyle);
            }

            if (playerStats.runtimeStats.IsDead || battleManager.currentEnemy.IsDead)
            {
                string outcome = playerStats.runtimeStats.IsDead ? "战败" : "胜利";
                GUI.Label(new Rect(0f, stageRect.y + stageRect.height * 0.35f, stageRect.width, 42f), outcome, titleStyle);
            }
        }

        private static string GetMainTimeStateText(float ratio)
        {
            if (ratio <= 0f)
            {
                return "香尽 · 强敌已至";
            }

            if (ratio <= 1f / 3f)
            {
                return "丧钟已鸣 · 立即决断";
            }

            if (ratio <= 2f / 3f)
            {
                return "时间过半 · 争分夺秒";
            }

            return "六十息倒数 · 分秒必争";
        }

        private void DrawBossSkillStrip(Rect rect)
        {
            EnsureBossSkillIcons();
            WuxiaUiTheme.DrawCompactSurface(rect,
                new Color(0.025f, 0.018f, 0.022f, 0.88f),
                battleManager.CurrentBossPhase == BossBattlePhase.BloodFrenzy
                    ? new Color(0.84f, 0.22f, 0.16f)
                    : Gold);

            float gap = 3f;
            float slotWidth = (rect.width - gap * 4f) / 3f;
            DrawBossSkillSlot(new Rect(rect.x + gap, rect.y + 2f, slotWidth, rect.height - 4f),
                bossFoxfireIcon, "狐火", BossBattlePhase.Foxfire, true);
            DrawBossSkillSlot(new Rect(rect.x + gap * 2f + slotWidth, rect.y + 2f, slotWidth, rect.height - 4f),
                bossArmorIcon, "妖甲", BossBattlePhase.DemonArmor,
                (int)battleManager.CurrentBossPhase >= (int)BossBattlePhase.DemonArmor);
            DrawBossSkillSlot(new Rect(rect.x + gap * 3f + slotWidth * 2f, rect.y + 2f, slotWidth, rect.height - 4f),
                bossFrenzyIcon, "狂暴", BossBattlePhase.BloodFrenzy,
                (int)battleManager.CurrentBossPhase >= (int)BossBattlePhase.BloodFrenzy);
        }

        private void DrawBossSkillSlot(Rect rect, Texture2D icon, string label,
            BossBattlePhase phase, bool unlocked)
        {
            bool active = battleManager.CurrentBossPhase == phase;
            Color accent = phase == BossBattlePhase.BloodFrenzy
                ? new Color(0.90f, 0.27f, 0.18f)
                : phase == BossBattlePhase.DemonArmor
                    ? new Color(0.80f, 0.66f, 0.34f)
                    : new Color(0.78f, 0.32f, 0.24f);
            FillRect(rect, new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f,
                active ? 0.96f : 0.58f));
            if (active)
            {
                FillRect(new Rect(rect.x, rect.y, 2f, rect.height), accent);
            }

            float iconSize = Mathf.Min(rect.height - 4f, 28f);
            Rect iconRect = new Rect(rect.x + 3f, rect.center.y - iconSize * 0.5f, iconSize, iconSize);
            if (icon != null)
            {
                Color previous = GUI.color;
                GUI.color = unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 0.60f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }
            else
            {
                ResponsiveGui.DrawSingleLineLabel(iconRect, label.Substring(0, 1), captionStyle, 8);
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(iconRect.xMax + 2f, rect.y, rect.xMax - iconRect.xMax - 4f, rect.height),
                unlocked ? label : "未显", active ? detailStyle : captionStyle, 7);
        }

        private void DrawBossAura(Rect rect)
        {
            if (!battleManager.IsBossBattle)
            {
                return;
            }

            Color aura = battleManager.CurrentBossPhase switch
            {
                BossBattlePhase.BloodFrenzy => new Color(0.92f, 0.12f, 0.08f, 0.16f),
                BossBattlePhase.DemonArmor => new Color(0.86f, 0.62f, 0.20f, 0.13f),
                _ => new Color(0.72f, 0.20f, 0.16f, 0.09f)
            };
            float pulse = 0.7f + Mathf.Sin(Time.unscaledTime * 5f) * 0.3f;
            for (int i = 0; i < 3; i++)
            {
                float inset = 8f + i * 8f;
                FillRect(new Rect(rect.x - inset, rect.y - inset,
                    rect.width + inset * 2f, rect.height + inset * 2f),
                    new Color(aura.r, aura.g, aura.b, aura.a * pulse * (1f - i * 0.25f)));
            }
        }

        private Color GetBossSpriteTint()
        {
            if (!battleManager.IsBossBattle)
            {
                return Color.white;
            }

            return battleManager.CurrentBossPhase switch
            {
                BossBattlePhase.BloodFrenzy => new Color(1f, 0.66f, 0.62f, 1f),
                BossBattlePhase.DemonArmor => new Color(1f, 0.88f, 0.68f, 1f),
                _ => Color.white
            };
        }

        private Color GetEnemySpriteTint()
        {
            Color tint = GetBossSpriteTint();
            if (battleManager.EnemyPoisonStacks <= 0)
            {
                return tint;
            }

            float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.5f;
            Color poisonTint = Color.Lerp(
                new Color(0.66f, 1f, 0.54f, 1f),
                new Color(0.88f, 0.58f, 1f, 1f),
                pulse);
            return new Color(
                tint.r * poisonTint.r,
                tint.g * poisonTint.g,
                tint.b * poisonTint.b,
                tint.a);
        }

        private void DrawBossSkillBanner(Rect stageRect)
        {
            if (!battleManager.IsBossBattle || battleManager.LastBossSkill == BossSkillId.None)
            {
                return;
            }

            float age = Time.unscaledTime - battleManager.LastBossSkillTriggeredAt;
            if (age < 0f || age > 1.15f)
            {
                return;
            }

            EnsureBossSkillIcons();
            float alpha = age < 0.18f ? age / 0.18f : 1f - Mathf.Clamp01((age - 0.82f) / 0.33f);
            float width = Mathf.Min(300f, ResponsiveGui.SafeArea.width - 36f);
            Rect panel = new Rect(stageRect.center.x - width * 0.5f,
                stageRect.y + 8f, width, 52f);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            WuxiaUiTheme.DrawPanel(panel, new Color(0.04f, 0.018f, 0.02f, 0.94f),
                battleManager.LastBossSkill == BossSkillId.BloodFrenzy
                    ? new Color(0.92f, 0.24f, 0.16f)
                    : Gold,
                WuxiaPanelKind.Boss);
            Texture2D icon = GetBossSkillIcon(battleManager.LastBossSkill);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(panel.x + 8f, panel.y + 6f, 40f, 40f), icon,
                    ScaleMode.ScaleToFit, true);
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 54f, panel.y + 4f, panel.width - 62f, 24f),
                battleManager.LastBossSkillName, bossWarningStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 54f, panel.y + 27f, panel.width - 62f, 18f),
                battleManager.LastBossSkill == BossSkillId.FoxfireBarrage
                    ? "三道狐火 · 可闪避 · 防御逐段生效"
                    : battleManager.LastBossSkill == BossSkillId.DemonArmor
                        ? "固定妖甲 · 所有伤害均可击破"
                        : "攻势加快 · 狐火间隔缩短",
                captionStyle, 8);
            GUI.color = previous;
        }

        private string GetBossPhaseOrdinal()
        {
            return battleManager.CurrentBossPhase switch
            {
                BossBattlePhase.Foxfire => "第一相",
                BossBattlePhase.DemonArmor => "第二相",
                BossBattlePhase.BloodFrenzy => "第三相",
                _ => "决战"
            };
        }

        private void EnsureBossSkillIcons()
        {
            bossFoxfireIcon ??= Resources.Load<Texture2D>("Icons/boss_foxfire_barrage");
            bossArmorIcon ??= Resources.Load<Texture2D>("Icons/boss_demon_armor");
            bossFrenzyIcon ??= Resources.Load<Texture2D>("Icons/boss_blood_frenzy");
        }

        private Texture2D GetBossSkillIcon(BossSkillId skill)
        {
            return skill switch
            {
                BossSkillId.FoxfireBarrage => bossFoxfireIcon,
                BossSkillId.DemonArmor => bossArmorIcon,
                BossSkillId.BloodFrenzy => bossFrenzyIcon,
                _ => null
            };
        }

        private static void DrawMainTimeTrack(Rect rect, float ratio, bool paused)
        {
            TimePressureBarRenderer.Draw(rect, ratio, paused);
        }

        private static GUIStyle CreateStyle(int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            return RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                normal = { textColor = color }
            });
        }

        private static void ConfigureFloatingNumberStyle(GUIStyle style)
        {
            style.wordWrap = false;
            style.clipping = TextClipping.Overflow;
        }

        private static void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
