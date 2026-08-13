using UnityEngine;

namespace WuxiaRoguelite.UI
{
    public enum WuxiaButtonKind
    {
        Primary,
        Secondary,
        Icon,
        Tab
    }

    public enum WuxiaPanelKind
    {
        Default,
        Paper,
        Combat,
        Boss
    }

    /// <summary>
    /// Shared semantic colors and IMGUI drawing primitives for the gradual UI migration.
    /// Gameplay screens should request a semantic component from here instead of creating
    /// another local approximation of the project palette.
    /// </summary>
    public static class WuxiaUiTheme
    {
        public static readonly Color BackgroundInk = Hex(0x0E1112, 0.98f);
        public static readonly Color BackgroundBrown = Hex(0x1B1512, 0.98f);
        public static readonly Color SurfaceWood = Hex(0x2A211B, 0.98f);
        public static readonly Color SurfaceIron = Hex(0x292F30, 0.98f);
        public static readonly Color SurfaceRaised = Hex(0x343A38, 0.98f);
        public static readonly Color TextPrimary = Hex(0xE9DFC3);
        public static readonly Color TextSecondary = Hex(0xA9ADA3);
        public static readonly Color TextDisabled = Hex(0x6F746E);
        public static readonly Color Brass = Hex(0xB58A46);
        public static readonly Color Gold = Hex(0xD1AA5A);
        public static readonly Color Jade = Hex(0x4E8B73);
        public static readonly Color InkGreen = Hex(0x3E6252);
        public static readonly Color Warning = Hex(0xB06F34);
        public static readonly Color Danger = Hex(0x963A31);
        public static readonly Color Paused = Hex(0x536F7A);

        private static Texture2D panelDefault;
        private static Texture2D panelPaper;
        private static Texture2D panelBoss;
        private static Texture2D slotFrame;
        private static Texture2D timerDial;
        private static Texture2D buttonNormal;
        private static Texture2D buttonHover;
        private static Texture2D buttonPressed;
        private static Texture2D buttonSelected;
        private static Texture2D buttonPrimary;
        private static Texture2D buttonPrimaryHover;
        private static GUIStyle panelDefaultStyle;
        private static GUIStyle panelPaperStyle;
        private static GUIStyle panelBossStyle;
        private static GUIStyle slotStyle;

        public static GUIStyle CreateButtonStyle(
            int fontSize,
            WuxiaButtonKind kind,
            TextAnchor alignment = TextAnchor.MiddleCenter,
            bool selected = false)
        {
            EnsureTextures();
            Texture2D normal = selected
                ? buttonSelected
                : kind == WuxiaButtonKind.Primary
                    ? buttonPrimary
                    : buttonNormal;
            Texture2D hover = kind == WuxiaButtonKind.Primary
                ? buttonPrimaryHover
                : buttonHover;

            GUIStyle style = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = alignment,
                wordWrap = false,
                border = new RectOffset(26, 26, 14, 14),
                padding = kind == WuxiaButtonKind.Icon
                    ? new RectOffset(7, 7, 7, 7)
                    : new RectOffset(12, 12, 6, 6)
            });

            style.normal.background = normal;
            style.hover.background = hover;
            style.active.background = buttonPressed;
            style.focused.background = selected ? buttonSelected : hover;
            style.onNormal.background = buttonSelected;
            style.onHover.background = buttonSelected;
            style.onActive.background = buttonPressed;
            style.onFocused.background = buttonSelected;

