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
        private const string GroundTexturePath = "Assets/Art/Generated/Environment/tex_mainmap_grass_albedo.png";
        private const string GroundMaterialPath = "Assets/Art/Generated/Environment/mat_mainmap_grass.mat";
        private const string RoadTexturePath = "Assets/Art/Generated/Environment/tex_mainmap_dirt_albedo.png";
        private const string RoadMaterialPath = "Assets/Art/Generated/Environment/mat_mainmap_dirt.mat";
        private const string WorldSurfaceShaderName = "Wuxia Roguelite/Stylized World Surface";
        private const string TinyRoot = "Assets/Art/ThirdParty/TinySwords";
        private const string CrimsonRoot = "Assets/Art/ThirdParty/CrimsonWarrior/Player";
        private const string EnemyVarietyRoot = "Assets/Art/ThirdParty/CraftPixEnemyVariety/Enemies";
        private const string KayKitRoot = "Assets/Art/ThirdParty/KayKitMedieval/Models";
        private const string PlayerIdlePath = CrimsonRoot + "/CrimsonWarrior_Idle_Right.png";
        private const string PlayerRunPath = CrimsonRoot + "/CrimsonWarrior_Run_Right.png";
        private const string PlayerAttackPath = CrimsonRoot + "/CrimsonWarrior_SwordAttack_Right.png";
        private const float PlayerWorldVisualScale = 2.1f;
        private const string EnemyIdlePath = TinyRoot + "/Units/RedWarrior/Warrior_Idle.png";
        private const string EnemyRunPath = TinyRoot + "/Units/RedWarrior/Warrior_Run.png";
        private const string EnemyAttackPath = TinyRoot + "/Units/RedWarrior/Warrior_Attack1.png";
        private const string EliteIdlePath = TinyRoot + "/Units/BlackWarrior/Warrior_Idle.png";
        private const string EliteRunPath = TinyRoot + "/Units/BlackWarrior/Warrior_Run.png";
        private const string EliteAttackPath = TinyRoot + "/Units/BlackWarrior/Warrior_Attack1.png";
        private const string CaveIdlePath = TinyRoot + "/Units/PurpleWarrior/Warrior_Idle.png";
        private const string CaveRunPath = TinyRoot + "/Units/PurpleWarrior/Warrior_Run.png";
        private const string CaveAttackPath = TinyRoot + "/Units/PurpleWarrior/Warrior_Attack1.png";
        private const string BlueIdlePath = TinyRoot + "/Units/BlueWarrior/Warrior_Idle.png";
        private const string BlueRunPath = TinyRoot + "/Units/BlueWarrior/Warrior_Run.png";
        private const string BlueAttackPath = TinyRoot + "/Units/BlueWarrior/Warrior_Attack1.png";
        private const string RatRunPath = EnemyVarietyRoot + "/Rat_Run.png";
        private const string RatAttackPath = EnemyVarietyRoot + "/Rat_Attack.png";
        private const string RiderRunPath = EnemyVarietyRoot + "/Rider_Run.png";
        private const string RiderAttackPath = EnemyVarietyRoot + "/Rider_Attack.png";
        private const string BallistaFlyPath = EnemyVarietyRoot + "/Ballista_Fly.png";
        private const string BallistaAttackPath = EnemyVarietyRoot + "/Ballista_Attack.png";
        private const string GeneratedEnemyRoot = "Assets/Art/Generated/Characters/Enemies";
        private const string InkWolfIdlePath = GeneratedEnemyRoot + "/InkWolf/spr_enemy_ink_wolf_idle_right_8f_v01.png";
        private const string InkWolfAttackPath = GeneratedEnemyRoot + "/InkWolf/spr_enemy_ink_wolf_attack_right_8f_v01.png";
        private const string StoneApeIdlePath = GeneratedEnemyRoot + "/StoneApe/spr_enemy_stone_ape_idle_right_8f_v01.png";
        private const string StoneApeAttackPath = GeneratedEnemyRoot + "/StoneApe/spr_enemy_stone_ape_attack_right_8f_v01.png";
        private const string BambooPuppetIdlePath = GeneratedEnemyRoot + "/BambooPuppet/spr_enemy_bamboo_puppet_idle_right_8f_v01.png";
        private const string BambooPuppetAttackPath = GeneratedEnemyRoot + "/BambooPuppet/spr_enemy_bamboo_puppet_attack_right_8f_v01.png";
        private const string CombatImpactVfxPath = "Assets/Art/Generated/Effects/spr_vfx_wuxia_impact_6f_v01.png";
        private const string CombatAudioRoot = "Assets/Audio/Generated/Combat";
        private const string CombatSwingSfxPath = CombatAudioRoot + "/sfx_combat_sword_swing_v01.wav";
        private const string CombatImpactSfxPath = CombatAudioRoot + "/sfx_combat_impact_light_v01.wav";
        private const string CombatCriticalSfxPath = CombatAudioRoot + "/sfx_combat_impact_critical_v01.wav";
        private const string CombatDodgeSfxPath = CombatAudioRoot + "/sfx_combat_dodge_v01.wav";
        private const string SkillIconRoot = "Assets/Art/Generated/Icons/Skills";
        private const string EquipmentItemIconRoot = "Assets/Art/Generated/Icons/Equipment";
        private const string JianQiIconPath = SkillIconRoot + "/ico_skill_jianqi_v01_128.png";
        private const string JiJianIconPath = SkillIconRoot + "/ico_skill_jijian_v01_128.png";
        private const string TieBuShanIconPath = SkillIconRoot + "/ico_skill_tiebushan_v01_128.png";
        private const string XiXingIconPath = SkillIconRoot + "/ico_skill_xixing_v01_128.png";
        private const string DuShaZhangIconPath = SkillIconRoot + "/ico_skill_dushazhang_v01_128.png";
        private const string PoJiaZhangIconPath = SkillIconRoot + "/ico_skill_pojiazhang_v01_128.png";
        private const string QingGangSwordIconPath = EquipmentItemIconRoot + "/ico_equipment_qinggang_sword_v01_128.png";
        private const string LightScaleIconPath = EquipmentItemIconRoot + "/ico_equipment_light_scale_v01_128.png";
        private const string PracticeBracerIconPath = EquipmentItemIconRoot + "/ico_equipment_practice_bracer_v01_128.png";
        private const string BlackIronRingIconPath = EquipmentItemIconRoot + "/ico_equipment_black_iron_ring_v01_128.png";
        private const string WandererCloakIconPath = EquipmentItemIconRoot + "/ico_equipment_wanderer_cloak_v01_128.png";
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
            Sprite[] blueIdle = LoadFrames(BlueIdlePath, fallbackSprite);
            Sprite[] blueRun = LoadFrames(BlueRunPath, fallbackSprite);
            Sprite[] blueAttack = LoadFrames(BlueAttackPath, fallbackSprite);
            Sprite[] ratRun = LoadFrames(RatRunPath, fallbackSprite);
            Sprite[] ratAttack = LoadFrames(RatAttackPath, fallbackSprite);
            Sprite[] riderRun = LoadFrames(RiderRunPath, fallbackSprite);
            Sprite[] riderAttack = LoadFrames(RiderAttackPath, fallbackSprite);
            Sprite[] ballistaFly = LoadFrames(BallistaFlyPath, fallbackSprite);
            Sprite[] ballistaAttack = LoadFrames(BallistaAttackPath, fallbackSprite);
            Sprite[] inkWolfIdle = LoadFrames(InkWolfIdlePath, fallbackSprite);
            Sprite[] inkWolfAttack = LoadFrames(InkWolfAttackPath, fallbackSprite);
            Sprite[] stoneApeIdle = LoadFrames(StoneApeIdlePath, fallbackSprite);
            Sprite[] stoneApeAttack = LoadFrames(StoneApeAttackPath, fallbackSprite);
            Sprite[] bambooPuppetIdle = LoadFrames(BambooPuppetIdlePath, fallbackSprite);
            Sprite[] bambooPuppetAttack = LoadFrames(BambooPuppetAttackPath, fallbackSprite);
            Sprite[] combatImpactFrames = LoadFrames(CombatImpactVfxPath, fallbackSprite);
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
            camera.gameObject.AddComponent<AudioListener>();

            Light sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            CreateMapGeometry();
            ApplyUnifiedWorldLighting();

            GameObject root = new GameObject("GameRoot");
            GameFlowController gameFlow = root.AddComponent<GameFlowController>();
            BattleManager battleManager = root.AddComponent<BattleManager>();
            BattleFeedbackAudio battleFeedbackAudio = root.AddComponent<BattleFeedbackAudio>();
            PrototypeHUDController hud = root.AddComponent<PrototypeHUDController>();
            BattleScreenController battleScreen = root.AddComponent<BattleScreenController>();
            CaveRoomController caveRoom = root.AddComponent<CaveRoomController>();

            GameObject player = CreateSpriteActor("Player", playerIdle, playerRun, Vector3.zero, PlayerWorldVisualScale);
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
            battleFeedbackAudio.battleManager = battleManager;
            BindCombatAudio(battleFeedbackAudio);
            hud.gameFlow = gameFlow;
            hud.playerStats = playerStats;
            hud.battleManager = battleManager;
            hud.statusIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(StatusIconPath);
            hud.equipmentIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(EquipmentIconPath);
            hud.healthBarBase = AssetDatabase.LoadAssetAtPath<Texture2D>(HealthBarBasePath);
            hud.healthBarFill = AssetDatabase.LoadAssetAtPath<Texture2D>(HealthBarFillPath);
            BindHudContentIcons(hud);
            battleScreen.gameFlow = gameFlow;
            battleScreen.playerStats = playerStats;
            battleScreen.battleManager = battleManager;
            battleScreen.actorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);
            battleScreen.playerIdleFrames = playerIdle;
            battleScreen.playerAttackFrames = playerAttack;
            battleScreen.enemyIdleFrames = bambooPuppetIdle;
            battleScreen.enemyAttackFrames = bambooPuppetAttack;
            battleScreen.eliteIdleFrames = eliteIdle;
            battleScreen.eliteAttackFrames = eliteAttack;
            battleScreen.caveIdleFrames = stoneApeIdle;
            battleScreen.caveAttackFrames = stoneApeAttack;
            battleScreen.impactEffectFrames = combatImpactFrames;
            battleScreen.enemyVisualProfiles = CreateEnemyVisualProfiles(
                ratRun, ratAttack, riderRun, riderAttack, ballistaFly, ballistaAttack,
                inkWolfIdle, inkWolfAttack, stoneApeIdle, stoneApeAttack,
                bambooPuppetIdle, bambooPuppetAttack);
            caveRoom.gameFlow = gameFlow;
            caveRoom.playerStats = playerStats;
            caveRoom.battleManager = battleManager;
            caveRoom.playerIdleFrames = playerIdle;
            caveRoom.playerRunFrames = playerRun;
            caveRoom.enemyIdleFrames = stoneApeIdle;
            caveRoom.merchantTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(StatusIconPath);
            caveRoom.treasureTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GoldPath);

            CreateEncounter("山贼喽啰", bambooPuppetIdle, bambooPuppetIdle, new Vector3(-5f, 0f, 3f),
                EncounterType.NormalEnemy, Stats("山贼喽啰", 35, 5, 1, 0.9f, "bamboo_puppet"), 10, 2, 1.15f);
            CreateEncounter("灰岩巨鼠", ratRun, ratRun, new Vector3(-2.2f, 0f, -4.6f), EncounterType.NormalEnemy, Stats("灰岩巨鼠", 28, 4, 0, 1.35f, "rat"), 9, 1);
            CreateEncounter("流寇", bambooPuppetIdle, bambooPuppetIdle, new Vector3(2.4f, 0f, 4f),
                EncounterType.NormalEnemy, Stats("流寇", 45, 6, 1, 1f, "bamboo_puppet"), 12, 3, 1.15f);
            CreateEncounter("黑风刀客", stoneApeIdle, stoneApeIdle, new Vector3(5.4f, 0f, -2f),
                EncounterType.EliteEnemy, Stats("黑风刀客", 120, 12, 3, 0.85f, "stone_ape"), 30, 10, 1.25f);
            CreateEncounter("机关弩车", ballistaFly, ballistaFly, new Vector3(-9f, 0f, 5.5f), EncounterType.NormalEnemy, Stats("机关弩车", 50, 7, 2, 1f, "ballista"), 14, 4);
            CreateEncounter("赤骑枪客", riderRun, riderRun, new Vector3(10f, 0f, 2.2f), EncounterType.NormalEnemy, Stats("赤骑枪客", 58, 8, 2, 0.95f, "rider"), 16, 5);
            CreateEncounter("南坡恶徒", bambooPuppetIdle, bambooPuppetIdle, new Vector3(6.5f, 0f, -9f),
                EncounterType.NormalEnemy, Stats("南坡恶徒", 42, 7, 1, 1.3f, "bamboo_puppet"), 13, 3, 1.15f);
            CreateEncounter("玄衣刀客", stoneApeIdle, stoneApeIdle, new Vector3(-8f, 0f, 9f),
                EncounterType.EliteEnemy, Stats("玄衣刀客", 135, 13, 4, 0.9f, "stone_ape"), 34, 12, 1.25f);

            CreateCaveEncounter("断崖石窟", new Vector3(-11f, 0f, -6f),
                Stats("守洞武人", 160, 14, 4, 0.85f, "stone_ape"), 35, 12, CaveContentType.Enemy);
            CreateCaveEncounter("隐市岩洞", new Vector3(11f, 0f, -7f),
                Stats("云游商人", 1, 0, 0, 1f), 0, 0, CaveContentType.Merchant);
            CreateCaveEncounter("古藏秘窟", new Vector3(-10.5f, 0f, 8f),
                Stats("秘藏古匣", 1, 0, 0, 1f), 18, 10, CaveContentType.Treasure);

            CreateEncounter("东市宝箱", new[] { goldSprite }, null, new Vector3(10.5f, 0f, 7.5f), EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 15, 8, 0.9f);
            CreateEncounter("西路宝箱", new[] { goldSprite }, null, new Vector3(-12f, 0f, 1.5f), EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 12, 6, 0.9f);
            CreateEncounter("南桥药草", new[] { herbSprite }, null, new Vector3(0f, 0f, -10f), EncounterType.Herb, Stats("药草", 1, 0, 0, 1f), 0, 0, 0.85f);
            CreateEncounter("北门药草", new[] { herbSprite }, null, new Vector3(1.5f, 0f, 10f), EncounterType.Herb, Stats("药草", 1, 0, 0, 1f), 0, 0, 0.85f);

            ApplyMainMapExpansion(
                enemyIdle, enemyRun, eliteIdle, eliteRun, blueIdle, blueRun, caveIdle, caveRun,
                ratRun, riderRun, ballistaFly,
                inkWolfIdle, stoneApeIdle, bambooPuppetIdle, goldSprite, herbSprite);
            ValidateEquipmentModel();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Main prototype scene generated: {ScenePath}");
        }

        [MenuItem("37 MiniGame/Refresh Player Art")]
        public static void RefreshPlayerArt()
        {
            ConfigurePlayerArtAssets();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            Sprite[] playerIdle = LoadFrames(PlayerIdlePath, fallbackSprite);
            Sprite[] playerRun = LoadFrames(PlayerRunPath, fallbackSprite);
            Sprite[] playerAttack = LoadFrames(PlayerAttackPath, fallbackSprite);

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Cannot refresh player art: Player was not found in the active scene.");
                return;
            }

            SpriteFrameAnimator animator = player.GetComponentInChildren<SpriteFrameAnimator>();
            SpriteRenderer renderer = player.GetComponentInChildren<SpriteRenderer>();
            if (animator == null || renderer == null)
            {
                Debug.LogError("Cannot refresh player art: SpriteFrameAnimator or SpriteRenderer is missing.");
                return;
            }

            animator.idleFrames = playerIdle;
            animator.moveFrames = playerRun;
            animator.transform.localScale = Vector3.one * PlayerWorldVisualScale;
            renderer.sprite = playerIdle[0];
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator.transform);

            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen != null)
            {
                battleScreen.playerIdleFrames = playerIdle;
                battleScreen.playerAttackFrames = playerAttack;
                battleScreen.playerSpriteScale = ActorVisualScale.Medium;
                battleScreen.bossSpriteScale = ActorVisualScale.Large;
                EditorUtility.SetDirty(battleScreen);
            }

            CaveRoomController caveRoom = UnityEngine.Object.FindAnyObjectByType<CaveRoomController>();
            if (caveRoom != null)
            {
                caveRoom.playerIdleFrames = playerIdle;
                caveRoom.playerRunFrames = playerRun;
                caveRoom.playerSpriteScale = ActorVisualScale.Medium;
                EditorUtility.SetDirty(caveRoom);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Crimson Warrior player art refreshed in the active scene.");
        }

        [MenuItem("37 MiniGame/Refresh Battle Feedback Assets")]
        public static void RefreshBattleFeedbackAssets()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before refreshing battle feedback assets.");
                return;
            }

            EnsureFolders();
            ConfigureBattleFeedbackAssets();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            BattleFeedbackAudio feedbackAudio = UnityEngine.Object.FindAnyObjectByType<BattleFeedbackAudio>();
            if (battleScreen == null || feedbackAudio == null)
            {
                Debug.LogError("Cannot refresh battle feedback: required components were not found.");
                return;
            }

            battleScreen.impactEffectFrames = LoadFrames(CombatImpactVfxPath, fallbackSprite);
            BindCombatAudio(feedbackAudio);
            EditorUtility.SetDirty(battleScreen);
            EditorUtility.SetDirty(feedbackAudio);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Battle feedback VFX and WAV assets refreshed in the active scene.");
        }

        [MenuItem("37 MiniGame/Refresh HUD Content Icons")]
        public static void RefreshHudContentIcons()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before refreshing HUD content icons.");
                return;
            }

            EnsureFolders();
            ConfigureHudContentIcons();
            PrototypeHUDController hud = UnityEngine.Object.FindAnyObjectByType<PrototypeHUDController>();
            if (hud == null)
            {
                Debug.LogError("Cannot refresh HUD icons: PrototypeHUDController was not found.");
                return;
            }

            BindHudContentIcons(hud);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Six martial-art icons and five equipment icons refreshed in the active scene.");
        }

        [MenuItem("37 MiniGame/Refresh Main Map Ground")]
        public static void RefreshMainMapGround()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before refreshing the main map ground.");
                return;
            }

            EnsureFolders();
            GameObject groundObject = GameObject.Find("Walkable Ground");
            Renderer groundRenderer = groundObject != null ? groundObject.GetComponent<Renderer>() : null;
            if (groundRenderer == null)
            {
                Debug.LogError("Cannot refresh the main map ground: Walkable Ground or its Renderer was not found.");
                return;
            }

            groundRenderer.sharedMaterial = GetOrCreateMainMapGroundMaterial();
            EditorUtility.SetDirty(groundRenderer);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Formal tiled grass material applied to Walkable Ground.");
        }

        [MenuItem("37 MiniGame/Apply Unified Map Art Style")]
        public static void ApplyUnifiedMapArtStyle()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before applying the unified map art style.");
                return;
            }

            EnsureFolders();
            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            GameObject groundObject = GameObject.Find("Walkable Ground");
            Renderer groundRenderer = groundObject != null ? groundObject.GetComponent<Renderer>() : null;
            if (mapRoot == null || groundRenderer == null)
            {
                Debug.LogError("Cannot apply the unified map art style: map root or Walkable Ground was not found.");
                return;
            }

            groundRenderer.sharedMaterial = GetOrCreateMainMapGroundMaterial();
            EditorUtility.SetDirty(groundRenderer);

            HashSet<string> roadNames = new HashSet<string>
            {
                "Main Dirt Road",
                "Cross Dirt Road",
                "North Ridge Road",
                "South Cave Road",
                "East Village Road",
                "East Village Loop",
                "North Ridge Trail",
                "West Forest Road",
                "West Forest Loop",
                "South Mine Trail"
            };

            Material roadMaterial = GetOrCreateMainMapRoadMaterial();
            int roadCount = 0;
            foreach (Renderer renderer in mapRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!roadNames.Contains(renderer.gameObject.name))
                {
                    continue;
                }

                renderer.sharedMaterial = roadMaterial;
                EditorUtility.SetDirty(renderer);
                roadCount++;
            }

            ApplyUnifiedWorldLighting();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"Unified 2.5D wuxia picture-book style applied to grass, {roadCount} road meshes, and world lighting.");
        }

        [MenuItem("37 MiniGame/Expand Main Map")]
        public static void ExpandMainMap()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before expanding the main map.");
                return;
            }

            if (GameObject.Find("3D Prototype Map") == null)
            {
                Debug.LogError("Cannot expand the main map: 3D Prototype Map was not found in the active scene.");
                return;
            }

            PrepareArtAssets();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            Sprite[] enemyIdle = LoadFrames(EnemyIdlePath, fallbackSprite);
            Sprite[] enemyRun = LoadFrames(EnemyRunPath, fallbackSprite);
            Sprite[] eliteIdle = LoadFrames(EliteIdlePath, fallbackSprite);
            Sprite[] eliteRun = LoadFrames(EliteRunPath, fallbackSprite);
            Sprite[] blueIdle = LoadFrames(BlueIdlePath, fallbackSprite);
            Sprite[] blueRun = LoadFrames(BlueRunPath, fallbackSprite);
            Sprite[] blueAttack = LoadFrames(BlueAttackPath, fallbackSprite);
            Sprite[] caveIdle = LoadFrames(CaveIdlePath, fallbackSprite);
            Sprite[] caveRun = LoadFrames(CaveRunPath, fallbackSprite);
            Sprite[] caveAttack = LoadFrames(CaveAttackPath, fallbackSprite);
            Sprite[] ratRun = LoadFrames(RatRunPath, fallbackSprite);
            Sprite[] ratAttack = LoadFrames(RatAttackPath, fallbackSprite);
            Sprite[] riderRun = LoadFrames(RiderRunPath, fallbackSprite);
            Sprite[] riderAttack = LoadFrames(RiderAttackPath, fallbackSprite);
            Sprite[] ballistaFly = LoadFrames(BallistaFlyPath, fallbackSprite);
            Sprite[] ballistaAttack = LoadFrames(BallistaAttackPath, fallbackSprite);
            Sprite[] inkWolfIdle = LoadFrames(InkWolfIdlePath, fallbackSprite);
            Sprite[] inkWolfAttack = LoadFrames(InkWolfAttackPath, fallbackSprite);
            Sprite[] stoneApeIdle = LoadFrames(StoneApeIdlePath, fallbackSprite);
            Sprite[] stoneApeAttack = LoadFrames(StoneApeAttackPath, fallbackSprite);
            Sprite[] bambooPuppetIdle = LoadFrames(BambooPuppetIdlePath, fallbackSprite);
            Sprite[] bambooPuppetAttack = LoadFrames(BambooPuppetAttackPath, fallbackSprite);
            Sprite goldSprite = LoadSingleSprite(GoldPath, fallbackSprite);
            Sprite herbSprite = LoadSingleSprite(HerbPath, fallbackSprite);

            ApplyMainMapExpansion(
                enemyIdle, enemyRun, eliteIdle, eliteRun, blueIdle, blueRun, caveIdle, caveRun,
                ratRun, riderRun, ballistaFly,
                inkWolfIdle, stoneApeIdle, bambooPuppetIdle, goldSprite, herbSprite);

            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen != null)
            {
                battleScreen.enemyVisualProfiles = CreateEnemyVisualProfiles(
                    ratRun, ratAttack, riderRun, riderAttack, ballistaFly, ballistaAttack,
                    inkWolfIdle, inkWolfAttack, stoneApeIdle, stoneApeAttack,
                    bambooPuppetIdle, bambooPuppetAttack);
                EditorUtility.SetDirty(battleScreen);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Main map refined to 44 x 38 with denser regional landmarks and six additional enemy encounters.");
        }

        [MenuItem("37 MiniGame/Refresh Enemy Variety")]
        public static void RefreshEnemyVariety()
        {
            ConfigureEnemyVarietyAssets();
            ConfigureGeneratedMonsterAssets();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            Sprite[] ratRun = LoadFrames(RatRunPath, fallbackSprite);
            Sprite[] ratAttack = LoadFrames(RatAttackPath, fallbackSprite);
            Sprite[] riderRun = LoadFrames(RiderRunPath, fallbackSprite);
            Sprite[] riderAttack = LoadFrames(RiderAttackPath, fallbackSprite);
            Sprite[] ballistaFly = LoadFrames(BallistaFlyPath, fallbackSprite);
            Sprite[] ballistaAttack = LoadFrames(BallistaAttackPath, fallbackSprite);
            Sprite[] blueIdle = LoadFrames(BlueIdlePath, fallbackSprite);
            Sprite[] blueAttack = LoadFrames(BlueAttackPath, fallbackSprite);
            Sprite[] caveIdle = LoadFrames(CaveIdlePath, fallbackSprite);
            Sprite[] caveAttack = LoadFrames(CaveAttackPath, fallbackSprite);
            Sprite[] inkWolfIdle = LoadFrames(InkWolfIdlePath, fallbackSprite);
            Sprite[] inkWolfAttack = LoadFrames(InkWolfAttackPath, fallbackSprite);
            Sprite[] stoneApeIdle = LoadFrames(StoneApeIdlePath, fallbackSprite);
            Sprite[] stoneApeAttack = LoadFrames(StoneApeAttackPath, fallbackSprite);
            Sprite[] bambooPuppetIdle = LoadFrames(BambooPuppetIdlePath, fallbackSprite);
            Sprite[] bambooPuppetAttack = LoadFrames(BambooPuppetAttackPath, fallbackSprite);

            ApplyEncounterVisual("野狼", "灰岩巨鼠", "rat", ratRun, 1.15f);
            ApplyEncounterVisual("北岭流寇", "机关弩车", "ballista", ballistaFly, 1.15f);
            ApplyEncounterVisual("东道悍匪", "赤骑枪客", "rider", riderRun, 1.15f);
            RenameEncounter("南坡恶狼", "南坡恶徒");

            foreach (string encounterName in new[] { "山贼喽啰", "流寇", "南坡恶徒", "东郊流寇", "紫衣毒客" })
            {
                ApplyEncounterVisual(encounterName, encounterName, "bamboo_puppet", bambooPuppetIdle, 1.15f);
            }

            foreach (string encounterName in new[] { "青衣快剑", "南矿毒刃" })
            {
                ApplyEncounterVisual(encounterName, encounterName, "ink_wolf", inkWolfIdle, 1.35f);
            }

            foreach (string encounterName in new[] { "黑风刀客", "玄衣刀客", "边城黑衣客" })
            {
                ApplyEncounterVisual(encounterName, encounterName, "stone_ape", stoneApeIdle, 1.25f);
            }

            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen != null)
            {
                battleScreen.enemyIdleFrames = bambooPuppetIdle;
                battleScreen.enemyAttackFrames = bambooPuppetAttack;
                battleScreen.caveIdleFrames = stoneApeIdle;
                battleScreen.caveAttackFrames = stoneApeAttack;
                battleScreen.enemyVisualProfiles = CreateEnemyVisualProfiles(
                    ratRun, ratAttack, riderRun, riderAttack, ballistaFly, ballistaAttack,
                    inkWolfIdle, inkWolfAttack, stoneApeIdle, stoneApeAttack,
                    bambooPuppetIdle, bambooPuppetAttack);
                EditorUtility.SetDirty(battleScreen);
            }

            CaveRoomController caveRoom = UnityEngine.Object.FindAnyObjectByType<CaveRoomController>();
            if (caveRoom != null)
            {
                caveRoom.enemyIdleFrames = stoneApeIdle;
                EditorUtility.SetDirty(caveRoom);
            }

            foreach (EncounterTrigger caveEncounter in
                UnityEngine.Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (caveEncounter.encounterType != EncounterType.HiddenCave ||
                    caveEncounter.caveContent != CaveContentType.Enemy)
                {
                    continue;
                }

                caveEncounter.enemyStats.visualId = "stone_ape";
                EditorUtility.SetDirty(caveEncounter);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Enemy variety art refreshed in the active scene.");
        }

        private static void ApplyEncounterVisual(string currentName, string displayName, string visualId,
            Sprite[] frames, float visualScale)
        {
            GameObject encounterObject = GameObject.Find(currentName) ?? GameObject.Find(displayName);
            if (encounterObject == null)
            {
                Debug.LogWarning($"Cannot refresh enemy art: {currentName} was not found.");
                return;
            }

            encounterObject.name = displayName;
            EncounterTrigger trigger = encounterObject.GetComponent<EncounterTrigger>();
            SpriteFrameAnimator animator = encounterObject.GetComponentInChildren<SpriteFrameAnimator>();
            SpriteRenderer renderer = encounterObject.GetComponentInChildren<SpriteRenderer>();
            if (trigger == null || animator == null || renderer == null)
            {
                Debug.LogWarning($"Cannot refresh enemy art: {displayName} is missing required components.");
                return;
            }

            trigger.enemyStats.displayName = displayName;
            trigger.enemyStats.visualId = visualId;
            animator.idleFrames = frames;
            animator.moveFrames = frames;
            bool usesFootPivot = frames[0].pivot.y <= frames[0].rect.height * 0.2f;
            animator.transform.localPosition = new Vector3(
                animator.transform.localPosition.x,
                usesFootPivot ? 0f : 0.8f,
                animator.transform.localPosition.z);
            animator.transform.localScale = Vector3.one * visualScale;
            renderer.sprite = frames[0];
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator.transform);
        }

        private static void RenameEncounter(string currentName, string displayName)
        {
            GameObject encounterObject = GameObject.Find(currentName) ?? GameObject.Find(displayName);
            if (encounterObject == null)
            {
                return;
            }

            encounterObject.name = displayName;
            EncounterTrigger trigger = encounterObject.GetComponent<EncounterTrigger>();
            if (trigger != null)
            {
                trigger.enemyStats.displayName = displayName;
                EditorUtility.SetDirty(trigger);
            }
        }

        private static BattleScreenController.EnemyVisualProfile[] CreateEnemyVisualProfiles(
            Sprite[] ratRun, Sprite[] ratAttack, Sprite[] riderRun, Sprite[] riderAttack,
            Sprite[] ballistaFly, Sprite[] ballistaAttack,
            Sprite[] inkWolfIdle, Sprite[] inkWolfAttack,
            Sprite[] stoneApeIdle, Sprite[] stoneApeAttack,
            Sprite[] bambooPuppetIdle, Sprite[] bambooPuppetAttack)
        {
            return new[]
            {
                CreateEnemyVisualProfile("blue", inkWolfIdle, inkWolfAttack, ActorVisualScale.Medium, true),
                CreateEnemyVisualProfile("purple", bambooPuppetIdle, bambooPuppetAttack, ActorVisualScale.Medium, true),
                CreateEnemyVisualProfile("rat", ratRun, ratAttack, ActorVisualScale.Small),
                CreateEnemyVisualProfile("rider", riderRun, riderAttack, ActorVisualScale.Medium),
                CreateEnemyVisualProfile("ballista", ballistaFly, ballistaAttack, ActorVisualScale.Medium),
                CreateEnemyVisualProfile("ink_wolf", inkWolfIdle, inkWolfAttack, ActorVisualScale.Medium, true),
                CreateEnemyVisualProfile("stone_ape", stoneApeIdle, stoneApeAttack, 1.12f, true),
                CreateEnemyVisualProfile("bamboo_puppet", bambooPuppetIdle, bambooPuppetAttack, ActorVisualScale.Medium, true)
            };
        }

        private static BattleScreenController.EnemyVisualProfile CreateEnemyVisualProfile(
            string id, Sprite[] idleFrames, Sprite[] attackFrames, float scale,
            bool flipHorizontally = false)
        {
            return new BattleScreenController.EnemyVisualProfile
            {
                id = id,
                idleFrames = idleFrames,
                attackFrames = attackFrames,
                scale = scale,
                flipHorizontally = flipHorizontally
            };
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
            CreateFolderIfMissing("Assets/Art/Generated", "Icons");
            CreateFolderIfMissing("Assets/Art/Generated/Icons", "Skills");
            CreateFolderIfMissing("Assets/Art/Generated/Icons", "Equipment");
            CreateFolderIfMissing("Assets/Art/Generated", "Effects");
            CreateFolderIfMissing("Assets/Art/Generated", "Environment");
            CreateFolderIfMissing("Assets/Art/Generated/Environment", "Shaders");
            CreateFolderIfMissing("Assets", "Audio");
            CreateFolderIfMissing("Assets/Audio", "Generated");
            CreateFolderIfMissing("Assets/Audio/Generated", "Combat");
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
            ConfigurePlayerArtAssets();
            ConfigureEnemyVarietyAssets();
            ConfigureGeneratedMonsterAssets();
            ConfigureBattleFeedbackAssets();
            ConfigureHudContentIcons();

            string[] tinySwordsSheets =
            {
                EnemyIdlePath, EnemyRunPath, EnemyAttackPath,
                EliteIdlePath, EliteRunPath, EliteAttackPath,
                CaveIdlePath, CaveRunPath, CaveAttackPath,
                BlueIdlePath, BlueRunPath, BlueAttackPath
            };

            foreach (string path in tinySwordsSheets)
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

        private static void ConfigureBattleFeedbackAssets()
        {
            ConfigureSpriteSheet(CombatImpactVfxPath, 256, 256, 256f);
            foreach (string path in new[]
                     {
                         CombatSwingSfxPath, CombatImpactSfxPath,
                         CombatCriticalSfxPath, CombatDodgeSfxPath
                     })
            {
                if (File.Exists(path))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                }
                else
                {
                    Debug.LogWarning($"Missing combat audio asset: {path}; procedural fallback will be used.");
                }
            }
        }

        private static void BindCombatAudio(BattleFeedbackAudio feedbackAudio)
        {
            feedbackAudio.swingSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(CombatSwingSfxPath);
            feedbackAudio.impactSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(CombatImpactSfxPath);
            feedbackAudio.criticalSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(CombatCriticalSfxPath);
            feedbackAudio.dodgeSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(CombatDodgeSfxPath);
        }

        private static void ConfigureHudContentIcons()
        {
            foreach (string path in new[]
                     {
                         JianQiIconPath, JiJianIconPath, TieBuShanIconPath,
                         XiXingIconPath, DuShaZhangIconPath, PoJiaZhangIconPath,
                         QingGangSwordIconPath, LightScaleIconPath, PracticeBracerIconPath,
                         BlackIronRingIconPath, WandererCloakIconPath
                     })
            {
                ConfigureIconTexture(path);
            }
        }

        private static void BindHudContentIcons(PrototypeHUDController hud)
        {
            hud.martialArtIcons = new[]
            {
                Icon("剑气诀", JianQiIconPath),
                Icon("疾剑式", JiJianIconPath),
                Icon("铁布衫", TieBuShanIconPath),
                Icon("吸星诀", XiXingIconPath),
                Icon("毒砂掌", DuShaZhangIconPath),
                Icon("破甲掌", PoJiaZhangIconPath)
            };
            hud.equipmentItemIcons = new[]
            {
                Icon("qinggang_sword", QingGangSwordIconPath),
                Icon("light_scale", LightScaleIconPath),
                Icon("practice_bracer", PracticeBracerIconPath),
                Icon("black_iron_ring", BlackIronRingIconPath),
                Icon("wanderer_cloak", WandererCloakIconPath)
            };
        }

        private static PrototypeHUDController.IconEntry Icon(string id, string path)
        {
            return new PrototypeHUDController.IconEntry
            {
                id = id,
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path)
            };
        }

        private static void ConfigurePlayerArtAssets()
        {
            ConfigureSpriteSheet(PlayerIdlePath, 80, 80, 32f);
            ConfigureSpriteSheet(PlayerRunPath, 80, 80, 32f);
            ConfigureSpriteSheet(PlayerAttackPath, 80, 80, 32f);
        }

        private static void ConfigureEnemyVarietyAssets()
        {
            string[] sheets =
            {
                RatRunPath, RatAttackPath,
                RiderRunPath, RiderAttackPath,
                BallistaFlyPath, BallistaAttackPath
            };

            foreach (string path in sheets)
            {
                ConfigureSpriteSheet(path, 96, 96, 32f);
            }
        }

        private static void ConfigureGeneratedMonsterAssets()
        {
            string[] sheets =
            {
                InkWolfIdlePath, InkWolfAttackPath,
                StoneApeIdlePath, StoneApeAttackPath,
                BambooPuppetIdlePath, BambooPuppetAttackPath
            };

            foreach (string path in sheets)
            {
                ConfigureSpriteSheet(path, 256, 256, 160f, new Vector2(0.5f, 0.125f));
            }
        }

        private static void ConfigureSpriteSheet(string path, int frameWidth, int frameHeight,
            float pixelsPerUnit, Vector2? customPivot = null)
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

            // A newly added PNG starts as a single/default texture. Persist the
            // Multiple-Sprite importer state before asking the data provider for
            // SpriteRects, otherwise Unity 6 discards the first rect write.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            importer = AssetImporter.GetAtPath(path) as TextureImporter;

            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> existingIds = dataProvider.GetSpriteRects()
                .GroupBy(spriteRect => spriteRect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID);

            List<SpriteRect> spriteRects = new List<SpriteRect>(columns * rows);
            List<SpriteNameFileIdPair> nameIdPairs = new List<SpriteNameFileIdPair>(columns * rows);
            string baseName = Path.GetFileNameWithoutExtension(path);
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    string spriteName = $"{baseName}_{index:D2}";
                    GUID spriteId = existingIds.TryGetValue(spriteName, out GUID existingId)
                        ? existingId
                        : GUID.Generate();
                    spriteRects.Add(new SpriteRect
                    {
                        name = spriteName,
                        spriteID = spriteId,
                        rect = new Rect(column * frameWidth, row * frameHeight, frameWidth, frameHeight),
                        alignment = customPivot.HasValue ? SpriteAlignment.Custom : SpriteAlignment.Center,
                        pivot = customPivot ?? new Vector2(0.5f, 0.5f)
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
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

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

        private static void ConfigureIconTexture(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Missing generated icon: {path}");
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
            importer.spritePixelsPerUnit = 128f;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 128;
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
            Material ground = GetOrCreateMainMapGroundMaterial();
            Material path = GetOrCreateMainMapRoadMaterial();
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

        private static Material GetOrCreateMainMapGroundMaterial()
        {
            return GetOrCreateWorldSurfaceMaterial(
                GroundTexturePath,
                GroundMaterialPath,
                "MainMap_Grass",
                new Color(0.72f, 0.76f, 0.66f, 1f),
                0.18f,
                "Prototype_Ground",
                new Color(0.18f, 0.36f, 0.22f));
        }

        private static Material GetOrCreateMainMapRoadMaterial()
        {
            return GetOrCreateWorldSurfaceMaterial(
                RoadTexturePath,
                RoadMaterialPath,
                "MainMap_DirtRoad",
                new Color(0.72f, 0.68f, 0.61f, 1f),
                0.18f,
                "Prototype_Path",
                new Color(0.38f, 0.32f, 0.24f));
        }

        private static Material GetOrCreateWorldSurfaceMaterial(
            string texturePath,
            string materialPath,
            string materialName,
            Color tint,
            float worldTiling,
            string fallbackName,
            Color fallbackColor)
        {
            if (!File.Exists(texturePath))
            {
                Debug.LogWarning($"Missing formal world texture: {texturePath}");
                return Material(fallbackName, fallbackColor);
            }

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.ToNearest;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.mipmapEnabled = true;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Shader shader = Shader.Find(WorldSurfaceShaderName) ?? Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError($"Cannot find the unified world shader or Standard fallback for {materialName}.");
                return Material(fallbackName, fallbackColor);
            }

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", tint);
            if (material.HasProperty("_WorldTiling"))
            {
                material.SetFloat("_WorldTiling", worldTiling);
            }
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }
            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 0f);
            }
            if (material.HasProperty("_GlossyReflections"))
            {
                material.SetFloat("_GlossyReflections", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyUnifiedWorldLighting()
        {
            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.17f, 0.19f, 0.18f, 1f);
                EditorUtility.SetDirty(camera);
            }

            Light sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.type == LightType.Directional);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.91f, 0.76f, 1f);
                sun.intensity = 1.05f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.65f;
                EditorUtility.SetDirty(sun);
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.34f, 0.32f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.22f, 0.20f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.09f, 0.07f, 1f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.35f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.28f, 0.30f, 0.28f, 1f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 75f;
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
            PlaceModel("mine", "Cliff Cave Entrance", scenery.transform, new Vector3(-12.2f, 0f, -6.1f), 4.5f, 82f);
            PlaceModel("mine", "Hidden Market Cave", scenery.transform, new Vector3(12.2f, 0f, -7.1f), 4.3f, -70f);
            PlaceModel("mine", "Ancient Vault Cave", scenery.transform, new Vector3(-11.7f, 0f, 8.1f), 4.4f, 100f);
            PlaceModel("watchtower", "Northwest Watchtower", scenery.transform, new Vector3(-13f, 0f, 11f), 2.3f, 15f);
            PlaceModel("watchtower", "Southeast Watchtower", scenery.transform, new Vector3(13f, 0f, -10.8f), 2.3f, 195f);
            PlaceModel("wall_gate", "Northern Gate", scenery.transform, new Vector3(0f, 0f, 12.1f), 4.2f, 0f);
            PlaceModel("bridge", "Old Road Bridge", scenery.transform, new Vector3(0f, 0f, -3.7f), 3.4f, 90f);

            PlaceModel("detail_treeA", "Tree A1", scenery.transform, new Vector3(-8f, 0f, 2.8f), 2.1f, 25f);
            PlaceModel("detail_treeB", "Tree B1", scenery.transform, new Vector3(-7.2f, 0f, -3.4f), 2.3f, -20f);
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

        private static void ApplyMainMapExpansion(
            Sprite[] enemyIdle, Sprite[] enemyRun, Sprite[] eliteIdle, Sprite[] eliteRun,
            Sprite[] blueIdle, Sprite[] blueRun, Sprite[] purpleIdle, Sprite[] purpleRun,
            Sprite[] ratRun, Sprite[] riderRun, Sprite[] ballistaFly,
            Sprite[] inkWolfIdle, Sprite[] stoneApeIdle, Sprite[] bambooPuppetIdle,
            Sprite goldSprite, Sprite herbSprite)
        {
            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            if (mapRoot == null)
            {
                return;
            }

            Transform previousExpansion = mapRoot.transform.Find("Expanded Main Map Content");
            if (previousExpansion != null)
            {
                UnityEngine.Object.DestroyImmediate(previousExpansion.gameObject);
            }

            ResizeMapObject(mapRoot.transform, "Walkable Ground", Vector3.zero, new Vector3(4.4f, 1f, 3.8f));
            ResizeMapObject(mapRoot.transform, "Main Dirt Road", new Vector3(0f, 0.025f, 0f), new Vector3(3.2f, 0.05f, 34f));
            ResizeMapObject(mapRoot.transform, "Cross Dirt Road", new Vector3(0f, 0.03f, 0.8f), new Vector3(40f, 0.05f, 2.5f));
            ResizeMapObject(mapRoot.transform, "North Ridge Road", new Vector3(-8.2f, 0.028f, 7.2f), new Vector3(16f, 0.05f, 2.1f));
            ResizeMapObject(mapRoot.transform, "South Cave Road", new Vector3(8.5f, 0.028f, -7.2f), new Vector3(17f, 0.05f, 2.1f));

            ResizeMapObject(mapRoot.transform, "North Boundary", new Vector3(0f, 0.55f, 19.2f), new Vector3(44f, 1.1f, 0.45f));
            ResizeMapObject(mapRoot.transform, "South Boundary", new Vector3(0f, 0.55f, -19.2f), new Vector3(44f, 1.1f, 0.45f));
            ResizeMapObject(mapRoot.transform, "West Boundary", new Vector3(-22.2f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 38f));
            ResizeMapObject(mapRoot.transform, "East Boundary", new Vector3(22.2f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 38f));

            Material path = GetMapMaterial(mapRoot.transform, "Main Dirt Road", "Prototype_Path", new Color(0.38f, 0.32f, 0.24f));
            GameObject expansion = new GameObject("Expanded Main Map Content");
            expansion.transform.SetParent(mapRoot.transform);

            GameObject roads = new GameObject("Regional Roads");
            roads.transform.SetParent(expansion.transform);
            CreateCube("East Village Road", roads.transform, new Vector3(16f, 0.028f, 7.5f), new Vector3(2.2f, 0.05f, 13.5f), path);
            CreateCube("East Village Loop", roads.transform, new Vector3(16f, 0.029f, 13.5f), new Vector3(11f, 0.05f, 2.1f), path);
            CreateCube("North Ridge Trail", roads.transform, new Vector3(-5f, 0.028f, 15f), new Vector3(27f, 0.05f, 2.05f), path);
            CreateCube("West Forest Road", roads.transform, new Vector3(-16f, 0.028f, -6.5f), new Vector3(2.1f, 0.05f, 15f), path);
            CreateCube("West Forest Loop", roads.transform, new Vector3(-15.5f, 0.029f, -13.5f), new Vector3(12f, 0.05f, 2f), path);
            CreateCube("South Mine Trail", roads.transform, new Vector3(6f, 0.028f, -14.5f), new Vector3(29f, 0.05f, 2.1f), path);

            GameObject scenery = new GameObject("Expanded KayKit Scenery");
            scenery.transform.SetParent(expansion.transform);

            PlaceModel("house", "East Hamlet House A", scenery.transform, new Vector3(13.2f, 0f, 12f), 3.1f, 15f);
            PlaceModel("house", "East Hamlet House B", scenery.transform, new Vector3(19f, 0f, 11.7f), 3f, -20f);
            PlaceModel("market", "East Hamlet Market", scenery.transform, new Vector3(18.2f, 0f, 5.2f), 3f, -60f);
            PlaceModel("well", "East Hamlet Well", scenery.transform, new Vector3(13.5f, 0f, 9f), 1.25f, 0f);
            PlaceModel("wall_gate", "East Hamlet Gate", scenery.transform, new Vector3(21f, 0f, 8.8f), 3.5f, 90f);
            PlaceModel("watchtower", "East Hamlet Watchtower", scenery.transform, new Vector3(20.2f, 0f, 16.4f), 2.2f, 200f);

            PlaceModel("watchtower", "West Forest Watchtower", scenery.transform, new Vector3(-20f, 0f, -2.2f), 2.25f, 20f);
            PlaceModel("wall_gate", "West Forest Gate", scenery.transform, new Vector3(-16f, 0f, -11.5f), 3.7f, 0f);
            PlaceModel("market", "West Road Caravan", scenery.transform, new Vector3(-18.6f, 0f, -15.8f), 2.7f, 35f);
            PlaceModel("bridge", "West Creek Bridge", scenery.transform, new Vector3(-15.5f, 0f, -8.8f), 3.2f, 0f);

            PlaceModel("mine", "South Ridge Mine", scenery.transform, new Vector3(19.8f, 0f, -14.8f), 4.5f, -90f);
            PlaceModel("watchtower", "South Gate Watchtower", scenery.transform, new Vector3(12.8f, 0f, -17.1f), 2.2f, 170f);
            PlaceModel("wall_straight", "South Gate Wall A", scenery.transform, new Vector3(9f, 0f, -17.4f), 3.8f, 0f);
            PlaceModel("wall_straight", "South Gate Wall B", scenery.transform, new Vector3(16.2f, 0f, -17.4f), 3.8f, 0f);

            PlaceModel("detail_treeA", "North Pine A", scenery.transform, new Vector3(-18.5f, 0f, 16.5f), 2.3f, 20f);
            PlaceModel("detail_treeB", "North Pine B", scenery.transform, new Vector3(-14f, 0f, 12.8f), 2.2f, -30f);
            PlaceModel("detail_treeC", "North Pine C", scenery.transform, new Vector3(-9.5f, 0f, 17.2f), 2.1f, 80f);
            PlaceModel("detail_treeA", "West Tree A", scenery.transform, new Vector3(-20f, 0f, -9f), 2.2f, 120f);
            PlaceModel("detail_treeB", "West Tree B", scenery.transform, new Vector3(-18f, 0f, -13f), 2.35f, 15f);
            PlaceModel("detail_treeC", "East Tree A", scenery.transform, new Vector3(20f, 0f, 2f), 2.15f, 45f);
            PlaceModel("detail_treeA", "East Tree B", scenery.transform, new Vector3(12f, 0f, 16.5f), 2.2f, 150f);
            PlaceModel("detail_rocks", "North Ridge Rock Cluster", scenery.transform, new Vector3(-3f, 0f, 17.2f), 1.8f, 25f);
            PlaceModel("detail_rocks", "South Mine Rock Cluster", scenery.transform, new Vector3(17.4f, 0f, -11.8f), 1.9f, 80f);
            PlaceModel("detail_rocks_small", "West Trail Stones", scenery.transform, new Vector3(-12f, 0f, -14.8f), 1.25f, 30f);
            PlaceModel("detail_rocks_small", "East Hamlet Stones", scenery.transform, new Vector3(18f, 0f, 15f), 1.3f, 110f);

            GameObject detailClusters = new GameObject("Regional Detail Clusters");
            detailClusters.transform.SetParent(scenery.transform);

            PlaceModel("wall_straight", "East Hamlet Wall North", detailClusters.transform, new Vector3(21f, 0f, 13.8f), 3.6f, 90f);
            PlaceModel("wall_straight", "East Hamlet Wall South", detailClusters.transform, new Vector3(21f, 0f, 4.2f), 3.6f, 90f);
            PlaceModel("detail_treeA", "East Orchard Tree A", detailClusters.transform, new Vector3(11.2f, 0f, 5.2f), 2.05f, 35f);
            PlaceModel("detail_treeB", "East Orchard Tree B", detailClusters.transform, new Vector3(20.2f, 0f, 2.8f), 2.2f, 120f);
            PlaceModel("detail_treeC", "East Orchard Tree C", detailClusters.transform, new Vector3(11.5f, 0f, 10.2f), 1.9f, -20f);
            PlaceModel("detail_rocks_small", "East Road Marker Stones", detailClusters.transform, new Vector3(12.2f, 0f, 2.8f), 1.15f, 70f);

            PlaceModel("wall_straight", "North Checkpoint Wall West", detailClusters.transform, new Vector3(-19.5f, 0f, 17.8f), 3.5f, 0f);
            PlaceModel("detail_treeA", "North Ridge Pine D", detailClusters.transform, new Vector3(-19.5f, 0f, 11.2f), 2.25f, 45f);
            PlaceModel("detail_treeC", "North Ridge Pine E", detailClusters.transform, new Vector3(7f, 0f, 17.3f), 2.05f, 135f);
            PlaceModel("detail_rocks", "North Pass Boulder A", detailClusters.transform, new Vector3(-11.8f, 0f, 11.5f), 1.65f, 20f);
            PlaceModel("detail_rocks_small", "North Pass Boulder B", detailClusters.transform, new Vector3(2.8f, 0f, 17.5f), 1.2f, 105f);

            PlaceModel("detail_treeA", "West Forest Tree C", detailClusters.transform, new Vector3(-20.3f, 0f, -6.2f), 2.3f, 20f);
            PlaceModel("detail_treeB", "West Forest Tree D", detailClusters.transform, new Vector3(-12.4f, 0f, -5.4f), 2.15f, 80f);
            PlaceModel("detail_treeC", "West Forest Tree E", detailClusters.transform, new Vector3(-20.5f, 0f, -15.8f), 2.2f, 160f);
            PlaceModel("detail_treeA", "West Forest Tree F", detailClusters.transform, new Vector3(-10.5f, 0f, -11.6f), 2.05f, -25f);
            PlaceModel("detail_rocks", "West Ruin Rocks", detailClusters.transform, new Vector3(-12.2f, 0f, -17.1f), 1.7f, 65f);
            PlaceModel("detail_rocks_small", "West Creek Stones", detailClusters.transform, new Vector3(-19.6f, 0f, -9.8f), 1.15f, 10f);

            PlaceModel("wall_straight", "South Mine Stockade", detailClusters.transform, new Vector3(18.8f, 0f, -10.1f), 3.7f, 90f);
            PlaceModel("detail_treeB", "South Slope Tree A", detailClusters.transform, new Vector3(-5.2f, 0f, -17.2f), 2.1f, 40f);
            PlaceModel("detail_treeC", "South Slope Tree B", detailClusters.transform, new Vector3(8.8f, 0f, -10.8f), 2f, 125f);
            PlaceModel("detail_rocks", "South Quarry Rocks A", detailClusters.transform, new Vector3(3.5f, 0f, -17.2f), 1.8f, 20f);
            PlaceModel("detail_rocks", "South Quarry Rocks B", detailClusters.transform, new Vector3(20.3f, 0f, -17.4f), 1.65f, 95f);
            PlaceModel("detail_rocks_small", "South Trail Stones", detailClusters.transform, new Vector3(-7f, 0f, -12f), 1.2f, 145f);

            GameObject eastBandit = CreateEncounter("东郊流寇", bambooPuppetIdle, bambooPuppetIdle, new Vector3(16f, 0f, 8f),
                EncounterType.NormalEnemy, Stats("东郊流寇", 52, 8, 2, 1.05f, "bamboo_puppet"), 16, 5, 1.15f);
            GameObject northBallista = CreateEncounter("北岭机关车", ballistaFly, ballistaFly, new Vector3(-1f, 0f, 15f),
                EncounterType.NormalEnemy, Stats("北岭机关车", 62, 9, 3, 0.95f, "ballista"), 18, 6);
            GameObject westWolf = CreateEncounter("墨鬃妖狼", inkWolfIdle, inkWolfIdle, new Vector3(-16f, 0f, -6f),
                EncounterType.NormalEnemy,
                Stats("墨鬃妖狼", 52, 9, 1, 1.4f, "ink_wolf", critChance: 0.08f, dodgeChance: 0.10f),
                16, 5, 1.35f);
            GameObject southRider = CreateEncounter("南关赤骑", riderRun, riderRun, new Vector3(12.5f, 0f, -14.5f),
                EncounterType.NormalEnemy, Stats("南关赤骑", 72, 10, 3, 0.9f, "rider"), 21, 7);
            GameObject northElite = CreateEncounter("边城黑衣客", stoneApeIdle, stoneApeIdle, new Vector3(-15.5f, 0f, 14.8f),
                EncounterType.EliteEnemy, Stats("边城黑衣客", 155, 15, 5, 0.85f, "stone_ape"), 40, 14, 1.25f);
            GameObject eastQuickblade = CreateEncounter("青衣快剑", inkWolfIdle, inkWolfIdle, new Vector3(11.7f, 0f, 7.2f),
                EncounterType.NormalEnemy,
                Stats("青衣快剑", 44, 8, 1, 1.45f, "ink_wolf", critChance: 0.08f, dodgeChance: 0.12f),
                17, 5, 1.35f);
            GameObject westPoisoner = CreateEncounter("紫衣毒客", bambooPuppetIdle, bambooPuppetIdle, new Vector3(-19f, 0f, -11.4f),
                EncounterType.NormalEnemy,
                Stats("紫衣毒客", 66, 9, 2, 1.05f, "bamboo_puppet", dodgeChance: 0.05f, lifeSteal: 0.16f),
                20, 7, 1.15f);
            GameObject northGuard = CreateEncounter("岩甲山魈", stoneApeIdle, stoneApeIdle, new Vector3(5.8f, 0f, 15.8f),
                EncounterType.EliteEnemy,
                Stats("岩甲山魈", 150, 15, 6, 0.72f, "stone_ape", critChance: 0.04f),
                38, 12, 1.25f);
            GameObject southAssassin = CreateEncounter("南矿毒刃", inkWolfIdle, inkWolfIdle, new Vector3(3.2f, 0f, -14.7f),
                EncounterType.NormalEnemy,
                Stats("南矿毒刃", 76, 11, 2, 1.12f, "ink_wolf", critChance: 0.10f, dodgeChance: 0.08f, lifeSteal: 0.08f),
                24, 8, 1.35f);
            GameObject westSiegeBow = CreateEncounter("西关重弩", ballistaFly, ballistaFly, new Vector3(-9.5f, 0f, -13.6f),
                EncounterType.NormalEnemy,
                Stats("西关重弩", 88, 14, 4, 0.62f, "ballista", critChance: 0.07f),
                25, 9, 1.2f);
            GameObject eastScout = CreateEncounter("青竹机关傀", bambooPuppetIdle, bambooPuppetIdle, new Vector3(19f, 0f, 7.3f),
                EncounterType.NormalEnemy,
                Stats("青竹机关傀", 82, 12, 4, 0.92f, "bamboo_puppet", dodgeChance: 0.04f),
                24, 8, 1.15f);
            GameObject southCave = CreateCaveEncounter("岩壁密窟", new Vector3(19.2f, 0f, -14.8f),
                Stats("岩窟守卫", 175, 16, 5, 0.82f, "stone_ape"), 42, 14, CaveContentType.Enemy);
            GameObject northTreasure = CreateEncounter("北岭宝箱", new[] { goldSprite }, null, new Vector3(-7f, 0f, 16.2f),
                EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 18, 10, 0.9f);
            GameObject eastHerb = CreateEncounter("东郊药草", new[] { herbSprite }, null, new Vector3(19.5f, 0f, 1.8f),
                EncounterType.Herb, Stats("药草", 1, 0, 0, 1f), 0, 0, 0.85f);

            GameObject[] regionalEncounters =
            {
                eastBandit, northBallista, westWolf, southRider, northElite,
                eastQuickblade, westPoisoner, northGuard, southAssassin, westSiegeBow, eastScout,
                southCave, northTreasure, eastHerb
            };
            foreach (GameObject regionalEncounter in regionalEncounters)
            {
                regionalEncounter.transform.SetParent(expansion.transform, true);
            }
        }

        private static void ResizeMapObject(Transform root, string objectName, Vector3 position, Vector3 scale)
        {
            Transform target = root.Find(objectName);
            if (target == null)
            {
                Debug.LogWarning($"Cannot resize map object: {objectName} was not found.");
                return;
            }

            target.position = position;
            target.localScale = scale;
        }

        private static Material GetMapMaterial(Transform root, string objectName, string fallbackName, Color fallbackColor)
        {
            Transform target = root.Find(objectName);
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            return renderer != null && renderer.sharedMaterial != null
                ? renderer.sharedMaterial
                : Material(fallbackName, fallbackColor);
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
            Sprite firstFrame = idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
            bool usesFootPivot = firstFrame != null &&
                firstFrame.pivot.y <= firstFrame.rect.height * 0.2f;
            visual.transform.localPosition = new Vector3(0f, usesFootPivot ? 0f : 0.8f, 0f);
            visual.transform.localScale = new Vector3(visualScale, visualScale, visualScale);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = firstFrame;
            renderer.color = Color.white;
            renderer.sortingOrder = 10;
            visual.AddComponent<BillboardSprite>();
            SpriteFrameAnimator animator = visual.AddComponent<SpriteFrameAnimator>();
            animator.idleFrames = idleFrames;
            animator.moveFrames = moveFrames;
            animator.framesPerSecond = 10f;

            return actor;
        }

        private static GameObject CreateEncounter(string name, Sprite[] idleFrames, Sprite[] moveFrames, Vector3 position, EncounterType type, CombatantStats stats, int cultivation, int copper, float visualScale = 1.15f, CaveContentType caveContent = CaveContentType.Random)
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
            if (type == EncounterType.NormalEnemy || type == EncounterType.EliteEnemy)
            {
                token.AddComponent<EnemyLevelLabel>();
            }
            return token;
        }

        private static GameObject CreateCaveEncounter(string name, Vector3 position, CombatantStats stats,
            int cultivation, int copper, CaveContentType caveContent)
        {
            GameObject entrance = new GameObject(name);
            entrance.transform.position = position;

            SphereCollider collider = entrance.AddComponent<SphereCollider>();
            collider.radius = 1.1f;
            collider.center = new Vector3(0f, 0.75f, 0f);
            collider.isTrigger = true;

            EncounterTrigger trigger = entrance.AddComponent<EncounterTrigger>();
            trigger.encounterType = EncounterType.HiddenCave;
            trigger.enemyStats = stats;
            trigger.cultivationReward = cultivation;
            trigger.copperReward = copper;
            trigger.caveContent = caveContent;
            return entrance;
        }

        private static CombatantStats Stats(string displayName, float hp, float attack, float defense,
            float attackSpeed, string visualId = "", float critChance = 0.03f,
            float dodgeChance = 0f, float lifeSteal = 0f)
        {
            return new CombatantStats
            {
                displayName = displayName,
                visualId = visualId,
                maxHealth = hp,
                currentHealth = hp,
                attack = attack,
                defense = defense,
                attackSpeed = attackSpeed,
                critChance = critChance,
                critMultiplier = 1.5f,
                lifeSteal = lifeSteal,
                dodgeChance = dodgeChance,
                moveSpeed = 0f
            };
        }
    }
}
#endif
