using System.Collections.Generic;
using UnityEngine;

namespace WuxiaRoguelite.Map
{
    [DisallowMultipleComponent]
    public sealed class MainMapBridgeSurface : MonoBehaviour
    {
        [Min(0.1f)] public float halfLength = 2.1f;
        [Min(0.1f)] public float halfWidth = 0.82f;
        [Min(0f)] public float maximumVisualRise = 1.02f;

        private static readonly HashSet<MainMapBridgeSurface> ActiveSurfaces =
            new HashSet<MainMapBridgeSurface>();

        private void OnEnable()
        {
            ActiveSurfaces.Add(this);
        }

        private void OnDisable()
        {
            ActiveSurfaces.Remove(this);
        }

        public static float GetVisualLift(Vector3 worldPosition)
        {
            float lift = 0f;
            foreach (MainMapBridgeSurface surface in ActiveSurfaces)
            {
                if (surface != null && surface.TryGetVisualLift(worldPosition, out float candidate))
                {
                    lift = Mathf.Max(lift, candidate);
                }
            }

            return lift;
        }

        public bool TryGetVisualLift(Vector3 worldPosition, out float lift)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            if (Mathf.Abs(local.x) > halfWidth || Mathf.Abs(local.z) > halfLength)
            {
                lift = 0f;
                return false;
            }

            float normalizedDistance = Mathf.Clamp01(Mathf.Abs(local.z) / halfLength);
            float arch = 1f - normalizedDistance * normalizedDistance;
            lift = arch * maximumVisualRise;
            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.28f, 0.82f, 0.74f, 0.5f);
            Gizmos.DrawWireCube(
                new Vector3(0f, maximumVisualRise * 0.5f, 0f),
                new Vector3(halfWidth * 2f, maximumVisualRise, halfLength * 2f));
        }
#endif
    }
}
