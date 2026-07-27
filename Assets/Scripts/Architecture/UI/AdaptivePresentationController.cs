using UnityEngine;
using WuxiaRoguelite.Application.Presentation;
using WuxiaRoguelite.CameraTools;

namespace WuxiaRoguelite.Architecture.UI
{
    [DisallowMultipleComponent]
    public sealed class AdaptivePresentationController : MonoBehaviour
    {
        [SerializeField] private GameObject portraitRoot;
        [SerializeField] private GameObject landscapeRoot;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private float portraitFieldOfView = 52f;
        [SerializeField] private float landscapeFieldOfView = 38.5f;
        [SerializeField] private Vector3 portraitCameraOffset = new Vector3(4.5f, 10.5f, -12.5f);
        [SerializeField] private Vector3 landscapeCameraOffset = new Vector3(6f, 7.2f, -10.5f);

        private int lastWidth = -1;
        private int lastHeight = -1;
        private PresentationLayoutMode currentMode;

        public PresentationLayoutMode CurrentMode => currentMode;

        private void OnEnable()
        {
            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
        }

        public void Refresh(bool force)
        {
            int width = Screen.width;
            int height = Screen.height;
            if (!force && width == lastWidth && height == lastHeight)
            {
                return;
            }

            lastWidth = width;
            lastHeight = height;
            currentMode = PresentationLayoutResolver.Resolve(width, height);
            bool portrait = currentMode == PresentationLayoutMode.Portrait;

            if (portraitRoot != null)
            {
                portraitRoot.SetActive(portrait);
            }

            if (landscapeRoot != null)
            {
                landscapeRoot.SetActive(!portrait);
            }

            if (targetCamera != null)
            {
                targetCamera.fieldOfView = portrait ? portraitFieldOfView : landscapeFieldOfView;
            }

            if (cameraFollow != null)
            {
                cameraFollow.offset = portrait ? portraitCameraOffset : landscapeCameraOffset;
            }
        }
    }
}
