using UnityEngine;

namespace WuxiaRoguelite.CameraTools
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(6f, 7.2f, -10.5f);
        public float lookAtHeight = 0.85f;
        public float smoothTime = 0.16f;

        [Header("Run Vision")]
        [Range(0.4f, 1.5f)] public float initialVisionScale = 0.74f;
        [Range(0.5f, 2f)] public float maximumVisionScale = 1.08f;

        public float VisionScale { get; private set; }
        public int VisionPercent => Mathf.RoundToInt(VisionScale * 100f);

        private Vector3 velocity;

        private void Awake()
        {
            ResetVision();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position + offset * VisionScale;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

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

            transform.position = target.position + offset * VisionScale;
            Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;
            Vector3 lookDirection = lookTarget - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }
    }
}
