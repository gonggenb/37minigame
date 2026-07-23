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
        public Sprite[] playerIdleFrames;
        public Sprite[] playerAttackFrames;
        public Sprite[] enemyIdleFrames;
        public Sprite[] enemyAttackFrames;
        public Sprite[] eliteIdleFrames;
        public Sprite[] eliteAttackFrames;
        public Sprite[] caveIdleFrames;
        public Sprite[] caveAttackFrames;
        public EnemyVisualProfile[] enemyVisualProfiles;
        [Min(0.5f)] public float playerSpriteScale = ActorVisualScale.Medium;
        [Min(0.5f)] public float bossSpriteScale = ActorVisualScale.Large;

        private GUIStyle titleStyle;
        private GUIStyle nameStyle;
        private GUIStyle centerStyle;
        private GUIStyle timerStyle;
        private GUIStyle actorMarkStyle;
        private GUIStyle damageStyle;
        private GUIStyle damageShadowStyle;
        private GUIStyle criticalDamageStyle;
        private GUIStyle criticalDamageShadowStyle;
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

        private const float DamageDisplayDuration = 0.72f;
        private const float HealthFlashDuration = 0.34f;

        private static readonly Color Backdrop = new Color(0.055f, 0.075f, 0.09f, 1f);
        private static readonly Color DistantMountain = new Color(0.11f, 0.19f, 0.20f, 1f);
        private static readonly Color Ground = new Color(0.18f, 0.16f, 0.13f, 1f);
        private static readonly Color Ink = new Color(0.07f, 0.08f, 0.075f, 1f);
        private static readonly Color PlayerColor = new Color(0.18f, 0.68f, 0.88f, 1f);
        private static readonly Color EnemyColor = new Color(0.82f, 0.22f, 0.17f, 1f);
        private static readonly Color HealthColor = new Color(0.24f, 0.78f, 0.40f, 1f);
        private void OnGUI()
        {
            if (battleManager == null || playerStats == null || gameFlow == null ||
                !battleManager.IsBattleActive || battleManager.currentEnemy == null || playerStats.runtimeStats == null)
            {
                return;
            }

            GUI.depth = -1000;
            EnsureStyles();
            TrackLatestAttack();
            TrackHealthChanges();

            float width = Screen.width;
            float height = Screen.height;
            DrawBackdrop(width, height);
            DrawHeader(width, height);

            float sidePadding = Mathf.Clamp(width * 0.035f, 12f, 28f);
            float healthTop = Mathf.Clamp(height * 0.21f, 72f, 100f);
            float healthHeight = Mathf.Clamp(height * 0.15f, 52f, 68f);
            float healthWidth = Mathf.Min(340f, width * 0.42f);
            Rect playerHealthRect = new Rect(sidePadding, healthTop, healthWidth, healthHeight);
            Rect enemyHealthRect = new Rect(width - sidePadding - healthWidth, healthTop, healthWidth, healthHeight);

            float messageHeight = Mathf.Clamp(height * 0.12f, 40f, 52f);
            Rect messageRect = new Rect(width * 0.16f, height - messageHeight - 12f, width * 0.68f, messageHeight);
            float stageTop = healthTop + healthHeight + 8f;
            float stageBottom = messageRect.y - 8f;
            float stageHeight = Mathf.Max(80f, stageBottom - stageTop);
            Rect stageRect = new Rect(0f, stageTop, width, stageHeight);

            float actorSize = Mathf.Clamp(Mathf.Min(width * 0.25f, stageHeight * 0.82f), 90f, 240f);
            float baseY = stageBottom - 14f;
            float attackDuration = 0.32f / battleManager.BattleSpeedMultiplier;
            float actionProgress = Mathf.Clamp01((Time.unscaledTime - attackStartedAt) / attackDuration);
            float lunge = Mathf.Sin(actionProgress * Mathf.PI) * Mathf.Min(54f, width * 0.05f);
            float shake = actionProgress > 0.38f && actionProgress < 0.78f
                ? Mathf.Sin(actionProgress * 70f) * 7f
                : 0f;

            float playerX = width * 0.22f - actorSize * 0.5f;
            float enemyX = width * 0.78f - actorSize * 0.5f;
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
            float enemyActorSize = actorSize * enemySpriteScale;
            Rect playerRect = new Rect(playerX + (actorSize - playerActorSize) * 0.5f,
                baseY - playerActorSize, playerActorSize, playerActorSize);
            Rect enemyRect = new Rect(enemyX + (actorSize - enemyActorSize) * 0.5f,
                baseY - enemyActorSize, enemyActorSize, enemyActorSize);
            DrawFighter(playerRect, PlayerColor, "侠", false,
                playerAttacking ? playerAttackFrames : playerIdleFrames, playerAttacking, actionProgress);
            DrawFighter(enemyRect, EnemyColor, "敌", enemyVisual != null ? enemyVisual.flipHorizontally : true,
                enemyAttacking ? currentEnemyAttackFrames : currentEnemyIdleFrames, enemyAttacking, actionProgress);
            DrawHealthPanel(playerHealthRect, playerStats.runtimeStats, playerDamageAmount, playerDamageStartedAt);
            DrawHealthPanel(enemyHealthRect, battleManager.currentEnemy, enemyDamageAmount, enemyDamageStartedAt);
            DrawDamagePopup(playerRect, playerDamageAmount, playerDamageStartedAt, playerDamageWasCritical, true);
            DrawDamagePopup(enemyRect, enemyDamageAmount, enemyDamageStartedAt, enemyDamageWasCritical, false);
            DrawCombatMessage(stageRect, messageRect, actionProgress);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = CreateStyle(24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            nameStyle = CreateStyle(17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            centerStyle = CreateStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            timerStyle = CreateStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.58f));
            actorMarkStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            damageStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            damageShadowStyle = CreateStyle(32, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.9f));
            criticalDamageStyle = CreateStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            criticalDamageShadowStyle = CreateStyle(38, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.95f));
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
            FillRect(new Rect(0f, 0f, width, height), Backdrop);
            FillRect(new Rect(0f, height * 0.34f, width, height * 0.26f), DistantMountain);
            FillRect(new Rect(0f, height * 0.58f, width, height * 0.42f), Ground);
            FillRect(new Rect(0f, height * 0.575f, width, 5f), new Color(0.65f, 0.52f, 0.28f));

            float moonSize = Mathf.Clamp(height * 0.13f, 64f, 110f);
            FillRect(new Rect(width * 0.5f - moonSize * 0.5f, height * 0.37f - moonSize * 0.5f, moonSize, moonSize), new Color(0.82f, 0.78f, 0.63f, 0.23f));
        }

        private void DrawHeader(float width, float height)
        {
            string title = gameFlow.CurrentPhase == GamePhase.BossBattle
                ? "决战 · 黑风寨"
                : gameFlow.CurrentPhase == GamePhase.CaveRunning
                    ? "秘境 · 自动战斗"
                    : "遭遇 · 自动战斗";
            GUI.Label(new Rect(0f, 8f, width, 36f), title, titleStyle);

            string timerText;
            if (gameFlow.CurrentPhase == GamePhase.NormalBattleRunning)
            {
                timerText = $"主地图倒数持续流逝  {gameFlow.mainTimeRemaining:0.0}s";
            }
            else if (gameFlow.CurrentPhase == GamePhase.CaveRunning)
            {
                timerText = $"主地图倒数已暂停  {gameFlow.mainTimeRemaining:0.0}s";
            }
            else
            {
                timerText = $"Boss 独立战斗时间  {gameFlow.bossBattleTime:0.0}s";
            }

            GUI.Label(new Rect(0f, 42f, width, 25f), timerText, timerStyle);
            FillRect(new Rect(width * 0.18f, Mathf.Min(70f, height * 0.19f), width * 0.64f, 1f), new Color(1f, 1f, 1f, 0.12f));
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
            Rect textureRect = sprite.textureRect;
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

        private void DrawHealthPanel(Rect rect, CombatantStats stats, float recentDamage, float damageStartedAt)
        {
            float hitAge = Time.unscaledTime - damageStartedAt;
            float flash = 1f - Mathf.Clamp01(hitAge / HealthFlashDuration);
            if (recentDamage > 0f && flash > 0f)
            {
                FillRect(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f),
                    new Color(1f, 0.12f, 0.08f, 0.82f * flash));
            }

            FillRect(rect, new Color(0f, 0f, 0f, 0.52f));
            GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, 24f), stats.displayName, nameStyle);

            Rect bar = new Rect(rect.x + 10f, rect.y + rect.height - 22f, rect.width - 20f, 14f);
            FillRect(bar, Ink);
            float innerWidth = bar.width - 4f;
            float currentRatio = stats.HealthRatio;
            FillRect(new Rect(bar.x + 2f, bar.y + 2f, innerWidth * currentRatio, bar.height - 4f), HealthColor);
            float damageAge = Time.unscaledTime - damageStartedAt;
            if (recentDamage > 0f && damageAge < DamageDisplayDuration && stats.maxHealth > 0f)
            {
                float beforeHitRatio = Mathf.Clamp01((stats.currentHealth + recentDamage) / stats.maxHealth);
                float lossWidth = innerWidth * Mathf.Max(0f, beforeHitRatio - currentRatio);
                float lossAlpha = 1f - Mathf.Clamp01(damageAge / DamageDisplayDuration);
                FillRect(new Rect(bar.x + 2f + innerWidth * currentRatio, bar.y + 2f, lossWidth, bar.height - 4f),
                    new Color(1f, 0.16f, 0.10f, 0.95f * lossAlpha));
            }
            GUI.Label(new Rect(bar.x, bar.y - 1f, bar.width, bar.height + 2f), $"{stats.currentHealth:0} / {stats.maxHealth:0}", centerStyle);
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
            string text = critical ? $"暴击  -{damage:0}" : $"气血  -{damage:0}";
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

        private void DrawCombatMessage(Rect stageRect, Rect messageRect, float actionProgress)
        {
            FillRect(messageRect, new Color(0f, 0f, 0f, 0.72f));
            FillRect(new Rect(messageRect.x, messageRect.y, messageRect.width, 2f), new Color(0.65f, 0.52f, 0.28f, 0.72f));
            GUI.Label(messageRect, battleManager.battleLog, centerStyle);

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

        private static GUIStyle CreateStyle(int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                normal = { textColor = color }
            };
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
