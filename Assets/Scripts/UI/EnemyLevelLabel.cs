using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;

namespace WuxiaRoguelite.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EncounterTrigger))]
    public class EnemyLevelLabel : MonoBehaviour
    {
        [Header("战场感知")]
        [Tooltip("镜头外的敌人在该距离内显示方向、等级和距离。")]
        [Min(1f)] public float awarenessDistance = 15f;

        private EncounterTrigger encounter;
        private SpriteRenderer spriteRenderer;
        private GUIStyle labelStyle;
        private GUIStyle shadowStyle;
        private GUIStyle edgeStyle;

        private void Awake()
        {
            encounter = GetComponent<EncounterTrigger>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            if (encounter == null || encounter.consumed ||
                (encounter.encounterType != EncounterType.NormalEnemy &&
                 encounter.encounterType != EncounterType.EliteEnemy))
            {
                return;
            }

            GameFlowController gameFlow = GameFlowController.Instance;
            if (gameFlow == null || gameFlow.CurrentPhase != GamePhase.MainMapRunning ||
                gameFlow.playerController == null)
            {
                return;
            }

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return;
            }

            Vector3 anchor = spriteRenderer != null
                ? new Vector3(spriteRenderer.bounds.center.x, spriteRenderer.bounds.max.y + 0.22f,
                    spriteRenderer.bounds.center.z)
                : transform.position + Vector3.up * 1.8f;
            float playerDistance = Vector3.Distance(
                gameFlow.playerController.transform.position, transform.position);
            bool isInsideCamera = WorldIndicatorUtility.IsInsideCamera(worldCamera, anchor);
            float effectiveAwarenessDistance = gameFlow.cameraFollow != null
                ? gameFlow.cameraFollow.GetAwarenessDistance(awarenessDistance)
                : awarenessDistance;
            if (!isInsideCamera && playerDistance > effectiveAwarenessDistance)
            {
                return;
            }

            EnsureStyle();
            if (!isInsideCamera)
            {
                DrawEdgeIndicator(worldCamera, anchor, playerDistance, gameFlow.playerStats);
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            GUI.depth = -100;
            float guiScale = ResponsiveGui.Scale;
            Vector2 guiPoint = ResponsiveGui.ScreenPointToGui(screenPoint, guiScale);
            string trait = WuxiaRoguelite.Battle.EnemyTraits.Label(encounter.Trait);
            float width = string.IsNullOrEmpty(trait) ? 52 : 164;
            Rect labelRect = new Rect(guiPoint.x - width / 2, guiPoint.y - 9f, width, 18f);
            string levelText = $"{encounter.enemyStats.DisplayLevel}级" + (string.IsNullOrEmpty(trait) ? "" : " · " + trait);
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(guiScale);
            var bounty = gameFlow.HasRunChallenge ? gameFlow.ChallengeRun.Find(encounter) : null;
            if (bounty != null && !bounty.completed)
            {
                Rect banner = new Rect(guiPoint.x - 110, labelRect.y - 34, 220, 30);
                WuxiaUiTheme.DrawCompactSurface(banner, new Color(.05f, .035f, .025f, .94f), WuxiaUiTheme.Danger);
                var icon = ChallengeArt.Get("bounty_writ");
                if (icon != null) GUI.DrawTexture(new Rect(banner.x + 2, banner.y, 30, 30), icon, ScaleMode.ScaleToFit, true);
                WuxiaUiComponents.Text(new Rect(banner.x + 36, banner.y, banner.width - 40, 30),
                    "悬赏强敌 · 胜利主修升重", 12, WuxiaUiTheme.Brass);
            }
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height),
                levelText, shadowStyle, 9);

            Color previous = GUI.color;
            GUI.color = encounter.encounterType == EncounterType.EliteEnemy
                ? new Color(1f, 0.62f, 0.55f)
                : new Color(1f, 0.90f, 0.64f);
            ResponsiveGui.DrawSingleLineLabel(labelRect, levelText, labelStyle, 9);
            GUI.color = previous;
            // Nearby targets expose the decision before contact. Distant targets remain compact.
            if (playerDistance <= 9f && !string.IsNullOrEmpty(trait))
            {
                string counter = EnemyMatchupInsight.Counter(encounter.Trait, gameFlow.playerStats);
                Rect insight = new Rect(Mathf.Clamp(guiPoint.x - 132, ResponsiveGui.SafeArea.x + 8,
                    ResponsiveGui.SafeArea.xMax - 272), labelRect.yMax + 2, 264,
                    string.IsNullOrEmpty(counter) ? 28 : 50);
                WuxiaUiTheme.DrawCompactSurface(insight, WuxiaUiTheme.BackgroundInk, WuxiaUiTheme.Brass);
                WuxiaUiComponents.Text(new Rect(insight.x + 6, insight.y, insight.width - 12, 26),
                    EnemyMatchupInsight.Weakness(encounter.Trait), 14);
                if (!string.IsNullOrEmpty(counter)) WuxiaUiComponents.Text(
                    new Rect(insight.x + 6, insight.y + 24, insight.width - 12, 24), counter, 14,
                    WuxiaUiTheme.TextPrimary);
            }
            GUI.matrix = originalGuiMatrix;
        }

        private void DrawEdgeIndicator(
            Camera worldCamera,
            Vector3 anchor,
            float playerDistance,
            PlayerStats playerStats)
        {
            float guiScale = ResponsiveGui.Scale;
            Vector2 markerPoint = WorldIndicatorUtility.GetClampedGuiPoint(
                worldCamera, anchor, guiScale, out Vector2 direction);
            string arrow = WorldIndicatorUtility.DirectionArrow(direction);
            string label = $"{arrow} {encounter.enemyStats.DisplayLevel}级  {Mathf.CeilToInt(playerDistance)}步";
            var flow = GameFlowController.Instance;
            if (flow != null && flow.HasRunChallenge && flow.ChallengeRun.Find(encounter) != null)
                label = $"{arrow} 悬赏  {Mathf.CeilToInt(playerDistance)}步";
            Rect panel = new Rect(markerPoint.x - 54f, markerPoint.y - 13f, 108f, 26f);

            int playerLevel = playerStats != null ? playerStats.level : 1;
            int levelDelta = encounter.enemyStats.DisplayLevel - playerLevel;
            Color riskColor = levelDelta >= 2
                ? new Color(1f, 0.38f, 0.32f, 0.96f)
                : levelDelta == 1
                    ? new Color(1f, 0.72f, 0.24f, 0.96f)
                    : new Color(0.50f, 0.88f, 0.67f, 0.96f);
            if (encounter.encounterType == EncounterType.EliteEnemy)
            {
                riskColor = Color.Lerp(riskColor, new Color(1f, 0.34f, 0.28f, 0.96f), 0.45f);
            }

            GUI.depth = -121;
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(guiScale);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.045f, 0.04f, 0.88f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = riskColor;
            GUI.DrawTexture(new Rect(panel.x, panel.y, 3f, panel.height), Texture2D.whiteTexture);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 5f, panel.y + 2f, panel.width - 9f, panel.height - 4f),
                label, edgeStyle, 10);
            GUI.color = previous;
            GUI.matrix = originalGuiMatrix;
        }

        private void EnsureStyle()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
            shadowStyle = new GUIStyle(labelStyle)
            {
                normal = { textColor = new Color(0f, 0f, 0f, 0.82f) }
            };
            edgeStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
        }
    }
}
