using UnityEngine;

namespace WuxiaRoguelite.Map
{
    /// <summary>Matches the authored Blender model after Y=180 rotation and -0.4 m height offset.</summary>
    public static class BambooValleyLayout
    {
        public static readonly Vector3 Spawn = new Vector3(-20f, 0f, -16f);
        public static readonly float[] BridgeZ = { -6f, 7f };
        public static readonly float[] BridgeWidths = { 2.7f, 1.9f };

        public static float RiverX(float z) => -7f + 2.1f * Mathf.Sin(z * 0.16f) + Mathf.Max(0f, -z - 4f) * 0.48f;

        public static bool IsBridge(float x, float z, float inset = 0f)
        {
            for (int i = 0; i < BridgeZ.Length; i++)
                if (Mathf.Abs(z - BridgeZ[i]) <= BridgeWidths[i] * 0.5f - inset &&
                    Mathf.Abs(x - RiverX(BridgeZ[i])) <= 3.7f)
                    return true;
            return false;
        }

        public static bool IsInsideRiver(Vector3 p, float padding = 0f) =>
            Mathf.Abs(p.x - RiverX(p.z)) < 2.35f + padding && !IsBridge(p.x, p.z, padding);

        public static bool IsInsideBounds(Vector3 p, float padding = 0f) =>
            Mathf.Pow(Mathf.Abs(p.x) / (23f - padding), 4f) +
            Mathf.Pow(Mathf.Abs(p.z) / (20f - padding), 4f) < 1f;

        public static float SurfaceHeight(float x, float z)
        {
            for (int i = 0; i < BridgeZ.Length; i++)
            {
                float distance = Mathf.Abs(x - RiverX(BridgeZ[i]));
                if (distance <= 3.6f && Mathf.Abs(z - BridgeZ[i]) <= BridgeWidths[i] * 0.5f)
                    return 0.14f + 0.65f * Mathf.Cos(distance / 3.6f * Mathf.PI * 0.5f);
            }
            if (new Vector2(x, z + 2f).magnitude < 3.15f) return 0.45f;
            if (new Vector2(x - 15f, z - 12f).magnitude < 5.4f) return 0.57f;
            float height = 0.12f * Mathf.Sin(x * 0.36f) * Mathf.Cos(z * 0.32f);
            height += Mathf.Max(0f, Mathf.Max(Mathf.Abs(x) / 25f, Mathf.Abs(z) / 22f) - 0.74f) * 2.4f;
            // Paving is 0.13 m above the authored terrain; visual-only rise keeps encounter roots planar.
            return height + 0.13f;
        }
    }
}
