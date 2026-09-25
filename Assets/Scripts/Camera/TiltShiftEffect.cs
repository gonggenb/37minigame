using UnityEngine;
using UnityEngine.Rendering;
using WuxiaRoguelite.GameFlow;

namespace WuxiaRoguelite.CameraTools
{
    /// <summary>Built-in pipeline scenery blur. Transparent actors and UI render afterwards.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class TiltShiftEffect : MonoBehaviour
    {
        public const string PreferenceKey = "WuxiaRoguelite.TiltShift.v1";
        [Range(0f, 1f)] public float intensity = 0.7f;
        [Range(0.1f, 0.4f)] public float focusHalfHeight = 0.22f;
        [Range(0.05f, 0.4f)] public float feather = 0.22f;
        [Range(0.5f, 4f)] public float blurRadius = 2.4f;

        private static bool? savedPreference;
        private Camera view;
        private CameraFollow follow;
        private Material material;
        private bool shaderUnavailable;
        private static readonly int BlurDirection = Shader.PropertyToID("_BlurDirection");
        private static readonly int BlurTexture = Shader.PropertyToID("_BlurTex");
        private static readonly int Focus = Shader.PropertyToID("_Focus");

        public static bool UserEnabled => savedPreference ??
            (savedPreference = PlayerPrefs.GetInt(PreferenceKey, 1) != 0).Value;

        public static void SetUserEnabled(bool value)
        {
            savedPreference = value;
            PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPreferenceCache() => savedPreference = null;

        private void Awake()
        {
            view = GetComponent<Camera>();
            follow = GetComponent<CameraFollow>();
        }

        // CameraFollow also calls this while this component is disabled, so turning
        // the preference off removes the image-effect render targets altogether.
        public void RefreshEnabledState()
        {
            var flow = GameFlowController.Instance;
            enabled = UserEnabled && intensity > 0f && !shaderUnavailable &&
                GraphicsSettings.currentRenderPipeline == null && follow != null &&
                follow.enabled && follow.target != null && flow != null &&
                flow.CurrentPhase == GamePhase.MainMapRunning;
        }

        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!EnsureMaterial())
            {
                Graphics.Blit(source, destination);
                return;
            }

            Vector3 anchor = view.WorldToViewportPoint(follow.target.position + Vector3.up * follow.lookAtHeight);
            if (anchor.z <= 0f)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // Preserve source color space/format, but never allocate MSAA, depth or mips.
            var descriptor = source.descriptor;
            descriptor.width = Mathf.Max(1, source.width / 2);
            descriptor.height = Mathf.Max(1, source.height / 2);
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            RenderTexture horizontal = null, vertical = null;
            try
            {
                horizontal = RenderTexture.GetTemporary(descriptor);
                vertical = RenderTexture.GetTemporary(descriptor);
                horizontal.filterMode = vertical.filterMode = FilterMode.Bilinear;
                horizontal.wrapMode = vertical.wrapMode = TextureWrapMode.Clamp;
                float radius = Mathf.Clamp(blurRadius, 0.5f, 4f) *
                    Mathf.Min(source.width, source.height) / 540f;
                material.SetVector(BlurDirection, new Vector4(radius / source.width, 0f, 0f, 0f));
                Graphics.Blit(source, horizontal, material, 0);
                material.SetVector(BlurDirection, new Vector4(0f, radius / source.height, 0f, 0f));
                Graphics.Blit(horizontal, vertical, material, 0);
                material.SetTexture(BlurTexture, vertical);
                material.SetVector(Focus, new Vector4(Mathf.Clamp01(anchor.y),
                    Mathf.Clamp(focusHalfHeight, 0.1f, 0.4f),
                    Mathf.Clamp(feather, 0.05f, 0.4f), Mathf.Clamp01(intensity)));
                Graphics.Blit(source, destination, material, 1);
            }
            finally
            {
                material.SetTexture(BlurTexture, null);
                if (horizontal != null) RenderTexture.ReleaseTemporary(horizontal);
                if (vertical != null) RenderTexture.ReleaseTemporary(vertical);
            }
        }

        private bool EnsureMaterial()
        {
            if (material != null) return true;
            if (shaderUnavailable) return false;
            Shader shader = Resources.Load<Shader>("Camera/TiltShift");
            if (shader == null || !shader.isSupported)
            {
                shaderUnavailable = true;
                Debug.LogWarning("Tilt shift shader unavailable; keeping the original camera image.", this);
                return false;
            }
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return true;
        }

        private void OnDisable()
        {
            if (material == null) return;
            Destroy(material);
            material = null;
        }
    }
}
