#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WuxiaRoguelite.Architecture.Battle;
using WuxiaRoguelite.Architecture.Characters;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Architecture.Interaction;
using WuxiaRoguelite.Architecture.Spawning;
using WuxiaRoguelite.Architecture.UI;
using WuxiaRoguelite.Config;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.UI;

namespace WuxiaRoguelite.EditorTools
{
    public static class ArchitectureSceneAutomation
    {
        private const string ScenePath = "Assets/Scenes/MainPrototype_Architecture.unity";
        private const string DatabasePath = "Assets/GameData/Generated/GameDatabase.asset";
        private const string CatalogPath = "Assets/Scripts/Config/SpawnPrefabCatalog.asset";
        private const string RegularFontPath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular-Subset.otf";
        private const string BoldFontPath = "Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.otf";

        private static readonly Color Ink = new Color32(20, 24, 30, 238);
        private static readonly Color InkSoft = new Color32(25, 31, 39, 218);
        private static readonly Color Paper = new Color32(232, 223, 198, 255);
        private static readonly Color Gold = new Color32(206, 164, 82, 255);
        private static readonly Color Cinnabar = new Color32(143, 48, 45, 255);
        private static readonly Color Jade = new Color32(54, 137, 112, 255);

        private static Font regularFont;
        private static Font boldFont;

        [MenuItem("37 MiniGame/Architecture/Rebuild Architecture Scene")]
        public static void RebuildArchitectureScene()
        {
            GameDatabase database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (database == null)
            {
                throw new InvalidOperationException($"未找到 GameDatabase：{DatabasePath}");
            }

            regularFont = AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath);
            boldFont = AssetDatabase.LoadAssetAtPath<Font>(BoldFontPath);
            if (regularFont == null || boldFont == null)
            {
                throw new InvalidOperationException("未找到 uGUI 中文字体资源。");
            }

            BuildArchitecturePrefabsAndCatalog();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SpawnPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<SpawnPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException($"未找到 SpawnPrefabCatalog：{CatalogPath}");
            }

            GameObject previousRoot = FindRoot(scene, "ArchitectureRoot");
            if (previousRoot != null)
            {
                if (Selection.activeGameObject == previousRoot ||
                    (Selection.activeTransform != null && Selection.activeTransform.IsChildOf(previousRoot.transform)))
                {
                    Selection.activeObject = null;
                }

                UnityEngine.Object.DestroyImmediate(previousRoot);
            }

            GameObject architectureRoot = CreateObject("ArchitectureRoot", null);
            architectureRoot.transform.position = Vector3.zero;

            GameObject dataObject = CreateObject("Data", architectureRoot.transform);
            GameDatabaseProvider provider = dataObject.AddComponent<GameDatabaseProvider>();
            SetReference(provider, "database", database);

            GameObject managerRoot = CreateObject("Manager", architectureRoot.transform);
            CharacterManager characterManager = CreateObject("CharacterManager", managerRoot.transform)
                .AddComponent<CharacterManager>();
            BattleRunner battleRunner = CreateObject("BattleRunner", managerRoot.transform)
                .AddComponent<BattleRunner>();
            RunManager runManager = CreateObject("RunManager", managerRoot.transform)
                .AddComponent<RunManager>();

            SetReference(characterManager, "databaseProvider", provider);
            SetReference(battleRunner, "characterManager", characterManager);
            SetReference(runManager, "characterManager", characterManager);
            SetReference(runManager, "battleRunner", battleRunner);

            GameObject regionRoot = CreateObject("SpawnRegions", architectureRoot.transform);
            SpawnRegion[] regions =
            {
                CreateRegion(regionRoot.transform, "Region_EastForest", "east_forest", new Vector3(12f, 0.08f, 7f), new Vector3(14f, 0f, 12f)),
                CreateRegion(regionRoot.transform, "Region_SouthQuarry", "south_quarry", new Vector3(10f, 0.08f, -10f), new Vector3(15f, 0f, 11f)),
                CreateRegion(regionRoot.transform, "Region_NorthPass", "north_pass", new Vector3(-9f, 0.08f, 10f), new Vector3(16f, 0f, 9f)),
                CreateRegion(regionRoot.transform, "Region_MainMap", "main_map", new Vector3(0f, 0.08f, 0f), new Vector3(38f, 0f, 31f))
            };

