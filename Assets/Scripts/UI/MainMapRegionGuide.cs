using UnityEngine;
using WuxiaRoguelite.GameFlow;

namespace WuxiaRoguelite.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMapRegionGuide : MonoBehaviour
    {
        [Header("路线信息")]
        public string regionName = "中央驿路";
        public string routeTheme = "起步 · 混合收益";
        public string riskLabel = "低风险";
        public Color accent = new Color(0.71f, 0.54f, 0.28f, 1f);

        [Header("显示范围")]
        [Min(0f)] public float worldHeight = 2.1f;
        [Min(1f)] public float detailDistance = 10f;
        [Min(1f)] public float maxVisibleDistance = 16f;

        private GUIStyle titleStyle;
        private GUIStyle detailStyle;
        private GUIStyle arrowStyle;

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            GameFlowController gameFlow = GameFlowController.Instance;
            if (gameFlow == null ||
                gameFlow.CurrentPhase != GamePhase.MainMapRunning ||
                gameFlow.playerController == null)
            {
                return;
            }

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return;
            }

            float playerDistance = Vector3.Distance(
                gameFlow.playerController.transform.position,
                transform.position);
            if (playerDistance > maxVisibleDistance)
            {
                return;
            }

            Vector3 anchor = transform.position + Vector3.up * worldHeight;
            if (!WorldIndicatorUtility.IsInsideCamera(worldCamera, anchor))
            {
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            EnsureStyles();
            float guiScale = ResponsiveGui.Scale;
            Vector2 guiPoint = ResponsiveGui.ScreenPointToGui(screenPoint, guiScale);
            bool showDetails = playerDistance <= detailDistance;
            float width = showDetails ? 158f : 112f;
            float height = showDetails ? 43f : 25f;
            Rect panel = new Rect(guiPoint.x - width * 0.5f, guiPoint.y - height - 9f, width, height);

            GUI.depth = -118;
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(guiScale);
            WuxiaUiTheme.DrawCompactSurface(
                panel,
                new Color(
                    WuxiaUiTheme.BackgroundBrown.r,
                    WuxiaUiTheme.BackgroundBrown.g,
                    WuxiaUiTheme.BackgroundBrown.b,
                    0.92f),
                accent);

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 8f, panel.y + 2f, panel.width - 16f, 20f),
                regionName, titleStyle, 10);
            if (showDetails)
            {
                ResponsiveGui.DrawSingleLineLabel(
                    new Rect(panel.x + 8f, panel.y + 21f, panel.width - 16f, 18f),
                    $"{routeTheme} · {riskLabel}", detailStyle, 8);
            }

            Color previous = GUI.color;
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.94f);
            GUI.Label(new Rect(guiPoint.x - 12f, guiPoint.y - 11f, 24f, 18f), "▼", arrowStyle);
            GUI.color = previous;
            GUI.matrix = originalGuiMatrix;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = WuxiaUiTheme.TextPrimary }
            });
            detailStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = WuxiaUiTheme.TextSecondary }
            });
            arrowStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
        }
    }
}
