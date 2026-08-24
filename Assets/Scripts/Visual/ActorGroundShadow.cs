using UnityEngine;
using UnityEngine.Rendering;

namespace WuxiaRoguelite.Visual
{
    [DisallowMultipleComponent]
    public sealed class ActorGroundShadow : MonoBehaviour
    {
        public Transform visualRoot;
        public Material shadowMaterial;
        public Vector2 baseSize = new Vector2(0.55f, 0.25f);
        [Range(0f, 1f)] public float opacity = 0.34f;
        public Color shadowColor = new Color(0.08f, 0.12f, 0.10f, 1f);

        private const float SurfaceOffset = 0.025f;
        private static Mesh sharedQuad;

        private Transform shadowTransform;
        private MeshRenderer shadowRenderer;
        private MaterialPropertyBlock propertyBlock;
        private float baseVisualY;

        private void Awake()
        {
            ResolveVisualRoot();
            baseVisualY = visualRoot != null ? visualRoot.localPosition.y : 0f;
            EnsureShadow();
            UpdateShadowTransform();
        }

        private void OnEnable()
        {
            EnsureShadow();
        }

        private void LateUpdate()
        {
            UpdateShadowTransform();
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            visualRoot = spriteRenderer != null ? spriteRenderer.transform : null;
        }

        private void EnsureShadow()
        {
            if (shadowTransform != null || shadowMaterial == null)
            {
                return;
            }

            Transform existing = transform.Find("Actor Ground Shadow");
            if (existing != null)
            {
                shadowTransform = existing;
                shadowRenderer = existing.GetComponent<MeshRenderer>();
            }
            else
            {
                GameObject shadowObject = new GameObject("Actor Ground Shadow");
                shadowObject.transform.SetParent(transform, false);
                shadowTransform = shadowObject.transform;
                MeshFilter filter = shadowObject.AddComponent<MeshFilter>();
                filter.sharedMesh = GetSharedQuad();
                shadowRenderer = shadowObject.AddComponent<MeshRenderer>();
            }

            if (shadowRenderer == null)
            {
                return;
            }

            shadowRenderer.sharedMaterial = shadowMaterial;
            shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
            shadowRenderer.lightProbeUsage = LightProbeUsage.Off;
            shadowRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.SetColor("_Color", new Color(
                shadowColor.r,
                shadowColor.g,
                shadowColor.b,
                opacity));
            shadowRenderer.SetPropertyBlock(propertyBlock);
        }

        private void UpdateShadowTransform()
        {
            if (shadowTransform == null)
            {
                return;
            }

            float visualScale = visualRoot != null ? visualRoot.localScale.x : 1f;
            float bridgeLift = visualRoot != null
                ? Mathf.Max(0f, visualRoot.localPosition.y - baseVisualY)
                : 0f;
            shadowTransform.localPosition = new Vector3(0f, bridgeLift + SurfaceOffset, 0f);
            shadowTransform.localRotation = Quaternion.identity;
            shadowTransform.localScale = new Vector3(
                baseSize.x * visualScale,
                1f,
                baseSize.y * visualScale);

            if (shadowRenderer != null && visualRoot != null)
            {
                shadowRenderer.enabled = visualRoot.gameObject.activeInHierarchy;
            }
        }

        private static Mesh GetSharedQuad()
        {
            if (sharedQuad != null)
            {
                return sharedQuad;
            }

            sharedQuad = new Mesh
            {
                name = "Runtime Actor Ground Shadow Quad",
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0.5f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            sharedQuad.RecalculateNormals();
            sharedQuad.RecalculateBounds();
            return sharedQuad;
        }
    }
}