            GameObject spawnedRoot = CreateObject("RuntimeSpawnedObjects", architectureRoot.transform);
            GameObject spawnerRoot = CreateObject("WorldSpawners", architectureRoot.transform);
            EnemySpawner enemySpawner = CreateObject("EnemySpawner", spawnerRoot.transform).AddComponent<EnemySpawner>();
            ItemSpawner itemSpawner = CreateObject("ItemSpawner", spawnerRoot.transform).AddComponent<ItemSpawner>();
            CaveSpawner caveSpawner = CreateObject("CaveSpawner", spawnerRoot.transform).AddComponent<CaveSpawner>();

            BindSpawner(enemySpawner, provider, catalog, regions, spawnedRoot.transform, runManager);
            BindSpawner(itemSpawner, provider, catalog, regions, spawnedRoot.transform, runManager);
            BindSpawner(caveSpawner, provider, catalog, regions, spawnedRoot.transform, runManager);
            SetReference(runManager, "enemySpawner", enemySpawner);
            SetReference(runManager, "itemSpawner", itemSpawner);
            SetReference(runManager, "caveSpawner", caveSpawner);

            PlayerController playerController = EnsurePlayerInteractionActor(scene);
            SetReference(runManager, "playerController", playerController);
            DisableLegacyArchitectureConflicts(scene);
            BuildUgui(architectureRoot.transform, runManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = architectureRoot;
            Debug.Log("Architecture 自动化完成：核心引用、uGUI、SpawnRegion、Prefab 与 Catalog 已重建。", architectureRoot);
        }

