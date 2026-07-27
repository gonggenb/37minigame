using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;

namespace WuxiaRoguelite.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EncounterTrigger))]
    public class TreasureMapIndicator : MonoBehaviour
    {
        [Header("世界表现")]
        [Min(1f)] public float visualScaleMultiplier = 1.3f;
        [Min(0f)] public float bobHeight = 0.14f;
        [Min(0.1f)] public float pulseSpeed = 3.4f;
        [Min(0.5f)] public float glowScale = 1.55f;

        [Header("地图提示")]
        [Min(1f)] public float worldHeight = 2.15f;
        [Min(1f)] public float nearDistance = 3f;
        [Min(1f)] public float maxVisibleDistance = 5.5f;
        [Tooltip("镜头外仍会显示宝箱方向和距离的最大范围。")]
        [Min(1f)] public float trackingDistance = 50f;

        private EncounterTrigger encounter;
        private Transform visual;
        private SpriteRenderer visualRenderer;
        private Transform glow;
        private SpriteRenderer glowRenderer;
        private Vector3 baseVisualPosition;
        private Vector3 baseVisualScale;
        private GUIStyle titleStyle;
        private GUIStyle hintStyle;
        private GUIStyle arrowStyle;
        private GUIStyle edgeStyle;

        private static Sprite glowSprite;

        private static readonly Color PanelColor = new Color(0.055f, 0.038f, 0.012f, 0.92f);
        private static readonly Color Gold = new Color(1f, 0.69f, 0.12f, 1f);
        private static readonly Color NearGold = new Color(1f, 0.9f, 0.36f, 1f);

        private void Awake()
        {
            encounter = GetComponent<EncounterTrigger>();
            visualRenderer = GetComponentInChildren<SpriteRenderer>();
            if (visualRenderer == null)
            {
                return;
            }

            visual = visualRenderer.transform;
            baseVisualPosition = visual.localPosition;
            baseVisualScale = visual.localScale;
            EnsureGlow();
        }

        private void Update()
        {
            if (encounter == null || encounter.encounterType != EncounterType.Treasure || visual == null)
            {
                return;
            }

            float wave = Mathf.Sin(Time.unscaledTime * pulseSpeed);
            float scalePulse = 1f + wave * 0.045f;
            visual.localPosition = baseVisualPosition + Vector3.up * (bobHeight + wave * bobHeight);
            visual.localScale = baseVisualScale * (visualScaleMultiplier * scalePulse);

            if (visualRenderer != null)
            {
                float shimmer = 0.5f + wave * 0.5f;
                visualRenderer.color = Color.Lerp(new Color(1f, 0.75f, 0.22f), Color.white, shimmer * 0.72f);
            }

            if (glow != null && glowRenderer != null)
            {
                float glowPulse = glowScale * (1f + wave * 0.12f);
                glow.localScale = Vector3.one * glowPulse;
                glowRenderer.color = new Color(1f, 0.66f, 0.08f, 0.28f + Shimmer01(wave) * 0.18f);
            }
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            GameFlowController gameFlow = GameFlowController.Instance;
            if (encounter == null || encounter.consumed ||
                encounter.encounterType != EncounterType.Treasure ||
                gameFlow == null || gameFlow.CurrentPhase != GamePhase.MainMapRunning)
            {
                return;
            }

            Camera worldCamera = Camera.main;
            if (worldCamera == null || gameFlow.playerController == null)
            {
                return;
            }

            float playerDistance = Vector3.Distance(
                gameFlow.playerController.transform.position, transform.position);
            bool isInsideCamera = IsChestInsideCamera(worldCamera);
            if (!isInsideCamera)
            {
                float effectiveTrackingDistance = gameFlow.cameraFollow != null
                    ? gameFlow.cameraFollow.GetAwarenessDistance(trackingDistance)
                    : trackingDistance;
                if (playerDistance <= effectiveTrackingDistance)
                {
                    DrawEdgeIndicator(worldCamera, playerDistance);
                }

                return;
            }

            if (playerDistance > maxVisibleDistance)
            {
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(transform.position + Vector3.up * worldHeight);
            float guiScale = ResponsiveGui.Scale;
            Vector2 guiPoint = ResponsiveGui.ScreenPointToGui(screenPoint, guiScale);
            float guiX = guiPoint.x;
            float guiY = guiPoint.y;

            bool playerIsNear = playerDistance <= nearDistance;
            float pulse = 0.76f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.18f;
            float width = playerIsNear ? 164f : 148f;
            float panelY = guiY - 45f;
            float leftHudWidth = Mathf.Min(330f, ResponsiveGui.Width * 0.48f);
            if (guiX < leftHudWidth && panelY < 126f)
            {
                panelY = 126f;
            }
            Rect panel = new Rect(guiX - width * 0.5f, panelY, width, 42f);

            EnsureStyles();
            GUI.depth = -118;
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(guiScale);
            DrawPanel(panel, playerIsNear ? NearGold : Gold, pulse);

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 6f, panel.y + 2f, panel.width - 12f, 20f),
                "◆ 珍藏宝箱 ◆", titleStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 6f, panel.y + 21f, panel.width - 12f, 18f),
                playerIsNear ? "靠近即可开启" : "装备 · 修为 · 铜钱", hintStyle, 9);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.78f, 0.16f, pulse);
            GUI.Label(new Rect(guiX - 18f, panel.yMax - 3f, 36f, 26f), "▼", arrowStyle);
            GUI.color = previousColor;
            GUI.matrix = originalGuiMatrix;
        }

        private bool IsChestInsideCamera(Camera worldCamera)
        {
            if (visualRenderer == null || !visualRenderer.enabled ||
                !visualRenderer.gameObject.activeInHierarchy)
            {
                return false;
            }

            return WorldIndicatorUtility.IsInsideCamera(worldCamera, visualRenderer.bounds.center);
        }

        private void DrawEdgeIndicator(Camera worldCamera, float playerDistance)
        {
            EnsureStyles();
            float guiScale = ResponsiveGui.Scale;
            Vector3 anchor = transform.position + Vector3.up * worldHeight;
            Vector2 markerPoint = WorldIndicatorUtility.GetClampedGuiPoint(
                worldCamera, anchor, guiScale, out Vector2 direction);
            string arrow = WorldIndicatorUtility.DirectionArrow(direction);
            string label = $"{arrow} 宝箱  {Mathf.CeilToInt(playerDistance)}m";
            Rect panel = new Rect(markerPoint.x - 53f, markerPoint.y - 14f, 106f, 28f);
            float pulse = 0.78f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.18f;

            GUI.depth = -122;
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(guiScale);
            Color previous = GUI.color;
            GUI.color = new Color(0.055f, 0.038f, 0.012f, 0.9f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(NearGold.r, NearGold.g, NearGold.b, pulse);
            GUI.DrawTexture(new Rect(panel.x, panel.y, 3f, panel.height), Texture2D.whiteTexture);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 6f, panel.y + 2f, panel.width - 10f, panel.height - 4f),
                label, edgeStyle, 10);
            GUI.color = previous;
            GUI.matrix = originalGuiMatrix;
        }

        private void EnsureGlow()
        {
            GameObject glowObject = new GameObject("TreasureGlow");
            glowObject.transform.SetParent(visual, false);
            glowObject.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            glowObject.transform.localScale = Vector3.one * glowScale;

            glow = glowObject.transform;
            glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = GetGlowSprite();
            glowRenderer.color = new Color(1f, 0.66f, 0.08f, 0.36f);
            glowRenderer.sortingOrder = visualRenderer.sortingOrder - 1;
        }

        private static Sprite GetGlowSprite()
        {
            if (glowSprite != null)
            {
                return glowSprite;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Treasure Glow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Pow(1f - Mathf.Clamp01(distance), 2.1f);
                    pixels[y * size + x] = new Color(1f, 0.72f, 0.12f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            glowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
            glowSprite.name = "Runtime Treasure Glow";
            return glowSprite;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.83f, 0.28f) }
            });
            hintStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
            arrowStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
            edgeStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.86f, 0.42f) }
            });
        }

        private static float Shimmer01(float wave)
        {
            return wave * 0.5f + 0.5f;
        }

        private static void DrawPanel(Rect rect, Color borderColor, float pulse)
        {
            Color previousColor = GUI.color;
            GUI.color = PanelColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = new Color(borderColor.r, borderColor.g, borderColor.b, pulse);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
