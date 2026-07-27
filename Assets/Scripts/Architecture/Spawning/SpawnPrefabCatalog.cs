using System;
using System.Collections.Generic;
using UnityEngine;

namespace WuxiaRoguelite.Architecture.Spawning
{
    [CreateAssetMenu(fileName = "SpawnPrefabCatalog", menuName = "一炷江湖/Spawn Prefab Catalog")]
    public sealed class SpawnPrefabCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            public string prefabId = string.Empty;
            public GameObject prefab;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [NonSerialized] private Dictionary<string, GameObject> index;

        public GameObject GetPrefab(string prefabId)
        {
            EnsureIndex();
            if (string.IsNullOrWhiteSpace(prefabId) || !index.TryGetValue(prefabId, out GameObject prefab) ||
                prefab == null)
            {
                throw new KeyNotFoundException($"找不到生成 Prefab：{prefabId}");
            }

            return prefab;
        }

        private void OnEnable()
        {
            index = null;
        }

        private void OnValidate()
        {
            index = null;
        }

        private void EnsureIndex()
        {
            if (index != null)
            {
                return;
            }

            index = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Length; i++)
            {
                Entry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.prefabId))
                {
                    continue;
                }

                if (!index.TryAdd(entry.prefabId, entry.prefab))
                {
                    throw new InvalidOperationException($"SpawnPrefabCatalog 存在重复 ID：{entry.prefabId}");
                }
            }
        }
    }
}
