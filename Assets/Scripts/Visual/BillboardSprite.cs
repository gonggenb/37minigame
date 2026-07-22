using UnityEngine;

namespace WuxiaRoguelite.Visual
{
    public class BillboardSprite : MonoBehaviour
    {
        public Camera targetCamera;

        private void LateUpdate()
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            Vector3 direction = transform.position - cameraToUse.transform.position;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
