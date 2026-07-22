using UnityEngine;

namespace WuxiaRoguelite.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerStats stats;
        public float groundY = 0f;
        public Transform movementReference;

        private Rigidbody body;
        private Vector2 moveInput;
        private bool canMove;
        private Vector3 spawnPosition;

        public bool IsMoving => canMove && moveInput.sqrMagnitude > 0.01f;

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
        }

        public void ResetToSpawn()
        {
            transform.position = spawnPosition;
            if (body != null)
            {
                body.position = spawnPosition;
                body.linearVelocity = Vector3.zero;
            }
        }

        private void Update()
        {
            if (!canMove)
            {
                moveInput = Vector2.zero;
                return;
            }

            moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        private void FixedUpdate()
        {
            float speed = stats != null && stats.runtimeStats != null ? stats.runtimeStats.moveSpeed : 5f;
            Vector3 movementDirection = GetMovementDirection();
            Vector3 movement = movementDirection * speed * Time.fixedDeltaTime;
            Vector3 nextPosition = body.position + movement;
            nextPosition.y = groundY;
            body.MovePosition(nextPosition);
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
    }
}
