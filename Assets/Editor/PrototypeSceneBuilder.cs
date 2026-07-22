#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.CameraTools;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.EditorTools
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainPrototype.unity";
        private const string SpritePath = "Assets/Art/Generated/prototype_square.png";
        private const string TinyRoot = "Assets/Art/ThirdParty/TinySwords";
        private const string KayKitRoot = "Assets/Art/ThirdParty/KayKitMedieval/Models";
        private const string PlayerIdlePath = TinyRoot + "/Units/BlueWarrior/Warrior_Idle.png";
        private const string PlayerRunPath = TinyRoot + "/Units/BlueWarrior/Warrior_Run.png";
        private const string PlayerAttackPath = TinyRoot + "/Units/BlueWarrior/Warrior_Attack1.png";
        private const string EnemyIdlePath = TinyRoot + "/Units/RedWarrior/Warrior_Idle.png";
        private const string EnemyRunPath = TinyRoot + "/Units/RedWarrior/Warrior_Run.png";
        private const string EnemyAttackPath = TinyRoot + "/Units/RedWarrior/Warrior_Attack1.png";
        private const string EliteIdlePath = TinyRoot + "/Units/BlackWarrior/Warrior_Idle.png";
        private const string EliteRunPath = TinyRoot + "/Units/BlackWarrior/Warrior_Run.png";
        private const string EliteAttackPath = TinyRoot + "/Units/BlackWarrior/Warrior_Attack1.png";
        private const string CaveIdlePath = TinyRoot + "/Units/PurpleWarrior/Warrior_Idle.png";
        private const string CaveRunPath = TinyRoot + "/Units/PurpleWarrior/Warrior_Run.png";
        private const string CaveAttackPath = TinyRoot + "/Units/PurpleWarrior/Warrior_Attack1.png";
        private const string GoldPath = TinyRoot + "/World/Gold_Resource.png";
        private const string HerbPath = TinyRoot + "/World/Bush.png";
        private const string StatusIconPath = TinyRoot + "/UI/Avatars_01.png";
        private const string EquipmentIconPath = TinyRoot + "/UI/Icon_05.png";
        private const string HealthBarBasePath = TinyRoot + "/UI/BigBar_Base.png";
        private const string HealthBarFillPath = TinyRoot + "/UI/BigBar_Fill.png";

        [MenuItem("37 MiniGame/Build Main Prototype Scene")]
        public static void BuildMainPrototypeScene()
        {
            EnsureFolders();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            PrepareArtAssets();
            Sprite[] playerIdle = LoadFrames(PlayerIdlePath, fallbackSprite);
            Sprite[] playerRun = LoadFrames(PlayerRunPath, fallbackSprite);
            Sprite[] playerAttack = LoadFrames(PlayerAttackPath, fallbackSprite);
            Sprite[] enemyIdle = LoadFrames(EnemyIdlePath, fallbackSprite);
            Sprite[] enemyRun = LoadFrames(EnemyRunPath, fallbackSprite);
            Sprite[] enemyAttack = LoadFrames(EnemyAttackPath, fallbackSprite);
            Sprite[] eliteIdle = LoadFrames(EliteIdlePath, fallbackSprite);
            Sprite[] eliteRun = LoadFrames(EliteRunPath, fallbackSprite);
            Sprite[] eliteAttack = LoadFrames(EliteAttackPath, fallbackSprite);
            Sprite[] caveIdle = LoadFrames(CaveIdlePath, fallbackSprite);
            Sprite[] caveRun = LoadFrames(CaveRunPath, fallbackSprite);
            Sprite[] caveAttack = LoadFrames(CaveAttackPath, fallbackSprite);
            Sprite goldSprite = LoadSingleSprite(GoldPath, fallbackSprite);
            Sprite herbSprite = LoadSingleSprite(HerbPath, fallbackSprite);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            Vector3 cameraOffset = new Vector3(6f, 7.2f, -10.5f);
            Vector3 cameraLookTarget = Vector3.up * 0.85f;
            camera.transform.position = cameraOffset;
            camera.transform.rotation = Quaternion.LookRotation(cameraLookTarget - cameraOffset, Vector3.up);
            camera.orthographic = false;
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
            camera.tag = "MainCamera";

            Light sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            CreateMapGeometry();

            GameObject root = new GameObject("GameRoot");
            GameFlowController gameFlow = root.AddComponent<GameFlowController>();
            BattleManager battleManager = root.AddComponent<BattleManager>();
            PrototypeHUDController hud = root.AddComponent<PrototypeHUDController>();
            BattleScreenController battleScreen = root.AddComponent<BattleScreenController>();
            CaveRoomController caveRoom = root.AddComponent<CaveRoomController>();

            GameObject player = CreateSpriteActor("Player", playerIdle, playerRun, Vector3.zero, 1.3f);
            Rigidbody playerBody = player.AddComponent<Rigidbody>();
            playerBody.useGravity = false;
            playerBody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            CapsuleCollider playerCollider = player.AddComponent<CapsuleCollider>();
            playerCollider.radius = 0.32f;
            playerCollider.height = 1.4f;
            playerCollider.center = new Vector3(0f, 0.7f, 0f);
            PlayerEquipment playerEquipment = player.AddComponent<PlayerEquipment>();
            PlayerStats playerStats = player.AddComponent<PlayerStats>();
            playerEquipment.playerStats = playerStats;
            playerStats.equipment = playerEquipment;
            PlayerController playerController = player.AddComponent<PlayerController>();
            playerController.stats = playerStats;
            playerController.groundY = 0f;
            playerController.movementReference = camera.transform;
            player.GetComponentInChildren<SpriteFrameAnimator>().movementSource = playerController;
            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.offset = cameraOffset;
            follow.lookAtHeight = 0.85f;

            gameFlow.playerStats = playerStats;
            gameFlow.playerController = playerController;
            gameFlow.battleManager = battleManager;
            gameFlow.caveRoom = caveRoom;
            battleManager.playerStats = playerStats;
            hud.gameFlow = gameFlow;
            hud.playerStats = playerStats;
            hud.battleManager = battleManager;
            hud.statusIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(StatusIconPath);
            hud.equipmentIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(EquipmentIconPath);
            hud.healthBarBase = AssetDatabase.LoadAssetAtPath<Texture2D>(HealthBarBasePath);
            hud.healthBarFill = AssetDatabase.LoadAssetAtPath<Texture2D>(HealthBarFillPath);
            battleScreen.gameFlow = gameFlow;
            battleScreen.playerStats = playerStats;
            battleScreen.battleManager = battleManager;
            battleScreen.actorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);
            battleScreen.playerIdleFrames = playerIdle;
            battleScreen.playerAttackFrames = playerAttack;
            battleScreen.enemyIdleFrames = enemyIdle;
            battleScreen.enemyAttackFrames = enemyAttack;
            battleScreen.eliteIdleFrames = eliteIdle;
            battleScreen.eliteAttackFrames = eliteAttack;
            battleScreen.caveIdleFrames = caveIdle;
            battleScreen.caveAttackFrames = caveAttack;
            caveRoom.gameFlow = gameFlow;
            caveRoom.playerStats = playerStats;
            caveRoom.battleManager = battleManager;
            caveRoom.playerIdleFrames = playerIdle;
            caveRoom.playerRunFrames = playerRun;
            caveRoom.enemyIdleFrames = caveIdle;
            caveRoom.merchantTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(StatusIconPath);
            caveRoom.treasureTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GoldPath);

            CreateEncounter("山贼喽啰", enemyIdle, enemyRun, new Vector3(-5f, 0f, 3f), EncounterType.NormalEnemy, Stats("山贼喽啰", 35, 5, 1, 0.9f), 10, 2);
            CreateEncounter("野狼", enemyIdle, enemyRun, new Vector3(-2.2f, 0f, -4.6f), EncounterType.NormalEnemy, Stats("野狼", 28, 4, 0, 1.35f), 9, 1);
            CreateEncounter("流寇", enemyIdle, enemyRun, new Vector3(2.4f, 0f, 4f), EncounterType.NormalEnemy, Stats("流寇", 45, 6, 1, 1f), 12, 3);
            CreateEncounter("黑风刀客", eliteIdle, eliteRun, new Vector3(5.4f, 0f, -2f), EncounterType.EliteEnemy, Stats("黑风刀客", 120, 12, 3, 0.85f), 30, 10);
            CreateEncounter("北岭流寇", enemyIdle, enemyRun, new Vector3(-9f, 0f, 5.5f), EncounterType.NormalEnemy, Stats("北岭流寇", 50, 7, 2, 1f), 14, 4);
            CreateEncounter("东道悍匪", enemyIdle, enemyRun, new Vector3(10f, 0f, 2.2f), EncounterType.NormalEnemy, Stats("东道悍匪", 58, 8, 2, 0.95f), 16, 5);
            CreateEncounter("南坡恶狼", enemyIdle, enemyRun, new Vector3(6.5f, 0f, -9f), EncounterType.NormalEnemy, Stats("南坡恶狼", 42, 7, 1, 1.3f), 13, 3);
            CreateEncounter("玄衣刀客", eliteIdle, eliteRun, new Vector3(-8f, 0f, 9f), EncounterType.EliteEnemy, Stats("玄衣刀客", 135, 13, 4, 0.9f), 34, 12);

            CreateEncounter("断崖石窟", caveIdle, caveRun, new Vector3(-11f, 0f, -6f), EncounterType.HiddenCave,
                Stats("守洞武人", 160, 14, 4, 0.85f), 35, 12, 1.15f, CaveContentType.Enemy);
            CreateEncounter("隐市岩洞", caveIdle, caveRun, new Vector3(11f, 0f, -7f), EncounterType.HiddenCave,
                Stats("云游商人", 1, 0, 0, 1f), 0, 0, 1.15f, CaveContentType.Merchant);
            CreateEncounter("古藏秘窟", caveIdle, caveRun, new Vector3(-10.5f, 0f, 8f), EncounterType.HiddenCave,
                Stats("秘藏古匣", 1, 0, 0, 1f), 18, 10, 1.15f, CaveContentType.Treasure);

            CreateEncounter("东市宝箱", new[] { goldSprite }, null, new Vector3(10.5f, 0f, 7.5f), EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 15, 8, 0.9f);
            CreateEncounter("西路宝箱", new[] { goldSprite }, null, new Vector3(-12f, 0f, 1.5f), EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 12, 6, 0.9f);
            CreateEncounter("南桥药草", new[] { herbSprite }, null, new Vector3(0f, 0f, -10f), EncounterType.Herb, Stats("药草", 1, 0, 0, 1f), 0, 0, 0.85f);
            CreateEncounter("北门药草", new[] { herbSprite }, null, new Vector3(1.5f, 0f, 10f), EncounterType.Herb, Stats("药草", 1, 0, 0, 1f), 0, 0, 0.85f);

            ValidateEquipmentModel();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Main prototype scene generated: {ScenePath}");
        }

        private static void ValidateEquipmentModel()
        {
            GameObject validationObject = new GameObject("Equipment Model Validation");
            try
            {
                PlayerEquipment equipment = validationObject.AddComponent<PlayerEquipment>();
                PlayerStats stats = validationObject.AddComponent<PlayerStats>();
                equipment.playerStats = stats;
                stats.equipment = equipment;
                stats.ResetRun();

                float baseAttack = stats.runtimeStats.attack;
                EquipmentItem weapon = equipment.inventory[0];
                equipment.Equip(weapon);
                if (!Mathf.Approximately(stats.runtimeStats.attack, baseAttack + weapon.attackBonus))
                {
                    throw new InvalidOperationException("Equipment validation failed while equipping a weapon.");
                }

                equipment.Unequip(EquipmentSlot.Weapon);
                if (!Mathf.Approximately(stats.runtimeStats.attack, baseAttack))
                {
                    throw new InvalidOperationException("Equipment validation failed while unequipping a weapon.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(validationObject);
            }
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Scripts");
            CreateFolderIfMissing("Assets", "Scenes");
            CreateFolderIfMissing("Assets", "Art");
            CreateFolderIfMissing("Assets/Art", "Generated");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static Sprite GetOrCreatePrototypeSprite()
        {
            if (!File.Exists(SpritePath))
            {
                Texture2D texture = new Texture2D(16, 16);
                Color[] pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }

                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            }

            AssetDatabase.ImportAsset(SpritePath);
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static void PrepareArtAssets()
        {
            string[] sheets =
            {
                PlayerIdlePath, PlayerRunPath, PlayerAttackPath,
                EnemyIdlePath, EnemyRunPath, EnemyAttackPath,
                EliteIdlePath, EliteRunPath, EliteAttackPath,
                CaveIdlePath, CaveRunPath, CaveAttackPath
            };

            foreach (string path in sheets)
            {
                ConfigureSpriteSheet(path, 192, 192, 64f);
            }

            ConfigureSingleSprite(GoldPath, 64f);
            ConfigureSingleSprite(HerbPath, 64f);
            ConfigureUiTexture(StatusIconPath);
            ConfigureUiTexture(EquipmentIconPath);
            ConfigureUiTexture(HealthBarBasePath);
            ConfigureUiTexture(HealthBarFillPath);
        }

        private static void ConfigureSpriteSheet(string path, int frameWidth, int frameHeight, float pixelsPerUnit)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Missing sprite sheet: {path}");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            Texture2D sourceTexture = new Texture2D(2, 2);
            sourceTexture.LoadImage(File.ReadAllBytes(path), true);
            int columns = Mathf.Max(1, sourceTexture.width / frameWidth);
            int rows = Mathf.Max(1, sourceTexture.height / frameHeight);
            UnityEngine.Object.DestroyImmediate(sourceTexture);

            List<SpriteRect> spriteRects = new List<SpriteRect>(columns * rows);
            List<SpriteNameFileIdPair> nameIdPairs = new List<SpriteNameFileIdPair>(columns * rows);
            string baseName = Path.GetFileNameWithoutExtension(path);
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GUID spriteId = GUID.Generate();
                    string spriteName = $"{baseName}_{index:D2}";
                    spriteRects.Add(new SpriteRect
                    {
                        name = spriteName,
                        spriteID = spriteId,
                        rect = new Rect(column * frameWidth, row * frameHeight, frameWidth, frameHeight),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    });
                    nameIdPairs.Add(new SpriteNameFileIdPair(spriteName, spriteId));
                    index++;
                }
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects.ToArray());
            ISpriteNameFileIdDataProvider nameProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider.SetNameFileIdPairs(nameIdPairs);
            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static void ConfigureSingleSprite(string path, float pixelsPerUnit)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Missing sprite: {path}");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void ConfigureUiTexture(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Missing UI texture: {path}");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadFrames(string path, Sprite fallback)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
            return frames.Length > 0 ? frames : new[] { fallback };
        }

        private static Sprite LoadSingleSprite(string path, Sprite fallback)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? fallback;
        }

        private static void CreateMapGeometry()
        {
            Material ground = Material("Prototype_Ground", new Color(0.18f, 0.36f, 0.22f));
            Material path = Material("Prototype_Path", new Color(0.38f, 0.32f, 0.24f));
            Material wall = Material("Prototype_Wall", new Color(0.22f, 0.2f, 0.18f));

            GameObject mapRoot = new GameObject("3D Prototype Map");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Walkable Ground";
            floor.transform.SetParent(mapRoot.transform);
            floor.transform.localScale = new Vector3(3f, 1f, 2.6f);
            floor.GetComponent<Renderer>().sharedMaterial = ground;

            CreateCube("Main Dirt Road", mapRoot.transform, new Vector3(0f, 0.025f, 0f), new Vector3(3.2f, 0.05f, 23f), path);
            CreateCube("Cross Dirt Road", mapRoot.transform, new Vector3(0f, 0.03f, 0.8f), new Vector3(25f, 0.05f, 2.5f), path);
            CreateCube("North Ridge Road", mapRoot.transform, new Vector3(-6.2f, 0.028f, 7.2f), new Vector3(12f, 0.05f, 2.1f), path);
            CreateCube("South Cave Road", mapRoot.transform, new Vector3(6.5f, 0.028f, -7.2f), new Vector3(13f, 0.05f, 2.1f), path);

            CreateInvisibleBoundary("North Boundary", mapRoot.transform, new Vector3(0f, 0.55f, 13.2f), new Vector3(30f, 1.1f, 0.45f), wall);
            CreateInvisibleBoundary("South Boundary", mapRoot.transform, new Vector3(0f, 0.55f, -13.2f), new Vector3(30f, 1.1f, 0.45f), wall);
            CreateInvisibleBoundary("West Boundary", mapRoot.transform, new Vector3(-15.2f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 26f), wall);
            CreateInvisibleBoundary("East Boundary", mapRoot.transform, new Vector3(15.2f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 26f), wall);

            PlaceKayKitScenery(mapRoot.transform);
        }

        private static void PlaceKayKitScenery(Transform parent)
        {
            GameObject scenery = new GameObject("KayKit Medieval Scenery");
            scenery.transform.SetParent(parent);

            PlaceModel("house", "Village House", scenery.transform, new Vector3(-6.5f, 0f, 5.7f), 3.2f, 35f);
            PlaceModel("house", "Northern Homestead", scenery.transform, new Vector3(7.5f, 0f, 9.5f), 3.0f, -20f);
            PlaceModel("market", "Roadside Market", scenery.transform, new Vector3(6.2f, 0f, 6.1f), 3.1f, -35f);
            PlaceModel("market", "Western Caravan", scenery.transform, new Vector3(-11.5f, 0f, 3.8f), 2.8f, 25f);
            PlaceModel("well", "Village Well", scenery.transform, new Vector3(3.7f, 0f, 1.9f), 1.3f, 0f);
            PlaceModel("mine", "Cliff Cave Entrance", scenery.transform, new Vector3(-12.2f, 0f, -6.1f), 3.4f, 82f);
            PlaceModel("mine", "Hidden Market Cave", scenery.transform, new Vector3(12.2f, 0f, -7.1f), 3.2f, -70f);
            PlaceModel("mine", "Ancient Vault Cave", scenery.transform, new Vector3(-11.7f, 0f, 8.1f), 3.3f, 100f);
            PlaceModel("watchtower", "Northwest Watchtower", scenery.transform, new Vector3(-13f, 0f, 11f), 2.3f, 15f);
            PlaceModel("watchtower", "Southeast Watchtower", scenery.transform, new Vector3(13f, 0f, -10.8f), 2.3f, 195f);
            PlaceModel("wall_gate", "Northern Gate", scenery.transform, new Vector3(0f, 0f, 12.1f), 4.2f, 0f);
            PlaceModel("bridge", "Old Road Bridge", scenery.transform, new Vector3(0f, 0f, -3.7f), 3.4f, 90f);

            PlaceModel("detail_treeA", "Tree A1", scenery.transform, new Vector3(-8f, 0f, 2.8f), 2.1f, 25f);
            PlaceModel("detail_treeB", "Tree B1", scenery.transform, new Vector3(-7.5f, 0f, -6.8f), 2.3f, -20f);
            PlaceModel("detail_treeC", "Tree C1", scenery.transform, new Vector3(7.6f, 0f, 5.2f), 2f, 30f);
            PlaceModel("detail_treeA", "Tree A2", scenery.transform, new Vector3(8f, 0f, -2.5f), 2.2f, 145f);
            PlaceModel("detail_treeB", "Tree B2", scenery.transform, new Vector3(-3.7f, 0f, 7.7f), 2f, 90f);
            PlaceModel("detail_treeC", "Tree C2", scenery.transform, new Vector3(4f, 0f, -7.7f), 2.15f, -45f);
            PlaceModel("detail_rocks", "Rock Cluster", scenery.transform, new Vector3(4.1f, 0f, -5.5f), 1.8f, 10f);
            PlaceModel("detail_rocks_small", "Small Rock Cluster", scenery.transform, new Vector3(-3.2f, 0f, 1.7f), 1.25f, 60f);
            PlaceModel("detail_treeA", "Tree A3", scenery.transform, new Vector3(-13f, 0f, -10f), 2.2f, 65f);
            PlaceModel("detail_treeB", "Tree B3", scenery.transform, new Vector3(12.8f, 0f, 10.6f), 2.2f, -35f);
            PlaceModel("detail_treeC", "Tree C3", scenery.transform, new Vector3(11.8f, 0f, -2.8f), 2.1f, 120f);
            PlaceModel("detail_rocks", "North Ridge Rocks", scenery.transform, new Vector3(-6f, 0f, 10.7f), 1.7f, 40f);
            PlaceModel("detail_rocks_small", "South Road Rocks", scenery.transform, new Vector3(8.8f, 0f, -11f), 1.3f, 10f);
        }

        private static GameObject PlaceModel(string assetName, string objectName, Transform parent, Vector3 position, float targetFootprint, float yRotation)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>($"{KayKitRoot}/{assetName}.fbx");
            if (model == null)
            {
                Debug.LogWarning($"Missing KayKit model: {assetName}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(model, parent) as GameObject;
            instance.name = objectName;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            instance.transform.localScale = Vector3.one;

            Bounds bounds = CalculateRendererBounds(instance);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.001f)
            {
                instance.transform.localScale *= targetFootprint / footprint;
            }

            bounds = CalculateRendererBounds(instance);
            instance.transform.position += Vector3.up * (position.y - bounds.min.y);
            return instance;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Material Material(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                name = name,
                color = color
            };
            return material;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void CreateInvisibleBoundary(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject boundary = CreateCube(name, parent, position, scale, material);
            boundary.GetComponent<Renderer>().enabled = false;
        }

        private static GameObject CreateSpriteActor(string name, Sprite[] idleFrames, Sprite[] moveFrames, Vector3 position, float visualScale)
        {
            GameObject actor = new GameObject(name);
            actor.transform.position = position;

            GameObject visual = new GameObject("SpriteVisual");
            visual.transform.SetParent(actor.transform);
            visual.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            visual.transform.localScale = new Vector3(visualScale, visualScale, visualScale);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
            renderer.color = Color.white;
            renderer.sortingOrder = 10;
            visual.AddComponent<BillboardSprite>();
            SpriteFrameAnimator animator = visual.AddComponent<SpriteFrameAnimator>();
            animator.idleFrames = idleFrames;
            animator.moveFrames = moveFrames;
            animator.framesPerSecond = 10f;

            return actor;
        }

        private static void CreateEncounter(string name, Sprite[] idleFrames, Sprite[] moveFrames, Vector3 position, EncounterType type, CombatantStats stats, int cultivation, int copper, float visualScale = 1.15f, CaveContentType caveContent = CaveContentType.Random)
        {
            GameObject token = CreateSpriteActor(name, idleFrames, moveFrames, position, visualScale);
            SphereCollider collider = token.AddComponent<SphereCollider>();
            collider.radius = 0.55f;
            collider.center = new Vector3(0f, 0.55f, 0f);
            collider.isTrigger = true;
            EncounterTrigger trigger = token.AddComponent<EncounterTrigger>();
            trigger.encounterType = type;
            trigger.enemyStats = stats;
            trigger.cultivationReward = cultivation;
            trigger.copperReward = copper;
            trigger.caveContent = caveContent;
        }

        private static CombatantStats Stats(string displayName, float hp, float attack, float defense, float attackSpeed)
        {
            return new CombatantStats
            {
                displayName = displayName,
                maxHealth = hp,
                currentHealth = hp,
                attack = attack,
                defense = defense,
                attackSpeed = attackSpeed,
                critChance = 0.03f,
                critMultiplier = 1.5f,
                lifeSteal = 0f,
                dodgeChance = 0f,
                moveSpeed = 0f
            };
        }
    }
}
#endif
