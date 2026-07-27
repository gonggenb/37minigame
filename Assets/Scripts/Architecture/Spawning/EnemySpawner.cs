using UnityEngine;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Architecture.Interaction;
using WuxiaRoguelite.Domain.Configuration;

namespace WuxiaRoguelite.Architecture.Spawning
{
    public sealed class EnemySpawner : ConfiguredSpawnerBase
    {
        [SerializeField] private RunManager runManager;

        public void SpawnForRun()
        {
            ClearSpawned();
            SpawnRules(SpawnEntityType.Enemy, ConfigureEnemy);
        }

        private void ConfigureEnemy(GameObject created, SpawnConfig rule)
        {
            EnemyEncounter encounter = created.GetComponentInChildren<EnemyEncounter>(true);
            if (encounter == null)
            {
                Debug.LogWarning($"Prefab {rule.prefabId} 缺少 EnemyEncounter。", created);
                return;
            }

            CharacterKind kind = databaseProvider.Database.GetCharacter(rule.configId).kind;
            encounter.Configure(
                runManager,
                rule.configId,
                rule.rewardId,
                kind == CharacterKind.CaveEnemy);
        }
    }
}
