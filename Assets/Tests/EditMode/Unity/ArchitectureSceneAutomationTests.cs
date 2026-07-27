using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace WuxiaRoguelite.Tests.Unity
{
    public sealed class ArchitectureSceneAutomationTests
    {
        private const string ScenePath = "Assets/Scenes/MainPrototype_Architecture.unity";
        private const string CatalogPath = "Assets/Scripts/Config/SpawnPrefabCatalog.asset";

        [Test]
        public void ArchitectureScene_HasCompleteCoreBindingsAndSpawnRegions()
        {
            WithArchitectureScene(scene =>
            {
                GameObject architectureRoot = Find(scene, "ArchitectureRoot");
                Assert.That(architectureRoot, Is.Not.Null);
                Assert.That(architectureRoot.transform.position, Is.EqualTo(Vector3.zero));

                Component provider = RequireComponent(scene, "ArchitectureRoot/Data", "GameDatabaseProvider");
                Component characters = RequireComponent(scene, "ArchitectureRoot/Manager/CharacterManager", "CharacterManager");
                Component battle = RequireComponent(scene, "ArchitectureRoot/Manager/BattleRunner", "BattleRunner");
                Component run = RequireComponent(scene, "ArchitectureRoot/Manager/RunManager", "RunManager");
                Component enemySpawner = RequireComponent(scene, "ArchitectureRoot/WorldSpawners/EnemySpawner", "EnemySpawner");
                Component itemSpawner = RequireComponent(scene, "ArchitectureRoot/WorldSpawners/ItemSpawner", "ItemSpawner");
                Component caveSpawner = RequireComponent(scene, "ArchitectureRoot/WorldSpawners/CaveSpawner", "CaveSpawner");

                AssertObjectReference(provider, "database");
                AssertObjectReference(characters, "databaseProvider");
                AssertObjectReference(battle, "characterManager");
                AssertObjectReference(run, "characterManager");
                AssertObjectReference(run, "battleRunner");
                AssertObjectReference(run, "playerController");
                AssertObjectReference(run, "enemySpawner");
                AssertObjectReference(run, "itemSpawner");
                AssertObjectReference(run, "caveSpawner");

                AssertSpawnerBindings(enemySpawner);
                AssertSpawnerBindings(itemSpawner);
                AssertSpawnerBindings(caveSpawner);
                AssertObjectReference(enemySpawner, "runManager");
                AssertObjectReference(itemSpawner, "runManager");
                AssertObjectReference(caveSpawner, "runManager");

                var expectedRegions = new HashSet<string>
                {
                    "east_forest",
                    "south_quarry",
                    "north_pass",
                    "main_map"
                };
                GameObject regionRoot = Find(scene, "ArchitectureRoot/SpawnRegions");
                Assert.That(regionRoot, Is.Not.Null);
                Component[] regionComponents = regionRoot.GetComponentsInChildren<Component>(true);
                foreach (Component component in regionComponents)
                {
                    if (component != null && component.GetType().Name == "SpawnRegion")
                    {
                        var serialized = new SerializedObject(component);
                        expectedRegions.Remove(serialized.FindProperty("regionId").stringValue);
                    }
                }

                Assert.That(expectedRegions, Is.Empty, "四个 CSV region_id 必须全部存在于场景中。");
            });
        }

        [Test]
        public void RunManager_EnablesPlayerMovementOnlyOnUnpausedMainMap()
        {
            GameObject playerObject = new GameObject("PlayerMovementTest", typeof(Rigidbody));
            GameObject runObject = new GameObject("RunManagerMovementTest");

            try
            {
                Type playerType = FindRuntimeType("WuxiaRoguelite.Player.PlayerController");
                Type runType = FindRuntimeType("WuxiaRoguelite.Architecture.GameFlow.RunManager");
                Type gameStateType = FindRuntimeType("WuxiaRoguelite.Domain.GameFlow.GameState");
                Component playerController = playerObject.AddComponent(playerType);
                Component runManager = runObject.AddComponent(runType);

                FieldInfo playerField = runType.GetField("playerController", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(playerField, Is.Not.Null, "RunManager 必须持有 PlayerController 引用。");
                playerField.SetValue(runManager, playerController);

                MethodInfo setState = runType.GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo setExplicitPause = runType.GetMethod("SetExplicitPause", BindingFlags.Instance | BindingFlags.Public);
                FieldInfo canMoveField = playerType.GetField("canMove", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(setState, Is.Not.Null);
                Assert.That(setExplicitPause, Is.Not.Null);
                Assert.That(canMoveField, Is.Not.Null);

                setState.Invoke(runManager, new[] { Enum.Parse(gameStateType, "MainMap") });
                Assert.That(canMoveField.GetValue(playerController), Is.True, "主地图探索时应允许移动。");

                setExplicitPause.Invoke(runManager, new object[] { true });
                Assert.That(canMoveField.GetValue(playerController), Is.False, "显式暂停时应锁定移动。");

                setExplicitPause.Invoke(runManager, new object[] { false });
                Assert.That(canMoveField.GetValue(playerController), Is.True, "解除暂停后应恢复主地图移动。");

                setState.Invoke(runManager, new[] { Enum.Parse(gameStateType, "NormalBattle") });
                Assert.That(canMoveField.GetValue(playerController), Is.False, "普通战斗阶段应锁定移动。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ArchitectureScene_HasCompleteUguiAndPresenterBindings()
        {
            WithArchitectureScene(scene =>
            {
                RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas", "Canvas");
                RequireComponent(scene, "ArchitectureRoot/UGUI/EventSystem", "EventSystem");

                Component mainMenu = RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas/MainMenuPanel", "MainMenuView");
                Component hud = RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas/HudPanel", "HudView");
                Component battle = RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas/BattlePanel", "BattleView");
                Component cave = RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas/CavePanel", "CaveView");
                Component levelUp = RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas/LevelUpPanel", "LevelUpView");
                Component result = RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas/ResultPanel", "ResultView");
                Component presenter = RequireComponent(scene, "ArchitectureRoot/UGUI/Canvas", "GameUiPresenter");

                AssertReferences(mainMenu, "root", "startButton");
                AssertReferences(hud, "root", "timerText", "healthText", "healthSlider", "progressionText", "currencyText", "statusText");
                AssertReferences(battle, "root", "titleText", "playerHealthText", "playerHealthSlider", "enemyHealthText", "enemyHealthSlider", "effectText", "battleTimeText");
                AssertReferences(cave, "root", "descriptionText", "exitButton");
                AssertArrayReferences(levelUp, "choiceButtons", 3);
                AssertArrayReferences(levelUp, "choiceLabels", 3);
                AssertReferences(levelUp, "root", "rerollButton", "rerollText");
                AssertReferences(result, "root", "resultText", "summaryText", "restartButton");
                AssertReferences(presenter, "runManager", "mainMenuView", "hudView", "battleView", "caveView", "levelUpView", "resultView");
            });
        }

        [Test]
        public void ArchitecturePrefabs_AreMigratedAndCatalogUsesCopies()
        {
            var expected = new Dictionary<string, PrefabExpectation>
            {
                { "prefab_enemy_bandit", new PrefabExpectation("Assets/Prefabs/Architecture/Enemies/山贼喽啰.prefab", "EnemyEncounter") },
                { "prefab_enemy_bamboo", new PrefabExpectation("Assets/Prefabs/Architecture/Enemies/流寇.prefab", "EnemyEncounter") },
                { "prefab_enemy_ink_wolf", new PrefabExpectation("Assets/Prefabs/Architecture/Enemies/灰岩巨鼠.prefab", "EnemyEncounter") },
                { "prefab_enemy_stone_ape", new PrefabExpectation("Assets/Prefabs/Architecture/Enemies/黑风刀客.prefab", "EnemyEncounter") },
                { "prefab_treasure", new PrefabExpectation("Assets/Prefabs/Architecture/Items/东市宝箱.prefab", "TreasureChest") },
                { "prefab_herb", new PrefabExpectation("Assets/Prefabs/Architecture/Items/北门药草.prefab", "HerbPickup") },
                { "prefab_hidden_cave", new PrefabExpectation("Assets/Prefabs/Architecture/Cave/古藏秘窟.prefab", "CaveEntrance") }
            };

            foreach (KeyValuePair<string, PrefabExpectation> pair in expected)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value.Path);
                Assert.That(prefab, Is.Not.Null, $"缺少 Architecture Prefab：{pair.Value.Path}");
                Assert.That(FindComponent(prefab, pair.Value.InteractionType), Is.Not.Null);
                Assert.That(FindComponent(prefab, "WorldInteractionTrigger"), Is.Not.Null);
                Assert.That(FindComponent(prefab, "EncounterTrigger"), Is.Null, "Architecture 副本不得保留旧 EncounterTrigger。");

                Collider collider = prefab.GetComponentInChildren<Collider>(true);
                Assert.That(collider, Is.Not.Null);
                Assert.That(collider.isTrigger, Is.True);
            }

            Object catalog = AssetDatabase.LoadMainAssetAtPath(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            var catalogObject = new SerializedObject(catalog);
            SerializedProperty entries = catalogObject.FindProperty("entries");
            Assert.That(entries, Is.Not.Null);
            Assert.That(entries.arraySize, Is.EqualTo(expected.Count));

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string prefabId = entry.FindPropertyRelative("prefabId").stringValue;
                Object prefab = entry.FindPropertyRelative("prefab").objectReferenceValue;
                Assert.That(expected.ContainsKey(prefabId), Is.True, $"Catalog 出现未规划 ID：{prefabId}");
                Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(expected[prefabId].Path));
            }
        }

        private static void AssertSpawnerBindings(Component spawner)
        {
            AssertObjectReference(spawner, "databaseProvider");
            AssertObjectReference(spawner, "prefabCatalog");
            AssertObjectReference(spawner, "spawnedRoot");
            AssertArrayReferences(spawner, "regions", 4);
        }

        private static void AssertReferences(Component component, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                AssertObjectReference(component, propertyName);
            }
        }

        private static void AssertObjectReference(Component component, string propertyName)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} 不存在。");
            Assert.That(property.objectReferenceValue, Is.Not.Null, $"{component.GetType().Name}.{propertyName} 未绑定。");
        }

        private static void AssertArrayReferences(Component component, string propertyName, int expectedSize)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            Assert.That(property.isArray, Is.True);
            Assert.That(property.arraySize, Is.EqualTo(expectedSize));
            for (int i = 0; i < property.arraySize; i++)
            {
                Assert.That(property.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            }
        }

        private static Component RequireComponent(Scene scene, string path, string typeName)
        {
            GameObject gameObject = Find(scene, path);
            Assert.That(gameObject, Is.Not.Null, $"场景缺少对象：{path}");
            Component component = FindComponent(gameObject, typeName, false);
            Assert.That(component, Is.Not.Null, $"{path} 缺少组件 {typeName}");
            return component;
        }

        private static Component FindComponent(GameObject gameObject, string typeName, bool includeChildren = true)
        {
            Component[] components = includeChildren
                ? gameObject.GetComponentsInChildren<Component>(true)
                : gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component != null && component.GetType().Name == typeName)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject Find(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            GameObject current = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == segments[0])
                {
                    current = root;
                    break;
                }
            }

            for (int i = 1; current != null && i < segments.Length; i++)
            {
                Transform child = current.transform.Find(segments[i]);
                current = child != null ? child.gameObject : null;
            }

            return current;
        }

        private static Type FindRuntimeType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            Assert.Fail($"未加载运行时类型：{fullName}");
            return null;
        }

        private static void WithArchitectureScene(System.Action<Scene> assertion)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedByTest = !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                assertion(scene);
            }
            finally
            {
                if (openedByTest && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private readonly struct PrefabExpectation
        {
            public PrefabExpectation(string path, string interactionType)
            {
                Path = path;
                InteractionType = interactionType;
            }

            public string Path { get; }
            public string InteractionType { get; }
        }
    }
}
