using UnityEngine;

namespace WuxiaRoguelite.UI
{
    public static class RuntimeChineseFont
    {
        private const string RegularResourcePath = "Fonts/NotoSansCJKsc-Regular-Subset";
        private const string BoldResourcePath = "Fonts/NotoSansCJKsc-Bold-Subset";

        private static Font regularFont;
        private static Font boldFont;
        private static bool fontsLoaded;
        private static bool missingFontReported;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            regularFont = null;
            boldFont = null;
            fontsLoaded = false;
            missingFontReported = false;
        }

        public static void PrepareSkin()
        {
            EnsureFontsLoaded();
            if (regularFont != null)
            {
                GUI.skin.font = regularFont;
            }
        }

        public static GUIStyle Apply(GUIStyle style)
        {
            if (style == null)
            {
                return null;
            }

            EnsureFontsLoaded();
            bool wantsBold = style.fontStyle == FontStyle.Bold ||
                             style.fontStyle == FontStyle.BoldAndItalic;
            Font selectedFont = wantsBold && boldFont != null ? boldFont : regularFont;
            if (selectedFont == null)
            {
                return style;
            }

            bool wantsItalic = style.fontStyle == FontStyle.Italic ||
                               style.fontStyle == FontStyle.BoldAndItalic;
            style.font = selectedFont;
            style.fontStyle = wantsItalic ? FontStyle.Italic : FontStyle.Normal;
            return style;
        }

        private static void EnsureFontsLoaded()
        {
            if (fontsLoaded)
            {
                return;
            }

            fontsLoaded = true;
            regularFont = Resources.Load<Font>(RegularResourcePath);
            boldFont = Resources.Load<Font>(BoldResourcePath);

            if ((regularFont == null || boldFont == null) && !missingFontReported)
            {
                missingFontReported = true;
                Debug.LogError(
                    "WebGL 中文字体资源未加载。请确认 Resources/Fonts 下的 Noto Sans CJK SC 字体已被导入。");
            }
        }
    }
}
