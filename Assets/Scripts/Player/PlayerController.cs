using UnityEngine;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerStats stats;
        public float groundY = 0f;
        public Transform movementReference;
        [Header("Main Map Visual")]
        public Transform visualRoot;
        [Range(0.5f, 3f)] public float landscapeVisualScale = 1.82f;
        [Range(0.5f, 3f)] public float portraitVisualScale = 1.5f;

        private Rigidbody body;
        private Vector2 moveInput;
        private bool canMove;
        private Vector3 spawnPosition;
        private Vector3 visualBaseLocalPosition;
        private float visualBridgeLift;
        private bool? appliedPortraitLayout;

        public bool IsMoving => canMove && moveInput.sqrMagnitude > 0.01f;
        public float HorizontalInput => canMove ? moveInput.x : 0f;

        public void SetMovementEnabled(bool enabled)
        {
            canMove = enabled;
            if (!enabled && body != null)
            {
                body.linearVelocity = Vector3.zero;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            spawnPosition = transform.position;
            stats = stats == null ? GetComponent<PlayerStats>() : stats;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            if (visualRoot == null)
            {
                SpriteRenderer visualRenderer = GetComponentInChildren<SpriteRenderer>();
                visualRoot = visualRenderer != null ? visualRenderer.transform : null;
            }

            if (visualRoot != null)
            {
                visualBaseLocalPosition = visualRoot.localPosition;
            }

            ApplyResponsiveVisualScale(true);
        }

        public void ResetToSpawn()
        {
            transform.position = spawnPosition;
            if (body != null)
            {
                body.position = spawnPosition;
                body.linearVelocity = Vector3.zero;
            }

            visualBridgeLift = 0f;
            ApplyBridgeVisualLift();
        }

        private void Update()
        {
            ApplyResponsiveVisualScale(false);
            if (!canMove)
            {
                moveInput = Vector2.zero;
                return;
            }

            Vector2 keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            moveInput = MobileInputController.MoveInput.sqrMagnitude > 0.01f
                ? MobileInputController.MoveInput
                : keyboardInput;
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        private void FixedUpdate()
        {
            float speed = stats != null && stats.runtimeStats != null ? stats.CurrentMoveSpeed : 5f;
            Vector3 movementDirection = GetMovementDirection();
            Vector3 movement = movementDirection * speed * Time.fixedDeltaTime;
            Vector3 nextPosition = body.position + movement;
            nextPosition.y = groundY;
            body.MovePosition(nextPosition);

            float targetLift = MainMapBridgeSurface.GetVisualLift(nextPosition);
            visualBridgeLift = Mathf.MoveTowards(
                visualBridgeLift,
                targetLift,
                Time.fixedDeltaTime * 3.8f);
            ApplyBridgeVisualLift();
        }

        private void ApplyBridgeVisualLift()
        {
            if (visualRoot != null)
            {
                visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * visualBridgeLift;
            }
        }

        private Vector3 GetMovementDirection()
        {
            if (movementReference == null)
            {
                return new Vector3(moveInput.x, 0f, moveInput.y);
            }

            Vector3 forward = movementReference.forward;
            Vector3 right = movementReference.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 direction = right * moveInput.x + forward * moveInput.y;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void ApplyResponsiveVisualScale(bool force)
        {
            if (visualRoot == null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            bool portrait = mainCamera != null && mainCamera.pixelWidth > 0 && mainCamera.pixelHeight > 0
                ? mainCamera.pixelHeight > mainCamera.pixelWidth
                : Screen.height > Screen.width;
            if (!force && appliedPortraitLayout.HasValue && appliedPortraitLayout.Value == portrait)
            {
                return;
            }

            float scale = portrait ? portraitVisualScale : landscapeVisualScale;
            visualRoot.localScale = Vector3.one * scale;
            appliedPortraitLayout = portrait;
        }
    }
}
