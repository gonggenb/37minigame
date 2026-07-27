#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WuxiaRoguelite.Architecture.Battle;
using WuxiaRoguelite.Architecture.GameFlow;
using WuxiaRoguelite.Architecture.Spawning;
using WuxiaRoguelite.Architecture.UI;
using WuxiaRoguelite.CameraTools;
using WuxiaRoguelite.Config;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.EditorTools
{
    public static class InkArtSceneAutomation
    {
        private const string SourceArtRoot = "Assets/Art/Q版水墨国风（行侠仗义五千年）";
        private const string RuntimeArtRoot = "Assets/Art/RuntimeInkArt";
        private const string SourceScenePath = "Assets/Scenes/MainPrototype_Architecture.unity";
        private const string ScenePath = "Assets/Scenes/MainPrototype_InkArt.unity";
        private const string CatalogPath = "Assets/GameData/Runtime/InkArtCatalog.asset";
        private const string SpawnCatalogPath = "Assets/GameData/Runtime/InkSpawnPrefabCatalog.asset";
        private const string RegularFontPath = "Assets/Resources/Fonts/NotoSansCJKsc-Regular-Subset.otf";
        private const string BoldFontPath = "Assets/Resources/Fonts/NotoSansCJKsc-Bold-Subset.otf";

        private static readonly Color Paper = new Color32(238, 228, 204, 255);
        private static readonly Color Ink = new Color32(30, 31, 29, 238);
        private static readonly Color InkSoft = new Color32(50, 49, 43, 224);
        private static readonly Color Cinnabar = new Color32(158, 57, 43, 255);
        private static readonly Color Gold = new Color32(215, 171, 77, 255);
        private static readonly Color Jade = new Color32(60, 139, 103, 255);

        private static Font regularFont;
        private static Font boldFont;

        private static readonly SpritePlan[] SpritePlans =
        {
            SpritePlan.Ui("background.portrait.main", "场景背景/zjm_bg.png", "UI/Portrait/background_main.png"),
            SpritePlan.Ui("background.landscape.main", "场景背景/beijing_001.png", "UI/Landscape/background_main.png"),
            SpritePlan.Ui("background.cave", "场景背景/wx_bg.png", "UI/Portrait/background_cave.png"),
            SpritePlan.World("world.map", "场景背景/sactx-0-2048x2048-ETC2-Textures_Map_shaolin-cc8e713e.png", "Environment/map_main.png", 100f),
            SpritePlan.World("world.landmark.east", "场景背景/taohuagu_dibiao.png", "Environment/Landmarks/landmark_east.png", 180f),
            SpritePlan.World("world.landmark.west", "场景背景/wudangpai_dibiao.png", "Environment/Landmarks/landmark_west.png", 180f),
            SpritePlan.World("world.landmark.north", "场景背景/gaibang_dibiao.png", "Environment/Landmarks/landmark_north.png", 180f),
            SpritePlan.World("world.landmark.south", "场景背景/mizong_dibiao.png", "Environment/Landmarks/landmark_south.png", 180f),
            SpritePlan.World("world.treasure", "UI界面/baoxiang.png", "Environment/treasure.png", 90f),
            SpritePlan.World("world.herb", "UI界面/hulu.png", "Environment/herb.png", 90f),
            SpritePlan.World("world.cave_entrance", "场景背景/rukou1.png", "Environment/cave_entrance.png", 180f),
            SpritePlan.Ui("ui.panel", "UI界面/common_board_xinxi_00.png", "UI/Shared/panel.png", new Vector4(48f, 48f, 48f, 48f)),
            SpritePlan.Ui("ui.button.primary", "UI界面/common_btn_big_yellow.png", "UI/Shared/button_primary.png", new Vector4(40f, 26f, 40f, 26f)),
            SpritePlan.Ui("ui.top_frame", "UI界面/common_board_top_shuimo_01.png", "UI/Shared/top_frame.png", new Vector4(48f, 28f, 48f, 28f)),
            SpritePlan.Ui("ui.boss_frame", "UI界面/zd_board_boss.png", "UI/Shared/boss_frame.png", new Vector4(32f, 24f, 32f, 24f)),
            SpritePlan.Ui("ui.health_bar", "UI界面/zd_board_jindutiao_01.png", "UI/Shared/health_bar.png", new Vector4(26f, 10f, 26f, 10f)),
            SpritePlan.Ui("effect.impact", "特效序列/jianmang_001_lrj_Tex.png", "Effects/impact.png")
        };

        private static readonly CharacterPlan[] CharacterPlans =
        {
            new CharacterPlan("player_wuxia", "nvjianke1", "Characters/Player", "idle_00", 6, 2.15f),
            new CharacterPlan("enemy_bandit", "shanzei", "Characters/Enemies/Bandit", "idle_0", 5, 1.9f),
            new CharacterPlan("enemy_bamboo_puppet", "jiguanrenou", "Characters/Enemies/Bamboo", "idle_00", 5, 1.8f),
            new CharacterPlan("enemy_ink_wolf", "yegou1", "Characters/Enemies/InkWolf", "idle_00", 5, 2.1f),
            new CharacterPlan("enemy_stone_ape", "xingxing", "Characters/Enemies/StoneApe", "idle_00", 6, 2.2f),
            new CharacterPlan("boss_fox_demon", "yiren", "Characters/Enemies/FoxBoss", "idle_00", 7, 2.45f),
            new CharacterPlan("boss_orc_warlord", "shitouren", "Characters/Enemies/OrcBoss", "idle_00", 6, 2.45f)
        };

        private static readonly IconPlan[] IconPlans =
        {
            new IconPlan("skill_sword_qi", "icon_jian_10001.png"),
            new IconPlan("skill_swift_sword", "icon_jian_10002.png"),
            new IconPlan("skill_armor_break", "icon_jian_10003.png"),
            new IconPlan("skill_venom_palm", "icon_jian_10004.png"),
            new IconPlan("skill_poison_heart", "icon_jian_10005.png"),
            new IconPlan("skill_life_drain", "icon_jian_10006.png"),
            new IconPlan("skill_iron_body", "icon_jian_10007.png"),
            new IconPlan("skill_golden_bell", "icon_jian_10008.png"),
            new IconPlan("skill_retaliation", "icon_jian_10009.png"),
            new IconPlan("equipment_qinggang_sword", "icon_jian_20001.png"),
            new IconPlan("equipment_light_scale", "icon_xiezi_10001.png"),
            new IconPlan("equipment_practice_bracer", "icon_xiezi_10002.png"),
            new IconPlan("equipment_black_iron_ring", "icon_xuantie.png"),
            new IconPlan("equipment_wanderer_cloak", "icon_xiezi_10003.png"),
            new IconPlan("equipment_poison_dart_pouch", "icon_xiezi_10004.png")
        };

        [MenuItem("37 MiniGame/Ink Art/Rebuild Ink Art Scene")]
        public static void RebuildInkArtScene()
        {
            regularFont = AssetDatabase.LoadAssetAtPath<Font>(RegularFontPath);
            boldFont = AssetDatabase.LoadAssetAtPath<Font>(BoldFontPath);
            if (regularFont == null || boldFont == null)
            {
                throw new InvalidOperationException("水墨 UI 需要现有的 Noto Sans CJK SC 字体资源。");
            }

            EnsureRuntimeFolders();
            CopyCuratedAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            InkArtCatalog catalog = BuildInkArtCatalog();
            SpawnPrefabCatalog spawnCatalog = BuildInkPrefabsAndCatalog(catalog);
            BuildScene(catalog, spawnCatalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"水墨美术场景已生成：{ScenePath}");
        }

        private static void EnsureRuntimeFolders()
        {
            string[] folders =
            {
                RuntimeArtRoot,
                $"{RuntimeArtRoot}/Environment",
                $"{RuntimeArtRoot}/Environment/Landmarks",
                $"{RuntimeArtRoot}/Characters",
                $"{RuntimeArtRoot}/Characters/Player",
                $"{RuntimeArtRoot}/Characters/Enemies",
                $"{RuntimeArtRoot}/UI",
                $"{RuntimeArtRoot}/UI/Shared",
                $"{RuntimeArtRoot}/UI/Portrait",
                $"{RuntimeArtRoot}/UI/Landscape",
                $"{RuntimeArtRoot}/Icons",
                $"{RuntimeArtRoot}/Effects",
                "Assets/Prefabs/InkArt",
                "Assets/Prefabs/InkArt/Enemies",
                "Assets/Prefabs/InkArt/Items",
                "Assets/Prefabs/InkArt/Cave",
                "Assets/GameData/Runtime"
            };

            foreach (string folder in folders)
            {
                EnsureFolder(folder);
            }
        }

        private static void CopyCuratedAssets()
        {
            foreach (SpritePlan plan in SpritePlans)
            {
                CopyAndImportSprite(plan.SourceRelativePath, plan.DestinationRelativePath, plan.PixelsPerUnit, plan.Border, plan.Pivot);
            }

            foreach (CharacterPlan character in CharacterPlans)
            {
                EnsureFolder($"{RuntimeArtRoot}/{character.DestinationFolder}");
                CopyAndImportSprite(
                    $"动画序列/{character.SourcePrefix}-{character.IdleSuffix}.png",
                    $"{character.DestinationFolder}/idle.png",
                    64f,
                    Vector4.zero,
                    new Vector2(0.5f, 0.08f));

                for (int i = 0; i < character.MoveFrameCount; i++)
                {
                    CopyAndImportSprite(
                        $"动画序列/{character.SourcePrefix}-run_{i}.png",
                        $"{character.DestinationFolder}/move_{i:00}.png",
                        64f,
                        Vector4.zero,
                        new Vector2(0.5f, 0.08f));
                }
            }

            foreach (IconPlan icon in IconPlans)
            {
                CopyAndImportSprite(
                    $"道具图标/{icon.SourceFileName}",
                    $"Icons/{icon.Id}.png",
                    100f,
                    Vector4.zero,
                    new Vector2(0.5f, 0.5f));
            }
        }

        private static void CopyAndImportSprite(
            string sourceRelativePath,
            string destinationRelativePath,
            float pixelsPerUnit,
            Vector4 border,
            Vector2 pivot)
        {
            string source = $"{SourceArtRoot}/{sourceRelativePath}";
            string destination = $"{RuntimeArtRoot}/{destinationRelativePath}";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(source) == null)
            {
                throw new FileNotFoundException($"精选水墨资源不存在：{source}");
            }

            EnsureFolder(Path.GetDirectoryName(destination)?.Replace('\\', '/'));
            if (AssetDatabase.LoadMainAssetAtPath(destination) != null)
            {
                AssetDatabase.DeleteAsset(destination);
            }

            if (!AssetDatabase.CopyAsset(source, destination))
            {
                throw new IOException($"复制水墨资源失败：{source} -> {destination}");
            }

            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(destination) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"无法取得 TextureImporter：{destination}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spritePivot = pivot;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static InkArtCatalog BuildInkArtCatalog()
        {
            InkArtCatalog catalog = AssetDatabase.LoadAssetAtPath<InkArtCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<InkArtCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var spriteEntries = new List<KeyValuePair<string, Sprite>>();
            foreach (SpritePlan plan in SpritePlans)
            {
                spriteEntries.Add(new KeyValuePair<string, Sprite>(
                    plan.Id,
                    LoadSprite($"{RuntimeArtRoot}/{plan.DestinationRelativePath}")));
            }

            foreach (IconPlan icon in IconPlans)
            {
                spriteEntries.Add(new KeyValuePair<string, Sprite>(
                    icon.Id,
                    LoadSprite($"{RuntimeArtRoot}/Icons/{icon.Id}.png")));
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty sprites = serialized.FindProperty("sprites");
            sprites.arraySize = spriteEntries.Count;
            for (int i = 0; i < spriteEntries.Count; i++)
            {
                SerializedProperty entry = sprites.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = spriteEntries[i].Key;
                entry.FindPropertyRelative("sprite").objectReferenceValue = spriteEntries[i].Value;
            }

            SerializedProperty characters = serialized.FindProperty("characters");
            characters.arraySize = CharacterPlans.Length;
            for (int i = 0; i < CharacterPlans.Length; i++)
            {
                CharacterPlan plan = CharacterPlans[i];
                SerializedProperty entry = characters.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = plan.Id;
                Sprite idle = LoadSprite($"{RuntimeArtRoot}/{plan.DestinationFolder}/idle.png");
                entry.FindPropertyRelative("portrait").objectReferenceValue = idle;
                entry.FindPropertyRelative("worldScale").floatValue = plan.WorldScale;
                SetSpriteArray(entry.FindPropertyRelative("idleFrames"), new[] { idle });

                var moveFrames = new Sprite[plan.MoveFrameCount];
                for (int frame = 0; frame < moveFrames.Length; frame++)
                {
                    moveFrames[frame] = LoadSprite($"{RuntimeArtRoot}/{plan.DestinationFolder}/move_{frame:00}.png");
                }

                SetSpriteArray(entry.FindPropertyRelative("moveFrames"), moveFrames);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static SpawnPrefabCatalog BuildInkPrefabsAndCatalog(InkArtCatalog catalog)
        {
            PrefabPlan[] plans =
            {
                PrefabPlan.Character("prefab_enemy_bandit", "Assets/Prefabs/Architecture/Enemies/山贼喽啰.prefab", "Assets/Prefabs/InkArt/Enemies/山贼喽啰.prefab", "enemy_bandit"),
                PrefabPlan.Character("prefab_enemy_bamboo", "Assets/Prefabs/Architecture/Enemies/流寇.prefab", "Assets/Prefabs/InkArt/Enemies/流寇.prefab", "enemy_bamboo_puppet"),
                PrefabPlan.Character("prefab_enemy_ink_wolf", "Assets/Prefabs/Architecture/Enemies/灰岩巨鼠.prefab", "Assets/Prefabs/InkArt/Enemies/灰岩巨鼠.prefab", "enemy_ink_wolf"),
                PrefabPlan.Character("prefab_enemy_stone_ape", "Assets/Prefabs/Architecture/Enemies/黑风刀客.prefab", "Assets/Prefabs/InkArt/Enemies/黑风刀客.prefab", "enemy_stone_ape"),
                PrefabPlan.Sprite("prefab_treasure", "Assets/Prefabs/Architecture/Items/东市宝箱.prefab", "Assets/Prefabs/InkArt/Items/东市宝箱.prefab", "world.treasure", 1.7f),
                PrefabPlan.Sprite("prefab_herb", "Assets/Prefabs/Architecture/Items/北门药草.prefab", "Assets/Prefabs/InkArt/Items/北门药草.prefab", "world.herb", 1.5f),
                PrefabPlan.Sprite("prefab_hidden_cave", "Assets/Prefabs/Architecture/Cave/古藏秘窟.prefab", "Assets/Prefabs/InkArt/Cave/古藏秘窟.prefab", "world.cave_entrance", 1.35f)
            };

            var prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (PrefabPlan plan in plans)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(plan.SourcePath) == null)
                {
                    throw new FileNotFoundException($"缺少 Architecture Prefab：{plan.SourcePath}");
                }

                if (AssetDatabase.LoadAssetAtPath<GameObject>(plan.DestinationPath) != null)
                {
                    AssetDatabase.DeleteAsset(plan.DestinationPath);
                }

                if (!AssetDatabase.CopyAsset(plan.SourcePath, plan.DestinationPath))
                {
                    throw new IOException($"复制 InkArt Prefab 失败：{plan.SourcePath}");
                }

                GameObject root = PrefabUtility.LoadPrefabContents(plan.DestinationPath);
                try
                {
                    if (!string.IsNullOrEmpty(plan.CharacterId))
                    {
                        ApplyCharacterVisual(root, catalog.GetCharacter(plan.CharacterId), null);
                    }
                    else
                    {
                        ApplyStaticVisual(root, catalog.GetSprite(plan.SpriteId), plan.Scale);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, plan.DestinationPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                prefabs[plan.PrefabId] = AssetDatabase.LoadAssetAtPath<GameObject>(plan.DestinationPath);
            }

            SpawnPrefabCatalog spawnCatalog = AssetDatabase.LoadAssetAtPath<SpawnPrefabCatalog>(SpawnCatalogPath);
            if (spawnCatalog == null)
            {
                spawnCatalog = ScriptableObject.CreateInstance<SpawnPrefabCatalog>();
                AssetDatabase.CreateAsset(spawnCatalog, SpawnCatalogPath);
            }

            var serialized = new SerializedObject(spawnCatalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = plans.Length;
            for (int i = 0; i < plans.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("prefabId").stringValue = plans[i].PrefabId;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[plans[i].PrefabId];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawnCatalog);
            AssetDatabase.SaveAssets();
            return spawnCatalog;
        }

        private static void BuildScene(InkArtCatalog catalog, SpawnPrefabCatalog spawnCatalog)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
            {
                throw new FileNotFoundException($"缺少架构场景：{SourceScenePath}");
            }

            EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                AssetDatabase.DeleteAsset(ScenePath);
            }

            if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
            {
                throw new IOException($"复制水墨场景失败：{SourceScenePath} -> {ScenePath}");
            }

            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            catalog = AssetDatabase.LoadAssetAtPath<InkArtCatalog>(CatalogPath);
            spawnCatalog = AssetDatabase.LoadAssetAtPath<SpawnPrefabCatalog>(SpawnCatalogPath);
            if (catalog == null || spawnCatalog == null)
            {
                throw new InvalidOperationException("打开水墨场景后无法重新加载 InkArt Catalog。");
            }

            RemoveLegacySerializedArt(scene);
            BuildWorldVisuals(scene, catalog);
            ApplyPlayerVisual(scene, catalog);
            BindInkSpawnCatalog(scene, spawnCatalog);
            BuildPresentation(scene, catalog);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void RemoveLegacySerializedArt(Scene scene)
        {
            GameObject architectureUgui = Find(scene, "ArchitectureRoot/UGUI");
            if (architectureUgui != null)
            {
                UnityEngine.Object.DestroyImmediate(architectureUgui);
            }

            GameObject oldPresentation = FindRoot(scene, "InkPresentationRoot");
            if (oldPresentation != null)
            {
                UnityEngine.Object.DestroyImmediate(oldPresentation);
            }

            GameObject sceneRoot = Find(scene, "Scene");
            if (sceneRoot == null)
            {
                throw new InvalidOperationException("架构场景缺少 Scene 根节点。");
            }

            GameObject oldInkRoot = Find(scene, "Scene/InkVisualRoot");
            if (oldInkRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldInkRoot);
            }

            GameObject kayKit = Find(scene, "Scene/KayKit Medieval Scenery");
            ClearChildren(kayKit);
            if (kayKit != null)
            {
                kayKit.SetActive(false);
            }

            GameObject expanded = Find(scene, "Scene/Expanded Main Map Content");
            if (expanded != null)
            {
                GameObject expandedKayKit = expanded.transform.Find("Expanded KayKit Scenery")?.gameObject;
                foreach (Transform child in expanded.transform.Cast<Transform>().ToArray())
                {
                    if (expandedKayKit != null && child.gameObject == expandedKayKit)
                    {
                        ClearChildren(expandedKayKit);
                        expandedKayKit.SetActive(false);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }
            }

            foreach (Renderer renderer in sceneRoot.GetComponentsInChildren<Renderer>(true))
            {
                UnityEngine.Object.DestroyImmediate(renderer);
            }

            GameObject plane = FindRoot(scene, "Plane");
            if (plane != null)
            {
                foreach (Renderer renderer in plane.GetComponentsInChildren<Renderer>(true))
                {
                    UnityEngine.Object.DestroyImmediate(renderer);
                }
            }

            RenderSettings.skybox = null;

            GameObject gameRoot = FindRoot(scene, "GameRoot");
            if (gameRoot != null)
            {
                DestroyBehaviourByName(gameRoot, "PrototypeHUDController");
                DestroyBehaviourByName(gameRoot, "BattleScreenController");
                DestroyBehaviourByName(gameRoot, "CaveRoomController");
            }
        }

        private static void BuildWorldVisuals(Scene scene, InkArtCatalog catalog)
        {
            GameObject sceneRoot = Find(scene, "Scene");
            GameObject visualRoot = CreateObject("InkVisualRoot", sceneRoot.transform);

            GameObject map = CreateObject("MapGround", visualRoot.transform);
            SpriteRenderer mapRenderer = map.AddComponent<SpriteRenderer>();
            mapRenderer.sprite = catalog.GetSprite("world.map");
            mapRenderer.sortingOrder = -100;
            map.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            map.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            map.transform.localScale = new Vector3(2.05f, 1.62f, 1f);

            CreateLandmark(visualRoot.transform, "EastLandmark", catalog.GetSprite("world.landmark.east"), new Vector3(12f, 0.2f, 7f), 0.75f);
            CreateLandmark(visualRoot.transform, "WestLandmark", catalog.GetSprite("world.landmark.west"), new Vector3(-11f, 0.2f, -4f), 0.72f);
            CreateLandmark(visualRoot.transform, "NorthLandmark", catalog.GetSprite("world.landmark.north"), new Vector3(-8f, 0.2f, 11f), 0.70f);
            CreateLandmark(visualRoot.transform, "SouthLandmark", catalog.GetSprite("world.landmark.south"), new Vector3(10f, 0.2f, -10f), 0.70f);

            Camera camera = FindRoot(scene, "Main Camera")?.GetComponent<Camera>();
            if (camera != null)
            {
                camera.backgroundColor = new Color32(202, 211, 201, 255);
                camera.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        private static void ApplyPlayerVisual(Scene scene, InkArtCatalog catalog)
        {
            GameObject player = FindRoot(scene, "Player");
            if (player == null)
            {
                throw new InvalidOperationException("水墨场景缺少 Player。");
            }

            GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(player);
            if (prefabRoot != null)
            {
                PrefabUtility.UnpackPrefabInstance(
                    prefabRoot,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            if (PrefabUtility.GetPrefabInstanceStatus(player) != PrefabInstanceStatus.NotAPrefab)
            {
                throw new InvalidOperationException("Player Prefab 未能在水墨场景中完全解包。");
            }

            ApplyCharacterVisual(player, catalog.GetCharacter("player_wuxia"), player.GetComponent<PlayerController>());
        }

        private static void BindInkSpawnCatalog(Scene scene, SpawnPrefabCatalog spawnCatalog)
        {
            string[] paths =
            {
                "ArchitectureRoot/WorldSpawners/EnemySpawner",
                "ArchitectureRoot/WorldSpawners/ItemSpawner",
                "ArchitectureRoot/WorldSpawners/CaveSpawner"
            };

            foreach (string path in paths)
            {
                GameObject spawnerObject = Find(scene, path);
                MonoBehaviour spawner = spawnerObject?.GetComponents<MonoBehaviour>().FirstOrDefault(component =>
                    component is EnemySpawner || component is ItemSpawner || component is CaveSpawner);
                if (spawner == null)
                {
                    throw new InvalidOperationException($"缺少生成器：{path}");
                }

                SetReference(spawner, "prefabCatalog", spawnCatalog);
            }
        }

        private static void BuildPresentation(Scene scene, InkArtCatalog catalog)
        {
            RunManager runManager = Find(scene, "ArchitectureRoot/Manager/RunManager")?.GetComponent<RunManager>();
            BattleRunner battleRunner = Find(scene, "ArchitectureRoot/Manager/BattleRunner")?.GetComponent<BattleRunner>();
            Camera camera = FindRoot(scene, "Main Camera")?.GetComponent<Camera>();
            CameraFollow cameraFollow = camera != null ? camera.GetComponent<CameraFollow>() : null;
            if (runManager == null || battleRunner == null || camera == null)
            {
                throw new InvalidOperationException("水墨场景缺少 RunManager、BattleRunner 或 Main Camera。");
            }

            GameObject root = CreateObject("InkPresentationRoot", null);
            AdaptivePresentationController controller = root.AddComponent<AdaptivePresentationController>();
            GameObject portrait = BuildLayout(root.transform, "PortraitCanvas", new Vector2(750f, 1338f), true, runManager, battleRunner, catalog);
            GameObject landscape = BuildLayout(root.transform, "LandscapeCanvas", new Vector2(1920f, 1080f), false, runManager, battleRunner, catalog);

            portrait.SetActive(true);
            landscape.SetActive(false);
            SetReference(controller, "portraitRoot", portrait);
            SetReference(controller, "landscapeRoot", landscape);
            SetReference(controller, "targetCamera", camera);
            SetReference(controller, "cameraFollow", cameraFollow);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(root.transform, false);
        }

        private static GameObject BuildLayout(
            Transform parent,
            string name,
            Vector2 referenceResolution,
            bool portrait,
            RunManager runManager,
            BattleRunner battleRunner,
            InkArtCatalog catalog)
        {
            GameObject canvasObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GameUiPresenter));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = portrait ? 220 : 210;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = portrait ? 0f : 0.5f;

            MainMenuView mainMenu = BuildMainMenu(canvasObject.transform, portrait, catalog);
            HudView hud = BuildHud(canvasObject.transform, portrait, catalog);
            BattleView battle = BuildBattle(canvasObject.transform, portrait, runManager, battleRunner, catalog);
            CaveView cave = BuildCave(canvasObject.transform, portrait, catalog);
            LevelUpView levelUp = BuildLevelUp(canvasObject.transform, portrait, catalog);
            ResultView result = BuildResult(canvasObject.transform, portrait, catalog);

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
            return canvasObject;
        }

        private static MainMenuView BuildMainMenu(Transform parent, bool portrait, InkArtCatalog catalog)
        {
            Sprite background = catalog.GetSprite(portrait ? "background.portrait.main" : "background.landscape.main");
            RectTransform panel = CreatePanel(parent, "MainMenuPanel", background, Color.white, true, false);
            MainMenuView view = panel.gameObject.AddComponent<MainMenuView>();
            RectTransform wash = CreatePanel(panel, "InkWash", null, new Color(0.08f, 0.07f, 0.055f, portrait ? 0.42f : 0.34f), false, false);
            Stretch(wash);

            Text title = CreateText(panel, "Title", "一炷江湖", portrait ? 70 : 82, Paper, TextAnchor.MiddleCenter, true);
            SetCentered(title.rectTransform, new Vector2(0f, portrait ? 310f : 190f), new Vector2(portrait ? 650f : 1100f, 120f));
            Text subtitle = CreateText(panel, "Subtitle", "六十息行侠 · 碰怪自动交锋 · 终局问鼎", portrait ? 25 : 30, Gold, TextAnchor.MiddleCenter, false);
            SetCentered(subtitle.rectTransform, new Vector2(0f, portrait ? 210f : 92f), new Vector2(portrait ? 650f : 1100f, 70f));
            Button start = CreateButton(panel, "StartButton", "踏入江湖", portrait ? 30 : 34, catalog.GetSprite("ui.button.primary"), out _);
            SetCentered((RectTransform)start.transform, new Vector2(0f, portrait ? -350f : -180f), new Vector2(portrait ? 380f : 420f, portrait ? 92f : 90f));
            SetReference(view, "root", panel.gameObject);
            SetReference(view, "startButton", start);
            return view;
        }

        private static HudView BuildHud(Transform parent, bool portrait, InkArtCatalog catalog)
        {
            RectTransform panel = CreatePanel(parent, "HudPanel", null, Color.clear, false, false);
            HudView view = panel.gameObject.AddComponent<HudView>();
            RectTransform card = CreatePanel(panel, "HudCard", catalog.GetSprite("ui.top_frame"), Color.white, false, true);
            if (portrait)
            {
                SetTopCenter(card, new Vector2(0f, -22f), new Vector2(700f, 238f));
            }
            else
            {
                SetTopLeft(card, new Vector2(24f, -24f), new Vector2(600f, 214f));
            }

            Text timer = CreateText(card, "TimerText", "余时 60.0s", portrait ? 32 : 30, Gold, TextAnchor.MiddleLeft, true);
            SetTopLeft(timer.rectTransform, new Vector2(34f, -26f), new Vector2(260f, 48f));
            Text health = CreateText(card, "HealthText", "气血 100/100", portrait ? 25 : 24, Paper, TextAnchor.MiddleRight, false);
            SetTopRight(health.rectTransform, new Vector2(-34f, -30f), new Vector2(300f, 42f));
            Slider healthSlider = CreateSlider(card, "HealthSlider", catalog.GetSprite("ui.health_bar"), Jade);
            SetTopCenter((RectTransform)healthSlider.transform, new Vector2(0f, -88f), new Vector2(portrait ? 620f : 520f, 30f));
            Text progression = CreateText(card, "ProgressionText", "境界 1 · 修为 0", portrait ? 23 : 22, Paper, TextAnchor.MiddleLeft, false);
            SetTopLeft(progression.rectTransform, new Vector2(34f, -130f), new Vector2(360f, 40f));
            Text currency = CreateText(card, "CurrencyText", "铜钱 0", portrait ? 23 : 22, Gold, TextAnchor.MiddleRight, false);
            SetTopRight(currency.rectTransform, new Vector2(-34f, -130f), new Vector2(240f, 40f));
            Text status = CreateText(card, "StatusText", "准备行走江湖", portrait ? 20 : 19, new Color(Paper.r, Paper.g, Paper.b, 0.82f), TextAnchor.MiddleCenter, false);
            SetBottomCenter(status.rectTransform, new Vector2(0f, 22f), new Vector2(portrait ? 620f : 520f, 42f));

            SetReference(view, "root", panel.gameObject);
            SetReference(view, "timerText", timer);
            SetReference(view, "healthText", health);
            SetReference(view, "healthSlider", healthSlider);
            SetReference(view, "progressionText", progression);
            SetReference(view, "currencyText", currency);
            SetReference(view, "statusText", status);
            return view;
        }

        private static BattleView BuildBattle(
            Transform parent,
            bool portrait,
            RunManager runManager,
            BattleRunner battleRunner,
            InkArtCatalog catalog)
        {
            RectTransform panel = CreatePanel(parent, "BattlePanel", null, new Color(0f, 0f, 0f, 0.72f), true, false);
            BattleView view = panel.gameObject.AddComponent<BattleView>();

            RectTransform stage = CreatePanel(panel, "InkBattleStage", null, Color.white, false, false);
            Stretch(stage);
            Image background = CreateImage(stage, "Background", catalog.GetSprite(portrait ? "background.portrait.main" : "background.landscape.main"), Color.white, false);
            Stretch(background.rectTransform);
            Image player = CreateImage(stage, "PlayerActor", null, Color.white, false);
            Image enemy = CreateImage(stage, "EnemyActor", null, Color.white, false);
            if (portrait)
            {
                SetBottomLeft(player.rectTransform, new Vector2(40f, 330f), new Vector2(300f, 420f));
                SetBottomRight(enemy.rectTransform, new Vector2(-40f, 330f), new Vector2(300f, 420f));
            }
            else
            {
                SetBottomLeft(player.rectTransform, new Vector2(180f, 150f), new Vector2(430f, 520f));
                SetBottomRight(enemy.rectTransform, new Vector2(-180f, 150f), new Vector2(430f, 520f));
            }

            InkBattleStage battleStage = stage.gameObject.AddComponent<InkBattleStage>();
            battleStage.Configure(
                runManager,
                battleRunner,
                catalog,
                background,
                player,
                enemy,
                portrait ? "background.portrait.main" : "background.landscape.main");
            EditorUtility.SetDirty(battleStage);

            RectTransform card = CreatePanel(panel, "BattleCard", catalog.GetSprite("ui.boss_frame"), Color.white, true, true);
            if (portrait)
            {
                SetBottomCenter(card, new Vector2(0f, 34f), new Vector2(700f, 300f));
            }
            else
            {
                SetTopCenter(card, new Vector2(0f, -28f), new Vector2(980f, 235f));
            }

            Text title = CreateText(card, "TitleText", "自动战斗", portrait ? 32 : 34, Gold, TextAnchor.MiddleCenter, true);
            SetTopCenter(title.rectTransform, new Vector2(0f, -18f), new Vector2(600f, 50f));
            Text playerHealth = CreateText(card, "PlayerHealthText", "少侠 100/100", portrait ? 21 : 22, Paper, TextAnchor.MiddleLeft, false);
            SetTopLeft(playerHealth.rectTransform, new Vector2(32f, -72f), new Vector2(portrait ? 300f : 420f, 36f));
            Text enemyHealth = CreateText(card, "EnemyHealthText", "强敌 100/100", portrait ? 21 : 22, Paper, TextAnchor.MiddleRight, false);
            SetTopRight(enemyHealth.rectTransform, new Vector2(-32f, -72f), new Vector2(portrait ? 300f : 420f, 36f));
            Slider playerSlider = CreateSlider(card, "PlayerHealthSlider", catalog.GetSprite("ui.health_bar"), Jade);
            Slider enemySlider = CreateSlider(card, "EnemyHealthSlider", catalog.GetSprite("ui.health_bar"), Cinnabar);
            float sliderWidth = portrait ? 294f : 405f;
            SetTopLeft((RectTransform)playerSlider.transform, new Vector2(32f, -112f), new Vector2(sliderWidth, 25f));
            SetTopRight((RectTransform)enemySlider.transform, new Vector2(-32f, -112f), new Vector2(sliderWidth, 25f));
            Text effect = CreateText(card, "EffectText", "护盾 0 · 破甲 0 · 毒层 0", portrait ? 19 : 20, Paper, TextAnchor.MiddleCenter, false);
            SetTopCenter(effect.rectTransform, new Vector2(0f, -158f), new Vector2(portrait ? 620f : 820f, 38f));
            Text battleTime = CreateText(card, "BattleTimeText", "战斗 0.0s · 主时间继续", portrait ? 18 : 20, Gold, TextAnchor.MiddleCenter, false);
            SetBottomCenter(battleTime.rectTransform, new Vector2(0f, 30f), new Vector2(portrait ? 620f : 820f, 38f));

            SetReference(view, "root", panel.gameObject);
            SetReference(view, "titleText", title);
            SetReference(view, "playerHealthText", playerHealth);
            SetReference(view, "playerHealthSlider", playerSlider);
            SetReference(view, "enemyHealthText", enemyHealth);
            SetReference(view, "enemyHealthSlider", enemySlider);
            SetReference(view, "effectText", effect);
            SetReference(view, "battleTimeText", battleTime);
            return view;
        }

        private static CaveView BuildCave(Transform parent, bool portrait, InkArtCatalog catalog)
        {
            RectTransform panel = CreatePanel(parent, "CavePanel", catalog.GetSprite("background.cave"), Color.white, true, false);
            CaveView view = panel.gameObject.AddComponent<CaveView>();
            RectTransform card = CreatePanel(panel, "CaveCard", catalog.GetSprite("ui.panel"), Color.white, true, true);
            SetCentered(card, Vector2.zero, new Vector2(portrait ? 660f : 840f, portrait ? 470f : 390f));
            Text title = CreateText(card, "Title", "古藏秘窟", portrait ? 42 : 46, Gold, TextAnchor.MiddleCenter, true);
            SetTopCenter(title.rectTransform, new Vector2(0f, -42f), new Vector2(portrait ? 580f : 720f, 70f));
            Text description = CreateText(card, "DescriptionText", "隐藏洞穴中主地图倒计时暂停。", portrait ? 25 : 27, Ink, TextAnchor.MiddleCenter, false);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetCentered(description.rectTransform, new Vector2(0f, 20f), new Vector2(portrait ? 540f : 680f, 150f));
            Button exit = CreateButton(card, "ExitButton", "返回主地图", portrait ? 27 : 29, catalog.GetSprite("ui.button.primary"), out _);
            SetBottomCenter((RectTransform)exit.transform, new Vector2(0f, 42f), new Vector2(300f, 76f));
            SetReference(view, "root", panel.gameObject);
            SetReference(view, "descriptionText", description);
            SetReference(view, "exitButton", exit);
            return view;
        }

        private static LevelUpView BuildLevelUp(Transform parent, bool portrait, InkArtCatalog catalog)
        {
            RectTransform panel = CreatePanel(parent, "LevelUpPanel", null, new Color(0.04f, 0.045f, 0.04f, 0.91f), true, false);
            LevelUpView view = panel.gameObject.AddComponent<LevelUpView>();
            Text title = CreateText(panel, "Title", "修为突破 · 择一武学", portrait ? 40 : 48, Gold, TextAnchor.MiddleCenter, true);
            SetTopCenter(title.rectTransform, new Vector2(0f, portrait ? -120f : -60f), new Vector2(portrait ? 700f : 1000f, 80f));

            var buttons = new Button[3];
            var labels = new Text[3];
            var icons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                buttons[i] = CreateButton(panel, $"ChoiceButton{i + 1}", $"武学选择 {i + 1}", portrait ? 22 : 24, catalog.GetSprite("ui.panel"), out labels[i]);
                Vector2 position = portrait
                    ? new Vector2(0f, 260f - i * 250f)
                    : new Vector2(-560f + i * 560f, 0f);
                Vector2 size = portrait ? new Vector2(650f, 190f) : new Vector2(500f, 390f);
                SetCentered((RectTransform)buttons[i].transform, position, size);
                labels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                labels[i].verticalOverflow = VerticalWrapMode.Truncate;
                labels[i].alignment = portrait ? TextAnchor.MiddleLeft : TextAnchor.LowerCenter;
                if (portrait)
                {
                    labels[i].rectTransform.offsetMin = new Vector2(150f, 18f);
                }
                else
                {
                    labels[i].rectTransform.offsetMin = new Vector2(24f, 24f);
                    labels[i].rectTransform.offsetMax = new Vector2(-24f, -190f);
                }

                icons[i] = CreateImage(buttons[i].transform, "ChoiceIcon", catalog.GetSprite(IconPlans[i].Id), Color.white, false);
                if (portrait)
                {
                    SetLeftCenter(icons[i].rectTransform, new Vector2(76f, 0f), new Vector2(112f, 112f));
                }
                else
                {
                    SetTopCenter(icons[i].rectTransform, new Vector2(0f, -46f), new Vector2(150f, 150f));
                }
            }

            Button reroll = CreateButton(panel, "RerollButton", "刷新（1）", portrait ? 24 : 26, catalog.GetSprite("ui.button.primary"), out Text rerollText);
            SetBottomCenter((RectTransform)reroll.transform, new Vector2(0f, portrait ? 74f : 46f), new Vector2(280f, 72f));
            SetReference(view, "root", panel.gameObject);
            SetReference(view, "rerollButton", reroll);
            SetReference(view, "rerollText", rerollText);
            SetReferenceArray(view, "choiceButtons", buttons);
            SetReferenceArray(view, "choiceLabels", labels);
            view.ConfigureInkArt(catalog, icons);
            EditorUtility.SetDirty(view);
            return view;
        }

        private static ResultView BuildResult(Transform parent, bool portrait, InkArtCatalog catalog)
        {
            RectTransform panel = CreatePanel(parent, "ResultPanel", null, new Color(0.04f, 0.045f, 0.04f, 0.91f), true, false);
            ResultView view = panel.gameObject.AddComponent<ResultView>();
            RectTransform card = CreatePanel(panel, "ResultCard", catalog.GetSprite("ui.panel"), Color.white, true, true);
            SetCentered(card, Vector2.zero, new Vector2(portrait ? 660f : 880f, portrait ? 620f : 520f));
            Text result = CreateText(card, "ResultText", "名震江湖", portrait ? 54 : 62, Cinnabar, TextAnchor.MiddleCenter, true);
            SetTopCenter(result.rectTransform, new Vector2(0f, -70f), new Vector2(portrait ? 560f : 740f, 90f));
            Text summary = CreateText(card, "SummaryText", "本局统计", portrait ? 25 : 28, Ink, TextAnchor.MiddleCenter, false);
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetCentered(summary.rectTransform, new Vector2(0f, 20f), new Vector2(portrait ? 540f : 700f, 240f));
            Button restart = CreateButton(card, "RestartButton", "再入江湖", portrait ? 28 : 30, catalog.GetSprite("ui.button.primary"), out _);
            SetBottomCenter((RectTransform)restart.transform, new Vector2(0f, 60f), new Vector2(310f, 80f));
            SetReference(view, "root", panel.gameObject);
            SetReference(view, "resultText", result);
            SetReference(view, "summaryText", summary);
            SetReference(view, "restartButton", restart);
            return view;
        }

        private static void ApplyCharacterVisual(
            GameObject root,
            InkArtCatalog.CharacterEntry character,
            PlayerController movementSource)
        {
            if (character == null || character.idleFrames == null || character.idleFrames.Length == 0)
            {
                throw new InvalidOperationException($"角色缺少水墨帧：{root.name}");
            }

            Transform visual = FindOrCreateVisual(root).transform;
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.sprite = character.idleFrames[0];
            renderer.sortingOrder = 10;
            if (visual.GetComponent<BillboardSprite>() == null)
            {
                visual.gameObject.AddComponent<BillboardSprite>();
            }

            SpriteFrameAnimator animator = visual.GetComponent<SpriteFrameAnimator>();
            if (animator == null)
            {
                animator = visual.gameObject.AddComponent<SpriteFrameAnimator>();
            }
            animator.idleFrames = character.idleFrames;
            animator.moveFrames = character.moveFrames;
            animator.movementSource = movementSource;
            animator.framesPerSecond = 8f;
            animator.randomizeStart = movementSource == null;
            visual.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one * character.worldScale;
        }

        private static void ApplyStaticVisual(GameObject root, Sprite sprite, float scale)
        {
            Transform visual = FindOrCreateVisual(root).transform;
            SpriteFrameAnimator animator = visual.GetComponent<SpriteFrameAnimator>();
            if (animator != null)
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }
            renderer.sprite = sprite;
            renderer.sortingOrder = 8;
            if (visual.GetComponent<BillboardSprite>() == null)
            {
                visual.gameObject.AddComponent<BillboardSprite>();
            }

            visual.localPosition = new Vector3(0f, 0.75f, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one * scale;
        }

        private static GameObject FindOrCreateVisual(GameObject root)
        {
            Transform direct = root.transform.Find("SpriteVisual");
            if (direct != null)
            {
                return direct.gameObject;
            }

            SpriteRenderer existing = root.GetComponentInChildren<SpriteRenderer>(true);
            if (existing != null)
            {
                existing.gameObject.name = "SpriteVisual";
                return existing.gameObject;
            }

            return CreateObject("SpriteVisual", root.transform);
        }

        private static void CreateLandmark(Transform parent, string name, Sprite sprite, Vector3 position, float scale)
        {
            GameObject landmark = CreateObject(name, parent);
            SpriteRenderer renderer = landmark.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2;
            landmark.AddComponent<BillboardSprite>();
            landmark.transform.position = position;
            landmark.transform.localScale = Vector3.one * scale;
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            bool raycastTarget,
            bool sliced)
        {
            Image image = CreateImage(parent, name, sprite, color, raycastTarget);
            image.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            Stretch(image.rectTransform);
            return image.rectTransform;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = false;
            return image;
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

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            int fontSize,
            Sprite sprite,
            out Text labelText)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = sprite != null ? Color.white : Cinnabar;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.93f, 0.72f, 1f);
            colors.pressedColor = new Color(0.78f, 0.66f, 0.42f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            button.colors = colors;

            labelText = CreateText(gameObject.transform, "Label", label, fontSize, Ink, TextAnchor.MiddleCenter, true);
            Stretch(labelText.rectTransform);
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Sprite frame, Color fillColor)
        {
            GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            RectTransform background = CreatePanel(sliderObject.transform, "Background", frame, Color.white, false, true);
            Stretch(background);
            RectTransform fillArea = CreateRect("Fill Area", sliderObject.transform);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = new Vector2(8f, 7f);
            fillArea.offsetMax = new Vector2(-8f, -7f);
            RectTransform fill = CreatePanel(fillArea, "Fill", null, fillColor, false, false);
            Stretch(fill);
            slider.fillRect = fill;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"运行时 Sprite 未正确导入：{path}");
            }

            return sprite;
        }

        private static void SetSpriteArray(SerializedProperty property, Sprite[] sprites)
        {
            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        private static void ClearChildren(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void DestroyBehaviourByName(GameObject root, string typeName)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == typeName)
                {
                    UnityEngine.Object.DestroyImmediate(behaviour);
                }
            }
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
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static GameObject Find(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            GameObject current = FindRoot(scene, segments[0]);
            for (int i = 1; current != null && i < segments.Length; i++)
            {
                Transform child = current.transform.Find(segments[i]);
                current = child != null ? child.gameObject : null;
            }

            return current;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(target.GetType().Name, propertyName);
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
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
            EditorUtility.SetDirty(target);
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
            EditorUtility.SetDirty(target);
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
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBottomCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBottomRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetLeftCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private readonly struct SpritePlan
        {
            private SpritePlan(string id, string source, string destination, float ppu, Vector4 border, Vector2 pivot)
            {
                Id = id;
                SourceRelativePath = source;
                DestinationRelativePath = destination;
                PixelsPerUnit = ppu;
                Border = border;
                Pivot = pivot;
            }

            public string Id { get; }
            public string SourceRelativePath { get; }
            public string DestinationRelativePath { get; }
            public float PixelsPerUnit { get; }
            public Vector4 Border { get; }
            public Vector2 Pivot { get; }

            public static SpritePlan Ui(string id, string source, string destination)
            {
                return new SpritePlan(id, source, destination, 100f, Vector4.zero, new Vector2(0.5f, 0.5f));
            }

            public static SpritePlan Ui(string id, string source, string destination, Vector4 border)
            {
                return new SpritePlan(id, source, destination, 100f, border, new Vector2(0.5f, 0.5f));
            }

            public static SpritePlan World(string id, string source, string destination, float ppu)
            {
                return new SpritePlan(id, source, destination, ppu, Vector4.zero, new Vector2(0.5f, 0.08f));
            }
        }

        private readonly struct CharacterPlan
        {
            public CharacterPlan(string id, string sourcePrefix, string destinationFolder, string idleSuffix, int moveFrameCount, float worldScale)
            {
                Id = id;
                SourcePrefix = sourcePrefix;
                DestinationFolder = destinationFolder;
                IdleSuffix = idleSuffix;
                MoveFrameCount = moveFrameCount;
                WorldScale = worldScale;
            }

            public string Id { get; }
            public string SourcePrefix { get; }
            public string DestinationFolder { get; }
            public string IdleSuffix { get; }
            public int MoveFrameCount { get; }
            public float WorldScale { get; }
        }

        private readonly struct IconPlan
        {
            public IconPlan(string id, string sourceFileName)
            {
                Id = id;
                SourceFileName = sourceFileName;
            }

            public string Id { get; }
            public string SourceFileName { get; }
        }

        private readonly struct PrefabPlan
        {
            private PrefabPlan(string prefabId, string sourcePath, string destinationPath, string characterId, string spriteId, float scale)
            {
                PrefabId = prefabId;
                SourcePath = sourcePath;
                DestinationPath = destinationPath;
                CharacterId = characterId;
                SpriteId = spriteId;
                Scale = scale;
            }

            public string PrefabId { get; }
            public string SourcePath { get; }
            public string DestinationPath { get; }
            public string CharacterId { get; }
            public string SpriteId { get; }
            public float Scale { get; }

            public static PrefabPlan Character(string prefabId, string sourcePath, string destinationPath, string characterId)
            {
                return new PrefabPlan(prefabId, sourcePath, destinationPath, characterId, string.Empty, 1f);
            }

            public static PrefabPlan Sprite(string prefabId, string sourcePath, string destinationPath, string spriteId, float scale)
            {
                return new PrefabPlan(prefabId, sourcePath, destinationPath, string.Empty, spriteId, scale);
            }
        }
    }
}
#endif
