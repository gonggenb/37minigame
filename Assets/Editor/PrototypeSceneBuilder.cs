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
using WuxiaRoguelite.Audio;
using WuxiaRoguelite.Battle;
using WuxiaRoguelite.CameraTools;
using WuxiaRoguelite.Cave;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.MartialArts;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainPrototype.unity";
        private const string SpritePath = "Assets/Art/Generated/prototype_square.png";
        private const string GroundTexturePath = "Assets/Art/Generated/Environment/tex_env_mainmap_grass_albedo_1024_v02.png";
        private const string GroundMaterialPath = "Assets/Art/Generated/Environment/mat_mainmap_grass.mat";
        private const string SkyMaterialPath = "Assets/Art/Generated/Environment/mat_mainmap_sky.mat";
        private const string RoadTexturePath = "Assets/Art/Generated/Environment/tex_env_mainmap_dirt_albedo_1024_v02.png";
        private const string RoadMaterialPath = "Assets/Art/Generated/Environment/mat_mainmap_dirt.mat";
        private const string WorldSurfaceShaderName = "Wuxia Roguelite/Stylized World Surface";
        private const string StylizedPropShaderName = "Wuxia Roguelite/Stylized Prop Surface";
        private const string StylizedScenicSpriteShaderName = "Wuxia Roguelite/Stylized Scenic Sprite";
        private const string ActorGroundShadowShaderName = "Wuxia Roguelite/Actor Ground Shadow";
        private const string EnvironmentMaterialRoot = "Assets/Art/Generated/Environment/Materials";
        private const string PropMaterialRoot = EnvironmentMaterialRoot + "/Props";
        private const string ScenicSpriteMaterialPath = EnvironmentMaterialRoot + "/mat_hd2d_scenic_sprite.mat";
        private const string ActorGroundShadowMaterialPath = EnvironmentMaterialRoot + "/mat_actor_ground_shadow.mat";
        private const string Hd2dEnvironmentRoot = "Assets/Art/Generated/Environment/HD2D";
        private const string Hd2dBackdropPath = Hd2dEnvironmentRoot + "/tex_env_hd2d_mountain_backdrop_2048x1152_v01.png";
        private const string Hd2dPanoramaPath = Hd2dEnvironmentRoot + "/tex_env_hd2d_mountain_panorama_2048x1024_v01.png";
        private const string Hd2dPanoramaMaterialPath = Hd2dEnvironmentRoot + "/mat_env_hd2d_panorama_sky_v01.mat";
        private const string Hd2dBambooPath = Hd2dEnvironmentRoot + "/spr_env_hd2d_bamboo_cluster_1024_v01.png";
        private const string Hd2dPineRockPath = Hd2dEnvironmentRoot + "/spr_env_hd2d_pine_rock_1024_v01.png";
        private const string Hd2dWaterTexturePath = Hd2dEnvironmentRoot + "/tex_env_hd2d_water_albedo_1024_v01.png";
        private const string Hd2dWaterMaterialPath = Hd2dEnvironmentRoot + "/mat_env_hd2d_water_v01.mat";
        private const string Hd2dMistPath = Hd2dEnvironmentRoot + "/spr_env_hd2d_mist_band_1024x256_v01.png";
        private const string Hd2dLightBeamPath = Hd2dEnvironmentRoot + "/spr_env_hd2d_light_beam_256x512_v01.png";
        private const string Hd2dWaterShaderName = "Wuxia Roguelite/HD2D Water Surface";
        private const string TinyRoot = "Assets/Art/ThirdParty/TinySwords";
        private const string CrimsonRoot = "Assets/Art/ThirdParty/CrimsonWarrior/Player";
        private const string KayKitRoot = "Assets/Art/ThirdParty/KayKitMedieval/Models";
        private const string QuaterniusVillageRoot = "Assets/Art/ThirdParty/QuaterniusMedievalVillage";
        private const string QuaterniusVillageModelRoot = QuaterniusVillageRoot + "/Models";
        private const string QuaterniusVillageTextureRoot = QuaterniusVillageRoot + "/Textures";
        private const string PlayerIdlePath = CrimsonRoot + "/CrimsonWarrior_Idle_Right.png";
        private const string PlayerRunPath = CrimsonRoot + "/CrimsonWarrior_Run_Right.png";
        private const string PlayerAttackPath = CrimsonRoot + "/CrimsonWarrior_SwordAttack_Right.png";
        private const float PlayerLandscapeVisualScale = 1.82f;
        private const float PlayerPortraitVisualScale = 1.5f;
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
        private const string GeneratedEnemyRoot = "Assets/Art/Generated/Characters/Enemies";
        private const string InkWolfIdlePath = GeneratedEnemyRoot + "/InkWolf/spr_enemy_ink_wolf_idle_right_8f_v01.png";
        private const string InkWolfAttackPath = GeneratedEnemyRoot + "/InkWolf/spr_enemy_ink_wolf_attack_right_8f_v01.png";
        private const string StoneApeIdlePath = GeneratedEnemyRoot + "/StoneApe/spr_enemy_stone_ape_idle_right_8f_v01.png";
        private const string StoneApeAttackPath = GeneratedEnemyRoot + "/StoneApe/spr_enemy_stone_ape_attack_right_8f_v01.png";
        private const string BambooPuppetIdlePath = GeneratedEnemyRoot + "/BambooPuppet/spr_enemy_bamboo_puppet_idle_right_8f_v01.png";
        private const string BambooPuppetAttackPath = GeneratedEnemyRoot + "/BambooPuppet/spr_enemy_bamboo_puppet_attack_right_8f_v01.png";
        private const string ReedMantisIdlePath = GeneratedEnemyRoot + "/ReedMantis/spr_enemy_reed_mantis_idle_right_8f_v01.png";
        private const string ReedMantisAttackPath = GeneratedEnemyRoot + "/ReedMantis/spr_enemy_reed_mantis_attack_right_8f_v01.png";
        private const string BronzeToadIdlePath = GeneratedEnemyRoot + "/BronzeToad/spr_enemy_bronze_toad_idle_right_8f_v01.png";
        private const string BronzeToadAttackPath = GeneratedEnemyRoot + "/BronzeToad/spr_enemy_bronze_toad_attack_right_8f_v01.png";
        private const string CrimsonScorpionIdlePath = GeneratedEnemyRoot + "/CrimsonScorpion/spr_enemy_crimson_scorpion_idle_right_8f_v01.png";
        private const string CrimsonScorpionAttackPath = GeneratedEnemyRoot + "/CrimsonScorpion/spr_enemy_crimson_scorpion_attack_right_8f_v01.png";
        private const bool GeneratedEnemyBattleFlip = true;
        // Legacy gameplay IDs keep their tuning and encounter identity, but their
        // presentation now resolves to the same generated wuxia monster family.
        // This prevents a scene refresh from mixing the bright CraftPix pack back
        // into the darker 256 px / 160 PPU map cast.
        private const string RatRunPath = InkWolfIdlePath;
        private const string RatAttackPath = InkWolfAttackPath;
        private const string RiderRunPath = BambooPuppetIdlePath;
        private const string RiderAttackPath = BambooPuppetAttackPath;
        private const string BallistaFlyPath = StoneApeIdlePath;
        private const string BallistaAttackPath = StoneApeAttackPath;
        private const string GeneratedBossRoot = "Assets/Art/Generated/Characters/Bosses";
        private const string FoxDemonBossIdlePath = GeneratedBossRoot + "/FoxDemon/spr_boss_fox_demon_idle_right_8f_v01.png";
        private const string FoxDemonBossAttackPath = GeneratedBossRoot + "/FoxDemon/spr_boss_fox_demon_attack_right_8f_v01.png";
        private const string XuanjiaMidBossRoot = GeneratedBossRoot + "/XuanjiaGateWarden";
        private const string XuanjiaMidBossIdlePath = XuanjiaMidBossRoot + "/spr_boss_xuanjia_gate_warden_idle_left_1f_v01.png";
        private const string XuanjiaMidBossAttackPath = XuanjiaMidBossRoot + "/spr_boss_xuanjia_gate_warden_attack_left_8f_v01.png";
        private const string XuanjiaMidBossSkillPath = XuanjiaMidBossRoot + "/spr_boss_xuanjia_gate_warden_skill_mountain_breaker_left_8f_v01.png";
        private const string OrcWarlordIdlePath = GeneratedBossRoot + "/OrcWarlord/spr_boss_orc_warlord_idle_right_8f_v01.png";
        private const string OrcWarlordAttackPath = GeneratedBossRoot + "/OrcWarlord/spr_boss_orc_warlord_attack_right_8f_v01.png";
        private const string OrcCaveGuardianIdlePath = GeneratedEnemyRoot + "/OrcCaveGuardian/spr_enemy_orc_cave_guardian_idle_right_8f_v01.png";
        private const string OrcCaveGuardianAttackPath = GeneratedEnemyRoot + "/OrcCaveGuardian/spr_enemy_orc_cave_guardian_attack_right_8f_v01.png";
        private const float BossBattleVisualScale = 1.62f;
        private const string XuanjiaDoubleCleavePath = XuanjiaMidBossRoot + "/spr_boss_xuanjia_gate_warden_double_cleave_left_8f_v01.png";
        private const string XuanjiaIronGuardPath = XuanjiaMidBossRoot + "/spr_boss_xuanjia_gate_warden_iron_guard_left_8f_v01.png";
        private const string DoubleCleaveVfxPath = "Assets/Art/Generated/Effects/XuanjiaGateWarden/spr_vfx_midboss_double_cleave_6f_v01.png";
        private const string IronGuardVfxPath = "Assets/Art/Generated/Effects/XuanjiaGateWarden/spr_vfx_midboss_iron_guard_6f_v01.png";
        private const float MidBossBattleVisualScale = 1.52f;
        private const float CaveBattleVisualScale = 1.48f;
        private const string CombatImpactVfxPath = "Assets/Art/Generated/Effects/spr_vfx_wuxia_impact_6f_v01.png";
        private const string SwordQiVfxPath = "Assets/Art/Generated/Effects/spr_vfx_sword_qi_6f_v01.png";
        private const string PoisonMistVfxPath = "Assets/Art/Generated/Effects/spr_vfx_poison_mist_6f_v01.png";
        private const string MountainBreakerVfxPath =
            "Assets/Art/Generated/Effects/XuanjiaGateWarden/spr_vfx_midboss_mountain_breaker_6f_v01.png";
        private const string SpeedBoostVfxPath =
            "Assets/Art/Generated/Effects/tex_vfx_speed_boost_wisp_v01.png";
        private const string CombatAudioRoot = "Assets/Audio/Generated/Combat";
        private const string CombatSwingSfxPath = CombatAudioRoot + "/sfx_combat_sword_swing_v01.wav";
        private const string CombatImpactSfxPath = CombatAudioRoot + "/sfx_combat_impact_light_v01.wav";
        private const string CombatCriticalSfxPath = CombatAudioRoot + "/sfx_combat_impact_critical_v01.wav";
        private const string CombatDodgeSfxPath = CombatAudioRoot + "/sfx_combat_dodge_v01.wav";
        private const string MusicAudioRoot = "Assets/Audio/Generated/Music";
        private const string MainMapMusicPath = MusicAudioRoot + "/bgm_mainmap_wuxia_urgent_60s_v01.wav";
        private const string NormalBattleStemPath =
            MusicAudioRoot + "/stem_normalbattle_wuxia_percussion_15s_v01.wav";
        private const string CaveMusicPath = MusicAudioRoot + "/bgm_cave_mystery_loop_32s_v01.wav";
        private const string CaveBattleStemPath =
            MusicAudioRoot + "/stem_cave_combat_tension_16s_v01.wav";
        private const string MidBossMusicPath = MusicAudioRoot + "/bgm_boss_xuanjia_ironpass_loop_64s_v01.wav";
        private const string BossIntroPath = MusicAudioRoot + "/stg_boss_fox_demon_moonfire_intro_3s_v04.wav";
        private const string BossMusicPath = MusicAudioRoot + "/bgm_boss_fox_demon_moonfire_loop_48s_v04.wav";
        private const string BossMomentumStemPath =
            MusicAudioRoot + "/stem_boss_fox_demon_moonfire_armor_12s_v04.wav";
        private const string BossEnrageStemPath =
            MusicAudioRoot + "/stem_boss_fox_demon_moonfire_frenzy_12s_v04.wav";
        private const string VictoryStingerPath = MusicAudioRoot + "/stg_result_victory_v01.wav";
        private const string DefeatStingerPath = MusicAudioRoot + "/stg_result_defeat_v01.wav";
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
        private const string MainHudIconRoot = "Assets/Art/Generated/Icons/HUD";
        private const string MainHudUiRoot = "Assets/Art/Generated/UI/MainHUD";
        private const string MainHudPortraitFramePath = MainHudUiRoot + "/frame_hud_player_v01_128.png";
        private const string MainHudPlayerStatusIconPath = MainHudIconRoot + "/ico_hud_player_status_v01_128.png";
        private const string MainHudTimeIconPath = MainHudIconRoot + "/ico_hud_time_v01_128.png";
        private const string MainHudCopperIconPath = MainHudIconRoot + "/ico_hud_copper_v01_128.png";
        private const string MainHudCultivationIconPath = MainHudIconRoot + "/ico_hud_cultivation_v01_128.png";
        private const string GoldPath = TinyRoot + "/World/Gold_Resource.png";
        private const string TreasureChestPath = "Assets/Art/Generated/World/spr_treasure_chest_closed_v01.png";
        private const string GeneratedHerbRoot = "Assets/Art/Generated/World/Herbs";
        private const string HealingHerbPath = GeneratedHerbRoot + "/spr_herb_healing_v01.png";
        private const string AttackHerbPath = GeneratedHerbRoot + "/spr_herb_attack_v01.png";
        private const string DefenseHerbPath = GeneratedHerbRoot + "/spr_herb_defense_v01.png";
        private const string MoveSpeedHerbPath = GeneratedHerbRoot + "/spr_herb_move_speed_v01.png";
        private const string MysteryHerbPath = GeneratedHerbRoot + "/spr_herb_mystery_v01.png";
        private const string StatusIconPath = TinyRoot + "/UI/Avatars_01.png";
        private const string EquipmentIconPath = TinyRoot + "/UI/Icon_05.png";
        private const string HealthBarBasePath = TinyRoot + "/UI/BigBar_Base.png";
        private const string HealthBarFillPath = TinyRoot + "/UI/BigBar_Fill.png";
        private const string GeneratedBackgroundRoot = "Assets/Art/Generated/Backgrounds";
        private const string MainMenuBackgroundPath =
            GeneratedBackgroundRoot + "/bg_mainmenu_misty_mountains_v01.png";
        private static readonly string[] NormalBattleBackgroundPaths =
        {
            GeneratedBackgroundRoot + "/bg_battle_central_inn_v01.png",
            GeneratedBackgroundRoot + "/bg_battle_east_bamboo_v01.png",
            GeneratedBackgroundRoot + "/bg_battle_north_pass_v01.png",
            GeneratedBackgroundRoot + "/bg_battle_west_forest_v01.png",
            GeneratedBackgroundRoot + "/bg_battle_south_quarry_v01.png"
        };
        private const string BossBattleBackgroundPath =
            GeneratedBackgroundRoot + "/bg_boss_bloodmoon_temple_v01.png";
        private const string BossIntroUiRoot = "Assets/Art/Generated/UI/BossIntro";
        private const string BossIntroPortraitPath =
            BossIntroUiRoot + "/portrait_boss_fox_demon_circle_v01_256.png";
        private const string BossIntroFramePath =
            BossIntroUiRoot + "/frame_boss_intro_fox_demon_v01_256.png";

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
            Sprite[] swordQiEffectFrames = LoadFrames(SwordQiVfxPath, fallbackSprite);
            Sprite[] poisonEffectFrames = LoadFrames(PoisonMistVfxPath, fallbackSprite);
            Sprite[] mountainBreakerEffectFrames = LoadFrames(MountainBreakerVfxPath, fallbackSprite);
            Sprite treasureChestSprite = LoadSingleSprite(TreasureChestPath, fallbackSprite);
            Sprite[] healingHerbFrames = LoadHerbFrames(HealingHerbPath, fallbackSprite);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            Vector3 cameraOffset = new Vector3(10f, 10.8f, -20f);
            Vector3 cameraLookTarget = Vector3.up * 0.72f;
            camera.transform.position = cameraOffset;
            camera.transform.rotation = Quaternion.LookRotation(cameraLookTarget - cameraOffset, Vector3.up);
            camera.orthographic = false;
            camera.fieldOfView = 34f;
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
            MobileInputController mobileInput = root.AddComponent<MobileInputController>();

            GameObject musicObject = new GameObject("MainMapMusic");
            AudioSource musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MainMapMusicPath);
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 192;
            musicSource.volume = 0.35f;
            AudioSource overlaySource = musicObject.AddComponent<AudioSource>();
            overlaySource.playOnAwake = false;
            overlaySource.loop = true;
            overlaySource.spatialBlend = 0f;
            overlaySource.priority = 184;
            overlaySource.volume = 0f;
            AudioSource specialMusicSource = musicObject.AddComponent<AudioSource>();
            specialMusicSource.playOnAwake = false;
            specialMusicSource.loop = false;
            specialMusicSource.spatialBlend = 0f;
            specialMusicSource.priority = 188;
            specialMusicSource.volume = 0.38f;
            AudioSource stingerSource = musicObject.AddComponent<AudioSource>();
            stingerSource.playOnAwake = false;
            stingerSource.loop = false;
            stingerSource.spatialBlend = 0f;
            stingerSource.priority = 160;
            stingerSource.volume = 0.55f;
            MainMapMusicController musicController = musicObject.AddComponent<MainMapMusicController>();
            musicController.gameFlow = gameFlow;
            musicController.battleManager = battleManager;
            musicController.musicSource = musicSource;
            musicController.overlaySource = overlaySource;
            musicController.specialMusicSource = specialMusicSource;
            musicController.stingerSource = stingerSource;
            musicController.normalBattleStem = AssetDatabase.LoadAssetAtPath<AudioClip>(NormalBattleStemPath);
            musicController.caveMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(CaveMusicPath);
            musicController.caveBattleStem = AssetDatabase.LoadAssetAtPath<AudioClip>(CaveBattleStemPath);
            musicController.bossIntro = AssetDatabase.LoadAssetAtPath<AudioClip>(BossIntroPath);
            musicController.midBossMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(MidBossMusicPath);
            musicController.bossMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(BossMusicPath);
            musicController.bossMomentumStem =
                AssetDatabase.LoadAssetAtPath<AudioClip>(BossMomentumStemPath);
            musicController.bossEnrageStem = AssetDatabase.LoadAssetAtPath<AudioClip>(BossEnrageStemPath);
            musicController.victoryStinger = AssetDatabase.LoadAssetAtPath<AudioClip>(VictoryStingerPath);
            musicController.defeatStinger = AssetDatabase.LoadAssetAtPath<AudioClip>(DefeatStingerPath);
            musicController.volume = 0.35f;
            musicController.overlayVolume = 0.2f;
            musicController.specialMusicVolume = 0.38f;
            musicController.stingerVolume = 0.55f;

            GameObject player = CreateSpriteActor(
                "Player", playerIdle, playerRun, Vector3.zero, PlayerLandscapeVisualScale);
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
            PlayerSpeedBoostVfx speedBoostVfx = player.AddComponent<PlayerSpeedBoostVfx>();
            speedBoostVfx.playerController = playerController;
            speedBoostVfx.playerStats = playerStats;
            speedBoostVfx.immortalQiTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpeedBoostVfxPath);
            SpriteFrameAnimator playerAnimator = player.GetComponentInChildren<SpriteFrameAnimator>();
            playerAnimator.movementSource = playerController;
            playerController.visualRoot = playerAnimator.transform;
            playerController.landscapeVisualScale = PlayerLandscapeVisualScale;
            playerController.portraitVisualScale = PlayerPortraitVisualScale;
            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            follow.offset = cameraOffset;
            follow.portraitOffset = CameraFollow.DefaultPortraitOffset;
            follow.lookAtHeight = 0.72f;
            follow.landscapeFieldOfView = 36.5f;
            follow.portraitFieldOfView = CameraFollow.DefaultPortraitFieldOfView;

            gameFlow.playerStats = playerStats;
            gameFlow.playerController = playerController;
            gameFlow.battleManager = battleManager;
            gameFlow.caveRoom = caveRoom;
            battleManager.playerStats = playerStats;
            mobileInput.gameFlow = gameFlow;
            mobileInput.battleManager = battleManager;
            battleFeedbackAudio.battleManager = battleManager;
            BindCombatAudio(battleFeedbackAudio);
            hud.gameFlow = gameFlow;
            hud.playerStats = playerStats;
            hud.battleManager = battleManager;
            hud.statusIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(MainHudPlayerStatusIconPath);
            hud.equipmentIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(EquipmentIconPath);
            hud.healthBarBase = AssetDatabase.LoadAssetAtPath<Texture2D>(HealthBarBasePath);
            hud.healthBarFill = AssetDatabase.LoadAssetAtPath<Texture2D>(HealthBarFillPath);
            BindHudContentIcons(hud);
            BindBossIntroAssets(hud);
            battleScreen.gameFlow = gameFlow;
            battleScreen.playerStats = playerStats;
            battleScreen.battleManager = battleManager;
            battleScreen.actorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);
            BindBattleBackgrounds(battleScreen);
            battleScreen.playerIdleFrames = playerIdle;
            battleScreen.playerAttackFrames = playerAttack;
            battleScreen.enemyIdleFrames = bambooPuppetIdle;
            battleScreen.enemyAttackFrames = bambooPuppetAttack;
            battleScreen.eliteIdleFrames = eliteIdle;
            battleScreen.eliteAttackFrames = eliteAttack;
            battleScreen.caveIdleFrames = stoneApeIdle;
            battleScreen.caveAttackFrames = stoneApeAttack;
            battleScreen.impactEffectFrames = combatImpactFrames;
            battleScreen.swordQiEffectFrames = swordQiEffectFrames;
            battleScreen.poisonEffectFrames = poisonEffectFrames;
            battleScreen.mountainBreakerEffectFrames = mountainBreakerEffectFrames;
            battleScreen.doubleCleaveEffectFrames = LoadFrames(DoubleCleaveVfxPath, fallbackSprite);
            battleScreen.ironGuardEffectFrames = LoadFrames(IronGuardVfxPath, fallbackSprite);
            BindFinalBossEffects(battleScreen);
            battleScreen.bossSpriteScale = BossBattleVisualScale;
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
                Stats("守洞武人", 160, 14, 4, 0.85f, "orc_cave_guardian"), 35, 12, CaveContentType.Random);
            CreateCaveEncounter("隐市岩洞", new Vector3(11f, 0f, -7f),
                Stats("守洞武人", 160, 14, 4, 0.85f, "orc_cave_guardian"), 35, 12, CaveContentType.Random);
            CreateCaveEncounter("古藏秘窟", new Vector3(-10.5f, 0f, 8f),
                Stats("守洞武人", 160, 14, 4, 0.85f, "orc_cave_guardian"), 35, 12, CaveContentType.Random);

            CreateEncounter("东市宝箱", new[] { treasureChestSprite }, null, new Vector3(10.5f, 0f, 7.5f), EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 15, 8, 0.9f);
            CreateEncounter("西路宝箱", new[] { treasureChestSprite }, null, new Vector3(-12f, 0f, 1.5f), EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 12, 6, 0.9f);
            CreateEncounter("南桥药草", healingHerbFrames, null, new Vector3(0f, 0f, -10f), EncounterType.Herb, Stats("止血草", 1, 0, 0, 1f), 0, 0, 0.85f);
            GameObject northAttackHerb = CreateEncounter("北门药草", LoadHerbFrames(AttackHerbPath, fallbackSprite), null,
                new Vector3(1.5f, 0f, 10f), EncounterType.Herb, Stats("赤阳草", 1, 0, 0, 1f), 0, 0, 0.85f);
            EncounterTrigger northAttackTrigger = northAttackHerb.GetComponent<EncounterTrigger>();
            northAttackTrigger.herbEffect = HerbEffectType.Attack;
            northAttackTrigger.herbBuffValue = 1.5f;

            ApplyMainMapExpansion(
                enemyIdle, enemyRun, eliteIdle, eliteRun, blueIdle, blueRun, caveIdle, caveRun,
                ratRun, riderRun, ballistaFly,
                inkWolfIdle, stoneApeIdle, bambooPuppetIdle, treasureChestSprite,
                healingHerbFrames, LoadHerbFrames(DefenseHerbPath, fallbackSprite),
                LoadHerbFrames(MoveSpeedHerbPath, fallbackSprite),
                LoadHerbFrames(MysteryHerbPath, fallbackSprite));
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
            animator.transform.localScale = Vector3.one * PlayerLandscapeVisualScale;
            renderer.sprite = playerIdle[0];
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator.transform);

            PrototypeHUDController hud = UnityEngine.Object.FindAnyObjectByType<PrototypeHUDController>();
            if (hud != null)
            {
                hud.playerPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>(MainHudPlayerStatusIconPath);
                EditorUtility.SetDirty(hud);
            }

            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen != null)
            {
                battleScreen.playerIdleFrames = playerIdle;
                battleScreen.playerAttackFrames = playerAttack;
                battleScreen.playerSpriteScale = ActorVisualScale.Medium;
                battleScreen.bossSpriteScale = BossBattleVisualScale;
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
            battleScreen.swordQiEffectFrames = LoadFrames(SwordQiVfxPath, fallbackSprite);
            battleScreen.poisonEffectFrames = LoadFrames(PoisonMistVfxPath, fallbackSprite);
            battleScreen.mountainBreakerEffectFrames = LoadFrames(MountainBreakerVfxPath, fallbackSprite);
            battleScreen.doubleCleaveEffectFrames = LoadFrames(DoubleCleaveVfxPath, fallbackSprite);
            battleScreen.ironGuardEffectFrames = LoadFrames(IronGuardVfxPath, fallbackSprite);
            BindFinalBossEffects(battleScreen);
            BindCombatAudio(feedbackAudio);
            EditorUtility.SetDirty(battleScreen);
            EditorUtility.SetDirty(feedbackAudio);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Battle feedback VFX and WAV assets refreshed in the active scene.");
        }

        [MenuItem("37 MiniGame/Refresh Battle Backgrounds")]
        public static void RefreshBattleBackgrounds()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before refreshing battle backgrounds.");
                return;
            }

            ConfigureBattleBackgroundAssets();
            BattleScreenController battleScreen =
                UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen == null)
            {
                Debug.LogError("Cannot refresh battle backgrounds: BattleScreenController was not found.");
                return;
            }

            BindBattleBackgrounds(battleScreen);
            EditorUtility.SetDirty(battleScreen);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Five random normal battle backgrounds and the dedicated Boss background were refreshed.");
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
            ConfigureBossIntroAssets();
            PrototypeHUDController hud = UnityEngine.Object.FindAnyObjectByType<PrototypeHUDController>();
            if (hud == null)
            {
                Debug.LogError("Cannot refresh HUD icons: PrototypeHUDController was not found.");
                return;
            }

            BindHudContentIcons(hud);
            BindBossIntroAssets(hud);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("HUD content icons and Boss-introduction portrait art refreshed in the active scene.");
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

            Material roadMaterial = GetOrCreateMainMapRoadMaterial();
            string[] roadNames =
            {
                "Main Dirt Road",
                "Cross Dirt Road",
                "North Ridge Road",
                "South Cave Road"
            };

            foreach (string roadName in roadNames)
            {
                GameObject roadObject = GameObject.Find(roadName);
                Renderer roadRenderer = roadObject != null ? roadObject.GetComponent<Renderer>() : null;
                if (roadRenderer == null)
                {
                    Debug.LogWarning($"Cannot refresh main map road material: {roadName} or its Renderer was not found.");
                    continue;
                }

                roadRenderer.sharedMaterial = roadMaterial;
                EditorUtility.SetDirty(roadRenderer);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Formal tiled grass and dirt materials applied to the main map ground.");
        }

        [MenuItem("37 MiniGame/Apply HD-2D Main World Art")]
        public static void ApplyHd2dMainWorldArt()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before applying the HD-2D main-world art.");
                return;
            }

            EnsureFolders();
            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            if (mapRoot == null)
            {
                Debug.LogError("Cannot apply HD-2D main-world art: 3D Prototype Map was not found.");
                return;
            }

            AssetDatabase.Refresh();
            ConfigureHd2dWorldArtAssets();

            Transform previous = mapRoot.transform.Find("HD2D Main World Art");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject hd2dRoot = new GameObject("HD2D Main World Art");
            hd2dRoot.transform.SetParent(mapRoot.transform);

            BuildHd2dBackdrop(hd2dRoot.transform);
            BuildHd2dRegionDistricts(hd2dRoot.transform);
            BuildHd2dStream(hd2dRoot.transform);
            BuildHd2dScenicLayers(hd2dRoot.transform);
            BuildHd2dAtmosphere(hd2dRoot.transform);
            RelocateRiverConflicts();
            ApplyHd2dWorldLighting(hd2dRoot.transform);
            ApplyHd2dCohesionPassInternal(mapRoot.transform);
            ApplySceneDirectorLevelRefactorInternal(mapRoot.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Original wuxia HD-2D main-world art applied: diorama depth, seamless mountain panorama, bridge-aligned roads, stream, scenic cutouts, mist, and warm landmark lights.");
        }

        [MenuItem("37 MiniGame/Apply HD-2D Cohesion Pass")]
        public static void ApplyHd2dCohesionPass()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before applying the HD-2D cohesion pass.");
                return;
            }

            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            if (mapRoot == null)
            {
                Debug.LogError("Cannot apply the HD-2D cohesion pass: 3D Prototype Map was not found.");
                return;
            }

            EnsureFolders();
            AssetDatabase.Refresh();
            ApplyHd2dCohesionPassInternal(mapRoot.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("HD-2D cohesion pass applied: matte prop materials, softened scenic sprites, contact shadows, stable billboards, and responsive landscape/portrait camera composition.");
        }

        [MenuItem("37 MiniGame/Apply Scene Director Level Refactor")]
        public static void ApplySceneDirectorLevelRefactor()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before applying the Scene Director level refactor.");
                return;
            }

            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            if (mapRoot == null)
            {
                Debug.LogError("Cannot apply the Scene Director level refactor: 3D Prototype Map was not found.");
                return;
            }

            EnsureFolders();
            AssetDatabase.Refresh();
            ApplySceneDirectorLevelRefactorInternal(mapRoot.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Scene Director refactor applied: clearer gameplay framing, authored POI clusters, road-edge breakup, environmental storytelling, and layered warm/cool lighting.");
        }

        [MenuItem("37 MiniGame/Apply Advanced 3D Environment Pass")]
        public static void ApplyAdvanced3dEnvironmentPass()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before applying the advanced 3D environment pass.");
                return;
            }

            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            if (mapRoot == null)
            {
                Debug.LogError("Cannot apply the advanced 3D environment pass: 3D Prototype Map was not found.");
                return;
            }

            EnsureFolders();
            ConfigureQuaterniusTextureImports();
            AssetDatabase.Refresh();
            ApplyAdvanced3dEnvironmentPassInternal(mapRoot.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Advanced 3D environment pass applied: modular buildings, fortified silhouettes, authored prop clusters, ground contact detail, and layered landmark lighting.");
        }

        private static void ApplyHd2dCohesionPassInternal(Transform mapRoot)
        {
            ApplyStylizedMapMaterials(mapRoot);
            ConfigureActorGroundShadows(GetOrCreateActorGroundShadowMaterial());
            ConfigureScenicSpriteMaterials(mapRoot, GetOrCreateScenicSpriteMaterial());
            ConfigureResponsiveHd2dCamera();
        }

        [MenuItem("37 MiniGame/Validate Main World River Crossings")]
        public static void ValidateMainWorldRiverCrossings()
        {
            List<string> failures = new List<string>();
            Transform barrierRoot = GameObject.Find("River Collision Banks")?.transform;
            BoxCollider[] barriers = barrierRoot != null
                ? barrierRoot.GetComponentsInChildren<BoxCollider>()
                : Array.Empty<BoxCollider>();
            if (barriers.Length == 0)
            {
                failures.Add("river collision banks are missing");
            }

            for (int i = 0; i < MainMapRiverLayout.BridgeNames.Length; i++)
            {
                GameObject bridge = GameObject.Find(MainMapRiverLayout.BridgeNames[i]);
                if (bridge == null)
                {
                    failures.Add($"missing bridge: {MainMapRiverLayout.BridgeNames[i]}");
                }
                else
                {
                    MainMapBridgeSurface surface = bridge.GetComponent<MainMapBridgeSurface>();
                    int railCount = bridge.GetComponentsInChildren<BoxCollider>()
                        .Count(collider => collider.gameObject.name.Contains("Bridge Rail Collider"));
                    int deckPlankCount = bridge.GetComponentsInChildren<Renderer>()
                        .Count(renderer => renderer.gameObject.name.Contains("Bridge Deck Plank"));
                    if (surface == null)
                    {
                        failures.Add($"bridge walk surface is missing: {MainMapRiverLayout.BridgeNames[i]}");
                    }
                    if (railCount < 6)
                    {
                        failures.Add($"bridge side collision is incomplete: {MainMapRiverLayout.BridgeNames[i]}");
                    }
                    if (deckPlankCount < 13)
                    {
                        failures.Add($"Unity-generated bridge deck is incomplete: {MainMapRiverLayout.BridgeNames[i]}");
                    }
                }

                Vector2 bridgePoint = MainMapRiverLayout.CenterLine[
                    MainMapRiverLayout.BridgePointIndices[i]];
                if (IsInsideAnyBarrier(new Vector3(bridgePoint.x, 0.55f, bridgePoint.y), barriers))
                {
                    failures.Add($"bridge gap is blocked: {MainMapRiverLayout.BridgeNames[i]}");
                }
            }

            Material panoramaSky = RenderSettings.skybox;
            Texture panoramaTexture = panoramaSky != null && panoramaSky.HasProperty("_MainTex")
                ? panoramaSky.GetTexture("_MainTex")
                : null;
            if (panoramaTexture == null ||
                AssetDatabase.GetAssetPath(panoramaTexture) != Hd2dPanoramaPath)
            {
                failures.Add("four-direction panorama sky is missing");
            }

            int[] blockedWaterSamples = { 1, 3, 4, 6, 7, 9 };
            foreach (int sampleIndex in blockedWaterSamples)
            {
                Vector2 waterPoint = MainMapRiverLayout.CenterLine[sampleIndex];
                if (!IsInsideAnyBarrier(new Vector3(waterPoint.x, 0.55f, waterPoint.y), barriers))
                {
                    failures.Add($"river sample is passable without a bridge: {sampleIndex}");
                }
            }

            foreach (EncounterTrigger encounter in UnityEngine.Object.FindObjectsByType<EncounterTrigger>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (MainMapRiverLayout.IsInsideRiver(encounter.transform.position, 0.8f))
                {
                    failures.Add($"encounter overlaps river: {encounter.name}");
                }
            }

            foreach (Renderer road in FindRoadRenderersCrossingRiverAwayFromBridges())
            {
                failures.Add($"road crosses river away from a bridge: {road.gameObject.name}");
            }

            Transform districtRoot = GameObject.Find("Five Main Map Districts")?.transform;
            string[] districtPatches =
            {
                "Central Courier Ground",
                "East Hamlet Ground",
                "West Forest Ground",
                "North Ridge Ground",
                "South Mine Ground"
            };
            if (districtRoot == null)
            {
                failures.Add("five-region visual division is missing");
            }
            else
            {
                foreach (string patchName in districtPatches)
                {
                    if (districtRoot.Find(patchName) == null)
                    {
                        failures.Add($"region ground patch is missing: {patchName}");
                    }
                }

                int districtGateCount = districtRoot.GetComponentsInChildren<MainMapRegionGuide>(true).Length;
                if (districtGateCount < 4)
                {
                    failures.Add("regional stone-gate markers are incomplete");
                }
            }

            if (failures.Count > 0)
            {
                Debug.LogError("Main-world river validation failed: " + string.Join(" · ", failures));
                return;
            }

            Debug.Log("Main-world validation passed: water is blocked, three Unity-generated bridges have arch lift, open rails and side collision, five regions have ground identity and stone-gate markers, roads only cross at bridges, encounters are clear of the river, and the panorama sky is assigned.");
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
                "South Mine Trail",
                "East Frontier Trail",
                "West Frontier Trail",
                "North Frontier Road",
                "South Frontier Road"
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
            Sprite treasureChestSprite = LoadSingleSprite(TreasureChestPath, fallbackSprite);
            Sprite[] healingHerbFrames = LoadHerbFrames(HealingHerbPath, fallbackSprite);

            ApplyMainMapExpansion(
                enemyIdle, enemyRun, eliteIdle, eliteRun, blueIdle, blueRun, caveIdle, caveRun,
                ratRun, riderRun, ballistaFly,
                inkWolfIdle, stoneApeIdle, bambooPuppetIdle, treasureChestSprite,
                healingHerbFrames, LoadHerbFrames(DefenseHerbPath, fallbackSprite),
                LoadHerbFrames(MoveSpeedHerbPath, fallbackSprite),
                LoadHerbFrames(MysteryHerbPath, fallbackSprite));

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
            Debug.Log(
                "Main map refined to 64 x 56 with layered 60-second routes, " +
                "36 enemies, eight caves, six treasure chests, and distributed recovery/buff pickups.");
        }

        [MenuItem("37 MiniGame/Build Tutorial Level")]
        public static void BuildTutorialLevel()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before building the tutorial level.");
                return;
            }

            const string tutorialPath = "Assets/Scenes/TutorialLevel.unity";
            EditorSceneManager.SaveOpenScenes();
            if (File.Exists(tutorialPath))
            {
                File.Copy(ScenePath, tutorialPath, true);
                AssetDatabase.ImportAsset(tutorialPath, ImportAssetOptions.ForceSynchronousImport);
            }
            else if (!AssetDatabase.CopyAsset(ScenePath, tutorialPath))
            {
                Debug.LogError("Could not copy MainPrototype into TutorialLevel.");
                return;
            }

            AssetDatabase.Refresh();
            Scene tutorialScene = EditorSceneManager.OpenScene(tutorialPath, OpenSceneMode.Single);
            HashSet<string> tutorialEncounterNames = new HashSet<string>
            {
                "南桥药草",
                "隐市岩洞",
                "西路宝箱",
                "山贼喽啰"
            };

            foreach (EncounterTrigger encounter in UnityEngine.Object.FindObjectsByType<EncounterTrigger>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool keep = tutorialEncounterNames.Contains(encounter.name);
                encounter.gameObject.SetActive(keep);
                if (!keep)
                {
                    continue;
                }

                encounter.ResetEncounter(rerollCaveContent: true);
                switch (encounter.name)
                {
                    case "南桥药草":
                        encounter.transform.position = new Vector3(0f, 0f, -6f);
                        encounter.herbEffect = HerbEffectType.Heal;
                        encounter.healRatio = 0.35f;
                        break;
                    case "隐市岩洞":
                        encounter.transform.position = new Vector3(6f, 0f, 0f);
                        encounter.caveContent = CaveContentType.Treasure;
                        break;
                    case "西路宝箱":
                        encounter.transform.position = new Vector3(-6f, 0f, 0f);
                        break;
                    case "山贼喽啰":
                        encounter.transform.position = new Vector3(0f, 0f, 6f);
                        encounter.enemyStats.maxHealth = 24f;
                        encounter.enemyStats.currentHealth = 24f;
                        encounter.enemyStats.attack = 3f;
                        encounter.enemyStats.defense = 0f;
                        encounter.enemyStats.attackSpeed = 0.75f;
                        encounter.cultivationReward = 8;
                        encounter.copperReward = 2;
                        break;
                }

                EditorUtility.SetDirty(encounter);
                EditorUtility.SetDirty(encounter.transform);
            }

            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            if (mapRoot != null)
            {
                // TutorialLevel is copied from the full main map, so every piece of
                // inherited geometry must be explicitly reconciled with its compact
                // 18 x 18 play area. Keeping the 53/61 metre roads or the main-map
                // scenery makes them spill far beyond the tutorial boundary.
                SetChildActive(mapRoot.transform, "Expanded Main Map Content", false);
                SetChildActive(mapRoot.transform, "HD2D Main World Art", false);
                SetChildActive(mapRoot.transform, "KayKit Medieval Scenery", false);
                ResizeMapObject(mapRoot.transform, "Walkable Ground", Vector3.zero, new Vector3(1.8f, 1f, 1.8f));
                ResizeMapObject(mapRoot.transform, "Main Dirt Road", new Vector3(0f, 0.03f, 0f), new Vector3(2.4f, 0.05f, 13.5f));
                ResizeMapObject(mapRoot.transform, "Cross Dirt Road", new Vector3(0f, 0.031f, 0f), new Vector3(13.5f, 0.05f, 2.4f));
                SetChildActive(mapRoot.transform, "North Ridge Road", false);
                SetChildActive(mapRoot.transform, "South Cave Road", false);
                ResizeMapObject(mapRoot.transform, "North Boundary", new Vector3(0f, 0.55f, 8.5f), new Vector3(18f, 1.1f, 0.45f));
                ResizeMapObject(mapRoot.transform, "South Boundary", new Vector3(0f, 0.55f, -8.5f), new Vector3(18f, 1.1f, 0.45f));
                ResizeMapObject(mapRoot.transform, "West Boundary", new Vector3(-8.5f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 18f));
                ResizeMapObject(mapRoot.transform, "East Boundary", new Vector3(8.5f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 18f));
                CreateTutorialCompactScenery(mapRoot.transform);
            }

            GameFlowController tutorialFlow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            if (tutorialFlow != null)
            {
                tutorialFlow.mainTimeLimit = LevelSequence.TutorialTimeLimitSeconds;
                EditorUtility.SetDirty(tutorialFlow);
            }

            EditorSceneManager.MarkSceneDirty(tutorialScene);
            EditorSceneManager.SaveScene(tutorialScene);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(tutorialPath, true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("Tutorial level built: four interactive targets, one-click 30-second notice, and automatic Level 2 hand-off.");
        }

        [MenuItem("37 MiniGame/Refresh Herb Art")]
        public static void RefreshHerbArt()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Exit Play Mode before refreshing herb art.");
                return;
            }

            ConfigureHerbAssets();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            int refreshed = 0;
            foreach (EncounterTrigger encounter in UnityEngine.Object.FindObjectsByType<EncounterTrigger>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (encounter.encounterType != EncounterType.Herb &&
                    encounter.encounterType != EncounterType.MysteryHerb)
                {
                    continue;
                }

                string path = GetHerbSpritePath(encounter);
                Sprite[] frames = LoadHerbFrames(path, fallbackSprite);
                SpriteFrameAnimator animator = encounter.GetComponentInChildren<SpriteFrameAnimator>(true);
                SpriteRenderer renderer = encounter.GetComponentInChildren<SpriteRenderer>(true);
                if (animator != null)
                {
                    animator.idleFrames = frames;
                    animator.moveFrames = frames;
                    EditorUtility.SetDirty(animator);
                }

                if (renderer != null)
                {
                    renderer.sprite = frames[0];
                    EditorUtility.SetDirty(renderer);
                }

                EditorUtility.SetDirty(encounter);
                refreshed++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"Refreshed {refreshed} herb pickups with distinct effect-specific world sprites.");
        }

        private static void SetChildActive(Transform parent, string childName, bool active)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private static void CreateTutorialCompactScenery(Transform mapRoot)
        {
            Transform previous = mapRoot.Find("Tutorial Compact Scenery");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject scenery = new GameObject("Tutorial Compact Scenery");
            scenery.transform.SetParent(mapRoot);

            // Each landmark stays inside the compact boundary and sits behind its
            // related interaction, leaving all four approach lanes unobstructed.
            PlaceModel("mine", "East Cave Landmark", scenery.transform, new Vector3(7.25f, 0f, 0f), 2.1f, -90f);
            PlaceModel("wall_gate", "North Training Gate", scenery.transform, new Vector3(0f, 0f, 7.45f), 3.1f, 0f);
            PlaceModel("detail_rocks_small", "Northwest Corner Stones", scenery.transform, new Vector3(-6.9f, 0f, 6.9f), 1.25f, 25f);
            PlaceModel("detail_rocks_small", "Northeast Corner Stones", scenery.transform, new Vector3(6.9f, 0f, 6.9f), 1.25f, 110f);
            PlaceModel("detail_rocks_small", "Southwest Corner Stones", scenery.transform, new Vector3(-6.9f, 0f, -6.9f), 1.25f, 205f);
            PlaceModel("detail_rocks_small", "Southeast Corner Stones", scenery.transform, new Vector3(6.9f, 0f, -6.9f), 1.25f, 290f);
        }

        [MenuItem("37 MiniGame/Validate Expanded Build Content")]
        public static void ValidateExpandedBuildContent()
        {
            EncounterTrigger[] encounters = UnityEngine.Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include);
            int normalEnemies = encounters.Count(item => item.encounterType == EncounterType.NormalEnemy);
            int eliteEnemies = encounters.Count(item => item.encounterType == EncounterType.EliteEnemy);
            int caves = encounters.Count(item => item.encounterType == EncounterType.HiddenCave);
            int iconCount = Resources.LoadAll<Texture2D>("Icons").Length;

            List<string> failures = new List<string>();
            if (MartialArtCatalog.AllIds.Length != 20) failures.Add($"武学 {MartialArtCatalog.AllIds.Length}/20");
            if (MartialArtCatalog.AllSecretIds.Length != 5) failures.Add($"秘传 {MartialArtCatalog.AllSecretIds.Length}/5");
            if (PlayerEquipment.TreasureItemIds.Length + 3 != 15) failures.Add($"装备 {PlayerEquipment.TreasureItemIds.Length + 3}/15");
            if (RunContentCatalog.AllRelicIds.Length != 8) failures.Add($"遗物 {RunContentCatalog.AllRelicIds.Length}/8");
            if (RunContentCatalog.AllConsumableIds.Length != 6) failures.Add($"消耗品 {RunContentCatalog.AllConsumableIds.Length}/6");
            if (normalEnemies != 28) failures.Add($"普通敌人 {normalEnemies}/28");
            if (eliteEnemies != 8) failures.Add($"精英敌人 {eliteEnemies}/8");
            if (caves != 8) failures.Add($"洞穴 {caves}/8");
            if (iconCount < 70) failures.Add($"独立图标 {iconCount}/70");
            ValidateCaveSceneAssets(failures);

            if (failures.Count > 0)
            {
                Debug.LogError("Expanded build validation failed: " + string.Join(" · ", failures));
                return;
            }

            Debug.Log(
                "Expanded build validation passed: 20 arts, 5 secrets, 15 equipment, " +
                "8 relics, 6 consumables, 28 normal enemies, 8 elite enemies, 8 caves, 70 icons, " +
                "and nine formal cave scene assets.");
        }

        [MenuItem("37 MiniGame/Refresh Cave Scene Art")]
        public static void RefreshCaveSceneArt()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/Resources/CaveScenes" });
            int refreshed = 0;
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool isExit = assetPath.EndsWith("cave_exit_arch_v01.png");
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.alphaIsTransparency = isExit;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                refreshed += 1;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Cave scene art refreshed: {refreshed} textures configured for runtime use.");
        }

        [MenuItem("37 MiniGame/Validate Cave Scene Art")]
        public static void ValidateCaveSceneArt()
        {
            List<string> failures = new List<string>();
            ValidateCaveSceneAssets(failures);
            if (failures.Count > 0)
            {
                Debug.LogError("Cave scene art validation failed: " + string.Join(" · ", failures));
                return;
            }

            Debug.Log(
                "Cave scene art validation passed: four landscape backgrounds, four portrait backgrounds, " +
                "and one transparent exit arch.");
        }

        private static void ValidateCaveSceneAssets(List<string> failures)
        {
            string[] themes = { "combat", "sanctuary", "vault", "mystic" };
            foreach (string theme in themes)
            {
                ValidateCaveTexture(
                    $"CaveScenes/bg_cave_{theme}_landscape_v01", 1920, 1080, failures);
                ValidateCaveTexture(
                    $"CaveScenes/bg_cave_{theme}_portrait_v01", 1080, 1920, failures);
            }

            ValidateCaveTexture("CaveScenes/cave_exit_arch_v01", 512, 512, failures);
        }

        private static void ValidateCaveTexture(
            string resourcePath,
            int expectedWidth,
            int expectedHeight,
            List<string> failures)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                failures.Add(resourcePath + " 缺失");
                return;
            }

            if (texture.width != expectedWidth || texture.height != expectedHeight)
            {
                failures.Add(
                    $"{resourcePath} {texture.width}x{texture.height}/" +
                    $"{expectedWidth}x{expectedHeight}");
            }
        }

        [MenuItem("37 MiniGame/Refresh Enemy Variety")]
        public static void RefreshEnemyVariety()
        {
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

            ApplyLegacyVisualFamilyAliases(
                ratRun, riderRun, ballistaFly);

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

        [MenuItem("37 MiniGame/Refresh Orc Battle Presentation")]
        public static void RefreshOrcBattlePresentation()
        {
            string[] requiredSheets =
            {
                OrcWarlordIdlePath, OrcWarlordAttackPath,
                OrcCaveGuardianIdlePath, OrcCaveGuardianAttackPath
            };
            if (requiredSheets.Any(path => !File.Exists(path)))
            {
                Debug.LogError("Orc battle presentation refresh stopped: one or more generated sprite sheets are missing.");
                return;
            }

            ConfigureGeneratedMonsterAssets();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            Sprite[] orcWarlordIdle = LoadFrames(OrcWarlordIdlePath, fallbackSprite);
            Sprite[] orcWarlordAttack = LoadFrames(OrcWarlordAttackPath, fallbackSprite);
            Sprite[] orcCaveGuardianIdle = LoadFrames(OrcCaveGuardianIdlePath, fallbackSprite);
            Sprite[] orcCaveGuardianAttack = LoadFrames(OrcCaveGuardianAttackPath, fallbackSprite);

            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen != null)
            {
                battleScreen.caveIdleFrames = orcCaveGuardianIdle;
                battleScreen.caveAttackFrames = orcCaveGuardianAttack;
                battleScreen.bossSpriteScale = BossBattleVisualScale;
                battleScreen.enemyVisualProfiles = UpsertEnemyVisualProfile(
                    battleScreen.enemyVisualProfiles,
                    CreateEnemyVisualProfile("orc_warlord", orcWarlordIdle, orcWarlordAttack,
                        BossBattleVisualScale, true));
                battleScreen.enemyVisualProfiles = UpsertEnemyVisualProfile(
                    battleScreen.enemyVisualProfiles,
                    CreateEnemyVisualProfile("orc_cave_guardian", orcCaveGuardianIdle, orcCaveGuardianAttack,
                        CaveBattleVisualScale, true));
                EditorUtility.SetDirty(battleScreen);
            }

            GameFlowController gameFlow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            if (gameFlow != null && gameFlow.bossStats != null)
            {
                gameFlow.bossStats.visualId = "orc_warlord";
                EditorUtility.SetDirty(gameFlow);
            }

            foreach (EncounterTrigger caveEncounter in
                UnityEngine.Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (caveEncounter.encounterType != EncounterType.HiddenCave ||
                    caveEncounter.caveContent != CaveContentType.Enemy)
                {
                    continue;
                }

                caveEncounter.enemyStats.visualId = "orc_cave_guardian";
                EditorUtility.SetDirty(caveEncounter);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Orc boss and cave enemy battle presentation refreshed without changing world-map visuals.");
        }

        [MenuItem("37 MiniGame/Refresh Fox Demon Boss Presentation")]
        public static void RefreshFoxDemonBossPresentation()
        {
            string[] requiredSheets =
            {
                FoxDemonBossIdlePath,
                FoxDemonBossAttackPath
            };
            if (requiredSheets.Any(path => !File.Exists(path)))
            {
                Debug.LogError("Fox demon boss refresh stopped: one or more generated sprite sheets are missing.");
                return;
            }

            ConfigureGeneratedMonsterAssets();
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            Sprite[] foxDemonIdle = LoadFrames(FoxDemonBossIdlePath, fallbackSprite);
            Sprite[] foxDemonAttack = LoadFrames(FoxDemonBossAttackPath, fallbackSprite);

            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen != null)
            {
                battleScreen.bossSpriteScale = BossBattleVisualScale;
                battleScreen.enemyVisualProfiles = UpsertEnemyVisualProfile(
                    battleScreen.enemyVisualProfiles,
                    CreateEnemyVisualProfile("fox_demon_boss", foxDemonIdle, foxDemonAttack,
                        BossBattleVisualScale, true));
                EditorUtility.SetDirty(battleScreen);
            }

            GameFlowController gameFlow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            if (gameFlow != null && gameFlow.bossStats != null)
            {
                gameFlow.bossStats.displayName = GameTextCatalog.FinalBossName;
                gameFlow.bossStats.visualId = GameTextCatalog.FinalBossVisualId;
                EditorUtility.SetDirty(gameFlow);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Fox demon boss battle presentation refreshed; cave enemy presentation was left unchanged.");
        }

        [MenuItem("37 MiniGame/Refresh Xuanjia Mid Boss Presentation")]
        public static void RefreshXuanjiaMidBossPresentation()
        {
            string[] requiredSheets =
            {
                XuanjiaMidBossIdlePath,
                XuanjiaMidBossAttackPath,
                XuanjiaMidBossSkillPath, XuanjiaDoubleCleavePath, XuanjiaIronGuardPath,
                DoubleCleaveVfxPath, IronGuardVfxPath, MountainBreakerVfxPath
            };
            if (requiredSheets.Any(path => !File.Exists(path)))
            {
                Debug.LogError("Xuanjia mid boss refresh stopped: one or more generated sprite sheets are missing.");
                return;
            }

            foreach (string path in requiredSheets)
            {
                bool effect = path.Contains("/Effects/");
                ConfigureSpriteSheet(path, 256, 256, effect ? 256f : 160f,
                    effect ? (Vector2?)null : new Vector2(0.5f, 0.125f));
            }
            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            Sprite[] idleFrames = LoadFrames(XuanjiaMidBossIdlePath, fallbackSprite);
            Sprite[] attackFrames = LoadFrames(XuanjiaMidBossAttackPath, fallbackSprite);
            Sprite[] skillFrames = LoadFrames(XuanjiaMidBossSkillPath, fallbackSprite);

            BattleScreenController battleScreen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (battleScreen != null)
            {
                battleScreen.enemyVisualProfiles = UpsertEnemyVisualProfile(
                    battleScreen.enemyVisualProfiles,
                    CreateEnemyVisualProfile(
                        GameTextCatalog.MidBossVisualId,
                        idleFrames,
                        attackFrames,
                        MidBossBattleVisualScale,
                        false,
                        skillFrames));
                battleScreen.mountainBreakerEffectFrames =
                    LoadFrames(MountainBreakerVfxPath, fallbackSprite);
                battleScreen.doubleCleaveEffectFrames = LoadFrames(DoubleCleaveVfxPath, fallbackSprite);
                battleScreen.ironGuardEffectFrames = LoadFrames(IronGuardVfxPath, fallbackSprite);
                EditorUtility.SetDirty(battleScreen);
            }

            GameFlowController gameFlow = UnityEngine.Object.FindAnyObjectByType<GameFlowController>();
            if (gameFlow != null)
            {
                gameFlow.midBossTriggerElapsedTime = MidBossTuning.TriggerElapsedTime;
                gameFlow.midBossWarningDuration = MidBossTuning.WarningDuration;
                gameFlow.midBossStats ??= new CombatantStats();
                gameFlow.midBossStats.displayName = GameTextCatalog.MidBossName;
                gameFlow.midBossStats.visualId = GameTextCatalog.MidBossVisualId;
                gameFlow.midBossStats.maxHealth = MidBossTuning.MaxHealth;
                gameFlow.midBossStats.currentHealth = MidBossTuning.MaxHealth;
                gameFlow.midBossStats.attack = MidBossTuning.Attack;
                gameFlow.midBossStats.defense = 3f;
                gameFlow.midBossStats.attackSpeed = 0.78f;
                gameFlow.midBossStats.critChance = 0.04f;
                gameFlow.midBossStats.critMultiplier = 1.5f;
                EditorUtility.SetDirty(gameFlow);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Xuanjia mid boss, Mountain Breaker skill frames, VFX, and 30-second checkpoint were refreshed.");
        }

        private static BattleScreenController.EnemyVisualProfile[] UpsertEnemyVisualProfile(
            BattleScreenController.EnemyVisualProfile[] profiles,
            BattleScreenController.EnemyVisualProfile replacement)
        {
            List<BattleScreenController.EnemyVisualProfile> result =
                profiles != null
                    ? profiles.Where(profile => profile != null && profile.id != replacement.id).ToList()
                    : new List<BattleScreenController.EnemyVisualProfile>();
            result.Add(replacement);
            return result.ToArray();
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
            ApplyEncounterFrames(trigger, frames, visualScale);
        }

        private static void ApplyLegacyVisualFamilyAliases(
            Sprite[] lightFrames, Sprite[] mediumFrames, Sprite[] heavyFrames)
        {
            foreach (EncounterTrigger trigger in UnityEngine.Object.FindObjectsByType<EncounterTrigger>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (trigger == null || trigger.enemyStats == null)
                {
                    continue;
                }

                switch (trigger.enemyStats.visualId)
                {
                    case "rat":
                        ApplyEncounterFrames(trigger, lightFrames, 1.15f);
                        break;
                    case "rider":
                        ApplyEncounterFrames(trigger, mediumFrames, 1.15f);
                        break;
                    case "ballista":
                        ApplyEncounterFrames(trigger, heavyFrames, 1.25f);
                        break;
                }
            }
        }

        private static void ApplyEncounterFrames(
            EncounterTrigger trigger, Sprite[] frames, float visualScale)
        {
            if (trigger == null || frames == null || frames.Length == 0)
            {
                return;
            }

            GameObject encounterObject = trigger.gameObject;
            SpriteFrameAnimator animator = encounterObject.GetComponentInChildren<SpriteFrameAnimator>();
            SpriteRenderer renderer = encounterObject.GetComponentInChildren<SpriteRenderer>();
            if (animator == null || renderer == null)
            {
                Debug.LogWarning($"Cannot refresh enemy art: {encounterObject.name} is missing visual components.");
                return;
            }

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
            List<BattleScreenController.EnemyVisualProfile> profiles = new List<BattleScreenController.EnemyVisualProfile>
            {
                CreateEnemyVisualProfile("blue", inkWolfIdle, inkWolfAttack, ActorVisualScale.Medium, GeneratedEnemyBattleFlip),
                CreateEnemyVisualProfile("purple", bambooPuppetIdle, bambooPuppetAttack, ActorVisualScale.Medium, GeneratedEnemyBattleFlip),
                CreateEnemyVisualProfile("rat", ratRun, ratAttack, ActorVisualScale.Small, GeneratedEnemyBattleFlip),
                CreateEnemyVisualProfile("rider", riderRun, riderAttack, ActorVisualScale.Medium, GeneratedEnemyBattleFlip),
                CreateEnemyVisualProfile("ballista", ballistaFly, ballistaAttack, ActorVisualScale.Medium, GeneratedEnemyBattleFlip),
                CreateEnemyVisualProfile("ink_wolf", inkWolfIdle, inkWolfAttack, ActorVisualScale.Medium, GeneratedEnemyBattleFlip),
                CreateEnemyVisualProfile("stone_ape", stoneApeIdle, stoneApeAttack, 1.12f, GeneratedEnemyBattleFlip),
                CreateEnemyVisualProfile("bamboo_puppet", bambooPuppetIdle, bambooPuppetAttack, ActorVisualScale.Medium, GeneratedEnemyBattleFlip)
            };

            string[] newMonsterSheets =
            {
                ReedMantisIdlePath, ReedMantisAttackPath,
                BronzeToadIdlePath, BronzeToadAttackPath,
                CrimsonScorpionIdlePath, CrimsonScorpionAttackPath
            };
            if (newMonsterSheets.All(File.Exists))
            {
                Sprite fallbackSprite = GetOrCreatePrototypeSprite();
                profiles.Add(CreateEnemyVisualProfile("reed_mantis",
                    LoadFrames(ReedMantisIdlePath, fallbackSprite),
                    LoadFrames(ReedMantisAttackPath, fallbackSprite), 1.02f, GeneratedEnemyBattleFlip));
                profiles.Add(CreateEnemyVisualProfile("bronze_toad",
                    LoadFrames(BronzeToadIdlePath, fallbackSprite),
                    LoadFrames(BronzeToadAttackPath, fallbackSprite), 1.08f, GeneratedEnemyBattleFlip));
                profiles.Add(CreateEnemyVisualProfile("crimson_scorpion",
                    LoadFrames(CrimsonScorpionIdlePath, fallbackSprite),
                    LoadFrames(CrimsonScorpionAttackPath, fallbackSprite), 1.04f, GeneratedEnemyBattleFlip));
            }

            string[] requiredOrcSheets =
            {
                OrcWarlordIdlePath, OrcWarlordAttackPath,
                OrcCaveGuardianIdlePath, OrcCaveGuardianAttackPath
            };
            if (requiredOrcSheets.All(File.Exists))
            {
                Sprite fallbackSprite = GetOrCreatePrototypeSprite();
                Sprite[] orcWarlordIdle = LoadFrames(OrcWarlordIdlePath, fallbackSprite);
                Sprite[] orcWarlordAttack = LoadFrames(OrcWarlordAttackPath, fallbackSprite);
                Sprite[] orcCaveGuardianIdle = LoadFrames(OrcCaveGuardianIdlePath, fallbackSprite);
                Sprite[] orcCaveGuardianAttack = LoadFrames(OrcCaveGuardianAttackPath, fallbackSprite);
                profiles.Add(CreateEnemyVisualProfile("orc_warlord", orcWarlordIdle, orcWarlordAttack,
                    BossBattleVisualScale, true));
                profiles.Add(CreateEnemyVisualProfile("orc_cave_guardian", orcCaveGuardianIdle,
                    orcCaveGuardianAttack, CaveBattleVisualScale, true));
            }

            if (File.Exists(FoxDemonBossIdlePath) && File.Exists(FoxDemonBossAttackPath))
            {
                Sprite fallbackSprite = GetOrCreatePrototypeSprite();
                Sprite[] foxDemonIdle = LoadFrames(FoxDemonBossIdlePath, fallbackSprite);
                Sprite[] foxDemonAttack = LoadFrames(FoxDemonBossAttackPath, fallbackSprite);
                profiles.Add(CreateEnemyVisualProfile("fox_demon_boss", foxDemonIdle, foxDemonAttack,
                    BossBattleVisualScale, true));
            }

            if (File.Exists(XuanjiaMidBossIdlePath) &&
                File.Exists(XuanjiaMidBossAttackPath) &&
                File.Exists(XuanjiaMidBossSkillPath))
            {
                Sprite fallbackSprite = GetOrCreatePrototypeSprite();
                profiles.Add(CreateEnemyVisualProfile(
                    GameTextCatalog.MidBossVisualId,
                    LoadFrames(XuanjiaMidBossIdlePath, fallbackSprite),
                    LoadFrames(XuanjiaMidBossAttackPath, fallbackSprite),
                    MidBossBattleVisualScale,
                    false,
                    LoadFrames(XuanjiaMidBossSkillPath, fallbackSprite)));
            }

            return profiles.ToArray();
        }

        private static BattleScreenController.EnemyVisualProfile CreateEnemyVisualProfile(
            string id, Sprite[] idleFrames, Sprite[] attackFrames, float scale,
            bool flipHorizontally = false, Sprite[] skillFrames = null)
        {
            return new BattleScreenController.EnemyVisualProfile
            {
                id = id,
                idleFrames = idleFrames,
                attackFrames = attackFrames,
                skillFrames = skillFrames,
                doubleCleaveFrames = id == GameTextCatalog.MidBossVisualId
                    ? LoadFrames(XuanjiaDoubleCleavePath, idleFrames[0]) : null,
                ironGuardFrames = id == GameTextCatalog.MidBossVisualId
                    ? LoadFrames(XuanjiaIronGuardPath, idleFrames[0]) : null,
                foxfireFrames = id == GameTextCatalog.FinalBossVisualId ? LoadFrames(FoxfireActionPath, idleFrames[0]) : null,
                demonArmorFrames = id == GameTextCatalog.FinalBossVisualId ? LoadFrames(DemonArmorActionPath, idleFrames[0]) : null,
                bloodFrenzyFrames = id == GameTextCatalog.FinalBossVisualId ? LoadFrames(BloodFrenzyActionPath, idleFrames[0]) : null,
                foxfireVisualScale = id == GameTextCatalog.FinalBossVisualId ? ReadFinalBossScales().foxfire : 1f,
                demonArmorVisualScale = id == GameTextCatalog.FinalBossVisualId ? ReadFinalBossScales().demon_armor : 1f,
                bloodFrenzyVisualScale = id == GameTextCatalog.FinalBossVisualId ? ReadFinalBossScales().blood_frenzy : 1f,
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
                if (equipment.GetSwordQiDamageRatio(3) <= 0f)
                {
                    throw new InvalidOperationException("Equipment validation failed for the Qinggang sword trigger.");
                }

                equipment.Unequip(EquipmentSlot.Weapon);
                if (!Mathf.Approximately(stats.runtimeStats.attack, baseAttack))
                {
                    throw new InvalidOperationException("Equipment validation failed while unequipping a weapon.");
                }

                stats.ApplyMartialArt("剑气诀");
                stats.ApplyMartialArt("剑气诀");
                if (stats.GetMartialArtRank("剑气诀") != 2 ||
                    stats.learnedMartialArts.Count(art => art == "剑气诀") != 1)
                {
                    throw new InvalidOperationException("Martial art rank validation failed.");
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
            CreateFolderIfMissing("Assets/Art/Generated", "Backgrounds");
            CreateFolderIfMissing("Assets/Art/Generated/Environment", "Shaders");
            CreateFolderIfMissing("Assets/Art/Generated/Environment", "Materials");
            CreateFolderIfMissing(EnvironmentMaterialRoot, "Props");
            CreateFolderIfMissing("Assets", "Audio");
            CreateFolderIfMissing("Assets/Audio", "Generated");
            CreateFolderIfMissing("Assets/Audio/Generated", "Combat");
            CreateFolderIfMissing("Assets/Audio/Generated", "Music");
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
            ConfigureGeneratedMonsterAssets();
            ConfigureBattleFeedbackAssets();
            ConfigureMovementVfxAsset();
            ConfigureBattleBackgroundAssets();
            ConfigureHudContentIcons();
            ConfigureBossIntroAssets();

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
            ConfigureSingleSprite(TreasureChestPath, 512f);
            ConfigureHerbAssets();
            ConfigureUiTexture(StatusIconPath);
            ConfigureUiTexture(EquipmentIconPath);
            ConfigureUiTexture(HealthBarBasePath);
            ConfigureUiTexture(HealthBarFillPath);
        }

        private static void ConfigureBattleFeedbackAssets()
        {
            ConfigureSpriteSheet(CombatImpactVfxPath, 256, 256, 256f);
            ConfigureSpriteSheet(SwordQiVfxPath, 256, 256, 256f);
            ConfigureSpriteSheet(PoisonMistVfxPath, 256, 256, 256f);
            ConfigureSpriteSheet(MountainBreakerVfxPath, 256, 256, 256f);
            ConfigureSpriteSheet(DoubleCleaveVfxPath, 256, 256, 256f);
            ConfigureSpriteSheet(IronGuardVfxPath, 256, 256, 256f);
            foreach (string path in FinalBossEffectPaths) ConfigureSpriteSheet(path, 256, 256, 256f);
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

        private static void ConfigureMovementVfxAsset()
        {
            if (!File.Exists(SpeedBoostVfxPath))
            {
                Debug.LogWarning($"Missing movement VFX asset: {SpeedBoostVfxPath}");
                return;
            }

            AssetDatabase.ImportAsset(SpeedBoostVfxPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(SpeedBoostVfxPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
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
                         BlackIronRingIconPath, WandererCloakIconPath,
                         MainHudPortraitFramePath, MainHudPlayerStatusIconPath, MainHudTimeIconPath,
                         MainHudCopperIconPath, MainHudCultivationIconPath
                     })
            {
                ConfigureIconTexture(path);
            }
        }

        private static void BindHudContentIcons(PrototypeHUDController hud)
        {
            hud.mainMenuBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(MainMenuBackgroundPath);
            hud.playerPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>(MainHudPlayerStatusIconPath);
            hud.playerPortraitFrame = AssetDatabase.LoadAssetAtPath<Texture2D>(MainHudPortraitFramePath);
            hud.timeHudIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(MainHudTimeIconPath);
            hud.copperHudIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(MainHudCopperIconPath);
            hud.cultivationHudIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(MainHudCultivationIconPath);
            hud.martialArtIcons = new[]
            {
                Icon("剑气诀", JianQiIconPath),
                Icon("疾剑式", JiJianIconPath),
                Icon("铁布衫", TieBuShanIconPath),
                Icon("吸星诀", XiXingIconPath),
                Icon("毒砂掌", DuShaZhangIconPath),
                Icon("破甲掌", PoJiaZhangIconPath),
                Icon("百毒心经", DuShaZhangIconPath),
                Icon("金钟罩", TieBuShanIconPath),
                Icon("反震诀", TieBuShanIconPath)
            };
            hud.equipmentItemIcons = new[]
            {
                Icon("qinggang_sword", QingGangSwordIconPath),
                Icon("light_scale", LightScaleIconPath),
                Icon("practice_bracer", PracticeBracerIconPath),
                Icon("black_iron_ring", BlackIronRingIconPath),
                Icon("wanderer_cloak", WandererCloakIconPath),
                Icon("poison_dart_pouch", BlackIronRingIconPath)
            };
        }

        private static void BindBossIntroAssets(PrototypeHUDController hud)
        {
            hud.bossPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>(BossIntroPortraitPath);
            hud.bossPortraitFrame = AssetDatabase.LoadAssetAtPath<Texture2D>(BossIntroFramePath);
        }

        private static void ConfigureBossIntroAssets()
        {
            ConfigureBossIntroTexture(BossIntroPortraitPath);
            ConfigureBossIntroTexture(BossIntroFramePath);
        }

        private static void ConfigureBossIntroTexture(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Missing Boss-introduction UI asset: {path}");
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
            importer.spritePixelsPerUnit = 256f;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 256;
            importer.SaveAndReimport();
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

        private static void ConfigureGeneratedMonsterAssets()
        {
            string[] sheets =
            {
                InkWolfIdlePath, InkWolfAttackPath,
                StoneApeIdlePath, StoneApeAttackPath,
                BambooPuppetIdlePath, BambooPuppetAttackPath,
                ReedMantisIdlePath, ReedMantisAttackPath,
                BronzeToadIdlePath, BronzeToadAttackPath,
                CrimsonScorpionIdlePath, CrimsonScorpionAttackPath,
                FoxDemonBossIdlePath, FoxDemonBossAttackPath,
                FoxfireActionPath, DemonArmorActionPath, BloodFrenzyActionPath,
                XuanjiaMidBossIdlePath, XuanjiaMidBossAttackPath, XuanjiaMidBossSkillPath,
                XuanjiaDoubleCleavePath, XuanjiaIronGuardPath,
                OrcWarlordIdlePath, OrcWarlordAttackPath,
                OrcCaveGuardianIdlePath, OrcCaveGuardianAttackPath
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

        private static void ConfigureBattleBackgroundAssets()
        {
            ConfigureBackgroundTexture(MainMenuBackgroundPath);
            foreach (string path in NormalBattleBackgroundPaths)
            {
                ConfigureBackgroundTexture(path);
            }

            ConfigureBackgroundTexture(BossBattleBackgroundPath);
        }

        private static void ConfigureBackgroundTexture(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Missing generated background: {path}");
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
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void BindBattleBackgrounds(BattleScreenController battleScreen)
        {
            battleScreen.normalBattleBackgrounds = NormalBattleBackgroundPaths
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(texture => texture != null)
                .ToArray();
            battleScreen.bossBattleBackground =
                AssetDatabase.LoadAssetAtPath<Texture2D>(BossBattleBackgroundPath);
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

        private static Sprite[] LoadHerbFrames(string path, Sprite fallback)
        {
            return new[] { LoadSingleSprite(path, fallback) };
        }

        private static void ConfigureHerbAssets()
        {
            foreach (string path in new[]
                     {
                         HealingHerbPath, AttackHerbPath, DefenseHerbPath,
                         MoveSpeedHerbPath, MysteryHerbPath
                     })
            {
                ConfigureSingleSprite(path, 160f);
            }
        }

        private static string GetHerbSpritePath(EncounterTrigger encounter)
        {
            if (encounter.encounterType == EncounterType.MysteryHerb)
            {
                return MysteryHerbPath;
            }

            return encounter.herbEffect switch
            {
                HerbEffectType.Attack => AttackHerbPath,
                HerbEffectType.Defense => DefenseHerbPath,
                HerbEffectType.MoveSpeed => MoveSpeedHerbPath,
                _ => HealingHerbPath
            };
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

        private static Material GetOrCreateMainMapSkyMaterial()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("Cannot find the built-in Skybox/Procedural shader.");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "MainMap_SoftSky"
                };
                AssetDatabase.CreateAsset(material, SkyMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetFloat("_SunDisk", 2f);
            material.SetFloat("_SunSize", 0.035f);
            material.SetFloat("_SunSizeConvergence", 5f);
            material.SetFloat("_AtmosphereThickness", 0.82f);
            material.SetColor("_SkyTint", new Color(0.38f, 0.55f, 0.74f, 1f));
            material.SetColor("_GroundColor", new Color(0.55f, 0.65f, 0.68f, 1f));
            material.SetFloat("_Exposure", 1.02f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureHd2dWorldArtAssets()
        {
            ConfigureHd2dSpriteTexture(Hd2dBackdropPath, 32f, 2048);
            ConfigureHd2dSpriteTexture(Hd2dBambooPath, 100f, 1024);
            ConfigureHd2dSpriteTexture(Hd2dPineRockPath, 100f, 1024);
            ConfigureHd2dSpriteTexture(Hd2dMistPath, 32f, 1024);
            ConfigureHd2dSpriteTexture(Hd2dLightBeamPath, 64f, 512);

            TextureImporter panoramaImporter = AssetImporter.GetAtPath(Hd2dPanoramaPath) as TextureImporter;
            if (panoramaImporter != null)
            {
                panoramaImporter.textureType = TextureImporterType.Default;
                panoramaImporter.npotScale = TextureImporterNPOTScale.None;
                panoramaImporter.wrapMode = TextureWrapMode.Repeat;
                panoramaImporter.filterMode = FilterMode.Bilinear;
                panoramaImporter.mipmapEnabled = true;
                panoramaImporter.sRGBTexture = true;
                panoramaImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                panoramaImporter.maxTextureSize = 2048;
                panoramaImporter.SaveAndReimport();
            }

            TextureImporter waterImporter = AssetImporter.GetAtPath(Hd2dWaterTexturePath) as TextureImporter;
            if (waterImporter != null)
            {
                waterImporter.textureType = TextureImporterType.Default;
                waterImporter.npotScale = TextureImporterNPOTScale.None;
                waterImporter.wrapMode = TextureWrapMode.Repeat;
                waterImporter.filterMode = FilterMode.Bilinear;
                waterImporter.mipmapEnabled = true;
                waterImporter.sRGBTexture = true;
                waterImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                waterImporter.maxTextureSize = 1024;
                waterImporter.SaveAndReimport();
            }
        }

        private static void ConfigureHd2dSpriteTexture(string path, float pixelsPerUnit, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing HD-2D world-art texture: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }

        private static void BuildHd2dBackdrop(Transform parent)
        {
            Material ground = GetOrCreateMainMapGroundMaterial();

            // Keep the playable 64 x 56 ground and its collision boundaries unchanged,
            // but continue the painted terrain well beyond the camera's maximum follow
            // offset. The overlap sits slightly below the real ground, so it cannot
            // introduce z-fighting or become walkable while hiding the hard map cut.
            GameObject boundarySkirt = GameObject.CreatePrimitive(PrimitiveType.Plane);
            boundarySkirt.name = "Boundary Ground Skirt";
            boundarySkirt.transform.SetParent(parent);
            boundarySkirt.transform.position = new Vector3(0f, -0.055f, 0f);
            boundarySkirt.transform.localScale = new Vector3(9f, 1f, 8.2f);
            boundarySkirt.GetComponent<Renderer>().sharedMaterial = ground;
            Collider boundarySkirtCollider = boundarySkirt.GetComponent<Collider>();
            if (boundarySkirtCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(boundarySkirtCollider);
            }

            CreateDecorativeTerrace("North Diorama Terrace", parent, new Vector3(0f, -0.32f, 29.8f), new Vector3(64f, 0.7f, 4.2f), ground);
            CreateDecorativeTerrace("South Diorama Terrace", parent, new Vector3(0f, -0.38f, -29.8f), new Vector3(64f, 0.55f, 3.2f), ground);
            BuildBoundaryForest(parent);
        }

        private static void BuildHd2dRegionDistricts(Transform parent)
        {
            GameObject regions = new GameObject("Five Main Map Districts");
            regions.transform.SetParent(parent);

            Material central = CreateDistrictSurfaceMaterial(
                "District_Central_Earth", new Color(0.70f, 0.66f, 0.53f));
            Material east = CreateDistrictSurfaceMaterial(
                "District_East_Amber", new Color(0.72f, 0.68f, 0.48f));
            Material west = CreateDistrictSurfaceMaterial(
                "District_West_Jade", new Color(0.55f, 0.70f, 0.55f));
            Material north = CreateDistrictSurfaceMaterial(
                "District_North_Slate", new Color(0.59f, 0.67f, 0.66f));
            Material south = CreateDistrictSurfaceMaterial(
                "District_South_Ochre", new Color(0.70f, 0.57f, 0.43f));
            Material stone = Material("District_Marker_Stone", new Color(0.43f, 0.42f, 0.35f));
            Material eastInlay = Material("District_East_Inlay", new Color(0.62f, 0.48f, 0.19f));
            Material westInlay = Material("District_West_Inlay", new Color(0.25f, 0.48f, 0.31f));
            Material northInlay = Material("District_North_Inlay", new Color(0.35f, 0.49f, 0.54f));
            Material southInlay = Material("District_South_Inlay", new Color(0.58f, 0.30f, 0.19f));

            CreateRegionGroundPatch("Central Courier Ground", regions.transform,
                new Vector3(0f, 0.008f, 1f), new Vector3(13f, 0.012f, 14f), central);
            CreateRegionGroundPatch("East Hamlet Ground", regions.transform,
                new Vector3(19f, 0.006f, 11f), new Vector3(21f, 0.009f, 18f), east);
            CreateRegionGroundPatch("West Forest Ground", regions.transform,
                new Vector3(-19f, 0.006f, -10.5f), new Vector3(21f, 0.009f, 19f), west);
            CreateRegionGroundPatch("North Ridge Ground", regions.transform,
                new Vector3(-1f, 0.006f, 18f), new Vector3(43f, 0.009f, 14f), north);
            CreateRegionGroundPatch("South Mine Ground", regions.transform,
                new Vector3(4f, 0.006f, -19f), new Vector3(45f, 0.009f, 12f), south);

            CreateRegionGate("East Hamlet District Gate", regions.transform,
                new Vector3(8.8f, 0f, 1f), 90f,
                "东郊机关庄", "快剑 · 破甲", "中风险", WuxiaUiTheme.Gold, stone, eastInlay);
            CreateRegionGate("West Forest District Gate", regions.transform,
                new Vector3(-8.8f, 0f, 0.4f), 90f,
                "西林毒泽", "毒掌 · 续航", "中风险", WuxiaUiTheme.Jade, stone, westInlay);
            CreateRegionGate("North Ridge District Gate", regions.transform,
                new Vector3(-1.2f, 0f, 9.4f), 0f,
                "北岭关隘", "铁壁 · 防御", "中高风险", WuxiaUiTheme.Paused, stone, northInlay);
            CreateRegionGate("South Mine District Gate", regions.transform,
                new Vector3(1.4f, 0f, -9.4f), 0f,
                "南矿山路", "装备 · 高收益", "中高风险", WuxiaUiTheme.Warning, stone, southInlay);
        }

        private static Material CreateDistrictSurfaceMaterial(string name, Color tint)
        {
            Shader shader = Shader.Find(WorldSurfaceShaderName) ?? Shader.Find("Standard");
            Material material = new Material(shader) { name = name };
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexturePath);
            if (texture != null)
            {
                material.SetTexture("_MainTex", texture);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }
            if (material.HasProperty("_WorldTiling"))
            {
                material.SetFloat("_WorldTiling", 0.18f);
            }
            return material;
        }

        private static void CreateRegionGroundPatch(
            string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            // Region identity is a visual overlay, not terrain volume. A thin Cube
            // intersects roads and stream banks and can expose diagonal faces or
            // shadow seams. Keep one flat plane just above the base grass while all
            // authored roads, river surfaces and bridgeheads remain above it.
            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Plane);
            patch.name = name;
            patch.transform.SetParent(parent);
            patch.transform.position = new Vector3(position.x, 0.0015f, position.z);
            patch.transform.localScale = new Vector3(scale.x / 10f, 1f, scale.z / 10f);
            Renderer renderer = patch.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Collider collider = patch.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void CreateRegionGate(
            string name,
            Transform parent,
            Vector3 position,
            float yRotation,
            string regionName,
            string routeTheme,
            string riskLabel,
            Color accent,
            Material stone,
            Material inlay)
        {
            GameObject gate = PlaceModel("detail_rocks", name, parent, position, 1.65f, yRotation);
            if (gate == null)
            {
                gate = new GameObject(name);
                gate.transform.SetParent(parent);
                gate.transform.position = position;
                gate.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

                CreateLocalCube("Route Waystone", gate.transform,
                    new Vector3(0f, 0.65f, 0f), new Vector3(0.72f, 1.3f, 0.52f), stone);
            }

            MainMapRegionGuide guide = gate.AddComponent<MainMapRegionGuide>();
            guide.regionName = regionName;
            guide.routeTheme = routeTheme;
            guide.riskLabel = riskLabel;
            guide.accent = accent;
            guide.worldHeight = 2.75f;
            guide.detailDistance = 9f;
            guide.maxVisibleDistance = 14f;
        }

        private static void BuildBoundaryForest(Transform parent)
        {
            GameObject forest = new GameObject("Boundary Forest Belt");
            forest.transform.SetParent(parent);
            Sprite bamboo = AssetDatabase.LoadAssetAtPath<Sprite>(Hd2dBambooPath);
            Sprite pine = AssetDatabase.LoadAssetAtPath<Sprite>(Hd2dPineRockPath);

            CreateBoundaryTreeLine(
                forest.transform, "North Forest", bamboo, pine,
                new Vector3(-31f, 0f, 29.6f), Vector3.right, 19, 3.45f, 0);
            CreateBoundaryTreeLine(
                forest.transform, "South Forest", bamboo, pine,
                new Vector3(-31f, 0f, -29.6f), Vector3.right, 19, 3.45f, 1);
            CreateBoundaryTreeLine(
                forest.transform, "West Forest", bamboo, pine,
                new Vector3(-33.5f, 0f, -27f), Vector3.forward, 17, 3.375f, 2);
            CreateBoundaryTreeLine(
                forest.transform, "East Forest", bamboo, pine,
                new Vector3(33.5f, 0f, -27f), Vector3.forward, 17, 3.375f, 3);
        }

        private static void CreateBoundaryTreeLine(
            Transform parent,
            string lineName,
            Sprite bamboo,
            Sprite pine,
            Vector3 start,
            Vector3 stepDirection,
            int count,
            float spacing,
            int variantOffset)
        {
            GameObject line = new GameObject(lineName);
            line.transform.SetParent(parent);

            for (int i = 0; i < count; i++)
            {
                bool usePine = (i + variantOffset) % 3 == 0;
                Sprite sprite = usePine ? pine : bamboo;
                float scale = usePine
                    ? 0.5f + ((i + variantOffset) % 2) * 0.04f
                    : 0.56f + ((i + variantOffset) % 3) * 0.035f;
                Vector3 position = start + stepDirection * (i * spacing);
                Color tint = Color.Lerp(
                    new Color(0.72f, 0.79f, 0.68f, 0.96f),
                    new Color(0.90f, 0.91f, 0.78f, 0.96f),
                    ((i + variantOffset) % 4) / 3f);
                CreateHd2dBillboard(
                    $"{lineName} Grove {i + 1:00}",
                    line.transform,
                    sprite,
                    position,
                    scale,
                    tint,
                    1 + (i + variantOffset) % 2,
                    (i + variantOffset) % 2 == 0);
            }
        }

        private static void BuildHd2dStream(Transform parent)
        {
            GameObject streamRoot = new GameObject("Celadon Stream");
            streamRoot.transform.SetParent(parent);
            Material water = GetOrCreateHd2dWaterMaterial();
            Material bank = GetOrCreateMainMapRoadMaterial();
            Vector2[] centerLine = MainMapRiverLayout.CenterLine;
            float[] halfWidths = MainMapRiverLayout.HalfWidths;
            AlignRoadsWithRiverCrossings();
            CreateStreamRibbon("Packed-Earth Stream Banks", streamRoot.transform, centerLine, halfWidths, 0.006f, 0.72f, bank);
            CreateStreamRibbon("Winding Celadon Water", streamRoot.transform, centerLine, halfWidths, 0.018f, 0f, water);

            BuildRiverBarriersAndBridges(streamRoot.transform, centerLine, halfWidths);

            PlaceModel("detail_rocks_small", "West Stream Bank Stones A", streamRoot.transform, new Vector3(-25f, 0f, -4.4f), 1.25f, 20f);
            PlaceModel("detail_rocks_small", "West Stream Bank Stones B", streamRoot.transform, new Vector3(-12.5f, 0f, -6.5f), 1.1f, 95f);
            PlaceModel("detail_rocks", "Central Stream Bank Rocks", streamRoot.transform, new Vector3(6.8f, 0f, -5.1f), 1.55f, 55f);
            PlaceModel("detail_rocks_small", "East Stream Bank Stones", streamRoot.transform, new Vector3(25.5f, 0f, -1.4f), 1.2f, 145f);
        }

        private static void BuildRiverBarriersAndBridges(
            Transform streamRoot,
            Vector2[] centerLine,
            float[] halfWidths)
        {
            DestroySceneObjectIfPresent("Old Road Bridge");
            DestroySceneObjectIfPresent("West Creek Bridge");

            GameObject crossingRoot = new GameObject("Formal River Crossings");
            crossingRoot.transform.SetParent(streamRoot);
            for (int i = 0; i < MainMapRiverLayout.BridgePointIndices.Length; i++)
            {
                int pointIndex = MainMapRiverLayout.BridgePointIndices[i];
                Vector2 previous = centerLine[Mathf.Max(0, pointIndex - 1)];
                Vector2 next = centerLine[Mathf.Min(centerLine.Length - 1, pointIndex + 1)];
                Vector2 tangent = (next - previous).normalized;
                float tangentAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                Vector2 point = centerLine[pointIndex];
                BuildFormalBridge(
                    MainMapRiverLayout.BridgeNames[i],
                    crossingRoot.transform,
                    new Vector3(point.x, 0f, point.y),
                    Mathf.Max(4.2f, halfWidths[pointIndex] * 2f + 1.2f),
                    tangentAngle + 90f);
            }

            GameObject barrierRoot = new GameObject("River Collision Banks");
            barrierRoot.transform.SetParent(streamRoot);
            float[] cumulative = MainMapRiverLayout.GetCumulativeDistances();
            float[] bridgeDistances = MainMapRiverLayout.GetBridgeDistances();

            for (int segmentIndex = 0; segmentIndex < centerLine.Length - 1; segmentIndex++)
            {
                float segmentStart = cumulative[segmentIndex];
                float segmentEnd = cumulative[segmentIndex + 1];
                List<Vector2> intervals = new List<Vector2> { new Vector2(segmentStart, segmentEnd) };
                foreach (float bridgeDistance in bridgeDistances)
                {
                    intervals = SubtractInterval(
                        intervals,
                        bridgeDistance - MainMapRiverLayout.BridgeGapHalfLength,
                        bridgeDistance + MainMapRiverLayout.BridgeGapHalfLength);
                }

                Vector2 segment = centerLine[segmentIndex + 1] - centerLine[segmentIndex];
                float segmentLength = segment.magnitude;
                foreach (Vector2 interval in intervals)
                {
                    if (interval.y - interval.x < 0.05f)
                    {
                        continue;
                    }

                    float startT = Mathf.InverseLerp(segmentStart, segmentEnd, interval.x);
                    float endT = Mathf.InverseLerp(segmentStart, segmentEnd, interval.y);
                    Vector2 start = Vector2.Lerp(centerLine[segmentIndex], centerLine[segmentIndex + 1], startT);
                    Vector2 end = Vector2.Lerp(centerLine[segmentIndex], centerLine[segmentIndex + 1], endT);
                    float width = Mathf.Max(
                        Mathf.Lerp(halfWidths[segmentIndex], halfWidths[segmentIndex + 1], startT),
                        Mathf.Lerp(halfWidths[segmentIndex], halfWidths[segmentIndex + 1], endT));
                    CreateRiverBarrierPiece(barrierRoot.transform, start, end, width);
                }
            }
        }

        private static void BuildFormalBridge(
            string bridgeName,
            Transform parent,
            Vector3 position,
            float bridgeLength,
            float yRotation)
        {
            GameObject bridge = new GameObject(bridgeName);
            bridge.transform.SetParent(parent);
            bridge.transform.position = position;
            // The generated bridge uses local Z as its crossing direction so the
            // deck arch, player lift and rail colliders share one coordinate system.
            bridge.transform.rotation = Quaternion.Euler(0f, 90f - yRotation, 0f);

            MainMapBridgeSurface surface = bridge.AddComponent<MainMapBridgeSurface>();
            surface.halfLength = bridgeLength * 0.5f;
            surface.halfWidth = 0.82f;
            surface.maximumVisualRise = 1.02f;

            BuildProceduralBridgeVisual(bridge.transform, bridgeLength, surface.maximumVisualRise);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int segment = -1; segment <= 1; segment++)
                {
                    GameObject rail = new GameObject($"Bridge Rail Collider {side:+0;-0} {segment + 2}");
                    rail.transform.SetParent(bridge.transform, false);
                    rail.transform.localPosition = new Vector3(
                        side * 0.88f,
                        0.72f,
                        segment * bridgeLength * 0.31f);
                    BoxCollider collider = rail.AddComponent<BoxCollider>();
                    collider.size = new Vector3(0.24f, 1.44f, bridgeLength * 0.38f);
                }
            }
        }

        private static void BuildProceduralBridgeVisual(
            Transform bridge,
            float bridgeLength,
            float maximumRise)
        {
            Material wood = Material("Bridge_Warm_Wood", new Color(0.34f, 0.20f, 0.10f));
            Material darkWood = Material("Bridge_Dark_Wood", new Color(0.18f, 0.11f, 0.065f));
            Material stone = Material("Bridge_Abutment_Stone", new Color(0.40f, 0.39f, 0.33f));
            float halfLength = bridgeLength * 0.5f;
            const int plankCount = 13;
            float plankDepth = bridgeLength / plankCount * 1.08f;

            GameObject deck = new GameObject("Unity Generated Arched Deck");
            deck.transform.SetParent(bridge, false);
            for (int i = 0; i < plankCount; i++)
            {
                float t = i / (plankCount - 1f);
                float z = Mathf.Lerp(-halfLength, halfLength, t);
                float height = BridgeArchHeight(z, halfLength, maximumRise);
                float slope = -2f * maximumRise * z / (halfLength * halfLength);
                GameObject plank = CreateLocalCube(
                    $"Bridge Deck Plank {i + 1:00}",
                    deck.transform,
                    new Vector3(0f, height - 0.065f, z),
                    new Vector3(1.82f, 0.13f, plankDepth),
                    i % 2 == 0 ? wood : darkWood);
                plank.transform.localRotation = Quaternion.Euler(
                    -Mathf.Atan(slope) * Mathf.Rad2Deg, 0f, 0f);
                UnityEngine.Object.DestroyImmediate(plank.GetComponent<Collider>());
            }

            GameObject rails = new GameObject("Unity Generated Open Rails");
            rails.transform.SetParent(bridge, false);
            const int postCount = 7;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < postCount; i++)
                {
                    float t = i / (postCount - 1f);
                    float z = Mathf.Lerp(-halfLength, halfLength, t);
                    float height = BridgeArchHeight(z, halfLength, maximumRise);
                    GameObject post = CreateLocalCube(
                        $"Bridge Rail Post {side:+0;-0} {i + 1:00}",
                        rails.transform,
                        new Vector3(side * 1.01f, height + 0.34f, z),
                        new Vector3(0.13f, 0.76f, 0.13f),
                        darkWood);
                    UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());

                    if (i >= postCount - 1)
                    {
                        continue;
                    }

                    float nextT = (i + 1f) / (postCount - 1f);
                    float nextZ = Mathf.Lerp(-halfLength, halfLength, nextT);
                    float middleZ = (z + nextZ) * 0.5f;
                    float middleHeight = BridgeArchHeight(middleZ, halfLength, maximumRise);
                    float slope = -2f * maximumRise * middleZ / (halfLength * halfLength);
                    GameObject handrail = CreateLocalCube(
                        $"Bridge Handrail {side:+0;-0} {i + 1:00}",
                        rails.transform,
                        new Vector3(side * 1.01f, middleHeight + 0.70f, middleZ),
                        new Vector3(0.12f, 0.12f, (nextZ - z) * 1.08f),
                        darkWood);
                    handrail.transform.localRotation = Quaternion.Euler(
                        -Mathf.Atan(slope) * Mathf.Rad2Deg, 0f, 0f);
                    UnityEngine.Object.DestroyImmediate(handrail.GetComponent<Collider>());
                }
            }

            for (int end = -1; end <= 1; end += 2)
            {
                GameObject abutment = CreateLocalCube(
                    end < 0 ? "Near Stone Bridgehead" : "Far Stone Bridgehead",
                    bridge,
                    new Vector3(0f, -0.08f, end * (halfLength + 0.18f)),
                    new Vector3(2.45f, 0.24f, 0.62f),
                    stone);
                UnityEngine.Object.DestroyImmediate(abutment.GetComponent<Collider>());
            }
        }

        private static float BridgeArchHeight(float localZ, float halfLength, float maximumRise)
        {
            float normalized = Mathf.Clamp01(Mathf.Abs(localZ) / halfLength);
            return maximumRise * (1f - normalized * normalized);
        }

        private static void AlignRoadsWithRiverCrossings()
        {
            MoveRoadToX("West Frontier Trail", MainMapRiverLayout.CenterLine[2].x);
            MoveRoadToX("East Frontier Trail", MainMapRiverLayout.CenterLine[8].x);

            GameObject westForestRoad = GameObject.Find("West Forest Road");
            if (westForestRoad != null)
            {
                westForestRoad.transform.position = new Vector3(-16f, 0.028f, -11f);
                westForestRoad.transform.localScale = new Vector3(2.1f, 0.05f, 6f);
                EditorUtility.SetDirty(westForestRoad.transform);
            }

            SplitOuterRingRoad(
                "Far West Ring",
                "Far West Ring South Bank",
                -17.1f,
                14.8f,
                9.35f,
                30.3f);
            SplitOuterRingRoad(
                "Far East Ring",
                "Far East Ring South Bank",
                -13.7f,
                21.6f,
                12.8f,
                23.4f);
        }

        private static void MoveRoadToX(string objectName, float x)
        {
            GameObject road = GameObject.Find(objectName);
            if (road == null)
            {
                return;
            }

            Vector3 position = road.transform.position;
            position.x = x;
            road.transform.position = position;
            EditorUtility.SetDirty(road.transform);
        }

        private static void SplitOuterRingRoad(
            string northRoadName,
            string southRoadName,
            float southCenterZ,
            float southLength,
            float northCenterZ,
            float northLength)
        {
            GameObject northRoad = GameObject.Find(northRoadName);
            if (northRoad == null)
            {
                return;
            }

            Renderer renderer = northRoad.GetComponent<Renderer>();
            Material roadMaterial = renderer != null ? renderer.sharedMaterial : GetOrCreateMainMapRoadMaterial();
            Vector3 originalPosition = northRoad.transform.position;
            Vector3 originalScale = northRoad.transform.localScale;
            northRoad.transform.position = new Vector3(originalPosition.x, originalPosition.y, northCenterZ);
            northRoad.transform.localScale = new Vector3(originalScale.x, originalScale.y, northLength);
            EditorUtility.SetDirty(northRoad.transform);

            DestroySceneObjectIfPresent(southRoadName);
            CreateCube(
                southRoadName,
                northRoad.transform.parent,
                new Vector3(originalPosition.x, originalPosition.y, southCenterZ),
                new Vector3(originalScale.x, originalScale.y, southLength),
                roadMaterial);
        }

        private static IEnumerable<Renderer> FindRoadRenderersCrossingRiverAwayFromBridges()
        {
            float[] cumulative = MainMapRiverLayout.GetCumulativeDistances();
            float[] bridgeDistances = MainMapRiverLayout.GetBridgeDistances();
            return UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(renderer => renderer != null && renderer.bounds.size.y < 0.2f)
                .Where(renderer => IsRoadSurfaceName(renderer.gameObject.name))
                .Where(renderer => RoadCrossesRiverAwayFromBridge(
                    renderer.bounds, cumulative, bridgeDistances));
        }

        private static bool IsRoadSurfaceName(string objectName)
        {
            return objectName.Contains("Road") ||
                   objectName.Contains("Trail") ||
                   objectName.Contains("Route") ||
                   objectName.Contains("Ring") ||
                   objectName.Contains("Plaza Ground");
        }

        private static bool RoadCrossesRiverAwayFromBridge(
            Bounds roadBounds,
            float[] cumulative,
            float[] bridgeDistances)
        {
            Vector2[] centerLine = MainMapRiverLayout.CenterLine;
            for (int segmentIndex = 0; segmentIndex < centerLine.Length - 1; segmentIndex++)
            {
                for (int sampleIndex = 0; sampleIndex <= 24; sampleIndex++)
                {
                    float t = sampleIndex / 24f;
                    Vector2 point = Vector2.Lerp(
                        centerLine[segmentIndex], centerLine[segmentIndex + 1], t);
                    if (point.x < roadBounds.min.x - 0.08f ||
                        point.x > roadBounds.max.x + 0.08f ||
                        point.y < roadBounds.min.z - 0.08f ||
                        point.y > roadBounds.max.z + 0.08f)
                    {
                        continue;
                    }

                    float distance = Mathf.Lerp(
                        cumulative[segmentIndex], cumulative[segmentIndex + 1], t);
                    bool isAtBridge = bridgeDistances.Any(bridgeDistance =>
                        Mathf.Abs(distance - bridgeDistance) <
                        MainMapRiverLayout.BridgeGapHalfLength + 0.15f);
                    if (!isAtBridge)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<Vector2> SubtractInterval(List<Vector2> source, float cutStart, float cutEnd)
        {
            List<Vector2> result = new List<Vector2>();
            foreach (Vector2 interval in source)
            {
                if (cutEnd <= interval.x || cutStart >= interval.y)
                {
                    result.Add(interval);
                    continue;
                }

                if (cutStart > interval.x)
                {
                    result.Add(new Vector2(interval.x, Mathf.Min(cutStart, interval.y)));
                }
                if (cutEnd < interval.y)
                {
                    result.Add(new Vector2(Mathf.Max(cutEnd, interval.x), interval.y));
                }
            }
            return result;
        }

        private static void CreateRiverBarrierPiece(Transform parent, Vector2 start, Vector2 end, float halfWidth)
        {
            Vector2 direction2D = end - start;
            float length = direction2D.magnitude;
            if (length <= 0.02f)
            {
                return;
            }

            GameObject barrier = new GameObject("Impassable River Section");
            barrier.transform.SetParent(parent);
            barrier.transform.position = new Vector3(
                (start.x + end.x) * 0.5f,
                0f,
                (start.y + end.y) * 0.5f);
            barrier.transform.rotation = Quaternion.LookRotation(
                new Vector3(direction2D.x, 0f, direction2D.y).normalized,
                Vector3.up);
            BoxCollider collider = barrier.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.55f, 0f);
            collider.size = new Vector3(
                (halfWidth + MainMapRiverLayout.BarrierBankPadding) * 2f,
                1.1f,
                length + 0.12f);
        }

        private static void RelocateRiverConflicts()
        {
            foreach (EncounterTrigger encounter in UnityEngine.Object.FindObjectsByType<EncounterTrigger>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!MainMapRiverLayout.IsInsideRiver(encounter.transform.position, 1.1f))
                {
                    continue;
                }

                encounter.transform.position = MainMapRiverLayout.GetNearestSafeBankPosition(
                    encounter.transform.position,
                    1.35f);
                EditorUtility.SetDirty(encounter.transform);
            }

            string[] movableScenery =
            {
                "Tree B1",
                "Tree A2",
                "Rock Cluster",
                "Tree C3",
                "West Forest Tree C",
                "West Forest Tree D"
            };
            foreach (string objectName in movableScenery)
            {
                GameObject scenery = GameObject.Find(objectName);
                if (scenery == null || !MainMapRiverLayout.IsInsideRiver(scenery.transform.position, 0.65f))
                {
                    continue;
                }

                scenery.transform.position = MainMapRiverLayout.GetNearestSafeBankPosition(
                    scenery.transform.position,
                    1.15f);
                EditorUtility.SetDirty(scenery.transform);
            }

            GameObject cave = GameObject.Find("断崖石窟");
            GameObject caveModel = GameObject.Find("Cliff Cave Entrance");
            if (cave != null && caveModel != null)
            {
                Vector3 modelPosition = cave.transform.position + new Vector3(-1.2f, 0f, -0.15f);
                Bounds bounds = CalculateRendererBounds(caveModel);
                caveModel.transform.position = modelPosition + Vector3.up * (modelPosition.y - bounds.min.y);
                EditorUtility.SetDirty(caveModel.transform);
            }
        }

        private static void DestroySceneObjectIfPresent(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static bool IsInsideAnyBarrier(Vector3 point, IEnumerable<BoxCollider> barriers)
        {
            return barriers.Any(collider =>
                collider != null &&
                (collider.ClosestPoint(point) - point).sqrMagnitude < 0.0001f);
        }

        private static void BuildHd2dScenicLayers(Transform parent)
        {
            GameObject scenicRoot = new GameObject("Pixel Scenic Cutouts");
            scenicRoot.transform.SetParent(parent);
            Sprite bamboo = AssetDatabase.LoadAssetAtPath<Sprite>(Hd2dBambooPath);
            Sprite pine = AssetDatabase.LoadAssetAtPath<Sprite>(Hd2dPineRockPath);

            CreateHd2dBillboard("West Bamboo Grove", scenicRoot.transform, bamboo, new Vector3(-27.2f, 0f, -6f), 0.72f, new Color(0.83f, 0.88f, 0.78f, 0.92f), 2);
            CreateHd2dBillboard("East Bamboo Grove", scenicRoot.transform, bamboo, new Vector3(27f, 0f, 11.5f), 0.58f, new Color(0.88f, 0.9f, 0.80f, 0.86f), 2);
            CreateHd2dBillboard("Northwest Old Pine", scenicRoot.transform, pine, new Vector3(-24.2f, 0f, 24.8f), 0.9f, new Color(0.82f, 0.88f, 0.83f, 0.94f), 1);
            CreateHd2dBillboard("Northeast Old Pine", scenicRoot.transform, pine, new Vector3(23.5f, 0f, 25.2f), 0.78f, new Color(0.86f, 0.9f, 0.84f, 0.90f), 1, true);

            GameObject ridgeProps = new GameObject("Diorama Ridge Props");
            ridgeProps.transform.SetParent(parent);
            PlaceModel("detail_treeA", "North Ridge Silhouette Tree A", ridgeProps.transform, new Vector3(-15f, 0f, 28.8f), 2.8f, 25f);
            PlaceModel("detail_treeB", "North Ridge Silhouette Tree B", ridgeProps.transform, new Vector3(-6f, 0f, 29.2f), 2.6f, -20f);
            PlaceModel("detail_treeC", "North Ridge Silhouette Tree C", ridgeProps.transform, new Vector3(9f, 0f, 29f), 2.7f, 80f);
            PlaceModel("detail_rocks", "North Ridge Silhouette Rocks A", ridgeProps.transform, new Vector3(-21f, 0f, 29.1f), 2.2f, 40f);
            PlaceModel("detail_rocks", "North Ridge Silhouette Rocks B", ridgeProps.transform, new Vector3(18f, 0f, 29.2f), 2.4f, 110f);
        }

        private static void BuildHd2dAtmosphere(Transform parent)
        {
            GameObject atmosphere = new GameObject("Atmosphere Layers");
            atmosphere.transform.SetParent(parent);
            Sprite mist = AssetDatabase.LoadAssetAtPath<Sprite>(Hd2dMistPath);
            Sprite beam = AssetDatabase.LoadAssetAtPath<Sprite>(Hd2dLightBeamPath);

            GameObject boundaryMist = new GameObject("Boundary Mist Ring");
            boundaryMist.transform.SetParent(atmosphere.transform);

            // The mist now overlaps the forest belt instead of sitting far beyond it.
            // Trees establish a readable physical boundary; the mist breaks up their
            // repeated silhouettes and blends the belt into the panorama.
            CreateHd2dBillboard("North Boundary Mist", boundaryMist.transform, mist, new Vector3(0f, 3.2f, 31.5f), 2.4f, new Color(0.78f, 0.86f, 0.84f, 0.42f), -100, false, true);
            CreateHd2dBillboard("South Boundary Mist", boundaryMist.transform, mist, new Vector3(0f, 3.0f, -31.5f), 2.4f, new Color(0.76f, 0.84f, 0.82f, 0.40f), -100, true, true);
            CreateHd2dBillboard("West Boundary Mist", boundaryMist.transform, mist, new Vector3(-35.5f, 3.1f, 0f), 2.2f, new Color(0.76f, 0.84f, 0.81f, 0.38f), -100, false, true);
            CreateHd2dBillboard("East Boundary Mist", boundaryMist.transform, mist, new Vector3(35.5f, 3.1f, 0f), 2.2f, new Color(0.78f, 0.86f, 0.83f, 0.38f), -100, true, true);

            CreateHd2dBillboard("Far Mist Band", atmosphere.transform, mist, new Vector3(0f, 2.4f, 35f), 2.35f, new Color(0.83f, 0.9f, 0.88f, 0.24f), -80, false, true);
            CreateHd2dBillboard("West Mist Band", atmosphere.transform, mist, new Vector3(-22f, 1.2f, 18f), 1.45f, new Color(0.78f, 0.86f, 0.84f, 0.16f), 0, false, true);
            CreateHd2dBillboard("East Mist Band", atmosphere.transform, mist, new Vector3(22f, 1.5f, 22f), 1.35f, new Color(0.80f, 0.88f, 0.86f, 0.14f), 0, true, true);

            CreateHd2dBillboard("North Gate Light Shaft", atmosphere.transform, beam, new Vector3(0f, 0f, 18.5f), 1.75f, new Color(1f, 0.86f, 0.56f, 0.34f), 3);
            CreateHd2dBillboard("East Hamlet Light Shaft", atmosphere.transform, beam, new Vector3(18f, 0f, 12f), 1.5f, new Color(1f, 0.78f, 0.45f, 0.26f), 3, true);
        }

        private static void ApplyHd2dWorldLighting(Transform parent)
        {
            ApplyUnifiedWorldLighting();
            Material panoramaSky = GetOrCreateHd2dPanoramaSkyMaterial();
            if (panoramaSky != null)
            {
                RenderSettings.skybox = panoramaSky;
                DynamicGI.UpdateEnvironment();
            }

            Light sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.type == LightType.Directional);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.86f, 0.68f, 1f);
                sun.intensity = 1.15f;
                sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
                sun.shadowStrength = 0.68f;
                EditorUtility.SetDirty(sun);
            }

            RenderSettings.ambientSkyColor = new Color(0.42f, 0.52f, 0.56f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.34f, 0.31f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.10f, 0.075f, 1f);
            RenderSettings.ambientIntensity = 0.86f;
            RenderSettings.reflectionIntensity = 0.25f;
            RenderSettings.fogColor = new Color(0.55f, 0.62f, 0.60f, 1f);
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 64f;

            CreateWarmLandmarkLight("North Gate Warm Light", parent, new Vector3(0f, 3.2f, 13.5f), 1.35f, 8f);
            CreateWarmLandmarkLight("East Hamlet Warm Light", parent, new Vector3(18f, 2.8f, 7.5f), 1.15f, 7f);
            CreateWarmLandmarkLight("West Caravan Warm Light", parent, new Vector3(-18.5f, 2.5f, -15f), 0.95f, 6.5f);

            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.clearFlags = panoramaSky != null
                    ? CameraClearFlags.Skybox
                    : CameraClearFlags.SolidColor;
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 140f);
                camera.allowHDR = true;
                camera.backgroundColor = new Color(0.62f, 0.69f, 0.67f, 1f);
                CameraFollow follow = camera.GetComponent<CameraFollow>();
                if (follow != null)
                {
                    follow.offset = new Vector3(10f, 10.8f, -20f);
                    follow.portraitOffset = CameraFollow.DefaultPortraitOffset;
                    follow.lookAtHeight = 0.72f;
                    follow.landscapeFieldOfView = 36.5f;
                    follow.portraitFieldOfView = CameraFollow.DefaultPortraitFieldOfView;
                    EditorUtility.SetDirty(follow);
                }
                EditorUtility.SetDirty(camera);
            }
        }

        private static Material GetOrCreateHd2dPanoramaSkyMaterial()
        {
            Shader shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                Debug.LogWarning("Cannot find the built-in Skybox/Panoramic shader.");
                return null;
            }

            Texture2D panorama = AssetDatabase.LoadAssetAtPath<Texture2D>(Hd2dPanoramaPath);
            if (panorama == null)
            {
                Debug.LogWarning($"Missing HD-2D panorama texture: {Hd2dPanoramaPath}");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(Hd2dPanoramaMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "HD2D_MountainPanoramaSky" };
                AssetDatabase.CreateAsset(material, Hd2dPanoramaMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", panorama);
            material.SetColor("_Tint", new Color(0.72f, 0.78f, 0.76f, 1f));
            material.SetFloat("_Exposure", 0.55f);
            material.SetFloat("_Rotation", 0f);
            material.SetFloat("_Mapping", 1f);
            material.SetFloat("_ImageType", 0f);
            material.SetFloat("_MirrorOnBack", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateHd2dWaterMaterial()
        {
            Shader shader = Shader.Find(Hd2dWaterShaderName) ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(Hd2dWaterMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "HD2D_CeladonWater" };
                AssetDatabase.CreateAsset(material, Hd2dWaterMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Hd2dWaterTexturePath);
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", new Color(0.68f, 0.80f, 0.79f, 0.86f));
            if (material.HasProperty("_WorldTiling")) material.SetFloat("_WorldTiling", 0.14f);
            if (material.HasProperty("_FlowSpeed")) material.SetFloat("_FlowSpeed", 0.025f);
            if (material.HasProperty("_Alpha")) material.SetFloat("_Alpha", 0.88f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateHd2dBillboard(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 position,
            float scale,
            Color color,
            int sortingOrder,
            bool flipX = false,
            bool centerPivot = false)
        {
            GameObject billboard = new GameObject(name);
            billboard.transform.SetParent(parent);
            billboard.transform.position = position;
            billboard.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = billboard.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.flipX = flipX;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            BillboardSprite billboardController = billboard.AddComponent<BillboardSprite>();
            billboardController.alignment = BillboardAlignment.CameraPlane;

            if (sprite != null && !centerPivot)
            {
                billboard.transform.position += Vector3.up * sprite.bounds.extents.y * scale;
            }

            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                billboard.transform.rotation = camera.transform.rotation;
            }
            return billboard;
        }

        private static void CreateDecorativeTerrace(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject terrace = CreateCube(name, parent, position, scale, material);
            Collider collider = terrace.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void CreateStreamSegment(string name, Transform parent, Vector3 position, Vector3 scale, float rotation, Material material)
        {
            GameObject segment = CreateCube(name, parent, position, scale, material);
            segment.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            Collider collider = segment.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void CreateStreamRibbon(
            string name,
            Transform parent,
            Vector2[] centerLine,
            float[] halfWidths,
            float height,
            float widthPadding,
            Material material)
        {
            if (centerLine == null || halfWidths == null || centerLine.Length < 2 || centerLine.Length != halfWidths.Length)
            {
                Debug.LogError($"Cannot build stream ribbon {name}: invalid center line or widths.");
                return;
            }

            int pointCount = centerLine.Length;
            Vector3[] vertices = new Vector3[pointCount * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[(pointCount - 1) * 6];
            float distance = 0f;

            for (int i = 0; i < pointCount; i++)
            {
                Vector2 previous = centerLine[Mathf.Max(0, i - 1)];
                Vector2 next = centerLine[Mathf.Min(pointCount - 1, i + 1)];
                Vector2 tangent = (next - previous).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                float width = halfWidths[i] + widthPadding;
                if (i > 0) distance += Vector2.Distance(centerLine[i - 1], centerLine[i]);

                Vector2 left = centerLine[i] + normal * width;
                Vector2 right = centerLine[i] - normal * width;
                vertices[i * 2] = new Vector3(left.x, height, left.y);
                vertices[i * 2 + 1] = new Vector3(right.x, height, right.y);
                uvs[i * 2] = new Vector2(0f, distance * 0.15f);
                uvs[i * 2 + 1] = new Vector2(1f, distance * 0.15f);

                if (i >= pointCount - 1) continue;
                int triangle = i * 6;
                int vertex = i * 2;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 2;
                triangles[triangle + 2] = vertex + 1;
                triangles[triangle + 3] = vertex + 1;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            Mesh mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject ribbon = new GameObject(name);
            ribbon.transform.SetParent(parent);
            ribbon.AddComponent<MeshFilter>().sharedMesh = mesh;
            ribbon.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateWarmLandmarkLight(string name, Transform parent, Vector3 position, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.64f, 0.30f, 1f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static Material GetOrCreateActorGroundShadowMaterial()
        {
            Shader shader = Shader.Find(ActorGroundShadowShaderName);
            if (shader == null)
            {
                Debug.LogError($"Cannot find actor ground-shadow shader: {ActorGroundShadowShaderName}");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(ActorGroundShadowMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "HD2D_ActorGroundShadow" };
                AssetDatabase.CreateAsset(material, ActorGroundShadowMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_Color", new Color(0.08f, 0.12f, 0.10f, 0.34f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateScenicSpriteMaterial()
        {
            Shader shader = Shader.Find(StylizedScenicSpriteShaderName);
            if (shader == null)
            {
                Debug.LogError($"Cannot find scenic sprite shader: {StylizedScenicSpriteShaderName}");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(ScenicSpriteMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "HD2D_SoftScenicSprite" };
                AssetDatabase.CreateAsset(material, ScenicSpriteMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_Color", Color.white);
            material.SetFloat("_Saturation", 0.72f);
            material.SetFloat("_Contrast", 0.82f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyStylizedMapMaterials(Transform mapRoot, bool logResult = true)
        {
            int updatedRendererCount = 0;
            foreach (MeshRenderer renderer in mapRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material stylized = GetOrCreateStylizedPropMaterial(materials[i]);
                    if (stylized == null || stylized == materials[i])
                    {
                        continue;
                    }

                    materials[i] = stylized;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                EditorUtility.SetDirty(renderer);
                updatedRendererCount++;
            }

            if (logResult)
            {
                Debug.Log($"Applied matte HD-2D prop materials to {updatedRendererCount} mesh renderers.");
            }
        }

        private static Material GetOrCreateStylizedPropMaterial(Material source)
        {
            if (source == null || source.shader == null)
            {
                return source;
            }

            string sourceShaderName = source.shader.name;
            if (sourceShaderName == StylizedPropShaderName ||
                sourceShaderName == WorldSurfaceShaderName ||
                sourceShaderName == Hd2dWaterShaderName ||
                sourceShaderName == ActorGroundShadowShaderName ||
                sourceShaderName.StartsWith("Skybox/", StringComparison.Ordinal))
            {
                return source;
            }

            Shader shader = Shader.Find(StylizedPropShaderName);
            if (shader == null)
            {
                Debug.LogError($"Cannot find stylized prop shader: {StylizedPropShaderName}");
                return source;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string sourceAssetName = string.IsNullOrEmpty(sourcePath)
                ? "generated"
                : Path.GetFileNameWithoutExtension(sourcePath);
            bool isQuaterniusMaterial = sourcePath.StartsWith(
                QuaterniusVillageModelRoot,
                StringComparison.OrdinalIgnoreCase);
            string safeName = isQuaterniusMaterial
                ? SanitizeAssetName($"Quaternius_{source.name}")
                : SanitizeAssetName($"{sourceAssetName}_{source.name}");
            string materialPath = $"{PropMaterialRoot}/{safeName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = $"HD2D_{safeName}" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            Texture texture = source.HasProperty("_MainTex")
                ? source.GetTexture("_MainTex")
                : source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : null;
            Color color = source.HasProperty("_Color")
                ? source.GetColor("_Color")
                : source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
            color.a = 1f;
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", color);
            material.SetFloat("_Saturation", isQuaterniusMaterial ? 0.82f : 0.72f);
            material.SetFloat("_Contrast", isQuaterniusMaterial ? 0.96f : 0.88f);
            if (material.HasProperty("_BumpMap") && source.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                material.SetFloat("_BumpScale", isQuaterniusMaterial ? 0.72f : 0.45f);
            }
            if (material.HasProperty("_Smoothness"))
            {
                float sourceSmoothness = source.HasProperty("_Glossiness")
                    ? source.GetFloat("_Glossiness")
                    : 0.18f;
                material.SetFloat("_Smoothness", Mathf.Clamp(sourceSmoothness, 0.08f, 0.32f));
            }
            if (source.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", source.GetTextureScale("_MainTex"));
                material.SetTextureOffset("_MainTex", source.GetTextureOffset("_MainTex"));
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string SanitizeAssetName(string value)
        {
            char[] characters = value
                .Select(character => char.IsLetterOrDigit(character) || character == '_' || character == '-'
                    ? character
                    : '_')
                .ToArray();
            string result = new string(characters).Trim('_');
            return string.IsNullOrEmpty(result) ? "material" : result;
        }

        private static void ConfigureActorGroundShadows(Material shadowMaterial)
        {
            if (shadowMaterial == null)
            {
                return;
            }

            int actorCount = 0;
            foreach (SpriteFrameAnimator animator in UnityEngine.Object.FindObjectsByType<SpriteFrameAnimator>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform actorTransform = animator.transform.parent;
                if (actorTransform == null)
                {
                    continue;
                }

                BillboardSprite billboard = animator.GetComponent<BillboardSprite>();
                if (billboard != null)
                {
                    billboard.alignment = BillboardAlignment.CameraPlane;
                    EditorUtility.SetDirty(billboard);
                }

                ActorGroundShadow shadow = actorTransform.GetComponent<ActorGroundShadow>();
                if (shadow == null)
                {
                    shadow = actorTransform.gameObject.AddComponent<ActorGroundShadow>();
                }

                shadow.visualRoot = animator.transform;
                shadow.shadowMaterial = shadowMaterial;
                shadow.baseSize = actorTransform.name == "Player"
                    ? new Vector2(0.58f, 0.25f)
                    : new Vector2(0.52f, 0.23f);
                shadow.opacity = actorTransform.name == "Player" ? 0.38f : 0.32f;
                EditorUtility.SetDirty(shadow);
                actorCount++;
            }

            Debug.Log($"Configured camera-plane billboards and contact shadows for {actorCount} map actors.");
        }

        private static void ConfigureScenicSpriteMaterials(Transform mapRoot, Material scenicMaterial)
        {
            if (scenicMaterial == null)
            {
                return;
            }

            Transform hd2dRoot = mapRoot.Find("HD2D Main World Art");
            if (hd2dRoot == null)
            {
                return;
            }

            int spriteCount = 0;
            foreach (SpriteRenderer renderer in hd2dRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!HasAncestorNamed(renderer.transform, "Boundary Forest Belt", hd2dRoot) &&
                    !HasAncestorNamed(renderer.transform, "Pixel Scenic Cutouts", hd2dRoot))
                {
                    continue;
                }

                renderer.sharedMaterial = scenicMaterial;
                Color color = renderer.color;
                color.a = Mathf.Min(color.a, 0.86f);
                renderer.color = color;
                BillboardSprite billboard = renderer.GetComponent<BillboardSprite>();
                if (billboard != null)
                {
                    billboard.alignment = BillboardAlignment.CameraPlane;
                    EditorUtility.SetDirty(billboard);
                }
                EditorUtility.SetDirty(renderer);
                spriteCount++;
            }

            Debug.Log($"Applied low-contrast scenic treatment to {spriteCount} HD-2D sprite cutouts.");
        }

        private static bool HasAncestorNamed(Transform target, string ancestorName, Transform stopAt)
        {
            Transform current = target;
            while (current != null && current != stopAt)
            {
                if (current.name == ancestorName)
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        private static void ConfigureResponsiveHd2dCamera()
        {
            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            CameraFollow follow = camera.GetComponent<CameraFollow>();
            if (follow == null)
            {
                return;
            }

            follow.offset = new Vector3(10f, 10.8f, -20f);
            follow.portraitOffset = CameraFollow.DefaultPortraitOffset;
            follow.lookAtHeight = 0.72f;
            follow.landscapeFieldOfView = 36.5f;
            follow.portraitFieldOfView = CameraFollow.DefaultPortraitFieldOfView;
            camera.fieldOfView = follow.landscapeFieldOfView;

            if (follow.target != null)
            {
                float previewVisionScale = Mathf.Clamp(
                    follow.initialVisionScale,
                    0.4f,
                    follow.maximumVisionScale);
                camera.transform.position = follow.target.position + follow.offset * previewVisionScale;
                Vector3 lookTarget = follow.target.position + Vector3.up * follow.lookAtHeight;
                camera.transform.rotation = Quaternion.LookRotation(
                    lookTarget - camera.transform.position,
                    Vector3.up);
            }

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(follow);
        }

        private static void ApplyAdvanced3dEnvironmentPassInternal(Transform mapRoot)
        {
            Transform previous = mapRoot.Find("Advanced 3D Environment Pass");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject passRoot = new GameObject("Advanced 3D Environment Pass");
            passRoot.transform.SetParent(mapRoot, false);
            Material road = GetOrCreateMainMapRoadMaterial();

            BuildAdvancedArrivalGate(passRoot.transform, road);
            BuildAdvancedEastCourierInn(passRoot.transform, road);
            BuildAdvancedWestAmbush(passRoot.transform, road);
            BuildAdvancedNorthWatch(passRoot.transform, road);
            BuildAdvancedSouthMine(passRoot.transform, road);
            BuildAdvancedGroundPolish(passRoot.transform, road);
            ApplyAdvancedGameplayReadableScale(passRoot.transform);

            Light sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.type == LightType.Directional);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.80f, 0.61f, 1f);
                sun.intensity = 1.26f;
                sun.shadowStrength = 0.76f;
                sun.shadowBias = 0.035f;
                sun.shadowNormalBias = 0.38f;
                sun.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
                EditorUtility.SetDirty(sun);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.56f, 0.62f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.36f, 0.35f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.16f, 0.13f, 1f);
            RenderSettings.ambientIntensity = 0.82f;
            RenderSettings.reflectionIntensity = 0.32f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.47f, 0.55f, 0.56f, 1f);
            RenderSettings.fogStartDistance = 29f;
            RenderSettings.fogEndDistance = 70f;
            ConfigureResponsiveHd2dCamera();
        }

        private static void ApplyAdvancedGameplayReadableScale(Transform passRoot)
        {
            // These clusters are viewed through a close portrait gameplay camera.
            // Keep their landmark silhouettes, but reduce their screen occupancy so
            // walls and tiled roofs cannot hide the player or nearby encounters.
            SetAdvancedClusterScale(passRoot, "Sparse Arrival Cluster - Fortified Courier Gate", 0.76f);
            SetAdvancedClusterScale(passRoot, "Medium East Cluster - Abandoned Two Storey Courier Inn", 0.64f);
            SetAdvancedClusterScale(passRoot, "Dense West Cluster - Caravan Ambush Aftermath", 0.80f);
            SetAdvancedClusterScale(passRoot, "Sparse North Ridge Cluster - Silent Watch House", 0.68f);
            SetAdvancedClusterScale(passRoot, "Medium Dense South Cluster - Freshly Abandoned Mine Works", 0.70f);
        }

        private static void SetAdvancedClusterScale(Transform passRoot, string clusterName, float uniformScale)
        {
            Transform cluster = passRoot != null ? passRoot.Find(clusterName) : null;
            if (cluster == null)
            {
                return;
            }

            cluster.localScale = Vector3.one * uniformScale;
            EditorUtility.SetDirty(cluster);
        }

        private static void BuildAdvancedArrivalGate(Transform parent, Material road)
        {
            GameObject cluster = CreateAdvancedCluster(
                "Sparse Arrival Cluster - Fortified Courier Gate",
                parent,
                new Vector3(0f, 0f, 16.3f),
                0f);

            PlaceQuaterniusModel("Wall_Arch", "Courier Gate Arch", cluster.transform,
                new Vector3(0f, 0f, 0f), 3.6f, 0f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", "Gate Wall West Inner", cluster.transform,
                new Vector3(-2.8f, 0f, 0f), 2.2f, 0f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", "Gate Wall West Outer", cluster.transform,
                new Vector3(-5.0f, 0f, 0.15f), 2.2f, -8f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", "Gate Wall East Inner", cluster.transform,
                new Vector3(2.8f, 0f, 0f), 2.2f, 0f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", "Gate Wall East Outer", cluster.transform,
                new Vector3(5.0f, 0f, 0.15f), 2.2f, 8f);
            PlaceQuaterniusModel("Roof_Front_Brick4", "Gate Rain Hood", cluster.transform,
                new Vector3(0f, 3.05f, 0f), 4.5f, 0f);
            PlaceQuaterniusModel("Prop_MetalFence_Ornament", "Gate Crest", cluster.transform,
                new Vector3(0f, 5.55f, -0.05f), 1.3f, 0f);
            PlaceQuaterniusModel("Prop_Brick1", "Gate Fallen Masonry A", cluster.transform,
                new Vector3(-4.3f, 0f, -1.0f), 0.70f, 24f);
            PlaceQuaterniusModel("Prop_Brick3", "Gate Fallen Masonry B", cluster.transform,
                new Vector3(-3.7f, 0f, -1.35f), 0.46f, 74f);
            PlaceQuaterniusModel("Prop_Brick4", "Gate Fallen Masonry C", cluster.transform,
                new Vector3(4.15f, 0f, -1.1f), 0.52f, 132f);
            CreateGroundOval("Gate Threshold Wear", cluster.transform,
                new Vector3(0f, 0.012f, -0.3f), new Vector2(3.5f, 1.05f), 0f, road,
                new Color(0.65f, 0.57f, 0.44f, 1f));
            CreateLanternPost("Gate Guidance Lantern West", cluster.transform,
                new Vector3(-2.15f, 0f, -1.25f), 20f, 0.95f);
            CreateLanternPost("Gate Guidance Lantern East", cluster.transform,
                new Vector3(2.15f, 0f, -1.25f), 160f, 0.95f);
        }

        private static void BuildAdvancedEastCourierInn(Transform parent, Material road)
        {
            GameObject cluster = CreateAdvancedCluster(
                "Medium East Cluster - Abandoned Two Storey Courier Inn",
                parent,
                new Vector3(18.2f, 0f, 9.0f),
                -72f);

            BuildAdvancedHouse("Courier Inn", cluster.transform, Vector3.zero, 0f, true);
            PlaceQuaterniusModel("Prop_Wagon", "Courier Wagon Left In Haste", cluster.transform,
                new Vector3(-5.7f, 0f, -2.3f), 3.3f, -28f);
            PlaceQuaterniusModel("Prop_Crate", "Uncollected Cargo A", cluster.transform,
                new Vector3(-4.5f, 0f, -3.8f), 0.95f, 17f);
            PlaceQuaterniusModel("Prop_Crate", "Uncollected Cargo B", cluster.transform,
                new Vector3(-3.6f, 0f, -4.1f), 0.72f, 48f);
            PlaceQuaterniusModel("Prop_WoodenFence_Single", "Courier Yard Fence A", cluster.transform,
                new Vector3(4.4f, 0f, 1.9f), 2.1f, 8f);
            PlaceQuaterniusModel("Prop_WoodenFence_Extension1", "Courier Yard Fence B", cluster.transform,
                new Vector3(5.8f, 0f, 2.7f), 1.85f, 56f);
            PlaceQuaterniusModel("Prop_Brick2", "Courier Yard Debris A", cluster.transform,
                new Vector3(4.0f, 0f, -3.0f), 0.45f, 15f);
            PlaceQuaterniusModel("Prop_Brick4", "Courier Yard Debris B", cluster.transform,
                new Vector3(4.6f, 0f, -3.4f), 0.34f, 96f);
            CreateGroundOval("Courier Wagon Turn", cluster.transform,
                new Vector3(-3.8f, 0.011f, -2.4f), new Vector2(3.2f, 1.2f), -18f, road,
                new Color(0.63f, 0.55f, 0.42f, 1f));
            CreateWarmLandmarkLight("Courier Inn Window Glow", cluster.transform,
                new Vector3(0f, 3.8f, -3.0f), 1.2f, 7.2f);
        }

        private static void BuildAdvancedWestAmbush(Transform parent, Material road)
        {
            GameObject cluster = CreateAdvancedCluster(
                "Dense West Cluster - Caravan Ambush Aftermath",
                parent,
                new Vector3(-21.8f, 0f, -10.8f),
                18f);

            PlaceQuaterniusModel("Prop_Wagon", "Overturned Supply Wagon", cluster.transform,
                new Vector3(0f, 0f, 0f), 3.8f, 66f, new Vector3(0f, 0f, 18f));
            PlaceQuaterniusModel("Prop_WoodenFence_Single", "Broken Barricade A", cluster.transform,
                new Vector3(-3.2f, 0f, 1.1f), 2.25f, 22f, new Vector3(0f, 0f, 11f));
            PlaceQuaterniusModel("Prop_WoodenFence_Extension2", "Broken Barricade B", cluster.transform,
                new Vector3(-1.9f, 0f, 2.4f), 1.95f, 83f, new Vector3(0f, 0f, -15f));
            PlaceQuaterniusModel("Prop_Crate", "Scattered Supply Crate A", cluster.transform,
                new Vector3(2.8f, 0f, -1.2f), 0.96f, 24f, new Vector3(8f, 0f, 12f));
            PlaceQuaterniusModel("Prop_Crate", "Scattered Supply Crate B", cluster.transform,
                new Vector3(3.5f, 0f, -0.4f), 0.73f, 71f, new Vector3(0f, 0f, -8f));
            PlaceQuaterniusModel("Prop_Brick1", "Ambush Debris A", cluster.transform,
                new Vector3(2.0f, 0f, 1.9f), 0.42f, 34f);
            PlaceQuaterniusModel("Prop_Brick3", "Ambush Debris B", cluster.transform,
                new Vector3(2.6f, 0f, 2.15f), 0.34f, 112f);
            PlaceDressingModel("detail_treeA", "Ambush Canopy A", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(-4.6f, 0f, 3.1f)), 2.6f, 35f);
            PlaceDressingModel("detail_treeC", "Ambush Canopy B", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(4.5f, 0f, 2.7f)), 2.3f, 138f);
            PlaceDressingModel("detail_rocks", "Ambush Cover Rock", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(-4.3f, 0f, -2.5f)), 1.75f, 78f);
            PlaceDressingModel("detail_rocks_small", "Ambush Scree", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(3.7f, 0f, 2.1f)), 1.05f, 22f);
            CreateGroundOval("Ambush Wheel Rut", cluster.transform,
                new Vector3(0f, 0.011f, -0.2f), new Vector2(3.4f, 1.0f), 58f, road,
                new Color(0.53f, 0.47f, 0.36f, 1f));
        }

        private static void BuildAdvancedNorthWatch(Transform parent, Material road)
        {
            GameObject cluster = CreateAdvancedCluster(
                "Sparse North Ridge Cluster - Silent Watch House",
                parent,
                new Vector3(-18.2f, 0f, 21.0f),
                12f);

            BuildAdvancedHouse("Silent Watch House", cluster.transform, Vector3.zero, 0f, false);
            PlaceQuaterniusModel("Stairs_Exterior_Straight", "Watch House Steps", cluster.transform,
                new Vector3(0f, 0f, -3.05f), 2.05f, 0f);
            PlaceQuaterniusModel("Prop_WoodenFence_Single", "Watch Ridge Fence A", cluster.transform,
                new Vector3(-4.0f, 0f, 1.9f), 2.1f, 12f);
            PlaceQuaterniusModel("Prop_WoodenFence_Extension1", "Watch Ridge Fence B", cluster.transform,
                new Vector3(-5.2f, 0f, 2.6f), 1.9f, 55f);
            PlaceQuaterniusModel("Prop_Crate", "Abandoned Signal Supplies", cluster.transform,
                new Vector3(3.8f, 0f, -1.7f), 0.84f, 14f);
            PlaceDressingModel("detail_rocks", "Watch Ridge Wind Rock", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(4.6f, 0f, 2.4f)), 1.6f, 44f);
            CreateGroundOval("Cold Watchfire Ash", cluster.transform,
                new Vector3(3.4f, 0.011f, -2.4f), new Vector2(1.0f, 0.75f), 15f, road,
                new Color(0.37f, 0.37f, 0.34f, 1f));
        }

        private static void BuildAdvancedSouthMine(Transform parent, Material road)
        {
            GameObject cluster = CreateAdvancedCluster(
                "Medium Dense South Cluster - Freshly Abandoned Mine Works",
                parent,
                new Vector3(18.7f, 0f, -17.8f),
                34f);

            BuildAdvancedWorkshop("Mine Ore House", cluster.transform, new Vector3(0f, 0f, 0f), 0f);
            PlaceQuaterniusModel("Prop_Wagon", "Ore Wagon Ready To Flee", cluster.transform,
                new Vector3(-5.2f, 0f, -0.8f), 3.1f, 32f);
            PlaceQuaterniusModel("Prop_Crate", "Mine Supply Crate A", cluster.transform,
                new Vector3(-3.3f, 0f, 2.2f), 0.92f, 16f);
            PlaceQuaterniusModel("Prop_Crate", "Mine Supply Crate B", cluster.transform,
                new Vector3(-2.4f, 0f, 2.5f), 0.70f, 61f);
            PlaceQuaterniusModel("Prop_WoodenFence_Single", "Rockfall Warning Fence A", cluster.transform,
                new Vector3(4.0f, 0f, -1.4f), 2.2f, -6f);
            PlaceQuaterniusModel("Prop_WoodenFence_Single", "Rockfall Warning Fence B", cluster.transform,
                new Vector3(5.8f, 0f, -1.0f), 2.0f, 20f);
            PlaceDressingModel("detail_rocks", "Fresh Mine Rockfall A", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(4.8f, 0f, 2.4f)), 2.05f, 34f);
            PlaceDressingModel("detail_rocks", "Fresh Mine Rockfall B", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(6.3f, 0f, 2.0f)), 1.45f, 118f);
            PlaceDressingModel("detail_rocks_small", "Mine Scree A", cluster.transform,
                cluster.transform.TransformPoint(new Vector3(3.7f, 0f, 3.2f)), 1.05f, 86f);
            PlaceQuaterniusModel("Prop_Brick2", "Mine Broken Masonry A", cluster.transform,
                new Vector3(2.8f, 0f, -2.6f), 0.52f, 18f);
            PlaceQuaterniusModel("Prop_Brick4", "Mine Broken Masonry B", cluster.transform,
                new Vector3(3.4f, 0f, -2.9f), 0.42f, 93f);
            CreateGroundOval("Mine Ore Dust Contact", cluster.transform,
                new Vector3(0.3f, 0.011f, -0.6f), new Vector2(4.0f, 1.7f), -12f, road,
                new Color(0.57f, 0.48f, 0.37f, 1f));
            CreateLanternPost("Mine Emergency Lantern", cluster.transform,
                new Vector3(-2.9f, 0f, -2.7f), 15f, 1.05f);
            CreateWarmLandmarkLight("Mine Furnace Glow", cluster.transform,
                new Vector3(0f, 2.2f, -2.4f), 1.25f, 6.5f);
        }

        private static GameObject CreateAdvancedCluster(
            string name,
            Transform parent,
            Vector3 position,
            float yRotation)
        {
            GameObject cluster = new GameObject(name);
            cluster.transform.SetParent(parent, false);
            cluster.transform.position = position;
            cluster.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            return cluster;
        }

        private static void BuildAdvancedHouse(
            string name,
            Transform parent,
            Vector3 localPosition,
            float localRotation,
            bool twoStoreys)
        {
            GameObject house = CreateAdvancedCluster(name, parent,
                parent.TransformPoint(localPosition), parent.eulerAngles.y + localRotation);
            int storeys = twoStoreys ? 2 : 1;
            for (int floor = 0; floor < storeys; floor++)
            {
                float y = floor * 3.10f;
                string frontCenter = floor == 0 ? "Wall_Plaster_Door_RoundInset" : "Wall_Plaster_Window_Wide_Round";
                string sideModel = floor == 0 ? "Wall_Plaster_Straight_Base" : "Wall_Plaster_Window_Thin_Round";
                PlaceQuaterniusModel(sideModel, name + " Front Left " + floor, house.transform,
                    new Vector3(-2f, y, -2.05f), 2.05f, 0f);
                PlaceQuaterniusModel(frontCenter, name + " Front Center " + floor, house.transform,
                    new Vector3(0f, y, -2.05f), 2.05f, 0f);
                PlaceQuaterniusModel(sideModel, name + " Front Right " + floor, house.transform,
                    new Vector3(2f, y, -2.05f), 2.05f, 0f);
                PlaceQuaterniusModel("Wall_Plaster_Straight", name + " Rear Left " + floor, house.transform,
                    new Vector3(-2f, y, 2.05f), 2.05f, 180f);
                PlaceQuaterniusModel("Wall_Plaster_Window_Wide_Flat2", name + " Rear Center " + floor, house.transform,
                    new Vector3(0f, y, 2.05f), 2.05f, 180f);
                PlaceQuaterniusModel("Wall_Plaster_Straight", name + " Rear Right " + floor, house.transform,
                    new Vector3(2f, y, 2.05f), 2.05f, 180f);
                PlaceQuaterniusModel(sideModel, name + " West Side A " + floor, house.transform,
                    new Vector3(-3.0f, y, -1.0f), 2.05f, 90f);
                PlaceQuaterniusModel("Wall_Plaster_Window_Thin_Round", name + " West Side B " + floor, house.transform,
                    new Vector3(-3.0f, y, 1.0f), 2.05f, 90f);
                PlaceQuaterniusModel("Wall_Plaster_Window_Thin_Round", name + " East Side A " + floor, house.transform,
                    new Vector3(3.0f, y, -1.0f), 2.05f, -90f);
                PlaceQuaterniusModel(sideModel, name + " East Side B " + floor, house.transform,
                    new Vector3(3.0f, y, 1.0f), 2.05f, -90f);
            }

            PlaceQuaterniusModel("Door_2_Round", name + " Closed Door", house.transform,
                new Vector3(0f, 0f, -2.32f), 1.08f, 0f);
            PlaceQuaterniusModel("DoorFrame_Round_WoodDark", name + " Door Frame", house.transform,
                new Vector3(0f, 0f, -2.35f), 1.45f, 0f);
            PlaceQuaterniusModel("WindowShutters_Wide_Round_Open", name + " Open Shutters", house.transform,
                new Vector3(2f, twoStoreys ? 3.72f : 0.65f, -2.30f), 2.20f, 0f);
            if (twoStoreys)
            {
                PlaceQuaterniusModel("Balcony_Simple_Straight", name + " Balcony Left", house.transform,
                    new Vector3(-1.5f, 3.0f, -2.72f), 2.0f, 0f);
                PlaceQuaterniusModel("Balcony_Cross_Straight", name + " Balcony Right", house.transform,
                    new Vector3(1.5f, 3.0f, -2.72f), 2.0f, 0f);
            }
            string roofModel = twoStoreys ? "Roof_RoundTiles_6x8" : "Roof_RoundTiles_4x6";
            float roofFootprint = twoStoreys ? 6.7f : 5.55f;
            float roofBase = storeys * 3.08f;
            PlaceQuaterniusModel(roofModel, name + " Deep Tile Roof", house.transform,
                new Vector3(0f, roofBase, 0f), roofFootprint, 0f);
            PlaceQuaterniusModel(twoStoreys ? "Roof_Front_Brick6" : "Roof_Front_Brick4",
                name + " Front Gable", house.transform,
                new Vector3(0f, roofBase, -2.15f), twoStoreys ? 5.6f : 4.4f, 0f);
            PlaceQuaterniusModel("Prop_Chimney", name + " Chimney", house.transform,
                new Vector3(1.6f, storeys * 3.08f + 2.3f, 0.5f), 1.0f, 0f);
        }

        private static void BuildAdvancedWorkshop(
            string name,
            Transform parent,
            Vector3 localPosition,
            float localRotation)
        {
            GameObject house = CreateAdvancedCluster(name, parent,
                parent.TransformPoint(localPosition), parent.eulerAngles.y + localRotation);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", name + " Front Left", house.transform,
                new Vector3(-1.05f, 0f, -2.0f), 2.1f, 0f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Door_Round", name + " Front Door", house.transform,
                new Vector3(1.05f, 0f, -2.0f), 2.1f, 0f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Window_Wide_Round", name + " Rear Left", house.transform,
                new Vector3(-1.05f, 0f, 2.0f), 2.1f, 180f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", name + " Rear Right", house.transform,
                new Vector3(1.05f, 0f, 2.0f), 2.1f, 180f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", name + " West Side A", house.transform,
                new Vector3(-2.05f, 0f, -1.0f), 2.1f, 90f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", name + " West Side B", house.transform,
                new Vector3(-2.05f, 0f, 1.0f), 2.1f, 90f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Window_Thin_Round", name + " East Side A", house.transform,
                new Vector3(2.05f, 0f, -1.0f), 2.1f, -90f);
            PlaceQuaterniusModel("Wall_UnevenBrick_Straight", name + " East Side B", house.transform,
                new Vector3(2.05f, 0f, 1.0f), 2.1f, -90f);
            PlaceQuaterniusModel("Door_4_Round", name + " Door", house.transform,
                new Vector3(1.05f, 0f, -2.28f), 1.08f, 0f);
            PlaceQuaterniusModel("Roof_RoundTiles_4x6", name + " Tile Roof", house.transform,
                new Vector3(0f, 3.10f, 0f), 5.25f, 0f);
            PlaceQuaterniusModel("Roof_Front_Brick4", name + " Front Gable", house.transform,
                new Vector3(0f, 3.10f, -2.15f), 4.2f, 0f);
            PlaceQuaterniusModel("Prop_Chimney2", name + " Furnace Chimney", house.transform,
                new Vector3(-1.35f, 4.8f, 0.5f), 1.15f, 0f);
        }

        private static void BuildAdvancedGroundPolish(Transform parent, Material road)
        {
            GameObject polish = new GameObject("Ground Contact And Route Edge Polish");
            polish.transform.SetParent(parent, false);
            Vector3[] positions =
            {
                new Vector3(-5.4f, 0.011f, 15.6f), new Vector3(5.6f, 0.011f, 16.0f),
                new Vector3(12.5f, 0.011f, 8.1f), new Vector3(14.6f, 0.011f, 4.8f),
                new Vector3(-14.0f, 0.011f, -6.0f), new Vector3(-17.0f, 0.011f, -8.2f),
                new Vector3(11.8f, 0.011f, -12.8f), new Vector3(14.2f, 0.011f, -15.1f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                CreateGroundOval("Layered Route Edge " + (i + 1).ToString("00"), polish.transform,
                    positions[i], new Vector2(0.78f + 0.14f * (i % 3), 0.28f + 0.08f * (i % 2)),
                    17f + i * 29f, road,
                    new Color(0.66f + 0.02f * (i % 2), 0.59f, 0.46f, 1f));
                PlaceQuaterniusModel(i % 2 == 0 ? "Prop_Brick1" : "Prop_Brick3",
                    "Route Edge Debris " + (i + 1).ToString("00"), polish.transform,
                    positions[i] + new Vector3(0.45f, -0.011f, -0.15f),
                    0.30f + 0.05f * (i % 3), 31f + i * 47f);
            }
        }

        private static void ConfigureQuaterniusTextureImports()
        {
            string[] normalPaths = Directory.GetFiles(QuaterniusVillageTextureRoot, "*_Normal.png");
            foreach (string fullPath in normalPaths)
            {
                string assetPath = fullPath.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null || importer.textureType == TextureImporterType.NormalMap)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.NormalMap;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
        }

        private static void ApplySceneDirectorLevelRefactorInternal(Transform mapRoot)
        {
            Transform previous = mapRoot.Find("Scene Director Level Refactor");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject refactorRoot = new GameObject("Scene Director Level Refactor");
            refactorRoot.transform.SetParent(mapRoot);

            Material road = GetOrCreateMainMapRoadMaterial();
            Sprite bamboo = AssetDatabase.LoadAssetAtPath<Sprite>(Hd2dBambooPath);

            BuildCentralArrivalCluster(refactorRoot.transform, road, bamboo);
            BuildEastHamletStoryCluster(refactorRoot.transform, road, bamboo);
            BuildWestForestStoryCluster(refactorRoot.transform, road, bamboo);
            BuildNorthPassStoryCluster(refactorRoot.transform, road, bamboo);
            BuildSouthMineStoryCluster(refactorRoot.transform, road, bamboo);
            BuildAuthoredRoadEdgeRhythm(refactorRoot.transform, road, bamboo);
            RecomposeExistingScenery();
            ConfigureScenicSpriteMaterials(refactorRoot.transform, GetOrCreateScenicSpriteMaterial());
            ConfigureResponsiveHd2dCamera();

            Light sun = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.type == LightType.Directional);
            if (sun != null)
            {
                sun.color = new Color(1f, 0.84f, 0.66f, 1f);
                sun.intensity = 1.15f;
                sun.shadowStrength = 0.68f;
                sun.transform.rotation = Quaternion.Euler(52f, -42f, 0f);
                EditorUtility.SetDirty(sun);
            }

            RenderSettings.ambientIntensity = 0.86f;
            RenderSettings.reflectionIntensity = 0.25f;
            RenderSettings.fogColor = new Color(0.50f, 0.59f, 0.58f, 1f);
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 64f;
        }

        private static void BuildCentralArrivalCluster(Transform parent, Material road, Sprite bamboo)
        {
            GameObject cluster = new GameObject("Sparse Central Arrival Cluster - Safe Zone");
            cluster.transform.SetParent(parent);

            CreateLanternPost("Bridge Arrival Lantern West", cluster.transform, new Vector3(-3.9f, 0f, -2.9f), 18f, 0.82f);
            CreateLanternPost("Bridge Arrival Lantern East", cluster.transform, new Vector3(4.2f, 0f, 3.0f), 198f, 0.72f);
            PlaceDressingModel("detail_rocks_small", "Courier Plaza Worn Stones", cluster.transform,
                new Vector3(-4.7f, 0f, 3.8f), 0.92f, 28f);
            PlaceDressingModel("detail_rocks_small", "Courier Plaza Milestone Debris", cluster.transform,
                new Vector3(4.9f, 0f, -3.9f), 0.72f, 114f);
            CreateGroundOval("Bridge Foot Worn Earth", cluster.transform,
                new Vector3(0.5f, 0.010f, -3.2f), new Vector2(2.6f, 1.05f), 8f, road, new Color(0.84f, 0.80f, 0.70f, 1f));
            CreateGroundOval("Courier Plaza Cart Turn", cluster.transform,
                new Vector3(-2.8f, 0.009f, 2.9f), new Vector2(1.45f, 0.70f), -22f, road, new Color(0.78f, 0.73f, 0.63f, 1f));
            CreateRoadsideShrub("Central Sparse Grass", cluster.transform, bamboo,
                new Vector3(5.2f, 0f, 4.0f), 0.13f, 0, false);
        }

        private static void BuildEastHamletStoryCluster(Transform parent, Material road, Sprite bamboo)
        {
            GameObject cluster = new GameObject("Medium East Hamlet Cluster - Recently Vacated Courier Stop");
            cluster.transform.SetParent(parent);

            PlaceDressingModel("market", "Abandoned Courier Awning", cluster.transform,
                new Vector3(25.7f, 0f, 8.2f), 2.55f, -64f);
            PlaceDressingModel("wall_straight", "Courier Stop Windbreak", cluster.transform,
                new Vector3(26.6f, 0f, 11.2f), 3.0f, 10f);
            PlaceDressingModel("detail_rocks", "Courier Stop Anchor Rock", cluster.transform,
                new Vector3(27.2f, 0f, 6.2f), 1.42f, 48f);
            PlaceDressingModel("detail_rocks_small", "Courier Stop Scattered Cargo Stones", cluster.transform,
                new Vector3(23.9f, 0f, 9.8f), 0.98f, 126f);
            CreateLanternPost("Courier Stop Last Lantern", cluster.transform, new Vector3(24.2f, 0f, 6.4f), 35f, 0.74f);
            CreateGroundOval("Courier Stop Trampled Earth", cluster.transform,
                new Vector3(25.4f, 0.009f, 8.6f), new Vector2(2.2f, 1.15f), 24f, road, new Color(0.74f, 0.68f, 0.55f, 1f));
            CreateRoadsideShrub("East Hamlet Grass A", cluster.transform, bamboo, new Vector3(27.8f, 0f, 10.8f), 0.17f, 1, false);
            CreateRoadsideShrub("East Hamlet Grass B", cluster.transform, bamboo, new Vector3(23.4f, 0f, 7.8f), 0.14f, 2, true);
            CreateRoadsideShrub("East Hamlet Grass C", cluster.transform, bamboo, new Vector3(27.6f, 0f, 5.0f), 0.12f, 1, true);
        }

        private static void BuildWestForestStoryCluster(Transform parent, Material road, Sprite bamboo)
        {
            GameObject cluster = new GameObject("Dense West Forest Cluster - Ambushed Caravan Aftermath");
            cluster.transform.SetParent(parent);

            PlaceDressingModel("wall_straight", "Toppled Forest Barricade", cluster.transform,
                new Vector3(-26.0f, 0f, -12.8f), 3.15f, 62f);
            PlaceDressingModel("detail_treeA", "Ambush Canopy Pine", cluster.transform,
                new Vector3(-27.8f, 0f, -15.2f), 2.35f, 25f);
            PlaceDressingModel("detail_treeC", "Ambush Leaning Pine", cluster.transform,
                new Vector3(-23.8f, 0f, -15.8f), 1.95f, 142f);
            PlaceDressingModel("detail_rocks", "Ambush Cover Boulder", cluster.transform,
                new Vector3(-27.4f, 0f, -10.5f), 1.58f, 96f);
            PlaceDressingModel("detail_rocks_small", "Ambush Wheel-Rut Stones A", cluster.transform,
                new Vector3(-23.8f, 0f, -12.0f), 1.05f, 15f);
            PlaceDressingModel("detail_rocks_small", "Ambush Wheel-Rut Stones B", cluster.transform,
                new Vector3(-25.0f, 0f, -16.9f), 0.82f, 174f);
            CreateGroundOval("Ambush Mud Scar", cluster.transform,
                new Vector3(-25.3f, 0.009f, -13.6f), new Vector2(2.45f, 0.82f), 58f, road, new Color(0.61f, 0.57f, 0.43f, 1f));
            CreateRoadsideShrub("West Dense Brush A", cluster.transform, bamboo, new Vector3(-28.8f, 0f, -13.5f), 0.22f, 2, false);
            CreateRoadsideShrub("West Dense Brush B", cluster.transform, bamboo, new Vector3(-22.5f, 0f, -14.6f), 0.18f, 3, true);
            CreateRoadsideShrub("West Dense Brush C", cluster.transform, bamboo, new Vector3(-27.0f, 0f, -17.0f), 0.16f, 2, true);
            CreateRoadsideShrub("West Dense Brush D", cluster.transform, bamboo, new Vector3(-23.0f, 0f, -10.5f), 0.14f, 3, false);
        }

        private static void BuildNorthPassStoryCluster(Transform parent, Material road, Sprite bamboo)
        {
            GameObject cluster = new GameObject("Sparse North Pass Cluster - Abandoned Watch Post");
            cluster.transform.SetParent(parent);

            PlaceDressingModel("watchtower", "North Pass Empty Watch Post", cluster.transform,
                new Vector3(-25.9f, 0f, 23.0f), 2.28f, 168f);
            PlaceDressingModel("wall_straight", "North Pass Broken Signal Wall", cluster.transform,
                new Vector3(-22.8f, 0f, 24.2f), 3.0f, 12f);
            PlaceDressingModel("detail_rocks", "North Pass Wind Rock", cluster.transform,
                new Vector3(-27.8f, 0f, 20.7f), 1.45f, 42f);
            PlaceDressingModel("detail_rocks_small", "North Pass Ash Stones", cluster.transform,
                new Vector3(-23.2f, 0f, 21.2f), 0.88f, 120f);
            CreateGroundOval("North Watch Fire Ash", cluster.transform,
                new Vector3(-24.5f, 0.009f, 21.5f), new Vector2(1.1f, 0.8f), 4f, road, new Color(0.54f, 0.52f, 0.47f, 1f));
            CreateRoadsideShrub("North Wind Grass A", cluster.transform, bamboo, new Vector3(-28.4f, 0f, 23.2f), 0.14f, 1, true);
            CreateRoadsideShrub("North Wind Grass B", cluster.transform, bamboo, new Vector3(-21.7f, 0f, 22.4f), 0.12f, 1, false);
        }

        private static void BuildSouthMineStoryCluster(Transform parent, Material road, Sprite bamboo)
        {
            GameObject cluster = new GameObject("Medium South Mine Cluster - Fresh Rockfall");
            cluster.transform.SetParent(parent);

            PlaceDressingModel("wall_straight", "Mine Emergency Timber Line", cluster.transform,
                new Vector3(22.8f, 0f, -16.2f), 3.2f, 78f);
            PlaceDressingModel("detail_rocks", "Fresh Rockfall Main Boulder", cluster.transform,
                new Vector3(22.0f, 0f, -18.2f), 1.78f, 35f);
            PlaceDressingModel("detail_rocks", "Fresh Rockfall Split Boulder", cluster.transform,
                new Vector3(25.0f, 0f, -17.0f), 1.34f, 118f);
            PlaceDressingModel("detail_rocks_small", "Fresh Rockfall Scree A", cluster.transform,
                new Vector3(23.8f, 0f, -19.0f), 1.08f, 15f);
            PlaceDressingModel("detail_rocks_small", "Fresh Rockfall Scree B", cluster.transform,
                new Vector3(20.5f, 0f, -19.4f), 0.90f, 166f);
            CreateLanternPost("Mine Warning Lantern", cluster.transform, new Vector3(20.7f, 0f, -16.6f), 102f, 0.88f);
            CreateGroundOval("Mine Ore Dust", cluster.transform,
                new Vector3(22.5f, 0.009f, -17.8f), new Vector2(2.6f, 1.2f), -18f, road, new Color(0.67f, 0.56f, 0.43f, 1f));
            CreateRoadsideShrub("South Dry Grass A", cluster.transform, bamboo, new Vector3(25.6f, 0f, -18.6f), 0.13f, 2, true);
            CreateRoadsideShrub("South Dry Grass B", cluster.transform, bamboo, new Vector3(20.2f, 0f, -20.0f), 0.11f, 1, false);
        }

        private static void BuildAuthoredRoadEdgeRhythm(Transform parent, Material road, Sprite bamboo)
        {
            GameObject edgeRoot = new GameObject("Road Edge Rhythm - Dense Medium Sparse Empty");
            edgeRoot.transform.SetParent(parent);

            Vector3[] wornEarth =
            {
                new Vector3(-1.8f, 0.009f, 7.8f), new Vector3(2.0f, 0.009f, 11.5f),
                new Vector3(8.4f, 0.009f, 1.8f), new Vector3(13.8f, 0.009f, -1.8f),
                new Vector3(-8.0f, 0.009f, -1.5f), new Vector3(-15.4f, 0.009f, 1.9f),
                new Vector3(2.0f, 0.009f, -9.5f), new Vector3(-2.1f, 0.009f, -14.5f)
            };
            for (int i = 0; i < wornEarth.Length; i++)
            {
                CreateGroundOval($"Irregular Road Shoulder {i + 1:00}", edgeRoot.transform, wornEarth[i],
                    new Vector2(0.72f + (i % 3) * 0.20f, 0.32f + (i % 2) * 0.12f),
                    18f + i * 31f, road, new Color(0.77f, 0.72f, 0.61f, 1f));
            }

            Vector3[] shoulderStones =
            {
                new Vector3(-2.4f, 0f, 9.2f), new Vector3(2.7f, 0f, 13.0f),
                new Vector3(9.6f, 0f, 2.5f), new Vector3(14.8f, 0f, -2.8f),
                new Vector3(-9.2f, 0f, -2.3f), new Vector3(-14.0f, 0f, 2.6f),
                new Vector3(2.5f, 0f, -10.8f), new Vector3(-2.6f, 0f, -13.2f)
            };
            for (int i = 0; i < shoulderStones.Length; i++)
            {
                PlaceDressingModel("detail_rocks_small", $"Road Shoulder Stone Cluster {i + 1:00}",
                    edgeRoot.transform, shoulderStones[i], 0.56f + (i % 3) * 0.10f, 22f + i * 47f);
            }

            Vector3[] grassPositions =
            {
                new Vector3(-3.0f, 0f, 8.5f), new Vector3(3.1f, 0f, 12.4f),
                new Vector3(9.0f, 0f, 3.3f), new Vector3(15.4f, 0f, -2.0f),
                new Vector3(-9.8f, 0f, -3.1f), new Vector3(-15.0f, 0f, 3.3f),
                new Vector3(3.2f, 0f, -11.5f), new Vector3(-3.1f, 0f, -12.5f)
            };
            for (int i = 0; i < grassPositions.Length; i++)
            {
                CreateRoadsideShrub($"Road Shoulder Grass {i + 1:00}", edgeRoot.transform, bamboo,
                    grassPositions[i], 0.095f + (i % 3) * 0.018f, i % 3, i % 2 == 0);
            }
        }

        private static GameObject PlaceDressingModel(
            string assetName,
            string objectName,
            Transform parent,
            Vector3 position,
            float targetFootprint,
            float yRotation)
        {
            if (MainMapRiverLayout.IsInsideRiver(position, 0.25f))
            {
                position = MainMapRiverLayout.GetNearestSafeBankPosition(position, 0.75f);
            }

            GameObject instance = PlaceModel(assetName, objectName, parent, position, targetFootprint, yRotation);
            if (instance == null)
            {
                return null;
            }

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            return instance;
        }

        private static void CreateRoadsideShrub(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 position,
            float scale,
            int sortingOrder,
            bool flipX)
        {
            if (sprite == null)
            {
                return;
            }

            CreateHd2dBillboard(name, parent, sprite, position, scale,
                new Color(0.44f, 0.60f, 0.42f, 0.72f), sortingOrder, flipX);
        }

        private static void CreateGroundOval(
            string name,
            Transform parent,
            Vector3 position,
            Vector2 radius,
            float yRotation,
            Material material,
            Color tint)
        {
            GameObject oval = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            oval.name = name;
            oval.transform.SetParent(parent);
            oval.transform.position = position;
            oval.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            oval.transform.localScale = new Vector3(radius.x, 0.006f, radius.y);
            Renderer renderer = oval.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", tint);
            renderer.SetPropertyBlock(block);
            UnityEngine.Object.DestroyImmediate(oval.GetComponent<Collider>());
        }

        private static void CreateLanternPost(
            string name,
            Transform parent,
            Vector3 position,
            float yRotation,
            float lightIntensity)
        {
            GameObject lantern = new GameObject(name);
            lantern.transform.SetParent(parent);
            lantern.transform.position = position;
            lantern.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            Material darkWood = Material("Lantern_DarkWood", new Color(0.18f, 0.10f, 0.055f));
            Material paper = Material("Lantern_WarmPaper", new Color(0.95f, 0.61f, 0.24f));
            GameObject post = CreateLocalCube("Post", lantern.transform,
                new Vector3(0f, 0.75f, 0f), new Vector3(0.10f, 1.5f, 0.10f), darkWood);
            GameObject arm = CreateLocalCube("Arm", lantern.transform,
                new Vector3(0.24f, 1.34f, 0f), new Vector3(0.48f, 0.08f, 0.08f), darkWood);
            GameObject lamp = CreateLocalCube("Warm Paper Lamp", lantern.transform,
                new Vector3(0.45f, 1.13f, 0f), new Vector3(0.27f, 0.38f, 0.27f), paper);
            GameObject cap = CreateLocalCube("Rain Cap", lantern.transform,
                new Vector3(0.45f, 1.36f, 0f), new Vector3(0.38f, 0.06f, 0.38f), darkWood);
            UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
            UnityEngine.Object.DestroyImmediate(arm.GetComponent<Collider>());
            UnityEngine.Object.DestroyImmediate(lamp.GetComponent<Collider>());
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());

            Light light = new GameObject("Warm Pool Light").AddComponent<Light>();
            light.transform.SetParent(lantern.transform, false);
            light.transform.localPosition = new Vector3(0.45f, 1.18f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.54f, 0.20f, 1f);
            light.intensity = lightIntensity;
            light.range = 4.8f;
            light.shadows = LightShadows.None;
        }

        private static void RecomposeExistingScenery()
        {
            RepositionScenery("Tree A2", new Vector3(25.0f, 0f, 2.5f), 1.60f);
            RepositionScenery("Tree B1", new Vector3(-8.8f, 0f, -4.9f), 1.98f);
            RepositionScenery("Tree B2", new Vector3(-20.5f, 0f, 12.2f), 1.62f);
            RepositionScenery("Tree C1", new Vector3(21.5f, 0f, 4.0f), 1.55f);
            RepositionScenery("Tree C2", new Vector3(25.5f, 0f, -10.5f), 1.55f);
            RepositionScenery("Tree C3", new Vector3(23.8f, 0f, -0.5f), 1.52f);
            RepositionScenery("East Orchard Tree A", new Vector3(18.2f, 0f, 3.1f), 1.62f);

            ScaleDecorativeRoot("East Route Sign", 0.68f);
            ScaleDecorativeRoot("West Route Sign", 0.68f);
            ScaleDecorativeRoot("North Route Sign", 0.68f);
            ScaleDecorativeRoot("South Route Sign", 0.68f);
        }

        private static void ScaleDecorativeRoot(string objectName, float uniformScale)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
            {
                return;
            }

            target.transform.localScale = Vector3.one * uniformScale;
            foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            EditorUtility.SetDirty(target.transform);
        }

        private static void RepositionScenery(string objectName, Vector3 position, float targetFootprint)
        {
            GameObject target = GameObject.Find(objectName);
            if (target == null)
            {
                return;
            }

            Bounds before = CalculateRendererBounds(target);
            float currentFootprint = Mathf.Max(before.size.x, before.size.z);
            if (currentFootprint > 0.001f)
            {
                target.transform.localScale *= targetFootprint / currentFootprint;
            }
            Bounds after = CalculateRendererBounds(target);
            target.transform.position = position + Vector3.up * (position.y - after.min.y);
            EditorUtility.SetDirty(target.transform);
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
            Material sky = GetOrCreateMainMapSkyMaterial();
            Camera camera = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera != null)
            {
                camera.clearFlags = sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.47f, 0.58f, 0.65f, 1f);
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

            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.47f, 0.52f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.31f, 0.30f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.09f, 0.07f, 1f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0.35f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.42f, 0.49f, 0.50f, 1f);
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 70f;
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
            PlaceModel("detail_treeC", "Tree C2", scenery.transform, new Vector3(7.5f, 0f, -8.6f), 2.15f, -45f);
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
            Sprite treasureChestSprite, Sprite[] healingHerbFrames,
            Sprite[] defenseHerbFrames, Sprite[] moveSpeedHerbFrames, Sprite[] mysteryHerbFrames)
        {
            GameObject mapRoot = GameObject.Find("3D Prototype Map");
            if (mapRoot == null)
            {
                return;
            }

            Sprite fallbackSprite = GetOrCreatePrototypeSprite();
            Sprite[] reedMantisIdle = LoadFrames(ReedMantisIdlePath, fallbackSprite);
            Sprite[] bronzeToadIdle = LoadFrames(BronzeToadIdlePath, fallbackSprite);
            Sprite[] crimsonScorpionIdle = LoadFrames(CrimsonScorpionIdlePath, fallbackSprite);

            Transform previousExpansion = mapRoot.transform.Find("Expanded Main Map Content");
            if (previousExpansion != null)
            {
                UnityEngine.Object.DestroyImmediate(previousExpansion.gameObject);
            }

            ResizeMapObject(mapRoot.transform, "Walkable Ground", Vector3.zero, new Vector3(6.4f, 1f, 5.6f));
            ResizeMapObject(mapRoot.transform, "Main Dirt Road", new Vector3(0f, 0.025f, 0f), new Vector3(3.2f, 0.05f, 53f));
            ResizeMapObject(mapRoot.transform, "Cross Dirt Road", new Vector3(0f, 0.03f, 0.8f), new Vector3(61f, 0.05f, 2.5f));
            ResizeMapObject(mapRoot.transform, "North Ridge Road", new Vector3(-8.2f, 0.028f, 7.2f), new Vector3(16f, 0.05f, 2.1f));
            ResizeMapObject(mapRoot.transform, "South Cave Road", new Vector3(8.5f, 0.028f, -7.2f), new Vector3(17f, 0.05f, 2.1f));

            ResizeMapObject(mapRoot.transform, "North Boundary", new Vector3(0f, 0.55f, 28.2f), new Vector3(64f, 1.1f, 0.45f));
            ResizeMapObject(mapRoot.transform, "South Boundary", new Vector3(0f, 0.55f, -28.2f), new Vector3(64f, 1.1f, 0.45f));
            ResizeMapObject(mapRoot.transform, "West Boundary", new Vector3(-32.2f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 56f));
            ResizeMapObject(mapRoot.transform, "East Boundary", new Vector3(32.2f, 0.55f, 0f), new Vector3(0.45f, 1.1f, 56f));

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
            CreateCube("East Frontier Trail", roads.transform, new Vector3(23f, 0.028f, 0f), new Vector3(2.05f, 0.05f, 42f), path);
            CreateCube("West Frontier Trail", roads.transform, new Vector3(-23f, 0.028f, 0f), new Vector3(2.05f, 0.05f, 42f), path);
            CreateCube("North Frontier Road", roads.transform, new Vector3(0f, 0.029f, 21f), new Vector3(48f, 0.05f, 2.05f), path);
            CreateCube("South Frontier Road", roads.transform, new Vector3(0f, 0.029f, -21f), new Vector3(48f, 0.05f, 2.05f), path);
            CreateCube("Far East Ring", roads.transform, new Vector3(29f, 0.029f, 0f), new Vector3(2.05f, 0.05f, 49f), path);
            CreateCube("Far West Ring", roads.transform, new Vector3(-29f, 0.029f, 0f), new Vector3(2.05f, 0.05f, 49f), path);
            CreateCube("Far North Route", roads.transform, new Vector3(0f, 0.029f, 25.5f), new Vector3(58f, 0.05f, 2.05f), path);
            CreateCube("Far South Route", roads.transform, new Vector3(0f, 0.029f, -25.5f), new Vector3(58f, 0.05f, 2.05f), path);

            CreateFormalMainMapWayfinding(expansion.transform, path);

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
            PlaceModel("watchtower", "East Frontier Watchtower", scenery.transform, new Vector3(24.5f, 0f, -4f), 2.2f, 190f);
            PlaceModel("mine", "East Cloud Cave", scenery.transform, new Vector3(25f, 0f, 17f), 4.2f, -90f);
            PlaceModel("mine", "Northwest Ruin Cave", scenery.transform, new Vector3(-25f, 0f, 17f), 4.2f, 90f);
            PlaceModel("mine", "Southwest Hidden Cave", scenery.transform, new Vector3(-23f, 0f, -20f), 4.2f, 35f);
            PlaceModel("watchtower", "North Frontier Watchtower", scenery.transform, new Vector3(9.5f, 0f, 21.5f), 2.15f, 170f);
            PlaceModel("market", "South Frontier Caravan", scenery.transform, new Vector3(-8.5f, 0f, -21f), 2.65f, 15f);
            PlaceModel("detail_treeA", "East Frontier Tree", scenery.transform, new Vector3(24.2f, 0f, 9.5f), 2.2f, 80f);
            PlaceModel("detail_treeB", "West Frontier Tree", scenery.transform, new Vector3(-24.5f, 0f, 5f), 2.25f, 130f);
            PlaceModel("detail_rocks", "North Frontier Rocks", scenery.transform, new Vector3(-3f, 0f, 21.5f), 1.8f, 20f);
            PlaceModel("detail_rocks_small", "South Frontier Stones", scenery.transform, new Vector3(12f, 0f, -21.3f), 1.3f, 90f);

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
            GameObject innerSwordsman = CreateEncounter("烟雨剑客", inkWolfIdle, inkWolfIdle, new Vector3(-3.5f, 0f, 7.8f),
                EncounterType.NormalEnemy,
                Stats("烟雨剑客", 40, 7, 1, 1.18f, "ink_wolf", critChance: 0.06f, dodgeChance: 0.06f),
                13, 4, 1.3f);
            GameObject eastAmbush = CreateEncounter("东岭伏兵", bambooPuppetIdle, bambooPuppetIdle, new Vector3(23f, 0f, 4.5f),
                EncounterType.NormalEnemy,
                Stats("东岭伏兵", 58, 9, 2, 1.02f, "bamboo_puppet", dodgeChance: 0.04f),
                18, 6, 1.15f);
            GameObject westRatPack = CreateEncounter("西门鼠群", ratRun, ratRun, new Vector3(-23f, 0f, 5.5f),
                EncounterType.NormalEnemy,
                Stats("西门鼠群", 48, 7, 1, 1.32f, "rat", dodgeChance: 0.06f),
                15, 4, 1.05f);
            GameObject northWanderer = CreateEncounter("北关游侠", riderRun, riderRun, new Vector3(2.5f, 0f, 20.5f),
                EncounterType.NormalEnemy,
                Stats("北关游侠", 70, 10, 3, 1f, "rider", critChance: 0.06f),
                22, 7, 1.15f);
            GameObject southBlade = CreateEncounter("南荒刀客", bambooPuppetIdle, bambooPuppetIdle, new Vector3(-2.5f, 0f, -20.5f),
                EncounterType.NormalEnemy,
                Stats("南荒刀客", 75, 11, 3, 0.95f, "bamboo_puppet", lifeSteal: 0.08f),
                23, 8, 1.15f);
            GameObject eastFrontierElite = CreateEncounter("东关铁卫", stoneApeIdle, stoneApeIdle, new Vector3(22f, 0f, -5f),
                EncounterType.EliteEnemy,
                Stats("东关铁卫", 165, 16, 6, 0.78f, "stone_ape"),
                42, 15, 1.25f);
            GameObject northFrontierElite = CreateEncounter("北漠刀魁", stoneApeIdle, stoneApeIdle, new Vector3(9f, 0f, 20f),
                EncounterType.EliteEnemy,
                Stats("北漠刀魁", 175, 17, 6, 0.8f, "stone_ape", critChance: 0.06f),
                45, 16, 1.25f);
            GameObject farEastShadow = CreateEncounter("东海影客", inkWolfIdle, inkWolfIdle, new Vector3(29f, 0f, 12f),
                EncounterType.NormalEnemy,
                Stats("东海影客", 82, 12, 2, 1.35f, "ink_wolf", critChance: 0.09f, dodgeChance: 0.14f),
                27, 9, 1.35f);
            GameObject farWestBlood = CreateEncounter("西漠血徒", riderRun, riderRun, new Vector3(-29f, 0f, -10f),
                EncounterType.NormalEnemy,
                Stats("西漠血徒", 96, 14, 3, 1.02f, "rider", critChance: 0.07f, lifeSteal: 0.16f),
                29, 10, 1.2f);
            GameObject farNorthPoisonElite = CreateEncounter("北门毒宗", bambooPuppetIdle, bambooPuppetIdle, new Vector3(-12f, 0f, 25.5f),
                EncounterType.EliteEnemy,
                Stats("北门毒宗", 190, 18, 5, 0.96f, "bamboo_puppet", dodgeChance: 0.07f, lifeSteal: 0.18f),
                48, 17, 1.2f);
            GameObject farSouthIronElite = CreateEncounter("南岭铁僧", stoneApeIdle, stoneApeIdle, new Vector3(13f, 0f, -25.5f),
                EncounterType.EliteEnemy,
                Stats("南岭铁僧", 220, 18, 8, 0.70f, "stone_ape", critChance: 0.04f),
                50, 18, 1.3f);
            GameObject westReedMantis = CreateEncounter("西道青芦刀螳", reedMantisIdle, reedMantisIdle, new Vector3(-16f, 0f, 0f),
                EncounterType.NormalEnemy,
                Stats("西道青芦刀螳", 54, 9, 1, 1.45f, "reed_mantis", critChance: 0.09f, dodgeChance: 0.12f),
                17, 5, 1.12f);
            GameObject northReedMantis = CreateEncounter("北坡青芦刀螳", reedMantisIdle, reedMantisIdle, new Vector3(-16f, 0f, 8f),
                EncounterType.NormalEnemy,
                Stats("北坡青芦刀螳", 62, 10, 2, 1.40f, "reed_mantis", critChance: 0.10f, dodgeChance: 0.10f),
                20, 6, 1.12f);
            GameObject ridgeBronzeToad = CreateEncounter("岭间铜甲石蟾", bronzeToadIdle, bronzeToadIdle, new Vector3(8f, 0f, 12f),
                EncounterType.NormalEnemy,
                Stats("岭间铜甲石蟾", 110, 11, 6, 0.68f, "bronze_toad"),
                25, 8, 1.18f);
            GameObject villageBronzeToad = CreateEncounter("东村铜甲石蟾", bronzeToadIdle, bronzeToadIdle, new Vector3(16f, 0f, 16f),
                EncounterType.NormalEnemy,
                Stats("东村铜甲石蟾", 118, 12, 7, 0.66f, "bronze_toad"),
                28, 9, 1.18f);
            GameObject southCrimsonScorpion = CreateEncounter("南坡赤砂毒蝎", crimsonScorpionIdle, crimsonScorpionIdle, new Vector3(-4f, 0f, -16f),
                EncounterType.NormalEnemy,
                Stats("南坡赤砂毒蝎", 70, 11, 3, 1.18f, "crimson_scorpion", critChance: 0.08f, dodgeChance: 0.05f, lifeSteal: 0.10f),
                23, 8, 1.16f);
            GameObject mineCrimsonScorpion = CreateEncounter("矿道赤砂毒蝎", crimsonScorpionIdle, crimsonScorpionIdle, new Vector3(16f, 0f, -8f),
                EncounterType.NormalEnemy,
                Stats("矿道赤砂毒蝎", 78, 12, 3, 1.16f, "crimson_scorpion", critChance: 0.09f, dodgeChance: 0.06f, lifeSteal: 0.12f),
                26, 9, 1.16f);
            GameObject southCave = CreateCaveEncounter("岩壁密窟", new Vector3(19.2f, 0f, -14.8f),
                Stats("岩窟守卫", 175, 16, 5, 0.82f, "stone_ape"), 42, 14, CaveContentType.Random);
            GameObject eastCloudCave = CreateCaveEncounter("东岭云窟", new Vector3(24f, 0f, 16.5f),
                Stats("云窟守卫", 180, 17, 5, 0.84f, "orc_cave_guardian"), 44, 15, CaveContentType.Random);
            GameObject northwestRuinCave = CreateCaveEncounter("西北残窟", new Vector3(-24f, 0f, 16.5f),
                Stats("残窟守卫", 180, 16, 6, 0.8f, "orc_cave_guardian"), 44, 15, CaveContentType.Random);
            GameObject southwestHiddenCave = CreateCaveEncounter("西南藏窟", new Vector3(-22f, 0f, -19f),
                Stats("藏窟守卫", 185, 17, 6, 0.8f, "orc_cave_guardian"), 46, 16, CaveContentType.Random);
            GameObject farWestRelicCave = CreateCaveEncounter("西陲供器窟", new Vector3(-29f, 0f, -24f),
                Stats("供器守卫", 195, 18, 6, 0.82f, "orc_cave_guardian"), 48, 17, CaveContentType.RelicShrine);
            GameObject northTreasure = CreateEncounter("北岭宝箱", new[] { treasureChestSprite }, null, new Vector3(-7f, 0f, 16.2f),
                EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 18, 10, 0.9f);
            GameObject innerTreasure = CreateEncounter("古道补给箱", new[] { treasureChestSprite }, null, new Vector3(4.8f, 0f, 6.8f),
                EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 14, 7, 0.9f);
            GameObject westTreasure = CreateEncounter("西境宝箱", new[] { treasureChestSprite }, null, new Vector3(-24f, 0f, -2f),
                EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 20, 11, 0.9f);
            GameObject southTreasure = CreateEncounter("南陲宝箱", new[] { treasureChestSprite }, null, new Vector3(6f, 0f, -20f),
                EncounterType.Treasure, Stats("宝箱", 1, 0, 0, 1f), 20, 11, 0.9f);
            GameObject eastMysteryHerb = CreateEncounter("东郊无名奇草", mysteryHerbFrames, null, new Vector3(19.5f, 0f, 1.8f),
                EncounterType.MysteryHerb, Stats("无名奇草", 1, 0, 0, 1f), 0, 0, 0.85f);
            EncounterTrigger eastMysteryTrigger = eastMysteryHerb.GetComponent<EncounterTrigger>();
            eastMysteryTrigger.mysteryCultivationReward = 45;
            eastMysteryTrigger.mysteryPoisonChance = 0.25f;
            eastMysteryTrigger.mysteryDebuffChance = 0.25f;
            eastMysteryTrigger.mysteryHealthLossRatio = 0.25f;
            GameObject northSpeedHerb = CreateEncounter("北坡轻身草", moveSpeedHerbFrames, null, new Vector3(-7f, 0f, 11.8f),
                EncounterType.Herb, Stats("轻身草", 1, 0, 0, 1f), 0, 0, 0.85f);
            EncounterTrigger northSpeedTrigger = northSpeedHerb.GetComponent<EncounterTrigger>();
            northSpeedTrigger.herbEffect = HerbEffectType.MoveSpeed;
            northSpeedTrigger.herbBuffValue = 0.12f;
            GameObject southHealingHerb = CreateEncounter("南岭止血草", healingHerbFrames, null, new Vector3(14f, 0f, -12f),
                EncounterType.Herb, Stats("止血草", 1, 0, 0, 1f), 0, 0, 0.85f);
            southHealingHerb.GetComponent<EncounterTrigger>().healRatio = 0.4f;
            GameObject eastDefenseHerb = CreateEncounter("东村铁骨草", defenseHerbFrames, null, new Vector3(21f, 0f, 10.5f),
                EncounterType.Herb, Stats("铁骨草", 1, 0, 0, 1f), 0, 0, 0.85f);
            EncounterTrigger eastDefenseTrigger = eastDefenseHerb.GetComponent<EncounterTrigger>();
            eastDefenseTrigger.herbEffect = HerbEffectType.Defense;
            eastDefenseTrigger.herbBuffValue = 1.5f;

            GameObject[] regionalEncounters =
            {
                eastBandit, northBallista, westWolf, southRider, northElite,
                eastQuickblade, westPoisoner, northGuard, southAssassin, westSiegeBow, eastScout,
                innerSwordsman, eastAmbush, westRatPack, northWanderer, southBlade,
                eastFrontierElite, northFrontierElite, farEastShadow, farWestBlood,
                farNorthPoisonElite, farSouthIronElite,
                westReedMantis, northReedMantis, ridgeBronzeToad, villageBronzeToad,
                southCrimsonScorpion, mineCrimsonScorpion,
                southCave, eastCloudCave, northwestRuinCave, southwestHiddenCave, farWestRelicCave,
                northTreasure, innerTreasure, westTreasure, southTreasure,
                eastMysteryHerb, northSpeedHerb, southHealingHerb, eastDefenseHerb
            };
            foreach (GameObject regionalEncounter in regionalEncounters)
            {
                regionalEncounter.transform.SetParent(expansion.transform, true);
            }
        }

        private static void CreateFormalMainMapWayfinding(Transform parent, Material roadMaterial)
        {
            GameObject wayfinding = new GameObject("Formal Main Map Wayfinding");
            wayfinding.transform.SetParent(parent);

            GameObject plaza = new GameObject("Central Courier Plaza");
            plaza.transform.SetParent(wayfinding.transform);
            CreateCube("Courier Plaza Ground", plaza.transform,
                new Vector3(0f, 0.034f, 0f), new Vector3(8.2f, 0.055f, 7.2f), roadMaterial);

            Material wood = Material("Wayfinding_DarkWood", new Color(0.20f, 0.13f, 0.08f));
            Material brass = Material("Wayfinding_Brass", new Color(0.54f, 0.38f, 0.17f));
            CreateCube("Plaza North Edge", plaza.transform,
                new Vector3(0f, 0.07f, 3.5f), new Vector3(8.4f, 0.08f, 0.12f), brass);
            CreateCube("Plaza South Edge", plaza.transform,
                new Vector3(0f, 0.07f, -3.5f), new Vector3(8.4f, 0.08f, 0.12f), brass);

            CreateRouteSignpost(
                "East Route Sign", wayfinding.transform, new Vector3(7.2f, 0f, 1.1f), 90f,
                "东郊机关庄", "快剑 · 破甲", "中风险", WuxiaUiTheme.Gold, wood, brass);
            CreateRouteSignpost(
                "West Route Sign", wayfinding.transform, new Vector3(-7.2f, 0f, -0.8f), 90f,
                "西林毒泽", "毒掌 · 续航", "中风险", WuxiaUiTheme.Jade, wood, brass);
            CreateRouteSignpost(
                "North Route Sign", wayfinding.transform, new Vector3(-1.2f, 0f, 7.2f), 0f,
                "北岭关隘", "铁壁 · 防御", "中高风险", WuxiaUiTheme.Paused, wood, brass);
            CreateRouteSignpost(
                "South Route Sign", wayfinding.transform, new Vector3(1.4f, 0f, -7.2f), 0f,
                "南矿山路", "装备 · 高收益", "中高风险", WuxiaUiTheme.Warning, wood, brass);

            PlaceModel("detail_rocks_small", "Plaza Boundary Stones NE", plaza.transform,
                new Vector3(4.4f, 0f, 3.8f), 1.05f, 25f);
            PlaceModel("detail_rocks_small", "Plaza Boundary Stones SW", plaza.transform,
                new Vector3(-4.4f, 0f, -3.8f), 1.05f, 205f);
        }

        private static void CreateRouteSignpost(
            string objectName,
            Transform parent,
            Vector3 position,
            float yRotation,
            string regionName,
            string routeTheme,
            string riskLabel,
            Color accent,
            Material wood,
            Material brass)
        {
            GameObject signpost = new GameObject(objectName);
            signpost.transform.SetParent(parent);
            signpost.transform.position = position;
            signpost.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            GameObject post = CreateCube("Post", signpost.transform,
                position + Vector3.up * 0.72f, new Vector3(0.14f, 1.45f, 0.14f), wood);
            GameObject board = CreateCube("Direction Board", signpost.transform,
                position + Vector3.up * 1.25f, new Vector3(1.55f, 0.42f, 0.16f), wood);
            GameObject inlay = CreateCube("Board Inlay", signpost.transform,
                position + Vector3.up * 1.25f + signpost.transform.forward * 0.09f,
                new Vector3(1.24f, 0.08f, 0.04f), brass);
            post.transform.rotation = signpost.transform.rotation;
            board.transform.rotation = signpost.transform.rotation;
            inlay.transform.rotation = signpost.transform.rotation;

            MainMapRegionGuide marker = signpost.AddComponent<MainMapRegionGuide>();
            marker.regionName = regionName;
            marker.routeTheme = routeTheme;
            marker.riskLabel = riskLabel;
            marker.accent = accent;
            marker.worldHeight = 2.05f;
            marker.detailDistance = 10f;
            marker.maxVisibleDistance = 16f;
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
            ApplyStylizedMapMaterials(instance.transform, false);
            return instance;
        }

        private static GameObject PlaceQuaterniusModel(
            string assetName,
            string objectName,
            Transform parent,
            Vector3 localPosition,
            float targetFootprint,
            float localYRotation,
            Vector3? localTilt = null)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{QuaterniusVillageModelRoot}/{assetName}.fbx");
            if (model == null)
            {
                Debug.LogWarning($"Missing Quaternius Medieval Village model: {assetName}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(model, parent) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.name = objectName;
            instance.transform.localPosition = localPosition;
            Vector3 tilt = localTilt ?? Vector3.zero;
            Quaternion authoredAxisConversion = model.transform.localRotation;
            instance.transform.localRotation = Quaternion.Euler(tilt) *
                Quaternion.Euler(0f, localYRotation, 0f) *
                authoredAxisConversion;
            instance.transform.localScale = Vector3.one;

            Bounds bounds = CalculateRendererBounds(instance);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.001f)
            {
                instance.transform.localScale *= targetFootprint / footprint;
            }

            bounds = CalculateRendererBounds(instance);
            float requestedBaseY = parent.TransformPoint(localPosition).y;
            instance.transform.position += Vector3.up * (requestedBaseY - bounds.min.y);
            ApplyStylizedMapMaterials(instance.transform, false);
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.isStatic = true;
            }
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
            Shader shader = Shader.Find(StylizedPropShaderName);
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                name = name,
                color = color
            };
            if (material.HasProperty("_Saturation")) material.SetFloat("_Saturation", 0.72f);
            if (material.HasProperty("_Contrast")) material.SetFloat("_Contrast", 0.88f);
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

        private static GameObject CreateLocalCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
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
            BillboardSprite billboard = visual.AddComponent<BillboardSprite>();
            billboard.alignment = BillboardAlignment.CameraPlane;
            SpriteFrameAnimator animator = visual.AddComponent<SpriteFrameAnimator>();
            animator.idleFrames = idleFrames;
            animator.moveFrames = moveFrames;
            animator.framesPerSecond = 10f;

            ActorGroundShadow shadow = actor.AddComponent<ActorGroundShadow>();
            shadow.visualRoot = visual.transform;
            shadow.shadowMaterial = GetOrCreateActorGroundShadowMaterial();
            shadow.baseSize = name == "Player"
                ? new Vector2(0.58f, 0.25f)
                : new Vector2(0.52f, 0.23f);
            shadow.opacity = name == "Player" ? 0.38f : 0.32f;

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
            else if (type == EncounterType.Treasure)
            {
                token.AddComponent<TreasureMapIndicator>();
            }
            else if (type == EncounterType.Herb ||
                     type == EncounterType.VisionRelic ||
                     type == EncounterType.MysteryHerb)
            {
                token.AddComponent<MapPickupIndicator>();
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
            entrance.AddComponent<CaveEntranceIndicator>();
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
