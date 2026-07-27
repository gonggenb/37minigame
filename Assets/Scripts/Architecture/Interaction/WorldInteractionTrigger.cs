using UnityEngine;

namespace WuxiaRoguelite.Architecture.Interaction
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class WorldInteractionTrigger : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour interactableComponent;
        private IWorldInteractable interactable;

        private void Awake()
        {
            ResolveInteractable();
        }

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
            ResolveInteractable();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerInteractionActor>() == null)
            {
                return;
            }

            ResolveInteractable();
            interactable?.Interact(other.gameObject);
        }

        private void ResolveInteractable()
        {
            interactable = interactableComponent as IWorldInteractable;
            if (interactable != null)
            {
                return;
            }

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IWorldInteractable found)
                {
                    interactableComponent = behaviours[i];
                    interactable = found;
                    return;
                }
            }
        }
    }
}
