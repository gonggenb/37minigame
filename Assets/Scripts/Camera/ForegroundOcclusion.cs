using UnityEngine;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Player;

namespace WuxiaRoguelite.CameraTools
{
    /// <summary>A camera-scoped opening in foreground scenery, centred on the visible actor.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class ForegroundOcclusion : MonoBehaviour
    {
        [Min(0.5f)] public float radius = 2f;
        [Min(0f)] public float centreHeight = 0.95f;
        private CameraFollow follow;
        private Transform cachedTarget;
        private PlayerController player;
        private float strength;
        private static readonly int Anchor = Shader.PropertyToID("_ForegroundAnchor");
        private static readonly int Opening = Shader.PropertyToID("_ForegroundOpening");

        private void Awake() => follow = GetComponent<CameraFollow>();

        private bool IsExploring => follow != null && follow.isActiveAndEnabled &&
            follow.target != null && GameFlowController.Instance != null &&
            GameFlowController.Instance.CurrentPhase == GamePhase.MainMapRunning;

        private void LateUpdate()
        {
            strength = Mathf.MoveTowards(strength, IsExploring ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        }

        // Set after camera following and actor height updates, and only for this camera's render.
        private void OnPreRender()
        {
            ClearGlobals();
            if (!IsExploring) return;
            if (cachedTarget != follow.target)
            {
                cachedTarget = follow.target;
                player = cachedTarget.GetComponent<PlayerController>();
            }
            Vector3 foot = player != null && player.visualRoot != null
                ? player.visualRoot.position : cachedTarget.position;
            Shader.SetGlobalVector(Anchor, new Vector4(foot.x, foot.y + centreHeight, foot.z, strength));
            Shader.SetGlobalVector(Opening, new Vector4(radius, radius * 1.15f, foot.y, 0f));
        }

        private static void ClearGlobals() => Shader.SetGlobalVector(Anchor, Vector4.zero);
        private void OnPostRender() => ClearGlobals();
        private void OnDisable() { strength = 0f; ClearGlobals(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetGlobals() => ClearGlobals();
    }
}
