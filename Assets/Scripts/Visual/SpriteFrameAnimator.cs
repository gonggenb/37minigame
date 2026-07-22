using UnityEngine;
using WuxiaRoguelite.Player;

namespace WuxiaRoguelite.Visual
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        public Sprite[] idleFrames;
        public Sprite[] moveFrames;
        public PlayerController movementSource;
        public float framesPerSecond = 10f;
        public bool randomizeStart = true;

        private SpriteRenderer spriteRenderer;
        private float startOffset;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            startOffset = randomizeStart ? Random.value * 2f : 0f;
        }

        private void Update()
        {
            if (movementSource != null && Mathf.Abs(movementSource.HorizontalInput) > 0.01f)
            {
                spriteRenderer.flipX = movementSource.HorizontalInput < 0f;
            }

            Sprite[] frames = movementSource != null && movementSource.IsMoving && moveFrames != null && moveFrames.Length > 0
                ? moveFrames
                : idleFrames;
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            int frameIndex = Mathf.FloorToInt((Time.time + startOffset) * framesPerSecond) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}
