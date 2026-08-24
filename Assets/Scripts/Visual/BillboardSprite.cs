using UnityEngine;

namespace WuxiaRoguelite.Visual
{
    public enum BillboardAlignment
    {
        CameraPlane,
        PositionFacing,
        YAxisFacing,
        Fixed
    }

    public class BillboardSprite : MonoBehaviour
    {
        public Camera targetCamera;
        public BillboardAlignment alignment = BillboardAlignment.CameraPlane;

        private void LateUpdate()
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            if (alignment == BillboardAlignment.Fixed)
            {
                return;
            }

            if (alignment == BillboardAlignment.CameraPlane)
            {
                transform.rotation = cameraToUse.transform.rotation;
                return;
            }

            Vector3 direction = transform.position - cameraToUse.transform.position;
            if (alignment == BillboardAlignment.YAxisFacing)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }
}
