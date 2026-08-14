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
        private CombatantStats backgroundTrackedEnemy;
        private Texture2D activeBattleBackground;
        private int lastNormalBackgroundIndex = -1;
        private bool introBackgroundPrepared;

        private const float DamageDisplayDuration = 0.72f;
        private const float HealthFlashDuration = 0.34f;
        private const float ScreenShakeDuration = 0.18f;
        private const float ImpactMarkerDuration = 0.28f;

        private static readonly Color Backdrop = new Color(0.055f, 0.075f, 0.09f, 1f);
        private static readonly Color DistantMountain = new Color(0.11f, 0.19f, 0.20f, 1f);
        private static readonly Color Ground = new Color(0.18f, 0.16f, 0.13f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.075f, 1f);
        private static readonly Color PlayerColor = new Color(0.18f, 0.68f, 0.88f, 1f);
        private static readonly Color EnemyColor = new Color(0.82f, 0.22f, 0.17f, 1f);
        private static readonly Color Gold = new Color(0.82f, 0.66f, 0.32f, 1f);
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
            float attackDuration = 0.32f / battleManager.BattleSpeedMultiplier;
            float actionProgress = Mathf.Clamp01((Time.unscaledTime - attackStartedAt) / attackDuration);
            float lunge = Mathf.Sin(actionProgress * Mathf.PI) * Mathf.Min(54f, width * 0.05f);
            float shake = actionProgress > 0.38f && actionProgress < 0.78f
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
            DrawFighter(playerRect, PlayerColor, "侠", false,
                playerAttacking ? playerAttackFrames : playerIdleFrames, playerAttacking, actionProgress);
            DrawFighter(enemyRect, EnemyColor, "敌", enemyVisual != null ? enemyVisual.flipHorizontally : true,
                enemyAttacking ? currentEnemyAttackFrames : currentEnemyIdleFrames, enemyAttacking, actionProgress);
            DrawImpactMarker(playerRect, playerDamageAmount, playerDamageStartedAt, playerDamageWasCritical, true);
            DrawImpactMarker(enemyRect, enemyDamageAmount, enemyDamageStartedAt, enemyDamageWasCritical, false);
            DrawDamagePopup(playerRect, playerDamageAmount, playerDamageStartedAt, playerDamageWasCritical, true);
            DrawDamagePopup(enemyRect, enemyDamageAmount, enemyDamageStartedAt, enemyDamageWasCritical, false);
            DrawCombatMessage(stageRect, messageRect, actionProgress);
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
                return;
            }

            if (observedAttackSequence == battleManager.AttackSequence)
            {
                return;
            }

            observedAttackSequence = battleManager.AttackSequence;
            attackStartedAt = Time.unscaledTime;
            hitFeedbackStartedAt = Time.unscaledTime;
            hitFeedbackWasCritical = battleManager.LastAttackWasCritical;
            hitFeedbackWasDodged = battleManager.LastAttackWasDodged;
            hitFeedbackTargetedPlayer = !battleManager.LastAttackWasPlayer;
        }

        private Vector2 CalculateScreenShake()
        {
            float age = Time.unscaledTime - hitFeedbackStartedAt;
            if (age < 0f || age >= ScreenShakeDuration)
            {
                return Vector2.zero;
            }

            float strength = hitFeedbackWasDodged ? 1.6f : hitFeedbackWasCritical ? 11f : 5.5f;
            if (hitFeedbackTargetedPlayer)
            {
                strength *= 1.12f;
            }

            float envelope = 1f - age / ScreenShakeDuration;
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
                        "主时间停止 · 自动交锋", captionStyle, 8);
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
                    "主时间停止 · 自动交锋", captionStyle, 8);
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

        private void DrawFighter(Rect rect, Color color, string mark, bool facesLeft, Sprite[] frames, bool attacking, float actionProgress)
        {
            FillRect(new Rect(rect.x + rect.width * 0.14f, rect.yMax + 4f, rect.width * 0.72f, 8f), new Color(0f, 0f, 0f, 0.42f));

            Sprite frame = GetFrame(frames, attacking, actionProgress);
            if (frame != null)
            {
                DrawSprite(rect, frame, facesLeft);
                return;
            }

            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, actorTexture != null ? actorTexture : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
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
                $"气血 {stats.currentHealth:0} / {stats.maxHealth:0}", centerStyle, 8);

            string statText = $"攻 {stats.attack:0} · 防 {stats.defense:0.#}";
            string effectText = battleManager.EnemyPoisonStacks > 0 || battleManager.EnemyArmorBreak > 0f
                ? $"毒 {battleManager.EnemyPoisonStacks} · 破甲 {battleManager.EnemyArmorBreak:0.0}"
                : "状态正常";
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
            float width = critical ? 240f : 190f;
            float height = critical ? 52f : 44f;
            Rect popup = new Rect(targetRect.center.x - width * 0.5f, targetRect.y - 16f - rise, width, height);
            string text = critical ? $"暴击  -{damage:0}" : $"受击  -{damage:0}";
            GUIStyle foreground = critical ? criticalDamageStyle : damageStyle;
            GUIStyle shadow = critical ? criticalDamageShadowStyle : damageShadowStyle;

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
                $"护盾 {battleManager.PlayerShield:0}  ·  毒 {battleManager.EnemyPoisonStacks} 层  ·  破甲 {battleManager.EnemyArmorBreak:0.0}";
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

        private static void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
