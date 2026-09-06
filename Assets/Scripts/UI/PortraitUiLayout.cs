using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.GameFlow;

namespace WuxiaRoguelite.UI
{
    /// <summary>Shared layout contract for independently drawn player and enemy HUDs.</summary>
    public static class PortraitUiLayout
    {
        public static float CombatHealthTop(GamePhase phase) =>
            phase == GamePhase.BossBattle ? 174f : phase == GamePhase.MidBossBattle ? 140f : 154f;

        public static Rect Modal(float preferredHeight, float preferredWidth = 492f)
        {
            Rect safe = ResponsiveGui.SafeArea;
            float width = Mathf.Min(preferredWidth, safe.width - 32f);
            float height = Mathf.Min(preferredHeight, safe.height - 40f);
            return new Rect(safe.center.x - width / 2f, safe.center.y - height / 2f, width, height);
        }
    }

    /// <summary>Reusable text, timer and row components; no gameplay state or baked text.</summary>
    public static class WuxiaUiComponents
    {
        private static readonly Dictionary<int, GUIStyle> labels = new Dictionary<int, GUIStyle>();
        private static GUIStyle touchButton;
        private static GUIStyle primaryButton;

        public static GUIStyle TouchButton(bool primary = false)
        {
            if (primary) return primaryButton ??= WuxiaUiTheme.CreateButtonStyle(18, WuxiaButtonKind.Primary);
            return touchButton ??= WuxiaUiTheme.CreateButtonStyle(16, WuxiaButtonKind.Secondary);
        }

        public static void Text(Rect rect, string text, int size = 16, Color? color = null,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = false)
        {
            int key = size * 100 + (int)anchor * 2 + (wrap ? 1 : 0);
            if (!labels.TryGetValue(key, out GUIStyle style))
            {
                style = RuntimeChineseFont.Apply(new GUIStyle
                {
                    fontSize = size, fontStyle = size >= 18 ? FontStyle.Bold : FontStyle.Normal,
                    alignment = anchor, wordWrap = wrap, clipping = TextClipping.Clip
                });
                labels[key] = style;
            }
            style.normal.textColor = color ?? WuxiaUiTheme.TextPrimary;
            GUI.Label(rect, text ?? string.Empty, style);
        }

        public static Color TimeColor(float ratio) => ratio <= 1f / 3f ? WuxiaUiTheme.Danger :
            ratio <= 2f / 3f ? WuxiaUiTheme.Brass : WuxiaUiTheme.Jade;

        public static void Timer(Rect rect, float seconds, float limit, bool paused)
        {
            float ratio = Mathf.Clamp01(seconds / Mathf.Max(1f, limit));
            Color accent = paused ? WuxiaUiTheme.Paused : TimeColor(ratio);
            WuxiaUiTheme.DrawTimerDial(rect, accent);
            // Sixty independent live marks sit over the unnumbered generated dial.
            // Rotate in the dial's logical space, then apply the caller's scale and
            // translation. RotateAroundPivot mixes the pivot with the existing GUI
            // scale on high-DPI players, moving the marks outside the dial.
            if (Event.current.type == EventType.Repaint)
            {
                Matrix4x4 matrix = GUI.matrix;
                try
                {
                    for (int i = 0; i < 60; i++)
                    {
                        GUI.matrix = TimerTickMatrix(matrix, rect.center, i);
                        WuxiaUiTheme.FillRect(new Rect(rect.center.x - 1f,
                            rect.center.y - rect.width * 0.39f, 2f, rect.height * 0.04f),
                            i < Mathf.CeilToInt(ratio * 60f) ? accent : WuxiaUiTheme.SurfaceIron);
                    }
                }
                finally
                {
                    GUI.matrix = matrix;
                }
            }
            Text(new Rect(rect.x + 12, rect.y + rect.height * 0.24f, rect.width - 24,
                rect.height * 0.43f), Mathf.CeilToInt(seconds).ToString("00"),
                Mathf.RoundToInt(rect.width * 0.33f), WuxiaUiTheme.TextPrimary, TextAnchor.MiddleCenter);
            Text(new Rect(rect.x + 12, rect.y + rect.height * 0.64f, rect.width - 24, 18),
                paused ? "暂停" : "秒", 12, accent, TextAnchor.MiddleCenter);
        }

        internal static Matrix4x4 TimerTickMatrix(Matrix4x4 parent, Vector2 center, int tick)
        {
            Vector3 pivot = new Vector3(center.x, center.y, 0f);
            return parent * Matrix4x4.Translate(pivot) *
                Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, tick * 6f)) *
                Matrix4x4.Translate(-pivot);
        }

        public static void ReportRow(Rect rect, string label, string value)
        {
            WuxiaUiTheme.DrawCompactSurface(rect, WuxiaUiTheme.BackgroundInk, WuxiaUiTheme.Brass);
            Text(new Rect(rect.x + 14, rect.y, rect.width * 0.53f - 14, rect.height), label, 16);
            Text(new Rect(rect.x + rect.width * 0.53f, rect.y, rect.width * 0.47f - 14, rect.height),
                value, 20, WuxiaUiTheme.TextPrimary, TextAnchor.MiddleRight);
        }
    }
}
