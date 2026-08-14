using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;

namespace WuxiaRoguelite.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EncounterTrigger))]
    public class MapPickupIndicator : MonoBehaviour
    {
        [Min(1f)] public float revealDistance = 7.5f;
        [Min(0f)] public float worldHeight = 2f;

        private EncounterTrigger encounter;
        private SpriteRenderer visualRenderer;
        private GUIStyle titleStyle;
        private GUIStyle detailStyle;

        private void Awake()
        {
            encounter = GetComponent<EncounterTrigger>();
            visualRenderer = GetComponentInChildren<SpriteRenderer>();
            TintVisual();
        }

        private void OnGUI()
        {
            RuntimeChineseFont.PrepareSkin();

            GameFlowController gameFlow = GameFlowController.Instance;
            if (encounter == null || encounter.consumed ||
                gameFlow == null || gameFlow.CurrentPhase != GamePhase.MainMapRunning ||
                gameFlow.playerController == null || !IsPickupType(encounter.encounterType))
            {
                return;
            }

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return;
            }

            float distance = Vector3.Distance(
                gameFlow.playerController.transform.position,
                transform.position);
            Vector3 anchor = visualRenderer != null
                ? new Vector3(
                    visualRenderer.bounds.center.x,
                    visualRenderer.bounds.max.y + 0.2f,
                    visualRenderer.bounds.center.z)
                : transform.position + Vector3.up * worldHeight;
            if (distance > revealDistance ||
                !WorldIndicatorUtility.IsInsideCamera(worldCamera, anchor))
            {
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            GetPresentation(out string title, out string detail, out Color accent);
            EnsureStyles();

            float guiScale = ResponsiveGui.Scale;
            Vector2 guiPoint = ResponsiveGui.ScreenPointToGui(screenPoint, guiScale);
            Rect panel = new Rect(guiPoint.x - 72f, guiPoint.y - 27f, 144f, 42f);

            GUI.depth = -116;
            Matrix4x4 originalGuiMatrix = ResponsiveGui.ApplyScale(guiScale);
            Color previous = GUI.color;
            GUI.color = new Color(0.025f, 0.035f, 0.03f, 0.9f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(panel.x, panel.y, 3f, panel.height), Texture2D.whiteTexture);
            GUI.color = previous;

            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 7f, panel.y + 2f, panel.width - 12f, 19f),
                title, titleStyle, 10);
            ResponsiveGui.DrawSingleLineLabel(
                new Rect(panel.x + 7f, panel.y + 20f, panel.width - 12f, 18f),
                detail, detailStyle, 8);
            GUI.matrix = originalGuiMatrix;
        }

        private static bool IsPickupType(EncounterType type)
        {
            return type == EncounterType.Herb ||
                   type == EncounterType.VisionRelic ||
                   type == EncounterType.MysteryHerb;
        }

        private void GetPresentation(out string title, out string detail, out Color accent)
        {
            switch (encounter.encounterType)
            {
                case EncounterType.VisionRelic:
                    title = "◇ 望气灵物";
                    detail = "永久扩大本局视野";
                    accent = new Color(0.38f, 0.78f, 1f);
                    return;
                case EncounterType.MysteryHerb:
                    title = "◆ 无名奇草";
                    detail = "大量修为 · 可能中毒";
                    accent = new Color(0.82f, 0.4f, 0.92f);
                    return;
                default:
                    switch (encounter.herbEffect)
                    {
                        case HerbEffectType.Attack:
                            title = "◆ 赤阳草";
                            detail = "本局攻击提升";
                            accent = new Color(1f, 0.48f, 0.28f);
                            return;
                        case HerbEffectType.Defense:
                            title = "◆ 铁骨草";
                            detail = "本局防御提升";
                            accent = new Color(0.78f, 0.72f, 0.55f);
                            return;
                        case HerbEffectType.MoveSpeed:
                            title = "◆ 轻身草";
                            detail = "本局移速提升";
                            accent = new Color(0.42f, 0.82f, 0.84f);
                            return;
                        default:
                            title = "◆ 止血草";
                            detail = "恢复部分气血";
                            accent = new Color(0.42f, 0.86f, 0.52f);
                            return;
                    }
            }
        }

        private void TintVisual()
        {
            if (encounter == null || visualRenderer == null)
            {
                return;
            }

            switch (encounter.encounterType)
            {
                case EncounterType.VisionRelic:
                    visualRenderer.color = new Color(0.52f, 0.86f, 1f);
                    break;
                case EncounterType.MysteryHerb:
                    visualRenderer.color = new Color(0.82f, 0.48f, 0.95f);
                    break;
                default:
                    if (encounter.herbEffect == HerbEffectType.Attack)
                    {
                        visualRenderer.color = new Color(1f, 0.58f, 0.42f);
                    }
                    break;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
            detailStyle = RuntimeChineseFont.Apply(new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.82f, 0.84f, 0.8f) }
            });
        }
    }
}
