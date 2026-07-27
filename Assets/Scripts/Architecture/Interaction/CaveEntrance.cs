using UnityEngine;
using WuxiaRoguelite.Architecture.GameFlow;

namespace WuxiaRoguelite.Architecture.Interaction
{
    public sealed class CaveEntrance : WorldInteractableBehaviour
    {
        [SerializeField] private RunManager runManager;

        public void Configure(RunManager manager)
        {
            runManager = manager;
            ResetInteraction();
        }

        protected override bool TryInteract(GameObject actor)
        {
            return runManager != null && runManager.TryEnterCave();
        }
    }
}
