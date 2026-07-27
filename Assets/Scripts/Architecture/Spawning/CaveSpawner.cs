using UnityEngine;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Architecture.Interaction;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Architecture.Spawning
{
    public sealed class CaveSpawner : ConfiguredSpawnerBase
    {
        [SerializeField] private RunManager runManager;

        public void SpawnForRun()
        {
            ClearSpawned();
            SpawnRules(SpawnEntityType.Cave, ConfigureCave);
        }

        private void ConfigureCave(GameObject created, SpawnConfig rule)
        {
            CaveEntrance cave = created.GetComponentInChildren<CaveEntrance>(true);
            if (cave == null)
            {
                Debug.LogWarning($"Prefab {rule.prefabId} 缺少 CaveEntrance。", created);
                return;
            }

            cave.Configure(runManager);
        }
    }
}
