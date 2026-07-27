using UnityEngine;

namespace WuxiaRoguelite.Architecture.Interaction
{
    public abstract class WorldInteractableBehaviour : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private bool deactivateOnConsume = true;

        public bool IsConsumed { get; private set; }

        public bool Interact(GameObject actor)
        {
            if (IsConsumed || !TryInteract(actor))
            {
                return false;
            }

            IsConsumed = true;
            if (deactivateOnConsume)
            {
                gameObject.SetActive(false);
            }

            return true;
        }

        public void ResetInteraction()
        {
            IsConsumed = false;
            gameObject.SetActive(true);
        }

        protected abstract bool TryInteract(GameObject actor);
    }
}
