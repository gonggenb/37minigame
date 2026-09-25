#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;

namespace WuxiaRoguelite.EditorTools
{
    public static class PingchuanTownRewardBuilder
    {
        [Serializable] private class Entry
        {
            public string name, type;
            public Vector3 position;
            public float routeDistance;
            public int tier, baseCultivation, cultivation, baseCopper, copper;
            public bool martialUpgrade;
        }
        [Serializable] private class Report
        {
            public string scope = "0.4m eight-neighbour shortest paths at player radius, excluding trigger encounters. Baked once; travel time and route balance need Play Mode validation.";
            public float midDistance = ExplorationRewardTuning.MidRouteDistance;
            public float farDistance = ExplorationRewardTuning.FarRouteDistance;
            public Entry[] encounters;
        }

        [MenuItem("37 MiniGame/Apply Pingchuan Exploration Rewards")]
        public static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save scene changes before applying rewards.");
            ApplyToActiveScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static void ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorSceneManager.GetActiveScene().path != PingchuanTownLevelBuilder.ScenePath)
                throw new InvalidOperationException("Open MainPrototype in Edit Mode first.");
            var encounters = UnityEngine.Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(e => e.gameObject.scene == EditorSceneManager.GetActiveScene()).OrderBy(e => e.name).ToArray();
            var distances = MeasureDistances(encounters);
            var entries = new List<Entry>();
            Undo.RecordObjects(encounters, "Apply exploration rewards");
            foreach (var e in encounters)
            {
                bool supported = e.encounterType == EncounterType.NormalEnemy ||
                    e.encounterType == EncounterType.EliteEnemy || e.encounterType == EncounterType.Treasure;
                // Keep the source rewards intact. Rebuilding or applying again cannot multiply them twice.
                e.rewardRouteDistance = supported ? distances[e] : -1f;
                EditorUtility.SetDirty(e);
                if (!supported) continue;
                entries.Add(new Entry { name = e.name, type = e.encounterType.ToString(), position = e.transform.position,
                    routeDistance = e.rewardRouteDistance, tier = e.ExplorationRewardTier,
                    baseCultivation = e.cultivationReward, cultivation = e.GrantedCultivationReward,
                    baseCopper = e.copperReward, copper = e.GrantedCopperReward, martialUpgrade = e.GrantsMartialArtUpgrade });
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Directory.CreateDirectory("docs/validation/pingchuan_town");
            File.WriteAllText("docs/validation/pingchuan_town/exploration_rewards.json",
                JsonUtility.ToJson(new Report { encounters = entries.ToArray() }, true));
            Debug.Log($"PINGCHUAN_REWARDS_APPLIED: {entries.Count} encounters; near {entries.Count(e => e.tier == 0)}, mid {entries.Count(e => e.tier == 1)}, far {entries.Count(e => e.tier == 2)}; {entries.Count(e => e.martialUpgrade)} far chests.");
        }

        private static Dictionary<EncounterTrigger, float> MeasureDistances(EncounterTrigger[] encounters)
        {
            const int nx = 241, nz = 226;
            const float step = .4f, radius = .34f;
            Vector3 Point(int k) => new Vector3(-48 + k % nx * step, .7f, -45 + k / nx * step);
            int Index(Vector3 p) => Mathf.Clamp(Mathf.RoundToInt((p.z + 45) / step), 0, nz - 1) * nx +
                Mathf.Clamp(Mathf.RoundToInt((p.x + 48) / step), 0, nx - 1);
            var hero = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (hero == null) throw new InvalidOperationException("Missing player.");
            var heroColliders = hero.GetComponentsInChildren<Collider>();
            var enabled = heroColliders.Select(c => c.enabled).ToArray();
            var clear = new bool[nx * nz];
            var distance = Enumerable.Repeat(float.PositiveInfinity, clear.Length).ToArray();
            try
            {
                foreach (var c in heroColliders) c.enabled = false;
                Physics.SyncTransforms();
                for (int i = 0; i < clear.Length; i++)
                    clear[i] = !Physics.CheckSphere(Point(i), radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                int start = Index(hero.transform.position);
                if (!clear[start]) throw new InvalidOperationException("Spawn is blocked.");
                var queue = new SortedSet<(float cost, int index)>();
                distance[start] = 0; queue.Add((0, start));
                while (queue.Count > 0)
                {
                    var current = queue.Min; queue.Remove(current);
                    int k = current.index, x = k % nx, z = k / nx;
                    for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0 || x + dx < 0 || x + dx >= nx || z + dz < 0 || z + dz >= nz) continue;
                        int next = k + dx + dz * nx;
                        if (!clear[next] || !clear[k + dx] || !clear[k + dz * nx]) continue;
                        float cost = current.cost + step * (dx != 0 && dz != 0 ? Mathf.Sqrt(2) : 1);
                        if (cost >= distance[next]) continue;
                        queue.Remove((distance[next], next));
                        distance[next] = cost; queue.Add((cost, next));
                    }
                }
                var result = new Dictionary<EncounterTrigger, float>();
                foreach (var e in encounters)
                {
                    float d = distance[Index(e.transform.position)];
                    if (float.IsInfinity(d)) throw new InvalidOperationException("Unreachable encounter: " + e.name);
                    result.Add(e, d);
                }
                return result;
            }
            finally
            {
                for (int i = 0; i < heroColliders.Length; i++) heroColliders[i].enabled = enabled[i];
                Physics.SyncTransforms();
            }
        }
    }
}
#endif
