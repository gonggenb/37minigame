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
        private GUIStyle duelStyle;
        private GUIStyle actorMarkStyle;
        private GUIStyle damageStyle;
        private GUIStyle damageShadowStyle;
        private GUIStyle criticalDamageStyle;
        private GUIStyle criticalDamageShadowStyle;
        private GUIStyle bossWarningStyle;
        private GUIStyle bossCountdownStyle;
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
        private static readonly Color HealthColor = new Color(0.24f, 0.78f, 0.40f, 1f);
        private static readonly Color Gold = new Color(0.82f, 0.66f, 0.32f, 1f);
        private static readonly Color Panel = new Color(0.025f, 0.035f, 0.045f, 0.78f);

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            if (battleManager == null || playerStats == null || gameFlow == null ||
                !battleManager.IsBattleActive || battleManager.currentEnemy == null || playerStats.runtimeStats == null)
            {
                return;
            }

            GUI.depth = -1000;
            EnsureStyles();
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
            float healthTop = portrait ? safe.y + 104f : Mathf.Clamp(height * 0.145f, 98f, 108f);
            float healthHeight = portrait ? 92f : Mathf.Clamp(height * 0.135f, 88f, 98f);
            float healthWidth = portrait
                ? (safe.width - 44f) * 0.5f
                : Mathf.Min(400f, width * 0.34f);
            Rect playerHealthRect = new Rect(sidePadding, healthTop, healthWidth, healthHeight);
            Rect enemyHealthRect = portrait
                ? new Rect(safe.xMax - 16f - healthWidth, healthTop, healthWidth, healthHeight)
                : new Rect(width - sidePadding - healthWidth, healthTop, healthWidth, healthHeight);
            DrawHealthPanel(playerHealthRect, playerStats.runtimeStats, playerDamageAmount, playerDamageStartedAt, true);
            DrawHealthPanel(enemyHealthRect, battleManager.currentEnemy, enemyDamageAmount, enemyDamageStartedAt, false);
            float duelTop = portrait ? healthTop + healthHeight + 8f : healthTop;
            float duelHeight = portrait ? 58f : healthHeight;
            DrawDuelFocus(width, duelTop, duelHeight);

            float messageHeight = portrait ? 112f : Mathf.Clamp(height * 0.105f, 68f, 76f);
            Rect messageRect = portrait
                ? new Rect(safe.x + 12f, safe.yMax - messageHeight - 12f, safe.width - 24f, messageHeight)
                : new Rect(width * 0.05f, height - messageHeight - 12f, width * 0.90f, messageHeight);
            float stageTop = portrait ? duelTop + duelHeight + 4f : healthTop + healthHeight + 2f;
            float stageBottom = messageRect.y - 2f;
            float stageHeight = Mathf.Max(80f, stageBottom - stageTop);
            Rect stageRect = new Rect(0f, stageTop, width, stageHeight);

            float baseActorSize = portrait
                ? Mathf.Clamp(Mathf.Min(width * 0.31f, stageHeight * 0.58f), 112f, 190f)
                : Mathf.Clamp(Mathf.Min(width * 0.25f, stageHeight * 0.94f), 120f, 290f);
            float actorSize = baseActorSize * battleActorScale;
            float baseY = stageBottom - 6f;
            float attackDuration = 0.32f / battleManager.BattleSpeedMultiplier;
            float actionProgress = Mathf.Clamp01((Time.unscaledTime - attackStartedAt) / attackDuration);
            float lunge = Mathf.Sin(actionProgress * Mathf.PI) * Mathf.Min(54f, width * 0.05f);
            float shake = actionProgress > 0.38f && actionProgress < 0.78f
                ? Mathf.Sin(actionProgress * 70f) * 7f
                : 0f;

            float playerX = width * (portrait ? 0.22f : 0.30f) - actorSize * 0.5f;
            float enemyX = width * (portrait ? 0.78f : 0.70f) - actorSize * 0.5f;
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
            duelStyle = CreateStyle(22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.52f));
            actorMarkStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            damageStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            damageShadowStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.9f));
            criticalDamageStyle = CreateStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            criticalDamageShadowStyle = CreateStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.95f));
            bossWarningStyle = CreateStyle(22, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.76f, 0.36f));
            bossCountdownStyle = CreateStyle(72, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.20f, 0.12f));
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
            BossApproachStage approachStage = gameFlow.CurrentBossApproachStage;
            float headerWidth = ResponsiveGui.IsPortrait
                ? Mathf.Max(260f, safe.width - 92f)
                : Mathf.Min(650f, width * 0.54f);
            float headerX = ResponsiveGui.IsPortrait
                ? safe.x + 14f
                : (width - headerWidth) * 0.5f;
            Rect headerRect = new Rect(headerX, safe.y + 7f, headerWidth, 82f);
            Color headerAccent = approachStage == BossApproachStage.FinalCountdown ||
                                 approachStage == BossApproachStage.Arrived
                ? new Color(0.94f, 0.18f, 0.11f)
                : approachStage == BossApproachStage.Imminent ||
                  approachStage == BossApproachStage.Omen
                    ? new Color(0.95f, 0.55f, 0.18f)
                    : Gold;
            FillRect(headerRect, new Color(0.02f, 0.03f, 0.04f, 0.70f));
            FillRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 2f), headerAccent);
            FillRect(new Rect(headerRect.x + headerRect.width * 0.18f, headerRect.yMax - 1f,
                headerRect.width * 0.64f, 1f),
                new Color(headerAccent.r, headerAccent.g, headerAccent.b, 0.55f));

            string title = gameFlow.CurrentPhase == GamePhase.BossBattle
                ? $"决战 · {gameFlow.bossStats.displayName}"
                : gameFlow.CurrentPhase == GamePhase.CaveRunning
                    ? "秘境 · 自动战斗"
                    : "遭遇 · 自动战斗";
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(headerRect.x, headerRect.y + 5f, headerRect.width, 34f),
                title, titleStyle, 14);

            string timerText;
            if (gameFlow.CurrentPhase == GamePhase.NormalBattleRunning)
            {
                switch (approachStage)
                {
                    case BossApproachStage.Arrived:
                        timerText = "妖姬已至 · 胜此战后即入决战";
                        break;
                    case BossApproachStage.FinalCountdown:
                        timerText = $"终局强敌将在 {Mathf.Max(1, Mathf.CeilToInt(gameFlow.mainTimeRemaining))} 息后降临";
                        break;
                    case BossApproachStage.Imminent:
                        timerText = $"妖气逼近 · 主地图余时 {gameFlow.mainTimeRemaining:0.0}s";
                        break;
                    case BossApproachStage.Omen:
                        timerText = $"强敌将至 · 主地图余时 {gameFlow.mainTimeRemaining:0.0}s";
                        break;
                    default:
                        timerText = $"主地图倒数持续流逝  {gameFlow.mainTimeRemaining:0.0}s";
                        break;
                }
            }
            else if (gameFlow.CurrentPhase == GamePhase.CaveRunning)
            {
                timerText = $"主地图倒数已暂停  {gameFlow.mainTimeRemaining:0.0}s";
            }
            else
            {
                timerText = $"Boss 独立战斗时间  {gameFlow.bossBattleTime:0.0}s";
            }

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(headerRect.x, headerRect.y + 39f, headerRect.width, 25f),
                timerText, timerStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(headerRect.x, headerRect.y + 62f, headerRect.width, 17f),
                "双方自动出招 · 战斗期间无需操作", captionStyle, 9);
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
                    "妖姬已至",
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

        private void DrawHealthPanel(Rect rect, CombatantStats stats, float recentDamage, float damageStartedAt,
            bool isPlayer)
        {
            float hitAge = Time.unscaledTime - damageStartedAt;
            float flash = 1f - Mathf.Clamp01(hitAge / HealthFlashDuration);
            if (recentDamage > 0f && flash > 0f)
            {
                FillRect(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f),
                    new Color(1f, 0.12f, 0.08f, 0.82f * flash));
            }

            Color accent = isPlayer ? PlayerColor : EnemyColor;
            FillRect(rect, Panel);
            FillRect(new Rect(isPlayer ? rect.x : rect.xMax - 5f, rect.y, 5f, rect.height), accent);
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(accent.r, accent.g, accent.b, 0.55f));

            Rect nameRect = new Rect(rect.x + 16f, rect.y + 5f, rect.width - 32f, 25f);
            float levelWidth = Mathf.Min(92f, nameRect.width * 0.30f);
            Rect displayNameRect = isPlayer
                ? new Rect(nameRect.x, nameRect.y, nameRect.width - levelWidth - 8f, nameRect.height)
                : new Rect(nameRect.x + levelWidth + 8f, nameRect.y,
                    nameRect.width - levelWidth - 8f, nameRect.height);
            Rect levelRect = isPlayer
                ? new Rect(nameRect.xMax - levelWidth, nameRect.y, levelWidth, nameRect.height)
                : new Rect(nameRect.x, nameRect.y, levelWidth, nameRect.height);
            ResponsiveGui.DrawSingleLineLabel(displayNameRect, stats.displayName,
                isPlayer ? leftNameStyle : rightNameStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(levelRect, $"境界 {stats.DisplayLevel}",
                isPlayer ? rightNameStyle : leftNameStyle, 9);

            Rect bar = new Rect(rect.x + 15f, rect.y + 36f, rect.width - 30f, 20f);
            FillRect(bar, Ink);
            float innerWidth = bar.width - 4f;
            float currentRatio = stats.HealthRatio;
            Color currentHealthColor = currentRatio <= 0.25f
                ? new Color(0.88f, 0.19f, 0.13f)
                : currentRatio <= 0.5f
                    ? new Color(0.92f, 0.62f, 0.16f)
                    : HealthColor;
            FillRect(new Rect(bar.x + 2f, bar.y + 2f, innerWidth * currentRatio, bar.height - 4f),
                currentHealthColor);
            float damageAge = Time.unscaledTime - damageStartedAt;
            if (recentDamage > 0f && damageAge < DamageDisplayDuration && stats.maxHealth > 0f)
            {
                float beforeHitRatio = Mathf.Clamp01((stats.currentHealth + recentDamage) / stats.maxHealth);
                float lossWidth = innerWidth * Mathf.Max(0f, beforeHitRatio - currentRatio);
                float lossAlpha = 1f - Mathf.Clamp01(damageAge / DamageDisplayDuration);
                FillRect(new Rect(bar.x + 2f + innerWidth * currentRatio, bar.y + 2f, lossWidth, bar.height - 4f),
                    new Color(1f, 0.16f, 0.10f, 0.95f * lossAlpha));
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(bar.x, bar.y - 1f, bar.width, bar.height + 2f),
                $"气血  {stats.currentHealth:0} / {stats.maxHealth:0}", centerStyle, 9);

            string statText =
                $"攻击 {stats.attack:0}    防御 {stats.defense:0.#}    攻速 {stats.attackSpeed:0.00}    暴击 {stats.critChance * 100f:0}%";
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(rect.x + 12f, rect.y + 62f, rect.width - 24f, 24f),
                statText, detailStyle, 8);
        }

        private void DrawDuelFocus(float width, float top, float height)
        {
            if (ResponsiveGui.IsPortrait)
            {
                float portraitWidth = Mathf.Min(300f, ResponsiveGui.SafeArea.width - 32f);
                Rect portraitRect = new Rect((width - portraitWidth) * 0.5f, top, portraitWidth, height);
                FillRect(portraitRect, new Color(0.025f, 0.03f, 0.035f, 0.82f));
                FillRect(new Rect(portraitRect.x, portraitRect.y, portraitRect.width, 2f), Gold);
                int portraitExchange = Mathf.Max(1, battleManager.AttackSequence);
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(portraitRect.x, portraitRect.y + 5f, portraitRect.width, 25f),
                    $"交锋 · 第 {portraitExchange} 招 · {battleManager.BattleElapsed:0.0}s",
                    duelStyle, 11);
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(portraitRect.x, portraitRect.y + 31f, portraitRect.width, 20f),
                    battleManager.LastAttackWasCritical ? "暴击交锋" :
                    battleManager.LastAttackWasDodged ? "身法闪避" : "双方自动演武",
                    captionStyle, 9);
                return;
            }

            float focusWidth = 168f;
            Rect focusRect = new Rect((width - focusWidth) * 0.5f, top + 5f, focusWidth, height - 10f);
            FillRect(focusRect, new Color(0.025f, 0.03f, 0.035f, 0.82f));
            FillRect(new Rect(focusRect.x, focusRect.y, focusRect.width, 2f), Gold);
            FillRect(new Rect(focusRect.x, focusRect.yMax - 2f, focusRect.width, 2f),
                new Color(Gold.r, Gold.g, Gold.b, 0.45f));
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(focusRect.x, focusRect.y + 9f, focusRect.width, 30f),
                "交  锋", duelStyle, 14);
            int exchange = Mathf.Max(1, battleManager.AttackSequence);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(focusRect.x, focusRect.y + 42f, focusRect.width, 22f),
                $"第 {exchange} 招  ·  {battleManager.BattleElapsed:0.0}s", detailStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(focusRect.x, focusRect.y + 64f, focusRect.width, 18f),
                battleManager.LastAttackWasCritical ? "暴击交锋" :
                battleManager.LastAttackWasDodged ? "身法闪避" : "自动演武", captionStyle, 9);
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
            FillRect(messageRect, new Color(0.015f, 0.02f, 0.025f, 0.84f));
            FillRect(new Rect(messageRect.x, messageRect.y, messageRect.width, 2f), Gold);

            float sideWidth = messageRect.width * 0.29f;
            float centerWidth = messageRect.width - sideWidth * 2f;
            Rect playerInfoRect = new Rect(messageRect.x, messageRect.y, sideWidth, messageRect.height);
            Rect logRect = new Rect(playerInfoRect.xMax, messageRect.y, centerWidth, messageRect.height);
            Rect enemyInfoRect = new Rect(logRect.xMax, messageRect.y, sideWidth, messageRect.height);
            FillRect(new Rect(playerInfoRect.xMax, messageRect.y + 11f, 1f, messageRect.height - 22f),
                new Color(1f, 1f, 1f, 0.13f));
            FillRect(new Rect(logRect.xMax, messageRect.y + 11f, 1f, messageRect.height - 22f),
                new Color(1f, 1f, 1f, 0.13f));

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(playerInfoRect.x, playerInfoRect.y + 6f, playerInfoRect.width, 18f),
                "少侠构筑", captionStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(playerInfoRect.x + 10f, playerInfoRect.y + 25f, playerInfoRect.width - 20f, 39f),
                GetPlayerBuildSummary(), detailStyle, 8);

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(logRect.x, logRect.y + 6f, logRect.width, 18f),
                "战况", captionStyle, 9);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(logRect.x + 10f, logRect.y + 24f, logRect.width - 20f, 42f),
                battleManager.battleLog, centerStyle, 8);

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(enemyInfoRect.x, enemyInfoRect.y + 6f, enemyInfoRect.width, 18f),
                "交锋状态", captionStyle, 9);
            string effectSummary =
                $"护盾 {battleManager.PlayerShield:0}  ·  毒 {battleManager.EnemyPoisonStacks} 层  ·  破甲 {battleManager.EnemyArmorBreak:0.0}";
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(enemyInfoRect.x + 10f, enemyInfoRect.y + 25f, enemyInfoRect.width - 20f, 39f),
                effectSummary, detailStyle, 8);

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

        private string GetPlayerBuildSummary()
        {
            if (playerStats.learnedMartialArts.Count == 0)
            {
                return $"境界 {playerStats.level}  ·  尚未习得武学";
            }

            string summary = $"境界 {playerStats.level}  ·  ";
            int shown = Mathf.Min(2, playerStats.learnedMartialArts.Count);
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    summary += "  /  ";
                }

                string artId = playerStats.learnedMartialArts[i];
                summary += $"{artId} {playerStats.GetMartialArtRank(artId)}重";
            }

            if (playerStats.learnedMartialArts.Count > shown)
            {
                summary += $"  等{playerStats.learnedMartialArts.Count}门";
            }

            return summary;
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
