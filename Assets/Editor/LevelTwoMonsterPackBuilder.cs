#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.Map;
using WuxiaRoguelite.Runtime;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Visual;

namespace WuxiaRoguelite.EditorTools
{
    /// <summary>Authored level-two appearance distribution. Retains encounter stats, rewards and locations.</summary>
    public static class LevelTwoMonsterPackBuilder
    {
        public static readonly string[] Ids = { "iron_tusk_boar", "scarlet_viper", "strawhat_bandit",
            "gourd_rogue_monk", "lantern_wraith", "moss_mushroom_imp" };
        private static readonly string[] Names = { GameTextCatalog.IronTuskBoarName, GameTextCatalog.ScarletViperName,
            GameTextCatalog.StrawhatBanditName, GameTextCatalog.GourdRogueMonkName,
            GameTextCatalog.LanternWraithName, GameTextCatalog.MossMushroomImpName };
        // Stable PC placement numbers: entry 1-6, town 7-12, plain 13-22,
        // camp 23-30, caves 31-36, pass 37-40. -1 restores the original template appearance.
        // Keep 22 original encounters and introduce three of each new species.
        private static readonly int[] SpeciesBySlot = {
            0,-1,2,-1,-1,5, -1,-1,4,-1,3,-1, -1,1,-1,0,-1,3,-1,-1,-1,2,
            -1,3,2,-1,-1,1,-1,-1, 5,-1,4,5,-1,-1, 0,1,-1,4 };
        public static string SheetPath(string id, string action) =>
            $"Assets/Art/Generated/Characters/Enemies/LevelTwoPack/{id}/spr_enemy_{id}_{action}_right_8f_v01.png";

        [MenuItem("37 MiniGame/Apply Level 2 Monster Pack")]
        public static void ApplyAndSave()
        {
            RequireLevelTwo();
            ApplyToActiveScene();
            WuxiaRoguelite.Editor.WebGLChineseFontBuildValidator.ValidateOrThrow();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("LEVEL2_MONSTERS_APPLIED: 22 original + 18 new normal encounters (3 per new species); stats, rewards and timing retained.");
        }