            style.normal.textColor = kind == WuxiaButtonKind.Primary ? TextPrimary : TextSecondary;
            style.hover.textColor = TextPrimary;
            style.active.textColor = Gold;
            style.focused.textColor = TextPrimary;
            style.onNormal.textColor = TextPrimary;
            style.onHover.textColor = TextPrimary;
            style.onActive.textColor = Gold;
            style.onFocused.textColor = TextPrimary;
            return style;
        }

        public static void DrawPanel(
            Rect rect,
            Color background,
            Color accent,
            WuxiaPanelKind kind = WuxiaPanelKind.Default)
        {
            EnsureTextures();
            FillRect(new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height),
                new Color(0.01f, 0.008f, 0.006f, 0.48f * background.a));
            FillRect(rect, background);

            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(0.76f + background.a * 0.24f));
            GUI.Box(rect, GUIContent.none, GetPanelStyle(kind));
            GUI.color = previous;

            Rect inner = new Rect(rect.x + 3f, rect.y + 3f,
                Mathf.Max(0f, rect.width - 6f), Mathf.Max(0f, rect.height - 6f));
            DrawOutline(inner, new Color(accent.r, accent.g, accent.b, 0.42f), 1f);
        }

        /// <summary>
        /// Low-profile HUD surface for rows that are too short for the formal
        /// nine-slice corners. It keeps the shared material colors and metal edge
        /// language without turning every compact status row into another framed window.
        /// </summary>
        public static void DrawCompactSurface(Rect rect, Color background, Color accent)
        {
            FillRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height),
                new Color(0.01f, 0.008f, 0.006f, 0.36f * background.a));
            FillRect(rect, background);
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f),
                new Color(accent.r, accent.g, accent.b, 0.72f));
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                new Color(accent.r, accent.g, accent.b, 0.34f));
            FillRect(new Rect(rect.x, rect.y + 4f, 2f, Mathf.Max(0f, rect.height - 8f)),
                new Color(accent.r, accent.g, accent.b, 0.55f));
        }

        public static void DrawSlot(Rect rect, Color background, Color accent, bool emphasized = false)
        {
            EnsureTextures();
            FillRect(rect, background);
            Color previous = GUI.color;
            GUI.color = emphasized ? Color.white : new Color(1f, 1f, 1f, 0.82f);
            GUI.Box(rect, GUIContent.none, slotStyle);
            GUI.color = previous;
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, emphasized ? 0.95f : 0.66f),
                emphasized ? 2f : 1f);
            float mark = Mathf.Min(6f, rect.width * 0.16f);
            FillRect(new Rect(rect.x + 2f, rect.y + 2f, mark, 1f), accent);
            FillRect(new Rect(rect.x + 2f, rect.y + 2f, 1f, mark), accent);
        }

        public static void DrawTimerDial(Rect rect, Color stateAccent)
        {
            EnsureTextures();
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.44f);
            GUI.DrawTexture(new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height),
                timerDial, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, timerDial, ScaleMode.ScaleToFit, true);
            GUI.color = previous;

            float marker = Mathf.Max(3f, rect.width * 0.055f);
            FillRect(new Rect(rect.center.x - marker * 0.5f, rect.y + 4f, marker, marker), stateAccent);
        }

        public static void DrawOutline(Rect rect, Color color, float thickness)
        {
            thickness = Mathf.Max(1f, thickness);
            FillRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            FillRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        public static void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawCornerMarks(Rect rect, Color accent)
        {
            Color color = new Color(accent.r, accent.g, accent.b, 0.82f);
            const float inset = 4f;
            float length = Mathf.Clamp(Mathf.Min(rect.width, rect.height) * 0.12f, 5f, 10f);

            FillRect(new Rect(rect.x + inset, rect.y + inset, length, 1f), color);
            FillRect(new Rect(rect.x + inset, rect.y + inset, 1f, length), color);
            FillRect(new Rect(rect.xMax - inset - length, rect.y + inset, length, 1f), color);
            FillRect(new Rect(rect.xMax - inset - 1f, rect.y + inset, 1f, length), color);
            FillRect(new Rect(rect.x + inset, rect.yMax - inset - 1f, length, 1f), color);
            FillRect(new Rect(rect.x + inset, rect.yMax - inset - length, 1f, length), color);
            FillRect(new Rect(rect.xMax - inset - length, rect.yMax - inset - 1f, length, 1f), color);
            FillRect(new Rect(rect.xMax - inset - 1f, rect.yMax - inset - length, 1f, length), color);
        }

        private static void EnsureTextures()
        {
            if (panelDefault != null)
            {
                return;
            }

            panelDefault = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_panel_default_v01_128");
            panelPaper = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_panel_paper_v01_128");
            panelBoss = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_panel_boss_v01_128");
            slotFrame = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_slot_default_v01_64");
            buttonNormal = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_button_normal_v01_128x48");
            buttonHover = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_button_hover_v01_128x48");
            buttonPressed = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_button_pressed_v01_128x48");
            buttonSelected = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_button_selected_v01_128x48");
            buttonPrimary = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_button_primary_v01_128x48");
            buttonPrimaryHover = Resources.Load<Texture2D>(
                "UI/Theme/tex_ui_button_primary_hover_v01_128x48");

            // PLACEHOLDER_UI: only used if a formal Resources asset is missing.
            panelDefault ??= CreateMaterialOverlay(64, 64, true,
                "PLACEHOLDER_UI_PanelMaterial");
            panelPaper ??= panelDefault;
            panelBoss ??= panelDefault;
            slotFrame ??= CreateMaterialOverlay(32, 32, false,
                "PLACEHOLDER_UI_SlotMaterial");
            timerDial = CreateTimerDial(128);
            buttonNormal ??= CreateButtonTexture(
                SurfaceIron, Hex(0x202526), Brass, "PLACEHOLDER_UI_Button_Normal");
            buttonHover ??= CreateButtonTexture(
                SurfaceRaised, SurfaceIron, Gold, "PLACEHOLDER_UI_Button_Hover");
            buttonPressed ??= CreateButtonTexture(
                Hex(0x1B1F20), Hex(0x242827), Warning, "PLACEHOLDER_UI_Button_Pressed");
            buttonSelected ??= CreateButtonTexture(
                Hex(0x3A3023), Hex(0x292820), Gold, "PLACEHOLDER_UI_Button_Selected");
            buttonPrimary ??= CreateButtonTexture(
                Hex(0x30271E), Hex(0x201C18), Brass, "PLACEHOLDER_UI_Button_Primary");
            buttonPrimaryHover ??= CreateButtonTexture(
                Hex(0x443724), Hex(0x29231B), Gold, "PLACEHOLDER_UI_Button_PrimaryHover");

            panelDefaultStyle = FrameStyle(panelDefault, new RectOffset(30, 30, 30, 30));
            panelPaperStyle = FrameStyle(panelPaper, new RectOffset(32, 32, 32, 32));
            panelBossStyle = FrameStyle(panelBoss, new RectOffset(34, 34, 34, 34));
            slotStyle = FrameStyle(slotFrame, new RectOffset(16, 16, 16, 16));
        }

        private static GUIStyle GetPanelStyle(WuxiaPanelKind kind)
        {
            return kind switch
            {
                WuxiaPanelKind.Paper => panelPaperStyle,
                WuxiaPanelKind.Boss => panelBossStyle,
                _ => panelDefaultStyle
            };
        }

        private static GUIStyle FrameStyle(Texture2D texture, RectOffset border)
        {
            GUIStyle style = new GUIStyle
            {
                border = border,
                overflow = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };
            style.normal.background = texture;
            return style;
        }

        private static Texture2D CreateMaterialOverlay(int width, int height, bool wood, string textureName)
        {
            Texture2D texture = NewTexture(width, height, textureName, FilterMode.Bilinear);
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float grain = wood
                        ? Mathf.Sin((y + Mathf.Sin(x * 0.28f) * 2f) * 0.72f) * 0.5f + 0.5f
                        : Hash01(x, y);
                    float noise = Hash01(x * 3 + 11, y * 5 + 17);
                    float alpha = Mathf.Lerp(0.015f, wood ? 0.12f : 0.075f,
                        Mathf.Clamp01(grain * 0.72f + noise * 0.28f));
                    pixels[y * width + x] = wood
                        ? new Color(0.62f, 0.43f, 0.25f, alpha)
                        : new Color(0.72f, 0.76f, 0.72f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateButtonTexture(
            Color top,
            Color bottom,
            Color edge,
            string textureName)
        {
            const int width = 64;
            const int height = 32;
            Texture2D texture = NewTexture(width, height, textureName, FilterMode.Bilinear);
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float vertical = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    int cornerDistance = Mathf.Min(x + y,
                        Mathf.Min((width - 1 - x) + y,
                            Mathf.Min(x + (height - 1 - y),
                                (width - 1 - x) + (height - 1 - y))));
                    if (cornerDistance < 4)
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }

                    int borderDistance = Mathf.Min(Mathf.Min(x, width - 1 - x),
                        Mathf.Min(y, height - 1 - y));
                    Color color = Color.Lerp(top, bottom, vertical);
                    float noise = (Hash01(x, y) - 0.5f) * 0.045f;
                    color.r = Mathf.Clamp01(color.r + noise);
                    color.g = Mathf.Clamp01(color.g + noise);
                    color.b = Mathf.Clamp01(color.b + noise);
                    if (borderDistance <= 1)
                    {
                        color = edge;
                    }
                    else if (borderDistance == 2)
                    {
                        color = Color.Lerp(BackgroundInk, edge, 0.34f);
                    }

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateTimerDial(int size)
        {
            Texture2D texture = NewTexture(size, size,
                "PLACEHOLDER_UI_TimerDial", FilterMode.Bilinear);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outer = size * 0.47f;
            float inner = size * 0.36f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 delta = new Vector2(x, y) - center;
                    float distance = delta.magnitude;
                    if (distance > outer)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    Color color;
                    if (distance > outer - size * 0.035f)
                    {
                        color = Hex(0x4A3421);
                    }
                    else if (distance > outer - size * 0.085f)
                    {
                        color = Color.Lerp(Brass, Gold, (outer - distance) / (size * 0.085f));
                    }
                    else if (distance > inner)
                    {
                        color = SurfaceIron;
                    }
                    else
                    {
                        float radial = Mathf.Clamp01(distance / inner);
                        color = Color.Lerp(Hex(0x171714), BackgroundInk, radial);
                    }

                    float angle = Mathf.Atan2(delta.y, delta.x);
                    float tickPhase = Mathf.Abs(Mathf.Sin(angle * 6f));
                    bool tick = distance > inner - size * 0.025f &&
                                distance < inner + size * 0.045f &&
                                tickPhase < 0.11f;
                    if (tick)
                    {
                        color = Gold;
                    }

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D NewTexture(int width, int height, string textureName, FilterMode filterMode)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            return texture;
        }

        private static float Hash01(int x, int y)
        {
            unchecked
            {
                uint value = (uint)(x * 374761393 + y * 668265263);
                value = (value ^ (value >> 13)) * 1274126177u;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                alpha);
        }
    }
}
