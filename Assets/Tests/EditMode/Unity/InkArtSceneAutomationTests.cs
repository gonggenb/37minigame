using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WuxiaRoguelite.Tests.Unity
{
    public sealed class InkArtSceneAutomationTests
    {
        private const string ScenePath = "Assets/Scenes/MainPrototype_InkArt.unity";
        private const string CatalogPath = "Assets/GameData/Runtime/InkArtCatalog.asset";
        private const string PrefabCatalogPath = "Assets/GameData/Runtime/InkSpawnPrefabCatalog.asset";
        private const string RuntimeArtRoot = "Assets/Art/RuntimeInkArt/";

        private static readonly string[] RequiredSpriteIds =
        {
            "background.portrait.main",
            "background.landscape.main",
            "background.cave",
            "ui.panel",
            "ui.button.primary",
            "ui.top_frame",
            "ui.boss_frame",
            "ui.health_bar",
            "world.treasure",
            "world.herb",
            "world.cave_entrance"
        };

        private static readonly string[] RequiredCharacterIds =
        {
            "player_wuxia",
            "enemy_bandit",
            "enemy_bamboo_puppet",
            "enemy_ink_wolf",
            "enemy_stone_ape",
            "boss_fox_demon",
            "boss_orc_warlord"
        };

        [Test]
        public void InkArtCatalog_HasRequiredStableIdsAndReferences()
        {
            UnityEngine.Object catalog = AssetDatabase.LoadMainAssetAtPath(CatalogPath);
            Assert.That(catalog, Is.Not.Null, $"缺少水墨美术目录：{CatalogPath}");

            var serialized = new SerializedObject(catalog);
            AssertEntries(serialized.FindProperty("sprites"), RequiredSpriteIds, false);
            AssertEntries(serialized.FindProperty("characters"), RequiredCharacterIds, true);
        }

        [Test]
        public void InkPresentationComponents_ExposeBattleAndLevelUpArtBindings()
        {
            Type battleStage = Type.GetType("WuxiaRoguelite.Architecture.UI.InkBattleStage, Assembly-CSharp");
            Assert.That(battleStage, Is.Not.Null, "缺少 InkBattleStage。");
            foreach (string fieldName in new[]
                     {
                         "runManager", "battleRunner", "catalog", "backgroundImage", "playerImage", "enemyImage"
                     })
            {
                Assert.That(battleStage.GetField(fieldName,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
                    Is.Not.Null, $"InkBattleStage 缺少字段 {fieldName}。");
            }

            Type levelUpView = Type.GetType("WuxiaRoguelite.Architecture.UI.LevelUpView, Assembly-CSharp");
            Assert.That(levelUpView, Is.Not.Null);
            Assert.That(levelUpView.GetField("choiceIcons",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
                Is.Not.Null, "LevelUpView 缺少 choiceIcons。");
            Assert.That(levelUpView.GetField("inkArtCatalog",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
                Is.Not.Null, "LevelUpView 缺少 inkArtCatalog。");
        }

        [Test]
        public void InkArtScene_HasPortraitLandscapeLayoutsAndCompletePresenters()
        {
            WithScene(scene =>
            {
                GameObject presentationRoot = Find(scene, "InkPresentationRoot");
                Assert.That(presentationRoot, Is.Not.Null);
                Assert.That(FindComponent(presentationRoot, "AdaptivePresentationController", false), Is.Not.Null);

                AssertLayout(scene, "InkPresentationRoot/PortraitCanvas", new Vector2(750f, 1338f));
                AssertLayout(scene, "InkPresentationRoot/LandscapeCanvas", new Vector2(1920f, 1080f));
            });
        }

        [Test]
        public void InkArtScene_PreservesArchitectureAndReplacesLegacyVisualLayer()
        {
            WithScene(scene =>
            {
                Assert.That(Find(scene, "ArchitectureRoot"), Is.Not.Null);
                Assert.That(Find(scene, "Player"), Is.Not.Null);
                Assert.That(Find(scene, "Scene/InkVisualRoot"), Is.Not.Null);

                GameObject kayKit = Find(scene, "Scene/KayKit Medieval Scenery");
                Assert.That(kayKit, Is.Not.Null);
                Assert.That(kayKit.activeSelf, Is.False, "KayKit 旧视觉根节点必须禁用。");

                GameObject expandedKayKit = Find(scene, "Scene/Expanded Main Map Content/Expanded KayKit Scenery");
                Assert.That(expandedKayKit, Is.Not.Null);
                Assert.That(expandedKayKit.activeSelf, Is.False, "扩展 KayKit 旧视觉根节点必须禁用。");

                Assert.That(AssetDatabase.LoadMainAssetAtPath(PrefabCatalogPath), Is.Not.Null);
            });
        }

        [Test]
        public void InkArtSceneAndPrefabs_DoNotReferenceOldMixedVisualAssets()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            string[] dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            string[] forbidden = dependencies
                .Where(IsForbiddenVisualDependency)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(forbidden, Is.Empty,
                "水墨场景仍引用旧 Generated/ThirdParty 或只读素材库资源：\n" + string.Join("\n", forbidden));
        }

        private static void AssertLayout(Scene scene, string canvasPath, Vector2 expectedReferenceResolution)
        {
            GameObject canvasObject = Find(scene, canvasPath);
            Assert.That(canvasObject, Is.Not.Null, $"缺少布局：{canvasPath}");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            Assert.That(canvas, Is.Not.Null);
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(expectedReferenceResolution));

            Component presenter = FindComponent(canvasObject, "GameUiPresenter", false);
            Assert.That(presenter, Is.Not.Null);
            AssertReferences(presenter,
                "runManager",
                "mainMenuView",
                "hudView",
                "battleView",
                "caveView",
                "levelUpView",
                "resultView");

            string[] panels =
            {
                "MainMenuPanel",
                "HudPanel",
                "BattlePanel",
                "CavePanel",
                "LevelUpPanel",
                "ResultPanel"
            };
            foreach (string panel in panels)
            {
                Assert.That(Find(scene, $"{canvasPath}/{panel}"), Is.Not.Null);
            }

            Component levelUp = FindComponent(Find(scene, $"{canvasPath}/LevelUpPanel"), "LevelUpView", false);
            Assert.That(levelUp, Is.Not.Null);
            var levelUpSerialized = new SerializedObject(levelUp);
            AssertObjectArray(levelUpSerialized.FindProperty("choiceIcons"), 3, "LevelUpView.choiceIcons");
            Assert.That(levelUpSerialized.FindProperty("inkArtCatalog").objectReferenceValue, Is.Not.Null);

            Component battleStage = FindComponent(Find(scene, $"{canvasPath}/BattlePanel"), "InkBattleStage", true);
            Assert.That(battleStage, Is.Not.Null, $"{canvasPath} 缺少 InkBattleStage。");
            AssertReferences(battleStage,
                "runManager",
                "battleRunner",
                "catalog",
                "backgroundImage",
                "playerImage",
                "enemyImage");
        }

        private static void AssertObjectArray(SerializedProperty property, int expectedSize, string label)
        {
            Assert.That(property, Is.Not.Null, $"{label} 不存在。");
            Assert.That(property.arraySize, Is.EqualTo(expectedSize));
            for (int i = 0; i < property.arraySize; i++)
            {
                Assert.That(property.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null,
                    $"{label}[{i}] 未绑定。");
            }
        }

        private static void AssertEntries(
            SerializedProperty entries,
            IReadOnlyCollection<string> requiredIds,
            bool characterEntries)
        {
            Assert.That(entries, Is.Not.Null);
            var found = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                string id = entry.FindPropertyRelative("id").stringValue;
                if (!requiredIds.Contains(id))
                {
                    continue;
                }

                found.Add(id);
                if (characterEntries)
                {
                    SerializedProperty idleFrames = entry.FindPropertyRelative("idleFrames");
                    SerializedProperty moveFrames = entry.FindPropertyRelative("moveFrames");
                    Assert.That(idleFrames.arraySize, Is.GreaterThan(0), $"{id} 缺少 idleFrames。");
                    Assert.That(moveFrames.arraySize, Is.GreaterThan(0), $"{id} 缺少 moveFrames。");
                    AssertAllObjectReferences(idleFrames, id);
                    AssertAllObjectReferences(moveFrames, id);
                }
                else
                {
                    Assert.That(entry.FindPropertyRelative("sprite").objectReferenceValue, Is.Not.Null,
                        $"{id} 未绑定 Sprite。");
                }
            }

            Assert.That(found, Is.EquivalentTo(requiredIds));
        }

        private static void AssertAllObjectReferences(SerializedProperty array, string id)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                Assert.That(array.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null,
                    $"{id}[{i}] 未绑定 Sprite。");
            }
        }

        private static bool IsForbiddenVisualDependency(string path)
        {
            if (!path.StartsWith("Assets/Art/", StringComparison.Ordinal))
            {
                return false;
            }

            if (path.StartsWith(RuntimeArtRoot, StringComparison.Ordinal))
            {
                return false;
            }

            return path.Contains("/Generated/", StringComparison.Ordinal) ||
                   path.Contains("/ThirdParty/", StringComparison.Ordinal) ||
                   path.Contains("Q版水墨国风", StringComparison.Ordinal);
        }

        private static void AssertReferences(Component component, params string[] propertyNames)
        {
            var serialized = new SerializedObject(component);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, $"{component.GetType().Name}.{propertyName} 不存在。");
                Assert.That(property.objectReferenceValue, Is.Not.Null,
                    $"{component.GetType().Name}.{propertyName} 未绑定。");
            }
        }

        private static Component FindComponent(GameObject gameObject, string typeName, bool includeChildren)
        {
            Component[] components = includeChildren
                ? gameObject.GetComponentsInChildren<Component>(true)
                : gameObject.GetComponents<Component>();
            return components.FirstOrDefault(component => component != null && component.GetType().Name == typeName);
        }

        private static GameObject Find(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            GameObject current = scene.GetRootGameObjects().FirstOrDefault(root => root.name == segments[0]);
            for (int i = 1; current != null && i < segments.Length; i++)
            {
                Transform child = current.transform.Find(segments[i]);
                current = child != null ? child.gameObject : null;
            }

            return current;
        }

        private static void WithScene(Action<Scene> assertion)
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
    }
}
