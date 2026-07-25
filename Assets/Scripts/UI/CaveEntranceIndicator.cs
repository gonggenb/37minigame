using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;

namespace WuxiaRoguelite.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EncounterTrigger))]
    public class CaveEntranceIndicator : MonoBehaviour
    {
        [Min(1f)] public float worldHeight = 2.7f;
        [Min(1f)] public float nearDistance = 4.5f;
        [Min(1f)] public float maxVisibleDistance = 16f;

        private EncounterTrigger encounter;
        private GUIStyle titleStyle;
        private GUIStyle hintStyle;
        private GUIStyle arrowStyle;

        private static readonly Color PanelColor = new Color(0.055f, 0.045f, 0.03f, 0.9f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.26f, 1f);
        private static readonly Color NearGold = new Color(1f, 0.9f, 0.42f, 1f);

        private void Awake()
        {
            encounter = GetComponent<EncounterTrigger>();
        }

        private void OnGUI()
        {
            GameFlowController gameFlow = GameFlowController.Instance;
            if (encounter == null || encounter.consumed ||
                encounter.encounterType != EncounterType.HiddenCave ||
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
            if (playerDistance > maxVisibleDistance)
            {
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(
                transform.position + Vector3.up * worldHeight);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            float guiX = screenPoint.x;
            float guiY = Screen.height - screenPoint.y;
            if (guiX < -90f || guiX > Screen.width + 90f || guiY < -60f || guiY > Screen.height + 60f)
            {
                return;
            }
            if (!IsClosestVisibleEntrance(gameFlow.playerController.transform.position, worldCamera))
            {
                return;
            }

            bool playerIsNear = playerDistance <= nearDistance;
            float pulse = 0.72f + Mathf.Sin(Time.unscaledTime * 4f) * 0.18f;
            float width = playerIsNear ? 156f : 132f;
            Rect panel = new Rect(guiX - width * 0.5f, guiY - 42f, width, 40f);

            EnsureStyles();
            GUI.depth = -120;
            DrawPanel(panel, playerIsNear ? NearGold : Gold, pulse);

            GUI.Label(new Rect(panel.x + 6f, panel.y + 2f, panel.width - 12f, 19f),
                "◆ 山洞入口 ◆", titleStyle);
            GUI.Label(new Rect(panel.x + 6f, panel.y + 20f, panel.width - 12f, 17f),
                playerIsNear ? "靠近即可进入" : "可探索区域", hintStyle);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.82f, 0.3f, pulse);
            GUI.Label(new Rect(guiX - 16f, guiY - 4f, 32f, 24f), "▼", arrowStyle);
            GUI.color = previousColor;
        }

        private bool IsClosestVisibleEntrance(Vector3 playerPosition, Camera worldCamera)
        {
            float distanceSqr = (transform.position - playerPosition).sqrMagnitude;
            CaveEntranceIndicator[] indicators =
                FindObjectsByType<CaveEntranceIndicator>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (CaveEntranceIndicator indicator in indicators)
            {
                if (indicator == this || !indicator.isActiveAndEnabled ||
                    indicator.encounter == null || indicator.encounter.consumed)
                {
                    continue;
                }

                float otherDistanceSqr = (indicator.transform.position - playerPosition).sqrMagnitude;
                if (otherDistanceSqr > indicator.maxVisibleDistance * indicator.maxVisibleDistance)
                {
                    continue;
                }

                Vector3 otherScreenPoint = worldCamera.WorldToScreenPoint(
                    indicator.transform.position + Vector3.up * indicator.worldHeight);
                if (otherScreenPoint.z <= 0f ||
                    otherScreenPoint.x < -90f || otherScreenPoint.x > Screen.width + 90f ||
                    otherScreenPoint.y < -60f || otherScreenPoint.y > Screen.height + 60f)
                {
                    continue;
                }

                if (otherDistanceSqr + 0.01f < distanceSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.84f, 0.38f) }
            };
            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            arrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
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
