using UnityEngine;

namespace WuxiaRoguelite.UI
{
    public static class TimePressureBarRenderer
    {
        private const string FrameResourcePath = "UI/tex_ui_timebar_frame_v01";
        private static Texture2D frameTexture;

        private static readonly Color Empty = new Color(0.025f, 0.022f, 0.02f, 0.96f);
        private static readonly Color LastTwenty = WuxiaUiTheme.Danger;
        private static readonly Color MiddleTwenty = WuxiaUiTheme.Warning;
        private static readonly Color FirstTwenty = WuxiaUiTheme.InkGreen;
        private static readonly Color Paused = WuxiaUiTheme.Paused;

        public static void Draw(Rect rect, float ratio, bool paused)
        {
            ratio = Mathf.Clamp01(ratio);
            Texture2D frame = GetFrameTexture();

            Rect inner = new Rect(
                rect.x + rect.width * 0.036f,
                rect.y + rect.height * 0.25f,
                rect.width * 0.928f,
                rect.height * 0.50f);
            FillRect(inner, Empty);

            if (paused)
            {
                FillRect(new Rect(inner.x, inner.y, inner.width * ratio, inner.height), Paused);
            }
            else
            {
                DrawSegment(inner, ratio, 0f, 1f / 3f, LastTwenty);
                DrawSegment(inner, ratio, 1f / 3f, 2f / 3f, MiddleTwenty);
                DrawSegment(inner, ratio, 2f / 3f, 1f, FirstTwenty);

                if (ratio > 0f && ratio <= 1f / 3f)
                {
                    float pulse = 0.35f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 7f));
                    FillRect(
                        new Rect(inner.x, inner.y, inner.width * ratio, inner.height),
                        new Color(1f, 0.12f, 0.04f, pulse));
                }
            }

            DrawBurningEdge(inner, ratio, paused);
            DrawTwentySecondMarkers(inner, paused);

            if (frame != null)
            {
                GUI.DrawTexture(rect, frame, ScaleMode.StretchToFill, true);
            }
            else
            {
                DrawFallbackFrame(rect);
            }
        }

        private static Texture2D GetFrameTexture()
        {
            if (frameTexture != null)
            {
                return frameTexture;
            }

            frameTexture = Resources.Load<Texture2D>(FrameResourcePath);
            if (frameTexture != null)
            {
                frameTexture.filterMode = FilterMode.Point;
                frameTexture.wrapMode = TextureWrapMode.Clamp;
            }

            return frameTexture;
        }

        private static void DrawSegment(Rect inner, float ratio, float start, float end, Color color)
        {
            float visibleEnd = Mathf.Min(ratio, end);
            if (visibleEnd <= start)
            {
                return;
            }

            FillRect(
                new Rect(
                    inner.x + inner.width * start,
                    inner.y,
                    inner.width * (visibleEnd - start),
                    inner.height),
                color);
        }

        private static void DrawBurningEdge(Rect inner, float ratio, bool paused)
        {
            if (ratio <= 0f)
            {
                return;
            }

            float pulse = paused ? 0f : 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
            float emberWidth = paused ? 2f : 3f + pulse * 3f;
            float emberX = Mathf.Clamp(
                inner.x + inner.width * ratio - emberWidth * 0.5f,
                inner.x,
                inner.xMax - emberWidth);
            Color ember = paused
                ? new Color(0.76f, 0.93f, 1f, 0.88f)
                : new Color(1f, 0.84f, 0.28f, 0.78f + pulse * 0.22f);
            FillRect(new Rect(emberX, inner.y - 1f, emberWidth, inner.height + 2f), ember);
        }

        private static void DrawTwentySecondMarkers(Rect inner, bool paused)
        {
            Color marker = paused
                ? new Color(0.82f, 0.95f, 1f, 0.72f)
                : new Color(0.05f, 0.035f, 0.025f, 0.80f);
            for (int i = 1; i <= 2; i++)
            {
                float x = inner.x + inner.width * i / 3f;
                FillRect(new Rect(x - 1f, inner.y - 2f, 2f, inner.height + 4f), marker);
            }
        }

        private static void DrawFallbackFrame(Rect rect)
        {
            Color iron = new Color(0.18f, 0.16f, 0.13f, 1f);
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), iron);
            FillRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), iron);
            FillRect(new Rect(rect.x, rect.y, 3f, rect.height), iron);
            FillRect(new Rect(rect.xMax - 3f, rect.y, 3f, rect.height), iron);
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
