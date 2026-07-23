using UnityEngine;
using WuxiaRoguelite.Map;

namespace WuxiaRoguelite.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EncounterTrigger))]
    public class EnemyLevelLabel : MonoBehaviour
    {
        private EncounterTrigger encounter;
        private SpriteRenderer spriteRenderer;
        private GUIStyle labelStyle;
        private GUIStyle shadowStyle;

        private void Awake()
        {
            encounter = GetComponent<EncounterTrigger>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnGUI()
        {
            if (encounter == null || encounter.consumed ||
                (encounter.encounterType != EncounterType.NormalEnemy &&
                 encounter.encounterType != EncounterType.EliteEnemy))
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
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            EnsureStyle();
            GUI.depth = -100;
            Rect labelRect = new Rect(screenPoint.x - 24f, Screen.height - screenPoint.y - 9f, 48f, 18f);
            string levelText = $"Lv.{encounter.enemyStats.DisplayLevel}";
            GUI.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height),
                levelText, shadowStyle);

            Color previous = GUI.color;
            GUI.color = encounter.encounterType == EncounterType.EliteEnemy
                ? new Color(1f, 0.62f, 0.55f)
                : new Color(1f, 0.90f, 0.64f);
            GUI.Label(labelRect, levelText, labelStyle);
            GUI.color = previous;
        }

        private void EnsureStyle()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            shadowStyle = new GUIStyle(labelStyle)
            {
                normal = { textColor = new Color(0f, 0f, 0f, 0.82f) }
            };
        }
    }
}
