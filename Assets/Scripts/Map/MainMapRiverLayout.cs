using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>
    /// Shared geometry for the main-world river. Scene authoring, validation and
    /// future runtime spawning must all use this layout so gameplay objects stay
    /// out of the water and crossings remain limited to bridges.
    /// </summary>
    public static class MainMapRiverLayout
    {
        public static readonly Vector2[] CenterLine =
        {
            new Vector2(-34f, -8.2f),
            new Vector2(-27f, -7.4f),
            new Vector2(-20f, -6.2f),
            new Vector2(-12f, -5.7f),
            new Vector2(-5f, -4.6f),
            new Vector2(2f, -4.1f),
            new Vector2(9f, -3.2f),
            new Vector2(17f, -2.8f),
            new Vector2(25f, -1.4f),
            new Vector2(34f, -0.4f)
        };

        public static readonly float[] HalfWidths =
        {
            1.35f, 1.65f, 1.25f, 1.55f, 1.25f,
            1.45f, 1.25f, 1.55f, 1.3f, 1.5f
        };

        public static readonly int[] BridgePointIndices = { 2, 5, 8 };
        public static readonly string[] BridgeNames =
        {
            "West Forest Bridge",
            "Central Courier Bridge",
            "East Hamlet Bridge"
        };

        public const float BridgeGapHalfLength = 1.55f;
        public const float BarrierBankPadding = 0.18f;

        public static bool IsInsideRiver(Vector3 worldPosition, float clearance = 0f)
        {
            return TryGetNearestPoint(
                       worldPosition,
                       out _,
                       out _,
                       out float halfWidth,
                       out _,
                       out float distance) &&
                   distance < halfWidth + Mathf.Max(0f, clearance);
        }

        public static Vector3 GetNearestSafeBankPosition(Vector3 worldPosition, float clearance)
        {
            if (!TryGetNearestPoint(
                    worldPosition,
                    out Vector3 nearest,
                    out Vector3 tangent,
                    out float halfWidth,
                    out _,
                    out _))
            {
                return worldPosition;
            }

            Vector3 bankNormal = new Vector3(-tangent.z, 0f, tangent.x).normalized;
            float side = Vector3.Dot(worldPosition - nearest, bankNormal);
            if (Mathf.Abs(side) < 0.01f)
            {
                side = worldPosition.z >= nearest.z ? 1f : -1f;
            }

            Vector3 result = nearest + bankNormal * Mathf.Sign(side) *
                (halfWidth + Mathf.Max(0f, clearance));
            result.y = worldPosition.y;
            return result;
        }

        public static bool TryGetNearestPoint(
            Vector3 worldPosition,
            out Vector3 nearestPoint,
            out Vector3 tangent,
            out float halfWidth,
            out float distanceAlongRiver,
            out float distance)
        {
            nearestPoint = worldPosition;
            tangent = Vector3.right;
            halfWidth = 0f;
            distanceAlongRiver = 0f;
            distance = float.MaxValue;

            if (CenterLine.Length < 2 || CenterLine.Length != HalfWidths.Length)
            {
                return false;
            }

            Vector2 point = new Vector2(worldPosition.x, worldPosition.z);
            float traversed = 0f;
            for (int i = 0; i < CenterLine.Length - 1; i++)
            {
                Vector2 delta = CenterLine[i + 1] - CenterLine[i];
                float segmentLength = delta.magnitude;
                if (segmentLength <= 0.001f)
                {
                    continue;
                }

                float t = Mathf.Clamp01(Vector2.Dot(point - CenterLine[i], delta) / delta.sqrMagnitude);
                Vector2 candidate = CenterLine[i] + delta * t;
                float candidateDistance = Vector2.Distance(point, candidate);
                if (candidateDistance < distance)
                {
                    distance = candidateDistance;
                    nearestPoint = new Vector3(candidate.x, worldPosition.y, candidate.y);
                    tangent = new Vector3(delta.x, 0f, delta.y).normalized;
                    halfWidth = Mathf.Lerp(HalfWidths[i], HalfWidths[i + 1], t);
                    distanceAlongRiver = traversed + segmentLength * t;
                }

                traversed += segmentLength;
            }

            return distance < float.MaxValue;
        }

        public static float[] GetBridgeDistances()
        {
            float[] cumulative = GetCumulativeDistances();
            float[] result = new float[BridgePointIndices.Length];
            for (int i = 0; i < result.Length; i++)
            {
                int index = Mathf.Clamp(BridgePointIndices[i], 0, cumulative.Length - 1);
                result[i] = cumulative[index];
            }
            return result;
        }

        public static float[] GetCumulativeDistances()
        {
            float[] cumulative = new float[CenterLine.Length];
            for (int i = 1; i < cumulative.Length; i++)
            {
                cumulative[i] = cumulative[i - 1] + Vector2.Distance(CenterLine[i - 1], CenterLine[i]);
            }
            return cumulative;
        }
    }
}
