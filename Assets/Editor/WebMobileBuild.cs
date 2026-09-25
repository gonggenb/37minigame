using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WuxiaRoguelite.GameFlow;
using WuxiaRoguelite.Player;
using WuxiaRoguelite.UI;
using WuxiaRoguelite.Audio;

namespace WuxiaRoguelite.Editor
{
    public static class WebMobileBuild
    {
        public const string MenuPath = "Assets/Scenes/BootMenu.unity";
        public const string ProfilePath = "Assets/Settings/Build Profiles/Web Mobile.asset";
        public const string PolicyPath = "ProjectSettings/WebMobileTexturePolicy.json";
        public const string EvidencePath = "docs/validation/webgl_mobile";
        [Serializable] public sealed class TexturePolicy { public bool enabled = true; public bool astc = true; }

        [MenuItem("37 MiniGame/Web Mobile/Apply Mobile Configuration")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuration.");
            Directory.CreateDirectory(EvidencePath);
            BuildMenu();
            var profile = BuildProfile.GetBuildProfileAtPath(ProfilePath);
            if (profile == null)
            {
                var platform = BuildProfile.GetInstalledPlatformModules().First(p => p.displayName == "Web");
                profile = BuildProfile.CreateBuildProfile(platform.platformGuid, "Web Mobile", p => { });
            }
            profile.overrideGlobalScenes = false;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("m_PlatformBuildProfile.m_Development").boolValue = false;
            serialized.FindProperty("m_PlatformBuildProfile.m_ConnectProfiler").boolValue = false;
            serialized.FindProperty("m_PlatformBuildProfile.m_BuildWithDeepProfilingSupport").boolValue = false;
            serialized.FindProperty("m_PlatformBuildProfile.m_AllowDebugging").boolValue = false;
            var optimization = serialized.FindProperty("m_PlatformBuildProfile.m_CodeOptimization");
            optimization.enumValueIndex = Array.IndexOf(optimization.enumNames, "DiskSizeLTO");
            var textures = serialized.FindProperty("m_PlatformBuildProfile.m_WebGLTextureSubtarget");
            textures.enumValueIndex = Array.IndexOf(textures.enumNames, "ASTC");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            BuildProfile.SetActiveBuildProfile(profile);
            PlayerSettings.WebGL.template = "PROJECT:WuxiaResponsive";
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.geometricMemoryGrowthStep = 0.1f;
            PlayerSettings.WebGL.memoryGeometricGrowthCap = 32;
            // This is a growth ceiling, not a 2 GiB initial allocation. Keep room for rare peaks.
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true; // Unity Play controls the response headers.
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.External;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            EditorUtility.SetDirty(profile);
            SetWebQuality();
            File.WriteAllText(PolicyPath, JsonUtility.ToJson(new TexturePolicy(), true));
            int changed = ApplyTextures();
            AssetDatabase.SaveAssets();
            Audit();
            WebGLChineseFontBuildValidator.ValidateOrThrow();
            Debug.Log("Web Mobile configured. Texture overrides updated: " + changed);
        }

        private static void SetWebQuality()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            var settings = new SerializedObject(asset);
            var platforms = settings.FindProperty("m_PerPlatformDefaultQuality");
            for (int i = 0; i < platforms.arraySize; i++)
            {
                var entry = platforms.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == "WebGL")
                    entry.FindPropertyRelative("second").intValue = 1; // Existing Low tier, native platforms unchanged.
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BuildMenu()
        {
            var original = SceneManager.GetActiveScene();
            // Create only once. Repeated configuration must not overwrite a designer's menu edits.
            if (!File.Exists(MenuPath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    var camera = new GameObject("Main Camera").AddComponent<Camera>();
                    camera.tag = "MainCamera";
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = WuxiaUiTheme.BackgroundInk;
                    camera.cullingMask = 0;
                    camera.gameObject.AddComponent<AudioListener>();
                    var light = new GameObject("Menu Light").AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 0f;
                    light.cullingMask = 0;
                    var root = new GameObject("Menu Flow");
                    var stats = root.AddComponent<PlayerStats>();
                    var flow = root.AddComponent<GameFlowController>();
                    flow.playerStats = stats;
                    flow.midBossTriggerElapsedTime = 0;
                    var hud = root.AddComponent<PrototypeHUDController>();
                    hud.gameFlow = flow; hud.playerStats = stats;
                    // Reuse the existing opening dialogue renderer with no battle-art references.
                    var dialogue = root.AddComponent<BattleScreenController>();
                    dialogue.gameFlow = flow; dialogue.playerStats = stats;
                    var music = new GameObject("Menu Music").AddComponent<MainMapMusicController>();
                    music.gameFlow = flow;
                    hud.musicController = music;
                    EditorSceneManager.SaveScene(scene, MenuPath);
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (original.IsValid()) SceneManager.SetActiveScene(original);
                }
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuPath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != MenuPath)).ToArray();
        }