        private static void RequireLevelTwo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                SceneManager.GetActiveScene().path != PingchuanTownLevelBuilder.ScenePath)
                throw new InvalidOperationException("Open MainPrototype in Edit Mode to apply this level-two pack.");
        }

        public static Sprite[] Frames(string id, string action)
        {
            string path = SheetPath(id, action);
            var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (frames.Length != 8 || frames.Any(s => s.rect.size != new Vector2(256,256) ||
                s.pivot != new Vector2(128,32) || s.pixelsPerUnit != 160 || s.texture.filterMode != FilterMode.Point))
                throw new InvalidOperationException("Invalid eight-frame monster sheet: " + path);
            return frames;
        }

        public static void AddProfilesForLevelTwo(List<BattleScreenController.EnemyVisualProfile> profiles)
        {
            if (SceneManager.GetActiveScene().path != PingchuanTownLevelBuilder.ScenePath) return;
            // The generic prototype builder can run before this optional art pack is installed.
            if (!Ids.All(id => File.Exists(SheetPath(id,"idle")) && File.Exists(SheetPath(id,"attack")))) return;
            for (int i = 0; i < Ids.Length; i++)
            {
                string id = Ids[i];
                profiles.RemoveAll(p => p != null && p.id == id);
                profiles.Add(new BattleScreenController.EnemyVisualProfile {
                    id = id, idleFrames = Frames(id,"idle"), attackFrames = Frames(id,"attack"),
                    scale = 1f, flipHorizontally = true });
            }
        }

        public static void ApplyToActiveScene()
        {
            RequireLevelTwo();
            var encounters = UnityEngine.Object.FindObjectsByType<EncounterTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(e => e.gameObject.scene == SceneManager.GetActiveScene() && e.encounterType == EncounterType.NormalEnemy).ToArray();
            var slots = new Dictionary<int, EncounterTrigger>();
            foreach (var e in encounters)
            {
                int offset = e.name.LastIndexOf(" PC ", StringComparison.Ordinal);
                if (offset < 0 || !int.TryParse(e.name.Substring(offset + 4), out int slot) || slot < 1 || slot > 40 || slots.ContainsKey(slot))
                    throw new InvalidOperationException("Unexpected level-two encounter: " + e.name);
                if (e.enemyStats == null || e.GetComponentInChildren<SpriteFrameAnimator>(true) == null)
                    throw new InvalidOperationException("Missing monster stats/animator: " + e.name);
                slots.Add(slot,e);
            }
            if (slots.Count != 40) throw new InvalidOperationException("Expected all 40 level-two normal enemy slots.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PingchuanTownLevelBuilder.TemplatePath);
            if (prefab == null) throw new InvalidOperationException("Missing original encounter templates.");
            var templates = prefab.GetComponentsInChildren<EncounterTrigger>(true).ToDictionary(e => e.name);
            var originals = new Dictionary<int, EncounterTrigger>();
            foreach (var pair in slots.Where(p => SpeciesBySlot[p.Key-1] < 0))
            {
                string name = pair.Value.name;
                string templateName = name.Substring(0, name.LastIndexOf(" PC ", StringComparison.Ordinal));
                if (!templates.TryGetValue(templateName, out var original) || original.enemyStats == null ||
                    original.GetComponentInChildren<SpriteFrameAnimator>(true) == null ||
                    original.GetComponentInChildren<SpriteFrameAnimator>(true).GetComponent<SpriteRenderer>() == null)
                    throw new InvalidOperationException("Missing original monster appearance: " + name);
                originals.Add(pair.Key, original);
            }
            var screen = UnityEngine.Object.FindFirstObjectByType<BattleScreenController>();
            if (screen == null) throw new InvalidOperationException("Missing battle screen.");
            foreach (string id in Ids) foreach (string action in new[] { "idle", "attack" })
                if (!File.Exists(SheetPath(id, action))) throw new FileNotFoundException(SheetPath(id, action));
            foreach (string id in Ids) foreach (string action in new[] { "idle", "attack" })
            {
                PrototypeSceneBuilder.ConfigureSpriteSheet(SheetPath(id,action),256,256,160,new Vector2(.5f,.125f));
                var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath(id,action));
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.maxTextureSize = 2048; importer.SaveAndReimport();
            }
            var idle = Ids.Select(id => Frames(id,"idle")).ToArray();
            var profiles = (screen.enemyVisualProfiles ?? Array.Empty<BattleScreenController.EnemyVisualProfile>()).ToList();
            AddProfilesForLevelTwo(profiles);
            Undo.RecordObject(screen,"Apply level two monster profiles");
            screen.enemyVisualProfiles = profiles.ToArray(); EditorUtility.SetDirty(screen);
            foreach (var pair in slots)
            {
                int species = SpeciesBySlot[pair.Key-1]; var e = pair.Value;
                var animator = e.GetComponentInChildren<SpriteFrameAnimator>(true);
                var renderer = animator.GetComponent<SpriteRenderer>();
                Undo.RecordObjects(new UnityEngine.Object[] {e,animator,renderer,animator.transform},"Apply level two monster");
                if (species < 0)
                {
                    // Restore explicitly so rerunning after an all-new pack also recovers the old monsters.
                    var original = originals[pair.Key];
                    var source = original.GetComponentInChildren<SpriteFrameAnimator>(true);
                    var sourceRenderer = source.GetComponent<SpriteRenderer>();
                    e.enemyStats.displayName = original.enemyStats.displayName; e.enemyStats.visualId = original.enemyStats.visualId;
                    animator.idleFrames = source.idleFrames; animator.moveFrames = source.moveFrames;
                    animator.framesPerSecond = source.framesPerSecond; animator.randomizeStart = source.randomizeStart;
                    animator.transform.localPosition = source.transform.localPosition;
                    animator.transform.localScale = source.transform.localScale;
                    renderer.sprite = sourceRenderer.sprite; renderer.flipX = sourceRenderer.flipX;
                }
                else
                {
                    e.enemyStats.displayName = Names[species]; e.enemyStats.visualId = Ids[species];
                    animator.idleFrames = idle[species]; animator.moveFrames = idle[species]; animator.framesPerSecond = 8;
                    animator.transform.localPosition = Vector3.zero; animator.transform.localScale = Vector3.one * 1.15f;
                    renderer.sprite = idle[species][0]; renderer.flipX = false;
                }
                EditorUtility.SetDirty(e); EditorUtility.SetDirty(animator); EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(animator.transform);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
#endif