        private static SpawnPrefabCatalog BuildArchitecturePrefabsAndCatalog()
        {
            EnsureFolder("Assets/Prefabs/Architecture");
            EnsureFolder("Assets/Prefabs/Architecture/Enemies");
            EnsureFolder("Assets/Prefabs/Architecture/Items");
            EnsureFolder("Assets/Prefabs/Architecture/Cave");

            PrefabPlan[] plans =
            {
                new PrefabPlan("prefab_enemy_bandit", "Assets/Prefabs/Enemy/山贼喽啰.prefab", "Assets/Prefabs/Architecture/Enemies/山贼喽啰.prefab", InteractionKind.Enemy, "enemy_bandit", "reward_normal_enemy"),
                new PrefabPlan("prefab_enemy_bamboo", "Assets/Prefabs/Enemy/流寇.prefab", "Assets/Prefabs/Architecture/Enemies/流寇.prefab", InteractionKind.Enemy, "enemy_bamboo_puppet", "reward_normal_enemy"),
                new PrefabPlan("prefab_enemy_ink_wolf", "Assets/Prefabs/Enemy/灰岩巨鼠.prefab", "Assets/Prefabs/Architecture/Enemies/灰岩巨鼠.prefab", InteractionKind.Enemy, "enemy_ink_wolf", "reward_normal_enemy"),
                new PrefabPlan("prefab_enemy_stone_ape", "Assets/Prefabs/Enemy/黑风刀客.prefab", "Assets/Prefabs/Architecture/Enemies/黑风刀客.prefab", InteractionKind.Enemy, "enemy_stone_ape", "reward_elite_enemy"),
                new PrefabPlan("prefab_treasure", "Assets/Prefabs/Items/东市宝箱.prefab", "Assets/Prefabs/Architecture/Items/东市宝箱.prefab", InteractionKind.Treasure, string.Empty, "reward_treasure"),
                new PrefabPlan("prefab_herb", "Assets/Prefabs/Items/北门药草.prefab", "Assets/Prefabs/Architecture/Items/北门药草.prefab", InteractionKind.Herb, string.Empty, "reward_herb"),
                new PrefabPlan("prefab_hidden_cave", "Assets/Prefabs/Canvas/古藏秘窟.prefab", "Assets/Prefabs/Architecture/Cave/古藏秘窟.prefab", InteractionKind.Cave, string.Empty, string.Empty)
            };

            var createdPrefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (PrefabPlan plan in plans)
            {
                createdPrefabs.Add(plan.PrefabId, BuildArchitecturePrefab(plan));
            }

            SpawnPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<SpawnPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SpawnPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = plans.Length;
            for (int i = 0; i < plans.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("prefabId").stringValue = plans[i].PrefabId;
                entry.FindPropertyRelative("prefab").objectReferenceValue = createdPrefabs[plans[i].PrefabId];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static GameObject BuildArchitecturePrefab(PrefabPlan plan)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(plan.SourcePath) == null)
            {
                throw new InvalidOperationException($"缺少源 Prefab：{plan.SourcePath}");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(plan.DestinationPath) != null)
            {
                AssetDatabase.DeleteAsset(plan.DestinationPath);
            }

            if (!AssetDatabase.CopyAsset(plan.SourcePath, plan.DestinationPath))
            {
                throw new InvalidOperationException($"无法复制 Prefab：{plan.SourcePath} -> {plan.DestinationPath}");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(plan.DestinationPath);
            try
            {
                RemoveLegacyDependencyComponents(root);
                EncounterTrigger[] legacyTriggers = root.GetComponentsInChildren<EncounterTrigger>(true);
                foreach (EncounterTrigger legacyTrigger in legacyTriggers)
                {
                    UnityEngine.Object.DestroyImmediate(legacyTrigger);
                }

                Collider collider = FindInteractionCollider(root);
                if (collider == null)
                {
                    throw new InvalidOperationException($"Prefab 缺少 Collider：{plan.SourcePath}");
                }

                collider.isTrigger = true;
                GameObject target = collider.gameObject;
                MonoBehaviour interaction = ConfigureInteraction(target, plan);
                WorldInteractionTrigger trigger = target.GetComponent<WorldInteractionTrigger>();
                if (trigger == null)
                {
                    trigger = target.AddComponent<WorldInteractionTrigger>();
                }

                SetReference(trigger, "interactableComponent", interaction);
                PrefabUtility.SaveAsPrefabAsset(root, plan.DestinationPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(plan.DestinationPath);
        }

        private static void RemoveLegacyDependencyComponents(GameObject root)
        {
            foreach (EnemyLevelLabel component in root.GetComponentsInChildren<EnemyLevelLabel>(true))
            {
                UnityEngine.Object.DestroyImmediate(component);
            }

            foreach (TreasureMapIndicator component in root.GetComponentsInChildren<TreasureMapIndicator>(true))
            {
                UnityEngine.Object.DestroyImmediate(component);
            }

            foreach (CaveEntranceIndicator component in root.GetComponentsInChildren<CaveEntranceIndicator>(true))
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static Collider FindInteractionCollider(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider.isTrigger)
                {
                    return collider;
                }
            }

            Collider rootCollider = root.GetComponent<Collider>();
            return rootCollider != null ? rootCollider : colliders.Length > 0 ? colliders[0] : null;
        }

        private static MonoBehaviour ConfigureInteraction(GameObject target, PrefabPlan plan)
        {
            switch (plan.Kind)
            {
                case InteractionKind.Enemy:
                {
                    EnemyEncounter encounter = target.GetComponent<EnemyEncounter>();
                    if (encounter == null)
                    {
                        encounter = target.AddComponent<EnemyEncounter>();
                    }

                    SetString(encounter, "characterId", plan.CharacterId);
                    SetString(encounter, "rewardId", plan.RewardId);
                    SetBool(encounter, "caveBattle", false);
                    return encounter;
                }
                case InteractionKind.Treasure:
                {
                    TreasureChest treasure = target.GetComponent<TreasureChest>();
                    if (treasure == null)
                    {
                        treasure = target.AddComponent<TreasureChest>();
                    }

                    SetString(treasure, "rewardId", plan.RewardId);
                    return treasure;
                }
                case InteractionKind.Herb:
                {
                    HerbPickup herb = target.GetComponent<HerbPickup>();
                    if (herb == null)
                    {
                        herb = target.AddComponent<HerbPickup>();
                    }

                    SetString(herb, "rewardId", plan.RewardId);
                    return herb;
                }
                case InteractionKind.Cave:
                {
                    CaveEntrance cave = target.GetComponent<CaveEntrance>();
                    return cave != null ? cave : target.AddComponent<CaveEntrance>();
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void BuildUgui(Transform architectureRoot, RunManager runManager)
        {
            GameObject uiRoot = CreateObject("UGUI", architectureRoot);
            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GameUiPresenter));
            canvasObject.transform.SetParent(uiRoot.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetParent(uiRoot.transform, false);

            MainMenuView mainMenu = BuildMainMenu(canvasObject.transform);
            HudView hud = BuildHud(canvasObject.transform);
            BattleView battle = BuildBattle(canvasObject.transform);
            CaveView cave = BuildCave(canvasObject.transform);
            LevelUpView levelUp = BuildLevelUp(canvasObject.transform);
            ResultView result = BuildResult(canvasObject.transform);

            GameUiPresenter presenter = canvasObject.GetComponent<GameUiPresenter>();
            SetReference(presenter, "runManager", runManager);
            SetReference(presenter, "mainMenuView", mainMenu);
            SetReference(presenter, "hudView", hud);
            SetReference(presenter, "battleView", battle);
            SetReference(presenter, "caveView", cave);
            SetReference(presenter, "levelUpView", levelUp);
            SetReference(presenter, "resultView", result);

            mainMenu.gameObject.SetActive(true);
            hud.gameObject.SetActive(false);
            battle.gameObject.SetActive(false);
            cave.gameObject.SetActive(false);
            levelUp.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
        }

        private static MainMenuView BuildMainMenu(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "MainMenuPanel", Ink, true);
            MainMenuView view = panel.gameObject.AddComponent<MainMenuView>();
            Text title = CreateText(panel, "Title", "一炷江湖", 76, Paper, TextAnchor.MiddleCenter, true);
            SetCentered(title.rectTransform, new Vector2(0f, 150f), new Vector2(900f, 110f));
            Text subtitle = CreateText(panel, "Subtitle", "六十息闯荡 · 碰怪自动交锋 · 终局决战", 28, Gold, TextAnchor.MiddleCenter, false);
            SetCentered(subtitle.rectTransform, new Vector2(0f, 72f), new Vector2(1000f, 60f));
            Button startButton = CreateButton(panel, "StartButton", "踏入江湖", 34, out _);
            SetCentered((RectTransform)startButton.transform, new Vector2(0f, -70f), new Vector2(330f, 86f));
            SetReferences(view, new Dictionary<string, UnityEngine.Object>
            {
                { "root", panel.gameObject },
                { "startButton", startButton }
            });
            return view;
        }

        private static HudView BuildHud(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "HudPanel", Color.clear, false);
            HudView view = panel.gameObject.AddComponent<HudView>();
            RectTransform card = CreatePanel(panel, "HudCard", InkSoft, true);
            SetTopLeft(card, new Vector2(24f, -24f), new Vector2(530f, 190f));

            Text timer = CreateText(card, "TimerText", "余时 60.0s", 30, Gold, TextAnchor.MiddleLeft, true);
            SetTopLeft(timer.rectTransform, new Vector2(20f, -12f), new Vector2(230f, 42f));
            Text health = CreateText(card, "HealthText", "气血 100/100", 24, Paper, TextAnchor.MiddleRight, false);
            SetTopLeft(health.rectTransform, new Vector2(265f, -15f), new Vector2(240f, 38f));
            Slider healthSlider = CreateSlider(card, "HealthSlider", Jade);
            SetTopLeft((RectTransform)healthSlider.transform, new Vector2(20f, -61f), new Vector2(485f, 24f));
            Text progression = CreateText(card, "ProgressionText", "境界 1 · 修为 0", 22, Paper, TextAnchor.MiddleLeft, false);
            SetTopLeft(progression.rectTransform, new Vector2(20f, -96f), new Vector2(285f, 34f));
            Text currency = CreateText(card, "CurrencyText", "铜钱 0", 22, Gold, TextAnchor.MiddleRight, false);
            SetTopLeft(currency.rectTransform, new Vector2(310f, -96f), new Vector2(195f, 34f));
            Text status = CreateText(card, "StatusText", "准备踏入江湖", 20, Paper, TextAnchor.UpperLeft, false);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.verticalOverflow = VerticalWrapMode.Truncate;
            SetTopLeft(status.rectTransform, new Vector2(20f, -132f), new Vector2(485f, 46f));

            SetReferences(view, new Dictionary<string, UnityEngine.Object>
            {
                { "root", panel.gameObject },
                { "timerText", timer },
                { "healthText", health },
                { "healthSlider", healthSlider },
                { "progressionText", progression },
                { "currencyText", currency },
                { "statusText", status }
            });
            return view;
        }

        private static BattleView BuildBattle(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "BattlePanel", Color.clear, false);
            BattleView view = panel.gameObject.AddComponent<BattleView>();
            RectTransform card = CreatePanel(panel, "BattleCard", InkSoft, true);
            SetBottomCenter(card, new Vector2(0f, 28f), new Vector2(940f, 238f));

            Text title = CreateText(card, "TitleText", "自动战斗", 30, Gold, TextAnchor.MiddleCenter, true);
            SetTopLeft(title.rectTransform, new Vector2(20f, -12f), new Vector2(900f, 42f));
            Text playerHealth = CreateText(card, "PlayerHealthText", "少侠 100/100", 22, Paper, TextAnchor.MiddleLeft, false);
            SetTopLeft(playerHealth.rectTransform, new Vector2(28f, -58f), new Vector2(410f, 34f));
            Slider playerSlider = CreateSlider(card, "PlayerHealthSlider", Jade);
            SetTopLeft((RectTransform)playerSlider.transform, new Vector2(28f, -94f), new Vector2(410f, 22f));
            Text enemyHealth = CreateText(card, "EnemyHealthText", "敌手 100/100", 22, Paper, TextAnchor.MiddleRight, false);
            SetTopLeft(enemyHealth.rectTransform, new Vector2(502f, -58f), new Vector2(410f, 34f));
            Slider enemySlider = CreateSlider(card, "EnemyHealthSlider", Cinnabar);
            SetTopLeft((RectTransform)enemySlider.transform, new Vector2(502f, -94f), new Vector2(410f, 22f));
            Text effect = CreateText(card, "EffectText", "护盾 0 · 敌方破甲 0 · 毒层 0", 20, Paper, TextAnchor.MiddleLeft, false);
            SetTopLeft(effect.rectTransform, new Vector2(28f, -137f), new Vector2(560f, 34f));
            Text battleTime = CreateText(card, "BattleTimeText", "战斗 0.0s · 主时间继续", 20, Gold, TextAnchor.MiddleRight, false);
            SetTopLeft(battleTime.rectTransform, new Vector2(590f, -137f), new Vector2(322f, 34f));
            Text hint = CreateText(card, "AutoHint", "战斗自动进行，无需操作", 18, new Color(Paper.r, Paper.g, Paper.b, 0.75f), TextAnchor.MiddleCenter, false);
            SetTopLeft(hint.rectTransform, new Vector2(28f, -181f), new Vector2(884f, 32f));

            SetReferences(view, new Dictionary<string, UnityEngine.Object>
            {
                { "root", panel.gameObject },
                { "titleText", title },
                { "playerHealthText", playerHealth },
                { "playerHealthSlider", playerSlider },
                { "enemyHealthText", enemyHealth },
                { "enemyHealthSlider", enemySlider },
                { "effectText", effect },
                { "battleTimeText", battleTime }
            });
            return view;
        }

        private static CaveView BuildCave(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "CavePanel", new Color(0.02f, 0.03f, 0.04f, 0.78f), true);
            CaveView view = panel.gameObject.AddComponent<CaveView>();
            RectTransform card = CreatePanel(panel, "CaveCard", Ink, true);
            SetCentered(card, Vector2.zero, new Vector2(760f, 360f));
            Text title = CreateText(card, "Title", "古藏秘窟", 42, Gold, TextAnchor.MiddleCenter, true);
            SetTopLeft(title.rectTransform, new Vector2(30f, -24f), new Vector2(700f, 60f));
            Text description = CreateText(card, "DescriptionText", "隐藏洞穴中主地图倒计时暂停。", 25, Paper, TextAnchor.MiddleCenter, false);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetTopLeft(description.rectTransform, new Vector2(60f, -102f), new Vector2(640f, 110f));
            Button exit = CreateButton(card, "ExitButton", "返回主地图", 28, out _);
            SetBottomCenter((RectTransform)exit.transform, new Vector2(0f, 34f), new Vector2(280f, 70f));
            SetReferences(view, new Dictionary<string, UnityEngine.Object>
            {
                { "root", panel.gameObject },
                { "descriptionText", description },
                { "exitButton", exit }
            });
            return view;
        }

