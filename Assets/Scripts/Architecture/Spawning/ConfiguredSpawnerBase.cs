using System;
using System.Collections.Generic;
using UnityEngine;
using WuxiaRoguelite.Config;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Architecture.Spawning
{
    public abstract class ConfiguredSpawnerBase : MonoBehaviour
    {
        [SerializeField] protected GameDatabaseProvider databaseProvider;
        [SerializeField] protected SpawnPrefabCatalog prefabCatalog;
        [SerializeField] protected SpawnRegion[] regions = Array.Empty<SpawnRegion>();
        [SerializeField] protected Transform spawnedRoot;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        public void ClearSpawned()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                {
                    Destroy(spawnedObjects[i]);
                }
            }

            spawnedObjects.Clear();
        }

        protected void SpawnRules(SpawnEntityType entityType, Action<GameObject, SpawnConfig> configure)
        {
            if (databaseProvider == null || !databaseProvider.IsConfigured || prefabCatalog == null)
            {
                throw new InvalidOperationException($"{GetType().Name} 需要绑定数据库和 SpawnPrefabCatalog。");
            }

            IReadOnlyList<SpawnConfig> rules = databaseProvider.Database.Spawns;
            for (int i = 0; i < rules.Count; i++)
            {
                SpawnConfig rule = rules[i];
                if (rule.entityType != entityType)
                {
                    continue;
                }

                SpawnRegion region = FindRegion(rule.regionId);
                int count = UnityEngine.Random.Range(rule.minCount, rule.maxCount + 1);
                for (int instance = 0; instance < count; instance++)
                {
                    if (rule.weight < 1f && UnityEngine.Random.value > rule.weight)
                    {
                        continue;
                    }

                    GameObject created = Instantiate(
                        prefabCatalog.GetPrefab(rule.prefabId),
                        region.RandomPoint(),
                        region.transform.rotation,
                        spawnedRoot);
                    created.name = $"{rule.id}_{instance + 1}";
                    spawnedObjects.Add(created);
                    configure(created, rule);
                }
            }
        }

        private SpawnRegion FindRegion(string regionId)
        {
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i] != null && regions[i].RegionId == regionId)
                {
                    return regions[i];
                }
            }

            throw new KeyNotFoundException($"{GetType().Name} 找不到出生区域：{regionId}");
        }
    }
}
