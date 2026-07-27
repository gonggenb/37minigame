using UnityEngine;

namespace WuxiaRoguelite.Architecture.Interaction
{
    public interface IWorldInteractable
    {
        bool IsConsumed { get; }
        bool Interact(GameObject actor);
        void ResetInteraction();
    }
}
