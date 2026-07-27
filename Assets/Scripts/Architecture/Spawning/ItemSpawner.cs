using UnityEngine;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Architecture.Interaction;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Architecture.Spawning
{
    public sealed class ItemSpawner : ConfiguredSpawnerBase
    {
        [SerializeField] private RunManager runManager;

        public void SpawnForRun()
        {
            ClearSpawned();
            SpawnRules(SpawnEntityType.Treasure, ConfigureTreasure);
            SpawnRules(SpawnEntityType.Herb, ConfigureHerb);
        }

        private void ConfigureTreasure(GameObject created, SpawnConfig rule)
        {
            TreasureChest treasure = created.GetComponentInChildren<TreasureChest>(true);
            if (treasure == null)
            {
                Debug.LogWarning($"Prefab {rule.prefabId} 缺少 TreasureChest。", created);
                return;
            }

            treasure.Configure(runManager, rule.configId);
        }

        private void ConfigureHerb(GameObject created, SpawnConfig rule)
        {
            HerbPickup herb = created.GetComponentInChildren<HerbPickup>(true);
            if (herb == null)
            {
                Debug.LogWarning($"Prefab {rule.prefabId} 缺少 HerbPickup。", created);
                return;
            }

            herb.Configure(runManager, rule.configId);
        }
    }
}