        public static bool ConfigureTexture(TextureImporter importer, bool astc)
        {
            string path = importer.assetPath;
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                !(path.StartsWith("Assets/Art/Generated/") || path.StartsWith("Assets/Resources/"))) return false;
            if (importer.textureType != TextureImporterType.Sprite && importer.textureType != TextureImporterType.Default)
                return false;
            var current = importer.GetPlatformTextureSettings("WebGL");
            var original = importer.GetDefaultPlatformTextureSettings();
            bool background = path.Contains("/Backgrounds/") || path.Contains("/CaveScenes/bg_") ||
                path.Contains("/MainMenu/") || path.Contains("/OpeningDialogue/");
            int cap = original.maxTextureSize;
            // Multi-sprite strips preserve their authored frame dimensions and pivots.
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                if (background) cap = Mathf.Min(cap, 1024);
                else if (path.Contains("/World/") || path.Contains("/Branding/")) cap = Mathf.Min(cap, 512);
                else if (path.Contains("/Icons/") || path.Contains("/ChallengeIcons/")) cap = Mathf.Min(cap, 256);
            }
            var format = astc
                ? (background ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6)
                : TextureImporterFormat.ETC2_RGBA8;
            bool changed = !current.overridden || current.maxTextureSize != cap || current.format != format ||
                current.crunchedCompression || current.compressionQuality != 50;
            if (!changed) return false;
            current.name = "WebGL";
            current.overridden = true;
            current.maxTextureSize = cap;
            current.format = format;
            current.textureCompression = TextureImporterCompression.Compressed;
            current.crunchedCompression = false;
            current.compressionQuality = 50;
            importer.SetPlatformTextureSettings(current);
            return true;
        }

        public static int ApplyTextures()
        {
            var policy = JsonUtility.FromJson<TexturePolicy>(File.ReadAllText(PolicyPath));
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Generated", "Assets/Resources" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer && ConfigureTexture(importer, policy.astc))
                    {
                        AssetDatabase.WriteImportSettingsIfDirty(path);
                        AssetDatabase.ImportAsset(path);
                        changed++;
                    }
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            return changed;
        }

        [Serializable] private sealed class SceneAudit { public string path; public int dependencies, textures, audio, models; }
        [Serializable] private sealed class AuditReport
        {
            public string unity, profile, textureSubtarget, compression;
            public int initialMemoryMiB, maximumMemoryMiB, growthCapMiB;
            public float growthFactor;
            public bool fallback, wasm2023;
            public SceneAudit[] scenes;
        }

        [MenuItem("37 MiniGame/Web Mobile/Audit Build Settings")]
        public static void Audit()
        {
            Directory.CreateDirectory(EvidencePath);
            var report = new AuditReport {
                unity = Application.unityVersion,
                profile = AssetDatabase.GetAssetPath(BuildProfile.GetActiveBuildProfile()),
                textureSubtarget = EditorUserBuildSettings.webGLBuildSubtarget.ToString(),
                compression = PlayerSettings.WebGL.compressionFormat.ToString(),
                initialMemoryMiB = PlayerSettings.WebGL.initialMemorySize,
                maximumMemoryMiB = PlayerSettings.WebGL.maximumMemorySize,
                growthCapMiB = PlayerSettings.WebGL.memoryGeometricGrowthCap,
                growthFactor = PlayerSettings.WebGL.geometricMemoryGrowthStep,
                fallback = PlayerSettings.WebGL.decompressionFallback, wasm2023 = PlayerSettings.WebGL.wasm2023,
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => {
                    var dependencies = AssetDatabase.GetDependencies(s.path, true);
                    return new SceneAudit { path = s.path, dependencies = dependencies.Length,
                        textures = dependencies.Count(p => p.EndsWith(".png")),
                        audio = dependencies.Count(p => p.EndsWith(".wav")),
                        models = dependencies.Count(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) };
                }).ToArray()
            };
            File.WriteAllText(EvidencePath + "/build_settings.json", JsonUtility.ToJson(report, true));
        }

        [MenuItem("37 MiniGame/Web Mobile/Build Release")]
        public static void BuildRelease()
        {
            var profile = BuildProfile.GetBuildProfileAtPath(ProfilePath);
            if (profile == null) throw new BuildFailedException("Apply Mobile Configuration first.");
            BuildProfile.SetActiveBuildProfile(profile);
            WebGLChineseFontBuildValidator.ValidateOrThrow();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions {
                buildProfile = profile, locationPathName = "Builds/WebMobile", options = BuildOptions.None
            });
            Directory.CreateDirectory(EvidencePath);
            File.WriteAllText(EvidencePath + "/build_result.txt", report.summary.result + "\nbytes=" +
                report.summary.totalSize + "\nseconds=" + report.summary.totalTime.TotalSeconds +
                "\nerrors=" + report.summary.totalErrors + "\nwarnings=" + report.summary.totalWarnings);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Web Mobile build failed.");
            Audit();
        }
    }

    public sealed class WebMobileTexturePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!File.Exists(WebMobileBuild.PolicyPath)) return;
            var policy = JsonUtility.FromJson<WebMobileBuild.TexturePolicy>(File.ReadAllText(WebMobileBuild.PolicyPath));
            if (policy != null && policy.enabled) WebMobileBuild.ConfigureTexture((TextureImporter)assetImporter, policy.astc);
        }
    }

    public sealed class WebMobileBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -900;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL || !File.Exists(WebMobileBuild.PolicyPath)) return;
            var profile = BuildProfile.GetActiveBuildProfile();
            var scenes = profile != null ? profile.GetScenesForBuild() : EditorBuildSettings.scenes;
            if (scenes.FirstOrDefault(s => s.enabled)?.path != WebMobileBuild.MenuPath)
                throw new BuildFailedException("Web Mobile requires BootMenu as the first enabled scene.");
        }
    }
}