        private static LevelUpView BuildLevelUp(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "LevelUpPanel", new Color(0.02f, 0.03f, 0.04f, 0.86f), true);
            LevelUpView view = panel.gameObject.AddComponent<LevelUpView>();
            Text title = CreateText(panel, "Title", "修为突破 · 择一武学", 44, Gold, TextAnchor.MiddleCenter, true);
            SetCentered(title.rectTransform, new Vector2(0f, 330f), new Vector2(900f, 70f));

            var buttons = new Button[3];
            var labels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                buttons[i] = CreateButton(panel, $"ChoiceButton{i + 1}", $"武学选择 {i + 1}", 25, out labels[i]);
                SetCentered((RectTransform)buttons[i].transform, new Vector2(0f, 190f - i * 145f), new Vector2(820f, 112f));
                labels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                labels[i].verticalOverflow = VerticalWrapMode.Truncate;
            }

            Button reroll = CreateButton(panel, "RerollButton", "刷新（1）", 24, out Text rerollText);
            SetCentered((RectTransform)reroll.transform, new Vector2(0f, -280f), new Vector2(260f, 66f));
            SetReferences(view, new Dictionary<string, UnityEngine.Object>
            {
                { "root", panel.gameObject },
                { "rerollButton", reroll },
                { "rerollText", rerollText }
            });
            SetReferenceArray(view, "choiceButtons", buttons);
            SetReferenceArray(view, "choiceLabels", labels);
            return view;
        }

        private static ResultView BuildResult(Transform parent)
        {
            RectTransform panel = CreatePanel(parent, "ResultPanel", new Color(0.02f, 0.03f, 0.04f, 0.88f), true);
            ResultView view = panel.gameObject.AddComponent<ResultView>();
            RectTransform card = CreatePanel(panel, "ResultCard", Ink, true);
            SetCentered(card, Vector2.zero, new Vector2(820f, 500f));
            Text result = CreateText(card, "ResultText", "名震江湖", 56, Gold, TextAnchor.MiddleCenter, true);
            SetTopLeft(result.rectTransform, new Vector2(50f, -42f), new Vector2(720f, 82f));
            Text summary = CreateText(card, "SummaryText", "本局统计", 27, Paper, TextAnchor.MiddleCenter, false);
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetTopLeft(summary.rectTransform, new Vector2(70f, -145f), new Vector2(680f, 190f));
            Button restart = CreateButton(card, "RestartButton", "再入江湖", 30, out _);
            SetBottomCenter((RectTransform)restart.transform, new Vector2(0f, 44f), new Vector2(300f, 76f));
            SetReferences(view, new Dictionary<string, UnityEngine.Object>
            {
                { "root", panel.gameObject },
                { "resultText", result },
                { "summaryText", summary },
                { "restartButton", restart }
            });
            return view;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color, bool raycastTarget)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            Stretch(rect);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Color color,
            TextAnchor alignment,
            bool bold)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = bold ? boldFont : regularFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, int fontSize, out Text labelText)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = Cinnabar;
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Cinnabar;
            colors.highlightedColor = new Color32(174, 69, 60, 255);
            colors.pressedColor = new Color32(105, 34, 33, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(70, 70, 70, 180);
            button.colors = colors;

            labelText = CreateText(gameObject.transform, "Label", label, fontSize, Paper, TextAnchor.MiddleCenter, true);
            Stretch(labelText.rectTransform);
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Color fillColor)
        {
            GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            RectTransform background = CreatePanel(sliderObject.transform, "Background", new Color32(55, 58, 64, 255), false);
            Stretch(background);
            RectTransform fillArea = CreateRect("Fill Area", sliderObject.transform);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);
            RectTransform fill = CreatePanel(fillArea, "Fill", fillColor, false);
            Stretch(fill);
            RectTransform handleArea = CreateRect("Handle Slide Area", sliderObject.transform);
            Stretch(handleArea);
            RectTransform handle = CreatePanel(handleArea, "Handle", Gold, false);
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(0f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = new Vector2(12f, 30f);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static PlayerController EnsurePlayerInteractionActor(Scene scene)
        {
            GameObject player = FindRoot(scene, "Player");
            if (player == null)
            {
                throw new InvalidOperationException("架构场景缺少 Player 根对象。");
            }

            if (player.GetComponent<PlayerInteractionActor>() == null)
            {
                player.AddComponent<PlayerInteractionActor>();
            }

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                throw new InvalidOperationException("架构场景 Player 缺少 PlayerController。");
            }

            return playerController;
        }

        private static void DisableLegacyArchitectureConflicts(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                DisableBehaviourByName(root, "GameFlowController");
                DisableBehaviourByName(root, "BattleManager");
                DisableBehaviourByName(root, "PrototypeHUDController");
                DisableBehaviourByName(root, "BattleScreenController");
                DisableBehaviourByName(root, "CaveRoomController");
                DisableBehaviourByName(root, "BattleFeedbackAudio");
                DisableBehaviourByName(root, "MainMapMusicController");
            }

            EncounterTrigger[] legacyEncounters = UnityEngine.Object.FindObjectsByType<EncounterTrigger>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (EncounterTrigger encounter in legacyEncounters)
            {
                if (encounter.gameObject.scene == scene)
                {
                    encounter.gameObject.SetActive(false);
                }
            }
        }

        private static void DisableBehaviourByName(GameObject root, string typeName)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == typeName)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static SpawnRegion CreateRegion(
            Transform parent,
            string name,
            string regionId,
            Vector3 position,
            Vector3 size)
        {
            GameObject gameObject = CreateObject(name, parent);
            gameObject.transform.position = position;
            SpawnRegion region = gameObject.AddComponent<SpawnRegion>();
            SetString(region, "regionId", regionId);
            SetVector3(region, "size", size);
            return region;
        }

        private static void BindSpawner(
            MonoBehaviour spawner,
            GameDatabaseProvider provider,
            SpawnPrefabCatalog catalog,
            SpawnRegion[] regions,
            Transform spawnedRoot,
            RunManager runManager)
        {
            SetReference(spawner, "databaseProvider", provider);
            SetReference(spawner, "prefabCatalog", catalog);
            SetReferenceArray(spawner, "regions", regions);
            SetReference(spawner, "spawnedRoot", spawnedRoot);
            SetReference(spawner, "runManager", runManager);
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetReferences(UnityEngine.Object target, Dictionary<string, UnityEngine.Object> values)
        {
            var serialized = new SerializedObject(target);
            foreach (KeyValuePair<string, UnityEngine.Object> pair in values)
            {
                SerializedProperty property = serialized.FindProperty(pair.Key);
                if (property == null)
                {
                    throw new MissingFieldException(target.GetType().Name, pair.Key);
                }

                property.objectReferenceValue = pair.Value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SetReferences(target, new Dictionary<string, UnityEngine.Object> { { propertyName, value } });
        }

        private static void SetReferenceArray<T>(UnityEngine.Object target, string propertyName, T[] values)
            where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(target.GetType().Name, propertyName);
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(target.GetType().Name, propertyName);
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(target.GetType().Name, propertyName);
            }

            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector3(UnityEngine.Object target, string propertyName, Vector3 value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(target.GetType().Name, propertyName);
            }

            property.vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBottomCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private readonly struct PrefabPlan
        {
            public PrefabPlan(
                string prefabId,
                string sourcePath,
                string destinationPath,
                InteractionKind kind,
                string characterId,
                string rewardId)
            {
                PrefabId = prefabId;
                SourcePath = sourcePath;
                DestinationPath = destinationPath;
                Kind = kind;
                CharacterId = characterId;
                RewardId = rewardId;
            }

            public string PrefabId { get; }
            public string SourcePath { get; }
            public string DestinationPath { get; }
            public InteractionKind Kind { get; }
            public string CharacterId { get; }
            public string RewardId { get; }
        }

        private enum InteractionKind
        {
            Enemy,
            Treasure,
            Herb,
            Cave
        }
    }
}
#endif
