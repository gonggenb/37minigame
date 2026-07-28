using UnityEngine;

namespace WuxiaRoguelite.CameraTools
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(6f, 7.2f, -10.5f);
        public Vector3 portraitOffset = new Vector3(7.8f, 9.5f, -14f);
        public float lookAtHeight = 0.85f;
        public float smoothTime = 0.16f;
        [Range(25f, 70f)] public float landscapeFieldOfView = 40f;
        [Range(25f, 70f)] public float portraitFieldOfView = 46f;

        [Header("Run Vision")]
        [Range(0.4f, 1.5f)] public float initialVisionScale = 0.74f;
        [Range(0.5f, 2f)] public float maximumVisionScale = 1.08f;

        public float VisionScale { get; private set; }
        public int VisionPercent => Mathf.RoundToInt(VisionScale * 100f);

        private Vector3 velocity;
        private Camera attachedCamera;

        private void Awake()
        {
            attachedCamera = GetComponent<Camera>();
            ResetVision();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            bool portrait = IsPortraitLayout();
            Vector3 responsiveOffset = portrait ? portraitOffset : offset;
            Vector3 targetPosition = target.position + responsiveOffset * VisionScale;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            ApplyFieldOfView(portrait, false);

            Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        public void ResetVision()
        {
            VisionScale = Mathf.Clamp(initialVisionScale, 0.4f, maximumVisionScale);
            velocity = Vector3.zero;
            SnapToTarget();
        }

        public int ExpandVision(float amount)
        {
            VisionScale = Mathf.Clamp(
                VisionScale + Mathf.Max(0f, amount),
                initialVisionScale,
                maximumVisionScale);
            return VisionPercent;
        }

        public float GetAwarenessDistance(float configuredMaximum)
        {
            float progress = Mathf.InverseLerp(initialVisionScale, maximumVisionScale, VisionScale);
            float explorationRange = Mathf.Lerp(6.5f, 16f, progress);
            return Mathf.Min(Mathf.Max(1f, configuredMaximum), explorationRange);
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            bool portrait = IsPortraitLayout();
            Vector3 responsiveOffset = portrait ? portraitOffset : offset;
            transform.position = target.position + responsiveOffset * VisionScale;
            ApplyFieldOfView(portrait, true);
            Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }

        private void ApplyFieldOfView(bool portrait, bool immediate)
        {
            if (attachedCamera == null)
            {
                attachedCamera = GetComponent<Camera>();
            }

            if (attachedCamera == null || attachedCamera.orthographic)
            {
                return;
            }

            float targetFieldOfView = portrait ? portraitFieldOfView : landscapeFieldOfView;
            attachedCamera.fieldOfView = immediate
                ? targetFieldOfView
                : Mathf.Lerp(
                    attachedCamera.fieldOfView,
                    targetFieldOfView,
                    1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f));
        }

        private bool IsPortraitLayout()
        {
            if (attachedCamera != null && attachedCamera.pixelWidth > 0 && attachedCamera.pixelHeight > 0)
            {
                return attachedCamera.pixelHeight > attachedCamera.pixelWidth;
            }

            return Screen.height > Screen.width;
        }
    }
}
