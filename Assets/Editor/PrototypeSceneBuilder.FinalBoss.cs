#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Runtime;

namespace WuxiaRoguelite.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private const string FoxSkillRoot = "Assets/Art/Generated/Characters/Bosses/FoxDemon/spr_boss_fox_demon_";
        private const string FoxfireActionPath = FoxSkillRoot + "foxfire_right_8f_v01.png";
        private const string DemonArmorActionPath = FoxSkillRoot + "demon_armor_right_8f_v01.png";
        private const string BloodFrenzyActionPath = FoxSkillRoot + "blood_frenzy_right_8f_v01.png";
        private static readonly string[] FinalBossActionPaths = { FoxfireActionPath, DemonArmorActionPath, BloodFrenzyActionPath };
        private static readonly string[] FinalBossEffectPaths = {
            "Assets/Art/Generated/Effects/FoxDemon/spr_vfx_fox_foxfire_6f_v01.png",
            "Assets/Art/Generated/Effects/FoxDemon/spr_vfx_fox_demon_armor_6f_v01.png",
            "Assets/Art/Generated/Effects/FoxDemon/spr_vfx_fox_blood_frenzy_6f_v01.png"
        };

        [Serializable] private class FinalBossVisualScales
        {
            public float foxfire = 1f;
            public float demon_armor = 1f;
            public float blood_frenzy = 1f;
        }
        private static FinalBossVisualScales ReadFinalBossScales()
        {
            const string path = "ArtSource/Previews/Characters/Bosses/FoxDemon/Skills_20260906/visual_scales.json";
            return File.Exists(path) ? JsonUtility.FromJson<FinalBossVisualScales>(File.ReadAllText(path)) : new FinalBossVisualScales();
        }

        private static void BindFinalBossEffects(BattleScreenController screen)
        {
            screen.foxfireEffectFrames = LoadFrames(FinalBossEffectPaths[0], null);
            screen.demonArmorEffectFrames = LoadFrames(FinalBossEffectPaths[1], null);
            screen.bloodFrenzyEffectFrames = LoadFrames(FinalBossEffectPaths[2], null);
        }

        [MenuItem("37 MiniGame/Refresh Fox Demon Skill Pack")]
        public static void RefreshFoxDemonSkillPack()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before refreshing final boss assets.");
            var screen = UnityEngine.Object.FindAnyObjectByType<BattleScreenController>();
            if (screen == null) throw new InvalidOperationException("Open MainPrototype before refreshing the skill pack.");
            var profile = Array.Find(screen.enemyVisualProfiles, p => p != null && p.id == GameTextCatalog.FinalBossVisualId);
            if (profile == null) throw new InvalidOperationException("The existing fox boss visual profile is required.");
            foreach (string path in FinalBossActionPaths.Concat(FinalBossEffectPaths))
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Missing final boss skill atlas", path);
                bool effect = FinalBossEffectPaths.Contains(path);
                ConfigureSpriteSheet(path, 256, 256, effect ? 256f : 160f,
                    effect ? (Vector2?)null : new Vector2(0.5f, 0.125f));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
                int expected = effect ? 6 : 8;
                Sprite[] frames = LoadFrames(path, null);
                if (frames.Length != expected || frames.Any(f => f == null || f.rect.width != 256 || f.rect.height != 256))
                    throw new InvalidOperationException("Incorrect slicing: " + path);
            }
            Undo.RecordObject(screen, "Bind fox demon skill pack");
            profile.foxfireFrames = LoadFrames(FoxfireActionPath, null);
            profile.demonArmorFrames = LoadFrames(DemonArmorActionPath, null);
            profile.bloodFrenzyFrames = LoadFrames(BloodFrenzyActionPath, null);
            var scales = ReadFinalBossScales();
            profile.foxfireVisualScale = scales.foxfire;
            profile.demonArmorVisualScale = scales.demon_armor;
            profile.bloodFrenzyVisualScale = scales.blood_frenzy;
            profile.flipHorizontally = true; // Masters face right; enemy stands on the right and attacks left.
            BindFinalBossEffects(screen);
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
            EditorSceneManager.SaveScene(screen.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Fox skill pack bound: three 8-frame actions and three 6-frame VFX strips.");
        }
    }
}
#endif
