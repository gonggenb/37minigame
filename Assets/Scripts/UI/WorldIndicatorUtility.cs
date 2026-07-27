using UnityEngine;

namespace WuxiaRoguelite.UI
{
    public static class WorldIndicatorUtility
    {
        private const float HorizontalViewportMargin = 0.06f;
        private const float VerticalViewportMargin = 0.1f;

        public static bool IsInsideCamera(Camera worldCamera, Vector3 worldPosition)
        {
            if (worldCamera == null)
            {
                return false;
            }

            Vector3 viewportPoint = worldCamera.WorldToViewportPoint(worldPosition);
            return viewportPoint.z > 0f &&
                   viewportPoint.x >= HorizontalViewportMargin &&
                   viewportPoint.x <= 1f - HorizontalViewportMargin &&
                   viewportPoint.y >= VerticalViewportMargin &&
                   viewportPoint.y <= 1f - VerticalViewportMargin;
        }

        public static Vector2 GetClampedGuiPoint(
            Camera worldCamera,
            Vector3 worldPosition,
            float guiScale,
            out Vector2 direction)
        {
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z < 0f)
            {
                screenPoint.x = Screen.width - screenPoint.x;
                screenPoint.y = Screen.height - screenPoint.y;
            }

            Vector2 rawGuiPoint = ResponsiveGui.ScreenPointToGui(screenPoint, guiScale);
            Rect safeArea = ResponsiveGui.SafeArea;
            float leftMargin = ResponsiveGui.IsPortrait ? 42f : 46f;
            float rightMargin = ResponsiveGui.IsPortrait ? 86f : 52f;
            float topMargin = ResponsiveGui.IsPortrait ? 128f : 82f;
            float bottomMargin = ResponsiveGui.IsPortrait ? 178f : 72f;
            Rect markerBounds = new Rect(
                safeArea.xMin + leftMargin,
                safeArea.yMin + topMargin,
                Mathf.Max(1f, safeArea.width - leftMargin - rightMargin),
                Mathf.Max(1f, safeArea.height - topMargin - bottomMargin));

            Vector2 center = markerBounds.center;
            direction = rawGuiPoint - center;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.up;
            }

            Vector2 edgePoint = center;
            float horizontalScale = Mathf.Abs(direction.x) > 0.001f
                ? markerBounds.width * 0.5f / Mathf.Abs(direction.x)
                : float.PositiveInfinity;
            float verticalScale = Mathf.Abs(direction.y) > 0.001f
                ? markerBounds.height * 0.5f / Mathf.Abs(direction.y)
                : float.PositiveInfinity;
            edgePoint += direction * Mathf.Min(horizontalScale, verticalScale);
            return new Vector2(
                Mathf.Clamp(edgePoint.x, markerBounds.xMin, markerBounds.xMax),
                Mathf.Clamp(edgePoint.y, markerBounds.yMin, markerBounds.yMax));
        }

        public static string DirectionArrow(Vector2 guiDirection)
        {
            if (Mathf.Abs(guiDirection.x) > Mathf.Abs(guiDirection.y) * 1.15f)
            {
                return guiDirection.x < 0f ? "←" : "→";
            }

            return guiDirection.y < 0f ? "↑" : "↓";
        }
    }
}
